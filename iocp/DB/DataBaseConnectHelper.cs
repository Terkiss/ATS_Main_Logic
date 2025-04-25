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
        string uri = "Server=private.dotge.site;Port=3306;Database=books;Uid=ade345;Pwd=dbslwms123";

        private MySqlConnection conn;
        public DataBaseConnectHelper()
        { 
        
        }

        public DataBaseConnectHelper(string connectionStr)
        {
            this.uri = connectionStr;
        }

        public void HelperStart()
        {
            MySqlConnection connection = new MySqlConnection(uri);

            connection.Open();

            conn = connection;
        }

        /// <summary>
        /// SQL RUN  NO RESULT
        /// </summary>
        /// <param name="sql"></param>
        public void sqlRun(string sql)
        {
            using (var conn = dataBaseOpen(uri))
            {
                conn.Open();
                using (MySqlCommand cmd = (MySqlCommand)conn.CreateCommand())
                {

                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public void sqlRun(string sql, MySqlParameter[] parameter)
        {
            using (var conn = dataBaseOpen(uri))
            {
                conn.Open();
                using (MySqlCommand cmd = (MySqlCommand)conn.CreateCommand())
                {

                    cmd.CommandText = sql;
                    foreach(var item in parameter)
                    {
                        cmd.Parameters.Add(item);
                    }
                  
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public MySqlConnection dataBaseOpen(string databaseURI)
        {
            return new MySqlConnection(databaseURI);
        }



        /// <summary>
        /// SQL BATCH RUN NO RESULT
        /// </summary>
        /// <param name="sqls"></param>
        public void sqlBatchRun(List<string> sqls)
        {

            using (MySqlConnection conn = dataBaseOpen(uri))
            {


                conn.Open();

                MySqlTransaction transaction = conn.BeginTransaction();


                foreach (var sql in sqls)
                {
                    MySqlCommand cmd2 = new MySqlCommand(sql, conn, transaction);
                    cmd2.ExecuteNonQuery();

                }
                transaction.Commit();
                //conn.Close();
                //conn.Dispose();
            }
        }


        public async void sqlParrelRun(string sql)
        {
            using (MySqlConnection conn = dataBaseOpen(uri))
            {
                await conn.OpenAsync();

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// sql 결과물의 열 수를 반환 합니다.
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public int sqlrunForCounter(string sql)
        {
            int i = 0;
            using (var conn = dataBaseOpen(uri))
            {

                conn.Open();




                using (MySqlCommand cmd = (MySqlCommand)conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    //var reader = cmd.ExecuteReader();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            i++;
                        }

                    }

                }
                //conn.Close();
                //conn.Dispose();
            }

            return i;
        }

        /// <summary>
        /// sql Result 함수
        /// 
        /// </summary>
        /// <param name="reader">mysql reader</param>
        public delegate void SqlResult(MySqlDataReader reader);

        /// <summary>
        /// sql run 결과로 mysqldatareader를 반환 합니다
        /// 발견된 이슈는thread 풀이 꽉 채워지는 문제가 있음
        /// 풀이 꽉 채워져서 database가 잠기는 문제  
        /// 사용하지 말것 
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public MySqlDataReader sqlRunResult(string sql)
        {
            var conn = dataBaseOpen(uri);
            conn.Open();
            MySqlDataReader reader;
            using (MySqlCommand cmd = (MySqlCommand)conn.CreateCommand())
            {
                cmd.CommandText = sql;
                reader = cmd.ExecuteReader();

                return reader;
            }
        }

        /// <summary>
        /// 데이터 베이스 sql를 실행하고 
        /// 받은 mysqldatareader를 콜백 을 이용해서 외부에서 처리하고 
        /// 콜백 콜이 끝나면 해당 연결을 닫습니다.
        /// 
        /// 
        /// </summary>
        /// <param name="sql"> sql 문</param>
        /// <param name="sqlResult"> 콜백 </param>
        public void sqlRunResult(string sql, SqlResult sqlResult)
        {
            using (var conn = dataBaseOpen(uri))
            {
                conn.Open();
                // MySqlDataReader reader;
                using (MySqlCommand cmd = (MySqlCommand)conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    // reader = cmd.ExecuteReader();


                    using (var reader = cmd.ExecuteReader())
                    {
                        sqlResult?.Invoke(reader);
                    }
                }
                //conn.Close();
                //conn.Dispose();
            }
        }


        /// <summary>
        /// 테이블 이름과 filed 데이터만 넣으면 자동으로 데이터 베이스에 입력 합니다.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="field"></param>
        public void insert(string tableName, string[] field)
        {
            string fieldSelect = "show full columns FROM " + tableName;

            List<string> fieldName = new List<string>();

            var conn = dataBaseOpen(uri);
            conn.Open();

            using (MySqlCommand cmd = (MySqlCommand)conn.CreateCommand())
            {
                cmd.CommandText = fieldSelect;

                using (MySqlDataReader reader = (MySqlDataReader)cmd.ExecuteReader())
                {
                    // 데이터 베이스 컬럼 조회
                    while (reader.Read())
                    {
                        fieldName.Add(reader.GetString(0));
                    }
                }

                string sql = insertCommandGen(fieldName, tableName, field);

                cmd.CommandText = sql;

                cmd.ExecuteNonQuery();
            }
            conn.Close();
            conn.Dispose();
        }

        /// <summary>
        /// sql 젠
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="tableName"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        private string insertCommandGen(List<string> fieldName, string tableName, string[] field)
        {
            string sqlCommand = "";
            if (fieldName.Count == field.Length)
            {
                sqlCommand = "insert into " + tableName + "(";

                for (int i = 0; i < fieldName.Count; i++)
                {
                    if (i == fieldName.Count - 1)
                    {
                        sqlCommand += fieldName[i] + ") values(";
                    }
                    else
                    {
                        sqlCommand += fieldName[i] + ",";
                    }
                }
                for (int i = 0; i < field.Length; i++)
                {
                    if (i == field.Length - 1)
                    {
                        sqlCommand += "'" + field[i] + "') ;";
                    }
                    else
                    {
                        sqlCommand += "'" + field[i] + "',";
                    }
                }
                return sqlCommand;
            }
            return sqlCommand;
        }
    }
}
