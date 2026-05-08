# TeruTeruServer Migration Guide

이 문서는 시스템 업그레이드 또는 설정 변경 시 필요한 마이그레이션 절차와 호환성 정책을 안내합니다.

## 1. ProtocolSelect 관리 정책

`ProtocolSelect` Enum은 시스템의 통신 규약을 정의하는 핵심 요소입니다.

- **예약 번호**: 0~1000번은 시스템 코어용으로 예약되어 있습니다. 플러그인 개발자는 1001번 이후를 사용하십시오.
- **삭제 지침**: 한 번 공개된 프로토콜 번호는 가급적 삭제하지 마십시오. 기능 제거 시 `[Obsolete]` 어트리뷰트를 붙여 관리합니다.

## 2. SDK 버전 호환 매트릭스

| SDK Version | Server Version | Compatibility | Note |
| :--- | :--- | :--- | :--- |
| 1.0.x | 2.0.x | Full | Phase 3 Plugin Architecture 기반 |
| 0.9.x | 2.0.x | Breaking | 헤더 크기 변경 (2 -> 6바이트) |

## 3. Breaking Change 정책

- **Major 변경**: 헤더 포맷, 암호화 방식, 메인 인터페이스(`ILogicService`) 시그니처 변경 시 발생합니다.
- **공지 절차**: 변경 최소 2주 전 `Documents/Technical/Breaking_Changes.md`에 공지됩니다.
- **지원 기간**: 이전 버전 SDK는 최소 1개 마이너 버전 동안 하위 호환성을 유지하려고 노력합니다.

## 4. 플러그인 마이그레이션 (ILogicService)

Milestone 4 이후 플러그인 아키텍처가 도입됨에 따라, 기존의 하드코딩된 로직은 `ILogicService`를 구현하는 클래스로 옮겨야 합니다.

1. `ILogicService` 구현 클래스 생성.
2. 기존 `switch-case` 핸들러를 메서드로 분리하고 `[Rpc]` 또는 `[Protocol]` 어트리뷰트 부착.
3. 생성자를 통해 필요한 의존성(`ISessionManager`, `IDatabaseService` 등)을 주입받도록 수정.

## 5. 세션 저장소 마이그레이션 (Redis 전환)

단일 서버 인스턴스에서 클러스터 환경으로 확장할 경우:

1. `Program.cs`에서 `InMemorySessionStore`를 `RedisSessionStore`(개발 예정)로 교체합니다.
2. 모든 세션 데이터 모델이 `[Serializable]` 하거나 JSON 직렬화 가능해야 합니다.
3. `IEventBus` 구현체를 `RabbitMQ` 또는 `Redis Pub/Sub`으로 변경하여 노드 간 이벤트를 동기화합니다.

## 6. 결론

본 서버는 Phase 3 아키텍처를 기점으로 강력한 추상화를 제공하므로, 가이드라인을 준수할 경우 향후 대규모 아키텍처 변경에도 최소한의 수정으로 마이그레이션이 가능합니다.
