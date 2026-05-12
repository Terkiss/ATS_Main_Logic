# Milestone 10: Game Security & Anti-Cheat — 작업자 지시문

## 목표

Documents/구현계획.md에 명시된 Milestone 10의 5개 작업 항목을 구현한다.
이 마일스톤은 M2의 기본 보안(RateLimit, ReplayAttack)과 M8의 ServerAuthorityValidator를 게임 특화 안티치트 시스템으로 확장한다.

> **중요**: 핵심은 "서버가 모든 클라이언트 행동을 의심하고 증거 기반으로 제재하는 것"이다. 오탐(false positive)을 최소화하면서도, 확실한 치트(속도핵, 패킷 변조)는 3 Tick 이내에 탐지해야 한다.

## 선행 조건 확인

Milestone 9가 커밋 완료되었다. 현재 브랜치: feature/phase2-architecture, HEAD: 00aa5dd

```
git status --short --branch
git log --oneline -3
```

## 기존 코드 파악 (필독)

### M2-M8에서 구축한 보안 인프라

- **TeruTeruServer.Runtime/Pipeline/RateLimitMiddleware.cs** (47줄)
  - L15: `_maxPacketsPerSecond` — 초당 최대 패킷 수 (기본 50)
  - L25-28: 초 단위 카운터 리셋 로직
  - L33-38: 초과 시 패킷 Drop + 경고 로그
  - ★ 현재는 Drop만 하고 제재(ban)로 이어지지 않는다. 자동 제재 파이프라인에서 이 Drop 이벤트를 SecurityEvent로 발행해야 한다.

- **TeruTeruServer.Runtime/Pipeline/ReplayAttackMiddleware.cs** (40줄)
  - L16: 패킷 구조 `[SendType(1)][ProtocolType(1)][SequenceNumber(4)]...`
  - L19: `BitConverter.ToUInt32(buffer, 2)` — offset 2에서 SequenceNumber 추출
  - L23-31: 이전 번호보다 작거나 같으면 Drop
  - ★ HMAC 검증은 SequenceNumber 검증 직후에 추가한다. 패킷 구조를 `[SendType(1)][ProtocolType(1)][SequenceNumber(4)][HMAC(32)]...[Payload]`로 확장.

- **TeruTeruServer.Runtime/GameEngine/ServerAuthorityValidator.cs** (64줄)
  - L22-43: `ValidateAndApply` — 입력 클램프 + 1틱 이동 검증
  - L49-61: `ValidateMovement` — 단일 틱 내 이동 거리 제곱 비교, epsilon = 1.5f
  - ★ 현재는 단일 틱만 검증한다. 경로 적분 검증을 추가하여 N틱 동안 누적 이동 거리가 `maxSpeed * N * tickInterval * epsilon`을 초과하면 탐지한다.
  - ★ ValidateAndApply가 false를 반환할 때 SecurityEvent를 발행해야 한다.

- **TeruTeruServer.SDK/Util/ClientSession.cs** (86줄)
  - L35-37: `CurrentSecondPacketCount`, `LastPacketTime`, `LastSequenceNumber` — M2 보안 필드
  - L41-42: `RttMs`, `PacketLossRate` — M3 품질 필드
  - L46: `RttHistory` (RttTracker) — M8 Lag Compensation 필드
  - ★ 여기에 M10 필드를 추가한다: `ViolationCount`, `BanLevel`, `LastViolationUtc`, `InputCountPerTick`

- **TeruTeruServer.Runtime/Pipeline/IPacketMiddleware.cs** (34줄)
  - L12-23: `PacketContext` — Socket, RawData, Session, IsProcessed
  - ★ PacketContext는 수정하지 않는다. 미들웨어에서 `context.Session`을 통해 위반 카운터에 접근.

- **TeruTeruServer.Runtime/Pipeline/PacketPipeline.cs** (48줄)
  - L20-44: 미들웨어 체인 실행. 50ms 초과 시 프로파일링 경고
  - ★ 파이프라인 자체는 수정하지 않는다.

- **TeruTeruServer.Runtime/MainServer.cs** (535줄)
  - L87-93: 파이프라인 미들웨어 등록 순서
    ```
    Validation → RateLimit → ReplayAttack → Decryption → Auth → Routing
    ```
  - ★ HMAC 검증 미들웨어는 ReplayAttack 직후, Decryption 직전에 삽입한다.
  - ★ 제재 검사 미들웨어는 Validation 직후, RateLimit 직전에 삽입한다.
    최종 순서: `Validation → BanCheck → RateLimit → ReplayAttack → HmacVerify → Decryption → Auth → Routing`

- **TeruTeruServer.SDK/GameEngine/GameInput.cs** (32줄)
  - L16: `ClientTick` — 입력 빈도 추적에 활용 가능
  - ★ 수정하지 않는다.

- **TeruTeruServer.SDK/Enums/ProtocolEnums.cs** (48줄)
  - 사용 중: 1-10, 20-27, 100-102
  - ★ 신규: `SecurityEventProtocol = 28` — 제재/경고 알림 클라이언트 전송용

- **TeruTeruServer.Cli/Program.cs** (122줄)
  - L113-118: Game Engine Services DI 등록
  - ★ SecurityEventLogger, AntiCheatValidator DI 등록을 추가해야 한다.

### 테스트 현황
- 총 36개 테스트 통과 중. 모두 유지해야 한다.

## 작업 항목 (5건)

### 작업 1: 서버 사이드 이동 검증 강화 (Path Integration)

**파일 범위:**
- [MODIFY] TeruTeruServer.Runtime/GameEngine/ServerAuthorityValidator.cs
- [NEW] TeruTeruServer.SDK/GameEngine/SecurityEvent.cs

**구현 내용:**
1. `ServerAuthorityValidator`에 경로 적분(Path Integration) 검증 추가:
   ```csharp
   // 세션별 최근 N틱 위치 히스토리 (ConcurrentDictionary<int, Queue<(float x, float z, long tick)>>)
   private readonly ConcurrentDictionary<int, Queue<(float x, float z, long tick)>> _positionHistory = new();
   private const int HISTORY_SIZE = 60; // 3초분 (20Hz)
   
   public SecurityEvent? ValidatePathIntegrity(int hostId, GameEntity entity, float maxSpeed)
   {
       // 1. 히스토리에 현재 위치 추가
       // 2. 최근 HISTORY_SIZE 틱 동안의 누적 이동 거리 계산
       // 3. maxSpeed * elapsed ticks * tickInterval * epsilon(2.0f)과 비교
       // 4. 초과 시 SecurityEvent 반환 (Type = "SpeedHack")
       // 5. 텔레포트 감지: 단일 틱 이동 거리가 maxSpeed * tickInterval * 5.0f 초과 시 
       //    SecurityEvent 반환 (Type = "Teleport")
   }
   ```
2. `SecurityEvent` 모델:
   ```csharp
   public class SecurityEvent
   {
       public int HostId { get; set; }
       public string EventType { get; set; } = "";   // "SpeedHack", "Teleport", "InputFlood", "PacketTamper"
       public string Description { get; set; } = "";
       public DateTime Timestamp { get; set; } = DateTime.UtcNow;
       public string Severity { get; set; } = "Warning";  // "Warning", "Critical"
   }
   ```
3. `ValidateAndApply` 내부에서 `ValidatePathIntegrity` 호출 후 SecurityEvent 발행.

---

### 작업 2: 게임 입력 빈도 검증

**파일 범위:**
- [NEW] TeruTeruServer.Runtime/GameEngine/InputFrequencyValidator.cs
- [MODIFY] TeruTeruServer.SDK/Util/ClientSession.cs (필드 추가)

**구현 내용:**
1. `ClientSession`에 필드 추가:
   ```csharp
   // Anti-Cheat 필드 (Milestone 10)
   public int ViolationCount { get; set; }
   public int BanLevel { get; set; }  // 0=정상, 1=경고, 2=임시차단, 3=영구차단
   public DateTime LastViolationUtc { get; set; }
   public int InputCountThisTick { get; set; }
   public long LastInputTick { get; set; }
   ```
2. `InputFrequencyValidator` 구현:
   ```csharp
   public class InputFrequencyValidator
   {
       private readonly int _maxInputsPerTick;
       
       public InputFrequencyValidator(int maxInputsPerTick = 3)
       {
           _maxInputsPerTick = maxInputsPerTick;
       }
       
       public SecurityEvent? Validate(ClientSession session, long currentTick)
       {
           if (session.LastInputTick == currentTick)
           {
               session.InputCountThisTick++;
               if (session.InputCountThisTick > _maxInputsPerTick)
               {
                   return new SecurityEvent
                   {
                       HostId = session.HostID,
                       EventType = "InputFlood",
                       Description = $"Input count {session.InputCountThisTick} exceeds limit {_maxInputsPerTick} at tick {currentTick}",
                       Severity = "Warning"
                   };
               }
           }
           else
           {
               session.LastInputTick = currentTick;
               session.InputCountThisTick = 1;
           }
           return null;
       }
   }
   ```

---

### 작업 3: 패킷 HMAC 무결성 검사

**파일 범위:**
- [NEW] TeruTeruServer.Runtime/Pipeline/HmacVerifyMiddleware.cs

**구현 내용:**
1. `HmacVerifyMiddleware` 구현:
   ```csharp
   public class HmacVerifyMiddleware : IPacketMiddleware
   {
       private readonly byte[] _serverKey;
       
       public HmacVerifyMiddleware(byte[] serverKey)
       {
           _serverKey = serverKey;
       }
       
       public async Task InvokeAsync(PacketContext context, Func<Task> next)
       {
           // 패킷 구조: [SendType(1)][Protocol(1)][SeqNum(4)][HMAC(32)][Payload...]
           // 최소 길이: 1+1+4+32 = 38
           if (context.RawData.Length >= 38 && context.Session != null)
           {
               byte[] receivedHmac = new byte[32];
               Array.Copy(context.RawData, 6, receivedHmac, 0, 32);
               
               // HMAC 계산 대상: [SendType(1)][Protocol(1)][SeqNum(4)] + [Payload(나머지)]
               byte[] dataToVerify = new byte[6 + (context.RawData.Length - 38)];
               Array.Copy(context.RawData, 0, dataToVerify, 0, 6);
               if (context.RawData.Length > 38)
                   Array.Copy(context.RawData, 38, dataToVerify, 6, context.RawData.Length - 38);
               
               using var hmac = new System.Security.Cryptography.HMACSHA256(_serverKey);
               byte[] computedHmac = hmac.ComputeHash(dataToVerify);
               
               if (!CryptographicEquals(receivedHmac, computedHmac))
               {
                   TeruTeruLogger.LogWarning($"[HMAC] HostID {context.Session.HostID} packet tampered. Disconnecting.");
                   context.IsProcessed = true;
                   // SecurityEvent 발행
                   return;
               }
               
               // HMAC를 제거한 깨끗한 패킷으로 교체
               byte[] cleanData = new byte[context.RawData.Length - 32];
               Array.Copy(context.RawData, 0, cleanData, 0, 6);
               if (context.RawData.Length > 38)
                   Array.Copy(context.RawData, 38, cleanData, 6, context.RawData.Length - 38);
               context.RawData = cleanData;
           }
           
           await next();
       }
       
       private static bool CryptographicEquals(byte[] a, byte[] b)
       {
           // 타이밍 공격 방지를 위한 고정 시간 비교
           if (a.Length != b.Length) return false;
           int diff = 0;
           for (int i = 0; i < a.Length; i++)
               diff |= a[i] ^ b[i];
           return diff == 0;
       }
   }
   ```
   - ★ `CryptographicEquals`: 타이밍 공격 방지를 위한 고정 시간 비교 필수.
   - ★ HMAC 키는 서버 설정에서 로드. Program.cs에서 DI 등록 시 `config`에서 읽거나 고정 키 사용.
   - ★ HMAC가 없는 기존 프로토콜(ConnectProtocol, LoginProtocol 등 인증 전 패킷)은 검증을 건너뛰어야 한다. `context.Session == null` 또는 `!context.Session.IsAuthenticated`인 경우 bypass.

---

### 작업 4: 행동 이상 탐지 로깅 (SecurityEventLogger)

**파일 범위:**
- [NEW] TeruTeruServer.Runtime/GameEngine/SecurityEventLogger.cs
- [NEW] TeruTeruServer.SDK/Interfaces/ISecurityEventLogger.cs

**구현 내용:**
1. `ISecurityEventLogger` 인터페이스:
   ```csharp
   public interface ISecurityEventLogger
   {
       void LogEvent(SecurityEvent evt);
       IReadOnlyList<SecurityEvent> GetRecentEvents(int hostId, int count = 10);
       int GetViolationCount(int hostId);
   }
   ```
2. `SecurityEventLogger` 구현:
   ```csharp
   public class SecurityEventLogger : ISecurityEventLogger
   {
       private readonly ConcurrentDictionary<int, List<SecurityEvent>> _events = new();
       private readonly object _logLock = new();
       
       public void LogEvent(SecurityEvent evt)
       {
           var list = _events.GetOrAdd(evt.HostId, _ => new List<SecurityEvent>());
           lock (list) { list.Add(evt); }
           
           // 파일 로그도 기록
           TeruTeruLogger.LogWarning($"[Security] {evt.EventType} | HostID: {evt.HostId} | {evt.Description} | Severity: {evt.Severity}");
       }
       
       public IReadOnlyList<SecurityEvent> GetRecentEvents(int hostId, int count = 10)
       {
           if (_events.TryGetValue(hostId, out var list))
           {
               lock (list) { return list.TakeLast(count).ToList(); }
           }
           return Array.Empty<SecurityEvent>();
       }
       
       public int GetViolationCount(int hostId)
       {
           if (_events.TryGetValue(hostId, out var list))
           {
               lock (list) { return list.Count; }
           }
           return 0;
       }
   }
   ```
3. DI 등록: `services.AddSingleton<ISecurityEventLogger, SecurityEventLogger>();`

---

### 작업 5: 자동 제재 파이프라인

**파일 범위:**
- [NEW] TeruTeruServer.Runtime/Pipeline/BanCheckMiddleware.cs
- [NEW] TeruTeruServer.Runtime/GameEngine/SanctionManager.cs
- [MODIFY] TeruTeruServer.SDK/Enums/ProtocolEnums.cs (SecurityEventProtocol = 28)
- [MODIFY] TeruTeruServer.Runtime/MainServer.cs (파이프라인 미들웨어 순서 변경)
- [MODIFY] TeruTeruServer.Cli/Program.cs (DI 등록)

**구현 내용:**
1. `SanctionManager` 클래스:
   ```csharp
   public class SanctionManager
   {
       private readonly ISecurityEventLogger _logger;
       private readonly ISessionManager _sessionManager;
       
       // 제재 임계치
       private const int WARNING_THRESHOLD = 3;
       private const int TEMP_BAN_THRESHOLD = 7;
       private const int PERMA_BAN_THRESHOLD = 15;
       
       public SanctionManager(ISecurityEventLogger logger, ISessionManager sessionManager)
       {
           _logger = logger;
           _sessionManager = sessionManager;
       }
       
       /// <summary>
       /// SecurityEvent를 처리하고, 위반 횟수에 따라 제재 수준을 결정합니다.
       /// </summary>
       public int ProcessViolation(ClientSession session, SecurityEvent evt)
       {
           _logger.LogEvent(evt);
           session.ViolationCount++;
           session.LastViolationUtc = DateTime.UtcNow;
           
           if (session.ViolationCount >= PERMA_BAN_THRESHOLD)
           {
               session.BanLevel = 3; // 영구 차단
               TeruTeruLogger.LogWarning($"[Sanction] HostID {session.HostID} PERMANENTLY BANNED ({session.ViolationCount} violations)");
           }
           else if (session.ViolationCount >= TEMP_BAN_THRESHOLD)
           {
               session.BanLevel = 2; // 임시 차단
               TeruTeruLogger.LogWarning($"[Sanction] HostID {session.HostID} TEMP BANNED ({session.ViolationCount} violations)");
           }
           else if (session.ViolationCount >= WARNING_THRESHOLD)
           {
               session.BanLevel = 1; // 경고
               TeruTeruLogger.LogWarning($"[Sanction] HostID {session.HostID} WARNING ({session.ViolationCount} violations)");
           }
           
           return session.BanLevel;
       }
   }
   ```
2. `BanCheckMiddleware` — 차단된 세션의 패킷을 거부:
   ```csharp
   public class BanCheckMiddleware : IPacketMiddleware
   {
       public async Task InvokeAsync(PacketContext context, Func<Task> next)
       {
           if (context.Session != null && context.Session.BanLevel >= 2)
           {
               TeruTeruLogger.LogInfo($"[BanCheck] HostID {context.Session.HostID} is banned (Level {context.Session.BanLevel}). Dropping packet.");
               context.IsProcessed = true;
               return;
           }
           await next();
       }
   }
   ```
3. `ProtocolSelect`에 추가:
   ```csharp
   SecurityEventProtocol = 28,    // 보안 이벤트 알림 (M10)
   ```
4. MainServer.cs — 파이프라인 미들웨어 순서 변경:
   ```csharp
   _pipeline.Use(new ValidationMiddleware());
   _pipeline.Use(new BanCheckMiddleware());              // [NEW M10]
   _pipeline.Use(new RateLimitMiddleware(50));
   _pipeline.Use(new ReplayAttackMiddleware());
   _pipeline.Use(new HmacVerifyMiddleware(hmacKey));     // [NEW M10]
   _pipeline.Use(new DecryptionMiddleware(new SeedCryptoService()));
   _pipeline.Use(new AuthMiddleware(_sessionManager, _sessionStore));
   _pipeline.Use(new RoutingMiddleware(_serverLogic));
   ```
5. Program.cs — DI 등록 추가:
   ```csharp
   services.AddSingleton<ISecurityEventLogger, SecurityEventLogger>();
   services.AddSingleton<SanctionManager>();
   services.AddSingleton<InputFrequencyValidator>();
   ```

## 변경 허용 범위

**허용:**
- TeruTeruServer.Runtime/Pipeline/ 하위 신규 파일 생성
- TeruTeruServer.Runtime/GameEngine/ 하위 신규 파일 생성 및 기존 파일 수정 (ServerAuthorityValidator.cs)
- TeruTeruServer.SDK/GameEngine/ 하위 신규 파일 생성
- TeruTeruServer.SDK/Interfaces/ 하위 신규 파일 생성
- TeruTeruServer.SDK/Enums/ProtocolEnums.cs 수정 (신규 enum 값 추가만)
- TeruTeruServer.SDK/Util/ClientSession.cs 수정 (필드 추가만, 기존 필드 삭제/타입 변경 금지)
- TeruTeruServer.Runtime/MainServer.cs 수정 (파이프라인 미들웨어 등록 순서 변경만)
- TeruTeruServer.Cli/Program.cs 수정 (DI 등록 추가만)
- 테스트 프로젝트에 신규 테스트 파일 생성
- IMPLEMENTATION_PROGRESS.md 갱신

**금지:**
- .agents/ 수정 금지
- ISessionManager.Players 타입 변경 금지
- PacketContext 기존 프로퍼티 변경 금지
- IPacketMiddleware 인터페이스 시그니처 변경 금지
- IRoomBroadcaster 시그니처 변경 금지
- 기존 36개 테스트 삭제/약화 금지
- 커밋/푸시 금지
- release gate 변경 금지
- ServerMemory.cs 수정 금지
- GameLoop.cs 수정 금지

## 검증

1. `./scripts/verify-release.sh` 통과 (기존 36개 + 신규 테스트)
2. 이동 검증 테스트:
   - 속도핵 시뮬레이션: maxSpeed의 2배로 이동 시 3 Tick 이내 SecurityEvent 발생 확인
   - 텔레포트 시뮬레이션: 대규모 좌표 점프 시 즉시 탐지 확인
   - 정상 이동: 허용 범위 내 이동 시 SecurityEvent 미발생 확인
3. 입력 빈도 검증 테스트:
   - 동일 Tick에 4개 이상 GameInput 수신 시 InputFlood 이벤트 발생 확인
4. HMAC 검증 테스트:
   - 올바른 HMAC: 통과 확인
   - 변조된 HMAC: 패킷 Drop 확인
   - HMAC 없는 패킷 (인증 전): bypass 확인
5. 자동 제재 테스트:
   - 위반 3회 → BanLevel 1 (경고)
   - 위반 7회 → BanLevel 2 (임시 차단, 패킷 Drop)
   - 위반 15회 → BanLevel 3 (영구 차단)

## 최종 보고 형식

1~8 항목 (이전 마일스톤과 동일)

## 특별 주의사항

★ **파이프라인 미들웨어 순서가 핵심이다**: BanCheck는 반드시 최상위(Validation 직후)에 위치해야 한다. 차단된 세션의 패킷이 RateLimit/Auth 등 하위 미들웨어까지 도달하면 불필요한 연산이 발생한다.

★ **HMAC bypass 조건**: `context.Session == null` (아직 세션 생성 전, ConnectProtocol 등) 또는 `!context.Session.IsAuthenticated` (로그인 전)인 경우 HMAC 검증을 건너뛴다. 인증 전 패킷에는 HMAC가 없다.

★ **ServerAuthorityValidator.ValidateMovement의 기존 epsilon=1.5f 유지**: 경로 적분 검증은 별도 메서드로 추가한다. 기존 단일 틱 검증 로직을 수정하지 않는다.

★ **ClientSession 필드 추가 시 생성자 수정 금지**: 새 필드는 모두 기본값(0, null 등)으로 초기화되므로 생성자(L54-73)를 수정할 필요가 없다.

★ **CryptographicEquals 필수**: HMAC 비교 시 `byte[].SequenceEqual()`을 사용하면 타이밍 사이드 채널 공격에 취약하다. 반드시 고정 시간 비교를 사용하라.

★ **SecurityEventLogger 메모리 관리**: 장기 운영 시 이벤트가 무한히 쌓인다. 세션당 최대 100개까지만 보관하고, 초과 시 오래된 것부터 삭제하라. 또는 세션 종료 시 `_events.TryRemove(hostId, out _)`로 정리하라.

★ **DI 전파 경로**: SanctionManager는 ISecurityEventLogger와 ISessionManager에 의존한다. Program.cs에서 `AddSingleton<SanctionManager>()`로 등록하면 DI 컨테이너가 자동 주입한다. MainServer.cs의 파이프라인에 BanCheckMiddleware와 HmacVerifyMiddleware를 추가할 때는 `new` 키워드로 직접 생성한다 (기존 미들웨어와 동일 패턴).
