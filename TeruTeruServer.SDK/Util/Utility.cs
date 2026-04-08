using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using OpenCvSharp;

namespace TeruTeruServer.SDK.Util
{ 
    /// <summary>
    /// 공통적으로 사용되는 유틸리티 기능을 제공하는 정적 클래스입니다.
    /// AI 분석 및 이미지 처리를 위한 헬퍼 메서드를 포함합니다.
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
            const string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            StringBuilder result = new StringBuilder(30);
            foreach (byte b in data)
            {
                result.Append(characters[b % characters.Length]);
            }

            return result.ToString();
        }

        /// <summary>
        /// byte[] 이미지 데이터를 OpenCV Mat 객체로 변환합니다.
        /// </summary>
        /// <param name="data">바이너리 이미지 데이터</param>
        /// <returns>OpenCV Mat 객체</returns>
        public static Mat ByteArrayToMat(byte[] data)
        {
            if (data == null || data.Length == 0) return null;
            return Cv2.ImDecode(data, ImreadModes.Color);
        }

        /// <summary>
        /// OpenCV Mat 객체를 특정 포맷의 byte[] 데이터로 변환합니다.
        /// </summary>
        /// <param name="mat">Mat 객체</param>
        /// <param name="extension">압축 포맷 (예: ".jpg", ".png")</param>
        /// <returns>바이너리 데이터</returns>
        public static byte[] MatToByteArray(Mat mat, string extension = ".jpg")
        {
            if (mat == null || mat.Empty()) return Array.Empty<byte>();
            return mat.ToBytes(extension);
        }
    }
}
