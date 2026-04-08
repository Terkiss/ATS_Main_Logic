using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using TeruTeruServer.Common.Protocol;
using TeruTeruServer.Common.Enums;
using TeruTeruServer.ManageLogic.Util;

namespace TeruTeruServer.Pipeline
{
    public class AuthMiddleware : IPacketMiddleware
    {
        private const string SecretKey = "TeruTeruServer_Super_Secret_Key_2026"; 

        public async Task InvokeAsync(PacketContext context, Func<Task> next)
        {
            var buffer = context.RawData;
            if (buffer.Length < 2) return;

            var sendType = (SendType)buffer[0];
            var protocolType = buffer[1];

            // 1. 인증 면제 프로토콜 체크 (연결 및 로그인)
            if (sendType == SendType.Json && (protocolType == (byte)ProtocolSelect.ConnectProtocol || protocolType == (byte)ProtocolSelect.LoginProtocol))
            {
                await next();
                return;
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
    }
}
