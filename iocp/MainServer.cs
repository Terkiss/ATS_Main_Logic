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
using System.Threading.Tasks;

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

        private RpcProxy rpcProxy;

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


        public MainServer(int maxConnection, int port, bool isUdp, bool isTCP)
        {
            this.Initialize(maxConnection, port, isUdp, isTCP);
            GUID = Guid.NewGuid().ToString();
            Console.WriteLine("Server Guid : " + GUID);
        }
        public MainServer(ServerConnectConfigParameter config)
        {
            this.Initialize(config.MaxConnection, config.Port, config.isUdp, config.isTcp);
            this.sendBufferSize = config.SendBufferSize;
            this.receiveBufferSize = config.ReceiveBufferSize;
            this.GUID = config.Guid;
        }


        // 생성자들 아래에 위치
        private void Initialize(int maxConnection, int port, bool isUdp, bool isTcp)
        {
            this.maxConnection = maxConnection;
            this.port = port;
            this.isUdp = isUdp;
            this.isTcp = isTcp;

            players = new Dictionary<int, Socket>();
            playerLock = new object();
            players.Clear();

    
            serverLogic = new ServerLogic(this);
            ServerMemory.MainServer = this;
            rpcProxy = new RpcProxy();
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
                else if (strCMD.Equals("Queue_Count"))
                {
                    var preOrderCount = ServerMemory.GetImageWork_PreOrder_QueueCount();
                    var completeCount = ServerMemory.GetImageWork_Complete_QueueCount();

                    TeruTeruLogger.LogInfo($"preOrderCount : {preOrderCount}, CompleteCount : {completeCount}");
                }
                else if (strCMD.Equals("2"))
                {
                    int Count = 0;
                    // 상대경로로 Receve 폴더 생성
                    string path = @"Receve";
                    if (!System.IO.Directory.Exists(path))
                    {
                        System.IO.Directory.CreateDirectory(path);
                    }
                    while (ServerMemory.GetImageWork_PreOrder_Queue(out SendImageData sendImageData))
                    {


                        // 이미지 파일 생성

                        string fileName = $"image_{Count}.jpg";

                        string filePath = System.IO.Path.Combine(path, fileName);


                        if (sendImageData.imgSize < sendImageData.data.Length)
                        {
                            byte[] imgByte = new byte[sendImageData.imgSize];

                            for (int i = 0; i < sendImageData.imgSize; i++)
                            {
                                imgByte[i] = sendImageData.data[i];
                            }

                            System.IO.File.WriteAllBytes(filePath, imgByte);
                        }
                        else
                        {
                            System.IO.File.WriteAllBytes(filePath, sendImageData.data);
                        }
                        Count++;
                    }


                    //// Receve folder creadte 
                    //// 상대경로로 Receve 폴더 생성
                    //string path = @"Receve";
                    //if (!System.IO.Directory.Exists(path))
                    //{
                    //    System.IO.Directory.CreateDirectory(path);
                    //}

                    //// 이미지 파일 생성

                    //string fileName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_fff") + "image.jpg";

                    //string filePath = System.IO.Path.Combine(path, fileName);


                    //byte[] imgByte = new byte[sendImageData.imgSize];

                    //for (int i = 0; i < sendImageData.imgSize; i++)
                    //{
                    //    imgByte[i] = sendImageData.data[i];
                    //}

                    //System.IO.File.WriteAllBytes(filePath, imgByte);
                }
                else if (strCMD.Equals("Worker_Start"))
                {
                    Thread workerThread = new Thread(
                        () =>
                        {
                            while (true)
                            {
                                // 작업 큐에서 작업을 가져와서 처리
                                if (ServerMemory.GetImageWork_PreOrder_Queue(out SendImageData PreOrderItem))
                                {
                            
                                    //TeruTeruLogger.LogInfo($"PreOrderItem : {PreOrderItem.imgSize}, {PreOrderItem.hostID}");
                                    
                                    rpcProxy.RequestObjectDetect(PreOrderItem);

                                }

                                if(ServerMemory.GetImageWork_Complete_Queue(out SendImageData completeItem))
                                {

                               
                                    TeruTeruLogger.LogInfo($"CompleteItem : {completeItem.imgSize}, {completeItem.hostID}");
                                }
                            }
                        }
                        );
                    workerThread.Start();
                }

            }
        }


        private void AcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                HandleAcceptedSocket(e);
            }
            else
            { 
                TeruTeruLogger.LogError("Accept failed: " + e.SocketError.ToString());
            }
        }



        private void HandleAcceptedSocket(SocketAsyncEventArgs e)
        {
            var acceptedSocket = e.AcceptSocket;

            this.LogAcceptedConnection(acceptedSocket);

            var receiveArgs = CreateReceiveArgs(acceptedSocket);

            // 수신을 비동기로 시작합니다.
            acceptedSocket.ReceiveAsync(receiveArgs);

            // 다음 accept 대기
            e.AcceptSocket = null;
            this.ServerSocket.AcceptAsync(e);

        }

        private void LogAcceptedConnection(Socket socket)
        {
            string user = "Unknown"; // 초기 연결 단계에서는 식별 불가
            Console.WriteLine("User? : " + user);
            Console.WriteLine("Date  : " + DateTime.Now);
        }
        private SocketAsyncEventArgs CreateReceiveArgs(Socket socket)
        { 
            var args = new SocketAsyncEventArgs();

            args.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveCompleted);
            args.SetBuffer(new byte[receiveBufferSize], 0, receiveBufferSize);
            args.UserToken = "Unknown"; // 초기 연결 단계에서는 식별 불가 후속 처리시 사용
            args.AcceptSocket = socket;
            return args;
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
    
}

