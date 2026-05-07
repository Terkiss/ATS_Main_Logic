using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TeruTeruServer.SDK.Enums;

namespace TeruTeruServer.Client
{
    public class PeerEndpointInfo
    {
        public int PeerHostID { get; set; }
        public string IP { get; set; } = string.Empty;
        public int Port { get; set; }
    }

    /// <summary>
    /// 클라이언트 내부에서 P2P(UDP 홀펀칭 및 시그널링)를 캡슐화하는 매니저입니다.
    /// </summary>
    public class P2PManager : IDisposable
    {
        private Socket? _udpSocket;
        private readonly TeruClient _client;
        private IPEndPoint? _serverUdpEndpoint;
        private bool _isRunning;
        private Action<string>? _onLog;
        private Action<int, byte[]>? _onPeerDataReceived;
        private P2PStatus _currentStatus = P2PStatus.Signaling;
        private readonly System.Timers.Timer _pingTimer;
        private DateTime _lastPingSent;
        private readonly bool _isReliable;
        private readonly SDK.Util.ReliableUdpLayer? _reliableLayer;

        public P2PManager(TeruClient client, bool isReliable = false, Action<string>? onLog = null)
        {
            _client = client;
            _onLog = onLog;
            _isReliable = isReliable;

            if (_isReliable)
            {
                _reliableLayer = new SDK.Util.ReliableUdpLayer();
            }

            _pingTimer = new System.Timers.Timer(5000); // 5 seconds
            _pingTimer.Elapsed += (s, e) => SendPingToServer();
        }

        public void SetPeerDataHandler(Action<int, byte[]> handler)
        {
            _onPeerDataReceived = handler;
        }

        public void Start(string serverIp, int serverPort, int localPort = 0)
        {
            if (_isRunning) return;

            try
            {
                _serverUdpEndpoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
                _udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _udpSocket.Bind(new IPEndPoint(IPAddress.Any, localPort));
                _isRunning = true;

                _ = Task.Run(ReceiveLoop);
                Log($"UDP Started on port {((IPEndPoint)_udpSocket.LocalEndPoint!).Port}");

                _pingTimer.Start();

                // 서버에 UDP 등록(UdpRegisterProtocol) - 더미 패킷 전송 (STUN)
                RegisterToServer();
            }
            catch (Exception ex)
            {
                Log($"Failed to start UDP: {ex.Message}");
            }
        }

        private void RegisterToServer()
        {
            if (_udpSocket == null || _serverUdpEndpoint == null || string.IsNullOrEmpty(_client.AuthToken)) return;

            try
            {
                byte[] tokenBytes = Encoding.UTF8.GetBytes(_client.AuthToken);
                byte[] packet = new byte[tokenBytes.Length + 10]; // 1 + 1 + 4 + 4 + N
                packet[0] = (byte)SendType.Direct;
                packet[1] = (byte)ProtocolSelect.UdpRegisterProtocol;
                // SequenceNumber (2-5) = 0
                Buffer.BlockCopy(BitConverter.GetBytes(tokenBytes.Length), 0, packet, 6, 4);
                Buffer.BlockCopy(tokenBytes, 0, packet, 10, tokenBytes.Length);

                _udpSocket.SendTo(packet, _serverUdpEndpoint);
                Log("Sent UdpRegisterProtocol to Server.");
            }
            catch (Exception ex)
            {
                Log($"UdpRegister Error: {ex.Message}");
            }
        }

        private void SendPingToServer()
        {
            if (_udpSocket == null || _serverUdpEndpoint == null || !_isRunning) return;
            try
            {
                byte[] packet = new byte[6];
                packet[0] = (byte)SendType.Direct;
                packet[1] = (byte)ProtocolSelect.P2PPingProtocol;
                _lastPingSent = DateTime.UtcNow;
                _udpSocket.SendTo(packet, _serverUdpEndpoint);
            }
            catch { }
        }

        private void NotifyRelayFallback()
        {
            if (_udpSocket == null || _serverUdpEndpoint == null || _currentStatus == P2PStatus.Relay) return;
            try
            {
                _currentStatus = P2PStatus.Relay;
                byte[] packet = new byte[6];
                packet[0] = (byte)SendType.Direct;
                packet[1] = (byte)ProtocolSelect.RelayFallbackProtocol;
                _udpSocket.SendTo(packet, _serverUdpEndpoint);
                Log("Fallback to RELAY mode due to HolePunch timeout.");
            }
            catch { }
        }

        public void HandleSignaling(byte[] payload)
        {
            try
            {
                string json = Encoding.UTF8.GetString(payload);
                var peerInfo = JsonSerializer.Deserialize<PeerEndpointInfo>(json);
                if (peerInfo != null)
                {
                    Log($"HolePunchRequest received for Peer {peerInfo.PeerHostID} ({peerInfo.IP}:{peerInfo.Port})");
                    _ = Task.Run(async () => {
                        bool success = await PunchHoleWithTimeout(peerInfo.IP, peerInfo.Port, 5000);
                        if (!success) NotifyRelayFallback();
                    });
                }
            }
            catch (Exception ex)
            {
                Log($"Signaling parse error: {ex.Message}");
            }
        }

        private async Task<bool> PunchHoleWithTimeout(string ip, int port, int timeoutMs)
        {
            if (_udpSocket == null) return false;

            try
            {
                var targetEp = new IPEndPoint(IPAddress.Parse(ip), port);
                byte[] dummy = Encoding.UTF8.GetBytes("PING");
                
                // Send multiple punches
                for (int i = 0; i < 3; i++)
                {
                    _udpSocket.SendTo(dummy, targetEp);
                    await Task.Delay(200);
                }

                Log($"Punched hole to {ip}:{port}. Waiting for response...");
                
                // Simplified timeout logic: in M3 we assume if no peer data comes within timeout, we fallback.
                // In a real scenario, we'd wait for a "PONG" from the peer.
                await Task.Delay(timeoutMs);
                
                return _currentStatus == P2PStatus.Direct; 
            }
            catch (Exception ex)
            {
                Log($"PunchHole Error: {ex.Message}");
                return false;
            }
        }

        public void SendDirectToPeer(IPEndPoint target, byte[] data)
        {
            if (_udpSocket == null || !_isRunning) return;
            try
            {
                byte[] packetToSend = data;
                if (_isReliable && _reliableLayer != null)
                {
                    packetToSend = _reliableLayer.Encapsulate(data);
                }
                _udpSocket.SendTo(packetToSend, target);
            }
            catch (Exception ex)
            {
                Log($"SendDirect Error: {ex.Message}");
            }
        }

        private async Task ReceiveLoop()
        {
            byte[] buffer = new byte[8192];
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

            try
            {
                while (_isRunning && _udpSocket != null)
                {
                    var result = await _udpSocket.ReceiveFromAsync(new ArraySegment<byte>(buffer), SocketFlags.None, remoteEP);
                    if (result.ReceivedBytes >= 6)
                    {
                        byte[] data = new byte[result.ReceivedBytes];
                        Buffer.BlockCopy(buffer, 0, data, 0, result.ReceivedBytes);

                        var sendType = (SendType)data[0];
                        var protocol = (ProtocolSelect)data[1];

                        if (sendType == SendType.Direct && protocol == ProtocolSelect.P2PPingProtocol)
                        {
                            long rtt = (long)(DateTime.UtcNow - _lastPingSent).TotalMilliseconds;
                            Log($"Server RTT: {rtt}ms");
                            continue;
                        }

                        // "PING" 패킷 수신 시 Direct 모드로 간주
                        if (result.ReceivedBytes == 4 && Encoding.UTF8.GetString(data) == "PING")
                        {
                            if (_currentStatus != P2PStatus.Direct)
                            {
                                _currentStatus = P2PStatus.Direct;
                                Log("P2P Direct Connection Established.");
                            }
                            continue;
                        }

                        if (_isReliable && _reliableLayer != null)
                        {
                            var orderedPackets = _reliableLayer.ProcessIncoming(data);
                            foreach (var p in orderedPackets)
                            {
                                _onPeerDataReceived?.Invoke(0, p);
                            }
                        }
                        else
                        {
                            _onPeerDataReceived?.Invoke(0, data);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"UDP Receive Loop Error: {ex.Message}");
            }
        }

        private void Log(string message)
        {
            _onLog?.Invoke($"[P2PManager] {message}");
        }

        public void Dispose()
        {
            _isRunning = false;
            _pingTimer.Stop();
            _pingTimer.Dispose();
            _udpSocket?.Close();
            _udpSocket?.Dispose();
        }
    }
}
