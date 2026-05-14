# Milestone 12: Live Operations & Scalability — 작업자 지시문

## 목표

Documents/구현계획.md에 명시된 Milestone 12의 5개 작업 항목을 구현한다.
이 마일스톤은 Phase 2의 최종 마일스톤으로, 라이브 서비스 운영을 위한 서버 수평 확장성과 가용성을 확보한다.

> **중요**: M5에서 구축한 ISessionStore / IClusterRegistry / IEventBus 추상화 계층이 정확히 이 마일스톤을 위한 것이다. Local 구현체를 Redis/분산 구현체로 교체하는 구조가 DI 레벨에서 이미 준비되어 있다. 새 인터페이스를 만들기보다 기존 인터페이스의 새 구현체를 작성하라.

## 선행 조건 확인

Milestone 11이 커밋 완료되었다. 현재 브랜치: feature/phase2-architecture, HEAD: 983dcaa

```
git status --short --branch
git log --oneline -3
```

## 기존 코드 파악 (필독)

### M5에서 구축한 분산 인프라 추상화

- **TeruTeruServer.SDK/Interfaces/ISessionStore.cs** (37줄)
  - L14: `TryAdd(int hostId, ClientSession session)` — 세션 추가
  - L19: `TryGet(int hostId, out ClientSession session)` — 세션 조회
  - L24: `TryRemove(int hostId, out ClientSession session)` — 세션 삭제
  - L29: `FindByReconnectToken(string token)` — Grace 재연결 토큰으로 검색
  - L34: `GetAll()` — 전체 세션 열거
  - ★ Redis 구현체는 이 5개 메서드만 구현하면 된다. 인터페이스 자체를 수정하지 않는다.

- **TeruTeruServer.SDK/Clustering/InMemorySessionStore.cs** (31줄)
  - L15: `InternalDictionary` — ConcurrentDictionary 직접 노출
  - ★ SessionManager.Players와의 호환성을 위해 이 프로퍼티가 존재한다. Redis 구현체에서는 이 프로퍼티가 없으므로, SessionManager 생성자(L30-40)의 `if (store is InMemorySessionStore)` 분기에 영향 없음을 확인하라.

- **TeruTeruServer.SDK/Interfaces/IClusterRegistry.cs** (36줄)
  - L13: `RegisterNode(ClusterNodeInfo node)` — 노드 등록
  - L18: `DeregisterNode(string nodeId)` — 노드 해제
  - L23: `GetNode(string nodeId)` — 노드 조회
  - L28: `GetActiveNodes()` — 활성 노드 목록
  - L33: `UpdateHeartbeat(string nodeId)` — 하트비트 갱신
  - ★ 현재 LocalClusterRegistry는 단일 프로세스용. 분산 환경에서는 Redis/Pub-Sub 기반 구현체가 필요.

- **TeruTeruServer.SDK/Clustering/ClusterNodeInfo.cs** (36줄)
  - L13: `NodeId`, L18: `Address`, L23: `Port`, L28: `Status ("Active"/"Draining"/"Down")`, L33: `LastHeartbeat`
  - ★ `Status = "Draining"` 상태는 Rolling Update 시 사용. 해당 노드는 신규 세션을 받지 않음.

- **TeruTeruServer.SDK/Interfaces/IEventBus.cs** (26줄)
  - L13: `Publish<T>(string channel, T message)` — 이벤트 발행
  - L18: `Subscribe<T>(string channel, Action<T> handler)` — 이벤트 구독
  - L23: `Unsubscribe(string channel)` — 구독 해제
  - ★ LocalEventBus는 프로세스 내부만 커버. 분산 이벤트 버스(Redis Pub/Sub)는 크로스 노드 이벤트를 지원.

- **TeruTeruServer.SDK/Clustering/LocalEventBus.cs** (47줄)
  - L15-29: Publish — `ConcurrentDictionary` + `lock` 기반 단일 프로세스 이벤트 발행
  - ★ 기존 코드 수정하지 않는다. 새 구현체를 별도로 만든다.

- **TeruTeruServer.SDK/Util/ServerMetrics.cs** (29줄)
  - L7-9: `_processedPacketCount`, `Tps` — 기본 패킷 처리량 메트릭
  - ★ 대시보드를 위해 CCU(동접), Zone 수, 평균 지연시간 등 메트릭을 확장해야 한다.
  - ★ static 클래스이므로 필드/메서드 추가만 허용. 기존 필드 삭제/타입 변경 금지.

- **TeruTeruServer.Cli/Program.cs** (136줄)
  - L80: `services.AddSingleton<ISessionStore, InMemorySessionStore>()` — 여기를 Redis 구현체로 교체 가능
  - L82: `services.AddSingleton<IEventBus, LocalEventBus>()` — 여기를 Redis Pub/Sub 구현체로 교체 가능
  - L83: `services.AddSingleton<IClusterRegistry, LocalClusterRegistry>()` — 여기를 분산 레지스트리로 교체 가능
  - L42-50: 클러스터 자기 등록 로직 이미 존재
  - ★ DI 교체 지점이 명확하다. config 기반으로 Local/Redis를 전환하는 팩토리 패턴 권장.

- **TeruTeruServer.Runtime/ServerConnectConfigParameter.cs** (117줄)
  - L62: `HmacKey` (M10 추가)
  - ★ 여기에 Redis 연결 문자열, 클러스터 모드 플래그 등 신규 설정 필드를 추가한다.

- **TeruTeruServer.SDK/Interfaces/ISessionManager.cs** (94줄)
  - L25: `Players` (ConcurrentDictionary) — 15곳 이상 참조
  - L30: `if (store is InMemorySessionStore inMemory)` — InMemory일 때만 직접 참조
  - ★ Players 타입 변경 금지. Redis 모드 시 로컬 캐시로만 활용됨 (L36-39의 주석 참조).

### 테스트 현황
- 총 46개 테스트 통과 중. 모두 유지해야 한다.

## 작업 항목 (5건)

### 작업 1: 게임 서버 클러스터링 및 라우팅

**파일 범위:**
- [NEW] TeruTeruServer.Runtime/Clustering/ClusterRouter.cs
- [NEW] TeruTeruServer.Runtime/Clustering/NodeHealthMonitor.cs
- [MODIFY] TeruTeruServer.SDK/Clustering/ClusterNodeInfo.cs (필드 추가)
- [MODIFY] TeruTeruServer.Runtime/ServerConnectConfigParameter.cs (설정 추가)

**구현 내용:**
1. `ClusterNodeInfo`에 부하 메트릭 필드 추가:
   ```csharp
   public int CurrentConnections { get; set; }   // 현재 동접 수
   public int ActiveZoneCount { get; set; }       // 활성 존 수
   public int ActiveSessionCount { get; set; }    // 활성 게임 세션 수
   public double CpuUsagePercent { get; set; }    // CPU 사용률 (0-100)
   ```
2. `ClusterRouter` — 부하 기반 노드 선택:
   ```csharp
   public class ClusterRouter
   {
       private readonly IClusterRegistry _registry;
       
       /// <summary>
       /// 가장 부하가 적은 Active 노드를 선택합니다.
       /// </summary>
       public ClusterNodeInfo? SelectLeastLoadedNode()
       {
           return _registry.GetActiveNodes()
               .Where(n => n.Status == "Active")
               .OrderBy(n => n.CurrentConnections)
               .FirstOrDefault();
       }
       
       /// <summary>
       /// 특정 Zone이 있는 노드를 찾습니다.
       /// </summary>
       public ClusterNodeInfo? FindNodeForZone(int zoneId);
   }
   ```
3. `NodeHealthMonitor` — 노드 헬스체크 및 자동 장애 감지:
   ```csharp
   public class NodeHealthMonitor
   {
       private readonly IClusterRegistry _registry;
       private readonly TimeSpan _heartbeatTimeout = TimeSpan.FromSeconds(30);
       
       /// <summary>
       /// Tick 핸들러에서 호출. 타임아웃된 노드를 "Down"으로 마킹.
       /// </summary>
       public void CheckHealth()
       {
           foreach (var node in _registry.GetActiveNodes())
           {
               if (DateTime.UtcNow - node.LastHeartbeat > _heartbeatTimeout)
               {
                   node.Status = "Down";
                   // 이벤트 발행: "cluster:node:down"
               }
           }
       }
   }
   ```
4. `ServerConnectConfigParameter` 설정 추가:
   ```csharp
   public string ClusterMode { get; set; } = "Local";   // "Local" | "Redis"
   public string RedisConnectionString { get; set; } = "localhost:6379";
   public string NodeId { get; set; } = "";              // 명시적 NodeId (비어있으면 Guid 사용)
   ```

---

### 작업 2: ISessionStore Redis 백엔드 구현

**파일 범위:**
- [NEW] TeruTeruServer.Runtime/Clustering/RedisSessionStore.cs
- [NEW] TeruTeruServer.Runtime/Clustering/RedisClusterRegistry.cs
- [NEW] TeruTeruServer.Runtime/Clustering/RedisEventBus.cs

**구현 내용:**
1. `RedisSessionStore : ISessionStore` — 구조만 구현, 실제 Redis 연결은 인터페이스로 추상화:
   ```csharp
   public class RedisSessionStore : ISessionStore
   {
       private readonly string _connectionString;
       private readonly ConcurrentDictionary<int, ClientSession> _localCache = new();
       
       // ISessionStore 5개 메서드 구현
       // TryAdd: 로컬 캐시 + Redis SET 호출 (시뮬레이션)
       // TryGet: 로컬 캐시 우선, 미스 시 Redis GET
       // FindByReconnectToken: 로컬 캐시 순회 (Redis SCAN 시뮬레이션)
   }
   ```
   - ★ 실제 Redis 의존성(StackExchange.Redis)은 NuGet으로 추가하지 않는다. 연결 로직은 `// TODO: Redis integration` 주석으로 남기고, 로컬 캐시 기반으로 동작하되 Redis 호환 구조를 갖추도록 한다.
   - ★ 이유: 외부 의존성 추가 시 CI/CD 환경과 release gate에 영향을 줄 수 있다. Redis 통합은 별도 PR로 진행.

2. `RedisClusterRegistry : IClusterRegistry` — 동일 패턴:
   ```csharp
   // 로컬 ConcurrentDictionary + Redis Hash 시뮬레이션
   ```

3. `RedisEventBus : IEventBus` — 동일 패턴:
   ```csharp
   // 로컬 Pub/Sub + Redis Pub/Sub 시뮬레이션
   // Publish 시 로컬 핸들러 실행 + Redis PUBLISH 호출 주석
   ```

4. Program.cs에서 config.ClusterMode 기반 DI 전환:
   ```csharp
   if (config.ClusterMode == "Redis")
   {
       services.AddSingleton<ISessionStore>(sp => new RedisSessionStore(config.RedisConnectionString));
       services.AddSingleton<IClusterRegistry>(sp => new RedisClusterRegistry(config.RedisConnectionString));
       services.AddSingleton<IEventBus>(sp => new RedisEventBus(config.RedisConnectionString));
   }
   else
   {
       services.AddSingleton<ISessionStore, InMemorySessionStore>();
       services.AddSingleton<IClusterRegistry, LocalClusterRegistry>();
       services.AddSingleton<IEventBus, LocalEventBus>();
   }
   ```

---

### 작업 3: 무중단 배포 (Rolling Update) 파이프라인

**파일 범위:**
- [NEW] TeruTeruServer.Runtime/Clustering/RollingUpdateCoordinator.cs

**구현 내용:**
1. `RollingUpdateCoordinator`:
   ```csharp
   public class RollingUpdateCoordinator
   {
       private readonly IClusterRegistry _registry;
       private readonly IGameSessionManager _sessionManager;
       
       /// <summary>
       /// 현재 노드를 Draining 상태로 전환합니다.
       /// 새로운 세션을 받지 않고, 기존 세션이 모두 종료되면 Down으로 전환됩니다.
       /// </summary>
       public bool StartDraining(string nodeId)
       {
           var node = _registry.GetNode(nodeId);
           if (node == null) return false;
           node.Status = "Draining";
           return true;
       }
       
       /// <summary>
       /// Draining 상태의 노드가 안전하게 종료 가능한지 확인합니다.
       /// </summary>
       public bool IsReadyForShutdown(string nodeId)
       {
           var node = _registry.GetNode(nodeId);
           if (node == null || node.Status != "Draining") return false;
           return node.ActiveSessionCount == 0;
       }
       
       /// <summary>
       /// 클러스터 전체에서 업데이트가 가능한 노드를 순서대로 반환합니다.
       /// 세션이 없는 노드 우선.
       /// </summary>
       public IReadOnlyList<ClusterNodeInfo> GetUpdateOrder()
       {
           return _registry.GetActiveNodes()
               .OrderBy(n => n.ActiveSessionCount)
               .ToList();
       }
   }
   ```
   - ★ Draining 노드는 MatchQueue에서 매칭 대상에서 제외되어야 한다. ClusterRouter.SelectLeastLoadedNode()에서 `Status == "Active"` 필터가 이미 적용되어 있으므로, 매치메이킹 시 새 세션은 자동으로 Active 노드에만 생성된다.

---

### 작업 4: 동접 급증 대응 (Auto Scaling) 연동

**파일 범위:**
- [NEW] TeruTeruServer.Runtime/Clustering/AutoScaleMonitor.cs

**구현 내용:**
1. `AutoScaleMonitor` — 스케일링 신호 판단:
   ```csharp
   public class AutoScaleMonitor
   {
       private readonly IClusterRegistry _registry;
       private readonly IEventBus _eventBus;
       
       // 임계치
       private const int SCALE_UP_THRESHOLD = 80;    // 평균 CCU 80% 초과 시
       private const int SCALE_DOWN_THRESHOLD = 20;   // 평균 CCU 20% 미만 시
       private const int MIN_NODES = 1;
       
       public ScaleDecision Evaluate()
       {
           var nodes = _registry.GetActiveNodes();
           if (nodes.Count == 0) return ScaleDecision.None;
           
           double avgLoad = nodes.Average(n => 
               n.CurrentConnections / (double)MAX_CONNECTIONS_PER_NODE * 100);
           
           if (avgLoad > SCALE_UP_THRESHOLD)
               return ScaleDecision.ScaleUp;
           else if (avgLoad < SCALE_DOWN_THRESHOLD && nodes.Count > MIN_NODES)
               return ScaleDecision.ScaleDown;
           
           return ScaleDecision.None;
       }
       
       /// <summary>
       /// Tick 핸들러에서 호출. 스케일링 판단 후 이벤트 발행.
       /// </summary>
       public void CheckAndNotify()
       {
           var decision = Evaluate();
           if (decision != ScaleDecision.None)
               _eventBus.Publish("cluster:scale", decision);
       }
   }
   
   public enum ScaleDecision { None, ScaleUp, ScaleDown }
   ```
   - ★ 실제 K8s/PM2 API 호출은 구현하지 않는다. 이벤트만 발행하여 외부 오케스트레이터가 구독하도록 설계.
   - Tick 핸들러: `gameLoop.RegisterTickHandler(tick => { if (tick % 1200 == 0) autoScaleMonitor.CheckAndNotify(); })` (60초마다)

---

### 작업 5: 실시간 운영 대시보드 (메트릭 수집)

**파일 범위:**
- [NEW] TeruTeruServer.Runtime/Clustering/ClusterDashboard.cs
- [MODIFY] TeruTeruServer.SDK/Util/ServerMetrics.cs (메트릭 필드 확장)
- [NEW] TeruTeruServer.SDK/Enums/ProtocolEnums.cs (신규 enum 추가)

**구현 내용:**
1. `ServerMetrics` 필드 확장:
   ```csharp
   // 대시보드 메트릭 (Milestone 12)
   private static int _concurrentConnections = 0;
   private static int _activeZoneCount = 0;
   private static int _activeSessionCount = 0;
   private static long _averageLatencyMs = 0;
   
   public static int ConcurrentConnections => _concurrentConnections;
   public static int ActiveZoneCount => _activeZoneCount;
   public static int ActiveSessionCount => _activeSessionCount;
   public static long AverageLatencyMs => _averageLatencyMs;
   
   public static void UpdateCcu(int count) => Interlocked.Exchange(ref _concurrentConnections, count);
   public static void UpdateZoneCount(int count) => Interlocked.Exchange(ref _activeZoneCount, count);
   public static void UpdateSessionCount(int count) => Interlocked.Exchange(ref _activeSessionCount, count);
   public static void UpdateLatency(long ms) => Interlocked.Exchange(ref _averageLatencyMs, ms);
   ```

2. `ClusterDashboard` — 클러스터 전체 상태 스냅샷 수집:
   ```csharp
   public class ClusterDashboard
   {
       private readonly IClusterRegistry _registry;
       private readonly ISessionManager _sessionManager;
       private readonly IZoneManager _zoneManager;
       private readonly IGameSessionManager _gameSessionManager;
       
       public DashboardSnapshot GetSnapshot()
       {
           var nodes = _registry.GetActiveNodes();
           return new DashboardSnapshot
           {
               TotalNodes = nodes.Count,
               ActiveNodes = nodes.Count(n => n.Status == "Active"),
               DrainingNodes = nodes.Count(n => n.Status == "Draining"),
               DownNodes = nodes.Count(n => n.Status == "Down"),
               TotalCcu = nodes.Sum(n => n.CurrentConnections),
               TotalZones = nodes.Sum(n => n.ActiveZoneCount),
               TotalSessions = nodes.Sum(n => n.ActiveSessionCount),
               Tps = ServerMetrics.Tps,
               AverageLatencyMs = ServerMetrics.AverageLatencyMs,
               Timestamp = DateTime.UtcNow
           };
       }
   }
   
   public class DashboardSnapshot
   {
       public int TotalNodes { get; set; }
       public int ActiveNodes { get; set; }
       public int DrainingNodes { get; set; }
       public int DownNodes { get; set; }
       public int TotalCcu { get; set; }
       public int TotalZones { get; set; }
       public int TotalSessions { get; set; }
       public long Tps { get; set; }
       public long AverageLatencyMs { get; set; }
       public DateTime Timestamp { get; set; }
   }
   ```
3. `ProtocolSelect`에 추가:
   ```csharp
   ClusterInfoProtocol = 31,      // 클러스터 상태 조회 (M12)
   DashboardProtocol = 32,        // 대시보드 데이터 요청 (M12)
   ```
4. Tick 핸들러로 주기적 메트릭 갱신:
   ```csharp
   gameLoop.RegisterTickHandler(tick =>
   {
       if (tick % 200 == 0) // 10초마다
       {
           ServerMetrics.UpdateCcu(_sessionManager.Players.Count);
           ServerMetrics.UpdateSessionCount(_gameSessionManager.GetActiveSessions().Count);
           // 자기 노드 정보 갱신
           clusterRegistry.UpdateHeartbeat(nodeId);
       }
   });
   ```

## 변경 허용 범위

**허용:**
- TeruTeruServer.Runtime/Clustering/ 하위 신규 파일 생성
- TeruTeruServer.SDK/Util/ServerMetrics.cs 수정 (필드/메서드 추가만, 기존 삭제 금지)
- TeruTeruServer.SDK/Clustering/ClusterNodeInfo.cs 수정 (필드 추가만)
- TeruTeruServer.SDK/Enums/ProtocolEnums.cs 수정 (신규 enum 값 추가만)
- TeruTeruServer.Runtime/ServerConnectConfigParameter.cs 수정 (필드 추가만)
- TeruTeruServer.Cli/Program.cs 수정 (DI 등록 분기 추가)
- 테스트 프로젝트에 신규 테스트 파일 생성
- IMPLEMENTATION_PROGRESS.md 갱신

**금지:**
- .agents/ 수정 금지
- ISessionStore 인터페이스 시그니처 변경 금지
- IClusterRegistry 인터페이스 시그니처 변경 금지
- IEventBus 인터페이스 시그니처 변경 금지
- ISessionManager.Players 타입 변경 금지
- InMemorySessionStore / LocalClusterRegistry / LocalEventBus 수정 금지 (기존 Local 구현체 보존)
- IRoomBroadcaster 시그니처 변경 금지
- GameLoop.cs 수정 금지
- 외부 NuGet 패키지 추가 금지 (StackExchange.Redis 등은 별도 PR)
- 기존 46개 테스트 삭제/약화 금지
- 커밋/푸시 금지
- release gate 변경 금지

## 검증

1. `./scripts/verify-release.sh` 통과 (기존 46개 + 신규 테스트)
2. 클러스터 라우팅 테스트:
   - 3개 노드 등록 후 SelectLeastLoadedNode가 동접 최소 노드를 반환하는지 확인
   - Down 노드가 Active 목록에서 제외되는지 확인
3. 노드 헬스체크 테스트:
   - 30초 타임아웃 초과 노드가 "Down"으로 전환되는지 확인
4. Rolling Update 테스트:
   - StartDraining → 세션 0 → IsReadyForShutdown == true
   - 활성 세션이 있으면 IsReadyForShutdown == false
5. Auto Scaling 테스트:
   - 평균 부하 80% 초과 → ScaleUp
   - 평균 부하 20% 미만 → ScaleDown (노드 > 1)
   - 단일 노드 → ScaleDown 금지
6. 대시보드 테스트:
   - DashboardSnapshot 필드가 올바르게 계산되는지 확인
7. DI 전환 테스트:
   - ClusterMode="Local" 시 InMemory/Local 구현체 사용 확인
   - ClusterMode="Redis" 시 Redis 구현체 사용 확인

## 특별 주의사항

★ **외부 NuGet 패키지 추가 금지**: Redis 구현체는 실제 Redis 연결 없이 동작 가능한 구조로 작성하라. `// TODO: Replace with StackExchange.Redis` 주석으로 통합 지점을 명시하되, ConcurrentDictionary 기반 로컬 캐시로 동일한 인터페이스를 이행(fulfill)하라. 이렇게 하면 release gate가 깨지지 않고, 실제 Redis 통합은 별도 PR로 진행할 수 있다.

★ **SessionManager.Players 호환성**: Redis 모드에서도 `SessionManager.Players`는 로컬 `ConcurrentDictionary`를 반환해야 한다. 이것은 성능 최적화를 위한 로컬 캐시 역할이며, Redis에서 읽은 세션을 로컬에도 동기화하는 패턴을 사용하라. SessionManager 생성자의 `else` 분기(L36-39)가 이 시나리오를 위해 이미 준비되어 있다.

★ **기존 Local 구현체 보존**: InMemorySessionStore, LocalClusterRegistry, LocalEventBus는 수정하지 않는다. 이들은 단일 서버 모드에서 계속 사용된다. Redis 구현체는 별도 파일로 작성한다.

★ **DI 전파 경로**: ClusterDashboard는 IClusterRegistry, ISessionManager, IZoneManager, IGameSessionManager에 의존한다. Program.cs에서 DI 등록 순서에 주의하라. 기존 서비스가 모두 등록된 이후에 ClusterDashboard와 AutoScaleMonitor를 등록하라.

★ **ClusterNodeInfo 필드 추가 시 생성자 수정 금지**: Program.cs L43-50의 초기화 블록에 신규 필드 초기화를 추가하라.

★ **ScaleDecision enum은 별도 파일이 아닌 AutoScaleMonitor.cs 내부에 정의**: 외부에서 참조할 필요가 없으므로 같은 파일에 둔다.

★ **Tick 핸들러 등록 위치**: 모든 신규 Tick 핸들러는 Program.cs의 기존 gameLoop.Start() 호출 이전에 등록하라 (L74 이전). 기존 패턴(L62-68, L71-72)을 따른다.

★ **DashboardSnapshot은 SDK가 아닌 Runtime에 배치**: 이 모델은 서버 내부 운영 도구용이므로 SDK에 노출할 필요 없다. TeruTeruServer.Runtime/Clustering/ 내에 배치하라.
