# 작업 지시문 (Worker Directive) - Milestone 16: Monitoring & Observability

본 지시문은 `Milestone 16 — Monitoring & Observability` 구현을 위한 구체적인 가이드라인입니다.

---

## 🎯 목표
OpenTelemetry 표준 API 및 SDK를 도입하여 TeruTeruServer 엔진의 런타임 메트릭(Active Connections, Packet Throughput, Pipeline Latency)과 핵심 분산 트레이싱(Packet Lifecycle) 체계를 계측화(Instrumentation)합니다. 또한 프로메테우스(Prometheus) 포맷 지표 노출 및 분산 추적 스팬(Span) 연동을 완비합니다.

---

## 🔍 기존 코드 분석 및 대상 파일
- **[MODIFY]** [TeruTeruServer.SDK.csproj](file:///Users/yujeonghun/Project/ATS_Main_Logic/TeruTeruServer.SDK/TeruTeruServer.SDK.csproj) (의존성 추가)
- **[MODIFY]** [TeruTeruServer.Runtime.csproj](file:///Users/yujeonghun/Project/ATS_Main_Logic/TeruTeruServer.Runtime/TeruTeruServer.Runtime.csproj) (의존성 추가)
- **[MODIFY]** [MainServer.cs](file:///Users/yujeonghun/Project/ATS_Main_Logic/TeruTeruServer.Runtime/MainServer.cs) (메트릭 갱신 훅 연동 및 시작 라이프사이클 초기화)
- **[MODIFY]** [PacketPipeline.cs](file:///Users/yujeonghun/Project/ATS_Main_Logic/TeruTeruServer.Runtime/Pipeline/PacketPipeline.cs) (패킷 파이프라인 구간 스팬 트레이싱 추가)
- **[NEW]** `TeruTeruServer.SDK/Util/TeruTeruDiagnostics.cs` (OpenTelemetry Meter/ActivitySource 정의 파일)
- **[NEW]** `TeruTeruServer.Runtime.Tests/ObservabilityTests.cs` (메트릭 및 트레이싱 생성 검증 단위 테스트)

---

## ★ 특별 주의사항 (Critical)

1. **DB 커넥션 강건화 및 누수 차단 상태 보존 (★)**
   - M15에서 적용된 Dapper ORM 마이그레이션과 `DatabaseConnector.SqlRunForReader` 내 ADO.NET 기반 `CommandBehavior.CloseConnection` 커넥션 누수 방지 코드를 절대 훼손하거나 롤백하지 마십시오.
   - Polly의 재시도 정책(지수 백오프)이 데이터베이스 조회 장애 발생 시 계속 유지되어야 합니다.

2. **패킷 파이프라인의 오버헤드 최소화**
   - 패킷 파이프라인(`PacketPipeline.cs`)은 매 초당 수천 개의 패킷이 경유하는 핫 패스(Hot path)입니다. OpenTelemetry 트레이싱(`Activity`) 시작/종료 시 스팬을 매번 무조건 할당하기보다, `ActivitySource.StartActivity` 호출 전 리스너 존재 여부를 검사해 성능 저하를 방지하십시오.
   - 로깅 및 트레이싱 문자열 조합 시 불필요한 GC 할당(Allocation)이 생기지 않도록 `StringBuilder` 보단 구조화 로그 매개변수 바인딩을 활용하십시오.

---

## 🛠️ 세부 구현 지침

### 1. 의존성 추가
- `TeruTeruServer.SDK.csproj` 및 `TeruTeruServer.Runtime.csproj`에 다음 OpenTelemetry NuGet 패키지를 추가합니다.
  - `OpenTelemetry`
  - `OpenTelemetry.Extensions.Hosting`
  - `OpenTelemetry.Exporter.Console`
  - `OpenTelemetry.Exporter.Prometheus.HttpListener` (또는 OTLP Exporter)

### 2. Diagnostics 및 지표(Metrics) 선언
- `TeruTeruDiagnostics.cs`에 static `Meter`("TeruTeruServer.Engine.Metrics")와 `ActivitySource`("TeruTeruServer.Engine.Tracing")를 정의합니다.
- 다음 핵심 지표를 구현합니다:
  - `teruteru_active_connections` (ObservableGauge): 현재 접속한 클라이언트 세션 수 (`_sessionManager.Players.Count`와 연동)
  - `teruteru_packets_processed_total` (Counter): 누적 처리된 패킷 수 (프로토콜 타입별 tag/dimension 지원)
  - `teruteru_pipeline_duration_seconds` (Histogram): 패킷 파이프라인 단계별 소요 시간

### 3. 패킷 파이프라인 Activity 계측화
- `PacketPipeline.ExecuteAsync` 시작 시 `ActivitySource.StartActivity("PacketProcessing")`를 호출하여 스팬을 엽니다.
- 미들웨어 처리 성공/실패 여부를 Activity Tag로 기록하고, 예외 발생 시 `Activity.SetStatus(ActivityStatusCode.Error)` 및 에러 로그 기록을 수행합니다.

### 4. OpenTelemetry Provider 초기화 및 시작
- `MainServer.cs` 또는 프로그램 진입점(`Program.cs`)에서 `Sdk.CreateMeterProviderBuilder()` 및 `Sdk.CreateTracerProviderBuilder()`를 구성하여 콘솔 혹은 Prometheus HttpListener Exporter를 추가합니다.
- 서버 종료 시 Provider들을 안전하게 Dispose하여 수집 버퍼를 플러시(Flush)하도록 정리 라이프사이클을 매핑합니다.

---

## 🚫 변경 제한 및 허용 범위
- **허용 범위**:
  - `TeruTeruServer.SDK` 및 `TeruTeruServer.Runtime` 내부 계측화 코드 추가.
  - 런타임 지표 수집용 헬퍼 클래스 생성 및 테스트 코드 작성.
- **금지 범위**:
  - 기존 패킷 시리얼라이제이션 레이아웃이나 보안 토큰 검증 로직 변경 금지.
  - 싱글톤 DI 생명주기 및 Grace 재연결 구조를 방해하는 동적 스레드 생성 금지.

---

## 🧪 검증 절차 (Verification)
1. **모니터링 단위 테스트 작성**:
   - `TeruTeruServer.Runtime.Tests/ObservabilityTests.cs`를 생성하여 메트릭 수치 갱신(Counter 누적 검증) 및 트레이스 Activity가 정상 발행되는지 메모리 기반 수집 환경(Mock)을 구현하여 확인합니다.
2. **공식 release gate 실행**:
   - `./scripts/verify-release.sh`를 구동하여 컴파일 경고/오류 0개 및 전체 테스트 통과를 보장합니다.
