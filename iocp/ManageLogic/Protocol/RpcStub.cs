using TeruTeruServer.ManageLogic.Util;
using TeruTeruServer.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TeruTeruServer.ManageLogic.Protocol
{
    public class RpcStub
    {
        private readonly IMessageSender _messageSender;
        private readonly ISessionManager _sessionManager;

        public RpcStub(IMessageSender messageSender, ISessionManager sessionManager)
        {
            _messageSender = messageSender;
            _sessionManager = sessionManager;
        }

        // 구 버전 호환성을 위한 기본 생성자 (점진적 교체용)
        public RpcStub() { }

        public byte[] HandleRequest(Socket socket, byte[] requestBytes)
        {
            MethodsSelector methodId =(MethodsSelector) requestBytes[0];
            object result = null;

            try
            {
                switch (methodId)
                {
                    case MethodsSelector.RequestConnection:
                        ConnectionData connectionData = MarshalUtil.Deserialize<ConnectionData>(requestBytes, 1);
                        result = ProcessConnection(connectionData, socket);
                        break;
                    case MethodsSelector.RequestRegisterRole:
                        SendData roleRegisterData = MarshalUtil.Deserialize<SendData>(requestBytes, 1);
                        RequestRegisterRole(roleRegisterData, socket);
                        break;
                    case MethodsSelector.SendImage: 
                        SendImageData sendImageData = MarshalUtil.Deserialize<SendImageData>(requestBytes, 1);
                        ReceiveImage(sendImageData);
                        break;
                    case MethodsSelector.ObjectDetectResult:
                        YoloDetectResult sendImageDataDetect = MarshalUtil.Deserialize<YoloDetectResult>(requestBytes, 1);
                        TeruTeruLogger.LogInfo($"{sendImageDataDetect.UserID}로부터 탐지 결과를 수신했습니다.");
                        break;
                    case MethodsSelector.NotifyPlayerExit: 
                        this.ProcessPlayerExit((int)requestBytes[1]);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown method ID: {methodId}");
                }
            }
            catch (Exception e)
            {
                TeruTeruLogger.LogError($"요청 처리 중 예외 발생: {e.Message}");
                return null;
            }

            byte[] bytes;
            if (methodId == MethodsSelector.RequestConnection)
            {
                bytes = (byte[])result;
            }
            else
            {
                bytes = result != null ? MarshalUtil.Serialize(result) : null;
            }

            if (bytes == null) return null;

            byte[] responseBytes = new byte[bytes.Length + 2];
            responseBytes[0] = (byte)SendType.Direct;
            responseBytes[1] = (byte)methodId;
            Array.Copy(bytes, 0, responseBytes, 2, bytes.Length);
            return responseBytes;
        }
       

        private byte[] ProcessConnection(ConnectionData connectionData, Socket socket)
        {
            if (_sessionManager == null) return null;

            if (_sessionManager.Players.Values.Contains(socket))
            {
                TeruTeruLogger.LogWarning("이미 등록된 소켓입니다.");
                return null;
            }
            
            string gameId = TeruTeruServer.ManageLogic.Util.Utility.GenerateUniqueId();
            var hostID = ServerMemory.GetHostID;

            // GUID 체크 로직 (실제로는 Config나 IMessageSender 등에서 가져와야 함)
            bool guidCheck = true; 

            if (guidCheck)
            {
                TeruTeruLogger.LogInfo("체크 성공 등록 절차");
                if (_sessionManager.TryAddPlayer(hostID, socket))
                {
                    ClientSession clientSession = new ClientSession(hostID, socket, gameId);
                    ConnectProtocol connectProtocol = new ConnectProtocol { HostId = hostID, Command = 1, Data = gameId };
                    string json = JsonSerializer.Serialize(connectProtocol);
                    return Encoding.UTF8.GetBytes(json);
                }
            }
            return null;
        }

        private void RequestRegisterRole(SendData roleData, Socket socket)
        {
            var stringData = Encoding.UTF8.GetString(roleData.Data);
            var split_data = stringData.Split("!!!");
            var gameid = split_data[0];
            var role = split_data[1];
            var name = split_data[2];

            var clientSession = ServerMemory.FindClientSession(gameid);
            clientSession.Role = role;
            clientSession.ClientName = name;

            string success = "Success";
            byte[] data = Encoding.UTF8.GetBytes(success);
            byte[] bytes = new byte[256];
            Array.Copy(data, bytes, data.Length);
            roleData.Data = bytes;
            roleData.Index = clientSession.HostID;

            var sendBytes = MarshalUtil.Serialize(roleData);
            byte[] responseBytes = new byte[sendBytes.Length + 2];
            responseBytes[0] = (byte)SendType.Direct;
            responseBytes[1] = (byte)MethodsSelector.RequestRegisterRole;
            Array.Copy(sendBytes, 0, responseBytes, 2, sendBytes.Length);
            
            _messageSender?.SendData(socket, responseBytes);
        }

        private void ReceiveImage(SendImageData sendImageData)
        {
            ServerMemory.AddImageWork_PreOrder_Queue(sendImageData);
        }

        private void ProcessPlayerExit(int index)
        {
            SendData sendData = new SendData { Index = index };
            var data = Encoding.UTF8.GetBytes("Exit");
            byte[] bytes = new byte[256];
            Array.Copy(data, bytes, data.Length);
            sendData.Data = bytes;

            var sendBytes = MarshalUtil.Serialize(sendData);
            byte[] responseBytes = new byte[sendBytes.Length + 2];
            responseBytes[0] = (byte)SendType.Direct;
            responseBytes[1] = (byte)MethodsSelector.NotifyPlayerExit;
            Array.Copy(sendBytes, 0, responseBytes, 2, sendBytes.Length); 

            var session_List = ServerMemory.GetClientSessions();
            foreach (var item in session_List)
            {
                if (index != item.HostID)
                {
                    _messageSender?.SendData(item.HostID, responseBytes);
                }
            }
        }
    }
}
