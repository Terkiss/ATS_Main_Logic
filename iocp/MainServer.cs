using TeruTeruServer.ManageLogic;
using TeruTeruServer.ManageLogic.Protocol;
using TeruTeruServer.ManageLogic.Util;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;
using System.Threading.Tasks;
using System.IO;
using TeruTeruServer.Command;

namespace TeruTeruServer
{
    public class MainServer
    {
        // 플레이어 소켓 정보를 저장하는 딕셔너리
        public Dictionary<int, Socket> players;

        // 플레이어 정보 접근을 위한 잠금 객체
        public object playerLock;

        // 동시 접속 가능한 최대 연결 수
        private int _maxConnection;

        // 서버 포트
        private int _port;

        // 지원되는 프로토콜 플래그
        private bool _isUdp;
        private bool _isTcp;

        // 대기 중인 메인 서버 소켓
        private Socket _serverSocket;

        // 네트워크 I/O 버퍼 크기
        private int _sendBufferSize;
        private int _receiveBufferSize;

        // 고유 서버 식별자
        public string GUID;

        private ServerLogic _serverLogic;

        private RpcProxy _rpcProxy;

        private CommandHandler _commandHandler;

        // 전송 버퍼 크기 프로퍼티
        public int SendBufferSize
        {
            get => _sendBufferSize;
            set => _sendBufferSize = value;
        }

        // 수신 버퍼 크기 프로퍼티
        public int ReceiveBufferSize
        {
            get => _receiveBufferSize;
            set => _receiveBufferSize = value;
        }


        public MainServer(int maxConnection, int port, bool isUdp, bool isTcp)
        {
            this.Initialize(maxConnection, port, isUdp, isTcp);
            GUID = Guid.NewGuid().ToString();
            Console.WriteLine("Server Guid : " + GUID);
        }

        public MainServer(ServerConnectConfigParameter config)
        {
            this.Initialize(config.MaxConnection, config.Port, config.IsUdp, config.IsTcp);
            this._sendBufferSize = config.SendBufferSize;
            this._receiveBufferSize = config.ReceiveBufferSize;
            this.GUID = config.Guid;
        }

        private void Initialize(int maxConnection, int port, bool isUdp, bool isTcp)
        {
            this._maxConnection = maxConnection;
            this._port = port;
            this._isUdp = isUdp;
            this._isTcp = isTcp;

            players = new Dictionary<int, Socket>();
            playerLock = new object();
            players.Clear();

            _serverLogic = new ServerLogic(this);
            ServerMemory.MainServer = this;
            _rpcProxy = new RpcProxy();
            _commandHandler = new CommandHandler(this);
        }




        /// <summary>
        /// 서버를 시작하는 메서드입니다.
        /// </summary>
        public void StartServer()
        {
            Console.Write("Server Type: ");
            StartSocketCheck();
            if (_isUdp)
            {
                UdpServerStart();
            }
            else if (_isTcp)
            {
                if (GUID == null)
                {
                    throw new ArgumentNullException("guid", "TCP 서버를 시작할 때 GUID는 null일 수 없습니다.");
                }

                TcpServerStart();
            }
            else
            {
                throw new InvalidOperationException("서버 유형으로 UDP나 TCP가 선택되지 않았습니다.");
            }
        }


        /// <summary>
        /// TCP 서버를 시작하는 메서드입니다.
        /// </summary>
        private void TcpServerStart()
        {
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, _port));
            _serverSocket.Listen(_maxConnection);

            Console.WriteLine("Server Start");
            Console.WriteLine("Server Version : 0.00.2");
            Console.WriteLine("Server Port : " + _port);
            Console.WriteLine("Server Max Connection : " + _maxConnection);
            Console.WriteLine("Server is TCP : " + _isTcp);
            Console.WriteLine("Server is UDP : " + _isUdp);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Server Configuration Complete!!!");

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Server Running");
            Console.ResetColor();

            StartAcceptLoop(_serverSocket);
            RunCommandLoop();
        }

        private void StartAcceptLoop(Socket listenSocket)
        {
            SocketAsyncEventArgs acceptEventArgs = new SocketAsyncEventArgs();
            acceptEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptCompleted);
            listenSocket.AcceptAsync(acceptEventArgs);
        }

        private void RunCommandLoop()
        {
            while (true)
            {
                Thread.Sleep(1000);
                string strCMD = Console.ReadLine();

                if (!HandleConsoleCommand(strCMD))
                    break;
            }
        }

        private bool HandleConsoleCommand(string command)
        {
            return _commandHandler.Handle(command);
        }

        private void AcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                HandleAcceptedSocket(e);
            }
            else
            { 
                TeruTeruLogger.LogError("수락(Accept) 실패: " + e.SocketError.ToString());
            }

            // 다음 수락 대기 (UDP의 경우 listenSocket이 null이 될 수 있으므로 체크)
            if (_isTcp && _serverSocket != null)
            {
                e.AcceptSocket = null;
                try
                {
                    _serverSocket.AcceptAsync(e);
                }
                catch (ObjectDisposedException) { }
            }
        }

        private void HandleAcceptedSocket(SocketAsyncEventArgs e)
        {
            var acceptedSocket = e.AcceptSocket;
            this.LogAcceptedConnection(acceptedSocket);

            var receiveArgs = CreateReceiveArgs(acceptedSocket);

            // 비동기 수신 시작
            acceptedSocket.ReceiveAsync(receiveArgs);
        }

        private void LogAcceptedConnection(Socket socket)
        {
            string user = "Unknown";
            Console.WriteLine("User? : " + user);
            Console.WriteLine("Date  : " + DateTime.Now);
            if (socket.RemoteEndPoint != null)
            {
                Console.WriteLine("Remote : " + socket.RemoteEndPoint.ToString());
            }
        }
        private SocketAsyncEventArgs CreateReceiveArgs(Socket socket)
        { 
            var args = new SocketAsyncEventArgs();

            args.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveCompleted);
            args.SetBuffer(new byte[_receiveBufferSize], 0, _receiveBufferSize);
            args.UserToken = "Unknown"; // 처리 중 업데이트 예정
            args.AcceptSocket = socket;
            return args;
        }





        private async void ReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
            {
                ProcessReceivedData(e.AcceptSocket, e.Buffer, e.BytesTransferred);
                
                // 다음 수신을 계속 등록
                try
                {
                    e.AcceptSocket.ReceiveAsync(e);
                }
                catch (ObjectDisposedException) { }
            }
            else if (e.SocketError == SocketError.ConnectionReset || e.BytesTransferred == 0)
            {
                HandleConnectionReset(e);
            }
        }



        private void ProcessReceivedData(Socket socket, byte[] buffer, int count)
        {
            var sendType = (SendType)buffer[0];

            if (sendType == SendType.Direct)
            {
                ProcessDirect(buffer, count, socket);
            }
            else if (sendType == SendType.Json)
            { 
                ProcessJson(buffer, count, socket);
            }
        }


        private void ProcessDirect(byte[] buffer, int count, Socket socket)
        { 
            byte[] data = new byte[count - 1];
            Array.Copy(buffer, 1, data, 0, count - 1);

            _serverLogic.ProcessDirectProtocol(data, socket);
        }

        private void ProcessJson(byte[] buffer, int count, Socket socket)
        { 
            byte[] data = new byte[count - 1];
            Array.Copy(buffer, 1, data, 0, count - 1);
            string json = Encoding.ASCII.GetString(data);

            TeruTeruLogger.LogInfo("Received JSON: " + json);

            _serverLogic.ProcessJsonProtocol(json, ProtocolSelect.ConnectProtocol, socket);
        }

        private void HandleConnectionReset(SocketAsyncEventArgs e)
        {
            // 클라이언트와의 연결이 끊겼을 때 (ConnectionReset 예외 발생)
            try
            {
                if (e.UserToken is int playerId)
                {
                    Console.WriteLine("플레이어 " + playerId + "와의 연결 끊김");
                }
                else
                {
                    Console.WriteLine("알 수 없는 클라이언트와의 연결 끊김 (UserToken: " + e.UserToken + ")");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error closing socket: " + ex.Message);
            }
        }



        public async void SendData(Socket socket, byte[] data)
        {
            if (!await TrySend(socket, data))
            {
                Console.WriteLine("연결이 끊긴 소켓입니다. 전송하지 않습니다.");
                socket.Close();

                // 기존: var key = players.FirstOrDefault(x => x.Value == socket).Key;
                // 변경: TryGetValueBySocket
                if (TryGetHostIDBySocket(socket, out int hostID))
                {
                    HandleDisconnectedSocket(hostID, socket);
                }
                else
                {
                    Console.WriteLine("소켓에 해당하는 플레이어를 찾을 수 없습니다.");
                }
            }
        }

        public async void SendData(int hostID, byte[] data)
        {
            if (players.TryGetValue(hostID, out var socket))
            {
                if (!await TrySend(socket, data))
                {
                    HandleDisconnectedSocket(hostID, socket);
                }
            }
        }
        private bool TryGetHostIDBySocket(Socket socket, out int hostID)
        {
            foreach (var kvp in players)
            {
                if (kvp.Value == socket)
                {
                    hostID = kvp.Key;
                    return true;
                }
            }
            hostID = -1;
            return false;
        }



        private async Task<bool> TrySend(Socket socket, byte[] data)
        {
            if (socket.Connected && !(socket.Poll(1000, SelectMode.SelectRead) && socket.Available == 0))
            {
                try
                {
                    await socket.SendAsync(new ReadOnlyMemory<byte>(data), SocketFlags.None);
                    return true;
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"데이터 전송 중 오류 발생: {ex.Message}");
                }
            }
            return false;
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
                if (!IsConnected(player.Value))
                {
                    HandleDisconnectedSocket(player.Key, player.Value);
                }
            }
        }

        public bool IsConnected(Socket socket)
        {
            if (_isUdp)
            {
                // UDP는 연결 지향이 아니므로 소켓이 유효하고 열려 있는지만 체크
                return socket != null && !socket.SafeHandle.IsInvalid && !socket.SafeHandle.IsClosed;
            }
            return socket.Connected && !(socket.Poll(1000, SelectMode.SelectRead) && socket.Available == 0);
        }
        public void HandleDisconnectedSocket(int hostID, Socket socket)
        {
            Console.WriteLine("연결이 끊긴 소켓입니다. 소켓을 닫습니다.");
            
            if (_isUdp && socket.RemoteEndPoint != null)
            {
                _udpSessions.TryRemove(socket.RemoteEndPoint, out _);
            }

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
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _serverSocket.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, _port));

            Console.WriteLine("UDP Server Start");
            Console.WriteLine("Server Version : 0.00.2");
            Console.WriteLine("Server Port : " + _port);
            Console.WriteLine("Server Max Connection : " + _maxConnection);
            Console.WriteLine("Server is TCP : " + _isTcp);
            Console.WriteLine("Server is UDP : " + _isUdp);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("UDP Server Configuration Complete!!!");

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("UDP Server Running");
            Console.ResetColor();

            StartUdpAcceptLoop();
            RunCommandLoop();
        }

        private void StartUdpAcceptLoop()
        {
            SocketAsyncEventArgs udpArgs = new SocketAsyncEventArgs();
            udpArgs.RemoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0);
            udpArgs.SetBuffer(new byte[_receiveBufferSize], 0, _receiveBufferSize);
            udpArgs.Completed += OnUdpReceiveFromCompleted;
            
            if (!_serverSocket.ReceiveFromAsync(udpArgs))
            {
                OnUdpReceiveFromCompleted(_serverSocket, udpArgs);
            }
        }

        private void OnUdpReceiveFromCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
            {
                HandleNewUdpPacket(e);
            }
            
            // 다음 패킷 수신 대기
            e.RemoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0);
            try
            {
                if (!_serverSocket.ReceiveFromAsync(e))
                {
                    OnUdpReceiveFromCompleted(_serverSocket, e);
                }
            }
            catch (ObjectDisposedException) { }
        }

        // 특정 Endpoint와 통신하는 소켓들을 관리하기 위한 딕셔너리
        private ConcurrentDictionary<EndPoint, Socket> _udpSessions = new ConcurrentDictionary<EndPoint, Socket>();

        private void HandleNewUdpPacket(SocketAsyncEventArgs e)
        {
            EndPoint remoteEP = e.RemoteEndPoint;

            // 이미 활성화된 세션 소켓이 있는지 확인
            if (_udpSessions.TryGetValue(remoteEP, out Socket sessionSocket))
            {
                // 기존 세션이 있으면 해당 소켓으로 데이터 처리 (하지만 보통은 OS가 해당 소켓으로 바로 전달함)
                byte[] data = new byte[e.BytesTransferred];
                Array.Copy(e.Buffer, e.Offset, data, 0, e.BytesTransferred);
                ProcessReceivedData(sessionSocket, data, e.BytesTransferred);
            }
            else
            {
                // 새로운 클라이언트로부터의 첫 패킷
                Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                clientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                clientSocket.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, _port));
                clientSocket.Connect(remoteEP);

                if (_udpSessions.TryAdd(remoteEP, clientSocket))
                {
                    var receiveArgs = CreateReceiveArgs(clientSocket);
                    
                    byte[] firstPacketData = new byte[e.BytesTransferred];
                    Array.Copy(e.Buffer, e.Offset, firstPacketData, 0, e.BytesTransferred);
                    
                    // 첫 패킷 처리
                    ProcessReceivedData(clientSocket, firstPacketData, e.BytesTransferred);
                    
                    // 이후 패킷은 이 세션 소켓에서 비동기 수신
                    clientSocket.ReceiveAsync(receiveArgs);
                }
                else
                {
                    clientSocket.Close();
                }
            }
        }
    }
    
}

