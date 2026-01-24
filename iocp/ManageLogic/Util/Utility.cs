using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TeruTeruServer.ManageLogic.Util
{ 
    /// <summary>
    /// 공통적으로 사용되는 유틸리티 기능을 제공하는 정적 클래스입니다.
    /// </summary>
    public static class Utility
    {
        // 고유 ID 생성에 사용될 문자 집합
        private static readonly char[] _characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();

        /// <summary>
        /// 보안 상으로 안전한 랜덤 바이트를 사용하여 고유 ID를 생성합니다.
        /// </summary>
        public static string GenerateUniqueId()
        {
            byte[] data = RandomNumberGenerator.GetBytes(24); 
            // 24바이트의 랜덤 데이터를 생성함
            const string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            StringBuilder result = new StringBuilder(30);
            foreach (byte b in data)
            {
                result.Append(characters[b % characters.Length]);
            }

            return result.ToString();
        }
    }
}
