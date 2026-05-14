using System;

namespace TeruTeruServer.SDK.Attributes
{
    /// <summary>
    /// 프로토콜 핸들러 또는 RPC 메서드 호출 전, 유효한 인증 토큰이 검증되었는지(세션이 IsAuthenticated 상태인지)
    /// 확인하도록 강제하는 보안 어트리뷰트입니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class RequiresAuthAttribute : Attribute
    {
    }
}
