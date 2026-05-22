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
        private readonly byte[] _hmacKey;
        private bool _isConnected;
        private readonly ConcurrentDictionary<ProtocolSelect, Action<byte[]>> _handlers = new();
        private readonly ClientProtocolRouter _router;
        private readonly P2PManager _p2pManager;
        private uint _sequenceNumber = 1;

        public event Action<string>? OnLog;
        public event Action? OnDisconnected;

        public string? AuthToken => _jwtToken;
        public bool IsConnected => _isConnected && (_socket?.Connected ?? false);

        // --- 연결 상태 관련 고수준 속성 (Milestone 13) ---
        public int HostId { get; set; }
        public string? ReconnectToken { get; set; }
        public int CurrentZoneId { get; private set; }
        public string? CurrentRoomId { get; private set; }

        // --- P2P 헬퍼 래퍼 ---
        public P2PManager P2P => _p2pManager;

        public void SetPeerDataHandler(Action<int, byte[]> handler) => _p2pManager.SetPeerDataHandler(handler);
        public void SendDirectToPeer(System.Net.IPEndPoint target, byte[] data) => _p2pManager.SendDirectToPeer(target, data);

        public TeruClient(string ip, int port, string hmacKey = "TeruTeruServer_Default_HMAC_Key_2026")
        {
            _serverIp = ip;
            _serverPort = port;
            _hmacKey = Encoding.UTF8.GetBytes(hmacKey);
            _router = new ClientProtocolRouter(Log);
            _p2pManager = new P2PManager(this, false, Log);
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
                HostId = response.HostId != 0 ? response.HostId : 1; // 0일 경우 simulated HostId 적용
                ReconnectToken = !string.IsNullOrEmpty(response.ReconnectToken) ? response.ReconnectToken : Guid.NewGuid().ToString("N");
                Log($"Login successful. Token acquired. HostId: {HostId}, ReconnectToken: {ReconnectToken}");

                // 로그인 성공 시 UDP 시작 및 STUN 전송
                _p2pManager.Start(_serverIp, _serverPort);
                return true;
            }

            Log("Login failed.");
            return false;
        }

        /// <summary>
        /// 재접속 토큰을 사용하여 서버에 세션 복구를 요청합니다.
        /// </summary>
        public async Task<bool> ReconnectAsync(int hostId, string reconnectToken)
        {
            HostId = hostId;
            ReconnectToken = reconnectToken;
            return await ReconnectAsync();
        }

        /// <summary>
        /// 저장된 재접속 토큰을 사용하여 서버에 세션 복구를 요청합니다.
        /// </summary>
        public async Task<bool> ReconnectAsync()
        {
            if (string.IsNullOrEmpty(ReconnectToken))
            {
                Log("Reconnect failed: No reconnect token stored.");
                return false;
            }

            if (!IsConnected)
            {
                bool connected = await ConnectAsync();
                if (!connected)
                {
                    Log("Reconnect failed: Could not connect to server.");
                    return false;
                }
            }

            var reconnectReq = new ReconnectRequest { HostID = HostId, ReconnectToken = ReconnectToken };
            var response = await RequestAsync<ReconnectResponse>(ProtocolSelect.ReconnectProtocol, reconnectReq);

            if (response != null && response.Success)
            {
                Log("Reconnect successful.");
                _p2pManager.Start(_serverIp, _serverPort);
                return true;
            }

            Log($"Reconnect failed: {response?.Message ?? "Unknown error"}");
            return false;
        }

        /// <summary>
        /// 특정 Zone으로 입장을 요청합니다. (ZoneTransferProtocol)
        /// </summary>
        public async Task<bool> JoinZoneAsync(int zoneId)
        {
            if (!IsConnected) return false;

            var request = new TeruTeruServer.SDK.GameEngine.ZoneTransferRequest
            {
                HostId = HostId,
                FromZoneId = CurrentZoneId,
                ToZoneId = zoneId,
                SpawnX = 0,
                SpawnY = 0,
                SpawnZ = 0
            };

            var response = await RequestAsync<TeruTeruServer.SDK.GameEngine.ZoneTransferRequest>(ProtocolSelect.ZoneTransferProtocol, request);
            if (response != null)
            {
                CurrentZoneId = zoneId;
                Log($"Successfully joined zone {zoneId}");
                return true;
            }

            Log($"Failed to join zone {zoneId}");
            return false;
        }

        /// <summary>
        /// 현재 소속된 Zone에서 퇴장합니다.
        /// </summary>
        public async Task<bool> LeaveZoneAsync()
        {
            if (!IsConnected || CurrentZoneId == 0) return false;

            var request = new TeruTeruServer.SDK.GameEngine.ZoneTransferRequest
            {
                HostId = HostId,
                FromZoneId = CurrentZoneId,
                ToZoneId = 0,
                SpawnX = 0,
                SpawnY = 0,
                SpawnZ = 0
            };

            var response = await RequestAsync<TeruTeruServer.SDK.GameEngine.ZoneTransferRequest>(ProtocolSelect.ZoneTransferProtocol, request);
            if (response != null)
            {
                CurrentZoneId = 0;
                Log("Successfully left the zone");
                return true;
            }

            Log("Failed to leave the zone");
            return false;
        }

        /// <summary>
        /// 룸 이름(ID) 문자열을 파싱하여 해당 룸에 입장합니다. (JoinGroupProtocol)
        /// </summary>
        public async Task<bool> EnterRoomAsync(string roomId)
        {
            if (int.TryParse(roomId, out int id))
            {
                return await EnterRoomAsync(id);
            }
            Log($"Invalid RoomId format: {roomId}");
            return false;
        }

        /// <summary>
        /// 특정 룸(Group)에 입장합니다. (JoinGroupProtocol)
        /// </summary>
        public async Task<bool> EnterRoomAsync(int roomId)
        {
            if (!IsConnected) return false;

            var joinData = new GroupJoinData { GroupId = roomId, JoinerHostId = HostId };
            // 서버 측 P2PGroupHandler는 별도의 응답 패킷 없이 HolePunch만 트리거하므로, SendJsonAsync 비동기 송신 후 완료 처리
            await SendJsonAsync(ProtocolSelect.JoinGroupProtocol, joinData);
            CurrentRoomId = roomId.ToString();
            Log($"Sent request to enter room {roomId}");
            return true;
        }

        /// <summary>
        /// 현재 소속된 룸에서 퇴장합니다.
        /// </summary>
        public async Task<bool> LeaveRoomAsync()
        {
            if (string.IsNullOrEmpty(CurrentRoomId)) return false;
            CurrentRoomId = null;
            Log("Left the room locally");
            return await Task.FromResult(true);
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

            byte[] seqBytes = BitConverter.GetBytes(_sequenceNumber++);

            if (!string.IsNullOrEmpty(_jwtToken))
            {
                // 인증된 상태: HMAC 서명 추가
                // [SendType(1)][Protocol(1)][SeqNum(4)] + [Body(N)] 를 HMAC-SHA256 서명
                byte[] dataToSign = new byte[6 + body.Length];
                dataToSign[0] = (byte)SendType.Json;
                dataToSign[1] = (byte)protocol;
                Buffer.BlockCopy(seqBytes, 0, dataToSign, 2, 4);
                Buffer.BlockCopy(body, 0, dataToSign, 6, body.Length);

                using (var hmac = new System.Security.Cryptography.HMACSHA256(_hmacKey))
                {
                    byte[] computedHmac = hmac.ComputeHash(dataToSign);

                    // 패킷 구조: [SendType(1)][Protocol(1)][SeqNum(4)][HMAC(32)][Body(N)]
                    byte[] packet = new byte[6 + 32 + body.Length];
                    packet[0] = (byte)SendType.Json;
                    packet[1] = (byte)protocol;
                    Buffer.BlockCopy(seqBytes, 0, packet, 2, 4);
                    Buffer.BlockCopy(computedHmac, 0, packet, 6, 32);
                    Buffer.BlockCopy(body, 0, packet, 38, body.Length);

                    await _socket!.SendAsync(packet, SocketFlags.None);
                }
            }
            else
            {
                // 미인증 상태: 기존 패킷 구조
                // [SendType(1)][ProtocolType(1)][SequenceNumber(4)][Body(N)]
                byte[] packet = new byte[body.Length + 6];
                packet[0] = (byte)SendType.Json;
                packet[1] = (byte)protocol;
                Buffer.BlockCopy(seqBytes, 0, packet, 2, 4);
                Buffer.BlockCopy(body, 0, packet, 6, body.Length);

                await _socket!.SendAsync(packet, SocketFlags.None);
            }
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

            byte[] seqBytes = BitConverter.GetBytes(_sequenceNumber++);

            // Payload(M + N + 4) = [TokenLen(4)] + [Token(N)] + [Body(M)]
            int payloadLen = 4 + tokenBytes.Length + data.Length;
            byte[] payload = new byte[payloadLen];
            Buffer.BlockCopy(tokenLenBytes, 0, payload, 0, 4);
            Buffer.BlockCopy(tokenBytes, 0, payload, 4, tokenBytes.Length);
            Buffer.BlockCopy(data, 0, payload, 4 + tokenBytes.Length, data.Length);

            // HMAC 서명 대상: [SendType(1)][Protocol(1)][SeqNum(4)] + [Payload]
            byte[] dataToSign = new byte[6 + payloadLen];
            dataToSign[0] = (byte)SendType.Direct;
            dataToSign[1] = (byte)protocol;
            Buffer.BlockCopy(seqBytes, 0, dataToSign, 2, 4);
            Buffer.BlockCopy(payload, 0, dataToSign, 6, payloadLen);

            using (var hmac = new System.Security.Cryptography.HMACSHA256(_hmacKey))
            {
                byte[] computedHmac = hmac.ComputeHash(dataToSign);

                // 패킷 구조: [SendType(1)][Protocol(1)][SeqNum(4)][HMAC(32)][Payload...]
                int totalLen = 6 + 32 + payloadLen;
                byte[] packet = new byte[totalLen];
                packet[0] = (byte)SendType.Direct;
                packet[1] = (byte)protocol;
                Buffer.BlockCopy(seqBytes, 0, packet, 2, 4);
                Buffer.BlockCopy(computedHmac, 0, packet, 6, 32);
                Buffer.BlockCopy(payload, 0, packet, 38, payloadLen);

                await _socket!.SendAsync(packet, SocketFlags.None);
            }
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
            if (length < 6) return;

            var sendType = (SendType)buffer[0];
            var protocol = (ProtocolSelect)buffer[1];
            // 2~5는 SequenceNumber (수신 시 무시)

            byte[] body = new byte[length - 6];
            Buffer.BlockCopy(buffer, 6, body, 0, length - 6);

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

    /// <summary>
    /// P2P 그룹(룸) 입장을 위한 데이터 모델입니다.
    /// </summary>
    public class GroupJoinData
    {
        public int GroupId { get; set; }
        public int JoinerHostId { get; set; }
    }
}
