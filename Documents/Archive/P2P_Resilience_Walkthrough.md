# [TeruTeruServer] P2P Network Resilience & Multicast Walkthrough

TeruTeruServer의 단일 TCP 리스너 기반 통신에서 벗어나, 유연한 세션 관리, 모바일 네트워크 전환 시 재접속(Reconnect), UDP 홀펀칭, 그리고 P2P 멀티캐스트를 지원하는 새로운 인프라 구축이 완료되었습니다.

---

## 🛠 주요 변경 사항

### 1. 연결 단절 유연성 (Session Resilience)
- **`ClientSession` 객체화**: 소켓에 의존하던 연결 관리가 상태(`SessionState`: Connected, Grace, Disconnected) 기반 세션 관리로 진화했습니다.
- **Grace 유예 기간**: 일시적인 통신 단절 시 서버는 소켓 오류를 감지해도 사용자를 즉시 강퇴하지 않고, `Grace` 모드로 변경 후 30초의 유예 기간을 제공합니다.
- **재접속(Reconnect) 지원**: 클라이언트는 30초 내에 `ProtocolSelect.ReconnectProtocol`과 `ReconnectToken`을 사용하여 접속하면 기존 HostID를 그대로 유지하며 소켓만 최신화할 수 있습니다 (`AuthMiddleware`에 구현됨).

### 2. P2P Hole Punching & Relay Fallback
서버 부하 감소 및 저지연 통신을 위해 클라이언트 간 직접(Direct) P2P 통신 인프라가 도입되었습니다.
- **서버 자체 STUN**: 클라이언트는 TCP 접속 후 `UdpRegisterProtocol`을 서버 UDP 포트로 전송하며, 서버는 이 패킷의 출발지 IP/Port를 `ClientSession.UdpEndPoint`에 기록합니다. (별도의 STUN 서버 불필요)
- **시그널링 (Signaling)**: 클라이언트 A가 B와 통신하고자 할 때 서버로 `HolePunchRequest`를 전송하면, 서버는 양측에 서로의 `PeerEndpointInfo`를 교환해 주어 동시 UDP Ping을 유도합니다.
- **릴레이 중계기**: NAT/방화벽으로 인해 직접 통신에 실패할 경우, 서버가 순수 바이트 단위로 패킷을 타겟에게 즉각 포워딩해 주는 `P2PRelayProtocol`이 구현되었습니다.

### 3. P2P 그룹 멀티캐스트 (Group Management)
다중 사용자가 동시에 위치나 화면을 공유할 수 있는 Room/Group 인프라입니다.
- **방 생성 및 입장**: `P2PGroup` 모델을 도입하여 멤버 관리를 고도화했습니다. 신규 사용자가 그룹에 진입하면 기존 사용자들에게 자동으로 홀펀칭 교환(`HolePunchRequest`)이 브로드캐스팅됩니다.
- **하이브리드 멀티캐스트 (Selective Multicast)**: 송신자는 홀펀칭에 성공한 Peer에게는 직접 패킷을 전송하고, 실패한 소수 Peer에게만 묶어서 서버로 `GroupRelayProtocol`을 보냅니다. 서버는 이를 파싱해 실패한 인원들에게만 분배 전송을 수행합니다.

---

## 🚦 검증 및 결과 (Validation)

- **TDD (Test-Driven Development)**:
  - `ClientSessionTests.cs`, `SessionManagerTests.cs`, `P2PGroupTests.cs` 등 핵심 객체에 대한 11개의 단위 테스트를 작성하여 모든 로직의 건전성을 확보했습니다.
  - 특히 `SessionManager`에서 동일 ID 발급/삭제 과정의 Concurrent 충돌을 완전히 해소했습니다.
  - `MainServerTests.cs` 신규 생성으로 런타임 초기화 및 Socket Check 타이머에 대한 검증을 수행했습니다.
- **컴파일 무결성**:
  - `LogicPlugin`과 `MainServer` 등 핵심 런타임에 새로운 파이프라인 핸들러(`P2PSignalingHandler`, `P2PGroupHandler` 등) 의존성을 성공적으로 주입(DI) 하였고, Nullable 처리를 보강하여 경고 0, 오류 0으로 빌드 성공을 확인했습니다.

> [!NOTE]
> 이제 코어 서버의 통신 인프라는 완성되었습니다. 향후 클라이언트(Unity, C# 콘솔 등)에서 위 프로토콜 순서에 맞춰 접속-UDP등록-그룹입장 시나리오를 구성하면 됩니다.
