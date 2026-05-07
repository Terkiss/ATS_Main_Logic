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

## 참고: 마일스톤 의존 관계

```
M1 (Stability)
  └─► M2 (Security)
        └─► M3 (P2P Maturity)
        └─► M5 (Clustering)
M1 (Stability)
  └─► M4 (Plugin Ecosystem)
        └─► M5 (Clustering)
M5 (Clustering)
  └─► M6 (Developer Experience)
```

---

*최종 수정: 2026-05-08*
*대상 프로젝트: TeruTeruServer v1.x*
