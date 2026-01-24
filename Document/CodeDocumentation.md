# 코드 문서 (Code Documentation)

## 목차
- [MainServer](#mainserver)
- [ServerLogic](#serverlogic)
- [RpcStub](#rpcstub)
- [RpcProxy](#rpcproxy)
- [Protocol 구조체 (`PlayerData` 등)](#protocol-구조체)
- [ServerMemory](#servermemory)
- [TeruTeruLogger](#teruterulogger)
- [ClientSession](#clientsession)
- [ConfigManager](#configmanager)
- [ServerConnectConfigParameter](#serverconnectconfigparameter)
- [WorkerStartCommand](#workerstartcommand)
- [Utility](#utility)
- [Encrypt](#encrypt)
- [MarshalUtil](#marshalutil)

---

## MainServer

**파일:** `iocp/MainServer.cs`

### 역할
- TCP/UDP 소켓을 열고 클라이언트 연결을 관리합니다.
- 수신된 데이터를 `ServerLogic` 혹은 `RpcStub`에 전달하여 프로토콜을 처리합니다.

### 주요 멤버
| 멤버 | 타입 | 설명 |
|------|------|------|
| `_port` | `int` | 서버가 바인딩할 포트 번호 |
| `_isTcp` / `_isUdp` | `bool` | 프로토콜 선택 |
| `players` | `Dictionary<int, Socket>` | 현재 연결된 클라이언트 소켓 맵 |
| `_udpSessions` | `ConcurrentDictionary<EndPoint, Socket>` | UDP 클라이언트 Endpoint별 연결된 소켓 관리 |
| `SendData(Socket, byte[])` | 메서드 | 지정된 소켓에 데이터를 전송 |
| `StartServer()` | 메서드 | 프로토콜 방식에 따라 TCP 혹은 UDP 서버 시작 |
| `UdpServerStart()` | 메서드 | UDP 리스너를 실행하고 "Connected UDP" 소켓으로 클라이언트별 세션 관리 |
| `ProcessDirectProtocol(byte[], Socket)` | 메서드 | `RpcStub`을 통해 직접 프로토콜 처리 |
| `ProcessJsonProtocol(string, ProtocolSelect, Socket)` | 메서드 | JSON 기반 프로토콜 라우팅 |

### 사용 예시
```csharp
var config = configManager.GetServerConfig();
var mainServer = new MainServer(config);
mainServer.StartServer();
```

---

## ServerLogic

**파일:** `iocp/ManageLogic/ServerLogic.cs`

### 역할
- `MainServer` 로부터 전달받은 바이트 데이터를 `RpcStub`에 위임하고, 응답을 클라이언트에 전송합니다.
- `ConnectProtocol` 처리 로직을 포함합니다 (호스트 ID 할당, GUID 검증 등).

### 주요 메서드
| 메서드 | 설명 |
|------|------|
| `ProcessDirectProtocol(byte[] buffer, Socket socket)` | `RpcStub.HandleRequest` 호출 후 결과 전송 |
| `ProcessJsonProtocol(string json, ProtocolSelect protocolSelect, Socket socket)` | `protocolSelect`에 따라 `ConnectProtocol` 등 처리 |
| `ConProtocol(Socket socket, ConnectProtocol protocol)` | 연결 요청 검증 및 클라이언트 등록 |

---

## RpcStub

**파일:** `iocp/ManageLogic/Protocol/RpcStub.cs`

### 역할
- 서버가 수신한 요청을 실제 비즈니스 로직으로 라우팅합니다.
- 현재는 `HandleRequest` 메서드 하나만 제공하지만, 향후 RPC 메서드가 추가될 수 있습니다.

### 주요 메서드
| 메서드 | 설명 |
|------|------|
| `HandleRequest(Socket socket, byte[] request)` | 요청을 파싱하고 적절한 응답 바이트 배열 반환 |

---

## RpcProxy

**파일:** `iocp/ManageLogic/Protocol/RpcProxy.cs`

### 역할
- 서버 내부에서 다른 클라이언트(예: Detector)에게 RPC 요청을 전송합니다.
- `RequestObjectDetect` 메서드가 현재 구현된 주요 기능입니다.

### 주요 메서드
| 메서드 | 설명 |
|------|------|
| `RequestObjectDetect(SendImageData sendImageData)` | 이미지 데이터를 `Detect` 클라이언트에게 전달하고, 결과를 대기 큐에 넣음 |

---

## Protocol 구조체 (`PlayerData` 등)

**파일:** `iocp/ManageLogic/Protocol/PlayerData.cs`

### 주요 구조체
- `ConnectionData` – 서버 연결용 GUID 보관
- `SendData` – 일반 데이터 전송 (인덱스 + 바이트 배열)
- `SendImageData` – 이미지 전송 (HostID, UserID, ImgSize, Data)
- `YoloDetectResult` – 객체 탐지 결과 (HostID, UserID, Data, DetectionResult)
- `ChatData` – 채팅 메시지 (인덱스, Sender, Message)
- `PlayerData` – 플레이어 상태 (위치, 회전, 애니메이션, 스킨 등)

각 구조체는 `[StructLayout(LayoutKind.Sequential, Pack = 1)]` 로 직렬화가 가능하도록 정의되어 있습니다.

---

## ServerMemory

**파일:** `iocp/ManageLogic/Util/ServerMemory.cs`

### 역할
- 전역 메모리 저장소 역할 (클라이언트 세션, 게임‑ID 매핑, 이미지 작업 큐 등).
- 스레드‑안전하게 `ConcurrentQueue`와 `lock`을 사용합니다.

### 주요 멤버
| 멤버 | 타입 | 설명 |
|------|------|------|
| `MainServer` | `MainServer` | 현재 실행 중인 서버 인스턴스 |
| `_hosts` | `Dictionary<int, ClientSession>` | HostID ↔ `ClientSession` 매핑 |
| `_gameID2HostID` | `Dictionary<string, int>` | GameID ↔ HostID 매핑 |
| `_imageWorkPreOrderQueue` | `ConcurrentQueue<SendImageData>` | 이미지 분석 요청 대기 큐 |
| `_imageWorkCompleteQueue` | `ConcurrentQueue<YoloDetectResult>` | 분석 완료 결과 큐 |
| `GetHostID` | 프로퍼티 | 새로운 HostID 생성 (자동 증가) |
| `AddHostToDictionary`, `RemoveHostFromDictionary` | 메서드 | Host 관리 |
| `AddGameIDToDictionary`, `RemoveGameIDFromDictionary` | 메서드 | GameID ↔ HostID 매핑 관리 |
| `GetImageWork_PreOrder_Queue`, `GetImageWork_Complete_Queue` | 메서드 | 큐에서 항목 꺼내기 |

---

## TeruTeruLogger

**파일:** `iocp/ManageLogic/Util/TeruTeruLogger.cs`

### 역할
- 콘솔 색상을 이용해 로그 레벨별 출력 및 파일 로깅을 담당합니다.
- `LogInfo`, `LogAttention`, `LogError`, `LogWarning`, `LogInvisible` 메서드 제공.

### 주요 메서드
| 메서드 | 레벨 |
|------|------|
| `LogInfo` | INFO (녹색) |
| `LogAttention` | ATTENTION (보라색) |
| `LogError` | ERROR (빨강) |
| `LogWarning` | WARNING (노랑) |
| `LogInvisible` | HARDWARE (별도 파일) |

---

## ClientSession

**파일:** `iocp/ManageLogic/Util/ClientSession.cs`

### 역할
- 하나의 클라이언트를 나타내는 객체로, HostID, GameID, Socket 등을 보관합니다.
- `Serialize<T>` 메서드로 구조체를 바이트 배열로 변환합니다.

### 주요 프로퍼티
| 프로퍼티 | 타입 | 설명 |
|----------|------|------|
| `HostID` | `int` | 클라이언트 고유 ID |
| `GameID` | `string` | 게임 세션 ID |
| `ClientSocket` | `Socket` | 연결된 소켓 |
| `Role` | `string` | 클라이언트 역할 (예: Detector) |
| `ClientName` | `string` | 클라이언트 이름 |

---

## ConfigManager

**파일:** `iocp/ConfigManager.cs`

### 역할
- `config.txt` 파일을 읽고, 없을 경우 사용자에게 입력받아 생성합니다.
- `ServerConnectConfigParameter` 객체를 반환하여 `MainServer` 초기화에 사용됩니다.

### 주요 메서드
| 메서드 | 설명 |
|------|------|
| `LoadConfig()` | 파일 존재 여부에 따라 `LoadFromFile` 혹은 `PromptConfigFromUser` 호출 |
| `LoadFromFile(string)` | 파일 라인 파싱 후 설정값 할당 |
| `PromptConfigFromUser()` | 콘솔 입력으로 설정값 수집 |
| `SaveConfig()` | 현재 설정을 `config.txt`에 저장 |
| `GetServerConfig()` | `ServerConnectConfigParameter` 인스턴스 반환 |

---

## ServerConnectConfigParameter

**파일:** `iocp/ServerConnectConfigParameter.cs`

### 역할
- 서버 연결에 필요한 설정값을 보관하는 POCO 클래스.
- IP, Port, MaxConnection, NetworkType, BufferSize, GUID 등을 포함합니다.

### 주요 프로퍼티
| 프로퍼티 | 타입 | 설명 |
|----------|------|------|
| `IP` | `string` | 서버 IP 주소 |
| `Port` | `int` | 포트 번호 |
| `MaxConnection` | `int` | 최대 동시 연결 수 |
| `IsTcp` / `IsUdp` | `bool` | 프로토콜 선택 |
| `SendBufferSize` / `ReceiveBufferSize` | `int` | 버퍼 크기 |
| `Guid` | `string` | 서버 고유 식별자 |

---

## WorkerStartCommand

**파일:** `iocp/Command/WorkerStartCommand.cs`

### 역할
- 백그라운드 워커 스레드를 시작하여 `ServerMemory` 큐에 쌓인 이미지 작업을 `RpcProxy.RequestObjectDetect` 로 전달합니다.
- 작업이 완료되면 `TeruTeruLogger` 로 결과를 로그합니다.

### 주요 로직
```csharp
while (true) {
    Thread.Sleep(30);
    if (ServerMemory.GetImageWork_PreOrder_Queue(out var preOrderItem))
        _rpcProxy.RequestObjectDetect(preOrderItem);
    if (ServerMemory.GetImageWork_Complete_Queue(out var completeItem)) {
        TeruTeruLogger.LogInfo($"분석 완료 유저: {completeItem.UserID}, 호스트: {completeItem.HostID}");
        TeruTeruLogger.LogInfo("탐지 결과 JSON: " + completeItem.DetectionResult);
    }
}
```

---

## Utility

**파일:** `iocp/ManageLogic/Util/Utility.cs`

### 역할
- 고유 ID 생성 (`GenerateUniqueId`) 등 공통 유틸리티 메서드 제공.
- 현재는 `RandomNumberGenerator` 를 이용해 24바이트 무작위 데이터를 기반으로 문자열을 반환합니다.

---

## Encrypt

**파일:** `iocp/ManageLogic/Util/Encrypt.cs`

### 역할
- AES‑CBC 방식으로 문자열을 암호화/복호화합니다.
- `GenerateRandomIV` 로 IV를 생성하고, PBKDF2 (`Rfc2898DeriveBytes`) 로 키를 파생합니다.

### 주요 메서드
| 메서드 | 설명 |
|------|------|
| `EncryptStringAES(string, string)` | 평문 + 비밀번호 → Base64 인코딩된 암호문 |
| `DecryptStringAES(string, string)` | 암호문 → 평문 |
| `GenerateRandomIV()` | 랜덤 IV 반환 |

---

## MarshalUtil

**파일:** `iocp/ManageLogic/Util/MarshalUtil.cs`

### 역할
- 구조체와 바이트 배열 간 직렬화/역직렬화를 담당합니다.
- `Serialize<T>(T)` 와 `Deserialize<T>(byte[])` 메서드 제공.

---

## 기타

- 모든 클래스와 메서드에는 한글 XML 주석이 포함되어 있어 IDE에서 자동 완성 시 설명을 확인할 수 있습니다.
- 로그는 `TeruTeruLogger` 를 통해 콘솔과 파일(`Logs/*.log`)에 기록됩니다.
- 네이밍 규칙: **private 필드**는 `_camelCase`, **public/프로퍼티**는 `PascalCase` 로統一되었습니다.

---

*이 문서는 프로젝트 루트의 `Document` 폴더에 위치합니다.*
