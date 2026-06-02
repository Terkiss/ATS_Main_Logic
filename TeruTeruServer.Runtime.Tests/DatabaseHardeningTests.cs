using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using TeruTeruServer.Runtime.DB;
using R19Management;
using Xunit;

namespace TeruTeruServer.Runtime.Tests
{
    public class DatabaseHardeningTests
    {
        // 생성자 없이 MySqlException 객체를 안전하게 동적 생성하는 헬퍼
        private static MySqlException CreateMySqlException(int number, string message)
        {
            var exception = (MySqlException)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(MySqlException));

            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;

            // Exception._message 설정
            var messageField = typeof(Exception).GetField("_message", flags);
            if (messageField != null)
            {
                messageField.SetValue(exception, message);
            }

            // MySqlException.number (또는 _number, errorCode, code, errno 등) 설정
            var fields = typeof(MySqlException).GetFields(flags);
            bool set = false;
            foreach (var f in fields)
            {
                if (f.FieldType == typeof(int) && 
                    (f.Name.Contains("number", StringComparison.OrdinalIgnoreCase) || 
                     f.Name.Contains("code", StringComparison.OrdinalIgnoreCase) || 
                     f.Name.Contains("errno", StringComparison.OrdinalIgnoreCase)))
                {
                    f.SetValue(exception, number);
                    set = true;
                }
            }

            // Base class인 DbException 등도 검사
            if (!set)
            {
                var baseType = typeof(MySqlException).BaseType;
                while (baseType != null && baseType != typeof(object))
                {
                    var baseFields = baseType.GetFields(flags);
                    foreach (var f in baseFields)
                    {
                        if (f.FieldType == typeof(int) && 
                            (f.Name.Contains("number", StringComparison.OrdinalIgnoreCase) || 
                             f.Name.Contains("code", StringComparison.OrdinalIgnoreCase) || 
                             f.Name.Contains("errno", StringComparison.OrdinalIgnoreCase)))
                        {
                            f.SetValue(exception, number);
                            set = true;
                        }
                    }
                    baseType = baseType.BaseType;
                }
            }

            return exception;
        }

        [Fact]
        public void ConnectionPool_ShouldBeReinforced()
        {
            // Given
            string rawUri = "Server=localhost;Port=3306;Database=test;Uid=root;Pwd=pw";

            // When
            var helper = new DatabaseConnector.DatabaseHelper(rawUri);

            // Then
            var uriField = typeof(DatabaseConnector.DatabaseHelper).GetField("uri", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(uriField);
            var finalizedUri = (string)uriField.GetValue(helper)!;

            // MySqlConnectionStringBuilder를 사용하여 파싱 후 검증
            var builder = new MySqlConnectionStringBuilder(finalizedUri);
            Assert.True(builder.Pooling);
            Assert.Equal(100u, builder.MaximumPoolSize);
            Assert.Equal(10u, builder.MinimumPoolSize);
        }

        [Fact]
        public void DatabaseHelper_SqlRun_ShouldRetryOnTransientDeadlock()
        {
            // Given
            int executionCount = 0;
            var transientException = CreateMySqlException(1213, "Deadlock found when trying to get lock");

            Func<MockDbCommand, object> onExecute = (cmd) =>
            {
                executionCount++;
                throw transientException;
            };

            var mockConn = new MockDbConnection(() => new MockDbCommand(onExecute));
            var helper = new DatabaseConnector.DatabaseHelper("Server=localhost;Database=test;Uid=root;Pwd=pw", (str) => mockConn);

            // When & Then
            var ex = Assert.Throws<MySqlException>(() => helper.SqlRun("SELECT 1"));

            Assert.Equal(4, executionCount);
            Assert.Equal(1213, ex.Number);
        }

        [Fact]
        public void DatabaseHelper_SqlRun_ShouldNotRetryOnNonTransientException()
        {
            // Given
            int executionCount = 0;
            var nonTransientException = CreateMySqlException(1045, "Access denied for user 'root'@'localhost'");

            Func<MockDbCommand, object> onExecute = (cmd) =>
            {
                executionCount++;
                throw nonTransientException;
            };

            var mockConn = new MockDbConnection(() => new MockDbCommand(onExecute));
            var helper = new DatabaseConnector.DatabaseHelper("Server=localhost;Database=test;Uid=root;Pwd=pw", (str) => mockConn);

            // When & Then
            var ex = Assert.Throws<MySqlException>(() => helper.SqlRun("SELECT 1"));

            Assert.Equal(1, executionCount);
            Assert.Equal(1045, ex.Number);
        }

        [Fact]
        public async Task DatabaseHelper_SqlParrelRun_ShouldRetryOnTransientExceptionAsync()
        {
            // Given
            int executionCount = 0;
            var transientException = CreateMySqlException(1205, "Lock wait timeout exceeded");

            Func<MockDbCommand, object> onExecute = (cmd) =>
            {
                executionCount++;
                throw transientException;
            };

            var mockConn = new MockDbConnection(() => new MockDbCommand(onExecute));
            var helper = new DatabaseConnector.DatabaseHelper("Server=localhost;Database=test;Uid=root;Pwd=pw", (str) => mockConn);

            // When & Then
            var ex = await Assert.ThrowsAsync<MySqlException>(async () => await helper.SqlParrelRun("SELECT 1"));

            Assert.Equal(4, executionCount);
            Assert.Equal(1205, ex.Number);
        }

        [Fact]
        public void DataBaseConnectHelper_Insert_ShouldQueryColumnsAndExecuteInsert()
        {
            // Given
            bool columnsQueried = false;
            bool insertExecuted = false;

            Func<MockDbCommand, object> onExecute = (cmd) =>
            {
                if (cmd.CommandText != null && cmd.CommandText.Contains("show full columns"))
                {
                    columnsQueried = true;
                    var rows = new List<Dictionary<string, object>>
                    {
                        new Dictionary<string, object> { { "Field", "id" } },
                        new Dictionary<string, object> { { "Field", "name" } }
                    };
                    return new MockDataReader(rows);
                }
                else if (cmd.CommandText != null && cmd.CommandText.Contains("INSERT INTO"))
                {
                    insertExecuted = true;
                    return 1;
                }
                return 0;
            };

            var mockConn = new MockDbConnection(() => new MockDbCommand(onExecute));
            var helper = new DataBaseConnectHelper("Server=localhost;Database=test;Uid=root;Pwd=pw", (str) => mockConn);

            // When
            helper.insert("players", new string[] { "123", "User1" });

            // Then
            Assert.True(columnsQueried);
            Assert.True(insertExecuted);
        }
    }

    #region Mock ADO.NET Implementation

    public class MockDbConnection : DbConnection
    {
        private string? _connectionString;
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override string ConnectionString 
        { 
            get => _connectionString ?? ""; 
            set => _connectionString = value; 
        }

        public override string Database => "mock";
        public override string DataSource => "localhost";
        public override string ServerVersion => "8.0";
        public override ConnectionState State => ConnectionState.Open; // Dapper에서 Open검사를 하므로 Open으로 설정

        private readonly Func<MockDbCommand> _commandFactory;
        public int OpenCount { get; private set; }

        public MockDbConnection(Func<MockDbCommand> commandFactory)
        {
            _commandFactory = commandFactory;
        }

        public override void Open()
        {
            OpenCount++;
        }

        public override void Close() { }

        protected override DbCommand CreateDbCommand()
        {
            var cmd = _commandFactory();
            cmd.Connection = this;
            return cmd;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return new MockDbTransaction(this);
        }

        public override void ChangeDatabase(string databaseName) { }
    }

    public class MockDbTransaction : DbTransaction
    {
        private readonly DbConnection _conn;
        protected override DbConnection DbConnection => _conn;
        public override IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;

        public MockDbTransaction(DbConnection conn)
        {
            _conn = conn;
        }

        public override void Commit() { }
        public override void Rollback() { }
    }

    public class MockDbCommand : DbCommand
    {
        protected override DbConnection? DbConnection { get; set; }
        protected override DbTransaction? DbTransaction { get; set; }

        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override string CommandText { get; set; } = "";
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; } = UpdateRowSource.None;

        protected override DbParameterCollection DbParameterCollection { get; } = new MockDbParameterCollection();

        public int ExecuteNonQueryCount { get; private set; }
        public int ExecuteScalarCount { get; private set; }
        public int ExecuteReaderCount { get; private set; }

        private readonly Func<MockDbCommand, object> _onExecute;

        public MockDbCommand(Func<MockDbCommand, object> onExecute)
        {
            _onExecute = onExecute;
        }

        public override void Cancel() { }

        protected override DbParameter CreateDbParameter()
        {
            return new MockDbParameter();
        }

        public override int ExecuteNonQuery()
        {
            ExecuteNonQueryCount++;
            var res = _onExecute(this);
            return Convert.ToInt32(res);
        }

        public override object ExecuteScalar()
        {
            ExecuteScalarCount++;
            return _onExecute(this);
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            ExecuteReaderCount++;
            return (DbDataReader)_onExecute(this);
        }

        public override void Prepare() { }
    }

    public class MockDbParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; }
        public override bool IsNullable { get; set; }
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override string ParameterName { get; set; } = "";
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override string SourceColumn { get; set; } = "";
        public override bool SourceColumnNullMapping { get; set; }
        public override object? Value { get; set; }
        public override int Size { get; set; }

        public override void ResetDbType() { }
    }

    public class MockDbParameterCollection : DbParameterCollection
    {
        private readonly List<object> _list = new();

        public override int Count => _list.Count;
        public override object SyncRoot => _list;
        public override bool IsFixedSize => false;
        public override bool IsReadOnly => false;
        public override bool IsSynchronized => false;

        public override int Add(object value)
        {
            _list.Add(value);
            return _list.Count - 1;
        }

        public override void AddRange(Array values)
        {
            foreach (var val in values) _list.Add(val!);
        }

        public override void Clear() => _list.Clear();
        public override bool Contains(object value) => _list.Contains(value);
        public override int IndexOf(object value) => _list.IndexOf(value);
        public override void Insert(int index, object value) => _list.Insert(index, value);
        public override void Remove(object value) => _list.Remove(value);
        public override void RemoveAt(int index) => _list.RemoveAt(index);

        protected override DbParameter GetParameter(int index) => (DbParameter)_list[index];
        protected override DbParameter GetParameter(string parameterName) => throw new NotImplementedException();
        protected override void SetParameter(int index, DbParameter value) => _list[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value) => throw new NotImplementedException();

        public override bool Contains(string value) => false;
        public override int IndexOf(string value) => -1;
        public override void RemoveAt(string value) { }

        public override void CopyTo(Array array, int index)
        {
            ((System.Collections.ICollection)_list).CopyTo(array, index);
        }

        public override System.Collections.IEnumerator GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }

    public class MockDataReader : DbDataReader
    {
        private readonly List<Dictionary<string, object>> _rows;
        private int _currentIndex = -1;

        public MockDataReader(List<Dictionary<string, object>> rows)
        {
            _rows = rows;
        }

        public override object this[int ordinal] => GetValue(ordinal);
        public override object this[string name] => GetValue(GetOrdinal(name));

        public override object GetValue(int ordinal) => _rows[_currentIndex].Values.ElementAt(ordinal);
        public override int FieldCount => _rows.Count > 0 ? _rows[0].Count : 0;
        public override int Depth => 0;
        public override bool IsClosed => false;
        public override int RecordsAffected => 0;
        public override bool HasRows => _rows.Count > 0;

        public override bool Read()
        {
            _currentIndex++;
            return _currentIndex < _rows.Count;
        }

        public override bool NextResult() => false;

        public override string GetName(int ordinal) => _rows[0].Keys.ElementAt(ordinal);
        public override int GetOrdinal(string name) => _rows[0].Keys.ToList().IndexOf(name);

        public override string GetDataTypeName(int ordinal) => typeof(string).Name;
        public override Type GetFieldType(int ordinal) => typeof(string);

        public override int GetValues(object[] values) => 0;
        public override bool GetBoolean(int ordinal) => false;
        public override byte GetByte(int ordinal) => 0;
        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => 0;
        public override char GetChar(int ordinal) => '\0';
        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => 0;
        public override Guid GetGuid(int ordinal) => Guid.Empty;
        public override short GetInt16(int ordinal) => 0;
        public override int GetInt32(int ordinal) => 0;
        public override long GetInt64(int ordinal) => 0;
        public override float GetFloat(int ordinal) => 0;
        public override double GetDouble(int ordinal) => 0;
        public override string GetString(int ordinal) => GetValue(ordinal).ToString()!;
        public override decimal GetDecimal(int ordinal) => 0;
        public override DateTime GetDateTime(int ordinal) => DateTime.MinValue;
        public override bool IsDBNull(int ordinal) => GetValue(ordinal) == null;

        public override System.Collections.IEnumerator GetEnumerator() => _rows.GetEnumerator();
    }

    #endregion
}
