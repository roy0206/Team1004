# 스포너 밸런스 리포트

`Team1004 > Spawner Balance Report`가 생성한다(이번 판은 에디터를 열 수 없어 같은 순수 C# 시뮬레이터를 오프라인 .NET 하네스로 돌려 적었다). 손으로 고치지 않는다.

- 생성 시각: 2026-09-06 04:37
- 스포너 설정: `SpawnerDefaults`(= `Assets/GameAssets/Design/Spawner/*.asset`와 같은 값으로 맞춰 둠)
- 스크롤 4.00 u/s, 레인 3, 이동 0.20s, 점프 1.60s, 쿨타임 1.20s, 입력 허용 오차(padding) 0.10s, 격자 0.05s, 플레이어 반폭 0.46
- 구간당 시드 20개, 구간 시간 25/30/35/15초, 시뮬레이션 스텝 0.10s
- 장애물: Rock 길이 2.44 / 히트박스 2.13×0.96 / 속도 x1.00 / 비중 5.00 / 레인 하, Fish 길이 0.90 / 히트박스 0.78×0.36 / 속도 x1.25 / 비중 3.00 / 레인 상/중/하, Log 길이 1.80 / 히트박스 1.57×0.85 / 속도 x0.80 / 비중 2.00 / 레인 상/중/하, LongRock 길이 3.35 / 히트박스 2.91×2.90 / 속도 x1.00 / 비중 1.00 / 레인 상

## 패턴 (구간 속도 배율은 네 구간 모두 1.0)

| 패턴 | 종류 | 티어 | 최악 입력 수 | 반응 여유(s) | 도달(s) | 막는 시간(s) | 비고 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Rock_Bottom | 일반 | 1 | 1 | 2.40 | 2.57 | 0.76 |  |
| Rocks_BottomTwice | 일반 | 1 | 1 | 1.50 | 2.57 | 1.66 |  |
| Rocks_BottomThrice | 일반 | 2 | 1 | 0.80 | 2.57 | 2.36 |  |
| Fish_Top | 일반 | 1 | 1 | 1.85 | 2.04 | 0.34 |  |
| Fish_Middle | 일반 | 1 | 1 | 1.85 | 2.04 | 0.34 |  |
| Fish_Bottom | 일반 | 1 | 1 | 1.85 | 2.04 | 0.34 |  |
| Log_Top | 일반 | 1 | 1 | 3.05 | 3.21 | 0.78 |  |
| Log_Middle | 일반 | 1 | 1 | 3.05 | 3.21 | 0.78 |  |
| Log_Bottom | 일반 | 1 | 1 | 3.05 | 3.21 | 0.78 |  |
| FishTop_RockBottom | 일반 | 1 | 1 | 1.85 | 2.57 | 0.76 |  |
| FishMiddle_RockBottom | 일반 | 2 | 2 | 1.65 | 2.57 | 0.76 |  |
| LogTop_RockBottom | 일반 | 1 | 1 | 2.40 | 3.21 | 0.78 |  |
| LogMiddle_RockBottom | 일반 | 2 | 2 | 2.20 | 3.21 | 0.78 |  |
| Fishes_TopMiddle | 일반 | 2 | 2 | 1.65 | 2.04 | 0.34 |  |
| Fishes_TopBottom | 일반 | 1 | 1 | 1.85 | 2.04 | 0.34 |  |
| FishTop_LogMiddle | 일반 | 2 | 2 | 1.70 | 3.21 | 0.78 |  |
| LogTop_FishMiddle | 일반 | 2 | 2 | 1.70 | 3.21 | 0.78 |  |
| FishTop_LogBottom | 일반 | 1 | 1 | 1.90 | 3.21 | 0.78 |  |
| RockBottom_ThenFishTop | 일반 | 2 | 1 | 1.15 | 2.57 | 1.04 |  |
| FishTop_ThenRockBottom | 일반 | 1 | 1 | 1.70 | 2.04 | 1.46 |  |
| RockBottom_ThenFishMiddle | 일반 | 3 | 2 | 1.25 | 2.57 | 0.94 |  |
| LogTop_ThenRockBottom | 일반 | 1 | 1 | 1.80 | 3.21 | 1.36 |  |
| LogMiddle_ThenRockBottom | 일반 | 2 | 2 | 1.60 | 3.21 | 1.36 |  |
| RockBottom_ThenLogTop | 일반 | 1 | 1 | 2.40 | 2.61 | 1.38 |  |
| RockBottom_ThenLogMiddle | 일반 | 2 | 2 | 2.40 | 2.61 | 1.38 |  |
| Rocks_BottomTwice_FishTopBetween | 일반 | 2 | 1 | 1.00 | 2.57 | 2.16 |  |
| Fish_TopThenBottom | 일반 | 2 | 1 | 1.35 | 2.04 | 0.84 |  |
| Fish_BottomThenTop | 일반 | 2 | 1 | 1.35 | 2.04 | 0.84 |  |
| Weave_RockBottom_ThenFishTopLogMiddle | 일반 | 3 | 2 | 0.95 | 2.57 | 1.68 |  |
| Weave_LogMiddle_ThenFishTopRockBottom | 일반 | 3 | 2 | 0.70 | 3.21 | 1.96 |  |
| Jump_FishTop_FishMiddle_RockBottom | 점프 필수 | 3 | 3 | 1.45 | 2.57 | 0.76 |  |
| Jump_LogTop_FishMiddle_RockBottom | 점프 필수 | 3 | 3 | 1.50 | 3.21 | 0.78 |  |
| Jump_FishTop_LogMiddle_RockBottom | 점프 필수 | 3 | 3 | 1.50 | 3.01 | 0.98 |  |
| LongRock_Solo | 점프 필수 | 3 | 3 | 2.00 | 2.59 | 0.96 |  |

## 구간별 코스

| 구간 | 시간(s) | 속도 배율 | 요구 반응 여유(s) | 여유 미달 패턴 수 | 평균 패턴 수 | 점프 필수 비율 | 평균 도착 간격(s) | 검증 거절/코스 | 빈 결정/코스 | 해결 가능 코스 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 25.00 | 1.00 | 1.30 | 6 | 8.20 | 0.07 | 3.29 | 0.00 | 0.00 | 20/20 |
| 2 | 30.00 | 1.00 | 1.00 | 3 | 10.80 | 0.12 | 2.99 | 2.10 | 0.50 | 20/20 |
| 3 | 35.00 | 1.00 | 0.80 | 1 | 13.40 | 0.13 | 2.75 | 11.15 | 2.70 | 20/20 |
| 4 | 15.00 | 1.00 | 1.30 | 6 | 3.45 | 0.00 | 3.65 | 0.00 | 0.00 | 20/20 |

## 장애물 등장 비율 (시드 20개 합계, 목표 돌 5 : 물고기 3 : 통나무 2)

| 구간 | Rock | Fish | Log | LongRock | 합계 |
| --- | --- | --- | --- | --- | --- |
| 1 | 129 (45.42%) | 80 (28.17%) | 63 (22.18%) | 12 (4.23%) | 284 |
| 2 | 194 (45.75%) | 126 (29.72%) | 78 (18.40%) | 26 (6.13%) | 424 |
| 3 | 252 (45.99%) | 161 (29.38%) | 101 (18.43%) | 34 (6.20%) | 548 |
| 4 | 56 (43.75%) | 44 (34.38%) | 28 (21.88%) | 0 (0.00%) | 128 |
| 전체 | 631 (45.59%) | 411 (29.70%) | 270 (19.51%) | 72 (5.20%) | 1384 |

## 구간별 패턴 사용 횟수 (시드 합계)

| 패턴 | 구간 1 | 구간 2 | 구간 3 | 구간 4 |
| --- | --- | --- | --- | --- |
| Rock_Bottom | 6 | 0 | 4 | 3 |
| Rocks_BottomTwice | 16 | 12 | 4 | 5 |
| Rocks_BottomThrice | 8 | 24 | 35 | 5 |
| Fish_Top | 6 | 4 | 4 | 5 |
| Fish_Middle | 6 | 3 | 4 | 3 |
| Fish_Bottom | 8 | 1 | 1 | 0 |
| Log_Top | 5 | 2 | 2 | 2 |
| Log_Middle | 5 | 5 | 4 | 5 |
| Log_Bottom | 10 | 1 | 2 | 0 |
| FishTop_RockBottom | 8 | 4 | 4 | 4 |
| FishMiddle_RockBottom | 2 | 7 | 10 | 0 |
| LogTop_RockBottom | 9 | 3 | 6 | 3 |
| LogMiddle_RockBottom | 3 | 7 | 16 | 0 |
| Fishes_TopMiddle | 3 | 9 | 11 | 2 |
| Fishes_TopBottom | 6 | 2 | 2 | 4 |
| FishTop_LogMiddle | 1 | 10 | 11 | 1 |
| LogTop_FishMiddle | 1 | 8 | 12 | 1 |
| FishTop_LogBottom | 3 | 3 | 2 | 4 |
| RockBottom_ThenFishTop | 2 | 13 | 10 | 0 |
| FishTop_ThenRockBottom | 7 | 4 | 0 | 3 |
| RockBottom_ThenFishMiddle | 0 | 5 | 9 | 0 |
| LogTop_ThenRockBottom | 6 | 6 | 5 | 3 |
| LogMiddle_ThenRockBottom | 7 | 14 | 15 | 2 |
| RockBottom_ThenLogTop | 9 | 9 | 3 | 7 |
| RockBottom_ThenLogMiddle | 3 | 8 | 8 | 0 |
| Rocks_BottomTwice_FishTopBetween | 5 | 8 | 17 | 3 |
| Fish_TopThenBottom | 3 | 7 | 9 | 2 |
| Fish_BottomThenTop | 3 | 9 | 9 | 2 |
| Weave_RockBottom_ThenFishTopLogMiddle | 0 | 0 | 10 | 0 |
| Weave_LogMiddle_ThenFishTopRockBottom | 1 | 2 | 5 | 0 |
| Jump_FishTop_FishMiddle_RockBottom | 0 | 0 | 0 | 0 |
| Jump_LogTop_FishMiddle_RockBottom | 0 | 0 | 0 | 0 |
| Jump_FishTop_LogMiddle_RockBottom | 0 | 0 | 0 | 0 |
| LongRock_Solo | 12 | 26 | 34 | 0 |

반응 여유는 패턴이 완전히 보인 뒤 아무 입력 없이 버틸 수 있는 최대 시간이다. 「요구 반응 여유」는 `DifficultyCurve.minReactionMargin`(구간별 검증 문턱)이고, 「여유 미달 패턴 수」는 그 문턱보다 자체 여유가 짧아 그 구간에서 사실상 뽑히지 않는 일반 패턴 수다. 검증 거절은 후보가 시뮬레이터 생존 검사에 떨어진 횟수, 빈 결정은 모든 후보가 떨어져 스폰을 미룬 횟수다.
