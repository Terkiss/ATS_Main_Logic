# Milestone 4: Plugin Ecosystem & Hot-reload — 작업자 지시문

## 목표

Documents/구현계획.md에 명시된 Milestone 4 (Plugin Ecosystem & Hot-reload)의 5개 작업 항목을 구현한다. 마일스톤 범위를 벗어난 확장은 하지 않는다.

## 선행 조건 확인

Milestone 2+3이 커밋 완료되었다. 현재 브랜치: feature/phase2-architecture, HEAD: 1d2a547

구현 시작 전 반드시 아래 명령으로 현재 상태를 확인하라.

```bash
git status --short --branch
git log --oneline -3
```

## 기존 코드 파악 (필독)

작업 전 반드시 아래 파일들의 현재 구조를 읽고 이해하라.

### 핵심 파일

- **TeruTeruServer.Runtime/PluginManager.cs** (107줄)
  - LogicProxy (L12-40): lock 기반 위임. 예외 방어 없음.
  - PluginManager (L42-106): FileSystemWatcher 기반 DLL 감시.
  - ★ 알려진 결함:
    1. ReloadPlugins()(L71)에서 AssemblyLoadContext를 매번 새로 생성하지만 이전 컨텍스트를 Unload()하지 않아 메모리 누수 발생.
    2. FileSystemWatcher의 Changed 이벤트가 파일 하나에 2-3회 중복 발생 → debounce 없이 ReloadPlugins()가 연타됨.
    3. 단일 DLL만 로드 (L76: FirstOrDefault()). 다중 플러그인 로드 불가.
    4. LogicProxy에서 예외 미격리 — 플러그인 예외가 서버 루프 전파 가능.

- **TeruTeruServer.SDK/Interfaces/ILogicService.cs** (15줄)
  - ProcessDirectProtocol(), ProcessJsonProtocol() 두 메서드만 정의.

- **TeruTeruServer.Runtime/Rpc/ProtocolRouter.cs** (137줄)
  - Initialize()(L27-53): 리플렉션으로 [Rpc], [Protocol] 어트리뷰트를 스캔하여 딕셔너리에 등록.
  - 이 스캔 결과를 외부에서 조회할 수 없음.

- **TeruTeruServer.SDK/Attributes/** 하위:
  - ProtocolAttribute.cs (Protocol enum 매핑)
  - RpcAttribute.cs (문자열 이름 기반 매핑)
  - RequiresAuthAttribute.cs (인증 필수 마킹)

- **TeruTeruServer.SDK/TeruTeruServer.SDK.csproj** (22줄)
  - TargetFramework: net8.0
  - NuGet 메타데이터(PackageId, Version, Authors 등) 없음.
  - XML 문서 주석 생성 설정 없음.

## 작업 항목 (5건)

### 작업 1: 무중단 플러그인 교체 (Hot-reload 완성)

**파일 범위:**
- [MODIFY] TeruTeruServer.Runtime/PluginManager.cs

**구현 내용:**
1. PluginManager에 이전 AssemblyLoadContext를 필드로 보관하라. ReloadPlugins() 진입 시 이전 컨텍스트가 존재하면 Unload()를 호출하라. Unload 후 WeakReference로 실제 해제 여부를 확인하는 헬퍼를 추가하라 (GC 강제 호출은 하되, 대기는 최대 5초로 제한).
2. FileSystemWatcher 이벤트에 debounce를 추가하라. 마지막 이벤트 수신 후 500ms 동안 추가 이벤트가 없을 때만 ReloadPlugins()를 호출하도록 System.Timers.Timer 기반 debounce를 구현하라.
3. ReloadPlugins()에서 다중 DLL 로드를 지원하라. plugins 폴더 내 모든 *.dll을 순회하며 ILogicService 구현체를 찾되, 첫 번째로 발견한 구현체를 활성 로직으로 사용하라.

**주의:**
- AssemblyLoadContext 생성 시 isCollectible: true는 이미 설정되어 있음 (L82).
- context를 using으로 감싸지 말 것 — Unload()는 비동기적이며 Dispose와 다르다.

---

### 작업 2: 플러그인 간 의존성 관리

**파일 범위:**
- [NEW] TeruTeruServer.SDK/Interfaces/IPluginDependency.cs
- [MODIFY] TeruTeruServer.Runtime/PluginManager.cs

**구현 내용:**
1. IPluginDependency 인터페이스를 SDK에 신설하라:
   ```csharp
   public interface IPluginDependency
   {
       string PluginName { get; }
       string[] DependsOn { get; }
   }
   ```
2. PluginManager.ReloadPlugins()에서 모든 DLL을 스캔하여 IPluginDependency를 구현한 타입을 수집하라. DependsOn 배열을 기반으로 위상 정렬(topological sort)하여 로드 순서를 결정하라. 순환 참조가 발견되면 해당 플러그인 그룹을 건너뛰고 경고 로그를 출력하라.
3. ILogicService를 구현하지만 IPluginDependency를 구현하지 않는 플러그인은 의존성 없음으로 처리하라 (기존 동작 호환성 보존).

---

### 작업 3: 어트리뷰트 자동 문서화

**파일 범위:**
- [NEW] TeruTeruServer.Runtime/Rpc/PluginMetadata.cs
- [MODIFY] TeruTeruServer.Runtime/Rpc/ProtocolRouter.cs

**구현 내용:**
1. PluginMetadata 클래스를 신설하라:
   ```csharp
   public class ProtocolEndpointInfo
   {
       public string MethodName { get; set; }
       public string ProtocolOrRpcName { get; set; }
       public string BindingType { get; set; }  // "Rpc" 또는 "Protocol"
       public bool RequiresAuth { get; set; }
   }
   ```
2. ProtocolRouter.Initialize()에서 스캔 결과를 `List<ProtocolEndpointInfo>`에 수집하라. 각 메서드의 [Rpc], [Protocol], [RequiresAuth] 어트리뷰트 존재 여부를 ProtocolEndpointInfo로 변환하여 저장하라.
3. ProtocolRouter에 `public IReadOnlyList<ProtocolEndpointInfo> GetRegisteredEndpoints()` 메서드를 추가하여 외부에서 조회 가능하게 하라.
4. 플러그인 로드 완료 시 TeruTeruLogger로 등록된 엔드포인트 목록을 출력하라.

---

### 작업 4: 플러그인 샌드박스 (오류 격리)

**파일 범위:**
- [MODIFY] TeruTeruServer.Runtime/PluginManager.cs (LogicProxy 부분)

**구현 내용:**
1. LogicProxy.ProcessDirectProtocol()과 ProcessJsonProtocol()의 위임 호출을 try-catch로 감싸라. 예외 발생 시 TeruTeruLogger.LogError()로 기록하되 예외를 상위로 전파하지 않는다.
2. LogicProxy에 연속 예외 카운터를 추가하라:
   - `int _consecutiveErrors` (기본값 0)
   - `bool _isDisabled` (기본값 false)
   - `const int MAX_CONSECUTIVE_ERRORS = 10`
3. 예외 발생 시 `_consecutiveErrors++`. 성공 시 0으로 리셋. `MAX_CONSECUTIVE_ERRORS` 도달 시 `_isDisabled = true`로 설정하고 "Plugin disabled due to consecutive errors" 경고를 출력하라. `_isDisabled` 상태에서는 위임 호출을 건너뛰고 즉시 리턴하라.
4. UpdateLogic() 호출 시(새 플러그인 로드 시) `_consecutiveErrors`와 `_isDisabled`를 0/false로 리셋하라.

**주의:**
- LogicProxy의 lock(_lock) 패턴은 유지하라. 예외 격리 로직(try-catch)은 lock 내부에 추가하라.

---

### 작업 5: SDK NuGet 패키징

**파일 범위:**
- [MODIFY] TeruTeruServer.SDK/TeruTeruServer.SDK.csproj

**구현 내용:**
1. PropertyGroup에 아래 NuGet 메타데이터를 추가하라:
   ```xml
   <PackageId>TeruTeruServer.SDK</PackageId>
   <Version>1.0.0</Version>
   <Authors>Terkiss</Authors>
   <Description>TeruTeruServer Plugin Development SDK</Description>
   <GenerateDocumentationFile>true</GenerateDocumentationFile>
   <NoWarn>$(NoWarn);CS1591</NoWarn>
   ```
2. `dotnet pack TeruTeruServer.SDK/TeruTeruServer.SDK.csproj -c Release`를 실행하여 .nupkg 파일이 정상 생성되는지 확인하라. 실제 NuGet 배포(push)는 하지 않는다.

**주의:**
- SDK의 TargetFramework는 net8.0이다. NuGet 메타데이터 추가 시 TargetFramework를 변경하지 말 것.

## 변경 허용 범위

**허용:**
- TeruTeruServer.Runtime/PluginManager.cs 수정
- TeruTeruServer.Runtime/Rpc/ProtocolRouter.cs 수정
- TeruTeruServer.Runtime/Rpc/ 하위 신규 파일 생성
- TeruTeruServer.SDK/Interfaces/ 하위 신규 파일 생성
- TeruTeruServer.SDK/TeruTeruServer.SDK.csproj 수정 (NuGet 메타만)
- 테스트 파일 추가 또는 수정
- IMPLEMENTATION_PROGRESS.md 갱신

**금지:**
- .agents/ 수정 금지
- 커밋/푸시 금지
- release gate 기준(scripts/verify-release.sh) 변경 금지
- 기존 M2/M3 보안·P2P 로직 변경 금지
- 기존 통과 중인 테스트 삭제 또는 약화 금지
- NuGet 패키지 실제 배포(push) 금지

## 검증

1. `./scripts/verify-release.sh` 통과 (오류 0개 필수)
2. `dotnet pack TeruTeruServer.SDK/TeruTeruServer.SDK.csproj -c Release` 성공
3. 신규 파일이 untracked로 방치되지 않도록 git add (파일 단위 명시만 허용)
4. IMPLEMENTATION_PROGRESS.md가 실제 구현 상태와 일치하는지 확인
5. git status로 변경/신규 파일 목록 보고

## 최종 보고 형식

1. 전체 완료 여부
2. 이번 완료 범위 (5개 작업 항목별)
3. 변경 파일 목록 (git status 기준)
4. 새 파일 분류 (git add 완료 여부 포함)
5. 핵심 구현 요약 (작업별 1~2줄)
6. 공식 release gate 결과 (verify-release.sh 출력)
7. dotnet pack 결과
8. 남은 리스크
9. 커밋/푸시 여부: 수행하지 않음

## 특별 주의사항

★ PluginManager.ReloadPlugins() L82-83에서 현재 `var context = new AssemblyLoadContext("LogicContext", isCollectible: true);` 이 코드는 매 호출마다 같은 이름으로 새 컨텍스트를 만든다. 이전 컨텍스트 참조를 필드에 저장하지 않으므로 Unload가 불가능하다. 이전 컨텍스트를 `_previousContext` 같은 필드로 보관한 뒤 Unload()를 호출해야 한다.

★ LogicProxy의 `lock(_lock)` 패턴은 유지하라. 예외 격리 로직(try-catch)은 lock 내부에 추가하라.

★ SDK의 TargetFramework는 net8.0이다. NuGet 메타데이터 추가 시 TargetFramework를 변경하지 말 것.
