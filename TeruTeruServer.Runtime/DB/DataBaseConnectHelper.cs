using TeruTeruServer.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using MySql.Data;
using MySql.Data.MySqlClient;
using Dapper;
using Polly;
using Polly.Retry;

namespace R19Management
{
    public class DataBaseConnectHelper
    {
        private string uri = string.Empty;
        private readonly Func<string, IDbConnection> _connectionFactory;

        private static bool IsTransient(Exception ex)
        {
            if (ex is MySqlException mysqlEx)
            {
                return mysqlEx.Number == 1213 || mysqlEx.Number == 1205 || mysqlEx.Number == 1040 ||
                       mysqlEx.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        private static readonly ResiliencePipeline ResiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => IsTransient(ex)),
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = false,
                Delay = TimeSpan.FromSeconds(1)
            })
            .Build();

        public DataBaseConnectHelper()
        {
            this._connectionFactory = connStr => new MySqlConnection(connStr);
        }

        public DataBaseConnectHelper(string connectionStr, Func<string, IDbConnection>? connectionFactory = null)
        {
            var builder = new MySqlConnectionStringBuilder(connectionStr);
            if (!builder.Pooling)
            {
                builder.Pooling = true;
            }
            if (builder.MaximumPoolSize < 100)
            {
                builder.MaximumPoolSize = 100;
            }
            if (builder.MinimumPoolSize < 10)
            {
                builder.MinimumPoolSize = 10;
            }
            this.uri = builder.ConnectionString;

            this._connectionFactory = connectionFactory ?? (connStr => new MySqlConnection(connStr));
        }

        private DynamicParameters ConvertParams(MySqlParameter[]? parameters)
        {
            var dynamicParams = new DynamicParameters();
            if (parameters != null)
            {
                foreach (var p in parameters)
                {
                    dynamicParams.Add(p.ParameterName, p.Value, p.DbType, p.Direction, p.Size);
                }
            }
            return dynamicParams;
        }

        private IDbConnection dataBaseOpen()
        {
            var conn = _connectionFactory(uri);
            if (conn.State != ConnectionState.Open) conn.Open();
            return conn;
        }

        /// <summary>
        /// SQL RUN NO RESULT
        /// </summary>
        public void sqlRun(string sql, MySqlParameter[]? parameters = null)
        {
            ResiliencePipeline.Execute(() =>
            {
                using (var conn = dataBaseOpen())
                {
                    conn.Execute(sql, ConvertParams(parameters));
                }
            });
        }

        /// <summary>
        /// SQL BATCH RUN NO RESULT
        /// </summary>
        public void sqlBatchRun(List<string> sqls)
        {
            if (sqls == null || sqls.Count == 0) return;

            ResiliencePipeline.Execute(() =>
            {
                using (IDbConnection conn = dataBaseOpen())
                {
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            foreach (var sql in sqls)
                            {
                                conn.Execute(sql, transaction: transaction);
                            }
                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            });
        }

        public async Task sqlParrelRun(string sql, MySqlParameter[]? parameters = null)
        {
            await ResiliencePipeline.ExecuteAsync(async token =>
            {
                using (IDbConnection conn = dataBaseOpen())
                {
                    await conn.ExecuteAsync(sql, ConvertParams(parameters));
                }
            });
        }

        /// <summary>
        /// sql 결과물의 열 수를 반환 합니다.
        /// </summary>
        public int sqlrunForCounter(string sql, MySqlParameter[]? parameters = null)
        {
            return ResiliencePipeline.Execute(() =>
            {
                using (var conn = dataBaseOpen())
                {
                    var result = conn.Query(sql, ConvertParams(parameters));
                    return result.Count();
                }
            });
        }

        public delegate void SqlResult(MySqlDataReader reader);

        /// <summary>
        /// 데이터 베이스 sql를 실행하고 콜백을 이용하여 처리합니다.
        /// </summary>
        public void sqlRunResult(string sql, SqlResult sqlResult, MySqlParameter[]? parameters = null)
        {
            ResiliencePipeline.Execute(() =>
            {
                using (var conn = dataBaseOpen())
                {
                    using (var reader = conn.ExecuteReader(sql, ConvertParams(parameters)))
                    {
                        sqlResult?.Invoke((MySqlDataReader)reader);
                    }
                }
            });
        }

        /// <summary>
        /// 테이블 이름과 field 데이터만 넣으면 자동으로 데이터 베이스에 입력 합니다. (매개변수화된 쿼리 사용)
        /// </summary>
        public void insert(string tableName, string[] field)
        {
            if (field == null) return;

            ResiliencePipeline.Execute(() =>
            {
                using (var conn = dataBaseOpen())
                {
                    string fieldSelect = "show full columns FROM " + tableName;
                    var columns = conn.Query(fieldSelect);
                    var fieldNames = new List<string>();
                    foreach (var col in columns)
                    {
                        var dict = (IDictionary<string, object>)col;
                        if (dict.TryGetValue("Field", out var fieldName) || dict.Values.Count > 0)
                        {
                            var nameObj = fieldName ?? dict.Values.FirstOrDefault();
                            if (nameObj != null)
                            {
                                fieldNames.Add(nameObj.ToString() ?? string.Empty);
                            }
                        }
                    }

                    if (fieldNames.Count == field.Length)
                    {
                        StringBuilder sqlBuilder = new StringBuilder();
                        sqlBuilder.Append($"INSERT INTO {tableName} (");
                        sqlBuilder.Append(string.Join(", ", fieldNames));
                        sqlBuilder.Append(") VALUES (");

                        var dynamicParams = new DynamicParameters();
                        for (int i = 0; i < field.Length; i++)
                        {
                            string paramName = $"p{i}";
                            sqlBuilder.Append(i == field.Length - 1 ? $"@{paramName}" : $"@{paramName}, ");
                            dynamicParams.Add(paramName, field[i]);
                        }
                        sqlBuilder.Append(");");

                        conn.Execute(sqlBuilder.ToString(), dynamicParams);
                    }
                }
            });
        }
    }
}
