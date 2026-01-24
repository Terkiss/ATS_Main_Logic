using TeruTeruServer.ManageLogic.Util;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Tls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TeruTeruServer.ManageLogic.Protocol
{
    public class RpcStub
    {
        public RpcStub()
        {
   
        }


        public byte[] HandleRequest(Socket socket, byte[] requestBytes)
        {
            MethodsSelector methodId =(MethodsSelector) requestBytes[0];  // 첫 번째 바이트로 메서드 ID를 확인
            object result = null;

            try
            {
                switch (methodId)
                {
                    case MethodsSelector.RequestConnection:
                        // 연결 요청 처리
                        ConnectionData connectionData = MarshalUtil.Deserialize<ConnectionData>(requestBytes, 1);
                        result = ProcessConnection(connectionData, socket);
                        break;
                    case MethodsSelector.RequestRegisterRole:
                        // 역할 등록 요청 처리
                        SendData roleRegisterData = MarshalUtil.Deserialize<SendData>(requestBytes, 1);
                        RequestRegisterRole(roleRegisterData, socket);
                        break;
                    case MethodsSelector.SendImage: 
                        // 수신된 이미지 데이터 처리
                        SendImageData sendImageData = MarshalUtil.Deserialize<SendImageData>(requestBytes, 1);
                        ReceiveImage(sendImageData);
                        break;
                    case MethodsSelector.ObjectDetectResult:
                        TeruTeruLogger.LogAttention("ObjectDetectResult 요청 처리 시작");
                        YoloDetectResult sendImageDataDetect = MarshalUtil.Deserialize<YoloDetectResult>(requestBytes, 1);

                        // 로그 출력
                        TeruTeruLogger.LogInfo($"{sendImageDataDetect.UserID}로부터 크기 {sendImageDataDetect.Data.Length}바이트의 객체 탐지 결과를 수신했습니다.");
                        TeruTeruLogger.LogInfo($"탐지 결과: {sendImageDataDetect.DetectionResult}");
                        break;

       


                    case MethodsSelector.NotifyPlayerExit: 
                        // 플레이어 퇴장 알림
                        this.ProcessPlayerExit((int)requestBytes[1]);
                        break;

                    default:
                        Console.WriteLine($"Unknown method ID: {methodId}");
                        throw new InvalidOperationException($"Unknown method ID: {methodId}");
                       
                }
            }
            catch(InvalidOperationException e)
            {
                // 잘못된 명령 요청
                Console.WriteLine($"잘못된 명령 요청: {e.Message}");
                return null;
            }
            catch (Exception e)
            {
                // 예외 발생시 로깅

                // 상세 로깅
                TeruTeruLogger.LogError($"예외 발생: {e.Message}");

                // 모든 변수 출력
                TeruTeruLogger.LogError($"예외 발생: {e.StackTrace}");
                TeruTeruLogger.LogError($"예외 발생: {e.InnerException}");
                TeruTeruLogger.LogError($"예외 발생: {e.TargetSite}");
                TeruTeruLogger.LogError($"예외 발생: {e.Source}");
                TeruTeruLogger.LogError($"예외 발생: {e.Data}");
                TeruTeruLogger.LogError($"예외 발생: {e.GetType()}");
                TeruTeruLogger.LogError($"예외 발생: {e.ToString()}");

                TeruTeruLogger.LogError(((int)methodId).ToString());



                Console.WriteLine($"요청 처리 중 예외 발생: {e.Message}");
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

         

            if (bytes == null)
            {
                return null;
            }
            else
            {

                byte[] responseBytes = new byte[bytes.Length + 2];
                responseBytes[0] = (byte)SendType.Direct;
                responseBytes[1] = (byte)methodId;

                Array.Copy(bytes, 0, responseBytes, 2, bytes.Length);
                return responseBytes;
            }
        }
       

        private byte[] ProcessConnection(ConnectionData connectionData, Socket socket)
        {
            // 서버 메모리 클레스로부터 메인서버 를 얻어온다
            MainServer mainServer = ServerMemory.MainServer;

            if (mainServer.players.ContainsValue(socket))
            {
                Console.WriteLine("이미 등록된 소켓입니다.");
                return null;
            }
            else
            {
                string gameId = Utility.GenerateUniqueId();
                Console.WriteLine($"Received connection request: {connectionData}");
                Console.WriteLine($"Game ID: {gameId}");
                Console.WriteLine($"UUID Print : {connectionData.Guid}");
                var hostID = ServerMemory.GetHostID;

                bool guidCheck = (mainServer.GUID.Equals(connectionData.Guid)) ? true : false;

                // GUID 체크 출력
                Console.WriteLine($"GUID Check : {guidCheck}");
                // hostid 출력
                Console.WriteLine($"Host ID : {hostID}");

                if (guidCheck)
                {
                    Console.WriteLine("체크 성공 등록 절차");
                    mainServer.players.Add(hostID, socket);

                    ClientSession clientSession = new ClientSession(hostID, socket,gameId);

                    clientSession.HostID = hostID;
                    clientSession.GameID = gameId;




                    ConnectProtocol connectProtocol = new ConnectProtocol
                    {
                        HostId = hostID,
                        Command = 1,
                        Data = gameId,
                        
                    };

                    string json = JsonSerializer.Serialize(connectProtocol);
                    byte[] tempByte = Encoding.UTF8.GetBytes(json);
              

              



                    return tempByte;
                   // mainServer.SendData(socket, sendData);
                }
                else
                {
                    // 실패 로깅
                    Console.WriteLine("체크 실패");
                    return null;
                }
            }
        }
        // 클라이언트에게 고유한 인덱스 부여




        private void RequestRegisterRole(SendData roleData, Socket socket)
        {
            var stringData = Encoding.UTF8.GetString(roleData.Data);
            TeruTeruLogger.LogInfo("Received data: " + stringData);

            var split_data = stringData.Split("!!!");

            var gameid = split_data[0];
            var role = split_data[1];
            var name = split_data[2];

            var clientSession = ServerMemory.FindClientSession(gameid);
            
            clientSession.Role = role;
            clientSession.ClientName = name;

            byte sendType = (byte)SendType.Direct;
            byte protocolType = (byte)MethodsSelector.RequestRegisterRole;
            string success = "Success";
            byte[] data = Encoding.UTF8.GetBytes(success);
            byte[] bytes = new byte[256];
            for (int i = 0; i < data.Length; i++)
            {
                bytes[i] = data[i];
            }
            for (int i = data.Length; i < 256; i++)
            {
                bytes[i] = 0;
            }
            roleData.Data = bytes;
            roleData.Index = clientSession.HostID;

            var sendBytes = MarshalUtil.Serialize(roleData);
            byte[] responseBytes = new byte[sendBytes.Length + 2];
            responseBytes[0] = sendType;
            responseBytes[1] = protocolType;
            Array.Copy(sendBytes, 0, responseBytes, 2, sendBytes.Length);
            
            // 클라이언트에 응답 전송
            ServerMemory.MainServer.SendData(socket, responseBytes);
        }

        private void ReceiveImage(SendImageData sendImageData)
        {
            int hostID = sendImageData.HostID;
            string gameID = Encoding.UTF8.GetString(sendImageData.UserID).TrimEnd('\0');

            ServerMemory.AddImageWork_PreOrder_Queue(sendImageData);
        }


        private SendData AssignPlayerIndex(SendData sendData)
        {
            // 받은 게임 ID로 서버 메모리에서 클라이언트 세션 참조
            // gameid는 byte[] 이므로 string으로 변환
            byte[] gameid_byte = new byte[sendData.Data[0]];
            Array.Copy(sendData.Data, 1, gameid_byte, 0, sendData.Data[0]);
            string gameid = Encoding.UTF8.GetString(gameid_byte);
            var clientSession = ServerMemory.FindClientSession(gameid);

            if (sendData.Index == -1)
            {

                // 클라이언트에게 인덱스 부여
                sendData.Index = clientSession.HostID;
                Console.WriteLine($"Assigned player index: {sendData.Index}");
                return sendData;
            }
            else
            {
                // 클라이언트가 이미 인덱스를 가지고 있는 경우
                Console.WriteLine($"Player already has an index: {sendData.Index}");
                return sendData;
            }

    
        }







        /// <summary>
        /// 플레이어가 게임에서 퇴장할 때의 처리를 담당합니다.
        /// 퇴장한 플레이어를 제외한 모든 연결된 클라이언트 세션에 
        /// 해당 플레이어가 퇴장했음을 알리는 알림을 전송합니다.
        /// </summary>
        /// <param name="index">퇴장한 플레이어의 인덱스(고유 식별자).</param>
        private void ProcessPlayerExit(int index)
        {
            SendData sendData = new SendData
            {
                Index = index,
            };
            var data = Encoding.UTF8.GetBytes("Exit");

            byte[] bytes = new byte[256];
            for (int i = 0; i < data.Length; i++)
            {
                bytes[i] = data[i];
            }
            for (int i = data.Length; i < 256; i++)
            {
                bytes[i] = 0;
            }
            sendData.Data = bytes;

            // 데이터 패킷을 바이트 배열로 직렬화
            var sendBytes = MarshalUtil.Serialize(sendData);

            // 전송 유형(Direct) 및 프로토콜 유형(NotifyPlayerExit) 정의
            byte sendType = (byte)SendType.Direct;
            byte protocolType = (byte)MethodsSelector.NotifyPlayerExit;

            // 타입과 프로토콜 인덱스를 포함한 응답 바이트 배열 준비
            byte[] responseBytes = new byte[sendBytes.Length + 2];
            responseBytes[0] = sendType;          
            responseBytes[1] = protocolType;      
            Array.Copy(sendBytes, 0, responseBytes, 2, sendBytes.Length); 

            // 세션 목록을 가져와 다른 플레이어에게 알림
            var session_List = ServerMemory.GetClientSessions();

            foreach (var item in session_List)
            {
                if (index != item.HostID)
                {
                    ServerMemory.MainServer.SendData(item.HostID, responseBytes);
                }
            }
        }

    }
}
