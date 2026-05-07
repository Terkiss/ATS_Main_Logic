# 📋 TeruTeru Server AI Engine v2.0 프로젝트 요약 보고서

이 보고서는 **TeruTeru Server** 프로젝트의 현재 아키텍처, 기술 스택, 주요 기능 및 개발 현황을 요약하여 PRD 생성 AI를 위한 기초 자료로 활용하기 위해 작성되었습니다.

---

## 1. 프로젝트 개요 (Project Overview)
- **프로젝트명:** TeruTeru Server AI Engine (v2.0)
- **성격:** 고성능 비동기 IO(IOCP) 기반 C# 서버 엔진 및 AI(YOLO) 호스팅 플랫폼
- **핵심 목표:** 고성능 네트워크 통신과 실시간 AI 분석 로직의 결합, 핫로딩을 통한 운영 유연성 확보

---

## 2. 핵심 아키텍처 (Core Architecture)
Phase 2 현대화 작업을 통해 **4계층 레이어드 아키텍처**로 재설계되었습니다.

| 레이어 | 프로젝트명 | 역할 및 특징 |
| :--- | :--- | :--- |
| **SDK** | `TeruTeruServer.SDK` | 공용 인터페이스, 프로토콜 구조체, AI 유틸리티, 데이터 모델 정의 |
| **Runtime** | `TeruTeruServer.Runtime` | IOCP 소켓 엔진, 미들웨어 파이프라인, 플러그인 매니저, 세션 관리 |
| **Commands** | `TeruTeruServer.Commands` | 서버 제어 및 모니터링을 위한 모듈화된 명령어 처리기 |
| **Cli** | `TeruTeruServer.Cli` | 엔진 호스팅, 인터랙티브 CUI 루프, 실시간 로그 출력 진입점 |
| **Logic** | `Logic.Default` | **비즈니스 로직 플러그인.** 서버 재시작 없이 핫로딩(Hot-Reloading) 가능 |

---

## 3. 주요 기술 스택 (Technical Stack)
- **Language/Runtime:** C# / .NET 8.0
- **Networking:** System.Net.Sockets (Async IO / IOCP 기반)
- **AI/CV:** OpenCvSharp4, TorchSharp (PyTorch for .NET), Microsoft.ML
- **Security:** JWT (JSON Web Token) 기반 인증, AES-CBC 암호화 파이프라인
- **Pattern:** Dependency Injection (Microsoft.Extensions.DependencyInjection), Middleware Pipeline, Plugin Architecture

---

## 4. 주요 기능 및 특징 (Key Features)
1. **미들웨어 파이프라인:** 패킷 수신 시 `복호화 -> 유효성 검증 -> JWT 인증 -> 로직 라우팅` 과정을 거치는 유연한 처리 구조.
2. **AI 호스팅 플랫폼:** 서버 엔진 내에 AI 프레임워크가 통합되어 있어, `SendImageData` 프로토콜을 통한 실시간 객체 탐지(YOLO) 및 결과 공유 가능.
3. **플러그인 핫로딩:** `AssemblyLoadContext`를 활용하여 서버 가동 중에도 비즈니스 로직(DLL)을 즉시 교체 및 반영.
4. **RPC 시스템 (진행 중):** Stub/Proxy 패턴을 통한 서버-클라이언트 간 원격 프로시저 호출 규약.
5. **보안 강화:** SQL Parameterized Query 적용으로 SQL Injection 방어, JWT 세션 관리로 보안성 확보.

---

## 5. 현재 개발 단계 (Current Status)
- **완료 (Phase 2):** 아키텍처 현대화, DI 컨테이너 도입, 미들웨어 파이프라인 및 JWT 인증 시스템 구축 완료.
- **진행 중 (Phase 3):** 
    - RPC 시스템(Stub/Proxy) 구체화 및 실구현.
    - YOLO 분석 결과 브로드캐스팅 로직 최적화.
    - 관리자 모니터링 도구 고도화.
    - 대규모 접속 부하 테스트 및 예외 처리 강화.

---

## 6. 향후 확장 계획 (Roadmap)
- **Phase 4:** Redis 등 외부 상태 저장소 도입(Scale-out 준비), Docker 컨테이너화 및 CI/CD 파이프라인 구축.

---
*작성일: 2026-04-14*
*작성자: Gemini CLI (feature/phase2-architecture)*
