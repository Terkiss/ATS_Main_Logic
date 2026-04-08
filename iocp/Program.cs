using TeruTeruServer;
using TeruTeruServer.ManageLogic;
using TeruTeruServer.Common.Protocol;
using TeruTeruServer.Common.Enums;
using TeruTeruServer.ManageLogic.Util;
using TeruTeruServer.DB;
using TeruTeruServer.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace TeruTeruServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var services = new ServiceCollection();
            var configManager = new ConfigManager();
            var config = configManager.GetServerConfig();

            if (config == null)
            {
                Console.WriteLine("설정(config)을 불러오지 못했습니다.");
                return;
            }

            ConfigureServices(services, config);

            using (var serviceProvider = services.BuildServiceProvider())
            {
                Console.WriteLine("설정 로드 성공.");
                
                // MainServer를 DI를 통해 가져옴 (순환 참조 없음)
                var mainServer = serviceProvider.GetRequiredService<MainServer>();
                mainServer.StartServer();
            }
        }

        private static void ConfigureServices(IServiceCollection services, ServerConnectConfigParameter config)
        {
            // 1. 기초 세션 관리자 등록
            services.AddSingleton<ISessionManager, SessionManager>();

            // 2. DB 서비스 등록 (Task 2 해결)
            string dbUri = "Server=localhost;Port=3306;Database=unity3d;Uid=root;Pwd=password"; // TODO: config 연동
            services.AddSingleton<IDatabaseService, DatabaseConnector.DatabaseHelper>(sp => 
                new DatabaseConnector.DatabaseHelper(dbUri));

            // 3. 비즈니스 로직 서비스 등록
            services.AddSingleton<ILogicService, ServerLogic>();

            // 4. 메인 서버를 IMessageSender로 등록하여 순환 참조 해결 (Task 3 해결)
            services.AddSingleton<IMessageSender>(sp => sp.GetRequiredService<MainServer>());
            
            services.AddSingleton<MainServer>(sp => 
            {
                var logic = sp.GetRequiredService<ILogicService>();
                var session = sp.GetRequiredService<ISessionManager>();
                return new MainServer(config, logic, session);
            });
        }
    }
}
