using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Util;
using TeruTeruServer.Runtime.DB;
using TeruTeruServer.Runtime;
using TeruTeruServer.Runtime.Rpc;

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

                Console.WriteLine("=== TeruTeruServer AI Engine Runtime Started ===");

                var mainServer = serviceProvider.GetRequiredService<MainServer>();
                mainServer.StartServer();
            }
        }

        private static void ConfigureServices(IServiceCollection services, ServerConnectConfigParameter config)
        {
            services.AddSingleton<ISessionManager, SessionManager>();

            // DB 서비스 등록
            string dbUri = "Server=localhost;Port=3306;Database=unity3d;Uid=root;Pwd=password";
            services.AddSingleton<IDatabaseService, DatabaseConnector.DatabaseHelper>(sp =>
                new DatabaseConnector.DatabaseHelper(dbUri));

            // [Plugin Architecture] 로직 프록시를 ILogicService로 등록
            var logicProxy = new LogicProxy();
            services.AddSingleton<LogicProxy>(logicProxy);
            services.AddSingleton<ILogicService>(sp => sp.GetRequiredService<LogicProxy>());

            // Protocol Router 등록 (기존 RpcStub을 대체)
            services.AddSingleton<IProtocolRouter, ProtocolRouter>();

            // 메인 서버를 IMessageSender로 등록하여 순환 참조 해결
            services.AddSingleton<IMessageSender>(sp => sp.GetRequiredService<MainServer>());

            services.AddSingleton<MainServer>(sp =>
            {
                var logic = sp.GetRequiredService<ILogicService>();
                var session = sp.GetRequiredService<ISessionManager>();
                return new MainServer(config, logic, session); // RoutingMiddleware에서 주입받지 않으므로 생성자 단순화 가능
            });
        }
    }
}
