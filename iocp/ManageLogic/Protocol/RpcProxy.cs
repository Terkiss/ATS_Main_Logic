using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeruTeruServer.ManageLogic.Util;

namespace TeruTeruServer.ManageLogic.Protocol
{
    public class RpcProxy
    {
        private MainServer _mainServer;

        public RpcProxy()
        {
            _mainServer = ServerMemory.MainServer;
        }

        public void RequestObjectDetect(SendImageData sendImageData)
        {
            if (sendImageData.ImgSize == 0)
            { 
                TeruTeruLogger.LogInfo("이미지 크기가 0입니다.");
                return;
            }

            var hostID = sendImageData.HostID;
            var gameID = Encoding.UTF8.GetString(sendImageData.UserID).TrimEnd('\0');
            TeruTeruLogger.LogInfo($"RequestObjectDetect: HostID = {hostID}, GameID = {gameID}");

            var clientSessionList = ServerMemory.GetClientSessions();

            List<ClientSession> detectClients = new List<ClientSession>();
            foreach (var item in clientSessionList)
            {
                if (item.Role.Equals("Detector"))
                { 
                    detectClients.Add(item);
                }
            }

            if (detectClients.Count == 0)
            {
                TeruTeruLogger.LogInfo("탐지(Detector) 클라이언트가 없습니다.");
                return;
            }

            Random random = new Random();
            int randomIndex = random.Next(0, detectClients.Count);
            var selectedClient = detectClients[randomIndex];
            TeruTeruLogger.LogInfo($"선택된 클라이언트: {selectedClient.HostID}");

            var sendByte = MarshalUtil.Serialize<SendImageData>(sendImageData);

            var requestByte = new byte[sendByte.Length + 2];
            requestByte[0] = (byte)SendType.Direct;
            requestByte[1] = (byte)MethodsSelector.RequestObjectDetect;

            Array.Copy(sendByte, 0, requestByte, 2, sendByte.Length);

            _mainServer.SendData(selectedClient.HostID, requestByte);
        }
    }
}
