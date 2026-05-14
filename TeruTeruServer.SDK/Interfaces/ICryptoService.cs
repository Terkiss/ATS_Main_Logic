namespace TeruTeruServer.SDK.Interfaces
{
    /// <summary>
    /// 플러그인 생태계와 미들웨어에서 공통으로 사용할 암호화 인터페이스입니다.
    /// Seed, AES 등 구체적인 암호화 로직을 추상화합니다.
    /// </summary>
    public interface ICryptoService
    {
        string Encrypt(string plainText, string password);
        string Decrypt(string encryptedText, string password);
    }
}
