# Stateless 설계 가이드 (Stateless Design Guide)

이 문서는 TeruTeruServer의 플러그인을 클러스터링 및 분산 환경에 적합하도록 설계하기 위한 가이드라인을 제공합니다.

## 1. 세션 외부화 원칙 (Externalize Session State)

모든 클라이언트 세션 데이터는 개별 서버 인스턴스의 로컬 메모리가 아닌, `ISessionStore`를 통해 관리되어야 합니다.

- **이유**: 사용자가 서버 A에 접속했다가 재연결 시 서버 B로 접속하더라도 동일한 세션 정보를 유지하기 위함입니다.
- **실천**: `ClientSession` 객체에 새로운 상태를 추가하거나 수정할 때, 해당 객체가 `ISessionStore`를 통해 공유되는지 확인하십시오.

## 2. ReconnectToken 기반 분산 재연결

사용자가 네트워크 불안정으로 인해 일시적으로 연결이 끊겼을 때, `ReconnectToken`을 사용하여 원래 세션을 복구할 수 있습니다.

```csharp
// AuthMiddleware의 내부 로직 예시
if (!_sessionManager.Players.TryGetValue(request.HostID, out session))
{
    // 로컬 메모리에 없으면 공유 저장소에서 토큰으로 검색
    session = _sessionStore.FindByReconnectToken(request.ReconnectToken);
}
```

- **주의**: L4/L7 로드밸런서 사용 시 스티키 세션(Sticky Session) 설정을 피하고, 모든 노드가 동일한 토큰으로 세션을 찾을 수 있도록 설계하십시오.

## 3. IEventBus를 통한 이벤트 전파

한 서버에서 발생한 이벤트(예: P2P 그룹 가입)를 다른 서버에 있는 관련 멤버들에게 알리려면 `IEventBus`를 사용하십시오.

```csharp
// 그룹 가입 시 이벤트 발행
_eventBus.Publish("p2p.group.join", new { GroupId = 123, HostId = 456 });
```

- **구현**: 로컬 환경에서는 `LocalEventBus`가 동작하지만, 분산 환경에서는 Redis Pub/Sub 또는 RabbitMQ 기반의 구현체로 교체됩니다.

## 4. 클러스터 노드 레지스트리 활용

클러스터 내의 활성 노드 목록을 조회하여 부하 분산이나 시그널링 대상 서버를 결정할 수 있습니다.

- `IClusterRegistry.GetActiveNodes()`를 통해 현재 서비스 가능한 서버 목록을 획득하십시오.

## 5. 결론 및 교체 포인트

Milestone 5에서 제공하는 기본 구현체(`InMemorySessionStore`, `LocalEventBus` 등)는 단일 프로세스에서 동작하지만, 프로덕션 환경에서는 아래와 같이 교체될 것을 염두에 두어야 합니다:

| 추상화 인터페이스 | 프로덕션 교체 대상 (예시) |
| :--- | :--- |
| `ISessionStore` | Redis, MongoDB |
| `IEventBus` | Redis Pub/Sub, RabbitMQ, Kafka |
| `IClusterRegistry` | Consul, Etcd, ZooKeeper |

이 가이드를 준수함으로써, 추가적인 코드 수정 없이 설정 변경만으로 수평적 확장(Scale-out)이 가능한 시스템을 유지할 수 있습니다.
