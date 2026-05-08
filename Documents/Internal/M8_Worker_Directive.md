# Milestone 8: Lag Compensation & Prediction — 작업자 지시문

## 목표

Documents/구현계획.md에 명시된 Milestone 8의 5개 작업 항목을 구현한다.
이 마일스톤은 M7에서 구축한 Tick Loop + SnapshotBuffer 위에 지연 보상과 클라이언트 예측 지원을 추가하여, FPS/액션 장르에서 공정한 히트 판정과 부드러운 이동 경험을 보장한다.

> **중요**: 이 마일스톤의 핵심은 "서버가 최종 권위를 유지하면서도, 클라이언트가 지연 없이 즉각 반응하는 것처럼 느끼게 하는 것"이다. 서버 권위를 포기하면 치트에 무방비가 되고, 클라이언트 예측을 지원하지 않으면 고무줄 현상이 발생한다.

## 선행 조건 확인

Milestone 7이 커밋 완료되었다. 현재 브랜치: feature/phase2-architecture, HEAD: 45d6547

구현 시작 전 반드시 아래 명령으로 현재 상태를 확인하라.

```
git status --short --branch
git log --oneline -3
```

## 기존 코드 파악 (필독)

### M7에서 구축한 핵심 인프라

- **TeruTeruServer.SDK/Interfaces/IGameLoop.cs** (46줄)
  - L8-44: IGameLoop 인터페이스. RegisterTickHandler(Action<long>) 제공.
  - ★ LagCompensator는 Tick 핸들러가 아니라, 공격 판정 시점에 호출되는 유틸리티다. GameLoop에 등록하지 마라.

- **TeruTeruServer.SDK/GameEngine/SnapshotBuffer.cs** (60줄)
  - L14: capacity=128 (기본값). 20Hz에서 128프레임 = 6.4초 분량.
  - L23-32: `Push(WorldState state)` — DeepClone된 스냅샷을 링 버퍼에 저장.
  - L37-48: `GetAtTick(long tickNumber)` — mod 연산으로 O(1) 조회, TickNumber 일치 확인.
  - ★ LagCompensator가 이 GetAtTick()을 호출하여 과거 프레임의 엔티티 위치를 가져온다.
  - ★ _latestTick이 volatile이 아님 — 단일 GameLoop 스레드만 Push하므로 현재는 안전하나, LagCompensator가 다른 스레드에서 GetAtTick 호출 시 stale read 가능. 현재 설계에서는 문제 없지만 주의.

- **TeruTeruServer.SDK/GameEngine/GameEntity.cs** (61줄)
  - L6-59: EntityId, OwnerHostId, X/Y/Z, RotationY, VelocityX/Z, State, IsDirty, DeepClone().
  - ★ 히트박스 판정에 필요한 콜라이더 크기(반경 등)가 현재 없다. 이번 마일스톤에서 추가하라.

- **TeruTeruServer.SDK/GameEngine/GameInput.cs** (32줄)
  - L16: `ClientTick` — 클라이언트가 보낸 Tick 번호. CSP Reconciliation에서 이 값을 서버 응답에 포함하여 클라이언트가 어디까지 서버에서 확인됐는지 알 수 있게 한다.

- **TeruTeruServer.SDK/GameEngine/WorldState.cs** (47줄)
  - L25: `ConcurrentDictionary<int, GameEntity> Entities`.
  - L30-44: `DeepClone()` — 전체 엔티티 깊은 복사.

- **TeruTeruServer.SDK/Util/ClientSession.cs** (77줄)
  - L41: `public long RttMs { get; set; }` — 이미 존재. M3에서 추가됨.
  - L42: `public double PacketLossRate { get; set; }`
  - L43: `public DateTime LastPingUtc { get; set; }`
  - ★ RttMs는 현재 단순 대입. Rolling Average로 갱신하는 로직을 추가해야 한다.
  - ★ ClientSession에 새 필드를 추가하는 것은 허용하되, 기존 필드 타입/이름 변경은 금지.

- **TeruTeruServer.SDK/Enums/ProtocolEnums.cs** (43줄)
  - 현재 사용 중: 1-10, 20, 21, 22, 100-102.
  - ★ 신규 프로토콜 배정:
    - `StateAckProtocol = 23` — 서버 → 클라이언트: 입력 확인 + 보정 데이터
    - `RttPingProtocol = 24` — 양방향: RTT 측정용 핑/퐁
    - `HitValidationProtocol = 25` — 클라이언트 → 서버: 피격 판정 요청

- **TeruTeruServer.Runtime/GameEngine/GameLoop.cs** (123줄)
  - L24-28: `GameLoop(tickRate=20)`, `_targetFrameTimeMs = 1000.0 / tickRate`.
  - ★ LagCompensator에서 되감기 Tick 수를 계산할 때 이 TickRate를 참조해야 한다: `rewindTicks = (int)(rttMs / _targetFrameTimeMs / 2)` (RTT의 절반 = one-way latency).

- **TeruTeruServer.Runtime/GameEngine/RoomBroadcaster.cs** (61줄)
  - L33-57: BroadcastToRoom(roomId, packet, excludeHostId). StateAck 전송에 활용.

### 테스트 프로젝트 현황
- SDK.Tests: 13개, Runtime.Tests: 12개 (통합 7 + 게임엔진 5), Logic.Default.Tests: 2개
- ★ 총 27개 기존 테스트가 모두 통과해야 한다.

## 작업 항목 (5건)

### 작업 1: 서버 권위 모델 확립

**파일 범위:**
- [NEW] TeruTeruServer.Runtime/GameEngine/ServerAuthorityValidator.cs

**구현 내용:**
1. `ServerAuthorityValidator` 클래스를 신설하라:
   ```csharp
   public class ServerAuthorityValidator
   {
       private readonly IGameLoop _gameLoop;

       public ServerAuthorityValidator(IGameLoop gameLoop);

       /// <summary>
       /// 클라이언트 입력을 검증하고 엔티티 상태에 적용합니다.
       /// 반환값: 적용 성공 여부
       /// </summary>
       public bool ValidateAndApply(GameInput input, GameEntity entity, float maxSpeed);

       /// <summary>
       /// 이동 거리가 물리적으로 가능한지 검증합니다.
       /// </summary>
       public bool ValidateMovement(GameEntity entity, float newX, float newZ, float maxSpeed);
   }
   ```
   - `ValidateAndApply`: 입력의 MoveX/MoveZ를 maxSpeed로 클램프 → 엔티티의 X/Z를 갱신 → IsDirty 설정.
   - `ValidateMovement`: `sqrt(dx² + dz²) / (tickInterval)` ≤ maxSpeed 확인.
   - ★ 이 클래스는 M10 (Anti-Cheat)의 기반이 된다. 속도 핵 탐지는 M10에서 구현하므로, 여기서는 단순 클램프만.

---

### 작업 2: 클라이언트 사이드 예측(CSP) 지원

**파일 범위:**
- [NEW] TeruTeruServer.SDK/GameEngine/StateAck.cs
- [MODIFY] TeruTeruServer.SDK/Enums/ProtocolEnums.cs

**구현 내용:**
1. `StateAck` 모델을 SDK에 신설하라:
   ```csharp
   public class StateAck
   {
       public long ServerTick { get; set; }         // 현재 서버 Tick
       public long LastProcessedClientTick { get; set; }  // 서버가 마지막으로 처리한 클라이언트 Tick
       public float X { get; set; }                 // 서버가 판정한 현재 위치
       public float Y { get; set; }
       public float Z { get; set; }
       public float VelocityX { get; set; }
       public float VelocityZ { get; set; }
   }
   ```
   - 클라이언트는 이 응답을 받고: `LastProcessedClientTick` 이전의 로컬 예측은 폐기, 이후의 예측은 서버 위치 기반으로 재계산 (Reconciliation).
2. `ProtocolSelect`에 추가:
   ```csharp
   StateAckProtocol = 23,         // 서버 → 클라이언트: 입력 확인 + 보정
   RttPingProtocol = 24,          // RTT 측정 핑/퐁
   HitValidationProtocol = 25,    // 피격 판정 요청
   ```

---

### 작업 3: Lag Compensation (히트박스 되감기)

**파일 범위:**
- [NEW] TeruTeruServer.Runtime/GameEngine/LagCompensator.cs
- [NEW] TeruTeruServer.SDK/GameEngine/HitValidationRequest.cs
- [NEW] TeruTeruServer.SDK/GameEngine/HitValidationResult.cs
- [MODIFY] TeruTeruServer.SDK/GameEngine/GameEntity.cs (HitboxRadius 추가)

**구현 내용:**
1. `GameEntity`에 히트박스 반경 필드를 추가하라:
   ```csharp
   /// <summary>
   /// 히트 판정용 구체 콜라이더 반경
   /// </summary>
   public float HitboxRadius { get; set; } = 0.5f;
   ```
   - DeepClone()에도 `HitboxRadius = this.HitboxRadius` 추가.

2. `HitValidationRequest` 모델:
   ```csharp
   public class HitValidationRequest
   {
       public int ShooterHostId { get; set; }
       public int TargetEntityId { get; set; }
       public long ClientTick { get; set; }     // 클라이언트가 발사한 시점의 Tick
       public float AimX { get; set; }          // 에임 방향 X
       public float AimY { get; set; }          // 에임 방향 Y
       public float AimZ { get; set; }          // 에임 방향 Z
   }
   ```

3. `HitValidationResult` 모델:
   ```csharp
   public class HitValidationResult
   {
       public bool IsHit { get; set; }
       public int TargetEntityId { get; set; }
       public long ServerTick { get; set; }
       public long RewindTick { get; set; }     // 실제로 되감은 Tick
       public float Distance { get; set; }      // 판정 시 거리
   }
   ```

4. `LagCompensator` 클래스:
   ```csharp
   public class LagCompensator
   {
       private readonly SnapshotBuffer _snapshotBuffer;
       private readonly IGameLoop _gameLoop;
       private readonly int _maxRewindTicks;  // 최대 되감기 제한 (기본: 40 = 2초@20Hz)

       public LagCompensator(SnapshotBuffer buffer, IGameLoop gameLoop, int maxRewindTicks = 40);

       /// <summary>
       /// 클라이언트의 RTT를 기반으로 되감기 Tick 수를 계산합니다.
       /// </summary>
       public int CalculateRewindTicks(long rttMs);

       /// <summary>
       /// 히트 판정을 수행합니다.
       /// 1. rttMs 기반으로 rewindTicks 계산
       /// 2. SnapshotBuffer에서 과거 Tick의 WorldState 조회
       /// 3. 해당 시점의 타겟 엔티티 위치와 슈터의 에임 레이를 비교
       /// 4. 히트박스(구체) 충돌 판정
       /// </summary>
       public HitValidationResult ValidateHit(HitValidationRequest request, long shooterRttMs);
   }
   ```
   - `CalculateRewindTicks`: `(int)(rttMs / (1000.0 / gameLoop.TickRate) / 2)`. RTT의 절반(one-way)을 Tick으로 환산.
   - `ValidateHit` 로직:
     1. rewindTick = currentTick - CalculateRewindTicks(rttMs)
     2. rewindTick을 `max(currentTick - maxRewindTicks, rewindTick)`으로 클램프 (무한 되감기 방지)
     3. `_snapshotBuffer.GetAtTick(rewindTick)` → 과거 WorldState
     4. 과거 WorldState에서 targetEntityId 엔티티 조회
     5. 슈터 위치 → 에임 방향의 레이와 타겟 구체(X/Y/Z, HitboxRadius) 간 거리 계산
     6. 거리 ≤ HitboxRadius → IsHit = true

---

### 작업 4: Entity Interpolation 가이드

**파일 범위:**
- [NEW] Documents/Technical/Interpolation_Guide.md

**구현 내용:**
1. 아래 섹션을 포함하는 가이드를 작성하라 (한국어):
   - **개요**: 왜 보간이 필요한가 (Tick 간 끊김 방지)
   - **수신 버퍼링**: 클라이언트가 스냅샷을 2개 이상 버퍼링한 후 보간 시작
   - **선형 보간 (Lerp)**: `position = lerp(prevPos, nextPos, t)` 공식, t 계산법
   - **외삽 (Extrapolation)**: 패킷 손실 시 Velocity 기반 외삽
   - **코드 예제**: Unity C# 기준 보간 코드 예시
   - **CSP와의 관계**: 로컬 플레이어는 CSP(예측), 다른 플레이어는 보간

---

### 작업 5: RTT 측정 강화

**파일 범위:**
- [NEW] TeruTeruServer.SDK/GameEngine/RttTracker.cs
- [MODIFY] TeruTeruServer.SDK/Util/ClientSession.cs (RollingRtt 관련 필드 추가)

**구현 내용:**
1. `RttTracker` 유틸리티 클래스:
   ```csharp
   public class RttTracker
   {
       private readonly int _sampleCount;
       private readonly Queue<long> _samples = new();

       public RttTracker(int sampleCount = 10);

       /// <summary>
       /// 새로운 RTT 샘플을 추가하고 Rolling Average를 반환합니다.
       /// </summary>
       public long AddSample(long rttMs);

       /// <summary>
       /// 현재 Rolling Average RTT (ms)
       /// </summary>
       public long AverageRttMs { get; }

       /// <summary>
       /// RTT 분산 (Jitter 측정용)
       /// </summary>
       public double JitterMs { get; }
   }
   ```
   - `AddSample`: Queue에 추가, _sampleCount 초과 시 가장 오래된 것 제거, 평균 재계산.
   - `JitterMs`: 표준편차 또는 max-min.
2. `ClientSession`에 필드 추가:
   ```csharp
   // Lag Compensation 연동 필드 (Milestone 8)
   public RttTracker? RttHistory { get; set; }
   ```
   - ★ 기존 `RttMs` 필드는 유지. `RttHistory.AverageRttMs`가 더 정확한 값이지만, 하위 호환을 위해 `RttMs`도 함께 갱신하라.

## 변경 허용 범위

**허용:**
- TeruTeruServer.Runtime/GameEngine/ 하위 신규 파일 생성
- TeruTeruServer.SDK/GameEngine/ 하위 신규 파일 생성
- TeruTeruServer.SDK/Enums/ProtocolEnums.cs 수정 (신규 enum 값 추가만)
- TeruTeruServer.SDK/GameEngine/GameEntity.cs 수정 (HitboxRadius 필드 추가 + DeepClone 갱신)
- TeruTeruServer.SDK/Util/ClientSession.cs 수정 (RttHistory 필드 추가만)
- Documents/Technical/ 하위 신규 파일 생성
- 테스트 프로젝트에 신규 테스트 파일 생성
- IMPLEMENTATION_PROGRESS.md 갱신

**금지:**
- .agents/ 수정 금지
- ClientSession의 기존 필드 타입/이름 변경 금지 (추가만 허용)
- ISessionManager.Players 타입 변경 금지
- 기존 인터페이스 시그니처 변경 금지 (추가만 허용)
- SnapshotBuffer의 기존 API 시그니처 변경 금지
- 기존 통과 중인 27개 테스트 삭제 또는 약화 금지
- 커밋/푸시 금지
- release gate 기준(scripts/verify-release.sh) 변경 금지
- ServerMemory.cs 수정 금지

## 검증

1. `./scripts/verify-release.sh` 통과 (오류 0개 필수, 기존 27개 + 신규 테스트)
2. LagCompensator 단위 테스트:
   - RTT 100ms → 되감기 1 Tick (20Hz 기준)
   - RTT 200ms → 되감기 2 Ticks
   - 되감기 Tick이 maxRewindTicks를 초과하면 클램프 확인
   - 과거 스냅샷에서 히트 판정 성공/실패 케이스
3. ServerAuthorityValidator 단위 테스트:
   - maxSpeed 초과 입력 → 클램프 확인
   - 정상 입력 → 엔티티 위치 갱신 + IsDirty 확인
4. RttTracker 단위 테스트:
   - 10개 샘플 Rolling Average 정확도
   - sampleCount 초과 시 오래된 샘플 제거 확인
5. IMPLEMENTATION_PROGRESS.md가 실제 구현 상태와 일치하는지 확인

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

★ **되감기 범위 제한**: maxRewindTicks를 반드시 설정하라. 기본값 40 (20Hz에서 2초). 이를 초과하는 RTT의 클라이언트는 되감기 없이 현재 상태로 판정한다. 무한 되감기는 치트 벡터가 된다.

★ **GameEntity.HitboxRadius 추가 시 DeepClone 갱신 필수**: GameEntity.DeepClone() (L43-58)에 `HitboxRadius = this.HitboxRadius`를 반드시 추가하라. 누락하면 스냅샷의 히트박스가 기본값(0.5f)으로 고정되어 판정이 부정확해진다.

★ **ProtocolSelect 충돌 방지**: 현재 사용 중 번호: 1-10, 20-22, 100-102. 신규 배정: StateAckProtocol=23, RttPingProtocol=24, HitValidationProtocol=25. TokenRefreshProtocol=21은 이미 사용 중이므로 건드리지 마라.

★ **ClientSession.RttMs와 RttTracker 이중 갱신**: RttTracker.AddSample() 호출 시 반환된 평균값을 `session.RttMs`에도 대입하라. 기존 코드가 `session.RttMs`를 직접 참조하는 곳이 있으므로 하위 호환을 보장해야 한다.

★ **구체(Sphere) 충돌 판정 공식**: 레이-구체 교차 판정은 아래 공식을 사용하라:
```
// 슈터 위치 S, 에임 방향 D (정규화), 타겟 위치 T, 반경 R
// 벡터 f = S - T
// discriminant = (D·f)² - (f·f - R²)
// discriminant ≥ 0 → 히트
```
3D 수학 라이브러리 없이 float 연산으로 직접 구현하라. System.Numerics.Vector3을 사용해도 좋다.

★ **LagCompensator는 Tick 핸들러가 아니다**: GameLoop에 등록하지 마라. LagCompensator는 공격 패킷이 도착했을 때 LogicPlugin이나 RoutingMiddleware에서 호출하는 유틸리티다. Tick과 독립적으로 동작한다.
