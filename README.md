# TeruTeru Server

고성능 비동기 IO(IOCP) 기반의 C# 서버 엔진입니다. 유니티 클라이언트와의 통신 및 객체 탐지(YOLO) 작업을 위한 중계 서버 역할을 수행합니다.

## 🚀 주요 기능

- **고성능 통신**: .NET의 `SocketAsyncEventArgs`를 사용한 비동기 IO 방식(IOCP 기반)으로 수천 명의 클라이언트 동시 접속을 효율적으로 처리합니다.
- **멀티 프로토콜 지원**:
  - **TCP**: 안정적인 데이터 전송을 위한 기본 프로토콜.
  - **UDP**: `Connected UDP` 방식을 통해 세션 기반의 빠른 실시간 데이터 전송 지원.
- **RPC(Remote Procedure Call) 시스템**:
  - 구조체 직렬화를 통한 직접 프로토콜 및 JSON 기반 프로토콜을 모두 지원합니다.
  - 클라이언트 간 객체 탐지 요청 및 결과 중계 기능을 제공합니다.
- **보안 및 유틸리티**:
  - **AES-CBC-256**: 강력한 문자열 암호화 및 복호화 기능을 내장하고 있습니다.
  - **TeruTeruLogger**: 색상별 로그 출력 및 하드웨어 로그 별도 기록 기능을 갖춘 고성능 로깅 시스템.
- **설정 관리**: 실행 시 `config.txt`를 통해 서버 모드(TCP/UDP), 포트, 최대 접속자 수 등을 동적으로 설정할 수 있습니다.

## 🛠 기술 스택

- **Language**: C# (.NET Core / Standard)
- **Network**: System.Net.Sockets (IOCP)
- **Serialization**: System.Runtime.InteropServices (Marshal), System.Text.Json
- **Security**: System.Security.Cryptography

## 📂 프로젝트 구조

```text
iocp/
├── Command/         # 서버 콘솔 명령어 처리 로직
├── ManageLogic/     # 서버 비즈니스 로직 및 프로토콜 핸들러
│   ├── Protocol/    # RPC Stub, Proxy 및 데이터 구조체
│   └── Util/        # 로깅, 암호화, 메모리 관리 유틸리티
├── Document/        # 상세 기술 문서
└── config.txt       # 서버 설정 파일
```

## 📖 문서 (Documentation)

자세한 사용법과 코드 설명은 아래 문서를 참고하세요.

- [**사용 설명서 (Usage Guide)**](./Document/Usage.md): 서버 실행 방법 및 클라이언트 연동 가이드
- [**코드 기술 문서 (Code Documentation)**](./Document/CodeDocumentation.md): 클래스 설명 및 아키텍처 상세

## 🚦 시작하기

1. **빌드**:
   ```bash
   dotnet build iocp.sln
   ```
2. **실행**:
   ```bash
   dotnet run --project iocp/TeruTeruServer.csproj
   ```
3. **설정**: 최초 실행 시 콘솔 프롬프트를 통해 IP, 포트, 프로토콜 방식을 입력하면 `config.txt`가 자동 생성됩니다.

---

