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

        public AuthMiddleware(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public async Task InvokeAsync(PacketContext context, Func<Task> next)
        {
            var buffer = context.RawData;
            if (buffer.Length < 2) return;

            var sendType = (SendType)buffer[0];
            var protocolType = buffer[1];

            // 1. 인증 면제 프로토콜 체크 (연결 및 로그인)
            if (sendType == SendType.Json)
            {
                if (protocolType == (byte)ProtocolSelect.ConnectProtocol || protocolType == (byte)ProtocolSelect.LoginProtocol)
                {
                    await next();
                    return;
                }
                else if (protocolType == (byte)ProtocolSelect.ReconnectProtocol)
                {
                    // 재접속 로직 처리
                    HandleReconnect(context);
                    return; // 재접속은 인증 파이프라인의 다음 단계로 넘기지 않음
                }
            }

            // 2. 패킷 헤더에서 토큰 추출 시도
            // 구조: [SendType(1)][ProtocolType(1)][TokenLength(4)][Token(N)][Data(M)]
            try
            {
                if (buffer.Length < 6) throw new Exception("Packet too short for auth header.");

                int tokenLength = BitConverter.ToInt32(buffer, 2);
                if (tokenLength > 0)
                {
                    if (buffer.Length < 6 + tokenLength) throw new Exception("Invalid token length in header.");
                    
                    string token = Encoding.UTF8.GetString(buffer, 6, tokenLength);
                    ValidateToken(token);

                    // 검증 성공 시, 실제 데이터만 남기도록 RawData 재설정 (다음 미들웨어 편의성)
                    byte[] actualData = new byte[buffer.Length - (6 + tokenLength) + 2];
                    actualData[0] = buffer[0]; // SendType
                    actualData[1] = buffer[1]; // ProtocolType
                    Array.Copy(buffer, 6 + tokenLength, actualData, 2, buffer.Length - (6 + tokenLength));
                    context.RawData = actualData;

                    await next();
                }
                else
                {
                    throw new Exception("Auth token missing.");
                }
            }
            catch (Exception ex)
            {
                TeruTeruLogger.LogError($"[Auth Failed] {ex.Message} - Remote: {context.ClientSocket.RemoteEndPoint}");
                context.ClientSocket.Close(); // 즉각 차단
                context.IsProcessed = true;
            }
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
                string json = Encoding.UTF8.GetString(buffer, 2, buffer.Length - 2);
                var request = System.Text.Json.JsonSerializer.Deserialize<ReconnectRequest>(json);

                if (request != null && _sessionManager.Players.TryGetValue(request.HostID, out var session))
                {
                    if (session.ReconnectToken == request.ReconnectToken && session.State == SessionState.Grace)
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
                byte[] packet = new byte[body.Length + 2];
                packet[0] = (byte)SendType.Json;
                packet[1] = (byte)protocol;
                Array.Copy(body, 0, packet, 2, body.Length);
                socket.Send(packet);
            }
            catch { }
        }
    }
}
