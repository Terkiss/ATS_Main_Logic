using System;
using System.Text;

namespace TeruTeruServer.SDK.Util
{
    public static class PacketUtility
    {
        /// <summary>
        /// 원본 버퍼에서 처음 2바이트(헤더)를 제외한 페이로드 바이트 배열을 추출합니다.
        /// </summary>
        public static byte[] ExtractPayload(this byte[] buffer)
        {
            if (buffer == null || buffer.Length < 2) return Array.Empty<byte>();
            byte[] payload = new byte[buffer.Length - 2];
            Array.Copy(buffer, 2, payload, 0, payload.Length);
            return payload;
        }

        /// <summary>
        /// 버퍼에서 2바이트 헤더를 제외한 부분을 UTF8 문자열(JSON)로 추출합니다.
        /// </summary>
        public static string ExtractJsonPayload(this byte[] buffer)
        {
            if (buffer == null || buffer.Length < 2) return string.Empty;
            return Encoding.UTF8.GetString(buffer, 2, buffer.Length - 2);
        }

        /// <summary>
        /// JSON 문자열을 UTF8 바이트 배열로 변환한 뒤, 앞에 2바이트의 더미 헤더 공간을 포함한 버퍼를 생성합니다.
        /// (핸들러 구조상 원본 버퍼 포맷이 필요할 때 사용)
        /// </summary>
        public static byte[] CreateBufferWithDummyHeader(string json)
        {
            if (string.IsNullOrEmpty(json)) return new byte[2];
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
            byte[] buffer = new byte[jsonBytes.Length + 2];
            Array.Copy(jsonBytes, 0, buffer, 2, jsonBytes.Length);
            return buffer;
        }
    }
}
