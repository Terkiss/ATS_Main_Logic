# Milestone 7: Real-time Tick & State Sync — 작업자 지시문

## 목표

Documents/구현계획.md에 명시된 Milestone 7의 5개 작업 항목을 구현한다.
이 마일스톤은 Phase 2 (Game Server Edition)의 첫 번째 마일스톤으로, 실시간 게임 서버의 핵심인 고정 주기 Tick Loop와 상태 동기화를 구축한다.

> **중요**: 이 마일스톤의 핵심은 "서버가 능동적으로 게임 상태를 갱신하고 클라이언트에 전송하는 구조"를 만드는 것이다. 현재 서버는 수신 이벤트 기반(Reactive)으로만 동작하며, 능동적 Tick 루프가 없다.

## 선행 조건 확인

Milestone 6이 커밋 완료되었다. 현재 브랜치: feature/phase2-architecture, HEAD: b74a2da

구현 시작 전 반드시 아래 명령으로 현재 상태를 확인하라.

```
git status --short --branch
git log --oneline -3
```

## 기존 코드 파악 (필독)

### 핵심 파일

- **TeruTeruServer.Cli/Program.cs** (99줄)
  - L58-96: `ConfigureServices()` — DI 컨테이너 구성.
  - L60-63: ISessionStore, ISessionManager, IEventBus, IClusterRegistry 등록.
  - L73-81: ILogicService 팩토리 등록 — `LogicPlugin(sender, db, session, router, bus)`.
  - L89-95: MainServer 팩토리 등록.
  - ★ IGameLoop DI 등록과 MainServer.StartServer() 후 GameLoop.Start() 호출을 추가해야 한다.

- **TeruTeruServer.Runtime/MainServer.cs** (535줄)
  - L96: `StartServer()` — 서버 소켓 리슨 시작. 여기서 GameLoop도 함께 시작하는 것이 자연스럽다.
  - L86-93: `InitializePipeline()` — 미들웨어 파이프라인. Tick Loop는 파이프라인과 독립적으로 동작한다.
  - ★ MainServer는 IMessageSender를 구현하고 있으므로, GameLoop에서 브로드캐스트 시 IMessageSender를 활용할 수 있다.

- **TeruTeruServer.SDK/Enums/ProtocolEnums.cs** (41줄)
  - L9-25: `ProtocolSelect` enum (byte 타입, 현재 1~10, 100~102 사용 중).
  - ★ 신규 프로토콜 번호 배정:
    - `StateSyncProtocol = 20` — Delta 상태 동기화
    - `GameInputProtocol = 22` — 클라이언트 게임 입력
    - ★ 기존 번호와 충돌하지 않도록 20번대를 게임 전용으로 예약하라.

- **TeruTeruServer.SDK/Util/ClientSession.cs** (77줄)
  - L18-43: 세션 필드. HostID, GameID, Socket, RttMs 등.
  - ★ ClientSession에 Tick 관련 필드를 추가하지 마라. 게임 엔티티 상태는 별도 `GameEntity` 모델에서 관리한다.

- **TeruTeruServer.Logic.Default/P2P/P2PGroupHandler.cs** (153줄)
  - L19-30: P2PGroupHandler 구조 — ISessionManager + IEventBus + ConcurrentDictionary<int, P2PGroup>.
  - L32-37: `CreateGroup()` — 그룹 생성. 이 P2PGroup을 게임 룸의 기반으로 활용할 수 있다.
  - ★ P2PGroup.Members 목록을 활용하여 룸 내 브로드캐스트 대상을 결정한다.

- **TeruTeruServer.SDK/Interfaces/ISessionManager.cs** (94줄)
  - L13: `ConcurrentDictionary<int, ClientSession> Players` — 전체 접속 플레이어 목록.
  - ★ Players 프로퍼티 타입을 절대 변경하지 마라 (M5 원칙 계승).

### 테스트 프로젝트 현황
- `TeruTeruServer.SDK.Tests` (13개)
- `TeruTeruServer.Runtime.Tests` (7개, 통합 테스트 포함)
- `TeruTeruServer.Logic.Default.Tests` (2개)
- ★ 총 22개 기존 테스트가 모두 통과해야 한다.

## 작업 항목 (5건)

### 작업 1: 서버 Tick Loop 구현

**파일 범위:**
- [NEW] TeruTeruServer.SDK/Interfaces/IGameLoop.cs
- [NEW] TeruTeruServer.Runtime/GameEngine/GameLoop.cs
- [MODIFY] TeruTeruServer.Cli/Program.cs (DI 등록 + Start 호출)

**구현 내용:**
1. `IGameLoop` 인터페이스를 SDK에 신설하라:
   ```csharp
   public interface IGameLoop
   {
       int TickRate { get; }           // Hz (예: 20)
       long CurrentTick { get; }       // 현재 Tick 번호
       bool IsRunning { get; }
       void Start();
       void Stop();
       void RegisterTickHandler(Action<long> handler);  // Tick 번호를 인자로 받는 콜백
       void UnregisterTickHandler(Action<long> handler);
   }
   ```
2. `GameLoop` 구현체를 Runtime/GameEngine/ 하위에 신설하라:
   - 별도 Thread에서 고정 간격 루프 실행.
   - `Stopwatch`를 사용하여 정밀한 Tick 간격 유지 (Thread.Sleep의 부정확성 보정).
   - Tick마다 등록된 모든 핸들러를 순차 호출.
   - TickRate는 생성자 파라미터로 받되, 기본값 20Hz.
   - IsBackground = true로 설정하여 메인 스레드 종료 시 함께 종료.
   - 에러 처리: 핸들러에서 예외 발생 시 로그만 남기고 다음 핸들러 계속 실행.
3. `Program.cs`에 DI 등록 추가:
   ```csharp
   services.AddSingleton<IGameLoop>(sp => new GameLoop(tickRate: 20));
   ```
4. MainServer.StartServer() 호출 직전 또는 직후에 GameLoop.Start() 호출.

---

### 작업 2: 게임 상태 스냅샷 구조 설계

**파일 범위:**
- [NEW] TeruTeruServer.SDK/GameEngine/GameEntity.cs
- [NEW] TeruTeruServer.SDK/GameEngine/WorldState.cs
- [NEW] TeruTeruServer.SDK/GameEngine/RoomState.cs
- [NEW] TeruTeruServer.SDK/GameEngine/SnapshotBuffer.cs

**구현 내용:**
1. `GameEntity` 모델:
   ```csharp
   public class GameEntity
   {
       public int EntityId { get; set; }
       public int OwnerHostId { get; set; }     // 소유 클라이언트 HostID (-1이면 서버 관할 NPC)
       public float X { get; set; }
       public float Y { get; set; }
       public float Z { get; set; }
       public float RotationY { get; set; }     // Y축 회전 (Yaw)
       public float VelocityX { get; set; }
       public float VelocityZ { get; set; }
       public string State { get; set; }        // "Idle", "Moving", "Attacking" 등
       public bool IsDirty { get; set; }        // Delta 계산용 플래그
   }
   ```
2. `WorldState` 모델:
   ```csharp
   public class WorldState
   {
       public long TickNumber { get; set; }
       public DateTime Timestamp { get; set; }
       public ConcurrentDictionary<int, GameEntity> Entities { get; set; }
       public WorldState DeepClone();           // 스냅샷 복제용
   }
   ```
3. `RoomState` 모델:
   ```csharp
   public class RoomState
   {
       public int RoomId { get; set; }
       public List<int> ParticipantHostIds { get; set; }
       public WorldState CurrentState { get; set; }
   }
   ```
4. `SnapshotBuffer` — 고정 크기 링 버퍼:
   ```csharp
   public class SnapshotBuffer
   {
       public SnapshotBuffer(int capacity);      // 예: 128 프레임
       public void Push(WorldState state);
       public WorldState? GetAtTick(long tickNumber);
       public WorldState? GetLatest();
   }
   ```
   - M8 (Lag Compensation)에서 과거 스냅샷을 참조하기 위해 필요.

---

### 작업 3: Delta Broadcast 구현

**파일 범위:**
- [NEW] TeruTeruServer.Runtime/GameEngine/DeltaCalculator.cs
- [MODIFY] TeruTeruServer.SDK/Enums/ProtocolEnums.cs (신규 프로토콜 추가)

**구현 내용:**
1. `DeltaCalculator` 정적 클래스:
   ```csharp
   public static class DeltaCalculator
   {
       public static List<GameEntity> CalculateDelta(WorldState previous, WorldState current);
       // IsDirty 플래그가 true인 엔티티만 반환
       // 비교 후 IsDirty를 false로 리셋
   }
   ```
2. `ProtocolSelect` enum에 추가:
   ```csharp
   StateSyncProtocol = 20,    // 서버 → 클라이언트: Delta 상태 동기화
   GameInputProtocol = 22,    // 클라이언트 → 서버: 게임 입력
   ```
   - ★ 기존 1~10, 21, 100~102 번호와 충돌하지 않도록 확인하라.
3. Delta 패킷 구조: JSON 직렬화 (향후 MessagePack 등으로 최적화 가능):
   ```json
   {
     "Tick": 12345,
     "Entities": [
       { "EntityId": 1, "X": 10.5, "Y": 0, "Z": 20.3, "State": "Moving" },
       ...
     ]
   }
   ```

---

### 작업 4: 브로드캐스트 최적화

**파일 범위:**
- [NEW] TeruTeruServer.SDK/Interfaces/IRoomBroadcaster.cs
- [NEW] TeruTeruServer.Runtime/GameEngine/RoomBroadcaster.cs

**구현 내용:**
1. `IRoomBroadcaster` 인터페이스:
   ```csharp
   public interface IRoomBroadcaster
   {
       void BroadcastToRoom(int roomId, byte[] packet);
       void BroadcastToRoom(int roomId, byte[] packet, int excludeHostId);
       void RegisterRoom(RoomState room);
       void UnregisterRoom(int roomId);
   }
   ```
2. `RoomBroadcaster` 구현체:
   - IMessageSender와 ISessionManager를 주입받아, RoomState.ParticipantHostIds를 기반으로 패킷 전송.
   - `BroadcastToRoom` 구현: ParticipantHostIds를 순회하며 IMessageSender.SendData(hostId, packet) 호출.
   - excludeHostId는 자기 자신에게 보내지 않을 때 사용 (이동 시 자신 제외 등).
3. DI 등록: `services.AddSingleton<IRoomBroadcaster, RoomBroadcaster>()`

---

### 작업 5: 입력 큐 구조

**파일 범위:**
- [NEW] TeruTeruServer.SDK/GameEngine/InputQueue.cs
- [NEW] TeruTeruServer.SDK/GameEngine/GameInput.cs

**구현 내용:**
1. `GameInput` 모델:
   ```csharp
   public class GameInput
   {
       public int HostId { get; set; }
       public long ClientTick { get; set; }     // 클라이언트가 보낸 Tick 번호
       public float MoveX { get; set; }
       public float MoveZ { get; set; }
       public float LookY { get; set; }         // 시선 방향 (Yaw)
       public string ActionType { get; set; }   // "Move", "Attack", "Jump" 등
   }
   ```
2. `InputQueue<T>` 제네릭 클래스:
   ```csharp
   public class InputQueue<T>
   {
       private readonly ConcurrentQueue<T> _queue = new();
       public void Enqueue(T input);
       public List<T> DrainAll();               // Tick 처리 시 모든 입력을 한 번에 꺼냄
       public int Count { get; }
   }
   ```
   - `DrainAll()`은 Tick 핸들러에서 호출하여 해당 Tick에 쌓인 모든 입력을 가져온다.
   - ConcurrentQueue를 사용하여 IOCP 수신 스레드와 Tick 스레드 간 Thread-safe 보장.

## 변경 허용 범위

**허용:**
- TeruTeruServer.SDK/Interfaces/ 하위 신규 파일 생성
- TeruTeruServer.SDK/GameEngine/ 디렉토리 신설 및 하위 파일 생성
- TeruTeruServer.Runtime/GameEngine/ 디렉토리 신설 및 하위 파일 생성
- TeruTeruServer.SDK/Enums/ProtocolEnums.cs 수정 (신규 enum 값 추가만)
- TeruTeruServer.Cli/Program.cs 수정 (DI 등록 + GameLoop.Start())
- 테스트 프로젝트에 신규 테스트 파일 생성
- IMPLEMENTATION_PROGRESS.md 갱신

**금지:**
- .agents/ 수정 금지
- ClientSession에 게임 엔티티 필드 추가 금지 (별도 GameEntity 모델 사용)
- ISessionManager.Players 타입 변경 금지
- 기존 인터페이스 시그니처 변경 금지 (추가만 허용)
- 기존 통과 중인 22개 테스트 삭제 또는 약화 금지
- 커밋/푸시 금지
- release gate 기준(scripts/verify-release.sh) 변경 금지
- ServerMemory.cs 수정 금지
- MainServer의 기존 IOCP 수신 로직 변경 금지

## 검증

1. `./scripts/verify-release.sh` 통과 (오류 0개 필수, 기존 22개 + 신규 테스트)
2. GameLoop 단위 테스트: 목표 Hz ±2ms 정밀도 확인
3. DeltaCalculator 단위 테스트: IsDirty 플래그 기반 변경 감지 확인
4. InputQueue 단위 테스트: DrainAll 동시성 안전성 확인
5. 신규 파일이 untracked로 방치되지 않도록 git add
6. IMPLEMENTATION_PROGRESS.md가 실제 구현 상태와 일치하는지 확인

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

★ **Thread Safety**: GameLoop 스레드와 IOCP 수신 스레드는 별개다. InputQueue는 ConcurrentQueue를 사용하고, WorldState.Entities는 ConcurrentDictionary를 사용하여 두 스레드 간 동시 접근을 안전하게 처리하라.

★ **ProtocolSelect 충돌 방지**: 현재 사용 중인 번호: 1-10, 21, 100-102. 게임 전용 프로토콜은 20번대(StateSyncProtocol=20, GameInputProtocol=22)를 사용하라. 단, TokenRefreshProtocol=21이 이미 있으므로 21은 피하라.

★ **Tick 정밀도**: Thread.Sleep()만으로는 정밀도가 부족하다. Stopwatch + SpinWait 조합으로 목표 간격을 맞춰라. 단, CPU 과점유 방지를 위해 SpinWait는 남은 시간이 1ms 이내일 때만 사용하라.

★ **기존 IOCP 수신 루프 비간섭**: MainServer의 기존 패킷 수신/파이프라인 처리 로직에 절대 손대지 마라. GameLoop는 완전히 독립적인 스레드로 동작해야 한다. 유일한 공유 지점은 IMessageSender를 통한 브로드캐스트와 InputQueue를 통한 입력 수신뿐이다.

★ **IsDirty 플래그 리셋 타이밍**: DeltaCalculator.CalculateDelta()에서 변경된 엔티티를 추출한 후 IsDirty를 false로 리셋하라. 리셋하지 않으면 매 Tick마다 전체 엔티티를 전송하게 된다.

★ **SnapshotBuffer의 DeepClone**: WorldState를 링 버퍼에 넣기 전에 반드시 DeepClone하라. 참조를 공유하면 과거 스냅샷이 현재 상태로 오염된다. M8 (Lag Compensation)에서 과거 상태 참조 정확성이 보장되어야 한다.
