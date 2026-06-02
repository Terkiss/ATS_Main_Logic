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
        /// <summary>서버와의 TCP 연결을 처리하는 소켓 객체입니다.</summary>
        private Socket? _socket;
        /// <summary>로그인 성공 시 발급받는 인증용 JWT 토큰입니다.</summary>
        private string? _jwtToken;
        /// <summary>연결할 대상 서버의 IP 주소입니다.</summary>
        private readonly string _serverIp;
        /// <summary>연결할 대상 서버의 포트 번호입니다.</summary>
        private readonly int _serverPort;
        /// <summary>패킷 인증을 위한 HMAC SHA-256 암호화 키 바이트 배열입니다.</summary>
        private readonly byte[] _hmacKey;
        /// <summary>현재 연결 상태를 나타내는 휘발성(volatile) 플래그입니다.</summary>
        private volatile bool _isConnected;
        /// <summary>연결 종료 이벤트가 중복 호출되는 것을 방지하기 위한 원자적 상태 플래그입니다.</summary>
        private int _isDisconnectInvoked;
        /// <summary>각 프로토콜 타입별로 등록된 수신 이벤트 핸들러(콜백) 모음입니다.</summary>
        private readonly ConcurrentDictionary<ProtocolSelect, Action<byte[]>> _handlers = new();
        /// <summary>수신된 패킷을 등록된 로직 클래스로 라우팅해주는 컴포넌트입니다.</summary>
        private readonly ClientProtocolRouter _router;
        /// <summary>P2P 연결 수립(홀펀칭) 및 직접 통신을 전담하는 관리자 객체입니다.</summary>
        private readonly P2PManager _p2pManager;
        /// <summary>보내는 패킷의 위변조 방지 및 순서 검증을 위한 일련번호(시퀀스)입니다.</summary>
        private int _sequenceNumber = 1;

        /// <summary>로그 발생 시 실행되는 이벤트 핸들러입니다.</summary>
        public event Action<string>? OnLog;
        /// <summary>서버와 연결이 끊어졌을 때 호출되는 이벤트 핸들러입니다.</summary>
        public event Action? OnDisconnected;

        /// <summary>현재 인증 상태의 JWT 토큰 정보입니다.</summary>
        public string? AuthToken => _jwtToken;
        /// <summary>소켓이 유효하고 실제 연결되어 있는지 여부를 나타냅니다.</summary>
        public bool IsConnected => _isConnected && (_socket?.Connected ?? false);

        // --- 연결 상태 관련 고수준 속성 (Milestone 13) ---
        /// <summary>로그인 성공 시 서버로부터 할당받는 호스트(세션)의 고유 ID입니다.</summary>
        public int HostId { get; set; }
        /// <summary>재접속 시 이전 세션을 매핑하기 위한 보안 토큰입니다.</summary>
        public string? ReconnectToken { get; set; }
        /// <summary>현재 클라이언트가 머물고 있는 게임 월드 Zone의 ID입니다.</summary>
        public int CurrentZoneId { get; private set; }
        /// <summary>현재 클라이언트가 입장한 P2P 룸(Group)의 ID입니다.</summary>
        public string? CurrentRoomId { get; private set; }

        // --- P2P 헬퍼 래퍼 ---
        /// <summary>P2P 관리자 컴포넌트에 대한 참조를 가져옵니다.</summary>
        public P2PManager P2P => _p2pManager;

        /// <summary>P2P 피어(다른 클라이언트)로부터 다이렉트 데이터를 수신했을 때 호출할 처리기를 등록합니다.</summary>
        public void SetPeerDataHandler(Action<int, byte[]> handler) => _p2pManager.SetPeerDataHandler(handler);
        /// <summary>대상 피어로 UDP 패킷을 즉시 직접 전송합니다.</summary>
        public void SendDirectToPeer(System.Net.IPEndPoint target, byte[] data) => _p2pManager.SendDirectToPeer(target, data);

        /// <summary>
        /// TeruClient 생성자입니다. IP, 포트 및 HMAC 인증 키를 구성합니다.
        /// </summary>
        public TeruClient(string ip, int port, string hmacKey = "TeruTeruServer_Default_HMAC_Key_2026")
        {
            _serverIp = ip;
            _serverPort = port;
            _hmacKey = Encoding.UTF8.GetBytes(hmacKey);
            _router = new ClientProtocolRouter(Log);
            _p2pManager = new P2PManager(this, false, Log);
        }

        /// <summary>
        /// Rpc 및 Protocol 속성이 지정된 비즈니스 로직 클래스를 라우터에 등록합니다.
        /// </summary>
        public void RegisterLogic(object logicInstance)
        {
            _router.Initialize(logicInstance);
        }

        /// <summary>
        /// 서버에 비동기식으로 TCP 연결을 수립하고, 데이터를 지속적으로 수신할 수 있는 ReceiveLoop를 배경 태스크로 가동합니다.
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await _socket.ConnectAsync(_serverIp, _serverPort);
                _isDisconnectInvoked = 0;
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
        /// 제공된 ID와 비밀번호를 통해 서버에 인증을 시도하고, 반환받은 JWT 토큰 및 재접속 토큰을 내부 필드에 안전하게 보관합니다.
        /// </summary>
        public async Task<bool> LoginAsync(string userId, string password)
        {
            if (!IsConnected) return false;

            var loginData = new LoginProtocol { UserId = userId, Password = password };
            var response = await RequestAsync<LoginProtocol>(ProtocolSelect.LoginProtocol, loginData);

            if (response != null && response.IsSuccess && !string.IsNullOrEmpty(response.AuthToken))
            {
                _jwtToken = response.AuthToken;
                HostId = response.HostId != 0 ? response.HostId : 1;
                ReconnectToken = !string.IsNullOrEmpty(response.ReconnectToken) ? response.ReconnectToken : Guid.NewGuid().ToString("N");
                Log($"Login successful. Token acquired. HostId: {HostId}, ReconnectToken: {ReconnectToken}");

                // 로그인 성공 시 UDP 통신 채널을 활성화하고 STUN 요청을 보내 홀펀칭을 준비함
                _p2pManager.Start(_serverIp, _serverPort);
                return true;
            }

            Log("Login failed.");
            return false;
        }

        /// <summary>
        /// 명시적으로 지정한 HostId와 재접속 토큰 정보를 설정한 뒤 세션 복구를 시도합니다.
        /// </summary>
        public async Task<bool> ReconnectAsync(int hostId, string reconnectToken)
        {
            HostId = hostId;
            ReconnectToken = reconnectToken;
            return await ReconnectAsync();
        }

        /// <summary>
        /// 내부 저장된 기존의 재접속 토큰과 HostId를 서버에 전송하여 끊어지기 전의 기존 세션을 안전하게 복구합니다.
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
        /// 서버의 특정 Zone(지역/채널)으로 캐릭터 입장을 요청합니다.
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
        /// 현재 머물고 있는 Zone에서 나와 기본 상태(Zone ID = 0)로 돌아갑니다.
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
        /// 문자열 형태의 룸 번호(RoomId)를 정수로 파싱한 뒤 해당 방에 들어갑니다.
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
        /// 특정 룸 번호에 대한 입장을 서버에 전달합니다. (P2P 홀펀칭 작업을 트리거합니다.)
        /// </summary>
        public async Task<bool> EnterRoomAsync(int roomId)
        {
            if (!IsConnected) return false;

            var joinData = new GroupJoinData { GroupId = roomId, JoinerHostId = HostId };
            await SendJsonAsync(ProtocolSelect.JoinGroupProtocol, joinData);
            CurrentRoomId = roomId.ToString();
            Log($"Sent request to enter room {roomId}");
            return true;
        }

        /// <summary>
        /// 현재 머물고 있던 P2P 룸을 나와 로컬 룸 변수를 초기화합니다.
        /// </summary>
        public async Task<bool> LeaveRoomAsync()
        {
            if (string.IsNullOrEmpty(CurrentRoomId)) return false;
            CurrentRoomId = null;
            Log("Left the room locally");
            return await Task.FromResult(true);
        }

        /// <summary>
        /// 서버에 특정 프로토콜 데이터를 보내고, 5초 타임아웃 제한 내에 들어오는 응답 메시지를 동기식처럼 대기하여 반환받습니다.
        /// </summary>
        public async Task<T?> RequestAsync<T>(ProtocolSelect protocol, object data) where T : class
        {
            var tcs = new TaskCompletionSource<T?>();

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
        /// 지정한 프로토콜 형식을 기반으로 객체를 JSON 시리얼라이즈하여 서버로 비동기 전송합니다.
        /// 로그인이 된 상태라면 패킷 검증용 HMAC 서명을 덧붙여 전송합니다.
        /// </summary>
        public async Task SendJsonAsync(ProtocolSelect protocol, object data)
        {
            if (!IsConnected) return;

            string json = JsonSerializer.Serialize(data);
            byte[] body = Encoding.UTF8.GetBytes(json);

            byte[] seqBytes = BitConverter.GetBytes(System.Threading.Interlocked.Increment(ref _sequenceNumber));

            if (!string.IsNullOrEmpty(_jwtToken))
            {
                byte[] dataToSign = new byte[6 + body.Length];
                dataToSign[0] = (byte)SendType.Json;
                dataToSign[1] = (byte)protocol;
                Buffer.BlockCopy(seqBytes, 0, dataToSign, 2, 4);
                Buffer.BlockCopy(body, 0, dataToSign, 6, body.Length);

                using (var hmac = new System.Security.Cryptography.HMACSHA256(_hmacKey))
                {
                    byte[] computedHmac = hmac.ComputeHash(dataToSign);

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
                byte[] packet = new byte[body.Length + 6];
                packet[0] = (byte)SendType.Json;
                packet[1] = (byte)protocol;
                Buffer.BlockCopy(seqBytes, 0, packet, 2, 4);
                Buffer.BlockCopy(body, 0, packet, 6, body.Length);

                await _socket!.SendAsync(packet, SocketFlags.None);
            }
        }

        /// <summary>
        /// 지정된 RPC 메서드를 호출하고 서버로부터 결과를 리턴 대기합니다.
        /// </summary>
        public async Task<T?> InvokeRpcAsync<T>(string methodName, object? parameters = null) where T : class
        {
            string paramJson = parameters != null ? JsonSerializer.Serialize(parameters) : "{}";
            var rpcReq = new RpcRequest { MethodName = methodName, Params = paramJson };
            return await RequestAsync<T>(ProtocolSelect.RpcProtocol, rpcReq);
        }

        /// <summary>
        /// 서버의 RPC 메서드를 비동기로 즉시 호출합니다. (반환 결과를 기다리지 않습니다.)
        /// </summary>
        public async Task InvokeRpcAsync(string methodName, object? parameters = null)
        {
            string paramJson = parameters != null ? JsonSerializer.Serialize(parameters) : "{}";
            var rpcReq = new RpcRequest { MethodName = methodName, Params = paramJson };
            await SendJsonAsync(ProtocolSelect.RpcProtocol, rpcReq);
        }

        /// <summary>
        /// 인증에 필요한 JWT 토큰 바이트와 본문 데이터를 합치고, HMAC 보안 서명을 덧붙인 다이렉트(바이너리) 데이터를 송신합니다.
        /// </summary>
        public async Task SendAuthenticatedDirectAsync(ProtocolSelect protocol, byte[] data)
        {
            if (!IsConnected || string.IsNullOrEmpty(_jwtToken)) return;

            byte[] tokenBytes = Encoding.UTF8.GetBytes(_jwtToken);
            byte[] tokenLenBytes = BitConverter.GetBytes(tokenBytes.Length);

            byte[] seqBytes = BitConverter.GetBytes(System.Threading.Interlocked.Increment(ref _sequenceNumber));

            int payloadLen = 4 + tokenBytes.Length + data.Length;
            byte[] payload = new byte[payloadLen];
            Buffer.BlockCopy(tokenLenBytes, 0, payload, 0, 4);
            Buffer.BlockCopy(tokenBytes, 0, payload, 4, tokenBytes.Length);
            Buffer.BlockCopy(data, 0, payload, 4 + tokenBytes.Length, data.Length);

            byte[] dataToSign = new byte[6 + payloadLen];
            dataToSign[0] = (byte)SendType.Direct;
            dataToSign[1] = (byte)protocol;
            Buffer.BlockCopy(seqBytes, 0, dataToSign, 2, 4);
            Buffer.BlockCopy(payload, 0, dataToSign, 6, payloadLen);

            using (var hmac = new System.Security.Cryptography.HMACSHA256(_hmacKey))
            {
                byte[] computedHmac = hmac.ComputeHash(dataToSign);

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

        /// <summary>
        /// 특정 프로토콜 타입에 대해 응답 발생 시 작동할 이벤트 핸들러를 동적 등록합니다.
        /// </summary>
        public void RegisterHandler(ProtocolSelect protocol, Action<byte[]> handler)
        {
            _handlers[protocol] = handler;
        }

        /// <summary>
        /// 특정 프로토콜에 대해 등록된 핸들러를 해제합니다.
        /// </summary>
        public void UnregisterHandler(ProtocolSelect protocol)
        {
            _handlers.TryRemove(protocol, out _);
        }

        /// <summary>
        /// 다중 스레드 레이스 컨디션을 방지하며 안전하게 소켓 연결 끊김 신호 및 이벤트를 전파합니다.
        /// </summary>
        private void HandleDisconnect()
        {
            if (System.Threading.Interlocked.CompareExchange(ref _isDisconnectInvoked, 1, 0) == 0)
            {
                _isConnected = false;
                try
                {
                    OnDisconnected?.Invoke();
                }
                catch (Exception ex)
                {
                    Log($"Error invoking OnDisconnected event: {ex.Message}");
                }
                Log("Disconnected from server.");
            }
        }

        /// <summary>
        /// 백그라운드 태스크에서 가동되며 소켓을 통해 들어오는 원시 데이터를 계속 수신하는 반복문 루프입니다.
        /// </summary>
        private async Task ReceiveLoop()
        {
            byte[] buffer = new byte[8192];
            try
            {
                while (_isConnected && (_socket?.Connected ?? false))
                {
                    int received = await _socket.ReceiveAsync(buffer, SocketFlags.None);
                    if (received == 0) break;

                    ProcessPacket(buffer, received);
                }
            }
            catch (Exception ex)
            {
                if (_isConnected)
                {
                    Log($"Receive loop error: {ex.Message}");
                }
            }
            finally
            {
                HandleDisconnect();
            }
        }

        /// <summary>
        /// 수신한 패킷의 헤더(SendType, Protocol 등)를 검사하여 적절한 이벤트 핸들러나 내부 비즈니스 로직 라우터로 분배합니다.
        /// </summary>
        private void ProcessPacket(byte[] buffer, int length)
        {
            if (length < 6) return;

            var sendType = (SendType)buffer[0];
            var protocol = (ProtocolSelect)buffer[1];

            byte[] body = new byte[length - 6];
            Buffer.BlockCopy(buffer, 6, body, 0, length - 6);

            if (sendType == SendType.Json)
            {
                string json = Encoding.UTF8.GetString(body);
                if (_handlers.TryGetValue(protocol, out var handler))
                {
                    handler.Invoke(body);
                }
                else
                {
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

        /// <summary>
        /// 내부 로그를 OnLog 이벤트를 구독하고 있는 외부 객체에 양식화하여 전파합니다.
        /// </summary>
        private void Log(string message)
        {
            OnLog?.Invoke($"[TeruClient] {message}");
        }

        /// <summary>
        /// 소켓을 파괴하고, P2P 매니저 및 자원들을 정리하며 리소스를 메모리 해제합니다.
        /// </summary>
        public void Dispose()
        {
            _isConnected = false;
            try
            {
                _socket?.Close();
                _socket?.Dispose();
            }
            catch (Exception ex)
            {
                Log($"Error disposing socket: {ex.Message}");
            }
            _p2pManager.Dispose();
            HandleDisconnect();
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
