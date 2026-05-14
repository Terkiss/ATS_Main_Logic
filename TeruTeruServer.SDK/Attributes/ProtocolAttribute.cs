using System;
using TeruTeruServer.SDK.Enums;

namespace TeruTeruServer.SDK.Attributes
{
    /// <summary>
    /// 특정 ProtocolSelect 번호에 대해 수동으로 매핑될 메서드임을 나타냅니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ProtocolAttribute : Attribute
    {
        public ProtocolSelect Protocol { get; }

        public ProtocolAttribute(ProtocolSelect protocol)
        {
            Protocol = protocol;
        }
    }
}
