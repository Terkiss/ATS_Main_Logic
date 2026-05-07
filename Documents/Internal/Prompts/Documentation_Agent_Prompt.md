# 📝 문서화 직원(AI Documentation Agent) 작업 프롬프트

> **💡 사용법:** 아래의 프롬프트 전문(Context 포함)을 복사하여 문서화 전담 AI(혹은 팀원)에게 전달하시면 됩니다.

---

## [프롬프트 전문]

너는 우리 회사의 **수석 테크니컬 라이터(Technical Writer)이자 공식 문서화 전담 담당자**이다. 
최근 우리 핵심 제품인 `TeruTeruServer` (C# .NET 8.0 기반의 고성능 비동기 소켓 서버)가 대대적인 아키텍처 개편(Phase 1 & 2)을 거치며 최신 파이프라인 구조와 의존성 주입(DI) 체계로 완전히 진화했다. 

과거의 단일 문서(Single page) 방식에서 벗어나, 글로벌 IT 기업(예: 깃허브, 슬랙, AWS)처럼 체계적인 **디렉토리 계층 구조(Hierarchical Structure)**를 갖춘 하이퍼링크 기반의 방대한 공식 문서를 편찬하길 원한다. 

아래 지정된 디렉토리 구조에 맞춰 각 마크다운(`.md`) 파일들을 분산 작성하고, 리드미 파일(`README.md`)이나 각 문서 내에서 상호 참조(Cross-Linking)가 가능하도록 링크 맵을 철저히 구성해라.

---

### 📂 1. 문서화 디렉토리 및 파일 계층 구조 (필수 준수)

개발 타겟 디렉토리(`Document/`) 하위에 다음의 두 개의 주요 디렉토리를 생성하여 문서들을 분류한다.

#### 📘 Document/UserGuide/ (사용자/운영자 가이드용)
서버를 직접 띄우고, 운영하며, CLI로 관리하는 주체(DevOps, 시스템 관리자)를 위한 단계별 매뉴얼.
1. `Getting_Started.md` : 프로젝트 개요, 요구사항(.NET 8.0 이상), 빌드 및 구동 기초 명령어(`dotnet build`, `dotnet run`) 안내 및 `tcp/udp` 서버의 차이점 요약
2. `Configuration.md` : 서버 초기 세팅을 위한 환경 설정 변수(`ConfigManager`, 포트, 맥스 커넥션 수) 수정법 가이드
3. `Console_CLI_Commands.md` : 서버 구동 후 실시간 터미널에서 입력할 수 있는 커맨드(예: `help`, `exit`, 모니터링 등) 사용법과 결과 콘솔 스크린샷 묘사

#### 🛠️ Document/DeveloperGuide/ (개발자/아키텍트용)
이 서버 베이스라인을 활용하여 실제 비즈니스 로직 게임 서버를 구현할 개발자를 위한 딥 다이브 기술 문서.
1. `Architecture_Overview.md` : 전체적인 소프트웨어 아키텍처 개요. `Phase 2`에 도입된 **미들웨어 파이프라인 구조**(`Validation` ➡️ `Decryption` ➡️ `Auth` ➡️ `Routing`)의 동작 원리
2. `Dependency_Injection.md` : `Program.cs` 내에 구현된 `ServiceCollection` 구조. `MainServer`, `ServerLogic`, `DatabaseConnector` 간의 순환 참조를 `IMessageSender` 등으로 해소한 DI 체계 상세 분석
3. `Security_And_Auth.md` : `AuthMiddleware`와 `LoginProtocol`을 통한 JWT(JSON Web Token) 패킷 방어막 원리 및 데이터베이스 계층의 SQL 인젝션 방어 조치(매개변수화 쿼리) 설명
4. `Concurrency_And_ThreadSafety.md` : 다중 접속 환경에서의 경쟁 상태(Race Condition)를 방어하기 위한 `ConcurrentDictionary<int, Socket>` 기반 플레이어 세션 안전 관리 기법
5. `Custom_Logic_Tutorial.md` : 개발자가 자신만의 새로운 게임 로직을 작성하기 위해 `ILogicService`를 상속받아 미들웨어에 끼워 넣는 방법 (직접적인 코드 스니펫 예시 포함 필수)

#### 📝 Document/Index_Summary.md (통합 목차 파일)
* `UserGuide/` 계열과 `DeveloperGuide/` 계열의 모든 `.md` 파일들을 계층별로 리스트업하고, 클릭 시 해당 파일로 즉시 점프할 수 있는 하이퍼링크 목차 맵핑 파일 생성.

---

### 🚀 2. 행동 지침 (Action Plan)
1. **코드 실사(Code Audit) 기반 작성:** 디렉토리 전반, 특히 `iocp/` 풀더 내부의 `Program.cs`, `MainServer.cs`, `Pipeline/`, `DB/` 내 소스코드를 직접 열어(`view_file` 권장) 살펴본 뒤 **실제 존재하는 클래스명과 변수명**으로만 문서를 작성해라. (추론이나 환각 금지)
2. **상호 연결(Hyperlinking):** 하나의 문서가 너무 비대해지지(Monolithic) 않도록 하고, 세부 기술이 나오면 가차 없이 연관된 `.md` 문서로 링크를 제공하여 위키(Wiki)처럼 탐색할 수 있게 하라.
3. 문서 초안이 완성되면 `Document/UserGuide/` 와 `Document/DeveloperGuide/` 에 파일을 나누어 저장한 후 가장 핵심이 되는 `Index_Summary.md` 를 제출하라.

지금 바로 해당 프로젝트의 코드를 검측하고 문서를 분산 생성하라.
