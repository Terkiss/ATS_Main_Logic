# 프로젝트 구현 진행 상황 (Implementation Progress)

## 현재 마일스톤: Milestone 8 — Lag Compensation & Prediction

- [x] **1. 서버 권위 모델 확립**
  - [x] `ServerAuthorityValidator` 클래스
  - [x] 입력 검증 → 상태 적용 파이프라인

- [x] **2. 클라이언트 사이드 예측(CSP) 지원**
  - [x] `AckSequence` 기반 응답 프로토콜
  - [x] `StateAckProtocol` 프로토콜 추가
  - [x] Reconciliation 데이터 구조

- [x] **3. Lag Compensation (히트박스 되감기)**
  - [x] `LagCompensator` 클래스
  - [x] SnapshotBuffer 과거 상태 조회 기반 판정
  - [x] HitValidation 결과 모델

- [x] **4. Entity Interpolation 가이드**
  - [x] `Documents/Technical/Interpolation_Guide.md` 작성

- [x] **5. RTT 측정 강화**
  - [x] `RttTracker` 유틸리티
  - [x] `RttPingProtocol` 프로토콜 추가
  - [x] ClientSession.RttMs Rolling Average 갱신

## 남은 리스크 및 이슈
- SnapshotBuffer 128프레임 용량이 RTT 200ms+ 환경에서 충분한지 검증 필요
- LagCompensator의 되감기 범위 제한 정책 필요 (무한 되감기 방지)
- ClientSession.RttMs 기존 필드 활용 시 기존 P2PPing 로직과 충돌 여부 확인
