# ATS System Architecture Design

본 문서는 Phase 2 및 Phase 3에 걸쳐 전면 개편된 TeruTeruServer의 시스템 아키텍처와 핵심 설계 원칙을 기술합니다. 시스템은 높은 확장성과 유지보수성을 달성하기 위해 역할 기반의 4계층 구조와 미들웨어 파이프라인, 그리고 핫로딩이 가능한 플러그인 시스템을 채택하고 있습니다.

## 1. 4-Tier Architecture (4계층 구조)

전체 서버 시스템은 철저하게 분리된 4개의 레이어로 구성되어 단방향 의존성을 유지합니다.

### 1.1 `TeruTeruServer.SDK` (Shared Core)
- **책임**: 전체 시스템이 공유하는 핵심 계약(Interface), 통신 프로토콜 모델, 열거형(Enum) 및 어트리뷰트 선언.
- **주요 구성요소**: `ILogicService`, `IProtocolRouter`, `IMessageSender`, `ProtocolSelect`, `RpcAttribute` 등.
- **특징**: 어떠한 구현체에도 의존하지 않는 순수 추상화 계층입니다.

### 1.2 `TeruTeruServer.Runtime` (Server Engine)
- **책임**: 네트워크 세션 관리, 비동기 소켓 I/O(IOCP 기반), 패킷 수신 및 송신, 타이머 루프 등 서버의 "심장" 역할.
- **주요 구성요소**: `MainServer`, `SessionManager`, `PacketPipeline`, 미들웨어 구현체(`ValidationMiddleware`, `DecryptionMiddleware` 등).
- **데이터 흐름**: 소켓에서 바이트 배열을 수신하면, 이를 분리하여 파이프라인(미들웨어)에 태우고 검증된 패킷만 비즈니스 로직(Plugin)으로 넘깁니다.

### 1.3 `TeruTeruServer.Commands` (Logic & Handlers)
- **책임**: 인증, 그룹 매칭, P2P 시그널링 등 실제 서비스 비즈니스 로직.
- **주요 구성요소**: `LogicPlugin` (기본 제공 로직 구현체), `P2PGroupHandler`, `P2PSignalingHandler`.
- **데이터 흐름**: `[Rpc]`, `[Protocol]` 어트리뷰트가 적용된 메서드로 자동 분기되어 클라이언트의 요청을 처리하고, 결과를 `IMessageSender`를 통해 반환합니다.

### 1.4 `TeruTeruServer.Cli` (Entry Point & Config)
- **책임**: 서버 실행 파일 생성, 의존성 주입(DI) 컨테이너 설정, 설정 파일(`config.txt`) 파싱 및 플러그인 동적 로드.
- **주요 구성요소**: `Program.cs`, `ConfigManager`, `PluginManager`.

---

## 2. Dependency Injection (의존성 주입)

시스템 모듈 간의 결합도(Coupling)를 낮추기 위해 .NET 기본 `Microsoft.Extensions.DependencyInjection` 컨테이너를 활용합니다.
- **모듈 조립**: `TeruTeruServer.Cli` 계층에서 `ISessionManager`, `IMessageSender` 등의 런타임 구현체를 싱글톤으로 등록합니다.
- **생성자 주입(Constructor Injection)**: `MainServer`나 `LogicPlugin` 등은 생성자를 통해 추상 인터페이스를 주입받아 작동하며, 이는 완벽한 단위 테스트(Mocking)를 가능하게 합니다.

---

## 3. Middleware Pipeline (패킷 처리 규약)

수신된 원시 소켓 데이터는 다음 5단계의 미들웨어 파이프라인을 거칩니다.

1. **Decryption (복호화)**: 암호화된 패킷 페이로드를 평문으로 복호화.
2. **Validation (검증)**: 패킷 길이, 프로토콜 헤더 등 구조적 무결성 검증.
3. **Auth (인증)**: 토큰 기반 패킷일 경우 JWT 만료 및 권한 검사.
4. **Routing (라우팅)**: `ProtocolRouter`를 통해 패킷 헤더(`ProtocolSelect`)를 분석하고 적합한 핸들러(`LogicPlugin`)로 연결.
5. **Logic (비즈니스 로직)**: 최종적인 서비스 비즈니스 로직 실행 (`HandleLogin`, P2P 시그널링 등).

> [!NOTE]
> 유효하지 않은 패킷은 파이프라인 도중에 즉시 버려지며(Drop), 상위 비즈니스 로직은 철저하게 검증된 안전한 데이터만 보장받습니다.

---

## 4. Hot-Loading (플러그인 동적 교체)

서버 런타임 종료 없이 비즈니스 로직을 교체할 수 있는 구조입니다.
- **`PluginManager`**: `plugins/` 폴더 내의 `.dll` 파일을 FileSystemWatcher로 모니터링합니다.
- 변경 감지 시 `AssemblyLoadContext`를 통해 새로운 어셈블리를 메모리에 로드하고, `ILogicService` 인터페이스를 구현하는 새 플러그인 객체를 생성하여 라우터(`IProtocolRouter`)에 재연결(Swap)합니다.
