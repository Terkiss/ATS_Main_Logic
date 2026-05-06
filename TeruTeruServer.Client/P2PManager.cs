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

        public P2PManager(TeruClient client, Action<string>? onLog = null)
        {
            _client = client;
            _onLog = onLog;
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
                byte[] packet = new byte[tokenBytes.Length + 6]; // 2 + 4 + N
                packet[0] = (byte)SendType.Direct;
                packet[1] = (byte)ProtocolSelect.UdpRegisterProtocol;
                Buffer.BlockCopy(BitConverter.GetBytes(tokenBytes.Length), 0, packet, 2, 4);
                Buffer.BlockCopy(tokenBytes, 0, packet, 6, tokenBytes.Length);

                _udpSocket.SendTo(packet, _serverUdpEndpoint);
                Log("Sent UdpRegisterProtocol to Server.");
            }
            catch (Exception ex)
            {
                Log($"UdpRegister Error: {ex.Message}");
            }
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
                    PunchHole(peerInfo.IP, peerInfo.Port);
                }
            }
            catch (Exception ex)
            {
                Log($"Signaling parse error: {ex.Message}");
            }
        }

        private void PunchHole(string ip, int port)
        {
            if (_udpSocket == null) return;

            try
            {
                var targetEp = new IPEndPoint(IPAddress.Parse(ip), port);
                byte[] dummy = Encoding.UTF8.GetBytes("PING");
                _udpSocket.SendTo(dummy, targetEp);
                Log($"Punched hole to {ip}:{port}");
            }
            catch (Exception ex)
            {
                Log($"PunchHole Error: {ex.Message}");
            }
        }

        public void SendDirectToPeer(IPEndPoint target, byte[] data)
        {
            if (_udpSocket == null || !_isRunning) return;
            try
            {
                _udpSocket.SendTo(data, target);
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
                    if (result.ReceivedBytes > 0)
                    {
                        byte[] data = new byte[result.ReceivedBytes];
                        Buffer.BlockCopy(buffer, 0, data, 0, result.ReceivedBytes);
                        
                        // "PING" 패킷 무시
                        if (result.ReceivedBytes == 4 && Encoding.UTF8.GetString(data) == "PING") continue;

                        // TODO: 필요시 IP/Port를 통해 PeerID를 매핑하여 OnPeerDataReceived 호출
                        _onPeerDataReceived?.Invoke(0, data); 
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
            _udpSocket?.Close();
            _udpSocket?.Dispose();
        }
    }
}
