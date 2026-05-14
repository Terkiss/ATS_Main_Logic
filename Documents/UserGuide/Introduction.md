**[문서 내비게이션 바]**
**[Technical]** [Architecture](../Technical/Architecture.md) | [API Reference](../Technical/API_Reference.md) | [Setup Guide](../Technical/Setup_Guide.md) | [Database Schema](../Technical/Database_Schema.md)
**[UserGuide]** [Introduction](./Introduction.md) | [Installation](./Installation.md) | [How to Use](./How_to_Use.md) | [Troubleshooting](./Troubleshooting.md)
---

# TeruTeru Server에 오신 것을 환영합니다! 🎉

안녕하세요! 혁신적인 차세대 서버 엔진 **TeruTeru Server AI Engine (v2.0)**을 선택해 주셔서 감사합니다. 

## 🤔 TeruTeru Server는 무엇인가요?
게임을 만들거나 실시간 서비스를 개발할 때, **"수많은 사람들의 네트워크 연결을 관리하는 것"**과 **"무거운 인공지능(AI) 분석을 실시간으로 처리하는 것"**은 물과 기름처럼 섞이기 어려운 문제였습니다.

TeruTeru Server는 이 두 가지를 완벽하게 하나로 합친 마법 같은 솔루션입니다! 
매우 빠르고 가벼운 IOCP 소켓 통신을 바탕으로, 이미지 객체 탐지(YOLO)와 같은 딥러닝 로직을 서버 내부에서 끊김 없이 처리할 수 있게 도와줍니다.

## 🚀 우리가 제공하는 핵심 가치 (Core Value)

1.  **개발자는 '로직'에만 집중하세요! (마법의 통신 시스템)**
    복잡한 바이트 배열 계산? 패킷 분해? 모두 엔진이 알아서 합니다. 여러분은 그저 `[Rpc]` 라는 태그 하나만 달아주세요. 마치 내 컴퓨터에 있는 함수를 부르듯 클라이언트와 서버가 통신합니다.
2.  **서버를 끄지 않고 업데이트하세요 (Hot-Reloading)**
    라이브 서비스 중 치명적인 버그를 발견하셨나요? 서버를 내리면 유저들이 튕깁니다. TeruTeru Server는 엔진을 켠 상태로 수정된 로직 파일을 넣기만 하면 즉시 교체되는 플러그인 아키텍처를 자랑합니다.
3.  **지연 시간 제로에 도전하는 P2P (Hybrid Multicast)**
    서버의 부하를 줄이고 반응 속도를 극대화하기 위해, 유저끼리 직접 데이터를 주고받는 P2P 기술을 지원합니다. 서버는 영리하게 상황을 판단하여 직접 통신이 불가능할 때만 릴레이 역할을 수행합니다.
4.  **실시간 게임 엔진과 보안**
    매 초 수십 번씩 상태를 계산하고 동기화하는 Tick Loop와 지연 보상 로직이 탑재되어 있습니다. 모든 데이터는 JWT와 HMAC 기반의 미들웨어를 거치며 철저하게 검증됩니다.

자, 이제 이 강력하고 유연한 서버 엔진의 세계로 함께 빠져볼까요? 다음 단계인 [설치 가이드](./Installation.md)로 이동해 주세요! 🚀