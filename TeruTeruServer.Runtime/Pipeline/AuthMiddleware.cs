using TeruTeruServer.SDK.Interfaces;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using TeruTeruServer.SDK.Protocol;
using TeruTeruServer.SDK.Enums;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.Runtime.Pipeline
{
    public class AuthMiddleware : IPacketMiddleware
    {
        private const string SecretKey = "TeruTeruServer_Super_Secret_Key_2026";
        private readonly ISessionManager _sessionManager;
        private readonly ISessionStore _sessionStore;

        public AuthMiddleware(ISessionManager sessionManager, ISessionStore sessionStore)
        {
            _sessionManager = sessionManager;
            _sessionStore = sessionStore;
        }

        public async Task InvokeAsync(PacketContext context, Func<Task> next)
        {
            var buffer = context.RawData;
            if (buffer.Length < 2) return;

            var sendType = (SendType)buffer[0];
            var protocolType = buffer[1];

            // 1. SendType.Json은 세션 인증 상태만 확인하고 헤더에 토큰이 없으므로 다음으로 넘김
            if (sendType == SendType.Json)
            {
                if (protocolType == (byte)ProtocolSelect.ReconnectProtocol)
                {
                    HandleReconnect(context);
                    return;
                }
                await next();
                return;
            }

            // 2. 패킷 헤더에서 토큰 추출 시도 (Direct 패킷 전용)
            // 구조: [SendType(1)][ProtocolType(1)][SequenceNumber(4)][TokenLength(4)][Token(N)][Data(M)]
            try
            {
                if (buffer.Length < 10) 
                {
                    await next();
                    return;
                }

                int tokenLength = BitConverter.ToInt32(buffer, 6);
                if (tokenLength > 0 && buffer.Length >= 10 + tokenLength)
                {
                    string token = Encoding.UTF8.GetString(buffer, 10, tokenLength);
                    ValidateToken(token);

                    if (context.Session != null)
                    {
                        context.Session.IsAuthenticated = true;
                    }

                    // 검증 성공 시, 실제 데이터만 남기도록 RawData 재설정
                    byte[] actualData = new byte[buffer.Length - (10 + tokenLength) + 6];
                    actualData[0] = buffer[0]; // SendType
                    actualData[1] = buffer[1]; // ProtocolType
                    // Sequence Number 복사
                    Array.Copy(buffer, 2, actualData, 2, 4);
                    // Payload 복사
                    Array.Copy(buffer, 10 + tokenLength, actualData, 6, buffer.Length - (10 + tokenLength));
                    context.RawData = actualData;
                }
            }
            catch (Exception ex)
            {
                TeruTeruLogger.LogWarning($"[Auth Parse Error] {ex.Message} - Remote: {context.ClientSocket.RemoteEndPoint}");
            }

            await next();
        }

        private void ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(SecretKey);
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);
        }

        private void HandleReconnect(PacketContext context)
        {
            try
            {
                var buffer = context.RawData;
                string json = buffer.ExtractJsonPayload();
                var request = System.Text.Json.JsonSerializer.Deserialize<ReconnectRequest>(json);
                ClientSession? session = null;

                if (request != null)
                {
                    // 1. 로컬에서 먼저 검색
                    if (!_sessionManager.Players.TryGetValue(request.HostID, out session))
                    {
                        // 2. 실패 시 전체 저장소(ISessionStore)에서 ReconnectToken으로 검색 (분산 환경 대비)
                        session = _sessionStore.FindByReconnectToken(request.ReconnectToken);
                    }
                }

                if (session != null)
                {
                    if (session.ReconnectToken == request!.ReconnectToken && session.State == SessionState.Grace)
                    {
                        // 소켓 덮어씌우기 및 상태 복원
                        session.ClientSocket = context.ClientSocket;
                        session.State = SessionState.Connected;
                        session.UpdateLastSeen();

                        TeruTeruLogger.LogInfo($"플레이어 {request.HostID} 재접속 성공.");

                        var response = new ReconnectResponse { Success = true, Message = "Reconnected" };
                        SendJsonResponse(context.ClientSocket, ProtocolSelect.ReconnectProtocol, response);
                    }
                    else
                    {
                        TeruTeruLogger.LogWarning($"플레이어 {request.HostID} 재접속 실패: 유효하지 않은 토큰이거나 Grace 상태가 아님.");
                        context.ClientSocket.Close();
                    }
                }
                else
                {
                    TeruTeruLogger.LogWarning("재접속 실패: 해당 세션을 찾을 수 없음.");
                    context.ClientSocket.Close();
                }
            }
            catch (Exception ex)
            {
                TeruTeruLogger.LogError($"재접속 처리 중 에러 발생: {ex.Message}");
                context.ClientSocket.Close();
            }
        }

        private void SendJsonResponse<T>(System.Net.Sockets.Socket socket, ProtocolSelect protocol, T data)
        {
            try
            {
                string json = System.Text.Json.JsonSerializer.Serialize(data);
                byte[] body = Encoding.UTF8.GetBytes(json);
                byte[] packet = new byte[body.Length + 6];
                packet[0] = (byte)SendType.Json;
                packet[1] = (byte)protocol;
                Array.Copy(body, 0, packet, 6, body.Length);
                socket.Send(packet);
            }
            catch { }
        }
    }
}
