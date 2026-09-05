# 스포너 밸런스 리포트

`Team1004 > Spawner Balance Report`가 생성한다. 손으로 고치지 않는다.

- 생성 시각: 2026-09-05 22:32
- 스포너 설정: `Assets/GameAssets/Design/Spawner/ObstacleSpawnerSettings.asset`
- GameConfig: `Assets/GameAssets/Design/GameConfig.asset`
- 스크롤 4.00 u/s, 레인 3, 이동 0.20s, 점프 0.80s, 쿨타임 1.20s, 입력 허용 오차(padding) 0.10s, 격자 0.05s
- 구간당 시드 20개, 구간 시간 25/30/35/15초(기획서 3판 9절), 시뮬레이션 스텝 0.10s

## 패턴 (속도 배율 1.0)

| 패턴 | 종류 | 티어 | 최악 입력 수 | 반응 여유(s) | 도달(s) | 막는 시간(s) | 비고 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Rock_Top | 일반 | 1 | 1 | 2.40 | 2.58 | 0.39 |  |
| Rock_Middle | 일반 | 1 | 1 | 2.40 | 2.58 | 0.39 |  |
| Rock_Bottom | 일반 | 1 | 1 | 2.40 | 2.58 | 0.39 |  |
| Fish_Top | 일반 | 2 | 1 | 1.25 | 1.44 | 0.26 |  |
| Fish_Middle | 일반 | 2 | 1 | 1.25 | 1.44 | 0.26 |  |
| Fish_Bottom | 일반 | 2 | 1 | 1.25 | 1.44 | 0.26 |  |
| Log_Top | 일반 | 1 | 1 | 2.45 | 2.61 | 0.83 |  |
| Log_Middle | 일반 | 1 | 1 | 2.45 | 2.61 | 0.83 |  |
| Log_Bottom | 일반 | 1 | 1 | 2.45 | 2.61 | 0.83 |  |
| Rocks_TopBottom | 일반 | 1 | 1 | 2.40 | 2.58 | 0.39 |  |
| Rocks_TopMiddle | 일반 | 2 | 2 | 2.20 | 2.58 | 0.39 |  |
| Rocks_MiddleBottom | 일반 | 2 | 2 | 2.20 | 2.58 | 0.39 |  |
| RockTop_FishBottom | 일반 | 2 | 1 | 1.25 | 2.58 | 0.39 |  |
| FishTop_RockBottom | 일반 | 2 | 1 | 1.25 | 2.58 | 0.39 |  |
| Fishes_TopMiddle | 일반 | 3 | 2 | 1.05 | 1.44 | 0.26 |  |
| RockMiddle_LogBottom | 일반 | 2 | 2 | 2.20 | 2.61 | 0.83 |  |
| LogTop_RockMiddle | 일반 | 2 | 2 | 2.20 | 2.61 | 0.83 |  |
| FishTop_LogMiddle | 일반 | 3 | 2 | 1.10 | 2.61 | 0.83 |  |
| Rocks_TopThenBottom | 일반 | 1 | 1 | 1.70 | 2.58 | 1.09 |  |
| Rocks_BottomThenTop | 일반 | 1 | 1 | 1.70 | 2.58 | 1.09 |  |
| Fish_TopThenBottom | 일반 | 2 | 1 | 0.75 | 1.44 | 0.76 |  |
| LogBottom_ThenRockMiddle | 일반 | 2 | 2 | 1.90 | 2.61 | 0.89 |  |
| LogTop_ThenRockMiddle | 일반 | 2 | 2 | 1.90 | 2.61 | 0.89 |  |
| Weave_MiddleThenTopBottom | 일반 | 2 | 2 | 1.50 | 2.58 | 1.29 |  |
| Jump_RocksAllLanes | 점프 필수 | 3 | 3 | 2.00 | 2.58 | 0.39 |  |
| Jump_FishTop_RocksMiddleBottom | 점프 필수 | 3 | 3 | 0.85 | 2.58 | 0.39 |  |
| Jump_LogBottom_RockMiddle_FishTop | 점프 필수 | 3 | 3 | 0.90 | 2.61 | 0.83 |  |
| Jump_LogMiddle_RockTop_RockBottom | 점프 필수 | 3 | 3 | 1.90 | 2.61 | 0.83 |  |

## 구간별 코스

| 구간 | 시간(s) | 최대 속도 배율 | 최소 반응 여유@최대속도(s) | 평균 패턴 수 | 점프 필수 비율 | 평균 도착 간격(s) | 검증 거절/코스 | 빈 결정/코스 | 해결 가능 코스 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 25.00 | 1.05 | 0.70 | 8.95 | 0.15 | 2.94 | 0.00 | 0.00 | 20/20 |
| 2 | 30.00 | 1.25 | 0.50 | 13.15 | 0.19 | 2.37 | 0.00 | 0.00 | 20/20 |
| 3 | 35.00 | 1.50 | 0.30 | 18.00 | 0.27 | 1.98 | 0.40 | 0.00 | 20/20 |
| 4 | 15.00 | 1.10 | 0.65 | 3.80 | 0.04 | 3.34 | 0.00 | 0.00 | 20/20 |

## 구간별 패턴 사용 횟수 (시드 합계)

| 패턴 | 구간 1 | 구간 2 | 구간 3 | 구간 4 |
| --- | --- | --- | --- | --- |
| Rock_Top | 10 | 5 | 7 | 3 |
| Rock_Middle | 10 | 7 | 3 | 9 |
| Rock_Bottom | 6 | 14 | 4 | 6 |
| Fish_Top | 8 | 16 | 13 | 0 |
| Fish_Middle | 6 | 5 | 11 | 2 |
| Fish_Bottom | 5 | 9 | 21 | 1 |
| Log_Top | 8 | 5 | 4 | 7 |
| Log_Middle | 9 | 6 | 9 | 5 |
| Log_Bottom | 10 | 7 | 8 | 2 |
| Rocks_TopBottom | 12 | 3 | 5 | 7 |
| Rocks_TopMiddle | 4 | 15 | 17 | 1 |
| Rocks_MiddleBottom | 2 | 14 | 18 | 2 |
| RockTop_FishBottom | 4 | 11 | 19 | 2 |
| FishTop_RockBottom | 5 | 12 | 17 | 1 |
| Fishes_TopMiddle | 0 | 5 | 8 | 0 |
| RockMiddle_LogBottom | 5 | 7 | 11 | 4 |
| LogTop_RockMiddle | 3 | 14 | 19 | 0 |
| FishTop_LogMiddle | 2 | 8 | 15 | 0 |
| Rocks_TopThenBottom | 9 | 8 | 4 | 5 |
| Rocks_BottomThenTop | 13 | 8 | 4 | 7 |
| Fish_TopThenBottom | 3 | 7 | 10 | 1 |
| LogBottom_ThenRockMiddle | 3 | 13 | 10 | 2 |
| LogTop_ThenRockMiddle | 8 | 5 | 9 | 4 |
| Weave_MiddleThenTopBottom | 8 | 8 | 17 | 2 |
| Jump_RocksAllLanes | 5 | 10 | 20 | 1 |
| Jump_FishTop_RocksMiddleBottom | 5 | 12 | 30 | 1 |
| Jump_LogBottom_RockMiddle_FishTop | 7 | 14 | 19 | 0 |
| Jump_LogMiddle_RockTop_RockBottom | 9 | 15 | 28 | 1 |

반응 여유는 패턴이 완전히 보인 뒤 아무 입력 없이 버틸 수 있는 최대 시간이다. 검증 거절은 후보가 시뮬레이터 생존 검사에 떨어진 횟수, 빈 결정은 모든 후보가 떨어져 스폰을 미룬 횟수다.
