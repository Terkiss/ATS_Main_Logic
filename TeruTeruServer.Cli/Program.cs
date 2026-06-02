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
        private const int TickRate = 20; // 1초당 20틱
        private const int EmptyInstanceCleanupIntervalTicks = 600; // 30초 (600틱)
        private const int MatchTryIntervalTicks = 20; // 1초 (20틱)
        private const int MetricsAndHeartbeatIntervalTicks = 200; // 10초 (200틱)
        private const int AutoScaleIntervalTicks = 1200; // 60초 (1200틱)

        static void Main(string[] args)
        {
            RegisterUnhandledExceptionHandler();

            var config = ConfigManager.LoadConfig("config.txt");
            if (config != null)
            {
                var services = new ServiceCollection();
                ConfigureServices(services, config);

                var serviceProvider = services.BuildServiceProvider();

                // 플러그인 매니저 및 기본 로직 초기화
                InitializePlugins(serviceProvider);

                // 클러스터 노드 등록
                string nodeId = string.IsNullOrEmpty(config.NodeId) ? (config.Guid ?? Guid.NewGuid().ToString("N")) : config.NodeId;
                RegisterClusterNode(serviceProvider, config, nodeId);

                Console.WriteLine("=== TeruTeruServer AI Engine Runtime Started ===");

                // 메인 서버 시작
                var mainServer = serviceProvider.GetRequiredService<MainServer>();
                mainServer.StartServer();

                // 게임 루프 초기화 및 시작
                var gameLoop = serviceProvider.GetRequiredService<IGameLoop>();
                RegisterTickHandlers(serviceProvider, gameLoop, nodeId);
                
                gameLoop.Start();
            }
        }

        private static void RegisterUnhandledExceptionHandler()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                const string dumpDir = "Logs";
                if (!Directory.Exists(dumpDir)) Directory.CreateDirectory(dumpDir);
                
                string dumpFile = Path.Combine(dumpDir, $"crashdump_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                Exception ex = (Exception)e.ExceptionObject;
                File.WriteAllText(dumpFile, "Crash Dump\n=================\n" + ex.ToString());
                TeruTeruLogger.LogError($"Fatal Exception: {ex.Message}. Dump saved to {dumpFile}");
            };
        }

        private static void InitializePlugins(IServiceProvider serviceProvider)
        {
            var proxy = serviceProvider.GetRequiredService<LogicProxy>();
            
            // 기본 로직 플러그인을 프록시에 주입하여 핫릴로드 전에도 기본 로직이 작동하도록 함
            var defaultLogic = serviceProvider.GetRequiredService<TeruTeruServer.Logic.Default.LogicPlugin>();
            proxy.UpdateLogic(defaultLogic);

            var pluginManager = new PluginManager("plugins", proxy, serviceProvider);
            pluginManager.StartMonitoring();
        }

        private static void RegisterClusterNode(IServiceProvider serviceProvider, ServerConnectConfigParameter config, string nodeId)
        {
            var clusterRegistry = serviceProvider.GetRequiredService<IClusterRegistry>();
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
        }

        private static void RegisterTickHandlers(IServiceProvider serviceProvider, IGameLoop gameLoop, string nodeId)
        {
            var zoneFactory = serviceProvider.GetRequiredService<ZoneFactory>();
            var matchQueue = serviceProvider.GetRequiredService<MatchQueue>();
            var healthMonitor = serviceProvider.GetRequiredService<NodeHealthMonitor>();
            var autoScaleMonitor = serviceProvider.GetRequiredService<AutoScaleMonitor>();
            var sessionManager = serviceProvider.GetRequiredService<ISessionManager>();
            var gameSessionManager = serviceProvider.GetRequiredService<IGameSessionManager>();
            var clusterRegistry = serviceProvider.GetRequiredService<IClusterRegistry>();

            // 30초마다 빈 인스턴스 정리
            gameLoop.RegisterTickHandler(tick =>
            {
                if (tick % EmptyInstanceCleanupIntervalTicks == 0)
                {
                    zoneFactory.CleanupEmptyInstances();
                }
            });

            // 1초마다 MatchQueue 틱 핸들러 실행
            gameLoop.RegisterTickHandler(tick =>
            {
                if (tick % MatchTryIntervalTicks == 0)
                {
                    matchQueue.TryMatch();
                }
            });

            // 10초마다 메트릭 갱신, 하트비트 갱신, 헬스 체크 실행 및 60초마다 오토 스케일링 판단
            gameLoop.RegisterTickHandler(tick =>
            {
                if (tick % MetricsAndHeartbeatIntervalTicks == 0)
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

                if (tick % AutoScaleIntervalTicks == 0)
                {
                    // 오토 스케일링 판단
                    autoScaleMonitor.CheckAndNotify();
                }
            });
        }

        private static void ConfigureServices(IServiceCollection services, ServerConnectConfigParameter config)
        {
            ConfigureClustering(services, config);
            ConfigureDatabase(services);
            ConfigureLogic(services);
            ConfigureGameEngine(services);
            ConfigureSecurity(services, config);
        }

        private static void ConfigureClustering(IServiceCollection services, ServerConnectConfigParameter config)
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

            // [Milestone 12] Clustering Services
            services.AddSingleton<ClusterRouter>();
            services.AddSingleton<NodeHealthMonitor>();
            services.AddSingleton<RollingUpdateCoordinator>();
            services.AddSingleton<AutoScaleMonitor>();
            services.AddSingleton<ClusterDashboard>();
        }

        private static void ConfigureDatabase(IServiceCollection services)
        {
            // DB 서비스 등록
            string dbUri = "Server=localhost;Port=3306;Database=unity3d;Uid=root;Pwd=password";
            services.AddSingleton<IDatabaseService, DatabaseConnector.DatabaseHelper>(sp =>
                new DatabaseConnector.DatabaseHelper(dbUri));
        }

        private static void ConfigureLogic(IServiceCollection services)
        {
            // [Plugin Architecture] 로직 프록시를 ILogicService로 등록
            var logicProxy = new LogicProxy();
            services.AddSingleton<LogicProxy>(logicProxy);
            services.AddSingleton<ILogicService>(sp => sp.GetRequiredService<LogicProxy>());

            // 기본 로직 플러그인 등록
            services.AddSingleton<TeruTeruServer.Logic.Default.LogicPlugin>(sp => 
            {
                var sender = sp.GetRequiredService<IMessageSender>();
                var db = sp.GetRequiredService<IDatabaseService>();
                var session = sp.GetRequiredService<ISessionManager>();
                var router = sp.GetRequiredService<IProtocolRouter>();
                var bus = sp.GetRequiredService<IEventBus>();
                var zone = sp.GetRequiredService<IZoneManager>();
                return new TeruTeruServer.Logic.Default.LogicPlugin(sender, db, session, router, bus, zone);
            });

            // Protocol Router 등록 (기존 RpcStub을 대체)
            services.AddSingleton<IProtocolRouter, ProtocolRouter>();
        }

        private static void ConfigureGameEngine(IServiceCollection services)
        {
            services.AddSingleton<IGameLoop>(sp => new GameLoop(tickRate: TickRate));
            services.AddSingleton<IRoomBroadcaster, RoomBroadcaster>();
            services.AddSingleton<IGameSessionManager, GameSessionManager>(); // [M11]
            services.AddSingleton<MatchQueue>(); // [M11]
            services.AddSingleton<IAoIFilter>(sp => new SpatialGrid(cellSize: 50f));
            services.AddSingleton<IZoneManager, ZoneManager>();
            services.AddSingleton<ZoneFactory>();
        }

        private static void ConfigureSecurity(IServiceCollection services, ServerConnectConfigParameter config)
        {
            services.AddSingleton<ISecurityEventLogger, SecurityEventLogger>();
            services.AddSingleton<SanctionManager>();
            services.AddSingleton<InputFrequencyValidator>();
            services.AddSingleton<ServerAuthorityValidator>();

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
        }
    }
}
