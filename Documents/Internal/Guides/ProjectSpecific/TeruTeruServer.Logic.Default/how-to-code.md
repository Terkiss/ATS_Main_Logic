# 🛠️ 게임 규칙 파트 코드 수정법 (Logic)

안녕! 여기서는 우리 놀이공원의 '꿀잼 규칙'들을 어떻게 직접 만드는지 배워볼 거야. ✨

## 📝 서버의 답변 메시지 수정하기 (Echo 서비스)

손님이 보낸 말을 서버가 그대로 되돌려주는 '메아리(Echo)' 서비스를 더 재미있게 바꿔보자!

### 1단계: 규칙서 열기
`TeruTeruServer.Logic.Default/LogicPlugin.cs` 파일을 열어봐.

### 2단계: 코드 수정하기
180번째 줄 근처에 있는 `HandleEcho` 함수를 찾아봐.

```csharp
[Rpc("Echo")]
public async Task<string> HandleEcho(Socket socket, string message)
{
    TeruTeruLogger.LogInfo($"RPC Echo called with: {message}");
    // 이 아래 줄을 바꿔보자! ✨
    return $"[멘토] 네가 '{message}'라고 말했지? 나도 그렇게 생각해! 💖";
}
```

### 3단계: 무엇이 변했나요?
이제 클라이언트에서 'Echo'라는 기능을 호출하면 서버가 내가 적은 다정한 말투로 대답해줄 거야!

---

## 📝 새로운 규칙 추가하기 (마법의 어트리뷰트)

우리 서버는 `[Protocol(...)]`이나 `[Rpc(...)]` 같은 '마법의 이름표(어트리뷰트)'를 붙이면 자동으로 기능을 인식해.

### 1단계: 새로운 함수 만들기
`LogicPlugin.cs` 파일 맨 아래쪽에 새로운 기능을 하나 추가해볼까?

```csharp
[Rpc("SayHello")]
public async Task<string> SayHello(Socket socket)
{
    return "안녕! 나는 이 놀이공원의 마스코트야! ✨";
}
```

### 2단계: 결과 확인
이제 손님(DummyClient)이 "SayHello"라고 서버에게 요청하면, 서버는 방금 만든 인사말을 보내주게 돼!

---

## ✨ 멘토의 미션: "서버의 비밀 정보 알려주기"

우리 서버의 현재 상태를 알려주는 기능을 조금 더 멋지게 바꿔볼까?

1.  `LogicPlugin.cs`의 186번째 줄에 있는 `GetServerInfo` 함수를 찾아봐.
2.  거기 보면 `ServerName`, `Version` 같은 정보가 적혀 있지?
3.  거기에 `"MentorMessage" = "오늘도 코딩 공부 화이팅! 🚀"` 이라는 항목을 하나 추가해봐.

성공했다면 이제 서버 정보를 확인할 때마다 멘토의 응원 메시지도 함께 보일 거야!
