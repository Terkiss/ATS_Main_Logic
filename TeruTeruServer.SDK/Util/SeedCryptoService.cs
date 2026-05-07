using TeruTeruServer.ManageLogic.Util;
using TeruTeruServer.SDK.Interfaces;

namespace TeruTeruServer.SDK.Util
{
    /// <summary>
    /// 기존 SeedEncrypt를 ICryptoService 인터페이스로 래핑한 클래스입니다.
    /// </summary>
    public class SeedCryptoService : ICryptoService
    {
        private readonly SeedEncrypt _seedEncrypt;

        public SeedCryptoService()
        {
            _seedEncrypt = new SeedEncrypt();
        }

        public string Encrypt(string plainText, string password)
        {
            return _seedEncrypt.EncryptString(plainText, password);
        }

        public string Decrypt(string encryptedText, string password)
        {
            return _seedEncrypt.DecryptString(encryptedText, password);
        }
    }
}
