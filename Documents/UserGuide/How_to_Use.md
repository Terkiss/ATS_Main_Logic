**[문서 내비게이션 바]**
**[Technical]** [Architecture](../Technical/Architecture.md) | [API Reference](../Technical/API_Reference.md) | [Setup Guide](../Technical/Setup_Guide.md) | [Database Schema](../Technical/Database_Schema.md)
**[UserGuide]** [Introduction](./Introduction.md) | [Installation](./Installation.md) | [How to Use](./How_to_Use.md) | [Troubleshooting](./Troubleshooting.md)
---

# 어떻게 사용하나요? (How to Use) 🛠️

서버를 켰다면, 이제 이 서버를 "나만의 방식"으로 움직이게 만들어야겠죠? 
가장 많이 쓰이는 두 가지 핵심 유스케이스를 친절하게 설명해 드립니다.

---

## 1. 관리자 콘솔 명령어 사용하기
서버가 켜져 있는 까만 창(콘솔)은 단순한 로그 뷰어가 아닙니다. 서버와 대화할 수 있는 창구입니다.

`[이미지 플레이스홀더: 서버 콘솔창에 명령어를 입력하는 스크린샷]`

*   **`Queue_Count` 입력 후 엔터:** 현재 서버가 밀리지 않고 AI 이미지 분석(YOLO)을 잘 처리하고 있는지, 대기열(Queue)에 쌓인 작업 개수를 알려줍니다.
*   **`Worker_Start` 입력 후 엔터:** "자, 이제 이미지 분석 시작해!" 하고 AI 일꾼(Worker) 스레드를 깨우는 명령어입니다.

---

## 2. 나만의 통신 기능 만들기 (Vibe Coding!)

클라이언트가 `"안녕!"` 하고 보내면 서버가 `"서버도 안녕!"` 하고 대답하게 만들고 싶으신가요? 네트워크 지식은 1도 필요 없습니다.

**`TeruTeruServer.Logic.Default`** 프로젝트 안에 있는 **`LogicPlugin.cs`** 파일을 열고 아래 코드만 추가해 보세요.

### 🔥 초간단 RPC 마법

```csharp
// 1. [Rpc] 태그를 붙이고 클라이언트가 호출할 이름을 적습니다.
[RequiresAuth] // 인증된 유저만 호출 가능하게 설정 (선택)
[Rpc("SayHello")]
public async Task<string> ReplyToClient(Socket socket, string clientMessage)
{
    // 2. 원하는 비즈니스 로직을 자유롭게 작성합니다.
    TeruTeruLogger.LogInfo($"유저 메시지 수신: {clientMessage}");
    
    // 3. 결과값을 리턴하면 엔진이 알아서 클라이언트에게 전송합니다.
    return $"서버도 반녕! 네가 보낸 메시지 '{clientMessage}'를 잘 받았어.";
}
```

### 💡 보너스 팁: 서버 끄지 않고 업데이트하기 (Hot-Reload)
위 코드를 다 작성하셨나요? 서버 콘솔창을 끄지 마세요! 그냥 다른 터미널에서 `dotnet build TeruTeruServer.Logic.Default` 한 줄만 쳐보세요. 서버 창에 `[PluginManager] Logic plugin hot-reloaded successfully.` 라는 메시지가 뜨면서 방금 짠 코드가 서버에 즉시 적용됩니다! 이게 바로 **Architecture 2.0**의 힘입니다. 😎