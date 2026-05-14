# TeruTeruServer — Engine Milestone Roadmap

> 본 문서는 TeruTeruServer의 현재 아키텍처(4-Tier, IOCP 비동기 소켓, 미들웨어 파이프라인, P2P 시그널링)를 기반으로 작성된 공식 개발 마일스톤 로드맵입니다.

---

## 우선순위 요약

| 순서 | 마일스톤 | 핵심 목표 | 선행 조건 |
|:---:|---|---|---|
| 1 | Core Stability & Observability | 운영 가시성 확보 | 없음 |
| 2 | Security Hardening | 보안 레이어 완성 | M1 |
| 3 | P2P Engine Maturity | P2P 엔진 완성도 향상 | M2 |
| 4 | Plugin Ecosystem & Hot-reload | 플러그인 생태계 완성 | M1 |
| 5 | Scalability & Clustering | 수평 확장 구조 | M2, M4 |
| 6 | Developer Experience & SDK | 개발자 경험 완성 | M5 |

---

## Milestone 1 — Core Stability & Observability

> **기반 안정화 및 운영 가시성 확보**

운영 환경 투입 이전에 반드시 선행되어야 하는 마일스톤. 현재 `TeruTeruLogger`와 `CommandHandler`가 기초 수준에 머물러 있으며, 파이프라인 내부 처리 흐름에 대한 가시성이 부족한 상태.

### 작업 항목

- **구조화 로깅 도입**: Serilog 또는 NLog 연동, JSON 포맷 로그 출력 지원
- **실시간 메트릭 노출**: 현재 연결 수, 패킷 처리량, 큐 길이를 폴링 또는 Push 방식으로 노출
- **Health Check 엔드포인트**: HTTP 미니 서버 또는 콘솔 커맨드 확장을 통한 서버 상태 점검
- **파이프라인 프로파일링**: 미들웨어 단계(`Validation` → `Decryption` → `Auth` → `Routing`)별 처리 시간 측정
- **크래시 리포트 자동 덤프**: 비정상 종료 시 스택 트레이스 및 세션 상태 자동 저장

### 완료 기준

- 모든 패킷이 처리 단계별 타임스탬프와 함께 로깅됨
- 외부 모니터링 도구(Grafana, Datadog 등)에서 메트릭 수집 가능한 상태

---

## Milestone 2 — Security Hardening

> **보안 레이어 완성**

현재 AES/Seed 암호화와 JWT 인증 구조가 설계 수준으로 존재하나, 실제 공격 벡터에 대한 대응이 미흡. 보안 완성 없이는 이후 확장 마일스톤으로 진행 불가.

### 작업 항목

- **JWT Refresh Token 흐름 구현**: Access Token 만료 시 재발급 사이클 설계 및 `LoginProtocol` 연동
- **Rate Limiting 미들웨어 추가**: 패킷 플러딩(DoS) 방어를 위한 연결별 요청 속도 제한
- **Replay Attack 방어**: Packet Sequence Number 또는 Nonce 도입으로 재전송 공격 차단
- **`AuthMiddleware` 강제화**: 보안 등급 태그(`[RequiresAuth]`)를 통해 우회 불가 구조 확립
- **Pluggable Crypto Interface**: 암호화 알고리즘을 인터페이스로 추상화하여 교체 용이성 확보

### 완료 기준

- Replay Attack 및 Rate Flooding 시나리오 테스트 통과
- 보안 등급이 지정된 모든 프로토콜이 `AuthMiddleware`를 반드시 통과함

---

## Milestone 3 — P2P Engine Maturity

> **P2P 시그널링·릴레이 엔진 완성도 향상**

Hole Punching + Relay Fallback 구조는 설계되어 있으나, Symmetric NAT 등 엣지케이스 미처리 가능성이 높음. TeruTeruServer의 핵심 차별화 기능인 P2P 엔진의 완성도를 높이는 단계.

### 작업 항목

- **Symmetric NAT 자동 감지 및 릴레이 전환**: NAT 유형 판별 후 Hole Punching 실패 시 자동으로 `P2PRelayHandler`로 전환
- **릴레이 QoS 제어**: `GroupRelayProtocol` 사용 시 대역폭 상한 및 우선순위 적용
- **P2P 연결 품질 측정**: RTT 핑, 패킷 손실률을 `ClientSession`에 기록하고 주기적으로 갱신
- **`P2PGroup` 멤버 이벤트 훅**: `OnJoin`, `OnLeave`, `OnRelaySwitch` 등 그룹 상태 변화를 Logic Plugin에서 구독 가능하도록 노출
- **UDP 패킷 순서 보장 옵션**: Sequence Number 레이어를 추가해 선택적 순서 보장 모드 제공

### 완료 기준

- Symmetric NAT 환경에서 릴레이 자동 전환 성공률 100%
- `P2PGroup` 이벤트가 Logic Plugin에서 정상적으로 수신됨

---

## Milestone 4 — Plugin Ecosystem & Hot-reload

> **로직 플러그인 생태계 완성**

현재 `PluginManager`가 DLL 동적 로드를 지원하지만 Hot-reload는 부분적. 플러그인 생태계가 완성되면 Logic 개발과 엔진 개발을 완전히 독립적으로 진행 가능.

### 작업 항목

- **무중단 플러그인 교체**: 언로드 → 재로드 사이클 완성 (서버 재시작 없이 DLL 교체)
- **플러그인 간 의존성 관리**: 플러그인 A가 플러그인 B의 서비스를 참조할 수 있는 구조 허용
- **어트리뷰트 자동 문서화**: `[Protocol]` / `[Rpc]` 어트리뷰트 정보를 런타임 메타로 추출하여 API 목록 자동 생성
- **플러그인 샌드박스**: 잘못된 플러그인이 엔진 크래시를 유발하지 않도록 AppDomain 또는 별도 프로세스로 격리
- **SDK NuGet 패키징**: `TeruTeruServer.SDK`를 외부 개발자가 참조 가능한 NuGet 패키지로 배포

### 완료 기준

- 운영 중 DLL 교체 후 신규 `[Protocol]` 핸들러가 즉시 동작함
- 비정상 플러그인 로드 시 엔진이 종료되지 않고 오류 로그만 기록됨

---

## Milestone 5 — Scalability & Clustering

> **단일 서버 → 수평 확장 구조**

현재 `ServerMemory.cs`가 static(단일 프로세스 내)으로 설계되어 있어, 멀티 인스턴스 환경에서는 세션 공유가 불가. 이 마일스톤에서 엔진의 수평 확장 기반을 구축.

### 작업 항목

- **`ISessionManager` 백엔드 추상화**: Redis 기반 세션 저장소로 교체 가능하도록 인터페이스 분리
- **서버 간 Grace 재연결 지원**: `ReconnectToken`을 사용한 재연결이 다른 서버 인스턴스에서도 성공하도록 세션 공유
- **Stateless 설계 가이드 확립**: 로드밸런서(L4/L7) 친화적인 구조 설계 원칙 문서화
- **`P2PGroup` 분산 저장소 연동**: Redis Pub/Sub 또는 메시지 브로커(RabbitMQ 등)를 통한 그룹 이벤트 브로드캐스트
- **클러스터 관리 콘솔**: 마스터 노드 개념 도입 및 서버 인스턴스 상태 모니터링 UI

### 완료 기준

- 2개 이상의 인스턴스 운영 중 한 인스턴스 장애 시 Grace 재연결이 다른 인스턴스에서 성공함
- `P2PGroup` 이벤트가 클러스터 전체에 정상 전파됨

---

## Milestone 6 — Developer Experience & SDK Finalization

> **외부·팀 확장을 위한 개발자 경험 완성**

엔진이 안정화된 이후, 생태계 확장과 팀 생산성 극대화를 위한 마일스톤. TeruTeruServer를 내부 도구에서 플랫폼 수준으로 격상.

### 작업 항목

- **공식 SDK 문서 자동 생성**: XML 주석 → DocFX / MkDocs 연동으로 API 레퍼런스 자동화
- **클라이언트 SDK 제공**: C# / Unity용 패킷 빌더 및 핸들러 템플릿 제공 (서버와 동일한 `ProtocolSelect` 기반)
- **로컬 Mock 서버 모드**: 실서버 없이 Logic Plugin만으로 패킷 흐름을 테스트 가능한 개발 환경
- **통합 테스트 프레임워크**: 패킷 시뮬레이터 인젝션을 통한 엔드-투-엔드 테스트 자동화
- **마이그레이션 가이드**: 버전 간 `ProtocolSelect` 호환성 관리 및 Breaking Change 정책 문서화

### 완료 기준

- 외부 개발자가 SDK 문서만으로 Logic Plugin을 독립적으로 개발 및 배포 가능
- 모든 핵심 프로토콜에 대한 통합 테스트가 CI 파이프라인에서 자동 실행됨

---

# Phase 2 — Game Server Edition

> TeruTeruServer를 MMORPG, FPS 등 **실시간 게임 서버**로 활용하기 위한 확장 로드맵입니다.
> Phase 1 (M1-M6)의 엔진 기반 위에 구축됩니다.

## Phase 2 우선순위 요약

| 순서 | 마일스톤 | 핵심 목표 | 선행 조건 |
|:---:|---|---|---|
| 7 | Real-time Tick & State Sync | 게임 루프 및 상태 동기화 기반 구축 | M6 |
| 8 | Lag Compensation & Prediction | 지연 보상 및 클라이언트 예측 | M7 |
| 9 | Zone & World Management | 공간 분할 및 월드 관리 | M7 |
| 10 | Game Security & Anti-Cheat | 게임 특화 보안 및 치트 방지 | M8 |
| 11 | Game Session & Matchmaking | 매치메이킹 및 게임 세션 관리 | M9 |
| 12 | Live Operations & Scalability | 무중단 운영 및 서버 확장 | M10, M11 |

---

## Milestone 7 — Real-time Tick & State Sync

> **게임 루프 및 상태 동기화 기반 구축**

실시간 게임 서버의 핵심은 일정한 주기(Tick)로 게임 상태를 갱신하고 모든 클라이언트에 브로드캐스트하는 것.
현재 `PacketPipeline`은 수신 이벤트 기반(Reactive)으로, 능동적 Tick 루프 구조가 부재.

### 작업 항목

- **서버 Tick Loop 구현**: 고정 주기(예: 20Hz MMORPG / 64Hz FPS) 기반의 `GameLoop` 스레드 도입
- **게임 상태 스냅샷 구조 설계**: `WorldState` / `RoomState` 등 스냅샷 단위로 상태를 관리하는 모델 정의
- **Delta Broadcast**: 매 Tick마다 변경된 부분만 클라이언트에 전송하는 Delta Sync 방식 구현
- **브로드캐스트 최적화**: `P2PGroup`을 활용한 룸·채널 단위 멀티캐스트, 불필요한 전체 전송 제거
- **입력 큐 구조**: 클라이언트 입력을 Tick 단위로 묶어 처리하는 `InputQueue` 설계

### 완료 기준

- 서버 Tick이 목표 Hz에서 ±2ms 오차 이내로 안정적으로 동작
- 10개 이상의 클라이언트가 동일 룸에서 Delta Sync 수신 성공

---

## Milestone 8 — Lag Compensation & Prediction

> **지연 보상 및 클라이언트 예측**

FPS, 액션 MMORPG 등 고속 실시간 게임에서 네트워크 지연(Latency)은 게임플레이를 직접적으로 훼손.
서버 권위(Server Authority)를 유지하면서도 클라이언트가 즉각적으로 반응하는 구조가 필요.

### 작업 항목

- **서버 권위 모델 확립**: 모든 게임 상태의 최종 판정은 서버에서 수행하는 원칙 구현
- **클라이언트 사이드 예측(CSP) 지원 인터페이스**: 클라이언트가 로컬 예측 후 서버 보정을 받을 수 있도록 `AckSequence` 기반 프로토콜 설계
- **Lag Compensation (히트박스 되감기)**: FPS용으로 과거 N 프레임의 `WorldState` 스냅샷을 유지하고, 피격 판정 시 클라이언트의 지연만큼 과거 상태를 참조
- **Entity Interpolation 가이드**: 클라이언트가 수신한 스냅샷 사이를 보간하여 부드럽게 렌더링하도록 SDK 문서화
- **RTT 측정 및 세션 품질 추적**: `ClientSession`에 Rolling Average RTT를 기록하고 Lag Compensation에 활용

### 완료 기준

- RTT 150ms 환경에서 FPS 피격 판정 오차가 허용 범위(±1 프레임) 이내
- CSP 적용 시 클라이언트 이동 예측과 서버 보정 간 고무줄 현상 최소화

---

## Milestone 9 — Zone & World Management

> **공간 분할 및 월드 관리**

MMORPG의 오픈 월드, FPS의 맵 인스턴스 등 게임 공간을 서버에서 효율적으로 관리하는 구조.
현재 `P2PGroup`은 논리 그룹 수준이며, 게임 공간 개념(Zone, Room, Channel)과 분리 필요.

### 작업 항목

- **Zone / Room / Channel 계층 설계**: `GameWorld` > `Zone` > `Room` 3단계 계층 모델 정의 및 `ServerMemory` 연동
- **공간 기반 관심 영역(AoI, Area of Interest)**: 플레이어 주변 일정 반경의 엔티티만 업데이트를 수신하도록 필터링 (쿼드트리 / 그리드 셀 방식)
- **동적 인스턴스 생성·소멸**: 던전, 매치 룸 등 요청 시 Zone 인스턴스를 생성하고 종료 시 자원 반환
- **서버 간 Zone 이동 (Zone Transfer)**: 플레이어가 Zone 경계를 넘을 때 세션 핸드오프 없이 자연스럽게 이동하는 프로토콜
- **NPC / 몬스터 엔티티 관리**: 서버 관할 AI 엔티티를 Tick Loop에 통합하여 스폰·패스파인딩·상태 갱신 처리

### 완료 기준

- AoI 필터링으로 동일 Zone 내 클라이언트 수신 패킷 수가 전체 브로드캐스트 대비 70% 이상 감소
- Zone 인스턴스 생성·소멸 사이클이 메모리 누수 없이 동작

---

## Milestone 10 — Game Security & Anti-Cheat

> **게임 특화 보안 및 치트 방지**

일반 보안(JWT, 암호화)을 넘어, 게임에 특화된 치트 탐지와 서버 권위 검증이 필요.

### 작업 항목

- **서버 사이드 이동 검증**: 클라이언트가 보고한 위치를 이전 위치, 최대 이동 속도, 경과 Tick 수와 비교하여 텔레포트·속도 핵 탐지
- **입력 빈도 검증**: 클라이언트 입력 패킷이 물리적으로 불가능한 빈도로 수신될 경우 플래그 처리
- **패킷 위변조 탐지**: Sequence Number + HMAC 서명으로 패킷 무결성 검증, 변조 패킷 즉시 세션 차단
- **행동 이상 탐지 로그**: 비정상적인 킬/딜 수치, 이동 패턴 등을 `ReportEvent`로 기록하여 사후 분석 지원
- **Rate Limiting + 세션 제재**: 반복 위반 세션에 대해 경고 → 임시 차단 → 영구 차단의 자동 제재 파이프라인

### 완료 기준

- 속도 핵(이동 속도 2배) 시나리오에서 평균 3 Tick 이내 탐지 및 차단
- 패킷 위변조 시 세션 즉시 종료 및 이벤트 로그 기록

---

## Milestone 11 — Game Session & Matchmaking

> **매치메이킹 및 게임 세션 관리**

플레이어가 게임에 참여하는 전체 흐름(로비 → 매치 → 게임 → 결과)을 서버에서 관리하는 구조.

### 작업 항목

- **매치메이킹 큐 시스템**: MMR / 레이팅 기반으로 플레이어를 자동으로 그룹화하는 `MatchQueue` 구현
- **게임 세션 생명주기 관리**: `Lobby` → `MatchFound` → `Loading` → `InGame` → `Result` → `Disbanded` 상태 머신 설계
- **팀 구성 및 밸런싱**: 팀 인원, 레이팅 밸런스를 고려한 자동 팀 배정 로직
- **관전자(Spectator) 모드 지원**: 게임 세션에 Read-only로 참여하는 관전 클라이언트 처리
- **게임 결과 처리 및 보상 훅**: 세션 종료 시 `OnGameEnd` 이벤트에서 DB에 결과 기록 및 보상 지급을 Logic Plugin으로 위임

### 완료 기준

- 매치메이킹 큐에서 8명이 정상적으로 매칭되어 게임 세션이 생성되고 전체 생명주기가 완주됨
- 세션 도중 플레이어 이탈(Grace 재연결) 및 복귀가 게임 상태 보존 상태로 성공

---

## Milestone 12 — Live Operations & Scalability

> **무중단 운영 및 서버 수평 확장**

라이브 서비스 게임은 패치 배포, 점검, 급격한 동접 증가 등 운영 이벤트를 무중단으로 처리해야 함.

### 작업 항목

- **게임 서버 클러스터링**: Zone / Room 단위로 부하를 여러 서버 인스턴스에 분산, Master 노드가 라우팅 관리
- **`ISessionManager` Redis 백엔드 교체**: Grace 재연결이 다른 서버 인스턴스에서도 성공하도록 세션 공유
- **무중단 배포(Rolling Update)**: 게임 세션이 없는 서버부터 순차적으로 재시작하는 배포 파이프라인
- **동접 급증 대응 (Auto Scaling)**: 동접 수 기반으로 게임 서버 인스턴스를 자동으로 증감하는 오케스트레이션 연동 (Kubernetes / PM2)
- **서버 상태 대시보드**: 클러스터 전체의 Zone 수, 동접 수, 패킷 처리량, 지연시간을 실시간으로 시각화하는 운영 콘솔

### 완료 기준

- 서버 1개 인스턴스 장애 시 해당 Zone 플레이어가 30초 이내 다른 인스턴스로 이전됨
- 동접 2배 급증 시 신규 인스턴스 자동 투입 및 부하 분산 성공

---

## 참고: 마일스톤 의존 관계

```
=== Phase 1: Engine Foundation ===
M1 (Stability)
  └─► M2 (Security)
        └─► M3 (P2P Maturity)
        └─► M5 (Clustering)
  └─► M4 (Plugin Ecosystem)
        └─► M5 (Clustering)
M5 (Clustering)
  └─► M6 (Developer Experience)

=== Phase 2: Game Server ===
M6 (Developer Experience)
  └─► M7 (Tick & State Sync)
        └─► M8 (Lag Compensation)
              └─► M10 (Anti-Cheat)
        └─► M9 (Zone & World)
              └─► M11 (Matchmaking)
M10 + M11
  └─► M12 (Live Operations)
```

## 게임 장르별 우선 적용 마일스톤

| 장르 | 최우선 마일스톤 | 차순위 |
|---|---|---|
| **FPS** | M7 → M8 (Lag Compensation 핵심) | M10 (Anti-Cheat) |
| **MMORPG** | M7 → M9 (Zone/World 핵심) | M11 (Matchmaking) |
| **배틀로얄** | M7 → M9 → M11 | M8, M10 |
| **턴제 / 카드** | M11 (세션 관리 핵심) | M12 |

---

*최종 수정: 2026-05-09*
*대상 프로젝트: TeruTeruServer v1.x — Engine Foundation + Game Server Edition*
