using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace TeruTeruServer.ServerEngineSDK.Interfaces
{
    public interface IDatabaseService
    {
        void SqlRun(string sql, MySqlParameter[] parameters = null);
        void SqlBatchRun(List<string> sqls);
        Task SqlParrelRun(string sql, MySqlParameter[] parameters = null);
        int SqlRunForCounter(string sql, MySqlParameter[] parameters = null);
        void Insert(string tableName, string[] field);
    }
}
