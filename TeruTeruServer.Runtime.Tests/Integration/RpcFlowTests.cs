using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TeruTeruServer.Logic.Default;
using TeruTeruServer.Runtime.Testing;
using TeruTeruServer.SDK.Clustering;
using TeruTeruServer.SDK.Enums;
using TeruTeruServer.SDK.Interfaces;
using TeruTeruServer.SDK.Protocol;
using TeruTeruServer.SDK.Util;
using Xunit;

namespace TeruTeruServer.Runtime.Tests.Integration
{
    public class RpcFlowTests
    {
        private readonly IServiceProvider _serviceProvider;

        public RpcFlowTests()
        {
            var services = new ServiceCollection();
            
            var mockMsgSender = new MockMessageSender();
            services.AddSingleton<IMessageSender>(mockMsgSender);
            services.AddSingleton<IDatabaseService>(new Mock<IDatabaseService>().Object);
            services.AddSingleton<ISessionStore, InMemorySessionStore>();
            services.AddSingleton<ISessionManager, SessionManager>();
            services.AddSingleton<IEventBus, LocalEventBus>();
            services.AddSingleton<IProtocolRouter, Rpc.ProtocolRouter>();

            services.AddSingleton<ILogicService>(sp => 
            {
                var sender = sp.GetRequiredService<IMessageSender>();
                var db = sp.GetRequiredService<IDatabaseService>();
                var session = sp.GetRequiredService<ISessionManager>();
                var router = sp.GetRequiredService<IProtocolRouter>();
                var bus = sp.GetRequiredService<IEventBus>();
                return new LogicPlugin(sender, db, session, router, bus);
            });

            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task Rpc_GetServerInfo_ReturnsValidObject()
        {
            // Arrange
            using var mockServer = new MockServer(_serviceProvider);
            var simulator = new PacketSimulator(mockServer);
            
            // RpcRequest 구성 (GetServerInfo는 인증 불필요)
            var rpcReq = new RpcRequest 
            { 
                MethodName = "GetServerInfo", 
                Params = "{}" 
            };

            // Act
            var response = await simulator.SendAndReceive<dynamic>(ProtocolSelect.RpcProtocol, rpcReq);

            // Assert
            Assert.NotNull(response);
            string json = response.ToString();
            Assert.Contains("TeruTeru Server AI Engine", json);
        }

        [Fact]
        public async Task Rpc_Echo_Unauthorized_ReturnsNoResponse()
        {
            // Arrange
            using var mockServer = new MockServer(_serviceProvider);
            var simulator = new PacketSimulator(mockServer);
            
            // Echo는 [RequiresAuth]가 걸려있음. 
            // 현재 LogicPlugin + ProtocolRouter 구현 상, 인증 실패 시 에러 JSON을 반환하거나 
            // 세션이 없으면 응답을 보내지 않음.
            
            var rpcReq = new RpcRequest 
            { 
                MethodName = "Echo", 
                Params = "\"Hello Integration Test\"" 
            };

            // Act
            var response = await simulator.SendAndReceive<dynamic>(ProtocolSelect.RpcProtocol, rpcReq);

            // Assert
            // 세션 없이 요청을 보냈으므로, 로직 핸들러에서 응답을 보내지 않아 null이 되어야 함.
            Assert.Null(response);
        }
    }
}
