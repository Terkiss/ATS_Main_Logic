# 프로젝트 구현 진행 상황 (Implementation Progress)

## 현재 마일스톤: Milestone 4 — Plugin Ecosystem & Hot-reload

- [x] **1. 무중단 플러그인 교체 (Hot-reload 완성)**
  - [x] 이전 `AssemblyLoadContext` 언로드 처리
  - [x] `WeakReference` 기반 언로드 확인
  - [x] `FileSystemWatcher` debounce 처리

- [x] **2. 플러그인 간 의존성 관리**
  - [x] `IPluginDependency` 인터페이스 도입
  - [x] 다중 DLL 로드 및 의존성 순서 자동 해결
  - [x] 순환 참조 감지

- [x] **3. 어트리뷰트 자동 문서화**
  - [x] 리플렉션 기반 `[Protocol]`, `[Rpc]`, `[RequiresAuth]` 어트리뷰트 스캔
  - [x] `PluginMetadata` 모델 구현 및 조회 API 노출

- [x] **4. 플러그인 샌드박스 (오류 격리)**
  - [x] `LogicProxy` 위임 호출 예외 방어
  - [x] 연속 예외 시 자동 비활성화 및 경고 로그
  - [x] 비활성화 상태 조회 인터페이스

- [x] **5. SDK NuGet 패키징**
  - [x] `.csproj` NuGet 메타데이터 추가
  - [x] `dotnet pack` 검증
  - [x] XML 문서 주석 자동 포함

## 남은 리스크 및 이슈
- `AssemblyLoadContext.Unload()` 후 실제 메모리 해제는 GC 타이밍에 의존하므로, 빈번한 핫리로드 시 메모리 누수 모니터링이 필요합니다.
- .NET 8 (SDK 타겟)에서 collectible AssemblyLoadContext의 제약사항을 사전 조사해야 합니다.
