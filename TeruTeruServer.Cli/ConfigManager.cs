using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeruTeruServer.Runtime;

namespace TeruTeruServer.Cli
{
    public static class ConfigManager
    {
        public static ServerConnectConfigParameter LoadConfig(string filePath)
        {
            if (!File.Exists(filePath))
            {
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                string fallbackPath = Path.Combine(exeDir, filePath);

                if (File.Exists(fallbackPath))
                {
                    filePath = fallbackPath;
                }
                else
                {
                    Console.WriteLine($"[Config] {filePath} not found. Using default settings.");
                    return new ServerConnectConfigParameter();
                }
            }

            var config = new ServerConnectConfigParameter();
            try
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var parts = line.Split('=');
                    if (parts.Length != 2) continue;

                    var key = parts[0].Trim().ToLower();
                    var value = parts[1].Trim();

                    switch (key)
                    {
                        case "port": config.SetPort(int.Parse(value)); break;
                        case "max_connection": config.SetMaxConnection(int.Parse(value)); break;
                        case "isudp": config.SetUdp(bool.Parse(value)); break;
                        case "istcp": config.SetTcp(bool.Parse(value)); break;
                        case "sendmassagesize": config.SendBufferSize = int.Parse(value); break;
                        case "receivemassagesize": config.ReceiveBufferSize = int.Parse(value); break;
                        case "guid": config.Guid = value; break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Config] Error parsing config file: {ex.Message}");
            }

            return config;
        }
    }
}
