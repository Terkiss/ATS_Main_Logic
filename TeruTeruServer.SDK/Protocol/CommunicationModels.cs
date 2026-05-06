using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text.Json;
using TeruTeruServer.SDK.Enums;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.SDK.Protocol
{
    public class PlayerData
    {
        public string Guid { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime ConnectTime { get; set; }
    }

    public class ReconnectRequest
    {
        public int HostID { get; set; }
        public string ReconnectToken { get; set; } = string.Empty;
    }

    public class ReconnectResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 서버에서 클라이언트로 특정 명령을 전달하는 프록시 클래스입니다.
    /// </summary>
    public class RpcProxy
    {
        private readonly IMessageSender? _messageSender;
        private readonly ISessionManager? _sessionManager;

        public RpcProxy() { }
        public RpcProxy(IMessageSender messageSender, ISessionManager sessionManager)
        {
            _messageSender = messageSender;
            _sessionManager = sessionManager;
        }

        /// <summary>
        /// JSON 객체를 직렬화하여 클라이언트에 전송합니다. (중복 코드 제거용)
        /// </summary>
        public void SendJsonResponse<T>(Socket socket, ProtocolSelect protocol, T data)
        {
            string json = JsonSerializer.Serialize(data);
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            byte[] packet = new byte[body.Length + 2];
            packet[0] = (byte)SendType.Json;
            packet[1] = (byte)protocol;
            Array.Copy(body, 0, packet, 2, body.Length);

            _messageSender.SendData(socket, packet);
        }

        /// <summary>
        /// 모든 클라이언트에게 YOLO 탐지 결과를 브로드캐스팅합니다.
        /// </summary>
        public void BroadcastDetectResult(YoloDetectResult result)
        {
            foreach (var session in _sessionManager.Players.Values)
            {
                if (session.ClientSocket != null && session.State == SessionState.Connected)
                {
                    SendJsonResponse(session.ClientSocket, ProtocolSelect.QueueCountCommand, result);
                }
            }
        }

        /// <summary>
        /// YOLO 워커에게 분석 요청을 트리거합니다. (가상)
        /// </summary>
        public void RequestObjectDetect(SendImageData data)
        {
            // 실제 분석 워커와의 IPC 또는 큐 연동 로직
            TeruTeruLogger.LogInfo($"Triggered YOLO Analysis Request for GUID: {data.Guid}");
        }
    }

    /// <summary>
    /// 클라이언트로부터 받은 패킷을 해석하여 서버 메서드를 호출하는 수신부입니다.
    /// </summary>
    public class RpcStub
    {
        private readonly IMessageSender _messageSender;
        private readonly ISessionManager _sessionManager;

        public RpcStub(IMessageSender messageSender, ISessionManager sessionManager)
        {
            _messageSender = messageSender;
            _sessionManager = sessionManager;
        }

        public byte[] HandleRequest(Socket socket, byte[] buffer)
        {
            if (buffer.Length < 2) return null;
            // 미들웨어에서 이미 인증을 거친 안전한 패킷만 이리로 옵니다.
            return null;
        }
    }

    public class MethodsSelector
    {
        public static byte NotifyPlayerExit(string guid) => 0;
    }

    public class SendImageData
    {
        public string Guid { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public int ImgSize { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class YoloDetectResult
    {
        public string Guid { get; set; } = string.Empty;
        public string UserID { get; set; } = string.Empty;
        public int HostID { get; set; }
        public string DetectionResult { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
