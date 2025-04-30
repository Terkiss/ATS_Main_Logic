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

        MainServer mainServer;
        public RpcProxy()
        {
            // Constructor logic here

            mainServer = ServerMemory.MainServer;


        }



        public void RequestObjectDetect(SendImageData sendImageData)
        {

            if (sendImageData.imgSize == 0)
            { 
                TeruTeruLogger.LogInfo("Image size is 0");
                return;
            }

            var hostID = sendImageData.hostID;
            var GameID = Encoding.UTF8.GetString(sendImageData.userID).TrimEnd('\0');
            TeruTeruLogger.LogInfo($"RequestObjectDetect: hostID = {hostID}, GameID = {GameID}");

            var clientSessionList = ServerMemory.GetClientSessions();

            List<ClientSession> DetectClient = new List<ClientSession>();
            foreach (var item in clientSessionList)
            {
                if (item.Role.Equals("Detector"))
                { 
                    DetectClient.Add(item);
                }
            }
            // 디텍트 클라이언트 중 랜덤하게 하나를 선택
            Random random = new Random();
            int randomIndex = random.Next(0, DetectClient.Count);
            var selectedClient = DetectClient[randomIndex];
            TeruTeruLogger.LogInfo($"Selected Client: {selectedClient.HostID}");
            // SendImageData sendImageData = new SendImageData();
            

            var sendByte = MarshalUtil.Serialize<SendImageData>(sendImageData);

            var requestByte = new byte[sendByte.Length + 2];
            requestByte[0] = (byte)SendType.Direct;
            requestByte[1] = (byte)MethodsSelector.RequestObjectDetect;

            Array.Copy(sendByte, 0, requestByte, 2, sendByte.Length);

            // Send the data to the selected client
            mainServer.SendData(selectedClient.HostID, requestByte);
     
        }

    }
}
