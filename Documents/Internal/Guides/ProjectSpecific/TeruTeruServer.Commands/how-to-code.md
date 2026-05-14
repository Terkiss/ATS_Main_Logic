# 🛠️ 관제탑 명령어 세트 코드 수정법 (Commands)

안녕! 여기서는 우리 놀이공원을 관리하는 '새로운 버튼'을 어떻게 만드는지 배워볼 거야. ✨

## 📝 새로운 명령어(버튼) 추가하기

서버에게 "안녕?"이라고 인사하면 서버도 "반가워!"라고 대답하는 명령어를 만들어보자.

### 1단계: 새로운 버튼 클래스 만들기
`TeruTeruServer.Commands` 폴더 안에 `HelloCommand.cs` 파일을 새로 만들고 아래 내용을 적어봐.

```csharp
using System;
using TeruTeruServer.SDK.Interfaces;

namespace TeruTeruServer.Commands
{
    public class HelloCommand : ICommand
    {
        public bool Execute(string[] args)
        {
            Console.WriteLine("관제탑 마스코트: 안녕하세요! 오늘도 서버가 튼튼하네요! ✨");
            return true;
        }
    }
}
```

### 2단계: 리모컨에 등록하기
`TeruTeruServer.Commands/CommandHandler.cs` 파일을 열어서 24번째 줄 아래에 한 줄을 추가해봐.

```csharp
// 24번째 줄 아래에 추가!
_commands["hello"] = new HelloCommand();
```

### 3단계: 무엇이 변했나요?
이제 관제탑(CLI)에서 `hello`라고 입력하면 방금 만든 마스코트의 인사말이 출력될 거야!

---

## 📝 명령어에 정보 추가하기 (Health 커맨드)

서버가 사용 중인 메모리 정보를 더 알기 쉽게 고쳐볼까?

### 1단계: 검진 버튼 열기
`TeruTeruServer.Commands/HealthCommand.cs` 파일을 열어봐.

### 2단계: 코드 수정하기
29번째 줄을 보면 메모리 사용량을 출력하는 부분이 있어.

```csharp
// 29번째 줄
Console.WriteLine($"Memory Used      : {memoryUsed} MB");
// 이렇게 바꿔보자! ✨
Console.WriteLine($"밥(Memory) 먹은 양 : {memoryUsed} MB (아주 배불러요!)");
```

---

## ✨ 멘토의 미션: "퇴장 인사말 남기기"

놀이공원 문을 닫을 때 손님들에게 마지막 인사를 건네볼까?

1.  `TeruTeruServer.Commands/ExitCommand.cs` 파일을 찾아봐.
2.  `Execute` 함수 안에서 `Environment.Exit(0);` 이 실행되기 직전에 한 줄을 추가해봐.
3.  `Console.WriteLine("오늘도 즐거운 하루였어요! 내일 또 만나요! ✨👋");`

이제 서버를 끌 때마다 따뜻한 인사말을 볼 수 있을 거야!
