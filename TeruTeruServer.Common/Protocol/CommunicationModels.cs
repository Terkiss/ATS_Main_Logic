using System;
using System.Collections.Generic;
using System.Net.Sockets;
using TeruTeruServer.Common.Enums;
using TeruTeruServer.Common.Interfaces;

namespace TeruTeruServer.Common.Protocol
{
    public class PlayerData
    {
        public string Guid { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime ConnectTime { get; set; }
    }

    public class RpcProxy
    {
        public RpcProxy() { }
        public RpcProxy(IMessageSender messageSender, ISessionManager sessionManager) { }
        public void RequestObjectDetect(string guid) { }
        public void RequestObjectDetect(SendImageData data) { }
        public void RequestObjectDetect(string guid, byte[] data) { }
    }

    public class RpcStub
    {
        public RpcStub() { }
        public RpcStub(IMessageSender messageSender, ISessionManager sessionManager) { }
        public byte[] HandleRequest(Socket socket, byte[] buffer) => Array.Empty<byte>();
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
