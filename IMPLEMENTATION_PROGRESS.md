# 프로젝트 구현 진행 상황 (Implementation Progress)

## 현재 마일스톤: Milestone 7 — Real-time Tick & State Sync (Phase 2 시작)

- [x] **1. 서버 Tick Loop 구현**
  - [x] `IGameLoop` 인터페이스 신설
  - [x] `GameLoop` 구현체 (Stopwatch 기반 정밀 타이밍)
  - [x] DI 등록 및 Program.cs 연동
  - [x] Tick 콜백 시스템 (Logic Plugin 연동 가능 구조)

- [x] **2. 게임 상태 스냅샷 구조 설계**
  - [x] `GameEntity` 모델
  - [x] `WorldState` / `RoomState` 모델
  - [x] 스냅샷 링 버퍼 (`SnapshotBuffer`)

- [x] **3. Delta Broadcast 구현**
  - [x] `DeltaCalculator` (스냅샷 diff)
  - [x] `StateSyncProtocol` 프로토콜 추가
  - [x] Delta 패킷 직렬화 기반 구축

- [x] **4. 브로드캐스트 최적화**
  - [x] `IRoomBroadcaster` 인터페이스
  - [x] ParticipantHostIds 기반 브로드캐스터 구현
  - [x] IMessageSender 연동

- [x] **5. 입력 큐 구조**
  - [x] `InputQueue<T>` 클래스
  - [x] `GameInputProtocol` 프로토콜 추가
  - [x] Tick 처리 시 입력 큐 소비 기반 구축

## 남은 리스크 및 이슈
- GameLoop 스레드와 기존 IOCP 수신 스레드 간 동기화 주의 필요
- ProtocolSelect enum에 신규 프로토콜 추가 시 기존 클라이언트와의 호환성 확인 필수
- P2PGroup은 현재 논리 그룹 수준 → M9에서 Zone/Room 개념으로 확장 예정
