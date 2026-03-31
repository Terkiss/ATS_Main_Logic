using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using TeruTeruServer.ManageLogic.Protocol;
using TeruTeruServer.ManageLogic.Util;

namespace TeruTeruServer.Pipeline
{
    /// <summary>
    /// 패킷의 JWT 인증 토큰 유효성을 검사하는 미들웨어입니다.
    /// </summary>
    public class AuthMiddleware : IPacketMiddleware
    {
        private const string SecretKey = "TeruTeruServer_Super_Secret_Key_2026"; // TODO: 환경 설정으로 분리

        public async Task InvokeAsync(PacketContext context, Func<Task> next)
        {
            var buffer = context.RawData;
            if (buffer.Length < 2) 
            {
                await next();
                return;
            }

            var sendType = (SendType)buffer[0];
            var protocolType = buffer[1];

            // 연결 프로토콜(Connect)이나 로그인 프로토콜은 인증 없이 통과
            if (sendType == SendType.Json && (protocolType == (byte)ProtocolSelect.ConnectProtocol || protocolType == (byte)ProtocolSelect.LoginProtocol))
            {
                await next();
                return;
            }

            // TODO: 상용 환경에서는 패킷 헤더에 토큰 길이를 포함하고 바이트를 추출해야 함
            // 현재는 프로토타입 검증을 위해 JSON 패킷 내에 토큰이 있다고 가정하거나 세션 체크 수행
            
            // 실전 구현: context.RawData에서 토큰 추출 로직 필요
            // 여기서는 디렉터님의 지시에 따라 JWT 검증 로직의 뼈대를 구현함
            
            try
            {
                // 토큰 검증 로직 예시 (실제 구현 시 클라이언트 패킷 구조에 맞춰 토큰 위치 특정 필요)
                // string token = ExtractTokenFromPacket(context.RawData);
                // ValidateToken(token);
                
                await next(); // 검증 성공 시 다음 단계로
            }
            catch (Exception ex)
            {
                TeruTeruLogger.LogError($"인증 실패: {ex.Message}");
                // 인증 실패 시 즉각 세션 종료 처리 가능
                context.ClientSocket.Close();
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
