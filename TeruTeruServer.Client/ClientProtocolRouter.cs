using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using TeruTeruServer.SDK.Attributes;
using TeruTeruServer.SDK.Enums;
using TeruTeruServer.SDK.Protocol;

namespace TeruTeruServer.Client
{
    /// <summary>
    /// 클라이언트에서 수신된 JSON 패킷을 사용자 로직 클래스(어트리뷰트 기반)로 자동 라우팅합니다.
    /// </summary>
    public class ClientProtocolRouter
    {
        private object? _logicInstance;
        private readonly Dictionary<string, MethodInfo> _rpcMethods = new();
        private readonly Dictionary<ProtocolSelect, MethodInfo> _manualMethods = new();
        private Action<string>? _onLog;

        public ClientProtocolRouter(Action<string>? onLog = null)
        {
            _onLog = onLog;
        }

        public void Initialize(object logicInstance)
        {
            _logicInstance = logicInstance;
            _rpcMethods.Clear();
            _manualMethods.Clear();

            var methods = _logicInstance.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var method in methods)
            {
                var rpcAttr = method.GetCustomAttribute<RpcAttribute>();
                if (rpcAttr != null)
                {
                    string rpcName = rpcAttr.Name ?? method.Name;
                    _rpcMethods[rpcName] = method;
                    Log($"[RPC-AUTO] Registered {rpcName} -> {method.Name}");
                }

                var protoAttr = method.GetCustomAttribute<ProtocolAttribute>();
                if (protoAttr != null)
                {
                    _manualMethods[protoAttr.Protocol] = method;
                    Log($"[PROTO-MANUAL] Registered {protoAttr.Protocol} -> {method.Name}");
                }
            }
        }

        public async Task RouteAsync(string jsonPayload, ProtocolSelect protocol)
        {
            if (_logicInstance == null) return;

            MethodInfo? methodToInvoke = null;
            string actualJson = jsonPayload;

            if (protocol == ProtocolSelect.RpcProtocol)
            {
                var rpcReq = JsonSerializer.Deserialize<RpcRequest>(jsonPayload);
                if (rpcReq != null && _rpcMethods.TryGetValue(rpcReq.MethodName, out methodToInvoke))
                {
                    actualJson = rpcReq.Params ?? "";
                }
            }
            else
            {
                _manualMethods.TryGetValue(protocol, out methodToInvoke);
            }

            if (methodToInvoke == null)
            {
                Log($"No handler found for protocol: {protocol}");
                return;
            }

            await InvokeMethodAsync(methodToInvoke, actualJson);
        }

        private async Task InvokeMethodAsync(MethodInfo method, string jsonPayload)
        {
            try
            {
                var parameters = method.GetParameters();
                object?[] args = new object[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    var param = parameters[i];
                    if (string.IsNullOrEmpty(jsonPayload))
                    {
                        args[i] = null;
                    }
                    else
                    {
                        try
                        {
                            args[i] = JsonSerializer.Deserialize(jsonPayload, param.ParameterType);
                        }
                        catch
                        {
                            args[i] = null;
                        }
                    }
                }

                var result = method.Invoke(_logicInstance, args);

                if (result is Task task)
                {
                    await task;
                }
            }
            catch (Exception ex)
            {
                Log($"Invoke Error in {method.Name}: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private void Log(string message)
        {
            _onLog?.Invoke($"[ClientRouter] {message}");
        }
    }
}
