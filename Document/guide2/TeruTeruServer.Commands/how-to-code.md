# 🛠️ 새로운 마법 주문 만들기 (코드 수정법)

안녕하세요! 이곳에서는 까만 터미널 화면에 칠 수 있는 새로운 마법 주문을 직접 만들어 볼 거예요. 
터미널에 `hello`라고 치면 예쁘게 인사해주는 마법을 만들어 볼까요? ✨

## 🧐 가장 중요한 코드 읽어보기

놀이공원을 끄는 가장 기본적이고 무시무시한 마법, `TeruTeruServer.Commands/ExitCommand.cs` 파일을 열어보세요.

```csharp
public class ExitCommand : ICommand
{
    public bool Execute(string[] args)
    {
        return false; // 프로그램 종료
    }
}
```
엄청 간단하죠?
`ICommand`라는 기본 양식을 복사해서 가져온 다음, `Execute`라는 곳 안에 **"이 주문을 외우면 어떤 일이 일어날까?"** 를 적어주면 끝이에요!
여기서는 `false`를 돌려줘서 "이제 놀이공원 문 닫아!" 라고 알려주고 있네요.

---

## 🪄 나만의 마법 부리기 (따라하기)

인사를 해주는 `HelloCommand`를 한 번 상상해 볼까요? (직접 파일을 만들어봐도 좋아요!)

1. `TeruTeruServer.Commands` 폴더 안에 `HelloCommand.cs`라는 파일을 새로 만듭니다.
2. 위에서 본 `ExitCommand`처럼 똑같이 코드를 적어주세요.
3. 그리고 `Execute` 안쪽의 내용을 이렇게 바꿔볼게요!
   ```csharp
   public class HelloCommand : ICommand
   {
       public bool Execute(string[] args)
       {
           Console.WriteLine("💖 안녕하세요! 관리자님, 오늘도 화이팅! 💖");
           return true; // 놀이공원 계속 운영!
       }
   }
   ```
4. 그리고 마법 주문서 관리자인 `CommandHandler.cs` 파일을 열어서 **"저기요! 이제 'hello'라고 치면 HelloCommand 마법이 나가게 해주세요!"** 라고 등록만 해주면 완성이에요! 

---

## ⚠️ 주의사항

명령어(주문 이름)를 등록할 때는 꼭 짧고 외우기 쉬운 영단어로 만들어주세요. 
만약 주문 이름을 `please_open_the_door_very_gently` 처럼 길게 만들면... 나중에 바쁜 관리자 아저씨가 타자 치다가 손가락에 쥐가 날지도 몰라요! 🐭
