# TeruTeru Server AI Engine (v2.0)

**TeruTeru Server**는 고성능 비동기 IO(IOCP) 기반의 C# 서버 엔진이자, 딥러닝(YOLO) 분석을 위한 **AI 호스팅 플랫폼**입니다. 아키텍처 2.0 리팩토링을 통해 서버 로직의 독립성과 핫로딩(Hot-Reloading) 기능을 확보했습니다.

## 🚀 주요 특징 (Architecture 2.0)

- **4계층 레이어드 아키텍처**: SDK, Runtime, Commands, Cli의 정교한 분리를 통해 유지보수성과 확장성을 극대화했습니다.
- **플러그인 핫로딩 (Hot-Reloading)**: 서버를 끄지 않고도 비즈니스 로직(DLL)을 즉시 교체 및 반영할 수 있는 동적 로딩 시스템을 지원합니다.
- **AI-Ready SDK**: 서버 엔진 SDK에 `OpenCV`, `TorchSharp(PyTorch)`, `ML.NET`이 기본 탑재되어 고성능 AI 로직 개발이 즉시 가능합니다.
- **미들웨어 파이프라인**: `Validation -> Decryption -> JWT Auth -> Routing`으로 이어지는 유연한 패킷 처리 파이프라인을 갖추고 있습니다.
- **보안 강화**: JWT(JSON Web Token) 기반의 세션 인증 시스템을 통해 패킷 보안을 강화했습니다.

## 📂 프로젝트 구조

| 프로젝트명 | 역할 | 비고 |
| :--- | :--- | :--- |
| **`TeruTeruServer.SDK`** | 개발 도구 및 규약 | 프로토콜, 인터페이스, AI 유틸리티, 공용 모델 포함 |
| **`TeruTeruServer.Runtime`** | 서버 코어 엔진 | 소켓(IOCP), 파이프라인, 플러그인 매니저, 세션 관리 |
| ** TeruTeruServer.Commands`** | 서버 제어 커맨드 | 서버 운영에 필요한 모듈화된 명령어 처리기 |
| **`TeruTeruServer.Cli`** | 인터랙티브 진입점 | 엔진 호스팅, CUI 입력 루프, 실시간 로그 모니터링 |
| **`Logic.Default`** | 비즈니스 로직 플러그인 | **사용자 구현 공간.** 핫로딩 대상 프로젝트 |

## 🛠 기술 스택

- **Core**: .NET 8.0 / C#
- **AI/CV**: OpenCvSharp4, TorchSharp, Microsoft.ML
- **Network**: System.Net.Sockets (Async IO), JWT Authentication
- **Architecture**: Dependency Injection, Plugin Architecture (AssemblyLoadContext)

## 🚦 시작하기 (Getting Started)

### 1. 전체 솔루션 빌드
```bash
dotnet build
```

### 2. 서버 실행 (Cli 프로젝트)
```bash
dotnet run --project TeruTeruServer.Cli
```

### 3. 로직 개발 및 핫로딩 테스트
1. `TeruTeruServer.Logic.Default` 프로젝트에서 로직을 수정합니다.
2. 해당 프로젝트만 빌드합니다: `dotnet build TeruTeruServer.Logic.Default`
3. 서버 엔진이 켜져 있는 상태에서 자동으로 `plugins` 폴더에 반영되어 로직이 갱신됩니다.

## 📖 추가 문서 (Documentation)

- [**개발 작업 로그**](./Document/Development_Task_Log.md): 리팩토링 및 페이즈별 진행 상황
- [**아키텍처 지시서**](./Document/Phase2_Architecture_Directive.md): 현대화 설계 철학

---
© 2026 TeruTeru Server Team. All rights reserved.
