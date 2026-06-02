using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using TeruTeruServer.SDK.Enums;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Models;
using TeruTeruServer.SDK.Protocol;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.Logic.Default.P2P
{
    public class GroupJoinData
    {
        public int GroupId { get; set; }
        public int JoinerHostId { get; set; }
    }

    public class P2PGroupHandler
    {
        private readonly ISessionManager _sessionManager;
        private readonly IEventBus _eventBus;
        private readonly ConcurrentDictionary<int, P2PGroup> _groups = new ConcurrentDictionary<int, P2PGroup>();
        private readonly RelayQoSController _qosController = new RelayQoSController();

        public P2PGroupHandler(ISessionManager sessionManager, IEventBus eventBus)
        {
            _sessionManager = sessionManager;
            _eventBus = eventBus;
        }

        public P2PGroup CreateGroup(int ownerHostId)
        {
            var group = P2PGroupFactory.Create(ownerHostId);
            _groups.TryAdd(group.GroupId, group);
            TeruTeruLogger.LogInfo($"그룹 생성됨: GroupID {group.GroupId}, 방장: {ownerHostId}");
            return group;
        }

        public void HandleJoinGroup(byte[] rawData)
        {
            try
            {
                var buffer = rawData;
                string json = buffer.ExtractJsonPayload();
                var data = System.Text.Json.JsonSerializer.Deserialize<GroupJoinData>(json);
                if (data != null)
                {
                    HandleJoinGroup(data);
                }
            }
            catch (Exception ex)
            {
                TeruTeruLogger.LogError($"JoinGroup 처리 중 에러: {ex.Message}");
            }
        }

        public void HandleJoinGroup(GroupJoinData data)
        {
            try
            {
                if (data != null)
                {
                    if (!_groups.TryGetValue(data.GroupId, out var group))
                    {
                        group = P2PGroupFactory.Create(data.JoinerHostId, data.GroupId);
                        _groups[data.GroupId] = group;
                        TeruTeruLogger.LogInfo($"그룹 자동 생성됨 (요청): GroupID {data.GroupId}, 방장: {data.JoinerHostId}");
                    }

                    // 새로 들어온 유저
                    int joiner = data.JoinerHostId;

                    // 기존 멤버들에게 시그널링 (HolePunchRequest 트리거)
                    foreach (var memberId in group.GetMemberIds())
                    {
                        if (memberId != joiner)
                        {
                            // Trigger signaling for joiner <-> memberId
                            if (_sessionManager.Players.TryGetValue(memberId, out var memberSession) &&
                                _sessionManager.Players.TryGetValue(joiner, out var joinerSession))
                            {
                                if (memberSession.ClientSocket != null && joinerSession.ClientSocket != null)
                                {
                                    if (joinerSession.UdpEndPoint is System.Net.IPEndPoint reqIP)
                                    {
                                        var reqInfo = new PeerEndpointInfo { PeerHostID = joiner, IP = reqIP.Address.ToString(), Port = reqIP.Port };
                                        memberSession.ClientSocket.SendJsonResponse(ProtocolSelect.HolePunchRequest, reqInfo);
                                    }

                                    if (memberSession.UdpEndPoint is System.Net.IPEndPoint targetIP)
                                    {
                                        var targetInfo = new PeerEndpointInfo { PeerHostID = memberId, IP = targetIP.Address.ToString(), Port = targetIP.Port };
                                        joinerSession.ClientSocket.SendJsonResponse(ProtocolSelect.HolePunchRequest, targetInfo);
                                    }
                                }
                            }
                        }
                    }

                    group.AddMember(joiner);
                    if (_sessionManager.Players.TryGetValue(joiner, out var jSession))
                    {
                        jSession.P2PState = P2PStatus.Signaling;
                        group.UpdateMemberStatus(joiner, P2PStatus.Signaling);
                    }
                    
                    _eventBus.Publish("p2p.group.join", new { GroupId = group.GroupId, HostId = joiner });
                    TeruTeruLogger.LogInfo($"플레이어 {joiner}가 그룹 {group.GroupId}에 입장했습니다.");
                }
            }
            catch (Exception ex)
            {
                TeruTeruLogger.LogError($"JoinGroup 처리 중 에러: {ex.Message}");
            }
        }

        public void HandleGroupRelay(byte[] rawData)
        {
            try
            {
                var buffer = rawData;
                if (buffer.Length < 6) return;

                int targetCount = BitConverter.ToInt32(buffer, 6);
                if (buffer.Length < 10 + (targetCount * 4)) return;

                int dataOffset = 10 + (targetCount * 4);
                byte[] relayData = new byte[buffer.Length - dataOffset + 6];
                relayData[0] = (byte)SendType.Direct;
                relayData[1] = (byte)ProtocolSelect.P2PRelayProtocol;
                // SequenceNumber (2-5) remains 0
                Array.Copy(buffer, dataOffset, relayData, 6, buffer.Length - dataOffset);

                for (int i = 0; i < targetCount; i++)
                {
                    int targetId = BitConverter.ToInt32(buffer, 10 + (i * 4));
                    
                    // QoS Check
                    if (!_qosController.CheckAllow(targetId, relayData.Length))
                    {
                        TeruTeruLogger.LogWarning($"Relay QoS Blocked: Host {targetId} bandwidth limit exceeded.");
                        continue;
                    }

                    if (_sessionManager.Players.TryGetValue(targetId, out var session) && session.ClientSocket != null)
                    {
                        session.ClientSocket.SendAsync(new ReadOnlyMemory<byte>(relayData), SocketFlags.None);
                    }
                }
            }
            catch (Exception ex)
            {
                TeruTeruLogger.LogError($"GroupRelay 중 에러 발생: {ex.Message}");
            }
        }


    }
}
