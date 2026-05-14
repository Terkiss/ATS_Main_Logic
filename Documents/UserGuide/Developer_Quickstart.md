# TeruTeru Server 개발자 퀵스타트 (Quickstart) 🚀

본 문서는 TeruTeru Server v2.0 아키텍처를 기반으로 새로운 기능을 개발하고 적용하는 방법을 빠르게 안내합니다.

## 1. 프로젝트 구조 이해
우리 프로젝트는 역할을 철저히 분리한 **4계층 구조**를 사용합니다.
- **`TeruTeruServer.SDK`**: 모든 규약과 도구가 담긴 공용 라이브러리.
- **`TeruTeruServer.Runtime`**: 소켓 I/O와 미들웨어를 처리하는 엔진 본체.
- **`TeruTeruServer.Logic.Default`**: **(핵심)** 여러분이 비즈니스 로직을 구현할 플러그인 프로젝트.
- **`TeruTeruServer.Cli`**: 서버를 실행하고 관리하는 호스트 애플리케이션.

---

## 2. 새로운 기능 개발하기 (Logic Plugin)
TeruTeru Server에서는 엔진을 건드리지 않고도 **`Logic.Default`** 프로젝트만 수정하여 기능을 추가할 수 있습니다.

### Step 1: 프로토콜 정의
`TeruTeruServer.SDK/Enums/ProtocolEnums.cs` 파일의 `ProtocolSelect` 열거형에 새로운 ID를 추가합니다.
```csharp
public enum ProtocolSelect : byte
{
    MyNewFeature = 150, // 새로운 기능 ID
}
```

### Step 2: 비즈니스 로직 핸들러 작성
`TeruTeruServer.Logic.Default` 프로젝트의 `LogicPlugin.cs` 파일에 `[Protocol]` 어트리뷰트를 사용해 메서드를 선언합니다.

```csharp
[Protocol(ProtocolSelect.MyNewFeature)]
public void HandleMyFeature(PacketContext context)
{
    // 1. 데이터 추출 (6바이트 헤더 자동 제외)
    string json = context.RawData.ExtractJsonPayload();
    var request = JsonSerializer.Deserialize<MyRequestDto>(json);

    // 2. 로직 처리
    Console.WriteLine($"신규 요청 수신: {request.Message}");

    // 3. 응답 전송
    var response = new MyResponseDto { Result = "Success" };
    context.SendJsonResponse(ProtocolSelect.MyNewFeature, response);
}
```

---

## 3. 마법의 핫로딩(Hot-Reloading) 적용 ✨
서버가 실행 중인 상태에서 코드를 반영해 봅시다.

1. **서버 실행** (Cli 프로젝트)
   ```bash
   dotnet run --project TeruTeruServer.Cli
   ```
2. **로직 수정 및 빌드**
   `Logic.Default`에서 코드를 수정한 후, **해당 프로젝트만** 다시 빌드합니다.
   ```bash
   dotnet build TeruTeruServer.Logic.Default
   ```
3. **결과 확인**
   서버 콘솔에 `[PluginManager] Logic plugin hot-reloaded successfully.` 로그가 뜨면 성공! 서버 재시작 없이 즉시 기능이 업데이트됩니다.

---

## 4. 클라이언트에서 호출하기 (Client SDK)
`TeruTeruServer.Client` SDK를 사용하면 통신이 매우 간단해집니다.

```csharp
using var client = new TeruClient("127.0.0.1", 8080);
await client.ConnectAsync();

// RPC 스타일로 간편하게 호출
await client.InvokeRpcAsync("MyNewFeature", new { Message = "Hello World!" });
```

---

## 5. 핵심 개발 규칙
- **6바이트 헤더**: 모든 패킷은 `[Type(1)][Protocol(1)][Seq(4)]` 헤더를 가집니다. 직접 패킷을 구성할 때는 `PacketUtility`를 활용하세요.
- **비동기 처리**: 네트워크와 DB 작업은 반드시 `async/await`를 사용하여 서버의 성능을 유지하세요.
- **보안**: 민감한 데이터는 `AuthMiddleware`를 통해 JWT 검증을 거치도록 설계하세요.

👉 더 자세한 내용은 [Architecture 가이드](../Technical/Architecture.md)를 참고하세요!
