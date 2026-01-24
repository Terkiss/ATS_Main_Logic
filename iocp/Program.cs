using TeruTeruServer;
using TeruTeruServer.ManageLogic;
using TeruTeruServer.ManageLogic.Protocol;
using TeruTeruServer.ManageLogic.Util;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace TeruTeruServer
{
    class Program
    {
        static void Main(string[] args)
        {
            

            ConfigManager configManager = new ConfigManager();


            var config = configManager.GetServerConfig();

            // 설정 확인
            if (config == null)
            {
                Console.WriteLine("설정(config)을 불러오지 못했습니다.");
                return;
            }
            else
            {
                Console.WriteLine("설정 로드 성공.");

                config.IP = "Server Environment";
                Console.Write(config);

            }


            MainServer mainServer = new MainServer(config);

            mainServer.StartServer();

        }
    }
}
