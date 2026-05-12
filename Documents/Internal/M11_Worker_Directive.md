# Milestone 11: Game Session & Matchmaking — 작업자 지시문

## 목표

Documents/구현계획.md에 명시된 Milestone 11의 5개 작업 항목을 구현한다.
이 마일스톤은 플레이어가 게임에 참여하는 전체 흐름(로비 → 매치 → 게임 → 결과)을 서버에서 관리하는 구조를 구축한다.

> **중요**: 핵심은 "게임 세션의 전체 생명주기를 서버가 관리하고, 매치메이킹부터 결과 처리까지 자동화하는 것"이다. 각 상태 전이가 명확하고, 이벤트가 Logic Plugin에 전달되어야 한다.

## 선행 조건 확인

Milestone 10이 커밋 완료되었다. 현재 브랜치: feature/phase2-architecture, HEAD: a68a514

```
git status --short --branch
git log --oneline -3
```

## 기존 코드 파악 (필독)

### M7-M10에서 구축한 핵심 인프라

- **TeruTeruServer.SDK/GameEngine/RoomState.cs** (26줄)
  - L13: `RoomId`, L18: `ParticipantHostIds (List<int>)`, L23: `CurrentState (WorldState)`
  - ★ RoomState는 단순 데이터 클래스. GameSession이 RoomState를 포함(has-a)하는 구조로 설계하라.
  - ★ RoomState 자체는 수정하지 않는다.

- **TeruTeruServer.Runtime/GameEngine/RoomBroadcaster.cs** (61줄)
  - L15: `ConcurrentDictionary<int, RoomState> _rooms`
  - L33-57: `BroadcastToRoom` — ParticipantHostIds 복사 후 순회 전송
  - ★ 관전자에게도 브로드캐스트해야 하므로, GameSession에 SpectatorHostIds를 별도로 관리하고 BroadcastToRoom 호출 시 관전자 목록을 추가하거나, 별도 BroadcastToSession 메서드를 만들어라. IRoomBroadcaster 시그니처는 변경하지 않는다.

- **TeruTeruServer.SDK/Interfaces/IEventBus.cs** (26줄)
  - L13: `Publish<T>(string channel, T message)` — 이벤트 발행
  - L18: `Subscribe<T>(string channel, Action<T> handler)` — 이벤트 구독
  - ★ OnGameEnd 이벤트를 "game:end" 채널로 발행하면 Logic Plugin에서 구독하여 결과 처리.

- **TeruTeruServer.SDK/Interfaces/IGameLoop.cs** (46줄)
  - L38: `RegisterTickHandler(Action<long> handler)`
  - ★ MatchQueue의 주기적 매칭 시도는 Tick 핸들러로 등록 (예: 매 20 Tick = 1초마다).

- **TeruTeruServer.SDK/Util/ClientSession.cs** (93줄)
  - L29: `SessionState State` — Connected, Grace, Disconnected
  - ★ ClientSession에 MMR/Rating 필드를 추가한다. 생성자 수정 금지 (기본값 사용).

- **TeruTeruServer.SDK/Enums/ProtocolEnums.cs** (49줄)
  - 사용 중: 1-10, 20-28, 100-102
  - ★ 신규:
    - `MatchmakingProtocol = 29` — 매치메이킹 요청/응답
    - `SessionStateProtocol = 30` — 세션 상태 변경 알림

- **TeruTeruServer.Cli/Program.cs** (130줄)
  - L113-128: Game Engine + Security DI 등록
  - ★ IGameSessionManager, MatchQueue DI 등록 추가

### 테스트 현황
- 총 42개 테스트 통과 중. 모두 유지해야 한다.

## 작업 항목 (5건)

### 작업 1: 매치메이킹 큐 시스템

**파일 범위:**
- [NEW] TeruTeruServer.SDK/GameEngine/MatchEntry.cs
- [NEW] TeruTeruServer.Runtime/GameEngine/MatchQueue.cs

**구현 내용:**
1. `MatchEntry` 모델:
   ```csharp
   public class MatchEntry
   {
       public int HostId { get; set; }
       public int Mmr { get; set; }
       public DateTime EnqueuedUtc { get; set; } = DateTime.UtcNow;
   }
   ```
2. `MatchQueue` 구현:
   ```csharp
   public class MatchQueue
   {
       private readonly ConcurrentQueue<MatchEntry> _queue = new();
       private readonly IGameSessionManager _sessionManager;
       private readonly int _teamSize;
       private readonly int _teamCount;
       private readonly int _mmrRange;
       
       public MatchQueue(IGameSessionManager sessionManager, int teamSize = 4, int teamCount = 2, int mmrRange = 200)
       
       public void Enqueue(MatchEntry entry);
       public bool Dequeue(int hostId);  // 큐 취소
       public int QueueLength { get; }
       
       /// <summary>
       /// Tick 핸들러에서 호출. 매칭 조건이 맞으면 GameSession 생성.
       /// </summary>
       public void TryMatch();
   }
   ```
   - `TryMatch`: 큐에서 `teamSize * teamCount`명이 모이면, MMR 범위 내의 플레이어를 그룹화하여 GameSession 생성
   - ★ 대기 시간이 길어지면 MMR 범위를 자동 확대하는 로직 권장 (30초마다 +100)
   - Tick 핸들러 등록: `gameLoop.RegisterTickHandler(tick => { if (tick % 20 == 0) matchQueue.TryMatch(); })`

---

### 작업 2: 게임 세션 생명주기 관리

**파일 범위:**
- [NEW] TeruTeruServer.SDK/GameEngine/GameSession.cs
- [NEW] TeruTeruServer.SDK/GameEngine/GameSessionState.cs (enum)
- [NEW] TeruTeruServer.SDK/Interfaces/IGameSessionManager.cs
- [NEW] TeruTeruServer.Runtime/GameEngine/GameSessionManager.cs

**구현 내용:**
1. `GameSessionState` enum:
   ```csharp
   public enum GameSessionState
   {
       Lobby,
       MatchFound,
       Loading,
       InGame,
       Result,
       Disbanded
   }
   ```
2. `GameSession` 모델:
   ```csharp
   public class GameSession
   {
       public int SessionId { get; set; }
       public GameSessionState State { get; set; } = GameSessionState.Lobby;
       public List<int> PlayerHostIds { get; set; } = new();
       public List<int> SpectatorHostIds { get; set; } = new();
       public Dictionary<int, int> TeamAssignments { get; set; } = new(); // hostId -> teamId
       public RoomState Room { get; set; } = new();
       public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
       public int WinningTeamId { get; set; } = -1;
   }
   ```
3. `IGameSessionManager` 인터페이스:
   ```csharp
   public interface IGameSessionManager
   {
       GameSession CreateSession(List<MatchEntry> players, int teamCount);
       bool TransitionState(int sessionId, GameSessionState newState);
       GameSession? GetSession(int sessionId);
       GameSession? GetPlayerSession(int hostId);
       bool AddSpectator(int sessionId, int hostId);
       bool RemovePlayer(int sessionId, int hostId);
       bool RejoinPlayer(int sessionId, int hostId);
       IReadOnlyList<GameSession> GetActiveSessions();
   }
   ```
4. `GameSessionManager` 구현:
   - `ConcurrentDictionary<int, GameSession> _sessions`
   - `ConcurrentDictionary<int, int> _playerSessionMap` (hostId → sessionId 역조회)
   - `CreateSession`: 팀 배정 → 상태 MatchFound → RoomState 생성 → RoomBroadcaster 등록
   - `TransitionState`: 상태 전이 검증 (Lobby→MatchFound→Loading→InGame→Result→Disbanded)
     - InGame→Result 전이 시 `OnGameEnd` 이벤트 발행
     - Result→Disbanded 전이 시 자원 정리
   - `RejoinPlayer`: Grace 상태의 플레이어가 복귀 시 기존 세션에 재입장
   - ★ 상태 전이는 단방향만 허용 (역전이 금지). enum 순서로 비교.

---

### 작업 3: 팀 구성 및 밸런싱

**파일 범위:**
- [NEW] TeruTeruServer.Runtime/GameEngine/TeamBalancer.cs

**구현 내용:**
1. `TeamBalancer` 클래스:
   ```csharp
   public class TeamBalancer
   {
       /// <summary>
       /// MMR 기반으로 팀을 균등 배정합니다. 스네이크 드래프트 방식.
       /// </summary>
       public Dictionary<int, int> AssignTeams(List<MatchEntry> players, int teamCount)
       {
           // 1. MMR 내림차순 정렬
           // 2. 스네이크 드래프트: 1→2→...→N→N→...→2→1 순서로 배정
           // 3. 결과: Dictionary<hostId, teamId>
       }
   }
   ```
   - 스네이크 드래프트: MMR 1위→팀1, 2위→팀2, 3위→팀2, 4위→팀1 (2팀 기준)
   - 팀 간 평균 MMR 차이를 최소화하는 것이 목표

---

### 작업 4: 관전자(Spectator) 모드 지원

**파일 범위:**
- [MODIFY] GameSessionManager (작업 2에서 생성)

**구현 내용:**
1. `GameSession.SpectatorHostIds`에 관전자 관리
2. `AddSpectator(int sessionId, int hostId)`:
   - InGame 상태인 세션에만 관전자 추가 허용
   - ParticipantHostIds에는 추가하지 않음 (입력 전송 불가)
   - 브로드캐스트 시 관전자에게도 전송
3. 관전자 퇴장: `RemovePlayer`와 유사하나 SpectatorHostIds에서 제거
4. ★ 관전자는 RoomState.ParticipantHostIds에 포함되지 않는다. 별도 관리.

---

### 작업 5: 게임 결과 처리 및 보상 훅

**파일 범위:**
- [NEW] TeruTeruServer.SDK/GameEngine/GameResult.cs
- [MODIFY] GameSessionManager (OnGameEnd 이벤트 발행)
- [MODIFY] TeruTeruServer.SDK/Enums/ProtocolEnums.cs

**구현 내용:**
1. `GameResult` 모델:
   ```csharp
   public class GameResult
   {
       public int SessionId { get; set; }
       public int WinningTeamId { get; set; }
       public Dictionary<int, int> PlayerTeams { get; set; } = new(); // hostId -> teamId
       public TimeSpan Duration { get; set; }
       public DateTime EndedUtc { get; set; } = DateTime.UtcNow;
   }
   ```
2. GameSessionManager `TransitionState(sessionId, InGame → Result)` 시:
   ```csharp
   var result = new GameResult { SessionId = sessionId, WinningTeamId = session.WinningTeamId, ... };
   _eventBus.Publish("game:end", result);
   ```
3. `ProtocolSelect`에 추가:
   ```csharp
   MatchmakingProtocol = 29,     // 매치메이킹 요청/응답 (M11)
   SessionStateProtocol = 30,    // 세션 상태 변경 알림 (M11)
   ```
4. ★ Logic Plugin에서 `_eventBus.Subscribe<GameResult>("game:end", result => { /* DB 기록, MMR 갱신 등 */ })` 패턴으로 활용.

## 변경 허용 범위

**허용:**
- TeruTeruServer.SDK/GameEngine/ 하위 신규 파일 생성
- TeruTeruServer.SDK/Interfaces/ 하위 신규 파일 생성
- TeruTeruServer.Runtime/GameEngine/ 하위 신규 파일 생성
- TeruTeruServer.SDK/Enums/ProtocolEnums.cs 수정 (신규 enum 값 추가만)
- TeruTeruServer.SDK/Util/ClientSession.cs 수정 (MMR 필드 추가만)
- TeruTeruServer.Cli/Program.cs 수정 (DI 등록 추가만)
- 테스트 프로젝트에 신규 테스트 파일 생성
- IMPLEMENTATION_PROGRESS.md 갱신

**금지:**
- .agents/ 수정 금지
- RoomState 기존 필드/타입 변경 금지
- IRoomBroadcaster 시그니처 변경 금지
- ISessionManager.Players 타입 변경 금지
- IPacketMiddleware 인터페이스 변경 금지
- 기존 42개 테스트 삭제/약화 금지
- 커밋/푸시 금지
- release gate 변경 금지
- ServerMemory.cs 수정 금지
- GameLoop.cs 수정 금지

## 검증

1. `./scripts/verify-release.sh` 통과 (기존 42개 + 신규 테스트)
2. 매치메이킹 테스트:
   - 8명(4v4) 큐 등록 → TryMatch → GameSession 생성 확인
   - MMR 차이가 범위(200) 이내인 플레이어만 매칭 확인
3. 세션 생명주기 테스트:
   - Lobby → MatchFound → Loading → InGame → Result → Disbanded 전체 순회
   - 역전이 시도 시 실패 확인
4. 팀 밸런싱 테스트:
   - 8명 스네이크 드래프트 후 팀 간 평균 MMR 차이 검증
5. 관전자 테스트:
   - InGame 세션에 관전자 추가 → PlayerHostIds에 미포함 확인
   - Lobby 세션에 관전자 추가 시도 → 실패 확인
6. 게임 결과 테스트:
   - InGame→Result 전이 시 IEventBus.Publish 호출 확인

## 특별 주의사항

★ **상태 전이는 단방향이다**: GameSessionState enum 순서(0=Lobby, 1=MatchFound, ..., 5=Disbanded)를 활용하여 `newState > currentState`만 허용하라.

★ **역조회 인덱스 필수**: `_playerSessionMap`으로 hostId→sessionId를 O(1)으로 조회해야 한다. 매번 모든 세션의 PlayerHostIds를 순회하면 동접 증가 시 성능 문제.

★ **관전자는 ParticipantHostIds에 포함하지 않는다**: RoomBroadcaster는 ParticipantHostIds 기반으로 전송한다. 관전자에게 전송하려면 GameSessionManager가 별도로 `IMessageSender.SendData`를 호출하거나, 브로드캐스트 시 SpectatorHostIds도 함께 전송하는 래핑 메서드를 만들어라.

★ **Grace 재연결과 세션 복귀**: 플레이어가 Grace 상태에서 재연결하면 `RejoinPlayer`로 기존 세션에 복귀. InGame 상태가 아닌 세션(Result, Disbanded)에는 복귀 불가.

★ **DI 전파 경로**: MatchQueue는 IGameSessionManager에 의존하고, GameSessionManager는 IEventBus와 IRoomBroadcaster에 의존한다. Program.cs에서 등록 순서에 주의하라 (IRoomBroadcaster → IGameSessionManager → MatchQueue).

★ **ConcurrentQueue 사용**: MatchQueue의 _queue는 IOCP 스레드에서 Enqueue, Tick 스레드에서 TryMatch가 호출되므로 ConcurrentQueue를 사용한다. 단, TryMatch에서 MMR 필터링을 위해 큐 전체를 임시 리스트로 꺼내서 처리하고, 매칭 안 된 플레이어를 다시 큐에 넣는 패턴을 사용하라.
