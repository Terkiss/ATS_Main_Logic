using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace TeruTeruServer.Logic.Default.Services
{
    public class JwtTokenProvider : ITokenProvider
    {
        private const string SecretKey = "TeruTeruServer_Super_Secret_Key_2026";
        private readonly byte[] _keyBytes;

        public JwtTokenProvider()
        {
            _keyBytes = Encoding.ASCII.GetBytes(SecretKey);
        }

        public string GenerateJwtToken(string userId, out string refreshToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            // Access Token (2 hours)
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] 
                { 
                    new Claim("id", userId), 
                    new Claim("type", "access") 
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(_keyBytes), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);

            // Refresh Token (7 days)
            var refreshDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] 
                { 
                    new Claim("id", userId), 
                    new Claim("type", "refresh") 
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(_keyBytes), SecurityAlgorithms.HmacSha256Signature)
            };
            var rToken = tokenHandler.CreateToken(refreshDescriptor);
            refreshToken = tokenHandler.WriteToken(rToken);

            return tokenHandler.WriteToken(token);
        }

        public bool ValidateRefreshToken(string token, out string userId)
        {
            userId = string.Empty;
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(_keyBytes),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var typeClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "type");

                if (typeClaim != null && typeClaim.Value == "refresh")
                {
                    userId = jwtToken.Claims.First(x => x.Type == "id").Value;
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
