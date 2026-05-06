# 🚀 TeruTeru Server (v2.0) - 신규 개발자 온보딩 가이드

## 👋 1. 반가운 환영 인사
환영합니다! 🎉 TeruTeru Server 팀에 합류하신 것을 진심으로 환영해요. 
새로운 프로젝트 코드를 처음 열어보면 누구나 조금 막막하고 낯설게 느껴질 수 있어요. 하지만 걱정하지 마세요! 이 가이드는 여러분이 우리 시스템의 전체적인 구조와 흐름을 쉽게 이해하고, 즐겁게 코딩을 시작할 수 있도록 돕기 위해 작성되었습니다. 커피 한 잔 ☕ 준비하시고, 천천히 읽어봐 주세요!

## 🔭 2. 프로젝트 한눈에 보기
**TeruTeru Server**는 고성능 비동기 통신(IOCP)을 기반으로 동작하는 C# 서버 엔진이자, 딥러닝(YOLO 등) 비전 분석을 제공하는 **AI 호스팅 플랫폼**입니다.

우리가 해결하고자 하는 가장 큰 핵심 과제는 **"중단 없는 서비스 운영"**과 **"안전하고 유연한 AI 기능 제공"**입니다. 이를 위해 최근 아키텍처 2.0으로 개편하면서 **플러그인 핫로딩(Hot-Reloading)** 기능을 도입했어요. 이제 서버를 끄지 않고도 비즈니스 로직(DLL)을 즉시 교체할 수 있답니다!

## 🛠️ 3. 기술 스택 (우리가 이 기술을 선택한 이유!)
- **Core (C# / .NET 8.0)**: 강력한 타입 시스템과 뛰어난 성능, 그리고 훌륭한 비동기 프로그래밍(async/await) 지원 덕분에 안정적이고 고성능의 서버를 구축하기 위해 선택했어요.
- **Network (System.Net.Sockets)**: 닷넷 코어의 고성능 비동기 IO(IOCP)를 직접 제어하여 대규모 트래픽에도 끄떡없는 네트워크 엔진을 만들기 위해 사용하고 있습니다.
- **AI / CV (TorchSharp, OpenCvSharp4, Microsoft.ML)**: C# 환경에서도 강력한 파이토치(PyTorch) 모델과 영상 처리 알고리즘을 바로 구동하여 실시간 객체 탐지 등을 수행하기 위해 SDK에 기본 탑재했습니다.
- **Architecture (AssemblyLoadContext)**: 서버 무중단 배포를 실현하기 위해 닷넷의 동적 어셈블리 로드 기능을 활용하여 플러그인 아키텍처를 구현했어요.
- **Security (JWT)**: 세션 탈취 및 위조 방지를 위해, 인증된 유저만 패킷을 보낼 수 있도록 JSON Web Token 기반 인증(Authentication) 시스템을 사용합니다.

## 📁 4. 폴더 구조 파헤치기 (Project Structure)
저희 프로젝트는 역할을 철저하게 분리한 4계층 아키텍처를 사용하고 있어요. 가장 중요한 디렉토리들을 살펴볼까요?

- 📦 **`TeruTeruServer.SDK`**: 프로젝트의 '계약서' 같은 곳이에요. 각종 공통 인터페이스(`ILogicService` 등)와 프로토콜 모델, AI 유틸리티 클래스가 정의되어 있습니다.
- 📦 **`TeruTeruServer.Runtime`**: 서버의 '심장'입니다! 소켓 통신과 세션 관리를 담당하는 `MainServer.cs`와 플러그인을 동적으로 교체해 주는 `PluginManager.cs`가 위치해 있어요.
- 📦 **`TeruTeruServer.Cli`**: 서버의 '얼굴'이자 진입점입니다. 서버를 호스팅하고, 터미널(CUI)에서 명령어를 입력받거나 실시간 로그(`ConfigManager.cs`)를 보여주는 `Program.cs`가 있습니다.
- 📦 **`TeruTeruServer.Commands`**: 서버 운영 중 터미널에 입력하는 관리자 명령어(Command)들의 처리 로직이 모듈화되어 있습니다.
- 📦 **`TeruTeruServer.Logic.Default` (🔥 매우 중요!)**: **여러분이 주로 작업하실 공간**입니다! 유저 로그인, 게임 로직, AI 분석 요청 등 실제 비즈니스 로직이 구현되는 핫로딩 대상 플러그인 프로젝트에요.

## 🚦 5. 시작하기 (Getting Started)
자, 이제 서버를 직접 띄워볼까요? 복잡한 설정 없이 아주 간단해요!

1. **전체 솔루션 빌드하기**
   터미널에서 아래 명령어를 입력해 전체 프로젝트 의존성을 빌드해주세요.
   ```bash
   dotnet build
   ```

2. **서버 실행하기**
   진입점인 `Cli` 프로젝트를 실행하여 서버 엔진을 가동합니다.
   ```bash
   dotnet run --project TeruTeruServer.Cli
   ```

3. **마법의 핫로딩(Hot-Reloading) 체험하기! ✨**
   서버가 켜져 있는 상태에서 터미널을 하나 더 열어주세요.
   `TeruTeruServer.Logic.Default` 폴더 내의 `LogicPlugin.cs` 코드(예: 로그 메시지 등)를 약간 수정한 뒤, 아래 명령어로 해당 로직 플러그인만 다시 빌드해보세요.
   ```bash
   dotnet build TeruTeruServer.Logic.Default
   ```
   서버를 실행해둔 터미널을 보시면 재부팅 없이도 `[PluginManager] Logic plugin hot-reloaded successfully.` 로그가 뜨며 로직이 즉시 반영된 것을 보실 수 있을 거예요!

## 🤝 6. 코드 컨벤션 & 협업 규칙
우리 팀이 함께 읽기 편하고 유지보수하기 좋은 코드를 유지하기 위한 약속입니다.

- **명명 규칙 (Naming Convention)**
  - 클래스와 메서드, 프로퍼티는 `PascalCase`를 사용합니다. (예: `PluginManager`, `ProcessJsonProtocol`, `SecretKey`)
  - 로컬 변수와 매개변수는 `camelCase`를 사용합니다. (예: `loginData`, `socket`)
  - `private` 혹은 `readonly` 멤버 변수는 언더바(`_`)로 시작하는 `_camelCase`를 사용합니다. (예: `_currentLogic`, `_dbService`, `_messageSender`)
- **Git 브랜치 전략**
  - 새로운 기능 개발 전에는 반드시 관련된 새로운 브랜치를 생성하여 작업해 주세요.
  - 의미 있는 논리적 단위로 커밋을 남기고, 커밋 메시지는 다른 사람이 읽어도 의도를 명확히 알 수 있도록 작성해 주세요.

## 🧠 7. 핵심 비즈니스 로직 흐름
데이터가 어떻게 흘러가는지 알면 코드가 훨씬 잘 읽혀요! 

네트워크 소켓을 통해 클라이언트가 데이터를 보내면, 서버 패킷 파이프라인(`Validation -> Decryption -> JWT Auth -> Routing`)을 거쳐 최종적으로 **`TeruTeruServer.Logic.Default`의 `LogicPlugin.cs`**에 도착합니다.

- **`LogicPlugin.cs`**: `ILogicService` 인터페이스를 구현한 비즈니스 로직의 총괄 클래스입니다.
  - `ProcessDirectProtocol(byte[] buffer, Socket socket)`: 이미지 덤프 데이터와 같은 특수한 바이너리 데이터나 직접적인(Direct) 프로토콜을 처리해요.
  - `ProcessJsonProtocol(string json, ProtocolSelect protocol, Socket socket)`: 회원가입, 로그인 등 JSON 형태의 일반적인 API 요청을 `ProtocolSelect` Enum 값에 따라 분기하여 처리합니다 (`HandleJsonProtocol` 등).
  - 인증 처리: 이곳에서 `GenerateJwtToken` 등을 통해 토큰을 발급하기도 합니다.
- **`PluginManager.cs` (`Runtime` 계층)**: 서버가 기동할 때 `plugins/` 폴더의 상태를 감시하는 녀석입니다. 개발자가 로직을 수정하고 빌드해서 새로운 DLL이 생성되면, `AssemblyLoadContext`를 통해 이전 로직을 내리고 새 버전의 로직 인스턴스를 주입(`UpdateLogic`)해 줍니다. 

## 🆘 8. 도움이 필요할 때
프로젝트의 코드가 거대해서 처음에는 길을 잃거나 막히는 부분이 생기는 것이 당연합니다. 혼자 끙끙 앓지 마세요!
- 📄 먼저 `Document/` 폴더 내의 개발 문서들을 참고해 보세요. 특히 `Technical_Audit_Report.md`나 `Phase2_Architecture_Directive.md` 같은 문서를 읽어보시면 우리 프로젝트의 아키텍처 기획 의도를 깊게 이해하는 데 큰 도움이 될 거예요.
- 🗣️ 문서를 봐도 잘 모르겠거나, 코드 리뷰가 필요하다면 언제든지 옆자리의 시니어 개발자나 멘토에게 편하게 질문해 주세요. 우리는 언제나 여러분의 질문을 환영하고 도울 준비가 되어 있답니다!

다시 한번 우리 팀 합류를 환영하며, 여러분의 활약을 기대합니다! 화이팅! 🚀
