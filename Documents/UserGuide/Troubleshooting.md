**[문서 내비게이션 바]**
**[Technical]** [Architecture](../Technical/Architecture.md) | [API Reference](../Technical/API_Reference.md) | [Setup Guide](../Technical/Setup_Guide.md) | [Database Schema](../Technical/Database_Schema.md)
**[UserGuide]** [Introduction](./Introduction.md) | [Installation](./Installation.md) | [How to Use](./How_to_Use.md) | [Troubleshooting](./Troubleshooting.md)
---

# 문제 해결 FAQ (Troubleshooting) 🚑

개발을 하다 보면 언제든 빨간색 에러 메시지와 마주칠 수 있습니다. 당황하지 마세요! 자주 발생하는 증상과 원인, 그리고 해결책을 진단해 드립니다.

---

### ❓ Q1: "포트가 이미 사용 중입니다 (SocketException: Address already in use)"
**증상:** 서버를 실행했는데 곧바로 튕기면서 빨간 글씨로 포트(Port) 관련 에러가 납니다.
**원인:** 이미 이전에 켜둔 TeruTeru Server가 안 꺼졌거나, 다른 프로그램이 8080(또는 설정한 포트)을 쓰고 있기 때문입니다.
**해결책:** 
1. 작업 관리자(Windows)나 `htop`(Linux)을 열어 백그라운드에 돌아가고 있는 서버 프로세스를 강제로 종료하세요.
2. 혹은 `config.txt`를 열어 포트 번호를 8081처럼 다른 번호로 바꿔서 실행해 보세요.

---

### ❓ Q2: 플러그인을 수정하고 빌드했는데 "파일이 잠겨 있습니다 (Access Denied / File locked)" 에러가 나요.
**증상:** 핫로딩(Hot-Reload)을 위해 Logic 프로젝트를 빌드(`dotnet build`)했는데, `plugins` 폴더로 복사할 수 없다고 합니다.
**원인:** 운영체제가 파일 복사를 방해하는 권한 문제일 수 있습니다. (윈도우 환경 특유의 일시적인 잠김 현상)
**해결책:** 엔진의 `PluginManager`는 이 상황을 우회하기 위해 `.dll` 파일을 읽을 때 메모리로 복사(`File.OpenRead`)하도록 설계되어 있습니다. 잠시 1~2초 기다렸다가 다시 빌드 명령어를 치면 정상적으로 덮어씌워집니다.

---

### ❓ Q3: 클라이언트가 데이터를 보냈는데 서버가 응답하지 않고 조용해요.
**증상:** 에러도 안 나는데 로직이 실행되지 않습니다.
**원인:** (1) 보낸 JSON 데이터의 형식이 서버가 기대하는 DTO 모양과 달라서 역직렬화(Deserialize)에 실패했거나, (2) 호출하려는 `[Rpc("이름")]` 의 이름 스펠링이 틀렸을 확률이 가장 높습니다.
**해결책:** 
*   클라이언트 측 콘솔을 열어 보내는 JSON 문자열을 확인하세요.
*   서버 로그에 `[Warning] No handler found for protocol...` 메시지가 찍혀있는지 확인하세요. 철자가 맞는지 대소문자를 다시 한번 점검합니다.

---

### ❓ Q4: "TCP 서버를 시작할 때 GUID는 null일 수 없습니다"
**증상:** 예외(Exception)가 발생하며 `MainServer.cs`에서 프로그램이 멈춥니다.
**원인:** 설정 파일(`config.txt`)이 손상되었거나 서버를 고유하게 식별할 GUID 값이 빠져있습니다.
**해결책:** `TeruTeruServer.Cli` 폴더 안에 있는 `config.txt`를 과감히 삭제해버리고 서버를 다시 켜보세요. 엔진이 처음부터 다시 올바른 양식으로 세팅 파일을 만들어 줍니다!

---

### ❓ Q5: P2P 연결이 안 되고 자꾸 "Relay 모드"로만 동작해요.
**증상:** 두 클라이언트가 직접 연결되지 않고 서버를 거쳐서만 데이터가 전달됩니다.
**원인:** 한쪽 이상의 네트워크 환경이 **Symmetric NAT**(매우 강력한 방화벽)이거나, UDP 포트가 완전히 막혀있어 홀펀칭(Hole Punching)이 불가능한 상태입니다.
**해결책:** 
1. 공유기 설정(관리 페이지)에서 **UPnP** 기능이 켜져 있는지 확인하세요.
2. 서버는 이 상황을 대비해 자동으로 릴레이로 전환하므로 기능상 문제는 없으나, 지연 시간을 줄이려면 공용 Wi-Fi나 회사 방화벽 대신 일반 가정용 인터넷 환경에서 테스트하는 것을 권장합니다.