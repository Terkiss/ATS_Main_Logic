# TeruTeruServer Client SDK API Reference

이 문서는 클라이언트 어플리케이션(Unity, C# App 등)에서 서버와 통신하기 위해 사용하는 `TeruTeruServer.Client` SDK의 레퍼런스입니다.

## 1. TeruClient

### Connection & Auth
- `async Task ConnectAsync(string address, int port)`: 서버에 소켓 연결을 시도합니다.
- `async Task<LoginResponse> LoginAsync(string userId, string password)`: 로그인을 시도하고 JWT 토큰을 획득합니다.
- `async Task<bool> ReconnectAsync(int hostId, string token)`: 유예 상태의 세션을 토큰으로 복구합니다.

### Messaging
- `async Task<TResponse?> InvokeRpcAsync<TResponse>(string methodName, object? parameters = null)`: 서버의 RPC 메서드를 호출하고 결과를 응답받습니다.
- `async Task SendProtocolAsync(ProtocolSelect protocol, object payload)`: JSON 기반 프로토콜을 전송합니다 (응답을 기다리지 않음).
- `void RegisterLogic(object logicInstance)`: `[Rpc]`, `[Protocol]` 어트리뷰트가 정의된 클래스를 등록하여 서버로부터 오는 메시지를 자동으로 수신합니다.

## 2. P2PManager

클라이언트 간 직접 통신(P2P)을 관리합니다.

- `async Task JoinGroupAsync(int groupId)`: P2P 그룹에 참여합니다.
- `void SendP2PData(int targetHostId, byte[] data)`: 특정 클라이언트에게 P2P 데이터를 전송합니다 (홀펀칭 실패 시 릴레이 자동 전환).
- `void BroadcastP2PData(byte[] data)`: 그룹 내 모든 멤버에게 P2P 데이터를 브로드캐스트합니다.

## 3. Usage Example

```csharp
var client = new TeruClient();
await client.ConnectAsync("127.0.0.1", 5555);

// 로그인
var loginResult = await client.LoginAsync("user1", "pass123");
if (loginResult.IsSuccess) 
{
    // RPC 호출
    var serverInfo = await client.InvokeRpcAsync<ServerInfo>("GetServerInfo");
    Console.WriteLine($"Server Version: {serverInfo.Version}");
}
```
