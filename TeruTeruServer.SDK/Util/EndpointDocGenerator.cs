using System.Collections.Generic;
using System.Text;
using TeruTeruServer.SDK.Models;

namespace TeruTeruServer.SDK.Util
{
    /// <summary>
    /// 등록된 엔드포인트 정보를 바탕으로 마크다운 문서를 생성하는 유틸리티입니다.
    /// </summary>
    public static class EndpointDocGenerator
    {
        /// <summary>
        /// ProtocolEndpointInfo 목록을 마크다운 테이블 형식으로 변환합니다.
        /// </summary>
        public static string GenerateMarkdown(IReadOnlyList<ProtocolEndpointInfo> endpoints)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Registered Endpoints Reference");
            sb.AppendLine();
            sb.AppendLine("| Method Name | Endpoint Name (RPC/Protocol) | Type | Requires Auth |");
            sb.AppendLine("| :--- | :--- | :--- | :--- |");

            foreach (var ep in endpoints)
            {
                sb.AppendLine($"| {ep.MethodName} | {ep.ProtocolOrRpcName} | {ep.BindingType} | {ep.RequiresAuth} |");
            }

            return sb.ToString();
        }
    }
}
