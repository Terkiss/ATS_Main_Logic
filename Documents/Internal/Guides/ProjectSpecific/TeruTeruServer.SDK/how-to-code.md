# 🛠️ 기초 공사 파트 코드 수정법 (SDK)

안녕! 여기서는 우리 놀이공원의 '기본 규칙'을 어떻게 바꾸는지 배워볼 거야. ✨

## 📝 새로운 대화 주제(프로토콜) 추가하기

서버와 손님이 새로운 주제로 대화하고 싶을 때는 여기서 이름을 먼저 만들어줘야 해.

### 1단계: 이름표 찾기
`TeruTeruServer.SDK/Enums/ProtocolSelect.cs` 파일을 열어봐.

### 2단계: 코드 수정하기
파일의 적당한 위치(대략 50번째 줄 근처)에 아래처럼 코드를 추가해봐!

```csharp
// [기존 코드들...]
LoginProtocol = 1,
ConnectProtocol = 2,
// 여기에 추가! ✨
GreetingMessage = 100, 
```

### 3단계: 무엇이 변했나요?
이제 서버와 클라이언트 모두 `GreetingMessage`라는 이름을 쓸 수 있게 됐어. 이건 마치 우리 놀이공원 사전에 새로운 단어를 하나 등록한 것과 같아!

---

## ✨ 멘토의 미션: "나만의 인사말 만들기"

우리 놀이공원에 오는 손님들에게 특별한 인사를 건네고 싶지 않니?

1.  `ProtocolSelect.cs` 파일에 `WelcomeMessage = 2026,`을 추가해봐.
2.  그리고 `TeruTeruServer.SDK/Protocol/` 폴더 안에 `WelcomeProtocol.cs`라는 파일을 만들어서 아래 내용을 적어봐.

```csharp
namespace TeruTeruServer.SDK.Protocol
{
    public class WelcomeProtocol
    {
        public string Message { get; set; } = "우리 놀이공원에 온 걸 환영해! ✨";
    }
}
```

성공했다면 멘토에게 자랑해줘! 너는 이제 우리 놀이공원의 '규칙 설계자'가 된 거야! 🎩✨
