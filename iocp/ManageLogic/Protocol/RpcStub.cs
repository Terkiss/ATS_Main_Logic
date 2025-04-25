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
       // private static int _nextPlayerIndex = 1; // 서버에서 관리하는 플레이어 인덱스
      //  private static readonly Dictionary<int, PlayerData> _playerData = new Dictionary<int, PlayerData>();

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
                        ProcessConnection(connectionData, socket);
                        break;
                    case MethodsSelector.RequestRegisterRole:
                        // 역할 등록 요청 처리
                        //RegisterRoleData registerRoleData = MarshalUtil.Deserialize<RegisterRoleData>(requestBytes, 1);
                        //result = RegisterRole(registerRoleData);

                        SendData roleResister = MarshalUtil.Deserialize<SendData>(requestBytes, 1);
                        RequestRegisterRole(roleResister, socket);
                        break;
                    case MethodsSelector.SendImage: // 이미지 수신  처리
                        SendImageData sendImageData = MarshalUtil.Deserialize<SendImageData>(requestBytes, 1);
                        Console.WriteLine($"Received image data: {sendImageData.data.Length} bytes");

                        ReceivImage(sendImageData);

                        break;

                    //case MethodsSelector.SendPlayerData: // 플레이어 데이터 처리
                    //    PlayerData playerData = MarshalUtil.Deserialize<PlayerData>(requestBytes, 1);
                    //    ProcessLocationUpdate(playerData);
                    //    break;
                    //case MethodsSelector.GeneratePlayer:
                    //    // 플레이어 생성
                    //    PlayerData player = MarshalUtil.Deserialize<PlayerData>(requestBytes, 1);
                    //    ProcessGenPlayer(player);
                    //    break;
                    //case MethodsSelector.SendChatData: // 채팅 메시지 처리
                    //    ChatData chatData = MarshalUtil.Deserialize<ChatData>(requestBytes, 1);
                    //    Console.WriteLine($"Received chat data: {chatData.message}");

                    //    ProcessChatData(chatData);
                    //    break;
                    case MethodsSelector.NotifyPlayerExit: // 플레이어 퇴장 노티
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
                Console.WriteLine($"요청 처리 중 예외 발생: {e.Message}");
                return null;
            }
    
            byte[] bytes = result != null ? MarshalUtil.Serialize(result) : null;

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
       

        private void ProcessConnection(ConnectionData connectionData, Socket socket)
        {
            // 서버 메모리 클레스로부터 메인서버 를 얻어온다
            MainServer mainServer = ServerMemory.MainServer;

            if (mainServer.players.ContainsValue(socket))
            {
                Console.WriteLine("이미 등록된 소켓입니다.");
            }
            else
            {
                string gameId = Utility.GenerateUniqueId();
                Console.WriteLine($"Received connection request: {connectionData}");
                Console.WriteLine($"Game ID: {gameId}");
                Console.WriteLine($"UUID Print : {connectionData.guid}");
                var hostID = ServerMemory.GetHostID;

                bool guidCheck = (mainServer.GUID.Equals(connectionData.guid)) ? true : false;

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


                    byte sendType = (byte)SendType.Direct;
                    byte protocolType = (byte)MethodsSelector.RequestConnection;


                    ConnectProtocol connectProtocol = new ConnectProtocol
                    {
                        HostId = hostID,
                        Command = 1,
                        Data = gameId,
                        
                    };

                    string json = JsonSerializer.Serialize(connectProtocol);
                    byte[] tempByte = Encoding.UTF8.GetBytes(json);
                    byte[] sendData = new byte[tempByte.Length + 2];
                    sendData[0] = sendType;
                    sendData[1] = protocolType;
                    Array.Copy(tempByte, 0, sendData, 2, tempByte.Length);


          

                    mainServer.SendData(socket, sendData);
                }
                else
                {
                    // 실패 로깅
                    Console.WriteLine("체크 실패");
                }
            }
        }
        // 클라이언트에게 고유한 인덱스 부여




        private void RequestRegisterRole(SendData roleData, Socket socket)
        {
            var stringData = Encoding.UTF8.GetString(roleData.data);
            TeruTeruLogger.LogInfo("수신된 데이터 " + stringData);

            var split_data = stringData.Split("!!!");

            var gameid = split_data[0];
            var role = split_data[1];
            var name = split_data[2];


            var sessionList = ServerMemory.GetClientSessions();


            var clientSession = ServerMemory.FindClientSession(gameid);
            
            clientSession.Role = role;
            clientSession.ClientName = name;
            
        }

        private void ReceivImage(SendImageData sendImageData)
        {
            // Receve folder creadte 
            // 상대경로로 Receve 폴더 생성
            string path = @"Receve";
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            // 이미지 파일 생성

            string fileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_fff")+"image.jpg";

            string filePath = System.IO.Path.Combine(path, fileName);


            byte[] imgByte = new byte[sendImageData.imgSize];

            for (int i = 0; i < sendImageData.imgSize; i++)
            {
                imgByte[i] = sendImageData.data[i];
            }

            System.IO.File.WriteAllBytes(filePath, imgByte);
        }


        private SendData AssignPlayerIndex(SendData sendData)
        {
            // 받은 게임 id로 서버 메모리에서 인덱스를 참조
            // gameid는 byte[] 이므로 string으로 변환
            byte[] gameid_byte = new byte[sendData.data[0]];
            Array.Copy(sendData.data, 1, gameid_byte, 0, sendData.data[0]);
            string gameid = Encoding.UTF8.GetString(gameid_byte);
            var clientSession = ServerMemory.FindClientSession(gameid);

            if (sendData.index == -1)
            {

                // 클라이언트에게 인덱스 부여
                sendData.index = clientSession.HostID;
                Console.WriteLine($"Assigned player index: {sendData.index}");
                return sendData;
            }
            else
            {
                // 클라이언트가 이미 인덱스를 가지고 있는 경우
                Console.WriteLine($"Player already has an index: {sendData.index}");
                return sendData;
            }

    
        }


        //private void ProcessGenPlayer(PlayerData sendData)
        //{
        //    var session =  ServerMemory.FindClientSession(sendData.Index);

        //    session.isGen = true;
        //    session.AnimState = sendData.AnimationState;
        //    session.Xpos = sendData.PositionX;
        //    session.Xpos = sendData.PositionY;
        //    session.Xpos = sendData.PositionZ;
        //    session.Xrot = sendData.RotationX;
        //    session.Yrot = sendData.RotationY;
        //    session.Zrot = sendData.RotationZ;
        //    session.Wrot = sendData.RotationW;

        //    session.SkinData = sendData.SkinData;
        //    session.Gender = sendData.Gender;

        //    // 현재 접속중인 모든 클라이 언트에게 브로드 캐스트
    
        //    var session_List = ServerMemory.GetClientSessions();

        //    var sendBytes = MarshalUtil.Serialize(sendData);

        //    byte sendType = (byte)SendType.Direct;
        //    byte protocolType = (byte)MethodsSelector.GeneratePlayer;

        //    byte[] responsByte = new byte[sendBytes.Length + 2];
        //    responsByte[0] = sendType;
        //    responsByte[1] = protocolType;
        //    Array.Copy(sendBytes, 0, responsByte, 2, sendBytes.Length);


        //    // 다른 사람 전송
        //    foreach (var item in session_List)
        //    {
        //        if (sendData.Index != item.HostID && item.isGen == true)
        //        {
        //            // 다른 클라이언트에게 플레이어 생성 정보 전송
        //            ServerMemory.MainServer.SendData(item.HostID, responsByte);

        //            var playerData = item.GetPlayerData();
        //            var playerData_Byte = MarshalUtil.Serialize(playerData);
        //            var sendByte = new byte[playerData_Byte.Length + 2];
        //            sendByte[0] = sendType;
        //            sendByte[1] = protocolType;
        //            Array.Copy(playerData_Byte, 0, sendByte, 2, playerData_Byte.Length);

        //            // 새로 접속한 클라이언트에게 다른 클라이언트의 생성 정보 전송
        //            ServerMemory.MainServer.SendData(sendData.Index, sendByte);

        //        }
        //    }
        //}

        //// 플레이어 데이터를 처리 (위치, 회전, 애니메이션 상태 업데이트)
        //private void ProcessLocationUpdate(PlayerData data)
        //{

        //    // 플레이어 데이터 업데이트
        //    //Console.WriteLine("position : " + data.PositionX + " " + data.PositionY + " " + data.PositionZ);
        //    //Console.WriteLine("rotation : " + data.RotationX + " " + data.RotationY + " " + data.RotationZ + " " + data.RotationW);

        //    var session = ServerMemory.FindClientSession(data.Index);

        //    session.AnimState = data.AnimationState;
        //    session.Xpos = data.PositionX;
        //    session.Xpos = data.PositionY;
        //    session.Xpos = data.PositionZ;
        //    session.Xrot = data.RotationX;
        //    session.Yrot = data.RotationY;
        //    session.Zrot = data.RotationZ;
        //    session.Wrot = data.RotationW;

        //    session.SkinData = data.SkinData;
        //    session.Gender = data.Gender;

        //    var sendBytes = MarshalUtil.Serialize(data);

        //    byte sendType = (byte)SendType.Direct;
        //    byte protocolType = (byte)MethodsSelector.SendPlayerData;

        //    byte[] responsByte = new byte[sendBytes.Length + 2];
        //    responsByte[0] = sendType;
        //    responsByte[1] = protocolType;
        //    Array.Copy(sendBytes, 0, responsByte, 2, sendBytes.Length);
        //    // 브로드 캐스팅
        //    var session_List = ServerMemory.GetClientSessions();
        //    foreach (var item in session_List)
        //    {
        //        if (data.Index != item.HostID)
        //        {
        //            ServerMemory.MainServer.SendData(item.HostID, responsByte);
        //        }
        //    }
        //}

        //private void ProcessChatData(ChatData chatData)
        //{
        //    Console.WriteLine($"Received chat data: {chatData.message}");
        //    // 채팅 을 [아이디] : 채팅 으로 로깅
        //    Console.WriteLine($"[{chatData.sender}] : {Encoding.UTF8.GetString(Convert.FromBase64String(chatData.message))}");
        //    var sendBytes = MarshalUtil.Serialize(chatData);

        //    byte sendType = (byte)SendType.Direct;
        //    byte protocolType = (byte)MethodsSelector.SendChatData;

        //    byte[] responsByte = new byte[sendBytes.Length + 2];

        //    responsByte[0] = sendType;
        //    responsByte[1] = protocolType;

        //    Array.Copy(sendBytes, 0, responsByte, 2, sendBytes.Length);
        //    var session_List = ServerMemory.GetClientSessions();
        //    foreach (var item in session_List)
        //    {
        //        if (chatData.index != item.HostID)
        //        {
        //            ServerMemory.MainServer.SendData(item.HostID, responsByte);
        //        }
        //    }

        //}


        /// <summary>
        /// 플레이어가 게임에서 퇴장할 때의 처리를 담당합니다.
        /// 퇴장한 플레이어를 제외한 모든 연결된 클라이언트 세션에 
        /// 해당 플레이어가 퇴장했음을 알리는 알림을 전송합니다.
        /// </summary>
        /// <param name="index">퇴장한 플레이어의 인덱스(고유 식별자).</param>
        private void ProcessPlayerExit(int index)
        {
            // 퇴장한 플레이어의 인덱스와 "Exit" 메시지를 포함한 데이터 패킷 생성
            SendData sendData = new SendData
            {
                index = index,
               
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
            sendData.data = bytes;

            // 데이터 패킷을 바이트 배열로 직렬화
            var sendBytes = MarshalUtil.Serialize(sendData);

            // 전송 유형(Direct) 및 프로토콜 유형(NotifyPlayerExit) 정의
            byte sendType = (byte)SendType.Direct;
            byte protocolType = (byte)MethodsSelector.NotifyPlayerExit;

            // 전송 유형, 프로토콜 유형, 직렬화된 데이터를 포함한 응답 바이트 배열 준비
            byte[] responsByte = new byte[sendBytes.Length + 2];
            responsByte[0] = sendType;          // 첫 번째 바이트: 전송 유형
            responsByte[1] = protocolType;      // 두 번째 바이트: 프로토콜 유형
            Array.Copy(sendBytes, 0, responsByte, 2, sendBytes.Length); // 직렬화된 데이터 복사

            // 서버 메모리에서 활성 클라이언트 세션 목록 가져오기
            var session_List = ServerMemory.GetClientSessions();

            // 각 클라이언트 세션 순회
            foreach (var item in session_List)
            {
                // 퇴장한 플레이어의 세션은 제외
                if (index != item.HostID)
                {
                    // 준비된 응답 바이트 배열을 클라이언트에 전송
                    ServerMemory.MainServer.SendData(item.HostID, responsByte);
                }
            }
        }

    }
}
