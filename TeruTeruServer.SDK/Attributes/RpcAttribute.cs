using System;

namespace TeruTeruServer.SDK.Attributes
{
    /// <summary>
    /// 해당 메서드가 RPC(Remote Procedure Call)로 호출 가능함을 나타냅니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RpcAttribute : Attribute
    {
        /// <summary>
        /// RPC 프로토콜 이름 (생략 시 메서드 이름 사용)
        /// </summary>
        public string Name { get; }

        public RpcAttribute(string name = null)
        {
            Name = name;
        }
    }
}
