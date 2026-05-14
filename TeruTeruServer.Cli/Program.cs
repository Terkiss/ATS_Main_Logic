using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Util;
using TeruTeruServer.Runtime.DB;
using TeruTeruServer.Runtime;
using TeruTeruServer.Runtime.Rpc;
using TeruTeruServer.Runtime.GameEngine;
using TeruTeruServer.Runtime.Clustering;

namespace TeruTeruServer.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                string dumpDir = "Logs";
                if (!Directory.Exists(dumpDir)) Directory.CreateDirectory(dumpDir);
                
                string dumpFile = Path.Combine(dumpDir, $"crashdump_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                Exception ex = (Exception)e.ExceptionObject;
                File.WriteAllText(dumpFile, "Crash Dump\n=================\n" + ex.ToString());
                TeruTeruLogger.LogError($"Fatal Exception: {ex.Message}. Dump saved to {dumpFile}");
            };

            var config = ConfigManager.LoadConfig("config.txt");
            if (config != null)
            {
                var services = new ServiceCollection();
                ConfigureServices(services, config);

                var serviceProvider = services.BuildServiceProvider();

                // 플러그인 매니저 시작
                var proxy = serviceProvider.GetRequiredService<LogicProxy>();
                var pluginManager = new PluginManager("plugins", proxy, serviceProvider);
                pluginManager.StartMonitoring();

                // 클러스터 노드 자기 자신 등록
                var clusterRegistry = serviceProvider.GetRequiredService<IClusterRegistry>();
                string nodeId = string.IsNullOrEmpty(config.NodeId) ? (config.Guid ?? Guid.NewGuid().ToString("N")) : config.NodeId;
                clusterRegistry.RegisterNode(new TeruTeruServer.SDK.Clustering.ClusterNodeInfo
                {
                    NodeId = nodeId,
                    Address = "localhost",
                    Port = config.Port,
                    Status = "Active",
                    LastHeartbeat = DateTime.UtcNow,
                    CurrentConnections = 0,
                    ActiveZoneCount = 0,
                    ActiveSessionCount = 0,
                    CpuUsagePercent = 0
                });

                Console.WriteLine("=== TeruTeruServer AI Engine Runtime Started ===");

                var mainServer = serviceProvider.GetRequiredService<MainServer>();
                mainServer.StartServer();

                // 틱 루프 시작
                var gameLoop = serviceProvider.GetRequiredService<IGameLoop>();
                var zoneFactory = serviceProvider.GetRequiredService<ZoneFactory>();
                
                // 30초(600틱)마다 빈 인스턴스 정리
                gameLoop.RegisterTickHandler(tick =>
                {
                    if (tick % 600 == 0)
                    {
                        zoneFactory.CleanupEmptyInstances();
                    }
                });

                // [Milestone 11] MatchQueue 틱 핸들러 등록 (매 20 Tick = 1초마다)
                var matchQueue = serviceProvider.GetRequiredService<MatchQueue>();
                gameLoop.RegisterTickHandler(tick => { if (tick % 20 == 0) matchQueue.TryMatch(); });

                // [Milestone 12] Clustering 틱 핸들러 등록
                var healthMonitor = serviceProvider.GetRequiredService<NodeHealthMonitor>();
                var autoScaleMonitor = serviceProvider.GetRequiredService<AutoScaleMonitor>();
                var sessionManager = serviceProvider.GetRequiredService<ISessionManager>();
                var gameSessionManager = serviceProvider.GetRequiredService<IGameSessionManager>();

                gameLoop.RegisterTickHandler(tick =>
                {
                    if (tick % 200 == 0) // 10초마다
                    {
                        // 메트릭 갱신
                        ServerMetrics.UpdateCcu(sessionManager.Players.Count);
                        ServerMetrics.UpdateSessionCount(gameSessionManager.GetActiveSessions().Count);
                        ServerMetrics.UpdateTps();

                        // 하트비트 갱신
                        clusterRegistry.UpdateHeartbeat(nodeId);
                        
                        // 헬스 체크
                        healthMonitor.CheckHealth();
                    }

                    if (tick % 1200 == 0) // 60초마다
                    {
                        // 오토 스케일링 판단
                        autoScaleMonitor.CheckAndNotify();
                    }
                });

                gameLoop.Start();
            }
        }

        private static void ConfigureServices(IServiceCollection services, ServerConnectConfigParameter config)
        {
            if (config.ClusterMode == "Redis")
            {
                services.AddSingleton<ISessionStore>(sp => new RedisSessionStore(config.RedisConnectionString));
                services.AddSingleton<IClusterRegistry>(sp => new RedisClusterRegistry(config.RedisConnectionString));
                services.AddSingleton<IEventBus>(sp => new RedisEventBus(config.RedisConnectionString));
            }
            else
            {
                services.AddSingleton<ISessionStore, TeruTeruServer.SDK.Clustering.InMemorySessionStore>();
                services.AddSingleton<IEventBus, TeruTeruServer.SDK.Clustering.LocalEventBus>();
                services.AddSingleton<IClusterRegistry, TeruTeruServer.SDK.Clustering.LocalClusterRegistry>();
            }
            
            services.AddSingleton<ISessionManager, SessionManager>();

            // DB 서비스 등록
            string dbUri = "Server=localhost;Port=3306;Database=unity3d;Uid=root;Pwd=password";
            services.AddSingleton<IDatabaseService, DatabaseConnector.DatabaseHelper>(sp =>
                new DatabaseConnector.DatabaseHelper(dbUri));

            // [Plugin Architecture] 로직 프록시를 ILogicService로 등록
            var logicProxy = new LogicProxy();
            services.AddSingleton<LogicProxy>(logicProxy);
            services.AddSingleton<ILogicService>(sp => 
            {
                var sender = sp.GetRequiredService<IMessageSender>();
                var db = sp.GetRequiredService<IDatabaseService>();
                var session = sp.GetRequiredService<ISessionManager>();
                var router = sp.GetRequiredService<IProtocolRouter>();
                var bus = sp.GetRequiredService<IEventBus>();
                return new TeruTeruServer.Logic.Default.LogicPlugin(sender, db, session, router, bus);
            });

            // Protocol Router 등록 (기존 RpcStub을 대체)
            services.AddSingleton<IProtocolRouter, ProtocolRouter>();

            // 메인 서버를 IMessageSender로 등록하여 순환 참조 해결
            services.AddSingleton<IMessageSender>(sp => sp.GetRequiredService<MainServer>());

            services.AddSingleton<MainServer>(sp =>
            {
                var logic = sp.GetRequiredService<ILogicService>();
                var session = sp.GetRequiredService<ISessionManager>();
                var store = sp.GetRequiredService<ISessionStore>();
                var securityLogger = sp.GetRequiredService<ISecurityEventLogger>();
                var sanctionManager = sp.GetRequiredService<SanctionManager>();
                return new MainServer(config, logic, session, store, securityLogger, sanctionManager);
            });

            // Game Engine Services
            services.AddSingleton<IGameLoop>(sp => new GameLoop(tickRate: 20));
            services.AddSingleton<IRoomBroadcaster, RoomBroadcaster>();
            services.AddSingleton<IGameSessionManager, GameSessionManager>(); // [M11]
            services.AddSingleton<MatchQueue>(); // [M11]
            services.AddSingleton<IAoIFilter>(sp => new SpatialGrid(cellSize: 50f));
            services.AddSingleton<IZoneManager, ZoneManager>();
            services.AddSingleton<ZoneFactory>();

            services.AddSingleton<ISecurityEventLogger, SecurityEventLogger>();
            services.AddSingleton<SanctionManager>();
            services.AddSingleton<InputFrequencyValidator>();
            services.AddSingleton<ServerAuthorityValidator>();

            // [Milestone 12] Clustering Services
            services.AddSingleton<ClusterRouter>();
            services.AddSingleton<NodeHealthMonitor>();
            services.AddSingleton<RollingUpdateCoordinator>();
            services.AddSingleton<AutoScaleMonitor>();
            services.AddSingleton<ClusterDashboard>();
        }
    }
}
