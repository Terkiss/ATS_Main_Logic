# 프로젝트 구현 진행 상황 (Implementation Progress)

## 현재 마일스톤: Milestone 5 — Scalability & Clustering

- [x] **1. ISessionStore 백엔드 추상화**
  - [x] `ISessionStore` 인터페이스 신설
  - [x] `InMemorySessionStore` 기본 구현
  - [x] `ISessionManager` → `ISessionStore` 주입 리팩터링

- [x] **2. 서버 간 Grace 재연결 지원**
  - [x] `ISessionStore`에 ReconnectToken 검색 추가
  - [x] `AuthMiddleware.HandleReconnect()` 분산 조회 로직

- [x] **3. 분산 이벤트 버스 인터페이스**
  - [x] `IEventBus` 인터페이스 (Publish/Subscribe)
  - [x] `LocalEventBus` 기본 구현
  - [x] `P2PGroupHandler` 이벤트 버스 연동

- [x] **4. 클러스터 노드 레지스트리**
  - [x] `IClusterRegistry` 인터페이스
  - [x] `LocalClusterRegistry` 기본 구현
  - [x] 노드 정보 모델 (ClusterNodeInfo)

- [x] **5. Stateless 설계 가이드 문서화**
  - [x] `Documents/Technical/Stateless_Design_Guide.md` 작성

## 남은 리스크 및 이슈
- 실제 Redis/RabbitMQ 연동은 M5 범위 밖이며, 인터페이스와 로컬 구현만 제공합니다.
- `ServerMemory.cs`의 static 사용을 완전히 제거하는 것은 대규모 리팩터링이므로, M5에서는 새로운 경로(ISessionStore)를 제공하고 기존 경로와 공존시킵니다.
