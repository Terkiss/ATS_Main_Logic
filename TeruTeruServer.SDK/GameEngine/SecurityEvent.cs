using System;

namespace TeruTeruServer.SDK.GameEngine
{
    /// <summary>
    /// 게임 보안 및 안티치트 위반 이벤트를 정의하는 클래스입니다.
    /// </summary>
    public class SecurityEvent
    {
        public int HostId { get; set; }
        public string EventType { get; set; } = "";   // "SpeedHack", "Teleport", "InputFlood", "PacketTamper"
        public string Description { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Severity { get; set; } = "Warning";  // "Warning", "Critical"
    }
}
