# Milestone 5: Scalability & Clustering — 작업자 지시문

## 목표

Documents/구현계획.md에 명시된 Milestone 5 (Scalability & Clustering)의 5개 작업 항목을 구현한다. 마일스톤 범위를 벗어난 확장은 하지 않는다.

> **중요**: M5의 핵심은 "인터페이스 추상화 + 로컬 기본 구현"이다. 실제 Redis/RabbitMQ 연동은 M5 범위 밖이며, 추후 구현체만 교체하면 분산 환경이 동작하도록 구조를 설계하는 것이 목표다.

## 선행 조건 확인

Milestone 4가 커밋 완료되었다. 현재 브랜치: feature/phase2-architecture, HEAD: f48ebeb

구현 시작 전 반드시 아래 명령으로 현재 상태를 확인하라.

```bash
git status --short --branch
git log --oneline -3
```

## 기존 코드 파악 (필독)

### 핵심 파일

- **TeruTeruServer.SDK/Interfaces/ISessionManager.cs** (60줄)
  - `ISessionManager` 인터페이스 (L11-18): `Players` (ConcurrentDictionary), `TryAddPlayer`, `EvictSession`, `MarkAsGrace`, `TryGetHostIdBySocket`
  - `SessionManager` 구현 (L20-58): 순수 in-memory. `Players` 프로퍼티가 `ConcurrentDictionary<int, ClientSession>`을 직접 노출
  - ★ 문제: `ISessionManager.Players`가 `ConcurrentDictionary`를 직접 반환하므로, 외부 저장소로 교체 시 인터페이스 자체가 장벽이 된다. 그러나 기존 코드 호환성 유지를 위해 Players 프로퍼티는 유지하되, 내부적으로 ISessionStore를 사용하도록 래핑한다.

- **TeruTeruServer.SDK/Util/ServerMemory.cs** (153줄)
  - 완전 static 클래스. `_hosts`, `_gameID2HostID`, 이미지 큐 등 모든 상태가 static.
  - `ClientSession` 생성자(ClientSession.cs L53-58)에서 `ServerMemory.AddHostToDictionary()`를 직접 호출.
  - ★ 주의: ServerMemory.cs를 직접 수정하거나 제거하지 않는다. 새로운 ISessionStore 경로와 공존시킨다.

- **TeruTeruServer.SDK/Util/ClientSession.cs** (77줄)
  - `ReconnectToken` (L30): `Guid.NewGuid().ToString("N")`으로 생성.
  - `P2PState`, `IsAuthenticated`, `RttMs` 등 M2/M3에서 추가된 필드 포함.

- **TeruTeruServer.Runtime/Pipeline/AuthMiddleware.cs** (154줄)
  - `HandleReconnect()` (L97-130): `_sessionManager.Players.TryGetValue(request.HostID, ...)` 로 로컬에서만 검색.
  - ★ 핵심 수정 대상: HostID 로컬 검색 실패 시 ISessionStore를 통해 재조회하는 폴백 추가.

- **TeruTeruServer.Logic.Default/P2P/P2PGroupHandler.cs** (149줄)
  - `HandleJoinGroup()`, `HandleGroupRelay()`: 로컬 `_groups` 딕셔너리에서만 그룹 관리.
  - 그룹 이벤트(OnMemberJoined 등)가 발생하지만 다른 서버 인스턴스로 전파되지 않음.

- **TeruTeruServer.Cli/Program.cs** (76줄)
  - DI 등록: `services.AddSingleton<ISessionManager, SessionManager>()` (L49)
  - 새 인터페이스(ISessionStore, IEventBus, IClusterRegistry)도 여기에 등록해야 함.

## 작업 항목 (5건)

### 작업 1: ISessionStore 백엔드 추상화

**파일 범위:**
- [NEW] TeruTeruServer.SDK/Interfaces/ISessionStore.cs
- [NEW] TeruTeruServer.SDK/Clustering/InMemorySessionStore.cs
- [MODIFY] TeruTeruServer.SDK/Interfaces/ISessionManager.cs (SessionManager 구현부)
- [MODIFY] TeruTeruServer.Cli/Program.cs (DI 등록)

**구현 내용:**
1. `ISessionStore` 인터페이스를 SDK에 신설하라:
   ```csharp
   public interface ISessionStore
   {
       bool TryAdd(int hostId, ClientSession session);
       bool TryGet(int hostId, out ClientSession session);
       bool TryRemove(int hostId, out ClientSession session);
       ClientSession? FindByReconnectToken(string token);
       IEnumerable<ClientSession> GetAll();
   }
   ```
2. `InMemorySessionStore`를 구현하라. 내부적으로 `ConcurrentDictionary<int, ClientSession>`을 사용한다. `FindByReconnectToken()`은 전체 Values를 순회하여 매칭한다.
3. `SessionManager` 구현체가 생성자에서 `ISessionStore`를 주입받도록 수정하라. `Players` 프로퍼티는 유지하되, 내부 저장/조회를 `ISessionStore`에 위임하라. 기존 코드 호환성을 위해 `Players`는 여전히 `ConcurrentDictionary`를 반환하되, `InMemorySessionStore`의 내부 딕셔너리를 참조하도록 한다.
4. `Program.cs`에 `services.AddSingleton<ISessionStore, InMemorySessionStore>()`를 추가하라.

---

### 작업 2: 서버 간 Grace 재연결 지원

**파일 범위:**
- [MODIFY] TeruTeruServer.Runtime/Pipeline/AuthMiddleware.cs

**구현 내용:**
1. `AuthMiddleware`가 생성자에서 `ISessionStore`를 추가로 주입받도록 하라.
2. `HandleReconnect()` (L105)에서 `_sessionManager.Players.TryGetValue()` 실패 시, `_sessionStore.FindByReconnectToken(request.ReconnectToken)`으로 폴백 검색을 수행하라.
3. 폴백 성공 시 동일한 재연결 로직(소켓 덮어씌우기, 상태 복원)을 수행하라.
4. 폴백도 실패 시 기존과 동일하게 소켓을 닫는다.

**주의:**
- `AuthMiddleware`의 생성자 시그니처가 변경되므로, `MainServer.cs`에서 생성하는 부분도 확인하라.

---

### 작업 3: 분산 이벤트 버스 인터페이스

**파일 범위:**
- [NEW] TeruTeruServer.SDK/Interfaces/IEventBus.cs
- [NEW] TeruTeruServer.SDK/Clustering/LocalEventBus.cs
- [MODIFY] TeruTeruServer.Logic.Default/P2P/P2PGroupHandler.cs
- [MODIFY] TeruTeruServer.Cli/Program.cs (DI 등록)

**구현 내용:**
1. `IEventBus` 인터페이스를 SDK에 신설하라:
   ```csharp
   public interface IEventBus
   {
       void Publish<T>(string channel, T message);
       void Subscribe<T>(string channel, Action<T> handler);
       void Unsubscribe(string channel);
   }
   ```
2. `LocalEventBus`를 구현하라. `ConcurrentDictionary<string, List<Delegate>>`로 구독자를 관리하고, Publish 시 동일 프로세스 내 구독자에게 동기적으로 전달한다.
3. `P2PGroupHandler`가 생성자에서 `IEventBus`를 주입받도록 수정하라. `HandleJoinGroup()` 완료 시 `_eventBus.Publish("p2p.group.join", new { GroupId, HostId })` 형태로 이벤트를 발행하라. `HandleGroupRelay()` 시에도 유사하게 발행하라.
4. `Program.cs`에 `services.AddSingleton<IEventBus, LocalEventBus>()`를 추가하라.

---

### 작업 4: 클러스터 노드 레지스트리

**파일 범위:**
- [NEW] TeruTeruServer.SDK/Interfaces/IClusterRegistry.cs
- [NEW] TeruTeruServer.SDK/Clustering/ClusterNodeInfo.cs
- [NEW] TeruTeruServer.SDK/Clustering/LocalClusterRegistry.cs
- [MODIFY] TeruTeruServer.Cli/Program.cs (DI 등록)

**구현 내용:**
1. `ClusterNodeInfo` 모델을 신설하라:
   ```csharp
   public class ClusterNodeInfo
   {
       public string NodeId { get; set; }
       public string Address { get; set; }
       public int Port { get; set; }
       public string Status { get; set; } // "Active", "Draining", "Down"
       public DateTime LastHeartbeat { get; set; }
   }
   ```
2. `IClusterRegistry` 인터페이스를 신설하라:
   ```csharp
   public interface IClusterRegistry
   {
       void RegisterNode(ClusterNodeInfo node);
       void DeregisterNode(string nodeId);
       ClusterNodeInfo? GetNode(string nodeId);
       IReadOnlyList<ClusterNodeInfo> GetActiveNodes();
       void UpdateHeartbeat(string nodeId);
   }
   ```
3. `LocalClusterRegistry`를 구현하라. 자기 자신만 등록하는 단일 노드 구현이다. `RegisterNode` 시 `ConcurrentDictionary`에 저장하고, `GetActiveNodes()`는 Status가 "Active"인 노드만 반환한다.
4. `Program.cs`에 `services.AddSingleton<IClusterRegistry, LocalClusterRegistry>()`를 추가하고, 서버 시작 시 자기 자신을 등록하라.

---

### 작업 5: Stateless 설계 가이드 문서화

**파일 범위:**
- [NEW] Documents/Technical/Stateless_Design_Guide.md

**구현 내용:**
1. 아래 섹션을 포함하는 가이드를 작성하라:
   - 세션 외부화 원칙 (ISessionStore가 왜 필요한가)
   - 로드밸런서 설정 권장사항 (L4 vs L7, 스티키 세션 회피)
   - ReconnectToken 기반 분산 재연결 흐름도
   - IEventBus를 통한 P2P 그룹 이벤트 전파 아키텍처
   - Redis/RabbitMQ 실제 연동 시 교체 포인트 안내
2. 문서는 한국어로 작성하되, 코드 예제는 C#으로 포함하라.

## 변경 허용 범위

**허용:**
- TeruTeruServer.SDK/Interfaces/ 하위 신규 파일 생성
- TeruTeruServer.SDK/Clustering/ 디렉토리 신설 및 하위 파일 생성
- TeruTeruServer.SDK/Interfaces/ISessionManager.cs 수정 (SessionManager 구현부만)
- TeruTeruServer.Runtime/Pipeline/AuthMiddleware.cs 수정 (HandleReconnect 폴백만)
- TeruTeruServer.Runtime/MainServer.cs 수정 (AuthMiddleware 생성자 변경 시)
- TeruTeruServer.Logic.Default/P2P/P2PGroupHandler.cs 수정 (IEventBus 주입만)
- TeruTeruServer.Cli/Program.cs 수정 (DI 등록만)
- Documents/Technical/ 하위 신규 파일 생성
- 테스트 파일 추가
- IMPLEMENTATION_PROGRESS.md 갱신

**금지:**
- .agents/ 수정 금지
- TeruTeruServer.SDK/Util/ServerMemory.cs 수정 금지 (공존 원칙)
- 커밋/푸시 금지
- release gate 기준(scripts/verify-release.sh) 변경 금지
- 기존 M2/M3/M4 로직 변경 금지
- 기존 통과 중인 테스트 삭제 또는 약화 금지
- 실제 Redis/RabbitMQ 패키지 설치 금지

## 검증

1. `./scripts/verify-release.sh` 통과 (오류 0개 필수)
2. 신규 파일이 untracked로 방치되지 않도록 git add (파일 단위 명시만 허용)
3. IMPLEMENTATION_PROGRESS.md가 실제 구현 상태와 일치하는지 확인
4. git status로 변경/신규 파일 목록 보고

## 최종 보고 형식

1. 전체 완료 여부
2. 이번 완료 범위 (5개 작업 항목별)
3. 변경 파일 목록 (git status 기준)
4. 새 파일 분류 (git add 완료 여부 포함)
5. 핵심 구현 요약 (작업별 1~2줄)
6. 공식 release gate 결과 (verify-release.sh 출력)
7. 남은 리스크
8. 커밋/푸시 여부: 수행하지 않음

## 특별 주의사항

★ `ISessionManager.Players` 프로퍼티는 `ConcurrentDictionary<int, ClientSession>`을 반환한다. 이 프로퍼티를 사용하는 코드가 프로젝트 전체에 15곳 이상 존재한다. Players를 제거하거나 타입을 변경하면 대규모 빌드 실패가 발생한다. InMemorySessionStore 내부의 ConcurrentDictionary를 SessionManager.Players에서 그대로 참조하도록 설계하여 기존 코드를 깨뜨리지 않는다.

★ `ServerMemory.cs`는 수정하지 않는다. ClientSession 생성자에서 ServerMemory.AddHostToDictionary()를 호출하는 기존 로직은 유지한다. ISessionStore는 ServerMemory와 별개의 새로운 경로이며, 두 경로가 당분간 공존한다.

★ `AuthMiddleware` 생성자 변경 시 MainServer.cs에서 AuthMiddleware를 생성하는 코드를 반드시 확인하라. DI가 아닌 직접 생성(new)일 수 있다.

★ `P2PGroupHandler` 생성자에 IEventBus 파라미터를 추가하면, LogicPlugin.cs에서 P2PGroupHandler를 생성하는 코드도 수정해야 한다. IEventBus를 LogicPlugin 생성자까지 전파하거나, IServiceProvider를 통해 해결하라.
