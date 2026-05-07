using System;
using System.Text;

namespace TeruTeruServer.SDK.Util
{
    public static class PacketUtility
    {
        /// <summary>
        /// 원본 버퍼에서 처음 6바이트(헤더: SendType(1) + Protocol(1) + Seq(4))를 제외한 페이로드 바이트 배열을 추출합니다.
        /// </summary>
        public static byte[] ExtractPayload(this byte[] buffer)
        {
            if (buffer == null || buffer.Length < 6) return Array.Empty<byte>();
            byte[] payload = new byte[buffer.Length - 6];
            Array.Copy(buffer, 6, payload, 0, payload.Length);
            return payload;
        }

        /// <summary>
        /// 버퍼에서 6바이트 헤더를 제외한 부분을 UTF8 문자열(JSON)로 추출합니다.
        /// </summary>
        public static string ExtractJsonPayload(this byte[] buffer)
        {
            if (buffer == null || buffer.Length < 6) return string.Empty;
            return Encoding.UTF8.GetString(buffer, 6, buffer.Length - 6);
        }

        /// <summary>
        /// JSON 문자열을 UTF8 바이트 배열로 변환한 뒤, 앞에 6바이트의 더미 헤더 공간을 포함한 버퍼를 생성합니다.
        /// </summary>
        public static byte[] CreateBufferWithDummyHeader(string json)
        {
            if (string.IsNullOrEmpty(json)) return new byte[6];
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
            byte[] buffer = new byte[jsonBytes.Length + 6];
            Array.Copy(jsonBytes, 0, buffer, 6, jsonBytes.Length);
            return buffer;
        }
    }
}
