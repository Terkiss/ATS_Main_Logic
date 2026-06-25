namespace TeruTeruServer.Logic.Default.Services
{
    public interface ITokenProvider
    {
        string GenerateJwtToken(string userId, out string refreshToken);
        bool ValidateRefreshToken(string token, out string userId);
    }
}
