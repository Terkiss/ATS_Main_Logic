# Milestone 6: Developer Experience & SDK Finalization — 작업자 지시문

## 목표

Documents/구현계획.md에 명시된 Milestone 6의 5개 작업 항목을 구현한다. 이 마일스톤은 TeruTeruServer를 내부 도구에서 플랫폼 수준으로 격상하기 위한 마지막 마일스톤이다.

> **중요**: M6의 핵심은 "외부 개발자가 SDK 문서만으로 Logic Plugin을 독립적으로 개발·배포 가능한 상태"를 달성하는 것이다. 코드 품질보다 개발자 경험(DX)이 최우선이다.

## 선행 조건 확인

Milestone 5가 커밋 완료되었다. 현재 브랜치: feature/phase2-architecture, HEAD: d3c01ed

구현 시작 전 반드시 아래 명령으로 현재 상태를 확인하라.

```
git status --short --branch
git log --oneline -3
```

## 기존 코드 파악 (필독)

### 핵심 파일

- **TeruTeruServer.SDK/TeruTeruServer.SDK.csproj** (28줄)
  - L11: `<GenerateDocumentationFile>true</GenerateDocumentationFile>` 이미 설정됨.
  - ★ SDK만 설정되어 있음. Runtime, Client, Logic.Default 프로젝트에는 미설정 → 작업 1에서 추가해야 함.

- **TeruTeruServer.Runtime/Rpc/ProtocolRouter.cs** (157줄)
  - L21: `private readonly List<ProtocolEndpointInfo> _endpoints`
  - L23: `public IReadOnlyList<ProtocolEndpointInfo> GetRegisteredEndpoints()` — 이미 구현됨.
  - 이 API를 활용하여 등록된 엔드포인트 목록을 자동으로 마크다운 문서로 생성하는 유틸리티를 만든다.

- **TeruTeruServer.Runtime/Rpc/PluginMetadata.cs** (29줄)
  - `ProtocolEndpointInfo`: MethodName, ProtocolOrRpcName, BindingType, RequiresAuth 필드 보유.

- **TeruTeruServer.Client/TeruClient.cs** (292줄)
  - 고수준 클라이언트 SDK. ConnectAsync(), LoginAsync(), InvokeRpcAsync(), SendProtocolAsync() 제공.
  - L23: `ConcurrentDictionary<ProtocolSelect, Action<byte[]>> _handlers` — 수동 핸들러 등록.
  - L42-45: `RegisterLogic(object)` → ClientProtocolRouter로 어트리뷰트 기반 라우팅.

- **TeruTeruServer.Client/ClientProtocolRouter.cs** (128줄)
  - 클라이언트 측 Rpc/Protocol 어트리뷰트 스캔 + 자동 라우팅.
  - 서버 ProtocolRouter와 거울 구조.

- **TeruTeruServer.Client/P2PManager.cs** (크기: 9900바이트)
  - P2P 연결 관리. UDP 홀펀칭, 릴레이 자동 전환.

- **DummyClient/Program.cs** (65줄)
  - 현재 참조 예제. MyClientLogic 클래스에 [Rpc], [Protocol] 어트리뷰트 사용 예시 존재.
  - ★ 단 2개 핸들러만 있어 예제로서 불충분. P2P, Reconnect 사용 예시가 없음 → 작업 2에서 보강.

- **TeruTeruServer.Cli/Program.cs** (약 100줄)
  - DI 컨테이너 구성. ISessionStore, ISessionManager, IEventBus, IClusterRegistry, ILogicService 등 모든 서비스 등록.
  - ★ ILogicService 등록 팩토리(L57-65)에서 LogicPlugin을 직접 생성하고 있음. MockServer 설계 시 이 팩토리 패턴을 참조하라.

- **TeruTeruServer.Runtime/MainServer.cs** (535줄)
  - L86-92: `InitializePipeline()` — RateLimitMiddleware → ReplayAttackMiddleware → DecryptionMiddleware → AuthMiddleware → RoutingMiddleware 순서.
  - MockServer는 이 파이프라인 구조를 참조하되, 소켓 없이 PacketContext를 직접 주입하는 방식.

### 테스트 프로젝트 현황
- `TeruTeruServer.SDK.Tests` (13개 테스트)
- `TeruTeruServer.Runtime.Tests` (3개 테스트)
- `TeruTeruServer.Logic.Default.Tests` (2개 테스트)
- ★ 통합 테스트(E2E)는 현재 없음 → 작업 4에서 신설.

## 작업 항목 (5건)

### 작업 1: SDK API 문서 자동 생성 기반 구축

**파일 범위:**
- [MODIFY] TeruTeruServer.Runtime/TeruTeruServer.Runtime.csproj
- [MODIFY] TeruTeruServer.Client/TeruTeruServer.Client.csproj
- [MODIFY] TeruTeruServer.Logic.Default/TeruTeruServer.Logic.Default.csproj
- [NEW] TeruTeruServer.SDK/Util/EndpointDocGenerator.cs
- [NEW] Documents/API/SDK_API_Reference.md
- [NEW] Documents/API/Client_API_Reference.md

**구현 내용:**
1. Runtime, Client, Logic.Default 프로젝트의 `.csproj`에 아래를 추가하라:
   ```xml
   <GenerateDocumentationFile>true</GenerateDocumentationFile>
   <NoWarn>$(NoWarn);CS1591</NoWarn>
   ```
2. `EndpointDocGenerator` 정적 클래스를 SDK에 신설하라:
   ```csharp
   public static class EndpointDocGenerator
   {
       public static string GenerateMarkdown(IReadOnlyList<ProtocolEndpointInfo> endpoints)
       // ProtocolEndpointInfo 목록을 마크다운 테이블 형식으로 변환
   }
   ```
   - ProtocolEndpointInfo는 Runtime 프로젝트에 있으므로, SDK에서 참조 가능하도록 모델을 SDK로 이동하거나, 인터페이스를 활용하라. SDK가 Runtime을 참조하면 순환 참조가 발생하므로 주의.
   - ★ 순환 참조 해결: `ProtocolEndpointInfo` 모델을 SDK로 이동하거나, GenerateMarkdown을 Runtime 프로젝트에 배치하는 방법 중 택 1.
3. `Documents/API/SDK_API_Reference.md`에 SDK 주요 인터페이스와 모델의 공개 API를 정리하라:
   - ILogicService, ISessionManager, ISessionStore, IEventBus, IClusterRegistry
   - ClientSession, ProtocolSelect, PacketContext
4. `Documents/API/Client_API_Reference.md`에 TeruClient 공개 API를 정리하라:
   - ConnectAsync, LoginAsync, InvokeRpcAsync, SendProtocolAsync, RegisterLogic
   - P2PManager 주요 메서드

---

### 작업 2: 클라이언트 SDK 템플릿 강화

**파일 범위:**
- [NEW] TeruTeruServer.Client/PacketBuilder.cs
- [MODIFY] DummyClient/Program.cs

**구현 내용:**
1. `PacketBuilder` 유틸리티를 Client 프로젝트에 신설하라:
   ```csharp
   public class PacketBuilder
   {
       public static byte[] BuildJsonPacket(ProtocolSelect protocol, object payload);
       public static byte[] BuildRpcPacket(string methodName, object? parameters = null);
   }
   ```
   - 내부적으로 SDK의 SeedCryptoService, 헤더 구성(6바이트)을 처리.
   - TeruClient.SendProtocolAsync()와 동일한 패킷 포맷을 외부에서도 직접 구성할 수 있게 한다.
2. `DummyClient/Program.cs`를 참조 예제로 보강하라:
   - 기존 [Rpc] + [Protocol] 핸들러 유지
   - [Protocol] ReconnectProtocol 핸들러 예시 추가
   - P2P 그룹 조인/릴레이 호출 예시 추가 (TeruClient.SendProtocolAsync 활용)
   - 각 단계에 주석으로 설명 추가 (외부 개발자 대상)

---

### 작업 3: 로컬 Mock 서버 모드

**파일 범위:**
- [NEW] TeruTeruServer.Runtime/Testing/MockServer.cs
- [NEW] TeruTeruServer.Runtime/Testing/MockPacketContext.cs

**구현 내용:**
1. `MockServer` 클래스를 Runtime/Testing/ 하위에 신설하라:
   ```csharp
   public class MockServer : IDisposable
   {
       public MockServer(IServiceProvider serviceProvider);
       public Task<byte[]?> ProcessPacketAsync(byte[] rawPacket);
       public Task<byte[]?> ProcessJsonAsync(ProtocolSelect protocol, object payload);
   }
   ```
   - 내부적으로 실제 MainServer의 미들웨어 파이프라인과 동일한 순서를 구성하되, 소켓 대신 MockPacketContext를 사용한다.
   - MockPacketContext: PacketContext를 상속하거나 래핑하여, ClientSocket 대신 응답 버퍼를 캡처하는 구조.
2. `MockPacketContext`:
   ```csharp
   public class MockPacketContext : PacketContext
   {
       public List<byte[]> CapturedResponses { get; }
       // ClientSocket을 null로 두되, SendResponse 호출 시 CapturedResponses에 저장
   }
   ```
3. DI 구성은 Program.cs의 ConfigureServices를 참조하되, MainServer와 소켓 관련 서비스는 제외한다.

**주의:**
- PacketContext 구조를 확인하라. PacketContext가 class인지 struct인지, Socket 필드가 required인지에 따라 MockPacketContext 설계가 달라진다.
- ★ MainServer.InitializePipeline() (L86-92)의 미들웨어 순서를 정확히 복제하라.

---

### 작업 4: 통합 테스트 프레임워크

**파일 범위:**
- [NEW] TeruTeruServer.Runtime.Tests/Integration/PacketSimulator.cs
- [NEW] TeruTeruServer.Runtime.Tests/Integration/LoginFlowTests.cs
- [NEW] TeruTeruServer.Runtime.Tests/Integration/RpcFlowTests.cs

**구현 내용:**
1. `PacketSimulator` 클래스를 테스트 프로젝트에 신설하라:
   ```csharp
   public class PacketSimulator
   {
       public PacketSimulator(MockServer server);
       public Task<T?> SendAndReceive<T>(ProtocolSelect protocol, object request);
       public Task<byte[]?> SendRaw(byte[] packet);
   }
   ```
   - MockServer를 주입받아 패킷을 보내고 응답을 디시리얼라이즈하여 반환.
2. `LoginFlowTests`:
   - 로그인 요청 → JWT 응답 확인
   - 잘못된 자격증명 → 실패 응답 확인
3. `RpcFlowTests`:
   - RPC 호출 → 올바른 메서드 라우팅 확인
   - 존재하지 않는 RPC → 에러 응답 확인

**주의:**
- MockServer에서 AuthMiddleware가 JWT 검증을 수행하므로, 로그인 후 JWT를 받아서 후속 요청에 포함하는 흐름이 필요하다.
- 테스트는 xUnit 프레임워크를 사용하라 (기존 테스트와 동일).

---

### 작업 5: 마이그레이션 가이드 문서화

**파일 범위:**
- [NEW] Documents/Technical/Migration_Guide.md

**구현 내용:**
1. 아래 섹션을 포함하는 가이드를 작성하라:
   - **ProtocolSelect 관리 정책**: 새 프로토콜 추가 시 규칙 (번호 예약, deprecated 처리)
   - **SDK 버전 호환 매트릭스**: SDK 1.0 ↔ Server 1.x 호환성 표
   - **Breaking Change 정책**: 어떤 변경이 Breaking이고, 공지/마이그레이션 경로는 어떻게 제공하는지
   - **Plugin 마이그레이션**: ILogicService 인터페이스 변경 시 기존 플러그인 업데이트 절차
   - **세션 저장소 마이그레이션**: InMemorySessionStore → Redis 전환 시 체크리스트
2. 문서는 한국어로 작성하되, 코드 예제는 C#으로 포함하라.

## 변경 허용 범위

**허용:**
- TeruTeruServer.SDK/Util/ 하위 신규 파일 생성
- TeruTeruServer.Client/ 하위 신규 파일 생성
- TeruTeruServer.Runtime/Testing/ 디렉토리 신설 및 하위 파일 생성
- TeruTeruServer.Runtime.Tests/Integration/ 디렉토리 신설 및 하위 파일 생성
- Documents/API/ 디렉토리 신설 및 하위 파일 생성
- Documents/Technical/ 하위 신규 파일 생성
- DummyClient/Program.cs 수정 (예제 보강만)
- .csproj 파일 수정 (XML doc 설정만)
- IMPLEMENTATION_PROGRESS.md 갱신

**금지:**
- .agents/ 수정 금지
- 기존 인터페이스 시그니처 변경 금지 (추가만 허용)
- 기존 통과 중인 테스트 삭제 또는 약화 금지
- 커밋/푸시 금지
- release gate 기준(scripts/verify-release.sh) 변경 금지
- ServerMemory.cs 수정 금지

## 검증

1. `./scripts/verify-release.sh` 통과 (오류 0개 필수)
2. 신규 통합 테스트가 모두 통과
3. `dotnet build` 시 XML 문서 생성 확인 (bin/ 하위 .xml 파일 존재)
4. 신규 파일이 untracked로 방치되지 않도록 git add (파일 단위 명시만 허용)
5. IMPLEMENTATION_PROGRESS.md가 실제 구현 상태와 일치하는지 확인
6. git status로 변경/신규 파일 목록 보고

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

★ **SDK → Runtime 순환 참조 금지**: `ProtocolEndpointInfo`는 현재 `TeruTeruServer.Runtime.Rpc` 네임스페이스에 있다. SDK에서 이 모델을 참조하려면 순환 참조가 발생한다. 해결 방법:
  - (권장) `ProtocolEndpointInfo`를 SDK 프로젝트로 이동하고, Runtime에서 SDK의 모델을 사용하도록 변경.
  - (대안) `EndpointDocGenerator`를 Runtime 프로젝트에 배치.

★ **패킷 헤더 6바이트**: M2에서 헤더가 2→6바이트로 변경되었다. PacketBuilder에서 이 6바이트 헤더(4바이트 길이 + 2바이트 프로토콜)를 정확히 구성해야 한다. SeedCryptoService의 암호화도 포함하라.

★ **MockServer의 미들웨어 순서**: MainServer.InitializePipeline() (L86-92)의 순서를 정확히 복제해야 한다: RateLimit → ReplayAttack → Decryption → Auth → Routing. 순서가 달라지면 통합 테스트 결과가 실서버와 불일치한다.

★ **ILogicService 등록 패턴**: Program.cs L57-65에서 LogicPlugin을 직접 생성하고 있다. MockServer에서도 동일한 패턴을 사용하거나, 테스트용 SimpleMockLogic을 별도 구현하라.

★ **기존 DummyClient는 실행 중**: 사용자의 DummyClient가 46시간 넘게 실행 중이다. DummyClient/Program.cs를 수정하되, 현재 실행에는 영향 없다 (재컴파일 필요). 기존 동작을 깨뜨리지 않도록 주의하라.
