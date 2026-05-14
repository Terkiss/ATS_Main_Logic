# 🛠️ 가짜 손님 파트 코드 수정법 (DummyClient)

안녕! 여기서는 우리 서버를 테스트하는 '로봇 손님'을 어떻게 똑똑하게 만드는지 배워볼 거야. ✨

## 📝 서버에게 말 걸기 (RPC 호출)

로봇 손님이 서버의 특정 기능을 호출하는 법이야.

### 1단계: 행동 지침서 열기
`DummyClient/Program.cs` 파일을 열어봐.

### 2단계: 코드 수정하기
64번째 줄 근처를 보면 `InvokeRpcAsync`라는 마법의 주문이 있어.

```csharp
// 64번째 줄 근처
var echoResult = await client.InvokeRpcAsync<string>("Echo", "로봇의 비밀 메시지! ✨");
Console.WriteLine($"서버의 대답: {echoResult}");
```

### 3단계: 무엇이 변했나요?
로봇 손님이 서버에게 "Echo"라는 기능을 실행해달라고 부탁하고, 그 결과를 받아서 화면에 보여줄 거야.

---

## 👂 서버가 하는 말 귀 기울이기

서버가 로봇 손님에게 갑자기 말을 걸 때(Push), 어떻게 반응할지 정해줄 수 있어.

### 1단계: 반응 규칙 찾기
`Program.cs` 파일 위쪽에 있는 `MyClientLogic` 클래스를 찾아봐.

### 2단계: 코드 수정하기
새로운 반응 규칙을 하나 추가해볼까?

```csharp
[Rpc("OnMentorSurprise")] // 서버가 "OnMentorSurprise"라고 부르면 실행!
public void OnSurprise(string message)
{
    Console.WriteLine($"[깜짝 소식] 멘토가 보낸 메시지: {message} ✨");
}
```

---

## ✨ 멘토의 미션: "자동 로그인 로봇 만들기"

로봇 손님이 켜지자마자 우리만의 아이디로 로그인하게 해볼까?

1.  `Program.cs`의 58번째 줄을 찾아봐.
2.  `client.LoginAsync("developer_test", "dev_pass")` 부분이 있지?
3.  거기 아이디를 `"my_robot"`, 비밀번호를 `"robot123"`으로 바꿔봐.
4.  서버를 켠 상태에서 이 로봇을 실행하면, 서버 로그에 `my_robot`이 들어왔다는 반가운 소식이 뜰 거야!

성공했다면 너는 이제 완벽한 '테스트 엔지니어'야! 🤖✨
