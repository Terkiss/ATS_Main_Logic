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

            // check config
            if (config == null)
            {
                Console.WriteLine("config is null");
                return;
            }
            else
            {
                Console.WriteLine("config is not null");

                config.IP = "Server Environment";
                Console.Write(config);

            }


            MainServer mainServer = new MainServer(config);

            mainServer.StartServer();

        }
    }
}
