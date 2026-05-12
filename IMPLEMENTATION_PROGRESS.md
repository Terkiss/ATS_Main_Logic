# 프로젝트 구현 진행 상황 (Implementation Progress)

## 현재 마일스톤: Milestone 10 — Logic Plugin Optimization & Security

- [ ] **1. Logic Plugin 고성능화**
  - [ ] RMI Source Generator 도입 (코드 생성 기반 RPC)
  - [ ] 패킷 시리얼라이저 최적화 (Memory<T> 활용)

- [ ] **2. 고급 안티 치트 및 검증**
  - [ ] 이동 경로 적분(Path Integration) 검증
  - [ ] 패킷 타임스탬프 무결성 검사

- [ ] **3. 서버 측 AI 고도화**
  - [ ] ServerEntity 기반의 FSM AI 로직 구현
  - [ ] 대규모 NPC Tick 분산 처리

- [x] **Milestone 9 — Zone & World Management (Completed)**
  - [x] Zone / Room / Channel 계층 설계
  - [x] 공간 기반 관심 영역 (AoI) 필터링 (SpatialGrid)
  - [x] 동적 인스턴스 생성·소멸 (ZoneFactory)
  - [x] Zone Transfer 프로토콜 및 로직
  - [x] NPC/몬스터 엔티티 모델 (ServerEntity)

## 남은 리스크 및 이슈
- SpatialGrid 셀 크기 최적값은 게임 장르에 따라 다름 → 설정 가능해야 함
- Zone 간 이동 시 엔티티 소유권 이전 동기화 주의 필요
- RoomState와 Zone의 관계 정리 필요 (Zone이 RoomState를 포함하는 구조)
