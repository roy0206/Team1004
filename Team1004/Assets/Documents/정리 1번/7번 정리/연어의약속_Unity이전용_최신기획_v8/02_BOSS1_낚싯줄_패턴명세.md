# BOSS 1 — 낚싯줄

## 역할
공통 패턴 문법을 처음 배우는 보스.
난이도: ★★☆☆☆

## 기본 공격
- BASIC_1 = [1]
- BASIC_2 = [2]
- BASIC_3 = [3]
- BASIC_12 = [1,2]
- BASIC_23 = [2,3]
- BASIC_13 = [1,3]

Single = 낚싯줄 1개.
Double = 동일 프리팹 2개 동시 사용.
기본 공격 비중은 세 보스 중 가장 높다.

## 복합 패턴

### FISHING_SEQUENTIAL_12
`[1] → [2]`
- stepInterval = 0.9초

### FISHING_ROUND_TRIP_12321
`[1] → [2] → [3] → [2] → [1]`
- stepInterval = 0.9초

### FISHING_CROSS_132
`[1] → [3] → [2]`
- stepInterval = 0.9초

### FISHING_PRESSURE_123
`[1] → 0.5초 → [2] → 0.5초 → [3]`
- stepInterval = 0.5초
- 낮은 빈도

### FISHING_SAFE_LANE_SHIFT_321
공격: `[1,2] → [1,3] → [2,3]`
안전: `3 → 2 → 1`
- stepInterval = 1.0초

## 패턴 종료 후 휴식
- restAfterPattern = 0.8초

## 30초 Phase
### 0~10초
- BASIC
- SEQUENTIAL_12

### 10~20초
추가:
- CROSS_132
- ROUND_TRIP_12321

### 20~30초
추가:
- PRESSURE_123
- SAFE_LANE_SHIFT_321
- 모든 패턴 허용

30초 안에 공통 복합 5종 최소 1회 등장 보장.
