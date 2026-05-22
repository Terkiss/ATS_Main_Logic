# 작업 지시문 (Worker Directive) - Milestone 15: Database Hardening & ORM

본 지시문은 `Milestone 15 — Database Hardening & ORM` 구현을 위한 구체적인 가이드라인입니다.

---

## 🎯 목표
기존 `DatabaseConnector.cs` 및 `DataBaseConnectHelper.cs`에 구현되어 있는 원시 ADO.NET 및 MySql.Data 연결 생성/수행 코드를 안전하고 현대적인 **Dapper ORM**으로 마이그레이션하고, 데이터베이스 트랜잭션 도중 일시적인 네트워크 장애나 데드락(Deadlock)에 견디기 위해 **Polly 복원력(Resilience) 정책**을 활용하여 커넥션 및 쿼리 실행을 강건화(Hardening)합니다.

---

## 🔍 기존 코드 정밀 분석 및 대상 파일
- **[MODIFY]** [DatabaseConnector.cs](file:///Users/yujeonghun/Project/ATS_Main_Logic/TeruTeruServer.Runtime/DB/DatabaseConnector.cs)
- **[MODIFY]** [DataBaseConnectHelper.cs](file:///Users/yujeonghun/Project/ATS_Main_Logic/TeruTeruServer.Runtime/DB/DataBaseConnectHelper.cs)
- **[MODIFY]** [TeruTeruServer.Runtime.csproj](file:///Users/yujeonghun/Project/ATS_Main_Logic/TeruTeruServer.Runtime/TeruTeruServer.Runtime.csproj) (NuGet 패키지 추가용)
- **[NEW]** `TeruTeruServer.Runtime.Tests/DatabaseHardeningTests.cs` (강건화 검증 단위 테스트)

---

## ★ 특별 주의사항 (Critical)

1. **소켓 및 세션 nullable 파라미터 전파 (★)**
   - 이전 마일스톤(M14) 작업의 결과로 `ClientSession` 생성자의 소켓 매개변수가 `Socket?`로 nullable화되었습니다. DB 조회를 통해 `ClientSession` 객체를 새로 역직렬화/복원할 때 소켓은 데이터베이스에 저장되지 않는 임시 자원이므로 생성자에 반드시 `null`을 안정적으로 전달할 수 있도록 연동 상태를 확인하십시오.

2. **Dapper 패키지 및 안전한 파라미터화**
   - 기존의 문자열 병합이나 raw 쿼리는 SQL Injection 공격 위험이 높고 쿼리 계획 캐시를 활용하지 못합니다. Dapper의 익명 객체 바인딩(`new { Param = value }`)을 사용하여 무조건 파라미터화된 쿼리 형태로 구현해야 합니다.

3. **Polly 복원력 정책의 적절한 재시도 간격(Exponential Backoff)**
   - MySqlException 에러 중 일시적 에러(네트워크 타임아웃, 커넥션 풀 부족, 데드락 번호 1213 등) 발생 시 무한 재시도가 아닌 **3회 제한 + 지수 백오프(예: 1초, 2초, 4초 대기)** 형태로 안전하게 Polly의 재시도 정책(`RetryPolicy`)을 결합하십시오.

---

## 🛠️ 세부 구현 지침

### 1. `TeruTeruServer.Runtime.csproj` 의존성 추가
- NuGet 패키지 **`Dapper`** 및 **`Polly`** 패키지를 프로젝트에 추가합니다.

### 2. `DatabaseHelper` (DatabaseConnector) 내 Dapper 및 Polly 적용
- `SqlRun`, `SqlBatchRun`, `SqlParrelRun`, `SqlRunForCounter` 등의 내부 구현을 Dapper API (`ExecuteAsync`, `QueryAsync`, `ExecuteScalarAsync` 등)를 사용하여 마이그레이션합니다.
- `Dapper`의 간결함을 살려 기존 `MySqlCommand` 및 Reader의 지저분한 자원 해제 루프를 전부 리팩토링합니다.
- 쿼리 실행을 Polly Policy로 래핑하여 예외 복원력을 갖춘 구조로 만듭니다.

### 3. Connection Pool 설정 연계
- `DatabaseConnector`의 `bindUri` 문자열에 커넥션 풀링 관련 속성(`Pooling=true;Max Pool Size=100;Min Pool Size=10;`)이 명확하게 활성화되었는지 또는 설정으로 주입받을 수 있는지 점검합니다.

### 4. DI 전파 경로 확인
- `DatabaseConnector.DatabaseHelper`는 `TeruTeruServer.Cli/Program.cs`에서 `IDatabaseService` 싱글톤으로 IoC 컨테이너에 등록됩니다.
- 생성자 구조 변경 등으로 인해 DI 주입 및 구성 흐름이 깨지지 않도록 등록 코드(`Program.cs`)와 수신 코드(`LogicPlugin.cs` 등)의 일관성을 유지하십시오.

---

## 🚫 변경 제한 및 허용 범위
- **허용 범위**:
  - `TeruTeruServer.Runtime/DB/` 내 데이터베이스 헬퍼 구현 및 속성 수정.
  - `IDatabaseService` 인터페이스 내 비동기 및 Dapper 매핑을 매끄럽게 하기 위한 필요 사양 확장 허용.
- **금지 범위**:
  - 기존 DI 생명주기(싱글톤 유지) 훼손 금지.
  - 리플렉션 등을 남용해 플러그인 생태계 밖의 데이터를 무단 파싱하거나 파이프라인 보안 로직을 우회하는 동작 금지.

---

## 🧪 검증 절차 (Verification)

1. **장애 주입 및 복원력 검증 테스트 작성**:
   - `TeruTeruServer.Runtime.Tests/DatabaseHardeningTests.cs`를 생성하여 다음을 테스트합니다.
     - 가상의 불안정한 데이터베이스 커넥션 프록시 또는 Mock 예외를 활용하여, 에러 발생 시 Polly 정책에 따라 정해진 횟수만큼 재시도(Retry)가 정상적으로 일어나는지 확인합니다.
     - Dapper를 통한 기본적인 CRUD 동작이 정상 수행되는지 검증합니다.
2. **공식 release gate 실행**:
   - `verify-release.sh`를 구동하여 빌드 오류 및 58개 이상의 전체 테스트 통과를 보장합니다.
