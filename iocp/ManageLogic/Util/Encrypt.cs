using TeruTeruServer.ServerEngineSDK.Interfaces;
﻿using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TeruTeruServer.ManageLogic.Util
{
    /// <summary>
    /// AES 알고리즘을 사용하여 문자열 암호화 및 복호화를 수행하는 클래스입니다.
    /// </summary>
    public class Encrypt
    {
        public static string EncryptStringAES(string inputText, string password)
        {
            using (Aes aesAlg = Aes.Create())
            {
                // 임의의 IV 생성
                aesAlg.IV = GenerateRandomIV();

                // PBKDF2를 사용하여 안전한 키 생성
                var keyGenerator = new Rfc2898DeriveBytes(password, salt: aesAlg.IV); // salt로 복호화 시에도 동일한 IV를 사용함
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

                // PBKDF2를 사용하여 키를 생성 (암호화 시와 동일한 salt 보장)
                var keyGenerator = new Rfc2898DeriveBytes(password, salt: aesAlg.IV); 
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
