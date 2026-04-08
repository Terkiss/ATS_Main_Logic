using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using TeruTeruServer.ServerEngineSDK.Enums;
using TeruTeruServer.ServerEngineSDK.Interfaces;
using TeruTeruServer.ServerEngineSDK.Protocol;
using TeruTeruServer.ServerEngineSDK.Util; // 추후 Util도 Common으로 옮기는 것 권장

namespace TeruTeruServer.Logic.Default
{
    public class LogicPlugin : ILogicService
    {
        private readonly IMessageSender _messageSender;
        private readonly IDatabaseService _dbService;
        private readonly ISessionManager _sessionManager;
        private readonly RpcProxy _rpcProxy;
        
        private const string SecretKey = "TeruTeruServer_Super_Secret_Key_2026"; 

        public LogicPlugin(IMessageSender messageSender, IDatabaseService dbService, ISessionManager sessionManager)
        {
            _messageSender = messageSender;
            _dbService = dbService;
            _sessionManager = sessionManager;
            _rpcProxy = new RpcProxy(_messageSender, _sessionManager);
        }

        public void ProcessDirectProtocol(byte[] buffer, Socket socket)
        {
            if (buffer == null || buffer.Length < 2) return;

            var sendType = (SendType)buffer[0];
            var protocolType = (ProtocolSelect)buffer[1];

            if (sendType == SendType.Json)
            {
                string json = Encoding.UTF8.GetString(buffer, 2, buffer.Length - 2);
                HandleJsonProtocol(json, protocolType, socket);
            }
            else if (sendType == SendType.Direct && protocolType == ProtocolSelect.ImageDumpCommand)
            {
                ProcessImageData(buffer, socket);
            }
        }

        public void ProcessJsonProtocol(string json, ProtocolSelect protocol, Socket socket)
        {
            HandleJsonProtocol(json, protocol, socket);
        }

        private void HandleJsonProtocol(string json, ProtocolSelect protocol, Socket socket)
        {
            switch (protocol)
            {
                case ProtocolSelect.ConnectProtocol:
                    ConProtocol(socket, JsonSerializer.Deserialize<ConnectProtocol>(json));
                    break;
                case ProtocolSelect.LoginProtocol:
                    HandleLogin(socket, JsonSerializer.Deserialize<LoginProtocol>(json));
                    break;
            }
        }

        private void HandleLogin(Socket socket, LoginProtocol loginData)
        {
            // [기존 로그인 로직]
            string token = GenerateJwtToken(loginData.UserId);
            loginData.IsSuccess = true;
            loginData.AuthToken = token;
            _rpcProxy.SendJsonResponse(socket, ProtocolSelect.LoginProtocol, loginData);
        }

        private void ConProtocol(Socket socket, ConnectProtocol protocol)
        {
            // [기존 연결 로직]
            protocol.IsSuccess = true;
            _rpcProxy.SendJsonResponse(socket, ProtocolSelect.ConnectProtocol, protocol);
        }

        private void ProcessImageData(byte[] buffer, Socket socket)
        {
            // [기존 이미지 처리 로직]
            byte[] imageData = new byte[buffer.Length - 2];
            Array.Copy(buffer, 2, imageData, 0, imageData.Length);

            var sendImgData = new SendImageData
            {
                Guid = Guid.NewGuid().ToString(),
                Data = imageData,
                ImgSize = imageData.Length,
                Timestamp = DateTime.UtcNow
            };

            // ServerMemory.SetImageData(sendImgData); // 서버 엔진과 공유 데이터 통신 필요
            _rpcProxy.RequestObjectDetect(sendImgData);
        }

        private string GenerateJwtToken(string userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(SecretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim("id", userId) }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
