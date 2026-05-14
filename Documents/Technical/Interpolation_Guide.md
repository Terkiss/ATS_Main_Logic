# Entity Interpolation & Prediction 가이드

이 문서는 TeruTeruServer의 실시간 패킷 구조를 활용하여 클라이언트에서 부드러운 이동을 구현하기 위한 지침을 제공합니다.

## 1. 개요 (Overview)
서버는 20Hz(50ms) 주기로 데이터를 전송합니다. 클라이언트가 이 데이터를 받는 즉시 위치를 갱신하면 프레임 간의 끊김(Stuttering)이 발생합니다. 이를 해결하기 위해 **보간(Interpolation)**을 사용합니다.

## 2. 수신 버퍼링 및 보간 (Interpolation)
클라이언트는 서버로부터 받은 스냅샷을 즉시 적용하지 않고, 최소 2개 이상의 스냅샷이 쌓일 때까지 버퍼에 보관합니다.

- **보간 공식**: `position = lerp(prevSnapshot.pos, nextSnapshot.pos, t)`
- **t (Interpolation Factor)**: `(renderTime - prevSnapshot.time) / (nextSnapshot.time - prevSnapshot.time)`
- **지연(Delay)**: 보간을 위해서는 최소 1~2틱(50~100ms) 정도의 의도적인 렌더링 지연이 필요합니다.

## 3. 외삽 (Extrapolation)
네트워크 불안정으로 인해 다음 패킷이 지연될 경우, 마지막으로 받은 속도(`Velocity`)를 기반으로 위치를 예측하여 이동시킵니다.

- **공식**: `predictedPos = lastPos + velocity * deltaTime`
- **주의**: 외삽이 너무 길어지면 실제 서버 위치와 차이가 커지므로(고무줄 현상), 일정 시간(예: 500ms) 이상 패킷이 오지 않으면 정지시키거나 보정을 준비해야 합니다.

## 4. 클라이언트 사이드 예측 (CSP)
로컬 플레이어(자신)는 서버의 응답을 기다리지 않고 입력 즉시 이동합니다.

1. 입력을 서버로 전송 (`GameInputProtocol`, `ClientTick` 포함).
2. 로컬에서 즉시 물리 적용 및 이동.
3. 서버로부터 `StateAck` 수신.
4. `LastProcessedClientTick` 확인 후, 서버가 확정한 위치와 자신의 예측 위치 비교.
5. 오차가 클 경우 서버 위치로 강제 보정 후, 이후의 입력들만 재적용 (**Reconciliation**).

## 5. Unity 예제 코드 (C#)

```csharp
void Update() {
    if (isLocalPlayer) {
        HandleCSP();
    } else {
        HandleInterpolation();
    }
}

void HandleInterpolation() {
    float renderTime = Time.time - interpolationDelay; // 예: 0.1s
    // 버퍼에서 renderTime 전후의 스냅샷 A, B를 찾아 Lerp 수행
    transform.position = Vector3.Lerp(snapshotA.pos, snapshotB.pos, t);
}
```

---
*본 가이드는 TeruTeruServer Milestone 8 표준을 따릅니다.*
