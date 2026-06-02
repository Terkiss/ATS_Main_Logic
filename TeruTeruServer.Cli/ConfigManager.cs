using System;
using System.IO;
using TeruTeruServer.Runtime;
using TeruTeruServer.SDK.Util;

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
                    TeruTeruLogger.LogWarning($"[Config] {filePath} not found. Using default settings.");
                    return new ServerConnectConfigParameter();
                }
            }

            var config = new ServerConnectConfigParameter();
            try
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#') || line.StartsWith("//"))
                        continue;

                    var parts = line.Split('=', 2);
                    if (parts.Length != 2) continue;

                    var key = parts[0].Trim().ToLower();
                    var value = parts[1].Trim();

                    switch (key)
                    {
                        case "port" when int.TryParse(value, out var port):
                            config.SetPort(port);
                            break;
                        case "max_connection" when int.TryParse(value, out var maxConn):
                            config.SetMaxConnection(maxConn);
                            break;
                        case "isudp" when bool.TryParse(value, out var isUdp):
                            config.SetUdp(isUdp);
                            break;
                        case "istcp" when bool.TryParse(value, out var isTcp):
                            config.SetTcp(isTcp);
                            break;
                        case "sendmassagesize" when int.TryParse(value, out var sendSize):
                            config.SendBufferSize = sendSize;
                            break;
                        case "receivemassagesize" when int.TryParse(value, out var recvSize):
                            config.ReceiveBufferSize = recvSize;
                            break;
                        case "guid":
                            config.Guid = value;
                            break;
                        default:
                            TeruTeruLogger.LogWarning($"[Config] Unknown or invalid key/value pair: {line}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                TeruTeruLogger.LogError($"[Config] Error parsing config file: {ex.Message}");
            }

            return config;
        }
    }
}
