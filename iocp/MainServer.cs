using TeruTeruServer.ManageLogic;
using TeruTeruServer.Common.Protocol;
using TeruTeruServer.Common.Enums;
using TeruTeruServer.ManageLogic.Util;
using TeruTeruServer.Command;
using TeruTeruServer.Pipeline;
using TeruTeruServer.Common.Interfaces;
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

namespace TeruTeruServer
{
    public class MainServer : IMessageSender
    {
        private readonly ISessionManager _sessionManager;
        private readonly ILogicService _serverLogic;
        
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

        private RpcProxy _rpcProxy;
        private CommandHandler _commandHandler;
        private PacketPipeline _pipeline;

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

        public MainServer(ServerConnectConfigParameter config, ILogicService logicService, ISessionManager sessionManager)
        {
            this._sessionManager = sessionManager;
            this._serverLogic = logicService;
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

            ServerMemory.MainServer = this;
            _rpcProxy = new RpcProxy();
            _commandHandler = new CommandHandler(this);

            // 파이프라인 초기화 및 미들웨어 등록
            _pipeline = new PacketPipeline();
            _pipeline.Use(new ValidationMiddleware());
            _pipeline.Use(new DecryptionMiddleware());
            _pipeline.Use(new AuthMiddleware()); 
            _pipeline.Use(new RoutingMiddleware(_serverLogic));
        }

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
            acceptedSocket.ReceiveAsync(receiveArgs);
        }

        private void LogAcceptedConnection(Socket socket)
        {
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
            args.AcceptSocket = socket;
            return args;
        }

        private async void ReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
            {
                byte[] data = new byte[e.BytesTransferred];
                Array.Copy(e.Buffer, e.Offset, data, 0, e.BytesTransferred);
                
                var context = new PacketContext(e.AcceptSocket, data);
                await _pipeline.ExecuteAsync(context);
                
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

        private void HandleConnectionReset(SocketAsyncEventArgs e)
        {
            try
            {
                if (_sessionManager.TryGetHostIdBySocket(e.AcceptSocket, out int playerId))
                {
                    Console.WriteLine("플레이어 " + playerId + "와의 연결 끊김");
                    HandleDisconnectedSocket(playerId, e.AcceptSocket);
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
                Console.WriteLine("연결이 끊긴 소켓입니다. 소켓을 닫습니다.");
                socket.Close();

                if (_sessionManager.TryGetHostIdBySocket(socket, out int hostID))
                {
                    HandleDisconnectedSocket(hostID, socket);
                }
            }
        }

        public async void SendData(int hostID, byte[] data)
        {
            if (_sessionManager.Players.TryGetValue(hostID, out var socket))
            {
                if (!await TrySend(socket, data))
                {
                    HandleDisconnectedSocket(hostID, socket);
                }
            }
        }

        private async Task<bool> TrySend(Socket socket, byte[] data)
        {
            if (socket != null && socket.Connected && !(socket.Poll(1000, SelectMode.SelectRead) && socket.Available == 0))
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
            socketCheckTimer = new System.Timers.Timer(1000);
            socketCheckTimer.Elapsed += (sender, e) => SocketCheck();
            socketCheckTimer.AutoReset = true;
            socketCheckTimer.Enabled = true;
        }

        public void StopSocketCheck()
        {
            socketCheckTimer?.Stop();
            socketCheckTimer?.Dispose();
        }

        public void SocketCheck()
        {
            foreach (var player in _sessionManager.Players)
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
                return socket != null && !socket.SafeHandle.IsInvalid && !socket.SafeHandle.IsClosed;
            }
            return socket != null && socket.Connected && !(socket.Poll(1000, SelectMode.SelectRead) && socket.Available == 0);
        }

        public void HandleDisconnectedSocket(int hostID, Socket socket)
        {
            if (_sessionManager.TryRemovePlayer(hostID, out _))
            {
                TeruTeruLogger.LogInfo($"플레이어 {hostID} 연결 종료 처리 중...");

                if (_isUdp && socket.RemoteEndPoint != null)
                {
                    _udpSessions.TryRemove(socket.RemoteEndPoint, out _);
                }

                try
                {
                    socket.Close();
                }
                catch (Exception ex)
                {
                    TeruTeruLogger.LogError($"소켓 닫기 오류: {ex.Message}");
                }

                ServerMemory.RemoveGameIDFromDictionary(hostID);

                byte[] tempArray = new byte[2];
                tempArray[0] = (byte)ProtocolSelect.ConnectProtocol;
                tempArray[1] = (byte)hostID;
                RpcStub rpcStub = new RpcStub(this, _sessionManager);
                var result = rpcStub.HandleRequest(socket, tempArray);

                if (result == null)
                {
                    TeruTeruLogger.LogInfo("플레이어 " + hostID + " 퇴장 알림 전송 완료");
                }
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

        private async void OnUdpReceiveFromCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
            {
                byte[] data = new byte[e.BytesTransferred];
                Array.Copy(e.Buffer, e.Offset, data, 0, e.BytesTransferred);
                var context = new PacketContext(e.AcceptSocket, data);
                await _pipeline.ExecuteAsync(context);
            }
            
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

        private ConcurrentDictionary<EndPoint, Socket> _udpSessions = new ConcurrentDictionary<EndPoint, Socket>();

        private async void HandleNewUdpPacket(SocketAsyncEventArgs e)
        {
            EndPoint remoteEP = e.RemoteEndPoint;
            if (_udpSessions.TryGetValue(remoteEP, out Socket sessionSocket))
            {
                byte[] data = new byte[e.BytesTransferred];
                Array.Copy(e.Buffer, e.Offset, data, 0, e.BytesTransferred);
                var context = new PacketContext(sessionSocket, data);
                await _pipeline.ExecuteAsync(context);
            }
            else
            {
                Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                clientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                clientSocket.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, _port));
                clientSocket.Connect(remoteEP);

                if (_udpSessions.TryAdd(remoteEP, clientSocket))
                {
                    var receiveArgs = CreateReceiveArgs(clientSocket);
                    byte[] firstPacketData = new byte[e.BytesTransferred];
                    Array.Copy(e.Buffer, e.Offset, firstPacketData, 0, e.BytesTransferred);
                    var context = new PacketContext(clientSocket, firstPacketData);
                    await _pipeline.ExecuteAsync(context);
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
