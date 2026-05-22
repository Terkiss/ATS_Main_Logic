using System;
using StackExchange.Redis;

namespace TeruTeruServer.Runtime.Clustering
{
    /// <summary>
    /// ConnectionMultiplexer 자원을 싱글톤 형태로 관리하고 상태를 체크할 수 있는 프로바이더 클래스입니다.
    /// </summary>
    public class RedisConnectionProvider : IDisposable
    {
        private static readonly object _lock = new();
        private static RedisConnectionProvider? _instance;
        
        private readonly string _connectionString;
        private readonly Lazy<ConnectionMultiplexer?> _lazyConnection;

        public static RedisConnectionProvider GetInstance(string connectionString)
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new RedisConnectionProvider(connectionString);
                    }
                }
            }
            return _instance;
        }

        private RedisConnectionProvider(string connectionString)
        {
            _connectionString = connectionString;
            _lazyConnection = new Lazy<ConnectionMultiplexer?>(() =>
            {
                try
                {
                    var options = ConfigurationOptions.Parse(_connectionString);
                    options.ConnectTimeout = 2000; // 2초 제한
                    options.SyncTimeout = 2000;
                    options.AbortOnConnectFail = false; // 연결 실패해도 재시도할 수 있도록 처리
                    
                    var muxer = ConnectionMultiplexer.Connect(options);
                    muxer.ConnectionFailed += (sender, e) =>
                    {
                        Console.WriteLine($"[Redis] Connection failed: {e.Exception?.Message}");
                    };
                    muxer.ConnectionRestored += (sender, e) =>
                    {
                        Console.WriteLine("[Redis] Connection restored.");
                    };
                    return muxer;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Redis] Error initializing ConnectionMultiplexer: {ex.Message}");
                    return null;
                }
            });
        }

        public ConnectionMultiplexer? Connection => _lazyConnection.Value;

        public IDatabase? GetDatabase()
        {
            try
            {
                var conn = Connection;
                if (conn != null && conn.IsConnected)
                {
                    return conn.GetDatabase();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Redis] Failed to get database: {ex.Message}");
            }
            return null;
        }

        public ISubscriber? GetSubscriber()
        {
            try
            {
                var conn = Connection;
                if (conn != null && conn.IsConnected)
                {
                    return conn.GetSubscriber();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Redis] Failed to get subscriber: {ex.Message}");
            }
            return null;
        }

        public bool IsConnected
        {
            get
            {
                try
                {
                    return Connection?.IsConnected ?? false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public void Dispose()
        {
            if (_lazyConnection.IsValueCreated && _lazyConnection.Value != null)
            {
                try
                {
                    _lazyConnection.Value.Close();
                    _lazyConnection.Value.Dispose();
                }
                catch
                {
                    // ignored
                }
            }
        }
    }
}
