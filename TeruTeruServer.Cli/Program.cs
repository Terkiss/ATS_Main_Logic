using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Util;
using TeruTeruServer.Runtime.DB;
using TeruTeruServer.Runtime;
using TeruTeruServer.Runtime.Rpc;
using TeruTeruServer.Runtime.GameEngine;

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
                clusterRegistry.RegisterNode(new TeruTeruServer.SDK.Clustering.ClusterNodeInfo
                {
                    NodeId = config.Guid ?? Guid.NewGuid().ToString("N"),
                    Address = "localhost",
                    Port = config.Port,
                    Status = "Active",
                    LastHeartbeat = DateTime.UtcNow
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

                gameLoop.Start();
            }
        }

        private static void ConfigureServices(IServiceCollection services, ServerConnectConfigParameter config)
        {
            services.AddSingleton<ISessionStore, TeruTeruServer.SDK.Clustering.InMemorySessionStore>();
            services.AddSingleton<ISessionManager, SessionManager>();
            services.AddSingleton<IEventBus, TeruTeruServer.SDK.Clustering.LocalEventBus>();
            services.AddSingleton<IClusterRegistry, TeruTeruServer.SDK.Clustering.LocalClusterRegistry>();

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
                return new MainServer(config, logic, session, store); // RoutingMiddleware에서 주입받지 않으므로 생성자 단순화 가능
            });

            // Game Engine Services
            services.AddSingleton<IGameLoop>(sp => new GameLoop(tickRate: 20));
            services.AddSingleton<IRoomBroadcaster, RoomBroadcaster>();
            services.AddSingleton<IAoIFilter>(sp => new SpatialGrid(cellSize: 50f));
            services.AddSingleton<IZoneManager, ZoneManager>();
            services.AddSingleton<ZoneFactory>();
        }
    }
}
