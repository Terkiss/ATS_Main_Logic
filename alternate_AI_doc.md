# AI를 위한 TeruTeruServer 기술 문서

이 문서는 TeruTeruServer 프로젝트에 대한 포괄적인 기술 개요를 제공합니다. 대규모 컨텍스트 창(최대 100만 토큰)을 가진 AI가 시스템의 아키텍처, 로직 흐름 및 구현 세부 사항을 이해할 수 있도록 작성되었습니다.

---

## 1. 프로젝트 개요

**TeruTeruServer**는 실시간 통신, P2P 시그널링 및 그룹 기반 상호 작용을 위해 설계된 고성능의 확장 가능한 C# 서버 엔진입니다. IOCP에서 영감을 받은 비동기 소켓 모델(`SocketAsyncEventArgs`), 미들웨어 기반 패킷 처리 파이프라인, 그리고 비즈니스 로직을 위한 모듈식 플러그인 기반 아키텍처를 활용합니다.

### 주요 목표:

* **확장성 (Scalability)**: 비동기 I/O를 사용하여 높은 동시 연결을 처리합니다.
* **모듈성 (Modularity)**: 4계층 아키텍처를 통한 관심사 분리.
* **유지보수성 (Maintainability)**: 의존성 주입(DI) 및 깔끔한 인터페이스.
* **유연성 (Flexibility)**: 로직 플러그인 핫 로딩(Hot-loading) 및 사용자 정의 가능한 미들웨어 파이프라인.
* **P2P 지원**: UDP 홀 펀칭(Hole Punching)을 위한 내장 시그널링 및 서버 측 릴레이 폴백(fallback).

---

## 2. 아키텍처: 4계층 모델

시스템은 단방향 의존성과 깔끔한 분리를 보장하기 위해 4개의 계층으로 엄격하게 나뉩니다.

### 2.1 `TeruTeruServer.SDK` (공유 코어)

* **역할**: 계약(Contracts), 모델 및 공유 유틸리티의 정의.
* **의존성 없음**: 솔루션 내의 다른 어떤 프로젝트에도 의존하지 않습니다.
* **주요 구성 요소**:
* **인터페이스**: `ILogicService`, `ISessionManager`, `IMessageSender`, `IProtocolRouter`.
* **열거형(Enums)**: `SendType` (Direct/Json), `ProtocolSelect`, `SessionState` (Connected/Grace/Disconnected).
* **모델**: `RpcRequest`, `P2PGroup`, `CommonProtocols`.
* **유틸리티**: 암호화(AES, Seed), `PacketUtility`, `TeruTeruLogger`.
* **세션**: 연결 상태, 토큰 및 P2P 상태를 추적하는 `ClientSession` 객체.



### 2.2 `TeruTeruServer.Runtime` (서버 엔진)

* **역할**: 서버의 "심장". 네트워킹, 세션 및 처리 파이프라인을 관리합니다.
* **주요 구성 요소**:
* **`MainServer`**: TCP/UDP 리스너를 오케스트레이션하고, 연결을 수락하며, 수신 루프를 시작합니다.
* **`PacketPipeline`**: 들어오는 `PacketContext`를 처리하는 미들웨어 체인입니다.
* **`SessionManager`**: 재연결을 위한 "유예(Grace)" 기간 처리를 포함하여 `ClientSession` 객체의 수명 주기를 관리합니다.
* **`RpcProxy` & `ProtocolRouter**`: 속성(Attributes)을 기반으로 적절한 핸들러에 패킷을 전달(Dispatch)합니다.



### 2.3 `TeruTeruServer.Logic.Default` (비즈니스 로직 플러그인)

* **역할**: 특정 서비스 로직의 구현.
* **구현**: `ILogicService`를 구현합니다.
* **주요 핸들러**:
* **`P2PSignalingHandler`**: 홀 펀칭 요청 및 엔드포인트 교환을 관리합니다.
* **`P2PRelayHandler`**: 직접 P2P 연결이 실패할 때 폴백(fallback) 릴레이를 처리합니다.
* **`P2PGroupHandler`**: 다중 사용자 상호 작용을 위한 논리적 그룹을 관리합니다.
* **`LogicPlugin`**: 플러그인의 주요 진입점으로, `[Protocol]` 또는 `[Rpc]` 속성으로 장식된 메서드를 포함합니다.



### 2.4 `TeruTeruServer.Cli` (진입점)

* **역할**: 호스트 애플리케이션. DI(의존성 주입)를 설정하고, 구성을 로드하며, `MainServer`를 시작합니다.
* **주요 구성 요소**:
* **`ConfigManager`**: `config.txt`를 읽습니다.
* **`PluginManager`**: `plugins/` 디렉터리에서 `.dll` 파일을 동적으로 로드하고 변경 사항을 모니터링할 수 있습니다(핫 로딩).



---

## 3. 네트워크 및 프로토콜 사양

### 3.1 패킷 구조

송수신되는 모든 패킷은 특정 바이트 수준의 형식을 따릅니다.

1. **헤더 (2바이트)**:
* `[0]`: `SendType` (0: Direct, 1: Json)
* `[1]`: `ProtocolSelect` (프로토콜 식별자)


2. **페이로드 (가변 길이)**:
* `SendType.Json`인 경우: UTF-8로 인코딩된 JSON 문자열입니다.
* `SendType.Direct`인 경우: 원시(Raw) 바이너리 데이터 (종종 보안을 위해 JWT와 같은 추가 헤더가 접두사로 붙습니다).



### 3.2 주요 프로토콜 (`ProtocolSelect`)

* `ConnectProtocol (1)`: 초기 연결 핸드셰이크.
* `LoginProtocol (2)`: 인증 및 JWT 발급.
* `ReconnectProtocol (3)`: `ReconnectToken`을 사용하여 세션 재개.
* `UdpRegisterProtocol (4)`: 홀 펀칭을 위한 공개 UDP 엔드포인트 등록.
* `HolePunchRequest (5)`: P2P 통신을 위한 피어(peer) 정보 요청.
* `RpcProtocol (100)`: 범용 원격 프로시저 호출(RPC).

---

## 4. 패킷 처리 파이프라인

`MainServer`는 수신된 모든 바이트 배열을 `PacketPipeline`으로 전달합니다. 파이프라인은 일련의 미들웨어를 순서대로 실행합니다:

1. **`ValidationMiddleware`**: 패킷이 최소 길이 및 기본 헤더 요구 사항을 충족하는지 확인합니다.
2. **`DecryptionMiddleware`**: 페이로드가 암호화되어 있는 경우 이를 복호화합니다.
3. **`AuthMiddleware`**:
* 민감한 프로토콜의 경우, 페이로드에서 JWT를 추출합니다.
* `SessionManager`를 통해 토큰의 유효성을 검사합니다.
* 인증된 `ClientSession`을 `PacketContext`에 연결합니다.


4. **`RoutingMiddleware`**: `IProtocolRouter`를 사용하여 `LogicPlugin`에서 `ProtocolSelect`와 일치하는 메서드를 찾아 호출합니다.

---

## 5. P2P 및 그룹 로직

서버는 단계별 접근 방식을 통해 P2P 통신을 용이하게 합니다:

1. **UDP 등록**: 클라이언트는 UDP를 통해 서버에 `Direct` 패킷을 보냅니다. 서버는 그들의 공인 IP/포트를 `ClientSession.UdpEndPoint`에 기록합니다.
2. **시그널링 (Signaling)**: 클라이언트 A가 클라이언트 B에 연결하려고 할 때 `HolePunchRequest`를 보냅니다. 서버는 클라이언트 B의 공개 엔드포인트를 A에게, 클라이언트 A의 공개 엔드포인트를 B에게 보냅니다.
3. **릴레이 폴백 (Relay Fallback)**: (Symmetric NAT 등의 이유로) 홀 펀칭이 실패할 경우, 클라이언트는 `P2PRelayProtocol` 또는 `GroupRelayProtocol`을 사용하여 패킷을 보냅니다. 그러면 서버는 이 패킷들을 의도된 수신자에게 전달(포워딩)합니다.

---

## 6. 세션 수명 주기 및 재연결

* **Connected (연결됨)**: 활성화된 소켓 연결 상태입니다.
* **Grace (유예)**: 소켓 연결이 끊어지면, 세션은 30초(구성 가능) 동안 `Grace` 상태로 전환됩니다.
* **Reconnection (재연결)**: 클라이언트는 `ReconnectToken`을 사용하여 유예 기간 내에 재연결할 수 있습니다. 성공할 경우, 새 소켓이 기존 `ClientSession`에 연결되어 상태가 보존됩니다.
* **Eviction (퇴거/제거)**: 유예 기간이 만료되면 세션이 완전히 제거되고 정리 로직(예: 그룹에서 제거)이 트리거됩니다.

---

## 7. 개발 및 확장성

### 새로운 로직 추가

새로운 기능을 추가하려면:

1. (SDK의) `ProtocolSelect`에 새 프로토콜을 정의합니다.
2. `LogicPlugin` (Logic 계층)에 메서드를 추가하고 `[Protocol(ProtocolSelect.YourProtocol)]` 속성으로 장식합니다.
3. RPC를 사용하는 경우 `[Rpc("MethodName")]`으로 장식합니다.
4. 로직 프로젝트를 다시 빌드하고 `.dll` 파일을 `plugins/` 폴더에 배치합니다.

### 구성 (`config.txt`)

포함된 매개변수는 다음과 같습니다:

* `Port`: 서버 수신 대기 포트.
* `MaxConnection`: 허용되는 최대 세션 수.
* `IsTcp` / `IsUdp`: 프로토콜 사용 여부 토글.
* `Guid`: 고유한 서버 식별자.
* `SendBufferSize` / `ReceiveBufferSize`: 네트워크 튜닝 값.

---

## 8. 데이터베이스 통합

서버는 `IDatabaseService`를 통해 데이터베이스 작업을 위한 표준 인터페이스를 제공합니다.

* **`DatabaseConnector`**: `MySql.Data`를 사용하는 MySQL 연결을 위한 래퍼(wrapper)입니다.
* **`DatabaseHelper`**: `IDatabaseService`를 구현하며 스칼라, 리더(Reader) 및 비쿼리(Non-query) 명령을 실행하기 위한 메서드를 제공합니다.
* **DI(의존성 주입) 사용**: 데이터베이스 서비스는 일반적으로 시작 시 DI 컨테이너에 등록되므로, 로직 플러그인이 영속성 작업을 매끄럽게 수행할 수 있습니다.

---

## 9. 콘솔 명령어

서버에는 관리 작업을 위한 명령줄 인터페이스(CLI)가 포함되어 있습니다.

* **`CommandHandler`**: 콘솔 입력을 구문 분석하여 `ICommand` 구현체로 전달합니다.
* **주요 명령어**:
* `exit`: 서버를 안전하게(Gracefully) 종료합니다.
* `Queue_Count`: 현재 대기 중인 작업/메시지의 수를 표시합니다.
* `Worker_Start`: 백그라운드 작업자 스레드를 관리합니다.
* `2` (ImageDump): 디버깅을 위한 이미지 덤프를 트리거합니다.



---

## 10. 주요 구현 세부 정보 (내부 참조용)

* **`MainServer.cs`**: 연결당 스레드 오버헤드를 방지하기 위해 `SocketAsyncEventArgs`를 사용합니다. `AcceptLoop`와 `ReceiveLoop`는 완전히 비동기적으로 작동합니다.
* **`ServerMemory.cs`**: HostID 또는 GameID로 세션을 빠르게 조회하기 위해 사용되는 SDK 내의 정적 저장소입니다.
* **`PacketContext.cs`**: 미들웨어 파이프라인을 통해 `Socket`, `Data` 및 `ClientSession`을 전달합니다.
* **스레드 안전성 (Thread Safety)**: 세션 및 그룹 관리에 `ConcurrentDictionary`를 사용합니다.

---

## AI 컨텍스트 요약

이 코드베이스에서 작업할 때는 SDK를 순수하고 구현에 구애받지 않도록(implementation-agnostic) 유지하는 것을 우선시하십시오. 비즈니스 로직은 항상 Logic 프로젝트/플러그인에 존재해야 하며, 네트워크 수준의 최적화나 파이프라인 변경은 Runtime 프로젝트에서 이루어져야 합니다. 새로운 프로토콜이 미들웨어 파이프라인(특히 보안이 필요한 경우 `AuthMiddleware`)에 적절하게 통합되었는지 항상 확인하십시오.
