using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TeruTeruServer.ManageLogic.Util
{
    public class Encrypt
    {
        public static string EncryptStringAES(string inputText, string password)
        {
            using (Aes aesAlg = Aes.Create())
            {
                // 임의의 IV 생성
                aesAlg.IV = GenerateRandomIV();

                // PBKDF2를 사용하여 안전한 키 생성
                var keyGenerator = new Rfc2898DeriveBytes(password, salt: aesAlg.IV); // salt can also be random
                aesAlg.Key = keyGenerator.GetBytes(aesAlg.KeySize / 8);

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(inputText);
                        }
                    }
                    byte[] ivAndData = new byte[aesAlg.IV.Length + msEncrypt.ToArray().Length];
                    Buffer.BlockCopy(aesAlg.IV, 0, ivAndData, 0, aesAlg.IV.Length);
                    Buffer.BlockCopy(msEncrypt.ToArray(), 0, ivAndData, aesAlg.IV.Length,
                        msEncrypt.ToArray().Length);

                    return Convert.ToBase64String(ivAndData);
                }
            }
        }

        public static string DecryptStringAES(string inputText, string password)
        {
            using (Aes aesAlg = Aes.Create())
            {
                byte[] fullCipher = Convert.FromBase64String(inputText);
                byte[] iv = new byte[aesAlg.IV.Length];
                Buffer.BlockCopy(fullCipher, 0, iv, 0, aesAlg.IV.Length);

                aesAlg.IV = iv;

                // Use PBKDF2 to generate the key
                var keyGenerator = new Rfc2898DeriveBytes(password, salt: aesAlg.IV); // salt should be the same as used in encryption
                aesAlg.Key = keyGenerator.GetBytes(aesAlg.KeySize / 8);

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(fullCipher, aesAlg.IV.Length,
                    fullCipher.Length - aesAlg.IV.Length))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor,
                        CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }



        private static byte[] GenerateRandomIV()
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.GenerateIV();
                return aesAlg.IV;
            }
        }
    }
}
