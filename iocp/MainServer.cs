using TeruTeruServer.ManageLogic;
using TeruTeruServer.ManageLogic.Protocol;
using TeruTeruServer.ManageLogic.Util;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;

namespace TeruTeruServer
{
    public class MainServer
    {
        // 플레이어 정보를 저장하는 딕셔너리
        public Dictionary<int, Socket> players;

        // 플레이어 정보에 접근하는 데 사용하는 락
        public object playerLock;

        // 최대 연결수
        private int maxConnection;

        // 포트
        private int port;

        // UDP, TCP 여부
        private bool isUdp;
        private bool isTcp;

        // 서버 소켓
        private Socket ServerSocket;

        // 송신 버퍼 크기와 수신 버퍼 크기
        private int sendBufferSize;
        private int receiveBufferSize;

        // 서버 GUID
        public string GUID;

        private ServerLogic serverLogic;


        // 송신 버퍼 크기 속성
        public int SendBufferSize
        {
            get
            {
                return sendBufferSize;
            }
            set
            {
                sendBufferSize = value;
            }
        }

        // 수신 버퍼 크기 속성
        public int ReceiveBufferSize
        {
            get
            {
                return receiveBufferSize;
            }
            set
            {
                receiveBufferSize = value;
            }
        }
        public MainServer(int maxConnection, int port, bool isUdp)
        {
            this.maxConnection = maxConnection;

            players = new Dictionary<int, Socket>();
            playerLock = new object();
            players.Clear();

            GUID = Guid.NewGuid().ToString();
            Console.WriteLine("Server Guid : " + GUID);
            this.port = port;

            serverLogic = new ServerLogic(this);

            ServerMemory.MainServer = this;
        }

        public MainServer(int maxConnection, int port, bool isUdp, bool isTCP)
        {
            this.maxConnection = maxConnection;
            this.port = port;
            this.isUdp = isUdp;
            this.isTcp = isTCP;

            players = new Dictionary<int, Socket>();
            playerLock = new object();
            players.Clear();

            GUID = Guid.NewGuid().ToString();
            Console.WriteLine("Server Guid : " + GUID);
            serverLogic = new ServerLogic(this);

            ServerMemory.MainServer = this;
        }
        public MainServer(ServerConnectConfigParameter config)
        {
            this.maxConnection = config.MaxConnection;
            this.port = config.Port;
            this.isUdp = config.isUdp;
            this.isTcp = config.isTcp;
            this.sendBufferSize = config.SendBufferSize;
            this.receiveBufferSize = config.ReceiveBufferSize;
            this.GUID = config.Guid;

            players = new Dictionary<int, Socket>();
            playerLock = new object();
            players.Clear();

            serverLogic = new ServerLogic(this);

            ServerMemory.MainServer = this;
        }

        /// <summary>
        /// 서버를 시작하는 메서드입니다.
        /// </summary>
        public void StartServer()
        {
            // 콘솔에 현재 서버 타입을 출력합니다.
            Console.Write("Server Type: ");
            StartSocketCheck();
            if (isUdp)
            {
                // UDP 서버를 시작합니다.
                UdpServerStart();
            }
            else if (isTcp)
            {
                if (GUID == null)
                {
                    // TCP 서버를 시작하기 위해 GUID가 필요합니다. GUID가 null인 경우 예외를 던집니다.
                    throw new ArgumentNullException("guid", "GUID cannot be null when starting TCP server.");
                }

                // TCP 서버를 시작합니다.
                TcpServerStart();
            }
            else
            {
                // 유효한 서버 타입이 선택되지 않은 경우 예외를 던집니다.
                throw new InvalidOperationException("Neither UDP nor TCP is selected for server type.");
            }
        }


        /// <summary>
        /// TCP 서버를 시작하는 메서드입니다.
        /// </summary>
        private void TcpServerStart()
        {
            // TCP 소켓을 생성합니다.
            ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // 서버 소켓을 지정된 포트로 바인딩합니다.
            ServerSocket.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, port));

            // 최대 연결 수를 설정하고 대기합니다.
            ServerSocket.Listen(maxConnection);

            // 서버 정보 출력
            Console.WriteLine("Server Start");
            Console.WriteLine("Server Version : 0.00.2");
            Console.WriteLine("Server Port : " + port);
            Console.WriteLine("Server Max Connection : " + maxConnection);
            Console.WriteLine("Server is TCP : " + isTcp);
            Console.WriteLine("Server is UDP : " + isUdp);

            // 서버 설정 완료 메시지 출력
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Server Configuration Complete!!!");

            // 서버 실행 메시지 출력
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Server Running");
            Console.ResetColor();

            // 클라이언트 연결 처리를 수행합니다.
            ProcessConnection(ServerSocket);
        }
        /// <summary>
        /// 클라이언트 연결을 처리하는 메서드입니다.
        /// </summary>
        /// <param name="listenSocket">리스닝 소켓</param>
        public void ProcessConnection(Socket listenSocket)
        {
            // 클라이언트 연결 수락을 비동기로 처리하기 위한 이벤트 및 핸들러 설정
            SocketAsyncEventArgs acceptEventArgs = new SocketAsyncEventArgs();
            acceptEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptCompleted);
            listenSocket.AcceptAsync(acceptEventArgs);


            // 서버 관리 명령 루프 (예: 'exit' 명령을 입력하면 종료)
            while (true)
            {
                Thread.Sleep(1000);
                string strCMD = Console.ReadLine();
                if (strCMD.Equals("exit"))
                {
                    break;
                }

            }
        }

        /// <summary>
        /// 클라이언트 연결 수락 완료 이벤트 핸들러입니다.
        /// </summary>
        /// <param name="sender">이벤트 발생자</param>
        /// <param name="e">SocketAsyncEventArgs 객체</param>
        public void AcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
         
                    //var receivDataCount = e.BytesTransferred;

                    //if (receivDataCount > 0)
                    //{
                    //    RpcStub stub = new RpcStub();
                    //    byte[] responseBytes = stub.HandleRequest(e.Buffer);
                    //    if (responseBytes != null)
                    //    {
                    //        SendData(e.AcceptSocket, responseBytes);
                    //    }

                    //    // 데이터를 수신하고 다시 데이터 수신을 시작합니다.
                    //    e.AcceptSocket.ReceiveAsync(e);
                    //}
              

                // 클라이언트 신원을 확인할 수 없는 경우를 처리합니다.
                string unknownUser = "Unknown";

                // 클라이언트에 대한 정보를 출력합니다.
                Console.WriteLine("User? :" + unknownUser);

                // 데이터 수신을 위한 SocketAsyncEventArgs를 설정합니다.
                SocketAsyncEventArgs receiveEventArgs = new SocketAsyncEventArgs();
                receiveEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveCompleted);
                receiveEventArgs.SetBuffer(new byte[receiveBufferSize], 0, receiveBufferSize);
                receiveEventArgs.UserToken = unknownUser;
                receiveEventArgs.AcceptSocket = e.AcceptSocket;

                //SendData(e.AcceptSocket, "Connect Success-Server\n");
                // 데이터 수신을 시작합니다.
                e.AcceptSocket.ReceiveAsync(receiveEventArgs);


                // 다음 연결을 대기합니다.
                Console.WriteLine("Date :: " + DateTime.Now);
                e.AcceptSocket = null;
                ServerSocket.AcceptAsync(e);
            }
        }

        /// <summary>
        /// 클라이언트로부터 데이터 수신 완료 이벤트 핸들러입니다.
        /// </summary>
        /// <param name="sender">이벤트 발생자</param>
        /// <param name="e">SocketAsyncEventArgs 객체</param>
        public async void ReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
            {


                var receivDataCount = e.BytesTransferred;

                if (receivDataCount > 0)
                {
                    // 처음 1바이트는 바이트 전송 또는 제이슨 전송을 식별합니다 0이면 바이트, 1이면 제이슨 통신
                    var sendType =(SendType) e.Buffer[0];

                    if (SendType.Direct == sendType)
                    {
                        //RpcStub rpcStub = new RpcStub();
                        //byte[] responseBytes = rpcStub.HandleRequest(e.Buffer);
                        //if (responseBytes != null)
                        //{
                        //    SendData(e.AcceptSocket, responseBytes);
                        //}
                        // 앞의 2개 바이트는 건너 띈고 복사한다

                        byte[] data = new byte[e.BytesTransferred - 1];
                        Array.Copy(e.Buffer, 1, data, 0, e.BytesTransferred - 1);
                        serverLogic.ProcessDirectProtocol(data, e.AcceptSocket);



                    }
                    else if (SendType.Json == sendType)
                    {
                        // 2번쨰 바이트는 프로토콜 식별자
                        var protocolSelect = (ProtocolSelect)e.Buffer[1];



                        // 식별자를 제외하고 바이트 배열을 string으로 변환
                        string json = Encoding.ASCII.GetString(e.Buffer, 2, e.BytesTransferred - 2);

                        // 디버깅을 위해 출력
                        Console.WriteLine("Received JSON: " + json);

                        serverLogic.ProcessJsonProtocol(json, protocolSelect, e.AcceptSocket);
                        // 제이슨 통신 
                        // 바이트 배열을 string으로 변환
                    }




                }
            }
            else if (e.SocketError == SocketError.ConnectionReset)
            {
                // 클라이언트와의 연결이 끊겼을 때 (ConnectionReset 예외 발생)
                try
                {
                    int playerId = (int)e.UserToken;
                    Console.WriteLine("플레이어 " + playerId + "와의 연결 끊김");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error closing socket: " + ex.Message);
                }
            }


            // 데이터를 수신하고 다시 데이터 수신을 시작합니다.
            e.AcceptSocket.ReceiveAsync(e);
        }
        /// <summary>
        /// 지정된 소켓을 통해 데이터를 비동기적으로 전송합니다. 소켓의 연결 상태를 확인한 후 전송을 시도하며, 오류가 발생하면 소켓을 닫습니다.
        /// </summary>
        /// <param name="socket">데이터를 전송할 대상 소켓입니다.</param>
        /// <param name="data">전송할 데이터가 담긴 바이트 배열입니다.</param>
        public async void SendData(Socket socket, byte[] data)
        {
            // 연결 상태 확인
            if (socket.Connected && !(socket.Poll(1000, SelectMode.SelectRead) && socket.Available == 0))
            {
                try
                {
                    await socket.SendAsync(new ReadOnlyMemory<byte>(data), SocketFlags.None);
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"데이터 전송 중 오류 발생: {ex.Message}");
                    // 연결이 끊긴 소켓은 처리하지 않고 닫음
                    socket.Close();
                }
            }
            else
            {
                Console.WriteLine("연결이 끊긴 소켓입니다. 전송하지 않습니다.");
                socket.Close();  // 연결이 끊긴 소켓을 닫아줌

                // 플레이어 딕셔너리에서 키값을 소켓으로 찿아보자
                var key = players.FirstOrDefault(x => x.Value == socket).Key;
               
                HandleDisconnectedSocket(key, socket);
            }
        }
        public async void SendData(int hostID, byte[] data)
        {
            var socket = players[hostID];
            // 연결 상태 확인
            if (socket.Connected && !(socket.Poll(1000, SelectMode.SelectRead) && socket.Available == 0))
            {
                try
                {
                    await socket.SendAsync(new ReadOnlyMemory<byte>(data), SocketFlags.None);
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"데이터 전송 중 오류 발생: {ex.Message}");
                    // 연결이 끊긴 소켓은 처리하지 않고 닫음
                    socket.Close();
                }
            }
            else
            {
                HandleDisconnectedSocket(hostID, socket);
            }
        }
        private System.Timers.Timer socketCheckTimer;
        public void StartSocketCheck()
        {
            
            socketCheckTimer = new System.Timers.Timer(1000); // 10,000ms = 10초
            socketCheckTimer.Elapsed += (sender, e) => SocketCheck();
            socketCheckTimer.AutoReset = true; // 반복적으로 실행
            socketCheckTimer.Enabled = true;   // 타이머 시작
        }

        public void StopSocketCheck()
        {
            socketCheckTimer.Stop();   // 타이머 중지
            socketCheckTimer.Dispose(); // 타이머 해제
        }



        public void SocketCheck()
        {
            foreach (var player in players)
            {
                if (!isConnect(player.Value))
                {
                    HandleDisconnectedSocket(player.Key, player.Value);
                }
            }
        }

        public bool isConnect(Socket socket)
        {
            return socket.Connected && !(socket.Poll(1000, SelectMode.SelectRead) && socket.Available == 0);
        }
        public void HandleDisconnectedSocket(int hostID, Socket socket)
        {
            Console.WriteLine("연결이 끊긴 소켓입니다. 전송하지 않습니다.");
            socket.Close();

            ServerMemory.RemoveGameIDFromDictionary(hostID);

            players.Remove(hostID);

            byte[] tempArray = new byte[2];
            tempArray[0] = (byte)MethodsSelector.NotifyPlayerExit;
            tempArray[1] = (byte)hostID;
            RpcStub rpcStub = new RpcStub();
            var result = rpcStub.HandleRequest(socket, tempArray);

            if(result == null)
            {
                Console.WriteLine("플레이어 "+hostID+" 퇴장");
            }  
        }

        private void UdpServerStart()
        {
            throw new NotImplementedException("UDP server is not yet implemented.");
        }
    }

    /// <summary>
    /// 버퍼 풀 클래스 (메모리 재사용을 위한 버퍼 관리)
    /// </summary>
    public class BufferPool
    {
        private readonly ConcurrentBag<byte[]> _buffers;
        private readonly int _bufferSize;

        public BufferPool(int bufferSize, int initialCount)
        {
            _buffers = new ConcurrentBag<byte[]>();
            _bufferSize = bufferSize;

            for (int i = 0; i < initialCount; i++)
            {
                _buffers.Add(new byte[_bufferSize]);
            }
        }

        public byte[] GetBuffer()
        {
            if (_buffers.TryTake(out byte[] buffer))
            {
                return buffer;
            }
            return new byte[_bufferSize]; // 버퍼 풀이 없으면 새로운 버퍼 생성
        }

        public void ReturnBuffer(byte[] buffer)
        {
            _buffers.Add(buffer);
        }
    }
}



//using GearsSoft.ManageLogic;
//using GearsSoft.ManageLogic.Protocol;
//using GearsSoft.ManageLogic.Util;
//using MySqlX.XDevAPI;
//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net.Sockets;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace GearsSoft
//{
//    public class MainServer
//    {
//        // 플레이어 정보를 저장하는 딕셔너리  
//        public Dictionary<int, Socket> players;

//        // 플레이어 정보에 접근하는 데 사용하는 락  
//        public object playerLock;

//        // 최대 연결수  
//        private int maxConnection;

//        // 포트  
//        private int port;

//        // UDP, TCP 여부  
//        private bool isUdp;
//        private bool isTcp;

//        // 서버 소켓  
//        private Socket ServerSocket;

//        // 송신 버퍼 크기와 수신 버퍼 크기  
//        private int sendBufferSize;
//        private int receiveBufferSize;

//        // 서버 GUID  
//        public string GUID;

//        // 클라이언트 정보를 저장하는 ConcurrentDictionary  
//        private ConcurrentDictionary<Socket, ClientInfo> _clients = new ConcurrentDictionary<Socket, ClientInfo>();

//        // 송신 버퍼 크기 속성  
//        public int SendBufferSize
//        {
//            get
//            {
//                return sendBufferSize;
//            }
//            set
//            {
//                sendBufferSize = value;
//            }
//        }

//        // 수신 버퍼 크기 속성  
//        public int ReceiveBufferSize
//        {
//            get
//            {
//                return receiveBufferSize;
//            }
//            set
//            {
//                receiveBufferSize = value;
//            }
//        }
//        public MainServer(int maxConnection, int port, bool isUdp)
//        {
//            this.maxConnection = maxConnection;

//            players = new Dictionary<int, Socket>();
//            playerLock = new object();
//            players.Clear();

//            GUID = Guid.NewGuid().ToString();
//            Console.WriteLine("Server Guid : " + GUID);
//            this.port = port;

//        }

//        public MainServer(int maxConnection, int port, bool isUdp, bool isTCP)
//        {
//            this.maxConnection = maxConnection;
//            this.port = port;
//            this.isUdp = isUdp;
//            this.isTcp = isTCP;

//            players = new Dictionary<int, Socket>();
//            playerLock = new object();
//            players.Clear();

//            GUID = Guid.NewGuid().ToString();
//            Console.WriteLine("Server Guid : " + GUID);
//        }
//        public MainServer(ServerConnectConfigParameter config)
//        {
//            this.maxConnection = config.MaxConnection;
//            this.port = config.Port;
//            this.isUdp = config.isUdp;
//            this.isTcp = config.isTcp;
//            this.sendBufferSize = config.SendBufferSize;
//            this.receiveBufferSize = config.ReceiveBufferSize;
//            this.GUID = config.Guid;

//            players = new Dictionary<int, Socket>();
//            playerLock = new object();
//            players.Clear();
//        }



//        /// <summary>  
//        /// 서버를 시작하는 메서드입니다.  
//        /// </summary>  
//        public void StartServer()
//        {
//            // 콘솔에 현재 서버 타입을 출력합니다.  
//            Console.Write("Server Type: ");

//            if (isUdp)
//            {
//                // UDP 서버를 시작합니다.  
//                UdpServerStart();
//            }
//            else if (isTcp)
//            {
//                if (GUID == null)
//                {
//                    // TCP 서버를 시작하기 위해 GUID가 필요합니다. GUID가 null인 경우 예외를 던집니다.  
//                    throw new ArgumentNullException("guid", "GUID cannot be null when starting TCP server.");
//                }

//                // TCP 서버를 시작합니다.  
//                TcpServerStart();
//            }
//            else
//            {
//                // 유효한 서버 타입이 선택되지 않은 경우 예외를 던집니다.  
//                throw new InvalidOperationException("Neither UDP nor TCP is selected for server type.");
//            }
//        }

//        /// <summary>  
//        /// TCP 서버를 시작하는 메서드입니다.  
//        /// </summary>  
//        private void TcpServerStart()
//        {
//            // TCP 소켓을 생성합니다.  
//            ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

//            // 서버 소켓을 지정된 포트로 바인딩합니다.  
//            ServerSocket.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, port));

//            // 최대 연결 수를 설정하고 대기합니다.  
//            ServerSocket.Listen(maxConnection);

//            // 서버 정보 출력  
//            Console.WriteLine("Server Start");
//            Console.WriteLine("Server Version : 0.20.0");
//            Console.WriteLine("Server Port : " + port);
//            Console.WriteLine("Server Max Connection : " + maxConnection);
//            Console.WriteLine("Server is TCP : " + isTcp);
//            Console.WriteLine("Server is UDP : " + isUdp);

//            // 서버 설정 완료 메시지 출력  
//            Console.ForegroundColor = ConsoleColor.Red;
//            Console.WriteLine("Server Configuration Complete!!!");

//            // 서버 실행 메시지 출력  
//            Console.ForegroundColor = ConsoleColor.Blue;
//            Console.WriteLine("Server Running");
//            Console.ResetColor();


//            while (true)
//            {
//                ServerSocket.BeginAccept(AcceptCallback, null);
//            }

//            // 클라이언트 연결 처리를 수행합니다.  
//            //ProcessConnection(ServerSocket);  
//        }

//        private void AcceptCallback(IAsyncResult ar)
//        {
//            Socket clientSocket = ServerSocket.EndAccept(ar);

//            // 클라이언트 정보 추가  

//            ClientInfo client = new ClientInfo { ClientSocket = clientSocket, Buffer = new byte[sendBufferSize] };
//            _clients[clientSocket] = client;

//            // 클라이언트로부터 비동기적으로 데이터 수신 시작  
//            clientSocket.BeginReceive(client.Buffer, 0, client.Buffer.Length, SocketFlags.None, ReceiveCallback, client);

//            // 다음 클라이언트 연결 대기  
//            ServerSocket.BeginAccept(AcceptCallback, null);
//        }

//        private void ReceiveCallback(IAsyncResult ar)
//        {
//            ClientInfo client = (ClientInfo)ar.AsyncState;
//            Socket clientSocket = client.ClientSocket;

//            // 클라이언트로부터 받은 데이터 처리
//            int received = clientSocket.EndReceive(ar);

//            if (received > 0)
//            {
//                RpcStub stub = new RpcStub();
//                byte[] responseBytes = stub.HandleRequest(client.Buffer);

//                if (responseBytes != null)
//                {
//                    // 응답이 필요한 경우 응답 전송
//                    clientSocket.BeginSend(responseBytes, 0, responseBytes.Length, SocketFlags.None, SendCallback, clientSocket);
//                }

//                // 계속해서 데이터 수신 대기
//                clientSocket.BeginReceive(client.Buffer, 0, client.Buffer.Length, SocketFlags.None, ReceiveCallback, client);
//            }
//            else
//            {
//                // 클라이언트 연결 종료 처리
//                clientSocket.Shutdown(SocketShutdown.Both);
//                clientSocket.Close();
//                _clients.TryRemove(clientSocket, out _);
//            }
//        }

//        private void SendCallback(IAsyncResult ar)
//        {
//            Socket clientSocket = (Socket)ar.AsyncState;
//            clientSocket.EndSend(ar);
//        }

//        public class ClientInfo
//        {
//            public Socket ClientSocket { get; set; }
//            public byte[] Buffer { get; set; }
//        }

//        /// <summary>  
//        /// UDP 서버를 시작하는 메서드입니다.  
//        /// 현재 미구현 상태입니다.  
//        /// </summary>  
//        /// <exception cref="NotImplementedException">UDP 서버가 아직 구현되지 않았습니다.</exception>  
//        private void UdpServerStart()
//        {
//            // 아직 UDP 서버가 구현되지 않았으므로 예외를 던집니다.  
//            throw new NotImplementedException("UDP server is not yet implemented.");
//        }


//    }
//}
