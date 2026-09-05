# BOSS 2 — 곰

## 역할
BOSS1에서 배운 패턴을 더 빠르게 처리하는 중간 보스.
난이도: ★★★☆☆

## 비주얼
- 곰 본체 = 배경 `BearShadow`
- 손등형 팔 = `BearArm_Back`
- 손 옆면형 팔 = `BearArm_Side`

## 기본 공격
Side 팔 사용.
- BEAR_BASIC_1 = [1]
- BEAR_BASIC_2 = [2]
- BEAR_BASIC_3 = [3]
- BEAR_BASIC_12 = [1,2]
- BEAR_BASIC_23 = [2,3]
- BEAR_BASIC_13 = [1,3]

Single = Side 팔 1개.
Double = 같은 Side 팔 2개 동시 사용.

## 복합 패턴

### BEAR_SEQUENTIAL_12
Back 팔.
`[1] → [2]`
- stepInterval = 0.7초

### BEAR_ROUND_TRIP_12321
Back 팔.
`[1] → [2] → [3] → [2] → [1]`
- stepInterval = 0.7초

### BEAR_CROSS_132
Back 팔.
`[1] → [3] → [2]`
- stepInterval = 0.7초

### BEAR_PRESSURE_123
Back 팔.
`[1] → 0.5초 → [2] → 0.5초 → [3]`
- stepInterval = 0.5초
- 기존 확정된 곰 1→2→3 연속 손등 공격을 이 패턴으로 사용

### BEAR_SAFE_LANE_SHIFT_321
Side 팔 2개 동시 사용.
공격: `[1,2] → [1,3] → [2,3]`
안전: `3 → 2 → 1`
- stepInterval = 0.8초

## 패턴 종료 후 휴식
- restAfterPattern = 0.6초

## 30초 Phase
### 0~8초
- BASIC
- SEQUENTIAL_12

### 8~18초
추가:
- CROSS_132
- ROUND_TRIP_12321
- Double 기본 공격 비중 증가

### 18~30초
추가:
- PRESSURE_123
- SAFE_LANE_SHIFT_321
- 모든 패턴 적극 사용

## BOSS1 대비 난이도 상승
- 일반 복합 간격: 0.9 → 0.7초
- Safe Lane: 1.0 → 0.8초
- Rest: 0.8 → 0.6초
- Double/연속 패턴 비중 증가
