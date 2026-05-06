using TeruTeruServer.SDK.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace R19Management
{
    public class DataBaseConnectHelper
    {
        private string uri;
        public DataBaseConnectHelper()
        {
        }

        public DataBaseConnectHelper(string connectionStr)
        {
            this.uri = connectionStr;
        }

        private MySqlConnection dataBaseOpen()
        {
            return new MySqlConnection(uri);
        }

        /// <summary>
        /// SQL RUN NO RESULT
        /// </summary>
        public void sqlRun(string sql, MySqlParameter[] parameters = null)
        {
            using (var conn = dataBaseOpen())
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    if (parameters != null)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// SQL BATCH RUN NO RESULT
        /// </summary>
        public void sqlBatchRun(List<string> sqls)
        {
            using (MySqlConnection conn = dataBaseOpen())
            {
                conn.Open();
                using (MySqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (var sql in sqls)
                        {
                            using (MySqlCommand cmd = new MySqlCommand(sql, conn, transaction))
                            {
                                cmd.ExecuteNonQuery();
                            }
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
        }

        public async Task sqlParrelRun(string sql, MySqlParameter[] parameters = null)
        {
            using (MySqlConnection conn = dataBaseOpen())
            {
                await conn.OpenAsync();
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    if (parameters != null)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// sql 결과물의 열 수를 반환 합니다.
        /// </summary>
        public int sqlrunForCounter(string sql, MySqlParameter[] parameters = null)
        {
            int i = 0;
            using (var conn = dataBaseOpen())
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    if (parameters != null)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            i++;
                        }
                    }
                }
            }
            return i;
        }

        public delegate void SqlResult(MySqlDataReader reader);

        /// <summary>
        /// 데이터 베이스 sql를 실행하고 콜백을 이용하여 처리합니다.
        /// </summary>
        public void sqlRunResult(string sql, SqlResult sqlResult, MySqlParameter[] parameters = null)
        {
            using (var conn = dataBaseOpen())
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    if (parameters != null)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }
                    using (var reader = cmd.ExecuteReader())
                    {
                        sqlResult?.Invoke(reader);
                    }
                }
            }
        }

        /// <summary>
        /// 테이블 이름과 field 데이터만 넣으면 자동으로 데이터 베이스에 입력 합니다. (매개변수화된 쿼리 사용)
        /// </summary>
        public void insert(string tableName, string[] field)
        {
            string fieldSelect = "show full columns FROM " + tableName;
            List<string> fieldNames = new List<string>();

            using (var conn = dataBaseOpen())
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(fieldSelect, conn))
                {
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            fieldNames.Add(reader.GetString(0));
                        }
                    }
                }

                if (fieldNames.Count == field.Length)
                {
                    StringBuilder sqlBuilder = new StringBuilder();
                    sqlBuilder.Append($"INSERT INTO {tableName} (");
                    sqlBuilder.Append(string.Join(", ", fieldNames));
                    sqlBuilder.Append(") VALUES (");

                    List<MySqlParameter> parameters = new List<MySqlParameter>();
                    for (int i = 0; i < field.Length; i++)
                    {
                        string paramName = $"@p{i}";
                        sqlBuilder.Append(i == field.Length - 1 ? paramName : paramName + ", ");
                        parameters.Add(new MySqlParameter(paramName, field[i]));
                    }
                    sqlBuilder.Append(");");

                    using (MySqlCommand insertCmd = new MySqlCommand(sqlBuilder.ToString(), conn))
                    {
                        insertCmd.Parameters.AddRange(parameters.ToArray());
                        insertCmd.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
