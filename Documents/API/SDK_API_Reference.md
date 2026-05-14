# TeruTeruServer SDK API Reference

이 문서는 TeruTeruServer 플러그인 개발을 위한 주요 인터페이스와 모델의 API 레퍼런스입니다.

## 1. Core Interfaces

### ILogicService
플러그인의 비즈니스 로직을 구현하는 메인 인터페이스입니다.
- `void ProcessDirectProtocol(byte[] buffer, Socket socket)`: 바이너리 프로토콜 직접 처리.
- `void ProcessJsonProtocol(string json, ProtocolSelect protocolSelect, Socket socket)`: JSON 프로토콜 처리 (자동 라우팅 미사용 시).

### ISessionManager
서버에 연결된 모든 클라이언트 세션을 관리합니다.
- `ConcurrentDictionary<int, ClientSession> Players`: 현재 활성화된 세션 목록.
- `bool TryAddPlayer(int hostId, ClientSession session)`: 세션 추가.
- `bool EvictSession(int hostId, out ClientSession? session)`: 세션 제거.
- `bool MarkAsGrace(int hostId)`: 세션을 유예 상태로 변경 (재연결 대기).

### ISessionStore
세션 데이터를 영속화하거나 클러스터 간에 공유하기 위한 저장소 인터페이스입니다.
- `void Set(int hostId, ClientSession session)`: 세션 저장.
- `ClientSession? Get(int hostId)`: 세션 조회.
- `ClientSession? FindByReconnectToken(string token)`: 재연결 토큰으로 세션 검색.

### IEventBus
분산 환경에서 서버 간 메시지 통신을 담당합니다.
- `void Publish<T>(string channel, T message)`: 메시지 발행.
- `void Subscribe<T>(string channel, Action<T> handler)`: 메시지 구독.

### IClusterRegistry
클러스터 노드 정보를 관리합니다.
- `IReadOnlyList<ClusterNodeInfo> GetActiveNodes()`: 활성 노드 목록 조회.

## 2. Models

### ClientSession
클라이언트의 연결 상태와 정보를 담고 있는 클래스입니다.
- `int HostID`: 세션 고유 ID.
- `Socket? ClientSocket`: 연결된 소켓 (유예 상태일 때 null).
- `string ReconnectToken`: 재연결 시 인증을 위한 토큰.
- `bool IsAuthenticated`: 로그인 인증 여부.

### ProtocolSelect (Enum)
시스템에서 미리 정의된 프로토콜 번호입니다.
- `ConnectProtocol (100)`
- `LoginProtocol (110)`
- `RpcProtocol (200)`
- `P2PRelayProtocol (300)`

### PacketContext
미들웨어 파이프라인에서 공유되는 패킷 처리 문맥입니다.
- `byte[] RawPacket`: 수신된 원본 바이트.
- `Socket ClientSocket`: 요청을 보낸 소켓.
- `void SendResponse(byte[] data)`: 응답 전송 유틸리티.
