# TeruTeru Server AI Engine (v2.0)

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/download)
[![C#](https://img.shields.io/badge/C%23-13.0-239120?logo=csharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**TeruTeru Server**는 고성능 비동기 IO(IOCP) 기반의 C# 서버 엔진이자, 딥러닝(YOLO) 분석을 위한 **AI 호스팅 플랫폼**입니다. 아키텍처 2.0 리팩토링을 통해 서버 로직의 독립성 확보, 핫로딩(Hot-Reloading) 기능, 그리고 **P2P 하이브리드 멀티캐스트 인프라**를 완성했습니다.

## 🚀 주요 특징 (Key Features)

- **🏛️ 강력한 4계층 분리**: SDK, Runtime, Commands, Client의 정교한 계층 분리로 유지보수성과 확장성을 극대화했습니다.
- **🌐 P2P 하이브리드 인프라**: 홀펀칭 기술을 활용한 Peer간 직접 통신을 지원하며, 실패 시 서버가 자동 릴레이로 전환되는 하이브리드 그룹 통신을 완성했습니다.
- **🎮 실시간 게임 엔진**: Tick Loop 기반의 상태 동기화(State Sync), 지연 보상(Lag Compensation), 그리고 예측 기반 네트워크 아키텍처를 탑재했습니다.
- **📂 플러그인 핫로딩 (Hot-Reloading)**: `AssemblyLoadContext`를 통해 서버 가동 중에도 비즈니스 로직(DLL)을 즉시 교체 및 반영할 수 있습니다.
- **🛡️ 8단계 보안 파이프라인**: `Validation -> BanCheck -> RateLimit -> ReplayAttack -> HmacVerify -> Decryption -> Auth -> Routing`으로 이어지는 철통 보안 시스템을 갖추고 있습니다.
- **🧠 AI-Ready SDK**: 서버 엔진 SDK에 `OpenCV`, `TorchSharp(PyTorch)`, `ML.NET`이 내장되어 실시간 AI 분석 로직 개발이 즉시 가능합니다.

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

## 📖 문서 가이드 (Documentation)

프로젝트의 상세 설계와 사용법은 `Documents/` 디렉토리에 체계적으로 정리되어 있습니다.

### 📘 핵심 문서 (Core)
- [**학생용 맞춤 가이드 (Coding Mentor)**](./Documents/Internal/Guides/ProjectSpecific/공통가이드/README.md) ✨: 코딩 동생들을 위한 친절한 테마파크 안내서
- [**개발자 온보딩 가이드**](./Documents/Internal/Guides/ProjectSpecific/developer-guide.md) 👨‍💻: 프로젝트 전체 구조 및 기술 세부 가이드
- [**AI 전용 기술 문서 (Full Context)**](./alternate_AI_doc.md) 🤖: 100만 토큰 AI 에이전트를 위한 상세 명세
- [**기술 세부 명세서**](./Documents/Technical/Protocol_Spec.md): 패킷 구조 및 프로토콜 규약
- [**클라이언트 SDK 가이드**](./Documents/Technical/Client_SDK_Guide.md): 클라이언트 연동 및 P2P 활용법

### 📂 상세 문서 구조
- [**Technical/**](./Documents/Technical): 아키텍처, 데이터베이스, 보안 등 심화 기술 문서
- [**UserGuide/**](./Documents/UserGuide): 설치, 사용법 및 트러블슈팅 가이드
- [**Internal/**](./Documents/Internal): 에이전트 프롬프트 및 프로젝트별 세부 지시서

---
© 2026 TeruTeru Server Team. All rights reserved.
