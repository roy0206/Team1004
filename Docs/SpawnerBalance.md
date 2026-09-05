# 스포너 밸런스 리포트

`Team1004 > Spawner Balance Report`가 생성한다(이번 판은 에디터를 열 수 없어 같은 순수 C# 시뮬레이터를 오프라인 .NET 하네스로 돌려 적었다). 손으로 고치지 않는다.

- 생성 시각: 2026-09-06 01:50
- 스포너 설정: `SpawnerDefaults`(= `Assets/GameAssets/Design/Spawner/*.asset`와 같은 값으로 맞춰 둠)
- 스크롤 4.00 u/s, 레인 3, 이동 0.20s, 점프 1.60s, 쿨타임 1.20s, 입력 허용 오차(padding) 0.10s, 격자 0.05s, 플레이어 반폭 0.46
- 구간당 시드 20개, 구간 시간 25/30/35/15초, 시뮬레이션 스텝 0.10s
- 장애물: Rock 길이 2.44 / 히트박스 2.13×0.96 / 속도 x1.00 / 비중 5.00 / 레인 하, Fish 길이 0.90 / 히트박스 0.78×0.39 / 속도 x1.25 / 비중 3.00 / 레인 상/중/하, Log 길이 1.80 / 히트박스 1.57×0.85 / 속도 x0.80 / 비중 2.00 / 레인 상/중/하

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

## 구간별 코스

| 구간 | 시간(s) | 속도 배율 | 요구 반응 여유(s) | 여유 미달 패턴 수 | 평균 패턴 수 | 점프 필수 비율 | 평균 도착 간격(s) | 검증 거절/코스 | 빈 결정/코스 | 해결 가능 코스 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 25.00 | 1.00 | 1.30 | 6 | 8.15 | 0.00 | 3.32 | 0.00 | 0.00 | 20/20 |
| 2 | 30.00 | 1.00 | 1.00 | 3 | 10.80 | 0.00 | 3.00 | 0.05 | 0.00 | 20/20 |
| 3 | 35.00 | 1.00 | 0.80 | 1 | 13.25 | 0.00 | 2.77 | 1.05 | 0.20 | 20/20 |
| 4 | 15.00 | 1.00 | 1.30 | 6 | 3.45 | 0.00 | 3.65 | 0.00 | 0.00 | 20/20 |

## 장애물 등장 비율 (시드 20개 합계, 목표 돌 5 : 물고기 3 : 통나무 2)

| 구간 | Rock | Fish | Log | 합계 |
| --- | --- | --- | --- | --- |
| 1 | 141 (47.64%) | 96 (32.43%) | 59 (19.93%) | 296 |
| 2 | 214 (48.53%) | 138 (31.29%) | 89 (20.18%) | 441 |
| 3 | 288 (49.40%) | 182 (31.22%) | 113 (19.38%) | 583 |
| 4 | 56 (43.75%) | 44 (34.38%) | 28 (21.88%) | 128 |
| 전체 | 699 (48.27%) | 460 (31.77%) | 289 (19.96%) | 1448 |

## 구간별 패턴 사용 횟수 (시드 합계)

| 패턴 | 구간 1 | 구간 2 | 구간 3 | 구간 4 |
| --- | --- | --- | --- | --- |
| Rock_Bottom | 5 | 3 | 2 | 3 |
| Rocks_BottomTwice | 21 | 14 | 11 | 5 |
| Rocks_BottomThrice | 8 | 25 | 38 | 5 |
| Fish_Top | 9 | 4 | 3 | 5 |
| Fish_Middle | 7 | 6 | 2 | 3 |
| Fish_Bottom | 8 | 3 | 2 | 0 |
| Log_Top | 5 | 4 | 3 | 2 |
| Log_Middle | 5 | 6 | 4 | 5 |
| Log_Bottom | 7 | 3 | 1 | 0 |
| FishTop_RockBottom | 7 | 3 | 3 | 4 |
| FishMiddle_RockBottom | 2 | 10 | 13 | 0 |
| LogTop_RockBottom | 10 | 4 | 8 | 3 |
| LogMiddle_RockBottom | 3 | 10 | 18 | 0 |
| Fishes_TopMiddle | 2 | 9 | 17 | 2 |
| Fishes_TopBottom | 9 | 5 | 3 | 4 |
| FishTop_LogMiddle | 1 | 7 | 10 | 1 |
| LogTop_FishMiddle | 2 | 10 | 14 | 1 |
| FishTop_LogBottom | 3 | 3 | 2 | 4 |
| RockBottom_ThenFishTop | 3 | 12 | 14 | 0 |
| FishTop_ThenRockBottom | 7 | 6 | 4 | 3 |
| RockBottom_ThenFishMiddle | 1 | 3 | 5 | 0 |
| LogTop_ThenRockBottom | 7 | 7 | 6 | 3 |
| LogMiddle_ThenRockBottom | 6 | 16 | 15 | 2 |
| RockBottom_ThenLogTop | 6 | 8 | 5 | 7 |
| RockBottom_ThenLogMiddle | 3 | 7 | 11 | 0 |
| Rocks_BottomTwice_FishTopBetween | 7 | 9 | 16 | 3 |
| Fish_TopThenBottom | 3 | 5 | 8 | 2 |
| Fish_BottomThenTop | 5 | 10 | 11 | 2 |
| Weave_RockBottom_ThenFishTopLogMiddle | 0 | 1 | 8 | 0 |
| Weave_LogMiddle_ThenFishTopRockBottom | 1 | 3 | 8 | 0 |
| Jump_FishTop_FishMiddle_RockBottom | 0 | 0 | 0 | 0 |
| Jump_LogTop_FishMiddle_RockBottom | 0 | 0 | 0 | 0 |
| Jump_FishTop_LogMiddle_RockBottom | 0 | 0 | 0 | 0 |

반응 여유는 패턴이 완전히 보인 뒤 아무 입력 없이 버틸 수 있는 최대 시간이다. 「요구 반응 여유」는 `DifficultyCurve.minReactionMargin`(구간별 검증 문턱)이고, 「여유 미달 패턴 수」는 그 문턱보다 자체 여유가 짧아 그 구간에서 사실상 뽑히지 않는 일반 패턴 수다. 검증 거절은 후보가 시뮬레이터 생존 검사에 떨어진 횟수, 빈 결정은 모든 후보가 떨어져 스폰을 미룬 횟수다.
