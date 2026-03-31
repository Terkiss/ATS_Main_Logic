# 📋 [작업 지시서] Phase 2: 아키텍처 현대화 (Architecture Modernization)

**수신:** 개발 1팀 (Gemini CLI)
**발신:** 프로젝트 디렉터
**날짜:** 2026년 4월 1일
**참조:** `TeruTeruServer` 기술 진단 결과 (Phase 2 계획안)

---

## 1. 📢 지시 배경
Phase 1에서 식별된 치명적 보안 및 스레드 결함이 해소되었으므로, 본 서버의 유지보수성, 확장성, 그리고 상용 서비스 수준의 아키텍처(Production-Ready Architecture)를 확보하기 위해 **Phase 2 (아키텍처 현대화)** 작업에 돌입합니다. 

본 작업은 현재 `MainServer.cs` 에 집중된 과도한 책임(God Object Anti-Pattern)을 분리하고, 강하게 결합된 모듈들을 유연한 구조로 재설계하는 것을 목표로 합니다.

---

## 2. 🚀 필수 구현 요구 사항 (Core Requirements)

### [Action 1] 미들웨어 네트워크 파이프라인 도입 (Middleware Pipeline)
현재 패킷 수신 시 단일 함수 내에서 모든 분기 처리가 이루어지고 있습니다. 
ASP.NET Core의 미들웨어 패턴이나 TCP 파이프라인 패턴을 차용하여 다음의 흐름을 강제하도록 재설계하십시오.
- **Pipeline Flow:** `[네트워크 패킷 수신] ➡️ [암복호화(Decryption) 단계] ➡️ [유효성 검증(Validation) 단계] ➡️ [인증 확인(Auth) 단계] ➡️ [비즈니스 로직(ServerLogic) 라우팅]`
- 각 단계는 독립적인 클래스(혹은 인터페이스)로 구현되어 언제든 순서를 바꾸거나 새로운 중간 검사 단계를 끼워 넣을 수 있어야 합니다.

### [Action 2] 토큰 기반 인증 시스템 및 로그인 규약 완성 (Authentication System)
현재 연결 시 GUID 발급 외에 실질적인 인증 방어막이 없습니다.
1. 사용자 인증을 위한 JWT(JSON Web Token) 또는 고유한 보안 세션 토큰 메커니즘을 설계 및 적용하십시오.
2. 현재 `ServerLogic.cs` 내에 기능이 비어있는(Placeholder) `LoginProtocol` 을 실제 토큰 발급 및 검증 로직으로 완전히 구현하십시오.

### [Action 3] 의존성 주입(DI, Dependency Injection) 적용
클래스 간의 직접 참조(강한 결합)로 인해 단위 테스트가 불가능하고 확장성이 저해되고 있습니다.
1. `.NET DI Container` (예: `Microsoft.Extensions.DependencyInjection`)를 도입하십시오.
2. `DatabaseConnector` 및 `ServerLogic` 등의 주요 모듈을 인터페이스(e.g., `IDatabaseService`, `ILogicRouter`)로 추출하십시오.
3. `MainServer` 가 구체적인 클래스가 아닌 인터페이스에 의존(Inversion of Control)하도록 생성자 주입(Constructor Injection) 방식을 전면 적용하십시오.

---

## 3. 🎯 기한 및 주의사항
- **진행 방식:** 위 3가지 Action을 순차적으로 진행하되, 각 Action이 완료될 때마다 컴파일 에러가 없는지, 기존 통신 로직이 깨지지 않았는지 테스트 코드를 작성하거나 수동 검증을 거쳐 보고할 것.
- **완료 보고:** Phase 2 아키텍처 초안 설계가 완료되면 UML 다이어그램이나 모듈 구조도를 텍스트로 요약하여 1차 리뷰를 디렉터에게 요청할 것.

> 본 문서를 수령한 개발 1팀은 오늘부터 즉각 브랜치를 분리(`feature/phase2-architecture`)하여 작업을 시작하십시오.
