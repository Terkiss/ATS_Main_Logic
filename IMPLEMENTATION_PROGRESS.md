# 프로젝트 구현 진행 상황 (Implementation Progress)

## 현재 마일스톤: Milestone 1 — Core Stability & Observability

- [x] **1. 구조화 로깅 도입**
  - [x] Serilog 패키지 설치 및 초기화 설정
  - [x] `TeruTeruLogger`를 구조화된 로그 지원 포맷으로 리팩터링
  - [x] JSON 포맷 파일 롤링 로그 파일 출력 설정 적용

- [x] **2. 파이프라인 프로파일링**
  - [x] 미들웨어 단계(`Validation` → `Decryption` → `Auth` → `Routing`)별 타임스탬프/소요시간 로직 추가
  - [x] 임계치 초과 시 성능 지연 경고 로깅 기능 구현

- [x] **3. 실시간 메트릭 모니터링**
  - [x] 현재 연결 세션 수 추적 로직 구성
  - [x] 초당 패킷 처리량(TPS) 및 큐 길이 측정 데이터 수집 로직 구현

- [x] **4. Health Check 및 크래시 덤프**
  - [x] `health` 콘솔 커맨드 추가 및 메트릭 출력 연동
  - [x] 글로벌 예외 핸들러 등록 및 `crashdump` 파일 자동 생성 로직 추가

## 남은 리스크 및 이슈
- 외부 모니터링 도구(Grafana, Datadog 등)로의 Push 방식 확장은 서버 구조가 안정화된 이후나 필요에 따라 점진적으로 도입할 예정입니다.
- 플러그인 로드/언로드 시의 로깅 컨텍스트 분리 문제는 Milestone 4(Plugin Ecosystem) 진행 시 추가로 점검해야 합니다.
