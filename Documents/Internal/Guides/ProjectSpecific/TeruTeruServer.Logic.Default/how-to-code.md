# 🛠️ 놀이기구 마법 부리기 (코드 수정법)

안녕하세요! 이곳에서는 게임의 진짜 규칙을 정하는 `LogicPlugin.cs` 파일을 열어서 직접 코드를 수정해 볼 거예요. 
우리 놀이공원에 손님이 찾아왔을 때, 관리자 아저씨의 화면에 예쁜 알림이 뜨도록 만들어 볼까요? ✨

## 🧐 가장 중요한 코드 읽어보기

운영 매뉴얼인 `TeruTeruServer.Logic.Default/LogicPlugin.cs` 파일을 열어보세요.

### 1. 쪽지 확인하는 곳 (`HandleJsonProtocol` 함수)
파일의 **58번째 줄** 쯤에 이렇게 생긴 코드가 있어요.
```csharp
private void HandleJsonProtocol(string json, ProtocolSelect protocol, Socket socket)
{
    switch (protocol)
    {
        case ProtocolSelect.ConnectProtocol:
            ConProtocol(socket, JsonSerializer.Deserialize<ConnectProtocol>(json));
            break;
        case ProtocolSelect.LoginProtocol:
            HandleLogin(socket, JsonSerializer.Deserialize<LoginProtocol>(json));
            break;
    }
}
```
여기는 관제탑에서 넘겨준 쪽지(패킷)의 '이름표(protocol)'를 보고, **"아! 이건 연결해달라는 쪽지구나!", "이건 로그인해달라는 쪽지구나!"** 하고 구분해서 알맞은 담당자에게 넘겨주는 곳이에요.

### 2. 로그인 처리하는 곳 (`HandleLogin` 함수)
파일의 **71번째 줄** 쯤에는 `HandleLogin` 함수가 있어요.
여기는 손님이 로그인 쪽지를 보냈을 때, 안전한 놀이공원 자유이용권(Token)을 발급해서 다시 돌려주는 중요한 곳이랍니다!

---

## 🪄 나만의 마법 부리기 (따라하기)

손님이 로그인을 시도할 때마다, 운영자 화면에 "손님이 로그인했어요!" 라고 예쁘게 띄워볼까요?

1. `TeruTeruServer.Logic.Default/LogicPlugin.cs` 파일을 열어주세요.
2. **71번째 줄** 쯤에 있는 `private void HandleLogin(...)` 함수 안으로 들어가 보세요.
3. 73번째 줄 `// [기존 로그인 로직]` 이라는 글자 바로 아래에, 이렇게 적어보세요!
   ```csharp
   Console.WriteLine($"🎉 우와! {loginData.UserId} 손님이 놀이공원에 놀러왔어요! 🎉");
   ```
4. 저장하고 서버를 켜보면, 누군가 로그인을 할 때마다 화면에 폭죽이 터지며 환영 인사가 뜰 거예요! 🎇

---

## ⚠️ 주의사항

이 파일 아래쪽에 있는 `GenerateJwtToken` 함수(**105번째 줄**)는 손님들에게 발급해주는 **'위조 방지 자유이용권(Token)'**을 만드는 아주 복잡하고 중요한 기계예요! 
여기에 있는 비밀 키(`SecretKey`)나 발급 규칙을 잘못 만지면 모든 손님들이 놀이기구를 탈 수 없게 쫓겨나 버리니, 이 부분은 조심해서 눈으로만 구경해 주세요! 🤫
