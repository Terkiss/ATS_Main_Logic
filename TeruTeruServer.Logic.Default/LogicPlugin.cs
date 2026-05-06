using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using TeruTeruServer.SDK.Enums;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Protocol;
using TeruTeruServer.SDK.Util;
using TeruTeruServer.SDK.Attributes;

namespace TeruTeruServer.Logic.Default
{
    public class LogicPlugin : ILogicService
    {
        private readonly IMessageSender _messageSender;
        private readonly IDatabaseService _dbService;
        private readonly ISessionManager _sessionManager;
        private readonly IProtocolRouter _router;
        private readonly RpcProxy _rpcProxy;
        private readonly TeruTeruServer.Logic.Default.P2P.P2PSignalingHandler _p2pSignalingHandler;
        private readonly TeruTeruServer.Logic.Default.P2P.P2PRelayHandler _p2pRelayHandler;
        private readonly TeruTeruServer.Logic.Default.P2P.P2PGroupHandler _p2pGroupHandler;

        private const string SecretKey = "TeruTeruServer_Super_Secret_Key_2026";

        public LogicPlugin(IMessageSender messageSender, IDatabaseService dbService, ISessionManager sessionManager, IProtocolRouter router)
        {
            _messageSender = messageSender;
            _dbService = dbService;
            _sessionManager = sessionManager;
            _router = router;
            _rpcProxy = new RpcProxy(_messageSender, _sessionManager);

            // 중요: 라우터에 자기 자신을 등록하여 어트리뷰트 분석 활성화
            _router.Initialize(this);

            _p2pSignalingHandler = new TeruTeruServer.Logic.Default.P2P.P2PSignalingHandler(_sessionManager);
            _p2pRelayHandler = new TeruTeruServer.Logic.Default.P2P.P2PRelayHandler(_sessionManager);
            _p2pGroupHandler = new TeruTeruServer.Logic.Default.P2P.P2PGroupHandler(_sessionManager);
        }

        public void ProcessDirectProtocol(byte[] buffer, Socket socket)
        {
            if (buffer == null || buffer.Length < 2) return;

            var sendType = (SendType)buffer[0];
            var protocolType = (ProtocolSelect)buffer[1];

            if (sendType == SendType.Json)
            {
                string json = buffer.ExtractJsonPayload();
                HandleJsonProtocol(json, protocolType, socket);
            }
            else if (sendType == SendType.Direct)
            {
                if (protocolType == ProtocolSelect.ImageDumpCommand)
                {
                    ProcessImageData(buffer, socket);
                }
                else if (protocolType == ProtocolSelect.P2PRelayProtocol)
                {
                    _p2pRelayHandler.HandleRelayData(buffer);
                }
                else if (protocolType == ProtocolSelect.GroupRelayProtocol)
                {
                    _p2pGroupHandler.HandleGroupRelay(buffer);
                }
            }
        }

        public async void ProcessJsonProtocol(string json, ProtocolSelect protocol, Socket socket)
        {
            try
            {
                // [플러그인 내부 라우팅] 엔진의 개입 없이 라우터가 어트리뷰트를 보고 분기 처리
                string resultJson = await _router.RouteAsync(json, protocol, socket);

                // 결과가 있는 경우 클라이언트에게 응답 (필요 시)
                // Tip: RPC 응답은 라우터 내부에서 처리하거나 여기서 공통 처리 가능
            }
            catch (Exception ex)
            {
                TeruTeruLogger.LogError($"ProcessJsonProtocol 예외 발생: {ex.Message}");
            }
        }

        private void HandleJsonProtocol(string json, ProtocolSelect protocol, Socket socket)
        {
            try
            {
                switch (protocol)
                {
                    case ProtocolSelect.ConnectProtocol:
                        if (json != null) ConProtocol(socket, JsonSerializer.Deserialize<ConnectProtocol>(json)!);
                        break;
                    case ProtocolSelect.LoginProtocol:
                        if (json != null) HandleLogin(socket, JsonSerializer.Deserialize<LoginProtocol>(json)!);
                        break;
                    case ProtocolSelect.UdpRegisterProtocol:
                        _p2pSignalingHandler.HandleUdpRegister(PacketUtility.CreateBufferWithDummyHeader(json), socket);
                        break;
                    case ProtocolSelect.HolePunchRequest:
                        if (_sessionManager.TryGetHostIdBySocket(socket, out int requesterHostID))
                        {
                            _p2pSignalingHandler.HandleHolePunchRequest(PacketUtility.CreateBufferWithDummyHeader(json), requesterHostID);
                        }
                        break;
                    case ProtocolSelect.JoinGroupProtocol:
                        _p2pGroupHandler.HandleJoinGroup(PacketUtility.CreateBufferWithDummyHeader(json));
                        break;
                }
            }
            catch (JsonException ex)
            {
                TeruTeruLogger.LogError($"[Json Parse Error] Protocol: {protocol}, Msg: {ex.Message}");
            }
            catch (Exception ex)
            {
                TeruTeruLogger.LogError($"[HandleJsonProtocol Error] Protocol: {protocol}, Msg: {ex.Message}");
            }
        }

        [Protocol(ProtocolSelect.LoginProtocol)]
        public void HandleLogin(Socket socket, LoginProtocol loginData)
        {
            string token = GenerateJwtToken(loginData.UserId);
            loginData.IsSuccess = true;
            loginData.AuthToken = token;
            _rpcProxy.SendJsonResponse(socket, ProtocolSelect.LoginProtocol, loginData);
        }

        [Protocol(ProtocolSelect.ConnectProtocol)]
        public void ConProtocol(Socket socket, ConnectProtocol protocol)
        {
            protocol.IsSuccess = true;
            _rpcProxy.SendJsonResponse(socket, ProtocolSelect.ConnectProtocol, protocol);
        }

        // --- [자동 연결 (RPC 방식)] ---

        [Rpc("Echo")]
        public async Task<string> HandleEcho(Socket socket, string message)
        {
            TeruTeruLogger.LogInfo($"RPC Echo called with: {message}");
            return $"Server Echo: {message} at {DateTime.Now}";
        }

        [Rpc("GetServerInfo")]
        public async Task<object> GetServerInfo(Socket socket)
        {
            return new
            {
                ServerName = "TeruTeru Server AI Engine",
                Version = "2.0.0-phase3-plugin-routing",
                CurrentTime = DateTime.Now,
                ActiveSessions = _sessionManager.Players.Count
            };
        }

        // --- [기타 로직] ---

        private void ProcessImageData(byte[] buffer, Socket socket)
        {
            byte[] imageData = buffer.ExtractPayload();

            var sendImgData = new SendImageData
            {
                Guid = Guid.NewGuid().ToString(),
                Data = imageData,
                ImgSize = imageData.Length,
                Timestamp = DateTime.UtcNow
            };
            _rpcProxy.RequestObjectDetect(sendImgData);
        }

        private string GenerateJwtToken(string userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(SecretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim("id", userId) }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
