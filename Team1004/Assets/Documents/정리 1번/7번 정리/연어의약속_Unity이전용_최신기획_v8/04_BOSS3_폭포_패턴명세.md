# BOSS 3 — 폭포

## 중요
BOSS3은 폭포다. 긴 돌로 대체되지 않았다.

## 역할
최종 보스. 레인 이동 + 빠른 연속 판단 + 점프를 모두 시험한다.
난이도: ★★★★★

## 공격 표현
1. 위험 레인 빨간 경고
2. 해당 레인에 임시 강한 물줄기
3. 기존 돌 공격체 통과
4. 공격 후 기본 물 흐름 복귀

## 기본 공격
- WATERFALL_BASIC_1 = [1]
- WATERFALL_BASIC_2 = [2]
- WATERFALL_BASIC_3 = [3]
- WATERFALL_BASIC_12 = [1,2]
- WATERFALL_BASIC_23 = [2,3]
- WATERFALL_BASIC_13 = [1,3]

기본 공격 비중은 세 보스 중 가장 낮다.

## 복합 패턴

### WATERFALL_SEQUENTIAL_12
`[1] → [2]`
- stepInterval = 0.6초

### WATERFALL_ROUND_TRIP_12321
`[1] → [2] → [3] → [2] → [1]`
- stepInterval = 0.6초

### WATERFALL_CROSS_132
`[1] → [3] → [2]`
- stepInterval = 0.6초

### WATERFALL_PRESSURE_123
`[1] → 0.5초 → [2] → 0.5초 → [3]`
- stepInterval = 0.5초
- 세 보스 중 높은 빈도

### WATERFALL_SAFE_LANE_SHIFT_321
공격: `[1,2] → [1,3] → [2,3]`
안전: `3 → 2 → 1`
- stepInterval = 0.65초

## 폭포 전용 패턴
### WATERFALL_FULL_JUMP_123
`[1,2,3]`
- telegraph = 1.5초
- 회피 = 1번 레인에서 점프
- Jump Ready가 아닐 때 선택 금지
- 선택 직전 점프 가능 여부 검사

## 패턴 종료 후 휴식
- 초기값 restAfterPattern = 0.45초
- Inspector 허용 범위 0.4~0.5초

## 30초 Phase
### 0~8초
- BASIC
- SEQUENTIAL_12
- CROSS_132 일부

### 8~20초
- 공통 복합 5종 전부
- FULL_JUMP_123

### 20~30초
- 모든 패턴
- Pattern Chain 사용 가능

## 폭포 전용 Pattern Chain
두 패턴의 실제 Collider는 절대 겹치지 않는다.
Pattern A 종료 → 0.25초 연결 간격 → Pattern B Telegraph 시작.

### CHAIN_A
`CROSS_132 → FULL_JUMP_123`

### CHAIN_B
`SEQUENTIAL_12 → SAFE_LANE_SHIFT_321`

### CHAIN_C
`ROUND_TRIP_12321 → PRESSURE_123`

FULL_JUMP_123이 Chain에 포함되면 Jump Ready를 사전 검사한다.

## BOSS2 대비 난이도 상승
- 일반 복합 간격: 0.7 → 0.6초
- Safe Lane: 0.8 → 0.65초
- Rest: 0.6 → 0.45초
- 전체 레인 점프 공격 추가
- 20초 이후 Pattern Chain 추가
