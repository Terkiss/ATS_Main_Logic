**[문서 내비게이션 바]**
**[Technical]** [Architecture](./Architecture.md) | [API Reference](./API_Reference.md) | [Setup Guide](./Setup_Guide.md) | [Database Schema](./Database_Schema.md)
**[UserGuide]** [Introduction](../UserGuide/Introduction.md) | [Installation](../UserGuide/Installation.md) | [How to Use](../UserGuide/How_to_Use.md) | [Troubleshooting](../UserGuide/Troubleshooting.md)
---

# API 레퍼런스 (API Reference)

이 문서는 서버 엔진과 클라이언트 간의 통신 규약 및 주요 인터페이스 명세를 다룹니다.

## 1. 패킷 구조 (Packet Structure)
모든 네트워크 패킷은 다음 구조를 가집니다 (TCP 기준).
`[전송 타입(1 byte)]` + `[프로토콜 타입(1 byte, Json 전송 시)]` + `[페이로드(Payload)]`

*   **SendType (1 byte)**: `0` = Direct (바이너리), `1` = Json
*   **ProtocolSelect (1 byte)**: 아래 열거형 참조

## 2. 프로토콜 열거형 (`ProtocolSelect`)
```csharp
public enum ProtocolSelect : byte
{
    ConnectProtocol = 1,
    LoginProtocol = 2,
    RpcProtocol = 100,       // 범용 RPC 자동 매핑 프로토콜
    QueueCountCommand = 101, // 큐 상태 확인
    ImageDumpCommand = 102   // AI 이미지 전송
}
```

## 3. 핵심 JSON DTO 명세

### 3.1. LoginProtocol (로그인)
클라이언트가 서버에 인증을 요청할 때 사용합니다.
```json
{
  "UserId": "test_user",
  "Password": "hashed_password",
  "AuthToken": null,
  "IsSuccess": false
}
```
*응답 시 `IsSuccess`가 true로 변경되고 `AuthToken`에 JWT 토큰이 담겨 반환됩니다.*

### 3.2. RpcRequest (범용 RPC 호출)
`[Rpc]` 애트리뷰트가 지정된 메서드를 원격으로 호출할 때 사용합니다.
```json
{
  "ProtocolSelector": 100,
  "Command": 0,
  "HostId": 0,
  "MethodName": "Echo",
  "Params": "\"Hello TeruTeru Server!\"" // 직렬화된 매개변수 데이터
}
```

## 4. 플러그인 라우팅 API

플러그인 개발자는 엔진 내부를 수정할 필요 없이 아래 애트리뷰트를 활용해 통신 엔드포인트를 노출합니다.

### `[Protocol(ProtocolSelect)]` (수동 매핑)
기존 열거형 기반의 강력한 타입 안정성을 제공하는 라우팅 방식입니다.
```csharp
[Protocol(ProtocolSelect.LoginProtocol)]
public void HandleLogin(Socket socket, LoginProtocol loginData) { ... }
```

### `[Rpc("MethodName")]` (자동 매핑)
메서드 이름 또는 지정된 문자열을 기반으로 동적 바인딩되는 유연한 라우팅 방식입니다. 파라미터는 JSON 역직렬화를 통해 자동 주입(Dependency Injection)됩니다.
```csharp
[Rpc("GetServerInfo")]
public async Task<object> GetServerInfo(Socket socket) { ... }
```