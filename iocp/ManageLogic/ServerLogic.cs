using TeruTeruServer.Common.Protocol;
using TeruTeruServer.Common.Enums;
using TeruTeruServer.ManageLogic.Util;
using TeruTeruServer.DB;
using TeruTeruServer.Common.Interfaces;
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

namespace TeruTeruServer.ManageLogic
{
    public class ServerLogic : ILogicService
    {
        private readonly IMessageSender _messageSender;
        private readonly IDatabaseService _dbService;
        private readonly ISessionManager _sessionManager;
        private readonly RpcStub _rpcStub;
        
        private const string SecretKey = "TeruTeruServer_Super_Secret_Key_2026"; 

        public ServerLogic(IMessageSender messageSender, IDatabaseService dbService, ISessionManager sessionManager)
        {
            _messageSender = messageSender;
            _dbService = dbService;
            _sessionManager = sessionManager;
            _rpcStub = new RpcStub(_messageSender, _sessionManager);
        }

        public void ProcessDirectProtocol(byte[] buffer, Socket socket)
        {
            var receivedDataCount = buffer.Length;

            if (receivedDataCount > 0)
            {
                byte[] responseBytes = _rpcStub.HandleRequest(socket, buffer);
                if (responseBytes != null)
                {
                    _messageSender.SendData(socket, responseBytes);
                }
            }
        }

        public void ProcessJsonProtocol(string json, ProtocolSelect protocolSelect, Socket socket)
        {
            switch (protocolSelect)
            {
                case ProtocolSelect.ConnectProtocol:
                    ConnectProtocol connectProtocol = JsonSerializer.Deserialize<ConnectProtocol>(json);
                    this.ConProtocol(socket, connectProtocol);
                    break;
                case ProtocolSelect.LoginProtocol:
                    LoginProtocol loginProtocol = JsonSerializer.Deserialize<LoginProtocol>(json);
                    this.HandleLogin(socket, loginProtocol);
                    break;
            }
        }

        private void HandleLogin(Socket socket, LoginProtocol loginData)
        {
            TeruTeruLogger.LogInfo($"Login attempt for user: {loginData.UserId}");

            // 1. 실제 DB 연동 인증 (IDatabaseService 활용)
            bool isSuccess = false;
            string query = "SELECT COUNT(*) FROM users WHERE userid = @uid AND password = @pwd";
            var parameters = new MySql.Data.MySqlClient.MySqlParameter[]
            {
                new MySql.Data.MySqlClient.MySqlParameter("@uid", loginData.UserId),
                new MySql.Data.MySqlClient.MySqlParameter("@pwd", loginData.Password) 
            };

            try 
            {
                int count = _dbService.SqlRunForCounter(query, parameters);
                isSuccess = (count > 0);
            }
            catch (Exception ex)
            {
                TeruTeruLogger.LogError($"DB 인증 중 오류 발생: {ex.Message}");
                isSuccess = false; 
            }

            if (isSuccess)
            {
                string token = GenerateJwtToken(loginData.UserId);
                
                if (_sessionManager.TryGetHostIdBySocket(socket, out int hostId))
                {
                    var sessions = ServerMemory.GetClientSessions();
                    var session = sessions.FirstOrDefault(s => s.HostID == hostId);
                    if (session != null)
                    {
                        session.AuthToken = token;
                    }
                    loginData.HostId = hostId;
                }

                loginData.IsSuccess = true;
                loginData.AuthToken = token;
                TeruTeruLogger.LogInfo($"Login success for {loginData.UserId}. JWT Token issued.");
            }
            else
            {
                loginData.IsSuccess = false;
                 TeruTeruLogger.LogWarning($"Login failed for user: {loginData.UserId}");
            }

            string responseJson = JsonSerializer.Serialize(loginData);
            byte[] tempByte = Encoding.UTF8.GetBytes(responseJson);
            byte[] sendData = new byte[tempByte.Length + 2];
            sendData[0] = (byte)SendType.Json;
            sendData[1] = (byte)ProtocolSelect.LoginProtocol;
            Array.Copy(tempByte, 0, sendData, 2, tempByte.Length);

            _messageSender.SendData(socket, sendData);
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

        #region Connection Protocol

        private void ConProtocol(Socket socket, ConnectProtocol protocol)
        {
            if (_sessionManager.Players.Values.Contains(socket))
            {
                TeruTeruLogger.LogWarning("소켓이 이미 등록되어 있습니다.");
            }
            else
            {
                string gameId = TeruTeruServer.ManageLogic.Util.Utility.GenerateUniqueId();
                var hostID = ServerMemory.GetHostID;
                
                bool guidCheck = true; 

                if (guidCheck)
                {
                    TeruTeruLogger.LogInfo("GUID 검증 성공. 클라이언트를 등록합니다...");

                    if (_sessionManager.TryAddPlayer(hostID, socket))
                    {
                        ClientSession clientSession = new ClientSession(hostID, socket, gameId);
                        
                        ConnectProtocol response = new ConnectProtocol
                        {
                            HostId = hostID,
                            Command = 1,
                            Data = gameId,
                        };

                        string json = JsonSerializer.Serialize(response);
                        byte[] tempByte = Encoding.UTF8.GetBytes(json);
                        byte[] sendData = new byte[tempByte.Length + 2];
                        sendData[0] = (byte)SendType.Json;
                        sendData[1] = (byte)ProtocolSelect.ConnectProtocol;
                        Array.Copy(tempByte, 0, sendData, 2, tempByte.Length);

                        _messageSender.SendData(socket, sendData);
                    }
                }
            }
        }

        #endregion
    }
}
