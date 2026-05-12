# 프로젝트 구현 진행 상황 (Implementation Progress)

## 현재 마일스톤: Milestone 11 — Game Session & Matchmaking

- [ ] **1. 매치메이킹 큐 시스템**
  - [ ] MatchQueue 구현 (플레이어 등록/해제/매칭)
  - [ ] MMR 기반 그룹화 로직
  - [ ] Tick 핸들러로 주기적 매칭 시도

- [ ] **2. 게임 세션 생명주기 관리**
  - [ ] GameSession 모델 + 상태 머신 (Lobby→MatchFound→Loading→InGame→Result→Disbanded)
  - [ ] IGameSessionManager 인터페이스 및 구현체
  - [ ] Grace 재연결 시 세션 복귀 지원

- [ ] **3. 팀 구성 및 밸런싱**
  - [ ] 자동 팀 배정 (MMR 균등 분배)
  - [ ] 팀 수 및 팀당 인원 설정

- [ ] **4. 관전자(Spectator) 모드 지원**
  - [ ] Read-only 참여 (브로드캐스트 수신만)
  - [ ] 관전자 입장/퇴장 관리

- [ ] **5. 게임 결과 처리 및 보상 훅**
  - [ ] OnGameEnd 이벤트 발행 (IEventBus)
  - [ ] GameResult 모델 정의

- [x] **Milestone 10 — Game Security & Anti-Cheat (Completed)**
- [x] **Milestone 9 — Zone & World Management (Completed)**

## 남은 리스크 및 이슈
- 매치메이킹 큐 대기 시간 상한 정책 필요
- 비대칭 팀(예: 1v5) 장르 지원 시 밸런싱 로직 확장 필요
- Grace 재연결 타이밍과 세션 상태 전이 충돌 처리 주의
