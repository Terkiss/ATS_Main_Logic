# TeruTeru Server AI Engine (v2.0)

**TeruTeru Server**는 고성능 비동기 IO(IOCP) 기반의 C# 서버 엔진이자, 딥러닝(YOLO) 분석을 위한 **AI 호스팅 플랫폼**입니다. 아키텍처 2.0 리팩토링을 통해 서버 로직의 독립성 확보, 핫로딩(Hot-Reloading) 기능, 그리고 **P2P 하이브리드 멀티캐스트 인프라**를 완성했습니다.

## 🚀 주요 특징 (Architecture 2.0)

- **강력한 계층 분리**: SDK, Runtime, Commands, Cli, Client의 정교한 분리를 통해 유지보수성과 확장성을 극대화했습니다.
- **P2P 하이브리드 멀티캐스트**: 홀펀칭 기술을 활용하여 성공한 Peer간 직접 통신을 지원하고, 실패한 경우에만 서버가 릴레이하는 최적화된 하이브리드 그룹 통신(`P2PGroup`)을 지원합니다.
- **미들웨어 파이프라인**: `Validation -> Decryption -> JWT Auth -> Routing`으로 이어지는 유연한 패킷 처리 파이프라인을 갖추고 있습니다.
- **플러그인 핫로딩 (Hot-Reloading)**: 서버를 끄지 않고도 비즈니스 로직(DLL)을 즉시 교체 및 반영할 수 있는 동적 로딩 시스템을 지원합니다.
- **AI-Ready SDK**: 서버 엔진 SDK에 `OpenCV`, `TorchSharp(PyTorch)`, `ML.NET`이 탑재되어 고성능 AI 로직 개발이 즉시 가능합니다.
- **테스트 주도 개발(TDD) 환경**: 모든 핵심 로직에 대한 검증을 완료한 견고한 테스트 환경(`.Tests`)을 갖추고 있습니다.

## 📂 프로젝트 구조

| 프로젝트명 | 역할 | 비고 |
| :--- | :--- | :--- |
| **`TeruTeruServer.SDK`** | 코어 개발 도구 | 프로토콜, 인터페이스, AI 유틸리티, 공용 모델(P2P 등) 포함 |
| **`TeruTeruServer.Runtime`** | 서버 코어 엔진 | 소켓(IOCP), 파이프라인, 플러그인 매니저, 세션 관리 |
| **`TeruTeruServer.Commands`** | 서버 제어 커맨드 | 서버 운영에 필요한 모듈화된 명령어 처리기 |
| **`TeruTeruServer.Cli`** | 인터랙티브 진입점 | 엔진 호스팅, CUI 입력 루프, 실시간 로그 모니터링 |
| **`TeruTeruServer.Client`** | 클라이언트 SDK | 게임/클라이언트 앱에서 서버와 P2P로 직결하기 위한 고수준 API |
| **`Logic.Default`** | 비즈니스 로직 플러그인 | **사용자 구현 공간.** 핫로딩 대상 프로젝트 |
| **`*.Tests`** | 단위 테스트 | 프레임워크 건전성 및 로직 무결성을 검증하는 TDD 프로젝트 모음 |

## 🛠 기술 스택

- **Core**: .NET 9.0 / C#
- **AI/CV**: OpenCvSharp4, TorchSharp, Microsoft.ML
- **Network**: System.Net.Sockets (Async IO UDP/TCP), JWT Authentication, P2P Holepunching
- **Architecture**: Dependency Injection, Plugin Architecture (AssemblyLoadContext), Middleware Pipeline

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

새로 합류하신 분들이나 AI 에이전트를 위한 친절한 맞춤형 가이드들이 준비되어 있습니다!

- [**초보자용 개발자 가이드**](./Document/guide/developer-guide.md) ✨
- [**클라이언트 SDK 가이드**](./Document/Client_SDK_Guide.md)
- [**아키텍처 지시서**](./Document/Phase2_Architecture_Directive.md)
- [**에이전트 페르소나 및 프롬프트**](./Document/Prompt)
- [**개발 작업 로그**](./Document/Development_Task_Log.md)

---
© 2026 TeruTeru Server Team. All rights reserved.
