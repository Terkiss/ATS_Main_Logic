using TeruTeruServer.SDK.Interfaces;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TeruTeruServer.Runtime.DB
{
    /// <summary>
    /// 데이터 베이스 커넥터 (Public 접근 허용)
    /// </summary>
    public class DatabaseConnector
    {
        public static DatabaseHelper database = null;
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
            public DatabaseHelper(string uri)
            {
                this.uri = uri;
            }

            public int SqlRunForCounter(string query, MySqlParameter[] parameters)
            {
                using (var conn = new MySqlConnection(uri))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        if (parameters != null) cmd.Parameters.AddRange(parameters);
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }

            public MySqlDataReader SqlRunForReader(string query, MySqlParameter[] parameters)
            {
                var conn = new MySqlConnection(uri);
                conn.Open();
                var cmd = new MySqlCommand(query, conn);
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteReader(System.Data.CommandBehavior.CloseConnection);
            }

            public void SqlRunForNoReturn(string query, MySqlParameter[] parameters)
            {
                using (var conn = new MySqlConnection(uri))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        if (parameters != null) cmd.Parameters.AddRange(parameters);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            // 인터페이스 미구현 멤버 추가
            public void SqlRun(string query, MySqlParameter[] parameters) => SqlRunForNoReturn(query, parameters);
            public void SqlBatchRun(List<string> queries) { /* TODO */ }
            public Task SqlParrelRun(string query, MySqlParameter[] parameters) => Task.CompletedTask;
            public void Insert(string table, string[] values) { /* TODO */ }
        }
    }
}
