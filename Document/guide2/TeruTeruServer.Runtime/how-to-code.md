# 🛠️ 관제탑에서 나만의 마법 부리기 (코드 수정법)

안녕하세요! 여기서는 관제탑의 메인 컴퓨터(`MainServer.cs`)를 열어서 직접 코드를 읽어보고 수정해 볼 거예요. 겁먹지 마세요, 제가 옆에서 도와줄게요! ✨

## 🧐 가장 중요한 코드 읽어보기

관제탑의 메인 컴퓨터 역할을 하는 파일은 `TeruTeruServer.Runtime/MainServer.cs` 예요. 이 파일을 열어서 가장 중요한 두 곳을 살펴볼게요.

### 1. 보안 검색대 설치하기 (`Initialize` 함수)
파일의 **77번째 줄** 쯤을 보면 `private void Initialize(...)` 라는 부분이 있어요.
```csharp
// 파이프라인 초기화 및 미들웨어 등록
_pipeline = new PacketPipeline();
_pipeline.Use(new ValidationMiddleware());
_pipeline.Use(new DecryptionMiddleware());
...
```
이곳은 손님들이 보낸 쪽지를 검사하는 **'보안 검색대(파이프라인)'**를 설치하는 곳이에요. "쪽지 형식이 맞는지 검사해(Validation)!", "암호를 풀어(Decryption)!" 하고 순서대로 규칙을 정해주고 있죠.

### 2. 관제탑 전원 켜기 (`TcpServerStart` 함수)
파일의 **117번째 줄** 쯤에는 `private void TcpServerStart()` 라는 부분이 있어요.
```csharp
Console.WriteLine("Server Start");
Console.WriteLine("Server Version : 0.00.2");
```
이곳은 관제탑의 전원을 켜고, 운영자 아저씨의 까만 화면(Console)에 "서버 켜졌습니다!" 하고 알려주는 부분이에요.

---

## 🪄 나만의 마법 부리기 (따라하기)

자, 이제 우리가 직접 마법(코드 수정)을 부려볼 시간이에요! 
관제탑이 켜질 때 나오는 딱딱한 인사말을 우리만의 귀여운 인사말로 바꿔볼까요?

1. `TeruTeruServer.Runtime/MainServer.cs` 파일을 열어주세요.
2. **123번째 줄** 근처로 가보세요.
   ```csharp
   Console.WriteLine("Server Start");
   ```
   이 코드가 보일 거예요.
3. 이 문장을 아래처럼 지우고, 여러분이 원하는 환영 인사로 바꿔보세요!
   ```csharp
   Console.WriteLine("💖 우리들의 환상적인 놀이공원 오픈! 어서오세요! 💖");
   ```
4. 저장하고 서버를 다시 켜보면? 까만 화면에 여러분이 적은 예쁜 인사말이 뜰 거예요! 🎉

---

## ⚠️ 주의사항

관제탑은 놀이공원 전체를 관리하는 아주 중요한 곳이에요! 
특히 `StartAcceptLoop`나 `HandleAcceptedSocket` 같은 이름이 붙은 곳들은 손님들을 맞이하는 **'입구 회전문'**과 같아서, 이곳의 코드를 잘못 만지면 회전문이 고장 나서 손님들이 아예 들어올 수 없게 되니 조심조심 눈으로만 구경해 주세요! 😉
