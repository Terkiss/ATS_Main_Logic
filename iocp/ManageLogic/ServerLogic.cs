using TeruTeruServer.ManageLogic.Protocol;
using TeruTeruServer.ManageLogic.Util;
using Org.BouncyCastle.Bcpg.OpenPgp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TeruTeruServer.ManageLogic
{
    public class ServerLogic
    {
        private MainServer mainServer;
        // 서버로직 생성자 작성
        public ServerLogic(MainServer mainServer)
        {
            this.mainServer = mainServer;
        }

        /// <summary>
        /// 직접 프로토콜을 처리하여 요청을 추출하고, 클라이언트에게 응답을 전송합니다.
        /// </summary>
        /// <param name="buffer">처리할 수신 데이터가 담긴 바이트 배열입니다.</param>
        /// <param name="socket">데이터를 전송한 클라이언트와 연결된 소켓입니다.</param>
        public void ProcessDirectProtocol(byte[] buffer, Socket socket)
        {
            var receivDataCount = buffer.Length;

            if (receivDataCount > 0)
            {
                RpcStub stub = new RpcStub();
                byte[] responseBytes = stub.HandleRequest(socket, buffer);
                if (responseBytes != null)
                {
                    mainServer.SendData(socket, responseBytes);
                }
            }
        }
        public void ProcessJsonProtocol(string json, ProtocolSelect protocolSelect, Socket socket)
        {
            switch (protocolSelect)
            {
                case ProtocolSelect.ConnectProtocol:

                    // json을 ConnectProtocol로 변환
                    ConnectProtocol connectProtocol = JsonSerializer.Deserialize<ConnectProtocol>(json);

                    this.ConProtocol(socket, connectProtocol);
                    break;
                case ProtocolSelect.LoginProtocol:
                    break;
            }
        }


        #region Connection Protocol

        private void ConProtocol(Socket socket, ConnectProtocol protocol)
        {
            if (mainServer.players.ContainsValue(socket))
            {
                Console.WriteLine("이미 등록된 소켓입니다.");
            }
            else
            {
                string gameId = Utility.GenerateUniqueId();
                Console.WriteLine($"Received connection request: {protocol.Guid}");
                Console.WriteLine($"Game ID: {gameId}");
                Console.WriteLine($"UUID Print : {protocol.Guid}");
                var hostID = ServerMemory.GetHostID;

                bool guidCheck = (mainServer.GUID.Equals(protocol.Guid)) ? true : false;

                // GUID 체크 출력
                Console.WriteLine($"GUID Check : {guidCheck}");
                // hostid 출력
                Console.WriteLine($"Host ID : {hostID}");

                if (guidCheck)
                {
                    Console.WriteLine("체크 성공 등록 절차");

                    mainServer.players.Add(hostID, socket);

                    ClientSession clientSession = new ClientSession(hostID, socket, gameId);

                    clientSession.HostID = hostID;
                    clientSession.GameID = gameId;

                    byte sendType = (byte)SendType.Json;
                    byte protocolType = (byte)ProtocolSelect.ConnectProtocol;


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

        #endregion
    }
}
