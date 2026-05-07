# 🚫 [작업 반려 및 보완 지시서] Phase 2: 아키텍처 현대화 2차 조치 요구

**수신:** 개발 1팀 (Gemini CLI)
**발신:** 프로젝트 디렉터
**날짜:** 2026년 4월 1일
**참조:** `Phase 2` 작업물 1차 코드 리뷰 및 빌드 검증

---

## 1. 📢 반려 사유 (Rejection Background)
제출된 Phase 2 (미들웨어 및 DI 도입)의 결과물을 디렉터가 1차 코드 리뷰한 결과, 전체적인 아키텍처 뼈대(Structure)의 전환은 훌륭하게 완수되었습니다. 

그러나 가장 중요한 **보안 토큰 인증의 누락**과 **DI 컨테이너의 잘못된 패턴(순환 참조 우회)**이 발견되어 상용 수준(Production-ready)의 병합 기준에 아직 미달합니다. 따라서 해당 제출물을 임시 보류(반려)하며, 본 2차 지시서를 토대로 **나머지 미비점(TODO)을 완전히 구현**한 후 재보고 바랍니다.

---

## 2. 🚨 필수 보완 요구 사항 (Remediation Tasks)

### [Task 1] `AuthMiddleware` 토큰 인증 메커니즘 실제 탑재
**문제점:** `AuthMiddleware.cs` 32번째 줄에 `// TODO: 실제 토큰을 포함시키고 이를 검증해야 함` 주석만 남긴 채, 모든 패킷이 단순히 통과(Bypass)하도록 구현되어 실질적인 인증 방어막이 존재하지 않습니다.
**조치 요구:** 
1. `JwtSecurityTokenHandler` 등 표준화된 라이브러리를 이용하여 패킷 내부에 포함된 토큰 값을 추출하고 검증(Decode & Validate)하는 진짜 미들웨어 로직을 완성하십시오.
2. 유효하지 않은 패킷은 다음 미들웨어 레이어(`next()`)로 넘기지 않고 즉각 `Exception` 처리나 강제 연결 해제 로직을 태워 방어하십시오.

### [Task 2] 데이터베이스 계층 의존성 주입(DI) 완성
**문제점:** `Program.cs` 53번째 줄에 `DatabaseHelper`의 의존성 등록 로직이 주석 처리(`// services.AddSingleton<IDatabaseService...`) 되어 있습니다. 
**조치 요구:** `DatabaseConnector` 또는 관련 계층에 `IDatabaseService` 인터페이스 추출을 완료하고, 주석을 해제하여 완벽하게 DI 컨테이너를 거쳐 비즈니스 로직에 주입되도록 완성하십시오.

### [Task 3] `MainServer` - `ServerLogic` 간 결합도(순환 참조) 해결
**문제점:** `Program.cs` 40번째 줄 인근, 수동으로 `serverLogic.SetMainServer(mainServer)`를 호출하여 의존성을 우회 주입하는 안티 패턴(Hack)이 사용되었습니다. 이는 구조적 결합을 뜻합니다.
**조치 요구:** 구조를 다음과 같이 재설계하십시오. `ServerLogic`이 `MainServer` 객체 전체를 아는 대신, 메시지 전송 규약인 `IMessageSender`(또는 Action/Event 등) 인터페이스를 `MainServer`가 상속받고, 로직층은 오직 인터페이스(`IMessageSender`)를 주입받게 만듦으로써 악의적인 꼬리치기(Circular Dependency)를 근본적으로 차단하십시오.

---

## 3. 📅 조치 보고 절차
본 반려 문서 하달 즉시, 개발팀은 할당된 `feature/phase2-architecture` 브랜치에 리뷰 코멘트들을 패치(Hotfix) 커밋한 뒤, 다시 디렉터에게 리뷰를 요청해 주십시오. 

이것이 완료되기 전엔 결단코 Phase 3 진입을 불허합니다.
