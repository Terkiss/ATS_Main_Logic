using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeruTeruServer
{
    internal class ConfigManager
    {
        private int port;
        private int maxConnection;
        private bool isUdp;
        private bool isTcp;

        private int sendBufferSize;
        private int receiveBufferSize;

        private string Guid;
        public ConfigManager()
        {
            LoadConfig();
        }

        private void LoadConfig()
        {
            string configFilePath = "config.txt";

            if (File.Exists(configFilePath))
            {
                // 파일이 존재할 경우 설정값 읽어오기
                string[] lines = File.ReadAllLines(configFilePath);

                foreach (string line in lines)
                {
                    string[] parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();

                        switch (key)
                        {
                            case "port":
                                port = int.Parse(value);
                                break;
                            case "max_connection":
                                maxConnection = int.Parse(value);
                                break;
                            case "isUdp":
                                isUdp = bool.Parse(value);
                                break;
                            case "isTcp":
                                isTcp = bool.Parse(value);
                                break;
                            case "SendMassageSize":
                                sendBufferSize = int.Parse(value);
                                break;
                            case "ReceiveMassageSize":
                                receiveBufferSize = int.Parse(value);
                                break;
                            case "Guid":
                                Guid = value;
                                break;
                        }
                    }
                }
            }
            else
            {
                // 파일이 존재하지 않을 경우 기본 설정값 지정
                Console.WriteLine("config.txt 파일이 존재하지 않습니다. 기본 설정값으로 실행합니다.");
                Console.WriteLine("설정값을 입력하세요.");
   
                Console.Write("port :");
                string portString = Console.ReadLine();
                Console.Write("max connection :");
                string maxConnectionString = Console.ReadLine();

                // 1이면 true 0이면 false
                Console.WriteLine("isUdp, isTcp는 1이면 true 0이면 false");
                Console.Write("isUdp :");
                string isUdpString = Console.ReadLine();
                Console.Write("isTcp :");
                string isTcpString = Console.ReadLine();
                Console.Write("SendMassageSize(Default 4096) :");
                string sendMassageSizeString = Console.ReadLine();
                Console.Write("ReceiveMassageSize(Default 4096) :");
                string receiveMassageSizeString = Console.ReadLine();

                sendBufferSize = int.Parse(sendMassageSizeString);
                receiveBufferSize = int.Parse(receiveMassageSizeString);
         
                
                Console.Write("입력 완료");
                
                Guid = MSGuidGenerator().ToString();

                port = int.Parse(portString);
                maxConnection = int.Parse( maxConnectionString);
                isUdp = (int.Parse(isUdpString) == 1) ? true:false ;
                isTcp = (int.Parse(isTcpString) == 1) ? true : false;
            
                if(isUdp == true && isTcp == true)
                {
                    Console.WriteLine("isUdp, isTcp 둘다 true일 수 없습니다.");
                    Console.WriteLine("Default인 Tcp 로 설정 합니다");
                    isUdp = false;
                }
                SaveConfig();
            }
        }

        private void SaveConfig()
        {
            string configFilePath = "config.txt";

            using (StreamWriter writer = new StreamWriter(configFilePath))
            {
                writer.WriteLine($"port={port}");
                writer.WriteLine($"max_connection={maxConnection}");
                writer.WriteLine($"isUdp={isUdp}");
                writer.WriteLine($"isTcp={isTcp}");
                writer.WriteLine($"SendMassageSize={sendBufferSize}");
                writer.WriteLine($"ReceiveMassageSize={receiveBufferSize}");
                writer.WriteLine($"Guid={Guid}");
            }
        }
        public ServerConnectConfigParameter GetServerConfig()
        {
            var serVerConfig =  new ServerConnectConfigParameter();
           
            serVerConfig.SetPort(port);
            serVerConfig.SetMaxConnection(maxConnection);
            serVerConfig.SetUdp(isUdp);
            serVerConfig.SetTcp(isTcp);
            serVerConfig.SendBufferSize = sendBufferSize;
            serVerConfig.ReceiveBufferSize = receiveBufferSize;
            serVerConfig.Guid = Guid;
            return serVerConfig;
        }

        static Guid MSGuidGenerator()
        {
            Guid guid = System.Guid.NewGuid();
            Console.WriteLine(guid);
            return guid;
        }
    }
}
