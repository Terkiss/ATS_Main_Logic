**[문서 내비게이션 바]**
**[Technical]** [Architecture](../Technical/Architecture.md) | [API Reference](../Technical/API_Reference.md) | [Setup Guide](../Technical/Setup_Guide.md) | [Database Schema](../Technical/Database_Schema.md)
**[UserGuide]** [Introduction](./Introduction.md) | [Installation](./Installation.md) | [How to Use](./How_to_Use.md) | [Troubleshooting](./Troubleshooting.md)
---

# 단 3분 만에 끝나는 설치 가이드 ⏱️

복잡한 환경 설정에 지치셨나요? TeruTeru Server는 초보자도 쉽게 따라 할 수 있도록 설계되었습니다. 아래 1-2-3 단계만 따라오시면 내 컴퓨터에 강력한 AI 서버가 켜집니다!

---

### Step 1: 준비물 챙기기 (필수 프로그램 설치)
이 서버는 마이크로소프트의 최신 기술인 **.NET 9.0** 위에서 돌아갑니다.
1.  [Microsoft .NET 다운로드 페이지](https://dotnet.microsoft.com/download/dotnet/9.0)에 접속합니다.
2.  내 컴퓨터 운영체제(Windows/Mac/Linux)에 맞는 **.NET SDK**를 다운로드하고 설치의 '다음' 버튼만 계속 누르시면 끝!

### Step 2: 코드 다운로드 및 빌드 (엔진 조립하기)
터미널(또는 명령 프롬프트, PowerShell)을 열고 소스코드가 있는 폴더로 이동합니다. 그리고 마법의 명령어 딱 하나만 입력하세요.

```bash
dotnet build iocp.sln
```
*✨ 와우! 이 명령어 한 줄로 엔진, 네트워크 규약, 기본 플러그인이 모두 알맞은 위치에 자동으로 조립됩니다.*

### Step 3: 서버 전원 켜기!
이제 조립된 서버를 켜볼 차례입니다. CLI(명령줄 인터페이스) 폴더로 들어가서 실행해 보세요.

```bash
cd TeruTeruServer.Cli
dotnet run
```

처음 켜셨다면 친절하게 포트 번호 등을 물어보는 화면이 나올 수 있습니다. (엔터만 쳐서 기본값을 사용하셔도 좋습니다!)

```text
=== TeruTeruServer AI Engine Runtime Started ===
[PluginManager] Monitoring plugins at: ...
Server Start
Server Port : 8080
Server Running
```
이런 화면이 보인다면 성공입니다! 축하합니다. 여러분의 컴퓨터가 이제 수만 명의 접속을 감당할 수 있는 AI 서버가 되었습니다. 🎉

👉 다음으로: 서버를 어떻게 마음대로 요리할 수 있는지 [사용 방법(How to Use)](./How_to_Use.md)에서 알아보세요!

---

### Step 4: 서버 설정 및 보안 (config.txt)
서버를 처음 실행하면 `config.txt` 파일이 생성됩니다. 보안을 위해 **HmacKey**를 자신만의 복잡한 문자열로 변경하는 것이 좋습니다. 이 키는 클라이언트와 서버가 서로를 신뢰하는 데 사용됩니다.