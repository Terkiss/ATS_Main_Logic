# 프로젝트 구현 진행 상황 (Implementation Progress)

## 현재 마일스톤: Milestone 10 — Game Security & Anti-Cheat

- [ ] **1. 서버 사이드 이동 검증 강화 (Path Integration)**
  - [ ] ServerAuthorityValidator에 경로 적분 검증 추가
  - [ ] 텔레포트/속도핵 탐지 (연속 틱 간 이동 거리 누적 비교)
  - [ ] 위반 감지 시 SecurityEvent 발행

- [ ] **2. 게임 입력 빈도 검증**
  - [ ] GameInput 패킷 수신 빈도 추적 (세션별)
  - [ ] 틱레이트 대비 비정상 빈도 감지 및 플래그 처리

- [ ] **3. 패킷 HMAC 무결성 검사**
  - [ ] HMAC-SHA256 서명 검증 미들웨어 구현
  - [ ] 변조 패킷 감지 시 세션 즉시 차단

- [ ] **4. 행동 이상 탐지 로깅 (SecurityEvent)**
  - [ ] SecurityEvent 모델 정의
  - [ ] SecurityEventLog 수집기 구현
  - [ ] 이벤트 기반 사후 분석 지원

- [ ] **5. 자동 제재 파이프라인**
  - [ ] ClientSession에 위반 카운터 및 제재 상태 필드 추가
  - [ ] 경고 → 임시 차단 → 영구 차단 자동 처리 로직
  - [ ] 제재 미들웨어 통합

- [x] **Milestone 9 — Zone & World Management (Completed)**
  - [x] Zone / Room / Channel 계층 설계
  - [x] 공간 기반 관심 영역 (AoI) 필터링 (SpatialGrid)
  - [x] 동적 인스턴스 생성·소멸 (ZoneFactory)
  - [x] Zone Transfer 프로토콜 및 로직
  - [x] NPC/몬스터 엔티티 모델 (ServerEntity)

## 남은 리스크 및 이슈
- HMAC 키 배포 전략 결정 필요 (세션별 키 vs 전역 키)
- 자동 제재의 오탐(false positive) 허용 범위 정책 필요
- 패킷 HMAC 추가 시 기존 클라이언트 SDK 호환성 고려 필요
