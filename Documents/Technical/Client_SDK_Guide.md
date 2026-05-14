# TeruTeru Client SDK 연동 가이드

본 문서는 클라이언트(Unity, MAUI, 콘솔 등)에서 `TeruTeruServer`와 손쉽게 연동하기 위해 새롭게 구축된 Vanilla Client SDK의 사용법을 설명합니다.

## 1. Zero-Config 철학과 고수준 추상화

기존의 클라이언트 구현 방식은 소켓 생성, 바이트 패킷 파싱, 콜백 매핑을 클라이언트 개발자가 직접 수동으로 구현해야 했습니다. 
개편된 SDK는 **Zero-Config** 철학을 바탕으로 이 모든 네트워크 세부 사항을 감추고, 개발자가 **어트리뷰트(`[Rpc]`) 기반의 메서드**만 선언하면 알아서 서버와 통신할 수 있도록 극도로 추상화되었습니다.

---

## 2. 연동 방법 (Integration Guide)

### 2.1 핸들러 객체(Logic Class) 작성
통신에서 사용할 비즈니스 로직 클래스를 만들고, 수신받을 메서드에 `[Rpc]` 또는 `[Protocol]` 어트리뷰트를 부여합니다.

```csharp
using TeruTeruServer.SDK.Attributes;
using TeruTeruServer.SDK.Enums;
using TeruTeruServer.SDK.Protocol;
using System;

public class MyClientLogic 
{
    // 서버가 "OnServerMessage"라는 RPC 호출을 보내면 자동으로 실행됩니다.
    [Rpc("OnServerMessage")]
    public void OnMessage(string msg) 
    {
        Console.WriteLine($"[MyClientLogic] Server Says: {msg}");
    }

    // 기존의 ProtocolSelect enum 기반 수동 매핑도 혼용 가능합니다.
    [Protocol(ProtocolSelect.QueueCountCommand)]
    public void OnQueueCountUpdate(YoloDetectResult result)
    {
        Console.WriteLine($"Detection: {result.DetectionResult}");
    }
}
```

### 2.2 연결 및 사용 시퀀스
`TeruClient`를 인스턴스화하고 위에서 만든 로직 객체를 등록(`RegisterLogic`)하기만 하면 수신 파이프라인이 완성됩니다.

```csharp
using TeruTeruServer.Client;

async Task Main()
{
    // 1. SDK 인스턴스 생성
    using var client = new TeruClient("127.0.0.1", 3000);
    
    // 2. 어트리뷰트 기반 로직 라우터 바인딩 (핵심)
    client.RegisterLogic(new MyClientLogic());

    // 3. 서버 접속
    if (await client.ConnectAsync())
    {
        // 4. 로그인 (내부적으로 JWT 발급 및 P2P 홀펀칭 활성화)
        await client.LoginAsync("my_user_id", "my_password");

        // 5. 서버에 RPC 전송
        // 서버의 "Echo" 메서드에 "Hello Server" 문자열 데이터를 파라미터로 전송합니다.
        await client.InvokeRpcAsync("Echo", "Hello Server");
    }
}
```

---

## 3. P2P 핸들링의 블랙박스화

P2P 통신(UDP 홀펀칭 및 시그널링)은 게임 및 실시간 데이터 송수신에 필수적이나 구현이 까다롭습니다. SDK는 `P2PManager`를 통해 이를 블랙박스(Black-box)로 자동 처리합니다.

- **자동 UDP 소켓 생성**: `LoginAsync`가 성공하면, 내부에서 즉시 UDP 소켓을 바인딩하고 비동기 수신 루프를 시작합니다.
- **자동 STUN (Server Binding)**: 서버로 `UdpRegisterProtocol` 패킷을 전송하여, 클라이언트의 외부 매핑 IP/Port(NAT 정보)를 서버의 메모리에 자동 등록합니다.
- **자동 홀펀칭 (Hole Punching)**: 다른 클라이언트와 P2P가 필요할 때, 서버가 `HolePunchRequest` 시그널을 보내면 SDK 내부에서 즉시 NAT 개방을 시도합니다.
- **연결 품질 모니터링 (M3)**: `P2PPingProtocol`을 통해 피어 간의 RTT 및 패킷 손실률을 실시간으로 측정하며, 품질이 낮아지면 자동으로 릴레이 모드(`RelayFallbackProtocol`)로 전환을 검토합니다.

---

## 4. 매치메이킹 및 그룹 관리 (M11)

SDK는 대규모 동시 접속 환경에서의 게임 세션 구성을 지원합니다.

- **Matchmaking**: `MatchmakingProtocol`을 통해 자신의 조건(레이팅, 지역 등)을 서버에 전달하고 대기열에 진입합니다.
- **Group Joining**: 서버가 매칭 성공 시 전달하는 `JoinGroupProtocol`을 수신하면, 내부적으로 해당 그룹에 속한 피어들과의 P2P 인프라가 자동으로 구축됩니다.
