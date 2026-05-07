# 📋 [긴급 지시] Phase 1: 보안 및 안정성 강화 작업 재조치 (Remediation Directive)

**수신:** 개발 1팀 (Gemini CLI)
**발신:** 프로젝트 디렉터
**날짜:** 2026년 4월 1일
**참조:** `TeruTeruServer` 기술 진단 결과 (Phase 1 검증 리포트)

---

## 1. 📢 지시 배경
Phase 1에서 지시한 **[보안 및 안정성 강화]** 작업 결과물에 대해 디렉터가 1차 기술 검증(Code Review & Build Test)을 수행한 결과, DB 쿼리 보안 측면에서는 요구사항이 훌륭하게 반영되었으나 **기본적인 빌드 무결성** 및 **스레드 안전성(동시성 제어)** 영역에서 심각한 누락과 휴먼 에러가 복합적으로 발견되었습니다. 

제품이 현재 **컴파일조차 되지 않는 상태**이므로, 우선순위를 가장 높여 아래의 결함들을 즉시 수정 후 재보고 바랍니다.

---

## 2. 🚨 조치 요구 사항 (Action Items)

### [Action 1] 컴파일 오류 수정 (Critical)
**대상 파일:** `iocp/DB/DatabaseConnector.cs`
**문제 증상:** `Task` 형식을 찾을 수 없다는 구문 에러 (CS0246) 발생
**수정 지시:**
1. 파일 최상단에 `using System.Threading.Tasks;` 네임스페이스 추가
2. 추가 후 `dotnet build ./iocp/TeruTeruServer.csproj` 를 수행하여 빌드가 100% 성공적으로 완료되는지 자체 검증 필수.

### [Action 2] 메인 서버 소켓 동시성 이슈 완전 해결 (High)
**대상 관련 파일:** `iocp/MainServer.cs`
**문제 증상:** 플레이어 세션을 저장하는 `players` 변수가 여전히 `Dictionary<int, Socket>` 으로 선언되어 있어 멀티스레드 및 비동기 환경에서 교착 상태 및 서버 크래시 유발 위험 존치. (현재 부분적으로 `TryRemove`와 같은 존재하지 않는 메서드를 혼용 중)
**수정 지시:**
1. **변수 타입 교체:** `public Dictionary<int, Socket> players;` 선언을 명시적으로 `public ConcurrentDictionary<int, Socket> players;` 로 수정할 것.
2. **초기화 구문 수정:** 생성자 및 `Initialize` 함수에서 `new Dictionary`로 할당된 부분을 `new ConcurrentDictionary`로 맞게 수정할 것.
3. **불필요한 Lock 제거:** `playerLock` 객체는 `ConcurrentDictionary` 체제에서는 무의미하므로 제거하거나, 만약 부득이하게 전체 반복문(`foreach`)을 돌 때 Snapshot(예: `.ToArray()`)을 사용하는 등 스레드 세이프티를 완전히 보장할 것.
4. **`players` 딕셔너리에 접근하는 모든 사용처 리팩토링:** `TryAdd`, `TryGetValue`, `TryRemove` 등 `Concurrent` 컬렉션의 메서드 시그니처 형식을 완벽히 준수할 것.

---

## 3. 📅 완료 기한 및 보고 절차
- **긴급도:** P0 (최상급)
- **보고 방식:** 해당 조치 완료 후, 코드 형상 관리에 커밋(Commit)하고 디렉터에게 "Phase 1 보완 패치 완료" 상태로 재보고할 것.
- 수정 중 발생하는 설계적인 예외 상황이 있다면 자의적으로 판단하지 말고 디렉터에게 즉시 논의 바랍니다.

> 본 문서의 수신 즉시 개발팀은 담당자를 배정하여 작업을 시작하십시오.
