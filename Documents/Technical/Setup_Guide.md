**[문서 내비게이션 바]**
**[Technical]** [Architecture](./Architecture.md) | [API Reference](./API_Reference.md) | [Setup Guide](./Setup_Guide.md) | [Database Schema](./Database_Schema.md)
**[UserGuide]** [Introduction](../UserGuide/Introduction.md) | [Installation](../UserGuide/Installation.md) | [How to Use](../UserGuide/How_to_Use.md) | [Troubleshooting](../UserGuide/Troubleshooting.md)
---

# 서버 구축 및 배포 가이드 (Setup Guide)

본 문서는 TeruTeru Server의 로컬 개발 환경 구축 및 빌드/실행 절차를 안내합니다.

## 1. 사전 요구 사항 (Prerequisites)
*   **OS:** Windows 10/11, Linux (Ubuntu 22.04+ 권장), macOS 13+
*   **Runtime:** `.NET 9.0 SDK` (필수)
*   **Database:** MySQL 8.0+ (세션 스토어용), Redis (분산 세션 관리용, M12 옵션)

## 2. 빌드 스크립트 (Build Process)
프로젝트는 솔루션 단위 빌드를 권장하며, 의존성 관계에 따라 자동으로 SDK -> Runtime -> Plugin 순서로 빌드됩니다.

```bash
# 전체 솔루션 복원 및 빌드
dotnet build iocp.sln
```
*💡 주의: `Logic.Default.csproj` 내에 PostBuild 타겟이 설정되어 있어, 빌드 성공 시 `.dll` 파일이 자동으로 `TeruTeruServer.Cli/plugins` 디렉토리로 복사됩니다.*

## 3. 서버 설정 (`config.txt`)
`TeruTeruServer.Cli` 프로젝트가 실행되는 루트 디렉토리(또는 `bin/Debug/...`)에 `config.txt` 파일이 위치해야 합니다. 파일이 없으면 실행 시 콘솔에서 입력을 받아 자동으로 생성합니다.

**config.txt 포맷 예시:**
```text
port=8080
max_connection=1000
isUdp=false
isTcp=true
SendMassageSize=4096
ReceiveMassageSize=4096
Guid=550e8400-e29b-41d4-a716-446655440000
```

## 4. 서버 실행 (Run)
CLI 진입점 프로젝트를 실행하여 서버를 가동합니다.

```bash
cd TeruTeruServer.Cli
dotnet run
```

### 실행 후 확인 사항
1. `=== TeruTeruServer AI Engine Runtime Started ===` 로그 확인.
2. `[PluginManager] Logic plugin hot-reloaded successfully.` 로그를 통해 비즈니스 로직 플러그인이 정상적으로 메모리에 적재되었는지 확인.
3. TCP/UDP 포트 개방 및 Listening 상태 점검.