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
        private int _port;
        private int _maxConnection;
        private bool _isUdp;
        private bool _isTcp;

        private int _sendBufferSize;
        private int _receiveBufferSize;

        private string _guid;
        public ConfigManager()
        {
            LoadConfig();
        }

        // 설정 파일(config.txt)을 불러오며, 없으면 사용자에게 입력을 받습니다.
        private void LoadConfig()
        {
            string configFilePath = "config.txt";

            if (File.Exists(configFilePath))
            {
                LoadFromFile(configFilePath);
            }
            else
            {
                PromptConfigFromUser();
                SaveConfig();  // 저장 위치 동일
            }
        }

        private void LoadFromFile(string path)
        {
            foreach (string line in File.ReadAllLines(path))
            {
                string[] parts = line.Split('=');
                if (parts.Length != 2) continue;

                string key = parts[0].Trim();
                string value = parts[1].Trim();

                switch (key)
                {
                    case "port": _port = int.Parse(value); break;
                    case "max_connection": _maxConnection = int.Parse(value); break;
                    case "isUdp": _isUdp = bool.Parse(value); break;
                    case "isTcp": _isTcp = bool.Parse(value); break;
                    case "SendMassageSize": _sendBufferSize = int.Parse(value); break;
                    case "ReceiveMassageSize": _receiveBufferSize = int.Parse(value); break;
                    case "Guid": _guid = value; break;
                }
            }
        }

        private void PromptConfigFromUser()
        {
            Console.WriteLine("config.txt 파일이 존재하지 않습니다. 기본 설정값으로 실행합니다.");
            Console.WriteLine("설정값을 입력하세요.");

            _port = PromptInt("port");
            _maxConnection = PromptInt("max connection");

            Console.WriteLine("isUdp, isTcp는 1이면 true, 0이면 false");
            _isUdp = PromptBool("isUdp");
            _isTcp = PromptBool("isTcp");

            if (_isUdp && _isTcp)
            {
                Console.WriteLine("isUdp, isTcp 둘다 true일 수 없습니다. TCP만 true로 설정합니다.");
                _isUdp = false;
            }

            _sendBufferSize = PromptInt("SendMassageSize(Default 4096)", 4096);
            _receiveBufferSize = PromptInt("ReceiveMassageSize(Default 4096)", 4096);

            _guid = MSGuidGenerator().ToString();

            Console.WriteLine("입력 완료");
        }

        private static int PromptInt(string label, int defaultValue = -1)
        {
            Console.Write($"{label} (기본값 {defaultValue}) : ");
            string input = Console.ReadLine();
            return int.TryParse(input, out int result) ? result : defaultValue;
        }

        private static bool PromptBool(string label)
        {
            Console.Write($"{label} (1/0) : ");
            string input = Console.ReadLine();
            return input == "1";
        }




        private void SaveConfig()
        {
            string configFilePath = "config.txt";

            using (StreamWriter writer = new StreamWriter(configFilePath))
            {
                writer.WriteLine($"port={_port}");
                writer.WriteLine($"max_connection={_maxConnection}");
                writer.WriteLine($"isUdp={_isUdp}");
                writer.WriteLine($"isTcp={_isTcp}");
                writer.WriteLine($"SendMassageSize={_sendBufferSize}");
                writer.WriteLine($"ReceiveMassageSize={_receiveBufferSize}");
                writer.WriteLine($"Guid={_guid}");
            }
        }
        public ServerConnectConfigParameter GetServerConfig()
        {
            var serverConfig = new ServerConnectConfigParameter();
           
            serverConfig.SetPort(_port);
            serverConfig.SetMaxConnection(_maxConnection);
            serverConfig.SetUdp(_isUdp);
            serverConfig.SetTcp(_isTcp);
            serverConfig.SendBufferSize = _sendBufferSize;
            serverConfig.ReceiveBufferSize = _receiveBufferSize;
            serverConfig.Guid = _guid;
            return serverConfig;
        }

        static Guid MSGuidGenerator()
        {
            Guid guid = System.Guid.NewGuid();
            Console.WriteLine(guid);
            return guid;
        }
    }
}
