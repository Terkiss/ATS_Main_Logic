using System;
using System.Net;
using System.Net.Sockets;
using TeruTeruServer.SDK.Enums;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Protocol;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.Logic.Default.P2P
{
    public class UdpRegisterData
    {
        public int HostID { get; set; }
    }

    public class HolePunchRequestData
    {
        public int TargetHostID { get; set; }
    }

    public class PeerEndpointInfo
    {
        public int PeerHostID { get; set; }
        public string IP { get; set; } = string.Empty;
        public int Port { get; set; }
    }

    public class P2PSignalingHandler
    {
        private readonly ISessionManager _sessionManager;

        public P2PSignalingHandler(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public void HandleUdpRegister(byte[] rawData, Socket clientSocket)
        {
            try
            {
                var buffer = rawData;
                string json = buffer.ExtractJsonPayload();
                var data = System.Text.Json.JsonSerializer.Deserialize<UdpRegisterData>(json);

                if (data != null && _sessionManager.Players.TryGetValue(data.HostID, out var session))
                {
                    // Save the public EndPoint the server sees from the UDP packet
                    session.UdpEndPoint = clientSocket.RemoteEndPoint;
                    TeruTeruLogger.LogInfo($"[UDP] HostID {data.HostID} Registered EndPoint: {session.UdpEndPoint}");
                }
            }
            catch (Exception ex)
            {
                TeruTeruLogger.LogError($"UDP 등록 처리 중 에러: {ex.Message}");
            }
        }

        public void HandleHolePunchRequest(byte[] rawData, int requesterHostID)
        {
            try
            {
                var buffer = rawData;
                string json = buffer.ExtractJsonPayload();
                var data = System.Text.Json.JsonSerializer.Deserialize<HolePunchRequestData>(json);
                if (data != null)
                {
                    HandleHolePunchRequest(data, requesterHostID);
                }
            }
            catch (Exception ex)
            {
                TeruTeruLogger.LogError($"HolePunchRequest 에러: {ex.Message}");
            }
        }

        public void HandleHolePunchRequest(HolePunchRequestData data, int requesterHostID)
        {
            try
            {
                if (data != null)
                {
                    if (_sessionManager.Players.TryGetValue(requesterHostID, out var requesterSession) &&
                        _sessionManager.Players.TryGetValue(data.TargetHostID, out var targetSession))
                    {
                        if (requesterSession.UdpEndPoint == null || targetSession.UdpEndPoint == null)
                        {
                            TeruTeruLogger.LogWarning($"HolePunchRequest 실패: UDP 엔드포인트 누락 (Requester: {requesterSession.UdpEndPoint}, Target: {targetSession.UdpEndPoint})");
                            return;
                        }

                        if (requesterSession.ClientSocket != null && targetSession.ClientSocket != null)
                        {
                            // Send target's EndPoint to requester
                            if (targetSession.UdpEndPoint is IPEndPoint targetIP)
                            {
                                var targetInfo = new PeerEndpointInfo { PeerHostID = data.TargetHostID, IP = targetIP.Address.ToString(), Port = targetIP.Port };
                                requesterSession.ClientSocket.SendJsonResponse(ProtocolSelect.HolePunchRequest, targetInfo);
                            }

                            // Send requester's EndPoint to target
                            if (requesterSession.UdpEndPoint is IPEndPoint reqIP)
                            {
                                var reqInfo = new PeerEndpointInfo { PeerHostID = requesterHostID, IP = reqIP.Address.ToString(), Port = reqIP.Port };
                                targetSession.ClientSocket.SendJsonResponse(ProtocolSelect.HolePunchRequest, reqInfo);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TeruTeruLogger.LogError($"HolePunchRequest 처리 중 에러: {ex.Message}");
            }
        }


    }
}
