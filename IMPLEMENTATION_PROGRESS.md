# 프로젝트 구현 진행 상황 (Implementation Progress)

## 현재 마일스톤: Milestone 3 — P2P Engine Maturity

- [x] **1. Symmetric NAT 자동 감지 및 릴레이 전환**
  - [x] NAT 유형 판별 및 P2PRelayHandler 자동 전환 로직
  - [x] Hole Punching 실패 타임아웃 처리

- [x] **2. 릴레이 QoS 제어**
  - [x] `GroupRelayProtocol` 대역폭 상한 및 우선순위 적용

- [x] **3. P2P 연결 품질 측정**
  - [x] RTT 핑, 패킷 손실률을 `ClientSession`에 주기적으로 갱신

- [x] **4. P2PGroup 멤버 이벤트 훅**
  - [x] `OnJoin`, `OnLeave`, `OnRelaySwitch` 이벤트를 Logic Plugin에 노출

- [x] **5. UDP 패킷 순서 보장 옵션**
  - [x] Sequence Number 레이어 기반 선택적 순서 보장 모드 제공

## 남은 리스크 및 이슈
- Symmetric NAT 환경에서 UDP 릴레이 전환 시 서버의 트래픽 부하 최적화 방안이 필요할 수 있습니다.
