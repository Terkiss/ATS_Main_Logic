# 작업 지시문 (Worker Directive) - Milestone 14: Redis Production Integration

본 지시문은 `Milestone 14 — Redis Production Integration` 구현을 위한 구체적인 가이드라인입니다.

---

## 🎯 목표
이전 마일스톤(M12)에서 임시 시뮬레이션(로컬 캐시)으로 작성되었던 Redis 연동체들을 실제 **`StackExchange.Redis`** 라이브러리를 사용하도록 실구현하고, 분산 환경에서 다중 게임 서버 인스턴스가 올바르게 동작하도록 통합합니다.

---

## 🔍 기존 코드 정밀 분석 및 대상 파일
- **[MODIFY]** [RedisSessionStore.cs](file:///Users/yujeonghun/Project/ATS_Main_Logic/TeruTeruServer.Runtime/Clustering/RedisSessionStore.cs)
- **[MODIFY]** [RedisEventBus.cs](file:///Users/yujeonghun/Project/ATS_Main_Logic/TeruTeruServer.Runtime/Clustering/RedisEventBus.cs)
- **[MODIFY]** [RedisClusterRegistry.cs](file:///Users/yujeonghun/Project/ATS_Main_Logic/TeruTeruServer.Runtime/Clustering/RedisClusterRegistry.cs)
- **[MODIFY]** [TeruTeruServer.Runtime.csproj](file:///Users/yujeonghun/Project/ATS_Main_Logic/TeruTeruServer.Runtime/TeruTeruServer.Runtime.csproj) (NuGet 패키지 추가용)
- **[MODIFY]** [ClientSession.cs](file:///Users/yujeonghun/Project/ATS_Main_Logic/TeruTeruServer.SDK/Util/ClientSession.cs) (직렬화 필터링 어트리뷰트 적용 가능)

---

## ★ 특별 주의사항 (Critical)

1. **`ClientSession` 소켓 직렬화 방지 (★)**
   - `ClientSession` 객체 내부에는 `Socket ClientSocket`이 포함되어 있습니다. 이는 시스템 자원이므로 **절대로 Redis에 직렬화하여 저장할 수 없습니다.**
   - 세션을 JSON 형식으로 Redis에 저장할 때 `ClientSocket` 및 `UdpEndPoint`와 같이 직렬화 불가능한 필드는 직렬화 대상에서 제외해야 합니다.
   - `System.Text.Json`의 `[JsonIgnore]` 속성을 사용하거나, Redis 저장용 경량 DTO(예: `RedisSessionData`)를 설계하여 데이터 저장 및 로드 시 매핑하는 것을 강력히 권장합니다.

2. **ConnectionMultiplexer 자원 관리**
   - `ConnectionMultiplexer.Connect`는 무거운 작업이므로 각 클래스 내부에서 매번 연결을 시도해서는 안 되며, 한 번 초기화된 커넥션을 재사용하도록 싱글톤에 준하게 설계해야 합니다.
   - 예외 처리와 로컬 백업 캐시 전환 로직(Circuit Breaker 패턴 연계)을 고려하여 Redis 연결이 끊어지더라도 서버가 즉시 크래시되지 않고 로컬 캐시를 이용해 버티도록 설계해야 합니다.

---

## 🛠️ 세부 구현 지침

### 1. `TeruTeruServer.Runtime.csproj` 의존성 추가
- NuGet 패키지 `StackExchange.Redis` (최신 안정 버전)를 프로젝트에 추가합니다.

### 2. `RedisSessionStore.cs` 실구현
- 실제 Redis Hash 또는 String 구조(예: Key = `session:{hostId}`)를 사용하여 세션 데이터를 CRUD 합니다.
- **로컬 캐시 동기화**: 성능을 위해 `_localCache`와 병행하되, Redis에 데이터가 변경되거나 삭제될 경우 로컬 캐시 역시 신속하게 갱신되도록 연동합니다.
- `FindByReconnectToken` 구현 시, Redis Key를 효율적으로 검색할 수 있도록 별도의 Secondary Index(예: Set 이나 Hash 구조)를 운용하거나 적절한 SCAN 로직을 도입합니다.

### 3. `RedisEventBus.cs` 실구현
- `StackExchange.Redis`의 Pub/Sub API를 사용합니다.
- `Publish<T>` 호출 시 메시지를 JSON 직렬화하여 Redis 채널로 발행합니다.
- `Subscribe<T>` 호출 시 Redis 채널을 구독하고 수신 메시지를 역직렬화하여 핸들러를 호출합니다.

### 4. `RedisClusterRegistry.cs` 실구현
- 다른 서버 인스턴스들의 부하 및 정보(`ClusterNodeInfo`)를 Redis Hash(예: Key = `nodes`, Field = `nodeId`) 구조에 등록하고 조회합니다.
- 하트비트 주기(`UpdateHeartbeat`)가 정상적으로 연동되어 만료된 노드를 감지할 수 있도록 TTL 또는 주기적인 무효화 검증을 고려합니다.

---

## 🚫 변경 제한 및 허용 범위
- **허용 범위**:
  - `TeruTeruServer.Runtime/Clustering/` 디렉토리 내 Redis 연동 소스 수정 및 추가.
  - 직렬화 무시를 위한 `ClientSession.cs` 내 속성 추가.
- **금지 범위**:
  - `ISessionManager.Players` 등 기존 세션 인터페이스의 주요 스펙 수정 금지.
  - 미들웨어 실행 구조의 오더링(`MainServer.cs` 내 `Initialize`) 임의 변경 금지.

---

## 🧪 검증 절차 (Verification)
1. **로컬 Redis 검증**:
   - 로컬 또는 Docker 환경에 Redis 서버를 기동합니다.
   - `dotnet test`를 통해 실제 Redis에 데이터를 삽입/삭제하고 Pub/Sub 메시지가 성공적으로 송수신되는지 단위 테스트를 실행합니다.
2. **릴리즈 Gate**:
   - `verify-release.sh` 스크립트를 최종 통과해야 합니다.
