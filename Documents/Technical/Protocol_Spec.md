# TeruTeruServer Protocol Specification

본 문서는 TeruTeruServer와 Client 간의 네트워크 통신에 사용되는 패킷 구조, 헤더 정보, 인증 방식 및 P2P/그룹 통신 규약을 상세히 정의합니다.

## 1. Packet Structure (패킷 구조)

네트워크 단에서 송수신되는 모든 바이트 배열(Packet)은 맨 앞 2바이트의 고정 헤더를 가지며, 페이로드의 형태에 따라 파싱 방식이 결정됩니다.

### 1.1 기본 구조
모든 패킷은 최소 **6바이트**의 헤더를 가져야 합니다.
- `[0] 바이트`: **SendType** (전송 형식, `byte`)
- `[1] 바이트`: **ProtocolSelect** (프로토콜 식별자, `byte`)
- `[2~5] 바이트`: **SequenceNumber** (패킷 순서 번호, `uint32`)
- `[6~N] 바이트`: **Payload** (본문 데이터)

### 1.2 SendType Enum
```csharp
public enum SendType : byte
{
    Direct = 0, // 바이트 배열 그대로 처리되는 Raw 데이터 (이미지 덤프, P2P 릴레이 등)
    Json = 1    // UTF-8로 인코딩된 JSON 문자열 데이터 (RPC, 상태 동기화 등)
}
```

### 1.3 ProtocolSelect Enum (주요 식별자)
```csharp
public enum ProtocolSelect : byte
{
    // --- Core Protocols ---
    ConnectProtocol = 1,
    LoginProtocol = 2,
    TokenRefreshProtocol = 21,
    ReconnectProtocol = 3,
    RpcProtocol = 100,

    // --- P2P & Relay (M3) ---
    UdpRegisterProtocol = 4,
    HolePunchRequest = 5,
    P2PRelayProtocol = 6,
    GroupRelayProtocol = 7,
    JoinGroupProtocol = 8,
    P2PPingProtocol = 9,
    RelayFallbackProtocol = 10,

    // --- Game Logic & Sync (M7, M8) ---
    StateSyncProtocol = 20,
    GameInputProtocol = 22,
    StateAckProtocol = 23,
    RttPingProtocol = 24,
    HitValidationProtocol = 25,

    // --- World & Zone (M9) ---
    ZoneTransferProtocol = 26,
    ZoneInfoProtocol = 27,

    // --- Security (M10) ---
    SecurityEventProtocol = 28,

    // --- Matchmaking & Session (M11) ---
    MatchmakingProtocol = 29,
    SessionStateProtocol = 30,

    // --- Scalability & Ops (M12) ---
    ClusterInfoProtocol = 31,
    DashboardProtocol = 32,

    // --- Commands ---
    QueueCountCommand = 101,
    ImageDumpCommand = 102
}
```

---

## 2. Authentication (보안 및 인증)

TeruTeruServer는 Stateless한 인증을 위해 **JWT(JSON Web Token)**를 사용합니다.

### 2.1 JWT 획득
`LoginProtocol`을 통해 클라이언트가 인증에 성공하면, 서버는 응답 JSON에 `AuthToken` 필드를 담아 발급합니다.

```json
{
  "UserId": "testuser",
  "IsSuccess": true,
  "AuthToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### 2.2 인증 패킷 전송 (Authenticated Direct Packet)
`Direct` 방식의 보안 프로토콜을 서버로 보낼 때는 페이로드 앞에 JWT 인증 정보를 포함해야 합니다.

- **구조**: `[SendType(1)][ProtocolSelect(1)][Seq(4)][TokenLength(4)][TokenBytes(N)][Payload(M)]`
- **검증**: 서버의 `AuthMiddleware`는 헤더 뒤의 4바이트 정수를 읽어 토큰 길이를 추출하고, 토큰 서명을 검증한 후, `Payload` 부분만 추출하여 라우터로 넘깁니다.

### 2.3 HMAC 메시지 변조 검증 (M10 강화)
모든 패킷은 데이터 무결성 보장을 위해 HMAC-SHA256 서명을 포함할 수 있습니다. 
`HmacVerifyMiddleware`는 미리 공유된 Secret Key를 사용하여 패킷의 변조 여부를 확인하며, 변조가 감지될 경우 해당 세션은 즉시 차단(Sanction) 처리됩니다.

---

## 3. P2P & Group Communication Protocol

서버의 개입을 최소화하고 클라이언트 간 직접 통신을 유도하기 위한 프로토콜 정의입니다.

### 3.1 UDP 등록 (`UdpRegisterProtocol`)
- **목적**: 클라이언트가 서버에 접속 직후 자신의 외부 IP와 Port 정보를 서버에 알립니다.
- **방식**: UDP 소켓을 통해 서버의 UDP 포트로 토큰이 포함된 `Direct` 패킷을 전송.

### 3.2 시그널링 (`HolePunchRequest`)
- **목적**: 그룹에 접속하거나 P2P 통신이 필요할 때, 서버가 클라이언트 양측에 상대방의 종단점(Endpoint) 정보를 내려줍니다.
- **방식**: `Json` 패킷으로 전송됨.
- **JSON 모델 (PeerEndpointInfo)**:
```json
{
  "PeerHostID": 42,
  "IP": "112.155.10.22",
  "Port": 50031
}
```

### 3.3 릴레이 (Fallback)
NAT 횡단(Hole Punching)에 실패하여 P2P 직접 통신이 불가능할 경우, 서버를 거쳐 브로드캐스트/유니캐스트를 수행합니다.
- `P2PRelayProtocol`: 1:1 통신을 서버가 중계합니다.
- `GroupRelayProtocol`: 특정 그룹에 속한 다수에게 서버가 메시지를 복제하여 중계(하이브리드 멀티캐스트)합니다.
