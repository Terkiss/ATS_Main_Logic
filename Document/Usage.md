# 프로젝트 사용 설명서

## 프로젝트 개요
이 프로젝트는 **Kabusiki_Main_Server** 라는 이름의 서버 엔진으로, 사용자 정의 RPC (Remote Procedure Call) 방식을 사용합니다. 서버는 TCP/UDP 기반으로 클라이언트와 통신하며, 이미지 분석, 채팅, 플레이어 데이터 전송 등 다양한 프로토콜을 지원합니다.

## 빌드 및 실행
1. **빌드**
   ```bash
   # 프로젝트 루트 디렉터리에서
   dotnet build
   ```
2. **실행**
   ```bash
   dotnet run --project iocp/Program.cs
   ```
   - `config.txt` 파일이 존재하지 않을 경우, 콘솔에서 포트, 최대 연결 수, UDP/TCP 선택 등을 입력받아 자동으로 생성합니다.

## 설정 파일 (`config.txt`)
- **포트** (`port`): 서버가 바인딩할 포트 번호
- **최대 연결 수** (`max_connection`): 동시에 허용할 클라이언트 수
- **프로토콜 선택** (`isUdp` / `isTcp`): UDP 혹은 TCP 사용 여부 (동시에 true 로 설정할 수 없습니다.)
- **버퍼 크기** (`SendMassageSize`, `ReceiveMassageSize`): 전송/수신 버퍼 크기 (기본 4096)
- **GUID** (`Guid`): 서버 고유 식별자

## 명령어 사용 (`CommandHandler`)
서버가 시작된 후 콘솔에서 다음과 같은 명령어를 입력할 수 있습니다.
- `exit` : 서버 종료
- `Queue_Count` : 현재 이미지 작업 큐의 개수 확인
- `2` : 이미지 덤프 명령 (예시)
- `Worker_Start` : 백그라운드 워커 스레드 시작 (이미지 분석 요청 처리)

## 사용자 정의 RPC 선언 및 사용법
### 1. RPC 메서드 선언
`RpcStub` 클래스에 새로운 RPC 메서드를 추가합니다.
```csharp
public class RpcStub {
    // 기존 메서드 예시
    public byte[] HandleRequest(Socket socket, byte[] request) { ... }

    // 새로운 RPC 메서드 선언 예시
    public byte[] MyCustomRpc(Socket socket, MyRequestDto request) {
        // 로직 구현
    }
}
```
- **매개변수**: `Socket` 객체와 요청 데이터를 담은 DTO(데이터 전송 객체) 를 받습니다.
- **반환값**: 클라이언트에 전송할 `byte[]` 형태의 응답 데이터를 반환합니다.

### 2. 프로토콜 클래스에 매핑
`ProtocolSelect` 열거형에 새로운 프로토콜 ID를 추가하고, `MainServer.ProcessJsonProtocol` 혹은 `ServerLogic` 에서 해당 ID를 처리하도록 매핑합니다.
```csharp
public enum ProtocolSelect {
    ConnectProtocol = 0,
    LoginProtocol = 1,
    // 새 프로토콜 추가
    MyCustomProtocol = 2,
}
```
```csharp
switch (protocolSelect) {
    case ProtocolSelect.ConnectProtocol:
        // 기존 로직
        break;
    case ProtocolSelect.MyCustomProtocol:
        // 새로운 RPC 호출
        var request = JsonSerializer.Deserialize<MyRequestDto>(json);
        var response = rpcStub.MyCustomRpc(socket, request);
        // 응답 전송
        break;
}
```
### 3. 클라이언트 측 호출 예시
```csharp
// 요청 객체 생성
var request = new MyRequestDto { /* 필드 설정 */ };
var json = JsonSerializer.Serialize(request);
byte[] data = Encoding.UTF8.GetBytes(json);
// 프로토콜 타입 및 전송 타입 지정
byte sendType = (byte)SendType.Json;
byte protocolType = (byte)ProtocolSelect.MyCustomProtocol;
byte[] packet = new byte[data.Length + 2];
packet[0] = sendType;
packet[1] = protocolType;
Array.Copy(data, 0, packet, 2, data.Length);
// 소켓을 통해 전송
socket.Send(packet);
```

## 기타 참고 사항
- 모든 클래스와 메서드에는 한글 XML 주석이 포함되어 있어 IDE에서 툴팁으로 확인할 수 있습니다.
- 로그는 `TeruTeruLogger` 를 통해 콘솔 및 파일에 기록됩니다.
- 프로젝트 전반에 걸쳐 네이밍 규칙은 **private 필드**는 `_camelCase`, **public/프로퍼티**는 `PascalCase` 로統一되었습니다.

---
*이 문서는 프로젝트 루트의 `Document` 폴더에 위치합니다.*
