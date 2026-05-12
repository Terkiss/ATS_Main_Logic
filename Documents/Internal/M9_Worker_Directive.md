# Milestone 9: Zone & World Management — 작업자 지시문

## 목표

Documents/구현계획.md에 명시된 Milestone 9의 5개 작업 항목을 구현한다.
이 마일스톤은 게임 공간을 계층적으로 관리하고, 관심 영역(AoI) 기반 브로드캐스트 최적화를 통해 대규모 동접 환경에서의 서버 부하를 대폭 감소시킨다.

> **중요**: 핵심은 "모든 플레이어에게 모든 엔티티를 전송하는 것이 아니라, 주변에 있는 엔티티만 전송하는 것"이다. AoI 필터링 없이는 동접 100명만 돼도 패킷 폭발이 발생한다.

## 선행 조건 확인

Milestone 8이 커밋 완료되었다. 현재 브랜치: feature/phase2-architecture, HEAD: e094d4d

```
git status --short --branch
git log --oneline -3
```

## 기존 코드 파악 (필독)

### M7-M8에서 구축한 핵심 인프라

- **TeruTeruServer.SDK/GameEngine/RoomState.cs** (26줄)
  - L8-24: RoomId, ParticipantHostIds (List<int>), CurrentState (WorldState).
  - ★ 현재 RoomState는 단순 데이터 클래스. Zone이 RoomState를 포함(has-a)하는 구조로 설계하라. RoomState 자체는 수정하지 않는다.

- **TeruTeruServer.SDK/Interfaces/IRoomBroadcaster.cs** (31줄)
  - L8-29: BroadcastToRoom, RegisterRoom, UnregisterRoom.
  - ★ AoI 필터링은 IRoomBroadcaster를 확장하지 말고, Zone 레벨에서 별도로 처리하라. IRoomBroadcaster는 "룸 전체 전송"용으로 유지.

- **TeruTeruServer.Runtime/GameEngine/RoomBroadcaster.cs** (61줄)
  - L15: `ConcurrentDictionary<int, RoomState> _rooms` — 룸 관리.
  - L46-48: BroadcastToRoom에서 ParticipantHostIds를 lock+복사 후 순회.

- **TeruTeruServer.SDK/GameEngine/GameEntity.cs** (67줄)
  - L11: EntityId, L16: OwnerHostId (-1이면 서버 NPC), L18-20: X/Y/Z.
  - ★ AoI에서 엔티티의 X/Z 좌표로 그리드 셀을 결정한다.

- **TeruTeruServer.SDK/GameEngine/WorldState.cs** (47줄)
  - L25: `ConcurrentDictionary<int, GameEntity> Entities`.
  - ★ Zone의 WorldState에는 해당 Zone에 속한 엔티티만 포함. Zone 간 이동 시 엔티티를 한 WorldState에서 다른 WorldState로 이전해야 한다.

- **TeruTeruServer.SDK/Interfaces/IGameLoop.cs** (46줄)
  - L38: `RegisterTickHandler(Action<long> handler)`.
  - ★ NPC 엔티티의 AI 갱신은 Tick 핸들러로 등록하여 매 Tick마다 실행한다.

- **TeruTeruServer.SDK/Enums/ProtocolEnums.cs** (46줄)
  - 사용 중: 1-10, 20-25, 100-102.
  - ★ 신규 프로토콜:
    - `ZoneTransferProtocol = 26` — Zone 이동 요청/응답
    - `ZoneInfoProtocol = 27` — Zone 정보 조회

- **TeruTeruServer.Cli/Program.cs** (~107줄)
  - L58-105: ConfigureServices. IGameLoop, IRoomBroadcaster 등록 포함.
  - ★ IZoneManager DI 등록을 추가해야 한다.

### 테스트 현황
- 총 32개 테스트 통과 중. 모두 유지해야 한다.

## 작업 항목 (5건)

### 작업 1: Zone / Room / Channel 계층 설계

**파일 범위:**
- [NEW] TeruTeruServer.SDK/GameEngine/Zone.cs
- [NEW] TeruTeruServer.SDK/GameEngine/GameWorld.cs
- [NEW] TeruTeruServer.SDK/Interfaces/IZoneManager.cs
- [NEW] TeruTeruServer.Runtime/GameEngine/ZoneManager.cs
- [MODIFY] TeruTeruServer.Cli/Program.cs (DI 등록)

**구현 내용:**
1. `Zone` 모델:
   ```csharp
   public class Zone
   {
       public int ZoneId { get; set; }
       public string ZoneName { get; set; } = "";
       public WorldState State { get; set; } = new();
       public List<int> PlayerHostIds { get; set; } = new();
       public bool IsInstance { get; set; }        // 인스턴스 던전 여부
       public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
   }
   ```
2. `GameWorld` 모델:
   ```csharp
   public class GameWorld
   {
       public ConcurrentDictionary<int, Zone> Zones { get; set; } = new();
       public string WorldName { get; set; } = "Default";
   }
   ```
3. `IZoneManager` 인터페이스:
   ```csharp
   public interface IZoneManager
   {
       Zone CreateZone(string name, bool isInstance = false);
       bool DestroyZone(int zoneId);
       Zone? GetZone(int zoneId);
       IReadOnlyList<Zone> GetAllZones();
       bool JoinZone(int zoneId, int hostId);
       bool LeaveZone(int zoneId, int hostId);
       Zone? GetPlayerZone(int hostId);
   }
   ```
4. `ZoneManager` 구현체:
   - GameWorld를 내부에서 관리.
   - JoinZone: PlayerHostIds에 추가 + 기본 GameEntity 생성 (OwnerHostId=hostId).
   - LeaveZone: PlayerHostIds에서 제거 + 엔티티 제거 + IsDirty.
   - GetPlayerZone: hostId → Zone 역조회 (ConcurrentDictionary<int, int> _playerZoneMap).
5. Program.cs에 DI 등록:
   ```csharp
   services.AddSingleton<IZoneManager, ZoneManager>();
   ```

---

### 작업 2: 공간 기반 관심 영역 (AoI)

**파일 범위:**
- [NEW] TeruTeruServer.Runtime/GameEngine/SpatialGrid.cs
- [NEW] TeruTeruServer.SDK/Interfaces/IAoIFilter.cs

**구현 내용:**
1. `IAoIFilter` 인터페이스:
   ```csharp
   public interface IAoIFilter
   {
       List<int> GetNearbyEntityIds(int zoneId, float x, float z, float radius);
       void UpdateEntityPosition(int zoneId, int entityId, float x, float z);
       void RemoveEntity(int zoneId, int entityId);
   }
   ```
2. `SpatialGrid` 구현체:
   ```csharp
   public class SpatialGrid : IAoIFilter
   {
       private readonly float _cellSize;
       // 키: (zoneId, cellX, cellZ), 값: HashSet<int> (entityIds)
       private readonly ConcurrentDictionary<(int, int, int), HashSet<int>> _cells = new();

       public SpatialGrid(float cellSize = 50f);

       public (int cellX, int cellZ) GetCellCoord(float x, float z);
       public List<int> GetNearbyEntityIds(int zoneId, float x, float z, float radius);
       public void UpdateEntityPosition(int zoneId, int entityId, float x, float z);
       public void RemoveEntity(int zoneId, int entityId);
   }
   ```
   - `GetCellCoord`: `(int)(x / cellSize), (int)(z / cellSize)`.
   - `GetNearbyEntityIds`: 중심 셀 + 인접 셀(3×3 그리드)의 엔티티 ID를 수집.
   - `UpdateEntityPosition`: 이전 셀에서 제거 → 새 셀에 추가 (셀이 변경된 경우만).
   - ★ HashSet에 대한 동시 접근은 lock 보호 필요. 셀 단위 lock 사용.
3. DI 등록: `services.AddSingleton<IAoIFilter, SpatialGrid>();`

---

### 작업 3: 동적 인스턴스 생성·소멸

**파일 범위:**
- [NEW] TeruTeruServer.Runtime/GameEngine/ZoneFactory.cs

**구현 내용:**
1. `ZoneFactory` 클래스:
   ```csharp
   public class ZoneFactory
   {
       private readonly IZoneManager _zoneManager;
       private readonly IAoIFilter _aoiFilter;

       public ZoneFactory(IZoneManager zoneManager, IAoIFilter aoiFilter);

       /// <summary>
       /// 던전 등 인스턴스 Zone을 생성합니다.
       /// </summary>
       public Zone CreateInstance(string templateName);

       /// <summary>
       /// 비어있는 인스턴스 Zone을 정리합니다.
       /// 빈 Zone을 찾아 DestroyZone 호출.
       /// </summary>
       public int CleanupEmptyInstances();
   }
   ```
   - `CreateInstance`: `_zoneManager.CreateZone(templateName, isInstance: true)`.
   - `CleanupEmptyInstances`: `GetAllZones().Where(z => z.IsInstance && z.PlayerHostIds.Count == 0 && (now - z.CreatedUtc) > TimeSpan.FromMinutes(1))` → DestroyZone.
   - ★ CleanupEmptyInstances를 Tick 핸들러로 등록하되, 매 Tick이 아니라 N Tick마다 실행 (예: 600 Tick = 30초@20Hz).

---

### 작업 4: Zone Transfer 프로토콜

**파일 범위:**
- [NEW] TeruTeruServer.SDK/GameEngine/ZoneTransferRequest.cs
- [MODIFY] TeruTeruServer.SDK/Enums/ProtocolEnums.cs

**구현 내용:**
1. `ZoneTransferRequest` 모델:
   ```csharp
   public class ZoneTransferRequest
   {
       public int HostId { get; set; }
       public int FromZoneId { get; set; }
       public int ToZoneId { get; set; }
       public float SpawnX { get; set; }     // 목적지 Zone에서의 스폰 위치
       public float SpawnZ { get; set; }
   }
   ```
2. `ProtocolSelect`에 추가:
   ```csharp
   ZoneTransferProtocol = 26,     // Zone 이동 요청/응답 (M9)
   ZoneInfoProtocol = 27,         // Zone 정보 조회 (M9)
   ```
3. ZoneManager에 `TransferPlayer` 메서드 추가:
   ```csharp
   public bool TransferPlayer(ZoneTransferRequest request)
   {
       // 1. FromZone에서 LeaveZone
       // 2. ToZone에 JoinZone
       // 3. 엔티티 위치를 SpawnX/Z로 설정
       // 4. 양쪽 Zone의 SpatialGrid 갱신
   }
   ```

---

### 작업 5: NPC/몬스터 엔티티 관리

**파일 범위:**
- [NEW] TeruTeruServer.SDK/GameEngine/ServerEntity.cs

**구현 내용:**
1. `ServerEntity` 모델 (GameEntity를 상속하거나 래핑):
   ```csharp
   public class ServerEntity : GameEntity
   {
       public string AiBehavior { get; set; } = "Idle";   // "Idle", "Patrol", "Chase", "Attack"
       public float PatrolRadius { get; set; } = 10f;
       public float SpawnX { get; set; }
       public float SpawnZ { get; set; }
       public int TargetHostId { get; set; } = -1;        // 추적 대상 (-1이면 없음)
       public float AggroRange { get; set; } = 15f;       // 어그로 범위

       public ServerEntity()
       {
           OwnerHostId = -1;  // 서버 관할
       }
   }
   ```
   - DeepClone() 오버라이드하여 ServerEntity 전용 필드도 복사.
   - ★ GameEntity.DeepClone()은 virtual이 아니므로, ServerEntity에서 `new` 키워드로 숨기거나 별도 Clone 메서드를 만들어라.
2. NPC 행동은 이 마일스톤에서 모델만 정의. 실제 AI 로직은 향후 Logic Plugin에서 Tick 핸들러로 구현.

## 변경 허용 범위

**허용:**
- TeruTeruServer.SDK/GameEngine/ 하위 신규 파일 생성
- TeruTeruServer.SDK/Interfaces/ 하위 신규 파일 생성
- TeruTeruServer.Runtime/GameEngine/ 하위 신규 파일 생성
- TeruTeruServer.SDK/Enums/ProtocolEnums.cs 수정 (신규 enum 값 추가만)
- TeruTeruServer.Cli/Program.cs 수정 (DI 등록 추가만)
- 테스트 프로젝트에 신규 테스트 파일 생성
- IMPLEMENTATION_PROGRESS.md 갱신

**금지:**
- .agents/ 수정 금지
- RoomState 기존 필드/타입 변경 금지
- IRoomBroadcaster 시그니처 변경 금지
- ISessionManager.Players 타입 변경 금지
- 기존 32개 테스트 삭제/약화 금지
- 커밋/푸시 금지
- release gate 변경 금지
- ServerMemory.cs 수정 금지

## 검증

1. `./scripts/verify-release.sh` 통과 (기존 32개 + 신규 테스트)
2. SpatialGrid 단위 테스트:
   - 엔티티를 셀에 배치 후 GetNearbyEntityIds로 인접 엔티티 조회
   - 셀 크기 50에서 거리 100 이내의 엔티티만 반환 확인
   - 엔티티 이동 시 셀 변경 정상 동작
3. ZoneManager 단위 테스트:
   - Zone 생성/삭제, 플레이어 입장/퇴장
   - TransferPlayer 정상 동작
4. ZoneFactory 테스트:
   - CleanupEmptyInstances: 빈 인스턴스 Zone 정리 확인

## 최종 보고 형식

1~8 항목 (이전 마일스톤과 동일)

## 특별 주의사항

★ **SpatialGrid 셀 단위 lock**: HashSet<int>에 대한 동시 접근이 발생한다 (IOCP 스레드에서 UpdateEntityPosition, Tick 스레드에서 GetNearbyEntityIds). 셀 키 `(zoneId, cellX, cellZ)` 단위로 lock을 사용하거나 ConcurrentDictionary<int, byte>를 대체재로 사용하라.

★ **RoomState와 Zone의 관계**: Zone이 RoomState를 대체하는 것이 아니다. Zone은 공간 개념 (MMORPG 필드, 던전), RoomState는 게임 세션 개념 (FPS 매치 룸). Zone 안에 여러 RoomState가 공존할 수 있다. 이 마일스톤에서는 Zone이 자체 WorldState를 가지고, RoomBroadcaster는 기존대로 동작하게 유지하라.

★ **ProtocolSelect 충돌 방지**: 현재 사용 중: 1-10, 20-25, 100-102. 신규: ZoneTransferProtocol=26, ZoneInfoProtocol=27.

★ **역조회 인덱스 (_playerZoneMap)**: ZoneManager에서 hostId → zoneId 역조회를 ConcurrentDictionary<int, int>로 유지하라. 매번 모든 Zone의 PlayerHostIds를 순회하면 O(N×M) 비용이 발생한다.

★ **GameEntity.DeepClone() 비가상**: GameEntity.DeepClone()은 virtual이 아니다. ServerEntity에서 오버라이드 불가. `new public ServerEntity DeepClone()` 방식을 사용하거나, GameEntity.DeepClone()을 virtual로 변경하는 것을 허용한다 (이 케이스에 한해 기존 인터페이스 예외).

★ **CleanupEmptyInstances 주기**: 매 Tick 실행하면 불필요한 부하. Tick 핸들러 내에서 `if (tick % 600 == 0) CleanupEmptyInstances()` 패턴으로 30초마다 실행하라.
