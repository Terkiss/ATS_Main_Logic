# M13 (Playground Cleanup & Merge) — 작업자 지시문 (Worker Directive)

## 목표
`playground` 브랜치에 산재되어 있는 28개의 미커밋 변경(코드 경고 제거, nullable reference type 어노테이션, 클라이언트 가이드 문서 등)을 정리하고, `.DS_Store` 등 불필요한 OS 생성 파일들을 배제한 뒤 `master` 브랜치에 안전하게 병합한다.

---

## 1. 선행 작업 및 환경 확인
1. 현재 `playground` 브랜치에 있는지 확인한다.
   ```bash
   git branch
   ```
2. 작업 환경의 dotnet 경로를 설정하고 릴리즈 게이트가 성공적으로 통과하는지 사전 검증한다.
   ```bash
   export PATH="$PATH:/usr/local/share/dotnet:$HOME/.dotnet"
   bash ./scripts/verify-release.sh
   ```

---

## 2. 세부 작업 지시 사항

### 작업 1: .DS_Store 무시 설정
- `.gitignore` 파일 최하단에 아래와 같이 `.DS_Store` 관련 무시 패턴을 추가한다.
  ```gitignore
  # macOS system files
  .DS_Store
  **/.DS_Store
  ```
- 추가 후 `git status`를 확인하여 untracked 리스트에서 `.DS_Store` 파일들이 전부 사라졌는지 검증한다.

### 작업 2: 신규 및 수정 문서 Staging
- 아래 신규 작성된 문서 파일만 명시적으로 스테이징한다.
  - `Documents/my/외부프로젝트_클라이언트_연동_가이드.md`
  - `Documents/my/다음작업_선택지.md`
  - `Documents/Internal/M13_Worker_Directive.md` (본 지시문)

### 작업 3: 코드 리팩터링 및 경고 제거 변경분 Staging
- `LogicPlugin.cs`, `Utility.cs` 등 SDK 및 Runtime 내에 진행된 코드 리팩터링 변경점을 스테이징한다.
- 불필요한 dll 파일이나 컴파일 산출물이 스테이징되지 않도록 주의한다. (예: `TeruTeruServer.Logic.Default.dll` 등은 빌드 결과물이므로 커밋 대상에서 제외하거나 필요시 함께 빌드된 상태로만 유지한다.)

---

## 3. 검증 절차
1. 모든 대상 파일을 스테이징한 상태에서 다시 한 번 릴리즈 게이트를 돌려 빌드 및 테스트(51개 전체)가 완벽히 통과하는지 확인한다.
   ```bash
   bash ./scripts/verify-release.sh
   ```
2. 통과 완료 보고서(테스트 개수, 스테이징 상태 등)를 작성하고 1차 검증관에게 인계한다.
   - **주의**: 직접 커밋하거나 푸시하지 않고, 1차 검증관의 검증 및 2차 최종 승인을 대기한다.

---

## 변경 허용 범위
- `Documents/my/` 하위 신규 문서
- `.gitignore` 수정
- TeruTeruServer.SDK, Runtime, Logic.Default 내의 경고 해결용 리팩터링 파일
- 컴파일 산출물 dll

## 금지 사항
- 공식 검증 전 `Completed` 선언 금지
- 강제 커밋/푸시 (`git commit`, `git push` 명령어 실행 금지 - 1차 검증관에게 인계)
- `.agents/` 디렉터리 내의 임의 수정 금지
