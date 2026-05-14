using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using TeruTeruServer.SDK.Attributes;
using TeruTeruServer.SDK.Enums;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Protocol;
using TeruTeruServer.SDK.Models;
using TeruTeruServer.SDK.Util;

namespace TeruTeruServer.Runtime.Rpc
{
    public class ProtocolRouter : IProtocolRouter
    {
        private ILogicService? _logicService;
        private readonly ISessionManager _sessionManager;
        private readonly Dictionary<string, MethodInfo> _rpcMethods = new();
        private readonly Dictionary<ProtocolSelect, MethodInfo> _manualMethods = new();
        private readonly List<ProtocolEndpointInfo> _endpoints = new();

        public IReadOnlyList<ProtocolEndpointInfo> GetRegisteredEndpoints() => _endpoints.AsReadOnly();

        public ProtocolRouter(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public void Initialize(ILogicService logicService)
        {
            _logicService = logicService;
            _rpcMethods.Clear();
            _manualMethods.Clear();
            _endpoints.Clear();

            var methods = _logicService.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var method in methods)
            {
                var requiresAuth = method.GetCustomAttribute<RequiresAuthAttribute>() != null;

                // [Rpc] 어트리뷰트 분석 (문자열 기반 자동 연결)
                var rpcAttr = method.GetCustomAttribute<RpcAttribute>();
                if (rpcAttr != null)
                {
                    string rpcName = rpcAttr.Name ?? method.Name;
                    _rpcMethods[rpcName] = method;
                    _endpoints.Add(new ProtocolEndpointInfo 
                    { 
                        MethodName = method.Name, 
                        ProtocolOrRpcName = rpcName, 
                        BindingType = "Rpc",
                        RequiresAuth = requiresAuth
                    });
                    TeruTeruLogger.LogInfo($"[RPC-AUTO] {rpcName} -> {method.Name}");
                }

                // [Protocol] 어트리뷰트 분석 (Enum 기반 수동 연결)
                var protoAttr = method.GetCustomAttribute<ProtocolAttribute>();
                if (protoAttr != null)
                {
                    _manualMethods[protoAttr.Protocol] = method;
                    _endpoints.Add(new ProtocolEndpointInfo 
                    { 
                        MethodName = method.Name, 
                        ProtocolOrRpcName = protoAttr.Protocol.ToString(), 
                        BindingType = "Protocol",
                        RequiresAuth = requiresAuth
                    });
                    TeruTeruLogger.LogInfo($"[PROTO-MANUAL] {protoAttr.Protocol} -> {method.Name}");
                }
            }
        }

        public async Task<string> RouteAsync(string jsonPayload, ProtocolSelect protocol, Socket socket)
        {
            if (_logicService == null) return "{\"error\": \"Logic not initialized\"}";

            MethodInfo? methodToInvoke = null;
            string actualJson = jsonPayload;

            if (protocol == ProtocolSelect.RpcProtocol)
            {
                // RPC 방식: RpcRequest 패킷 파싱 후 매핑
                var rpcReq = JsonSerializer.Deserialize<RpcRequest>(jsonPayload);
                if (rpcReq != null && _rpcMethods.TryGetValue(rpcReq.MethodName, out methodToInvoke))
                {
                    actualJson = rpcReq.Params;
                }
            }
            else
            {
                // 수동 방식: Protocol 번호로 직접 매핑
                _manualMethods.TryGetValue(protocol, out methodToInvoke);
            }

            if (methodToInvoke == null)
            {
                TeruTeruLogger.LogWarning($"No handler found for protocol: {protocol}");
                return "{\"error\": \"Handler not found\"}";
            }

            var requiresAuth = methodToInvoke.GetCustomAttribute<RequiresAuthAttribute>();
            if (requiresAuth != null)
            {
                if (!_sessionManager.TryGetHostIdBySocket(socket, out int hostId) ||
                    !_sessionManager.Players.TryGetValue(hostId, out var session) ||
                    !session.IsAuthenticated)
                {
                    TeruTeruLogger.LogWarning($"Unauthorized access to {methodToInvoke.Name} by {socket.RemoteEndPoint}");
                    return "{\"error\": \"Unauthorized\"}";
                }
            }

            return await InvokeMethodAsync(methodToInvoke, actualJson, socket);
        }

        private async Task<string> InvokeMethodAsync(MethodInfo method, string jsonPayload, Socket socket)
        {
            try
            {
                var parameters = method.GetParameters();
                object[] args = new object[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    var param = parameters[i];
                    if (param.ParameterType == typeof(Socket))
                    {
                        args[i] = socket;
                    }
                    else
                    {
                        args[i] = JsonSerializer.Deserialize(jsonPayload, param.ParameterType)!;
                    }
                }

                var result = method.Invoke(_logicService, args);

                if (result is Task task)
                {
                    await task;
                    var resultProperty = task.GetType().GetProperty("Result");
                    return resultProperty != null ? JsonSerializer.Serialize(resultProperty.GetValue(task)) : "{\"status\": \"ok\"}";
                }

                return result != null ? JsonSerializer.Serialize(result) : "{\"status\": \"ok\"}";
            }
            catch (Exception ex)
            {
                TeruTeruLogger.LogError($"Invoke Error in {method.Name}: {ex.InnerException?.Message ?? ex.Message}");
                return $"{{\"error\": \"{ex.InnerException?.Message ?? ex.Message}\"}}";
            }
        }
    }
}
