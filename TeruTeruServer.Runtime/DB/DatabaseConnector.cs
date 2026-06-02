using TeruTeruServer.SDK.Interfaces;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Dapper;
using Polly;
using Polly.Retry;

namespace TeruTeruServer.Runtime.DB
{
    /// <summary>
    /// 데이터 베이스 커넥터 (Public 접근 허용)
    /// </summary>
    public class DatabaseConnector
    {
        public static DatabaseHelper? database = null;
        string bindUri = "Server={0};Port=3306;Database={1};Uid={2};Pwd={3}";

        public DatabaseConnector(string ip, string useDatabase, string id, string pwd)
        {
            string uriAddress = string.Format(bindUri, ip, useDatabase, id, pwd);

            if (database == null)
            {
                database = new DatabaseHelper(uriAddress);
            }
        }

        public class DatabaseHelper : IDatabaseService
        {
            private string uri;
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

            public DatabaseHelper(string uri, Func<string, IDbConnection>? connectionFactory = null)
            {
                // 커넥션 풀링 관련 속성 점검 및 강제 활성화
                var builder = new MySqlConnectionStringBuilder(uri);
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

                // 테스트에서 Mock Connection을 주입할 수 있도록 Factory 설정
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

            public int SqlRunForCounter(string query, MySqlParameter[]? parameters = null)
            {
                return ResiliencePipeline.Execute(() =>
                {
                    using (var conn = _connectionFactory(uri))
                    {
                        if (conn.State != ConnectionState.Open) conn.Open();
                        return Convert.ToInt32(conn.ExecuteScalar(query, ConvertParams(parameters)));
                    }
                });
            }

            public MySqlDataReader SqlRunForReader(string query, MySqlParameter[]? parameters = null)
            {
                return ResiliencePipeline.Execute(() =>
                {
                    var conn = _connectionFactory(uri);
                    if (conn.State != ConnectionState.Open) conn.Open();
                    
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = query;
                    if (parameters != null)
                    {
                        foreach (var p in parameters)
                        {
                            var param = cmd.CreateParameter();
                            param.ParameterName = p.ParameterName;
                            param.Value = p.Value;
                            param.DbType = p.DbType;
                            param.Direction = p.Direction;
                            param.Size = p.Size;
                            cmd.Parameters.Add(param);
                        }
                    }
                    
                    var reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                    return (MySqlDataReader)reader;
                });
            }

            public void SqlRunForNoReturn(string query, MySqlParameter[]? parameters = null)
            {
                ResiliencePipeline.Execute(() =>
                {
                    using (var conn = _connectionFactory(uri))
                    {
                        if (conn.State != ConnectionState.Open) conn.Open();
                        conn.Execute(query, ConvertParams(parameters));
                    }
                });
            }

            public void SqlRun(string query, MySqlParameter[]? parameters = null) => SqlRunForNoReturn(query, parameters);

            public void SqlBatchRun(List<string> queries)
            {
                if (queries == null || queries.Count == 0) return;

                ResiliencePipeline.Execute(() =>
                {
                    using (var conn = _connectionFactory(uri))
                    {
                        if (conn.State != ConnectionState.Open) conn.Open();
                        using (var transaction = conn.BeginTransaction())
                        {
                            try
                            {
                                foreach (var query in queries)
                                {
                                    conn.Execute(query, transaction: transaction);
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

            public async Task SqlParrelRun(string query, MySqlParameter[]? parameters = null)
            {
                await ResiliencePipeline.ExecuteAsync(async token =>
                {
                    using (var conn = _connectionFactory(uri))
                    {
                        if (conn.State != ConnectionState.Open) conn.Open();
                        await conn.ExecuteAsync(query, ConvertParams(parameters));
                    }
                });
            }

            public void Insert(string tableName, string[] field)
            {
                if (field == null) return;

                ResiliencePipeline.Execute(() =>
                {
                    using (var conn = _connectionFactory(uri))
                    {
                        if (conn.State != ConnectionState.Open) conn.Open();
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
}
