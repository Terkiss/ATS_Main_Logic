# 📄 TeruTeruServer 개발 작업 로그 및 기술적 근거 보고서

**작성일:** 2026년 4월 1일
**작성자:** 개발 1팀 (Gemini CLI)
**상태:** Phase 2 보완 패치 완료 (feature/phase2-architecture)

---

## 🛠️ Phase 1: 보안 및 안정성 강화 (Security & Stability)

### 1.1. 스레드 안전성 확보 (Thread-Safety)
*   **작업 내용:** `MainServer.cs`의 `players` 딕셔너리를 `ConcurrentDictionary<int, Socket>`으로 교체.
*   **근거:** 기존 `Dictionary`는 멀티스레드 환경에서 데이터 무결성을 보장하지 않으며, 특히 비동기 Accept/Receive가 빈번한 서버 환경에서 `InvalidOperationException` 또는 교착 상태(Deadlock)를 유발할 위험이 큼. `ConcurrentDictionary` 도입을 통해 별도의 수동 Lock 없이 원자적(Atomic) 작업을 보장함.

### 1.2. SQL Injection 취약점 제거
*   **작업 내용:** `DatabaseConnector.cs` 및 `DataBaseConnectHelper.cs` 내의 문자열 조립 방식 쿼리를 매개변수화된 쿼리(Parameterized Query)로 전면 리팩토링.
*   **근거:** 사용자의 입력값이 포함된 문자열을 직접 SQL로 실행하는 것은 가장 치명적인 보안 결함임. ADO.NET의 `MySqlParameter`를 사용하여 쿼리와 데이터를 분리함으로써 악의적인 SQL 명령어 삽입을 원천 차단함.

### 1.3. 데이터베이스 리소스 누수 방지
*   **작업 내용:** `MySqlConnection`, `MySqlCommand`, `MySqlDataReader`에 `using` 문 적용 및 콜백 기반 처리 도입.
*   **근거:** 수동으로 `Close()`를 호출하는 방식은 예외 발생 시 리소스가 해제되지 않아 커넥션 풀 고갈(Connection Pool Starvation)을 유발함. `IDisposable` 인터페이스를 준수하여 가비지 컬렉션 전 리소스 반환을 확정함.

---

## 🏗️ Phase 2: 아키텍처 현대화 (Architecture Modernization)

### 2.1. 미들웨어 파이프라인 도입 (Middleware Pattern)
*   **작업 내용:** `IPacketMiddleware` 인터페이스 및 `PacketPipeline` 엔진 구축.
*   **근거:** `MainServer.cs`가 패킷 수신부터 비즈니스 로직 처리까지 모두 담당하는 **God Object**가 되는 것을 방지(SRP 준수). 패킷 처리 과정을 `검증 -> 복호화 -> 인증 -> 라우팅` 단계로 모듈화하여 유지보수성과 확장성을 확보함.

### 2.2. JWT 기반 인증 시스템 탑재
*   **작업 내용:** `System.IdentityModel.Tokens.Jwt` 라이브러리를 도입하여 로그인 시 토큰 발급 및 `AuthMiddleware`에서 검증 로직 구현.
*   **근거:** 단순 `GUID` 대조 방식은 세션 하이재킹 및 재전송 공격에 취약함. 표준화된 JWT(JSON Web Token)를 사용하여 무상태(Stateless) 인증 체계를 구축하고 보안 강도를 Production 수준으로 격상함.

### 2.3. 의존성 주입(DI) 및 순환 참조 제거
*   **작업 내용:** `Microsoft.Extensions.DependencyInjection` 도입 및 `IMessageSender`, `ISessionManager`, `IDatabaseService` 인터페이스 추출.
*   **근거:** 
    *   **탈결합(Decoupling):** 클래스 간 직접 참조를 인터페이스 의존으로 변경(DIP 준수)하여 단위 테스트 및 모듈 교체가 용이하게 함.
    *   **순환 참조 해결:** `MainServer`와 `ServerLogic`이 서로를 참조하던 안티 패턴을 `IMessageSender` 인터페이스를 통한 단방향 의존성 주입으로 해결함.

---

## 📊 기술적 결정 근거 요약 (Summary of Basis)

| 작업 영역 | 문제점 (AS-IS) | 해결책 (TO-BE) | 기술적 근거 |
| :--- | :--- | :--- | :--- |
| **동시성** | Dictionary 경합 위험 | `ConcurrentDictionary` | 스레드 안전한 컬렉션 사용으로 서버 크래시 방지 |
| **보안** | SQL Injection 노출 | 매개변수화된 쿼리 | 보안 표준 준수 및 데이터베이스 보호 |
| **구조** | God Object / 스파게티 코드 | 미들웨어 파이프라인 | 단일 책임 원칙(SRP) 강화 |
| **인증** | 단순 GUID 비교 (취약) | 표준 JWT 인증 | 보안 인증 메커니즘의 현대화 |
| **결합도** | 강한 결합 / 순환 참조 | DI 컨테이너 및 인터페이스 | 의존성 역전 원칙(DIP) 및 확장성 확보 |

---
**보고 완료:** 위 작업들은 디렉터의 지시서(`Remediation Directive`, `Architecture Directive`)와 기술 진단 보고서를 바탕으로 수행되었으며, 빌드 테스트를 통해 무결성이 검증되었습니다.
