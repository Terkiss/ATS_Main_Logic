using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using TeruTeruServer.SDK.Enums;
using TeruTeruServer.SDK.Protocol;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.Client
{
    /// <summary>
    /// TeruTeruServer와 통신하기 위한 고수준 클라이언트 SDK입니다.
    /// </summary>
    public class TeruClient : IDisposable
    {
        private Socket? _socket;
        private string? _jwtToken;
        private readonly string _serverIp;
        private readonly int _serverPort;
        private bool _isConnected;
        private readonly ConcurrentDictionary<ProtocolSelect, Action<byte[]>> _handlers = new();
        private readonly ClientProtocolRouter _router;
        private readonly P2PManager _p2pManager;

        public event Action<string>? OnLog;
        public event Action? OnDisconnected;

        public string? AuthToken => _jwtToken;
        public bool IsConnected => _isConnected && (_socket?.Connected ?? false);

        public TeruClient(string ip, int port)
        {
            _serverIp = ip;
            _serverPort = port;
            _router = new ClientProtocolRouter(Log);
            _p2pManager = new P2PManager(this, Log);
        }

        public void RegisterLogic(object logicInstance)
        {
            _router.Initialize(logicInstance);
        }

        /// <summary>
        /// 서버에 연결하고 배경 수신 루프를 시작합니다.
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await _socket.ConnectAsync(_serverIp, _serverPort);
                _isConnected = true;
                
                _ = Task.Run(ReceiveLoop);
                Log($"Connected to server at {_serverIp}:{_serverPort}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Connection failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 서버에 로그인을 요청하고 JWT 토큰을 획득합니다.
        /// </summary>
        public async Task<bool> LoginAsync(string userId, string password)
        {
            if (!IsConnected) return false;

            var loginData = new LoginProtocol { UserId = userId, Password = password };
            var response = await RequestAsync<LoginProtocol>(ProtocolSelect.LoginProtocol, loginData);
            
            if (response != null && response.IsSuccess && !string.IsNullOrEmpty(response.AuthToken))
            {
                _jwtToken = response.AuthToken;
                Log("Login successful. Token acquired.");
                
                // 로그인 성공 시 UDP 시작 및 STUN 전송
                _p2pManager.Start(_serverIp, _serverPort);
                return true;
            }

            Log("Login failed.");
            return false;
        }

        /// <summary>
        /// 데이터를 전송하고 응답을 기다립니다. (JSON 기반)
        /// </summary>
        public async Task<T?> RequestAsync<T>(ProtocolSelect protocol, object data) where T : class
        {
            var tcs = new TaskCompletionSource<T?>();
            
            // 임시 핸들러 등록
            void Handler(byte[] body)
            {
                try
                {
                    string json = Encoding.UTF8.GetString(body);
                    var result = JsonSerializer.Deserialize<T>(json);
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    Log($"Request deserialization error: {ex.Message}");
                    tcs.SetResult(null);
                }
                finally
                {
                    UnregisterHandler(protocol);
                }
            }

            RegisterHandler(protocol, Handler);
            await SendJsonAsync(protocol, data);

            // 타임아웃 처리 (5초)
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(5000));
            if (completedTask == tcs.Task)
            {
                return await tcs.Task;
            }
            
            UnregisterHandler(protocol);
            Log($"Request timeout: {protocol}");
            return null;
        }

        /// <summary>
        /// JSON 데이터를 비동기로 전송합니다.
        /// </summary>
        public async Task SendJsonAsync(ProtocolSelect protocol, object data)
        {
            if (!IsConnected) return;

            string json = JsonSerializer.Serialize(data);
            byte[] body = Encoding.UTF8.GetBytes(json);

            // [SendType(1)][ProtocolType(1)][Body(N)]
            byte[] packet = new byte[body.Length + 2];
            packet[0] = (byte)SendType.Json;
            packet[1] = (byte)protocol;
            Buffer.BlockCopy(body, 0, packet, 2, body.Length);

            await _socket!.SendAsync(packet, SocketFlags.None);
        }

        /// <summary>
        /// 서버에 RPC 메서드를 호출하고 결과를 대기합니다.
        /// </summary>
        public async Task<T?> InvokeRpcAsync<T>(string methodName, object? parameters = null) where T : class
        {
            string paramJson = parameters != null ? JsonSerializer.Serialize(parameters) : "{}";
            var rpcReq = new RpcRequest { MethodName = methodName, Params = paramJson };
            return await RequestAsync<T>(ProtocolSelect.RpcProtocol, rpcReq);
        }

        /// <summary>
        /// 서버에 RPC 메서드를 호출합니다. (결과 불필요)
        /// </summary>
        public async Task InvokeRpcAsync(string methodName, object? parameters = null)
        {
            string paramJson = parameters != null ? JsonSerializer.Serialize(parameters) : "{}";
            var rpcReq = new RpcRequest { MethodName = methodName, Params = paramJson };
            await SendJsonAsync(ProtocolSelect.RpcProtocol, rpcReq);
        }

        /// <summary>
        /// 인증 토큰을 포함한 Direct 데이터를 전송합니다.
        /// </summary>
        public async Task SendAuthenticatedDirectAsync(ProtocolSelect protocol, byte[] data)
        {
            if (!IsConnected || string.IsNullOrEmpty(_jwtToken)) return;

            byte[] tokenBytes = Encoding.UTF8.GetBytes(_jwtToken);
            byte[] tokenLenBytes = BitConverter.GetBytes(tokenBytes.Length);

            // 구조: [SendType(1)][ProtocolType(1)][TokenLen(4)][Token(N)][Body(M)]
            int totalLen = 2 + 4 + tokenBytes.Length + data.Length;
            byte[] packet = new byte[totalLen];

            packet[0] = (byte)SendType.Direct;
            packet[1] = (byte)protocol;
            Buffer.BlockCopy(tokenLenBytes, 0, packet, 2, 4);
            Buffer.BlockCopy(tokenBytes, 0, packet, 6, tokenBytes.Length);
            Buffer.BlockCopy(data, 0, packet, 6 + tokenBytes.Length, data.Length);

            await _socket!.SendAsync(packet, SocketFlags.None);
        }

        public void RegisterHandler(ProtocolSelect protocol, Action<byte[]> handler)
        {
            _handlers[protocol] = handler;
        }

        public void UnregisterHandler(ProtocolSelect protocol)
        {
            _handlers.TryRemove(protocol, out _);
        }

        private async Task ReceiveLoop()
        {
            byte[] buffer = new byte[8192];
            try
            {
                while (IsConnected)
                {
                    int received = await _socket!.ReceiveAsync(buffer, SocketFlags.None);
                    if (received == 0) break;

                    ProcessPacket(buffer, received);
                }
            }
            catch (Exception ex)
            {
                Log($"Receive loop error: {ex.Message}");
            }
            finally
            {
                _isConnected = false;
                OnDisconnected?.Invoke();
                Log("Disconnected from server.");
            }
        }

        private void ProcessPacket(byte[] buffer, int length)
        {
            if (length < 2) return;

            var sendType = (SendType)buffer[0];
            var protocol = (ProtocolSelect)buffer[1];

            byte[] body = new byte[length - 2];
            Buffer.BlockCopy(buffer, 2, body, 0, length - 2);

            if (sendType == SendType.Json)
            {
                string json = Encoding.UTF8.GetString(body);
                // 명시적으로 등록된 콜백 핸들러 (RequestAsync 등) 우선 처리
                if (_handlers.TryGetValue(protocol, out var handler))
                {
                    handler.Invoke(body);
                }
                else
                {
                    // 그 외 일반 JSON 패킷은 라우터로 전달하여 [Rpc], [Protocol] 어트리뷰트가 있는 메서드 실행
                    _ = _router.RouteAsync(json, protocol);
                }
            }
            else if (sendType == SendType.Direct)
            {
                if (protocol == ProtocolSelect.HolePunchRequest)
                {
                    _p2pManager.HandleSignaling(body);
                }
                else if (_handlers.TryGetValue(protocol, out var handler))
                {
                    handler.Invoke(body);
                }
                else
                {
                    Log($"Unhandled Direct Protocol: {protocol}");
                }
            }
        }

        private void Log(string message)
        {
            OnLog?.Invoke($"[TeruClient] {message}");
        }

        public void Dispose()
        {
            _isConnected = false;
            _socket?.Close();
            _socket?.Dispose();
            _p2pManager.Dispose();
        }
    }
}
