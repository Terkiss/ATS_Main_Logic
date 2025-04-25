using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TeruTeruServer.ManageLogic.Util
{ 
    public static class Utility
    {
        private static readonly char[] chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();

        public static string GenerateUniqueId()
        {
            byte[] data = RandomNumberGenerator.GetBytes(24); // 24 bytes to generate a base64 string of ~30 characters

            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            StringBuilder result = new StringBuilder(30);
            foreach (byte b in data)
            {
                result.Append(chars[b % chars.Length]);
            }

            return result.ToString();
        }
    }
}
