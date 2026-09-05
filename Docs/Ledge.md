# 상류 단차 (Game.Ledge)

일반 구간 중간에 한 번씩 나오는 1레인 높이의 지형 단차다. 연어가 화면 오른쪽으로만 가는 게 아니라 실제로 상류로 올라간다는 것을 플레이로 표현한다. 기획서 4판 `05_상류단차_시스템.md`(전문), `04_점프_시스템.md`, `06_물흐름_월드스크롤_카메라.md`(단차 카메라), `09_일반구간_난이도_스폰.md`(구간 1·2 중간), `14_충돌_실패_재시작_체크포인트.md`(단차 실패는 죽지 않음), `15_인게임HUD_히트박스_안내UI.md`(첫 단차 배경 `↑`), `19_그래픽_애니메이션_리소스.md`(아래 강·지형·위쪽 강), `25_미확정_후순위_항목.md`(정확한 초는 미확정)를 반영했다.

**2026-09-06 기획 답변 반영**: 진행 1에 5초 단차가 추가되어 구간 1은 5초·12.5초 두 번, 구간 2는 15초 한 번(전부 세 조각)이 되었고, 넘은 뒤는 (가) 화면 복귀 유지다. 모양은 한때 「작은 폭포」였다가 **2026-09-06 사용자 결정으로 「긴 돌」로 확정**되었다. 「일정 (데이터)」·「단차 모양 (긴 돌)」·「생성기」를 본다.

**2026-09-06 QTE 전환 (사용자 지시)**: 「막히면 월드 정지 후 다시 점프」가 사라지고 **QTE**가 들어왔다. 단차 앞면이 플레이어에게 `qteStartDistance`(2.5 unit)까지 오면 월드가 **멈추지 않고 `qteWorldSpeedScale`(0.12)로 느려지며** ↑↓ 교대 입력 `requiredPresses`(6)회를 요구한다. 다 누르면 연어가 자동으로 상단 레인에서 점프해 넘어가고, 앞면이 플레이어 x에 닿을 때까지 못 누르면 **벽에 부딪혀 실패**한다(일반 피격 연출 → `PlayFlow.ReportImpact()`). `LedgePhase.Blocked`·`LedgeSignal.Blocked`·`ILedgeHandler.Blocked`는 삭제됐고 `Qte`/`QteStarted`가 그 자리에 있다. `instantFail`은 **더 이상 읽지 않는다**(아래 「데이터」). 공용 부품은 `Docs/Qte.md`.

`Game.Ledge`는 `Game.Play`를 참조하지 않는다. `PlayFlow`는 `ILedgeHandler` 델리게이트 표면만 알고 단차 내부를 모른다(`Docs/Boss.md`의 `BossDirector`/`BossHandler`와 같은 형태).

## 동작 요약

| 단계 | 조건 | 하는 일 |
| --- | --- | --- |
| `Waiting` | 구간 시작 ~ `StartTime` | 단차는 화면 밖 오른쪽에 비활성 대기 |
| `Incoming` | `StartTime` ~ `ApproachTime` | 단차가 `spawnX`(7.5)에서 스크롤 속도로 들어온다 |
| `Approaching` | `ledgeTime - approachSafeTime` | **장애물 스폰 중단**(`IsSpawnSuspended`). 플레이어가 단차를 보고 1번 레인으로 이동하고 점프 쿨타임을 회복할 2초를 얻는다. 쿨타임을 강제로 0으로 만들지 않는다 |
| `Qte` | 앞면이 `playerX + qteStartDistance`(2.5)에 닿음 + 착지 상태 | **월드 감속**(`WorldSpeedScale` = `qteWorldSpeedScale` 0.12). ↑↓ 교대 QTE 시작(`QteStarted`). 스폰은 계속 중단. 플레이어의 레인 이동·점프 입력은 QTE가 가져간다(`LanePlayer.QteCaptureInput`). **이미 공중이면 QTE를 열지 않는다**(직접 뛰어넘는 길을 남긴다) |
| QTE 성공 | `requiredPresses`회 교대 입력 완료 | `LanePlayer.ForceJump()`(상단 레인 스냅 + 쿨타임 무시 점프). 다음 틱에 공중이 감지되어 `Passing`, 월드 속도 1로 복귀(`Resumed`) |
| 도착 | `ledgeTime`(단차 앞면 x = `playerX`) | 공중이면 `Passing`, 착지 상태면 `Failed` |
| `Passing` | 공중 | 단차가 플레이어 아래를 지나간다. 착지했는데 앞면이 아직 `playerX - clearMargin`보다 오른쪽이면 `Failed`(너무 일찍 뛴 경우) |
| `Rising` | 착지 + 앞면이 완전히 지나감 | `Cleared`. `cameraMoveDuration`(0.7초) 동안 단차 조각이 `stepHeight`(1.1)만큼 내려와 **위쪽 강바닥이 평소 강바닥 높이에 맞춰진다**. 배경도 `environmentRiseAmount`만큼 같이 내려간다 |
| `Retiring` | 상승 완료 | `Finished`. 장애물 스폰 재개. 배경 오프셋은 `environmentSettleDuration` 동안 0으로 돌아오고 단차는 왼쪽으로 빠져나간다 |
| `Done` | 앞면 x ≤ `retireX` | 단차 비활성 |
| `Failed` | 착지 상태로 도착 | `Failed` 이벤트. `PlayFlow.OnLedgeFailed` → **`ReportImpact()`**(마지막 폭포와 같은 길). 0.1초 정지 + 흔들림 + 왼쪽 밀림 뒤 `Fail()`. R로 구간을 처음부터 다시 한다 |

플레이어의 레인 로직은 손대지 않는다. 착지는 항상 1번 레인(y 1.1)이고 "위쪽 강에 올라간 것"은 순수하게 월드가 내려가는 연출이다. 상류에서도 레인은 3개 그대로다(기획서 05절).

`↑` 스프라이트는 **맨 처음 나오는 단차 하나에만** 붙는다(`hintSection`(1) 구간의 첫 항목 = `LedgeData.HintEntryIndex`, 기본 진행 1의 5초 단차). 앞면 왼쪽 2.2 unit 지점이고 HUD가 아니라 월드 배경 오브젝트이며 정렬 -4로 배경 층과 물 사이에 들어간다. 같은 구간의 두 번째 단차(12.5초)와 구간 2(15초)는 힌트가 없다.

## 왜 "월드가 내려간다"인가

플레이어·레인·스포너·카메라는 고정 좌표에 있어야 한다(레인 y 1.1/0/−1.1, `playerX` −4.2, ortho 3.6, 수면 y 2.0). 카메라를 실제로 올리면 레인·스포너 좌표가 전부 어긋난다. 그래서 카메라 대신 지형이 내려온다.

단차를 넘기 전후의 화면은 **같아야** 한다(상류에서도 같은 사이드뷰, 같은 3레인). 그래서 영구적인 배경 y 오프셋은 두지 않는다. 실제로 움직이는 것은 단차 조각뿐이다.

- 단차 앞면(계단 벽)의 윗변은 `laneY[0] + stepHeight` = 2.2. 1번 레인(1.1)보다 1레인 높고 점프 정점(1.1 + `jumpHeight` 2.2 = 3.3)보다 낮아 기본 점프 하나로 넘어간다.
- 위쪽 강바닥의 윗선은 평소 강바닥(**−1.65**, `Docs/Environment.md` 「강바닥」)보다 `stepHeight`만큼 높은 **−0.55**다.
- 넘은 뒤 0.7초 동안 조각 전체가 1.1 내려오면 위쪽 강바닥이 −1.65가 되어 환경 `RiverbedLayer`와 정확히 이어진다. 이때 앞면의 긴 돌은 이미 화면 왼쪽 밖이다(도착 후 0.55초면 x < −6.4).
- 배경(`EnvironmentThing`)은 `environmentRiseAmount`(0.35, 1레인보다 작다)만큼만 같이 내려갔다가 `environmentSettleDuration`(1.2초) 동안 0으로 돌아온다. 카메라가 한 번 올라갔다 자리 잡는 느낌만 주고 강바닥 높이를 영구히 바꾸지 않는다. 0으로 두면 배경은 전혀 움직이지 않는다.

## 파일

| asmdef | 경로 | 타입 | 책임 |
| --- | --- | --- | --- |
| `Game.Ledge` | `Assets/Scripts/Ledge/` | `LedgePlan` | 순수 struct. 도착 시각·접근 시각·등장 시각·등장 x를 계산하고 `FrontXAt(t)`를 준다. 테스트 대상 |
| | | `LedgeClock` | 순수 C#. 위상 전이(대기→등장→접근→**QTE**→통과→상승→퇴장, 실패)와 QTE 시작·충돌 판정. `ImpactRemaining`(도착까지 남은 시계 초)·`FrontDistance`를 준다. 테스트 대상 |
| | | `LedgePhase`, `LedgeSignal` | 위상 enum, 이번 틱에 일어난 일의 비트 플래그 |
| | | `LedgeData : ScriptableObject` | 조정값 전부. `Create > Team1004 > Ledge Data` |
| | | `ILedgeHandler` | `PlayFlow`가 보는 표면. `BeginSection`/`Tick`/`Stop`, 읽기 `IsQteActive`·`WorldSpeedScale`·`IsSpawnSuspended`·`IsHoldingWorld`(항상 false, 옛 API 호환) + 이벤트 6개(`Approaching`/`QteStarted`/`Resumed`/`Cleared`/`Finished`/`Failed`) |
| | | `ILedgeWorld` | 단차가 월드에 요청하는 것(`SetWorldScrolling`, **`SetWorldSpeedScale`**, `SetObstacleSpawning`). 통합이 구현하면 push, 안 하면 `WorldSpeedScale`/`IsSpawnSuspended` polling(`PlayFlow`가 쓰는 쪽) |
| | | `LedgeThing : MonoThing` | 단차 조각 하나. 모듈 3개를 붙이고 `Schedule`/`Advance`/`Retire` |
| | | `LedgeScrollModule : Module` | `Ticks = None`. 앞면 x와 세로 오프셋을 `localPosition`에 적는다 |
| | | `LedgeGateModule : Module` | `Ticks = None`. `LedgeClock` 보관. `Ease`(SmoothStep) 제공 |
| | | `LedgeHintModule : Module` | `Ticks = Update`. `↑` 표시 on/off와 위아래 흔들림 |
| | | `LedgeScheduleEntry` | `[Serializable]`. `section` + `time` 한 쌍 |
| | | `LedgeDirector : MonoThing, ILedgeHandler` | 단차 **3개** 소유. 구간 → 항목 큐 계산, 한 번에 하나씩 활성화, 이벤트 발생, 배경 오프셋, 월드 감속 요청. **QTE 글루**: 자기 자신에 `QteModule`을 붙이고(`Game.Qte`) 시작·중단·성공 점프·`QtePanel` 갱신을 한다 |
| `Game.Ledge.Editor` | `Assets/Scripts/Ledge/Editor/` | `LedgeSetup` | 생성기 메뉴. 텍스처 → 데이터 에셋 → 프리팹 |
| | | `LedgePatterns` | 임시 텍스처 무늬(바위, 바위 윗면, `↑`) |
| `Game.Ledge.Tests` | `Assets/Scripts/Ledge/Tests/` | `LedgeScheduleTests` | EditMode 23개. 일정 계산, **QTE 시작 거리 판정**, **도착 = 실패 판정**, 공중 선점 시 QTE 생략, 항목 목록, 구간 시각 프라이밍, 5초 조각이 12.5초 조각보다 먼저 퇴장하는지 |

asmdef 참조: `Game.Ledge` → `Core.Modules`, `Core.Foundation`, `Game.Config`, `Game.Environment`, `Game.Player`(`IsAirborne` 읽기 + `QteCaptureInput`·`ForceJump` 호출), `Game.Qte`, `Game.Water`. **`Game.Play`·`Game.Spawner`·`Game.Boss`는 참조하지 않는다.** 순환 없음. DOTween은 쓰지 않는다(상승·흔들림은 모듈 틱 계산이라 `Time.timeScale = 0`에서 자동으로 멈춘다).

## 데이터 (`Assets/GameAssets/Design/Ledge/LedgeData.asset`)

| 필드 | 기본값 | 근거 |
| --- | --- | --- |
| `ledgeEntries` | `[(1, 5), (1, 12.5), (2, 15)]` | **구간 번호 + 도착 시각(초) 쌍의 평면 목록**(2026-09-06 기획 답변). 한 구간에 여러 개를 둘 수 있다. 항목 순서가 곧 `ledges[]` 조각 번호다(항목 0 → `Ledge1`, 1 → `Ledge2`, 2 → `Ledge3`). `time`이 0 이하면 그 항목은 없는 것으로 본다. 구간 3·4에는 항목이 없다 |
| `hintSection` | `1` | 배경 `↑`를 보이는 구간. 기획서 15절 "두 번째 단차는 힌트 반복 없음" |
| `approachSafeTime` | `2.0` | 기획서 05절 확정. 이 시각부터 장애물 스폰 중단 |
| `stepHeight` | `1.1` | 단차 높이 1레인 = `laneY[0] - laneY[1]` |
| `cameraMoveDuration` | `0.7` | 기획서 05절 확정(0.5~1.0 중 프로토타입 추천값) |
| `spawnX` | `7.5` | 화면 오른쪽(x +6.4) 밖. 진입 이동 시간 = (7.5 −(−4.2))/4 = 2.925초 |
| `retireX` | `-20` | 앞면이 여기까지 가면 조각을 비활성 |
| `clearMargin` | `1.2` | 계단 벽 폭. 착지 시점에 앞면이 `playerX − 1.2`보다 오른쪽이면 아직 못 넘은 것으로 보고 **실패**로 본다 |
| `instantFail` | `false` | **폐기(2026-09-06 QTE 전환).** 이제 도착 = 언제나 실패이므로 `LedgeClock`이 이 값을 읽지 않는다. 직렬화 필드와 `LedgeData.InstantFail`은 에셋 호환을 위해 남겨 뒀다 |
| `environmentRiseAmount` | `0.35` | 상승 중 배경이 같이 내려가는 양. 0이면 배경 고정 |
| `environmentSettleDuration` | `1.2` | 배경 오프셋이 0으로 돌아오는 시간 |
| `sectionEndMargin` | `3.0` | `ledgeTime`이 `구간 시간 − 이 값`보다 크면 구간 안에 못 들어간다고 보고 경고 후 건너뛴다 |
| `qteData` | `Design/Qte/QteData.asset` | QTE 공용 데이터(누를 횟수 6, 첫 키 ↑, 번쩍임 0.18초, 액션 이름). 비어 있으면 경고 후 「6회·↑ 먼저」로 대체한다. `Docs/Qte.md` |
| `qteStartDistance` | `2.5` | 앞면이 플레이어에서 이 거리(unit) 안에 들어오면 QTE 시작. 4 u/s에서 도착 **0.625초 전**이고, 감속 0.12를 곱하면 실제 입력 창은 **약 5.2초**다 |
| `qteWorldSpeedScale` | `0.12` | QTE 중 월드 속도 배율. 배경·스크롤 루트·단차 조각·거리·구간 시간·스포너 `Advance`가 전부 이 배율로 느려진다. 0으로 두지 않는다(정지가 아니라 감속이라는 것이 기획 지시다) |

`GameConfig.Current`에서 읽는 값(데이터로 복제하지 않는다): `ScrollSpeed`(4), `PlayerX`(−4.2), `LaneY[0]`(1.1), `JumpHeight`, `JumpDuration`, `JumpCooldown`.

## 일정 (데이터, 2026-09-06 확정)

| 구간 | 도착 시각 | 조각 | `↑` 힌트 | 근거 |
| --- | --- | --- | --- | --- |
| 진행 1 (25초) | **5초** | `Ledge1` (`ledges[0]`) | ○ | 기획 답변 「진행1 5초 추가」. 튜토리얼 성격이라 처음 나오는 이 조각에만 힌트가 붙는다 |
| 진행 1 (25초) | **12.5초** | `Ledge2` (`ledges[1]`) | | 기존 값 유지 |
| 진행 2 (30초) | **15초** | `Ledge3` (`ledges[2]`) | | 기존 값 유지 |
| 진행 3 (35초) | 없음 | | | |
| 진행 4 (15초) | 없음 | | | |

### 5초 단차가 구간 앞머리를 먹는다 (의도된 것)

진입 2.925초 + 접근 2초가 도착 시각 앞에 붙으므로 5초 단차는 **2.075초에 화면 오른쪽에서 들어오기 시작하고 3.0초부터 장애물 스폰이 멈춘다**. 구간 시작 유예(`sectionStartGrace` 2초)와 조작 힌트(2.5초)가 끝나는 지점과 겹치므로, 실질적으로 진행 1은 「조작 힌트 → 바로 첫 단차」로 시작한다. 기획 답변이 이 단차를 튜토리얼로 뒀으므로 그대로 둔다.

### 5초 조각은 12.5초 조각이 필요해지기 전에 퇴장한다

한 조각의 수명은 도착 시각 + 약 3.95초다(퇴장 조건 `FrontX ≤ retireX`(−20): `(playerX − retireX) / scrollSpeed = 15.8 / 4 = 3.95`초. 상승 0.7초·배경 복귀 1.2초는 그 안에 끝난다). 막혀 있는 동안은 구간 시각이 멈추므로 이 값은 플레이와 무관하게 고정이다.

```text
5초 조각   도착 5.0  → Done 8.95
12.5초 조각 등장 9.575 (StartTime)
여유 0.625초
```

그래서 두 조각이 동시에 화면에 있지 않고 `LedgeDirector`는 **항상 하나만 활성**으로 둔다. 이 여유가 사라지면(예: 기획이 5초를 6초 이후로 옮기면) 뒤 조각은 등장 시각을 놓치고 `LedgeDirector`가 경고를 남기며 건너뛴다(`sectionElapsed > ApproachTime`). 그때는 조각 재사용이 아니라 **도착 시각을 벌리는 쪽**으로 고친다.

### `LedgeDirector`의 구간 시계

`BeginSection`이 그 구간의 항목을 전부 모아 도착 시각 순으로 큐를 만든다. `Tick`은 다음 순서로 돈다.

1. `sectionElapsed`를 `deltaTime × WorldSpeedScale`만큼 올린다. QTE 중이면 배율이 `qteWorldSpeedScale`(0.12)이라 구간 시각도 그만큼 느리게 간다. `WorldSpeedScale`은 **`Advance` 전에** 정해서 `PlayFlow`가 같은 프레임에 같은 값을 쓰게 한다.
2. 활성 조각이 없고 큐의 다음 항목이 `sectionElapsed ≥ StartTime`이면 그 조각을 `Schedule(plan, data, showHint, startTime: sectionElapsed)`로 켠다. `LedgeClock`은 `time`을 0이 아니라 **구간 시각으로 프라이밍**해서 시작하므로, 도착은 항상 정확히 `ledgeTime`이다. 켠 프레임에는 `Advance(0)`만 해서 같은 `deltaTime`을 두 번 세지 않는다.
3. `Advance` 뒤 `sectionElapsed = active.Time`으로 되맞춘다.
4. 조각이 `Done`/`Failed`면 퇴장시키고 큐를 한 칸 민다.

`PlayFlow.sectionTime`도 같은 배율로 흐르므로 둘은 사실상 같은 시각을 본다(감속 시작·해제 프레임에서 최대 한 프레임만큼 어긋날 수 있으나 0.06 unit 수준이다).

## 일정 계산 (`LedgePlan`)

```text
travelTime = (spawnX − playerX) / scrollSpeed        7.5 −(−4.2) = 11.7 → 2.925초
lead       = max(approachSafeTime, travelTime)       max(2.0, 2.925) = 2.925초
StartTime  = max(0, ledgeTime − lead)                12.5 − 2.925 = 9.575초
StartX     = playerX + scrollSpeed × (ledgeTime − StartTime)   = 7.5
ApproachTime = max(StartTime, ledgeTime − approachSafeTime)    = 10.5초
FrontXAt(t) = StartX − scrollSpeed × (t − StartTime)
```

`lead`에 `travelTime`을 넣기 때문에 접근 안내(2초)보다 등장이 먼저다. 반대로 `approachSafeTime`이 진입 시간보다 길면 `StartX`가 그만큼 더 오른쪽으로 밀려 **도착은 언제나 정확히 `ledgeTime`**이다(`FrontXAt(ledgeTime) == playerX`, 테스트 `Plan_FrontReachesPlayer_ExactlyAtLedgeTime`).

앞면 위치는 스크롤을 적분하지 않고 매 틱 위 식으로 다시 계산한다. 오차가 쌓이지 않고, `Blocked` 동안 시각이 멈추므로 단차도 저절로 제자리에 선다.

## 이벤트 순서

아래 시각은 **단차 시계**(`sectionElapsed`) 기준이다. QTE 중에는 이 시계가 실시간의 0.12배로 흐른다.

QTE 성공:

```text
BeginSection(1, 25)
 t=9.575   Spawned      단차 활성, x 7.5에서 진입
 t=10.5    Approaching  → 장애물 스폰 중단
 t=11.875  QteStarted   앞면이 playerX + 2.5 도달 → 월드 0.12배, ↑↓ 교대 6회 요구
           (실제 시간으로 약 5.2초 창. 남은 시간 막대가 줄어든다)
 t≈12.1    (6회 완료)    ForceJump: 상단 레인 스냅 + 쿨타임 무시 점프
 t≈12.1+   Resumed      공중 감지 → Passing, 월드 속도 1로 복귀
 t=12.5+   Cleared      착지 + 앞면 통과 → 상승 시작
 t=+0.7    Finished     → 장애물 스폰 재개, 배경 복귀 시작
 t=+3.5    Retired      비활성
```

QTE 실패(벽 충돌):

```text
 t=11.875  QteStarted
 t=12.5    Failed       앞면 x = playerX. PlayFlow.ReportImpact()
                        → Hit 0.1초 정지 + 흔들림 + 왼쪽 밀림 → Fail() → 결과 패널
```

직접 점프(QTE 창 전에 뛴 경우):

```text
 t<11.875  (공중)       QTE를 열지 않는다
 t=12.5    (도착, 공중)  단차가 플레이어 아래를 지나감 → Cleared / Finished / Retired
```

이벤트는 전부 `Action<int>`이고 인자는 구간 번호다. 통합은 이벤트를 안 써도 되고 `IsQteActive`/`WorldSpeedScale`/`IsSpawnSuspended`만 매 프레임 읽어도 된다(권장). `Failed`만은 구독해야 실패가 흐름에 전달된다.

## 단차 모양 (긴 돌, 2026-09-06 사용자 결정)

한때 「작은 폭포」였으나 **사용자 결정으로 「긴 돌」이 되었다.** 단차 앞면은 낙수 띠가 아니라 맨 아래 레인을 꽉 채우는 바위 한 덩이다. `Cascade`·`Foam` 오브젝트와 거기 붙어 있던 `WaterInteractor`는 프리팹에서 **삭제했다**(각 조각마다 2개씩, 세 조각 합쳐 6개). 판정과 일정은 하나도 바뀌지 않았다 — 막힘·QTE는 여전히 물리가 아니라 `LedgeClock`이 정한다.

| 오브젝트 | 스프라이트 | 크기(unit) | 중심(로컬) | 정렬 | 뜻 |
| --- | --- | --- | --- | --- | --- |
| `UpperBed` | `Placeholder/Env_Riverbed.png` | Tiled 30 × 5 | (15, −2.75, z −0.005) | 7 | 위쪽 강바닥(모래) |
| `Step` (옛 `Cascade` 자리) | **`Art/오브젝트/긴돌.png`** | Simple **0.9772 × 1.1** (균등 스케일 **0.1412067**) | (**0.4886**, **−1.1**, z **−0.01**) | 7 | 단차 앞면의 긴 돌. 윗변 **−0.55**, 아랫변 **−1.65** |
| `Hint` | `Ledge/Ledge_Hint.png` | 1.0 × 1.2 | (−2.2, 0.6) | −4 | `↑`. 맨 처음 조각에만 연결 |

- **크기 계산.** `긴돌.png`의 알파 박스는 **692 × 779 px**(1920×1080 캔버스)이고 pivot은 알파 박스 중심 `(0.6557292, 0.36064816)`이다(`ObjectArtPostprocessor`가 임포트마다 다시 건다 — `Docs/Animation.md` 「후처리기 범위」). 높이를 `stepHeight`(1.1)에 맞추면 균등 스케일이 `1.1 / (779/100) = 0.1412067`이고 폭은 `(692/100) × 0.1412067 = 0.9772` unit이다. pivot이 알파 박스 중심이라 오브젝트 원점이 곧 돌의 중심이고, 중심 y를 **맨 아래 레인 중심 −1.1**에 두면 윗변 −0.55(= 위쪽 강바닥 윗선)와 아랫변 −1.65(= 아래 강바닥 윗선)가 저절로 맞는다. 맨 아래 레인 장애물 `Rock`과 같은 규칙이다(`Docs/Spawner.md` 「돌은 높이 기준으로 맞춘다」).
- **가로 위치.** 로컬 원점 x = 0이 단차 앞면이므로 돌의 왼쪽 변이 0에 오도록 중심을 폭의 절반(0.4886)만큼 오른쪽에 둔다. 옛 `Cascade`(중심 0.6, 폭 1.2)와 같은 자리다. 폭 0.9772는 `clearMargin`(1.2)보다 0.22 좁다 — 판정에는 영향이 없지만(막힘은 로직) 눈으로는 돌이 조금 얇게 보인다. 더 넓은 그림이 필요하면 `Docs/ArtRequestList.md` 4번.
- **정렬과 z.** `UpperBed`와 같은 정렬 7이고 z만 −0.01로 더 앞이라 위쪽 강바닥보다 앞에 그려진다. 이 프리팹이 이미 쓰는 방식이다(`UpperBed`의 z −0.005도 환경 강바닥 z 0보다 앞이라는 뜻이다). 정렬 값을 따로 두지 않으므로 `UpperBed` 정렬을 바꾸면 돌도 같은 대역에서 따라간다.
- **`WaterInteractor`가 하나도 없다.** 낙수 띠·거품이 사라져 단차가 수면을 세로로 관통하지 않는다. 돌은 윗변이 −0.55라 수면(2.0)보다 2.55 아래이므로 항적을 만들 이유가 없다. 예전에 `wakeOnly`·`wakeScale`로 눌러 두었던 「층이 바뀔 때 수면이 요동치던」 문제(`Docs/Environment.md`)도 원인이 통째로 없어졌다. `UpperBed`·`Hint`에는 원래 없었다.
- **`UpperBed` 중심 y가 −2.85 → −2.75로 바뀌어 있다**(사용자가 손으로 조정 중인 값이다). 그러면 모래 윗선이 −0.45가 되어 돌 윗변(−0.55)보다 0.1 높다. 돌이 앞에 그려지므로 틈이 보이지는 않지만, 둘을 정확히 맞추려면 `UpperBed` 중심을 −2.85로 되돌리거나 `Step` 중심 y를 −1.0으로 올린다.
- `Placeholder/Ledge_Cascade.png`·`Ledge_Foam.png`는 이제 아무 데서도 참조하지 않는다(`Ledge/Ledge_Rock.png`·`Ledge_RockEdge.png`와 같은 처지다). 파일은 지우지 않고 남겨 뒀다.

### 강바닥 맞춤

환경에 강바닥 층(`RiverbedLayer`, 모래, 윗선 y **−1.65**)이 있어 단차의 위쪽 강바닥을 거기에 맞춘다. `Docs/Environment.md` 「강바닥」을 같이 본다.

```text
넘기 전   모래 윗선 −0.55  (= 평소 −1.65 + stepHeight 1.1), 앞면에 긴 돌(−1.65 ~ −0.55)
하강      LedgeScrollModule이 조각 전체를 0.7초 동안 1.1 내린다
넘은 뒤   모래 윗선 −1.65  = 환경 RiverbedLayer 윗선. 정확히 이어진다
```

- `UpperBed`는 소스가 400×500 px(4.0 × 5.0 unit)라 `m_Size` 30 × 5로 **세로 반복이 정확히 1회**다. 모래 실루엣의 평균 윗선이 스프라이트 윗변에서 0.20 아래에 있다.
- **아래쪽 강바닥은 조각에 없다.** 앞면(로컬 x 0) 왼쪽은 환경 `RiverbedLayer`가 그대로 맡는다. `UpperBed`는 로컬 x 0 ~ 30이라 언제나 앞면 오른쪽만 덮는다.
- 그래서 화면은 「모래(−1.65) → 긴 돌 → 모래(−0.55)」로 읽히고, 넘고 나면 오른쪽 모래가 내려와 왼쪽과 한 장이 된다.

## 생성기

> **생성기는 얼어 있다 (2026-09-06).** 에디터가 열린 상태에서 작업했기 때문에 이번 변경(단차 3개, 폭포 모양, `ledgeEntries`)은 `LedgeSet.prefab`과 `LedgeData.asset`의 **YAML을 직접 고쳐서** 넣었고 `LedgeSetup`은 **손대지 않았다**. 그래서 `LedgeSetup`은 아직 「바위 계단 벽 + 단차 2개」를 만든다.
>
> - `Team1004 > Generate Ledge Assets`(`Generate(false)`)는 프리팹이 이미 있으면 경고만 남기므로 **안전하다**. 코어루프 생성기가 부르는 것도 이 형태다.
> - **`Regenerate Ledge Prefab (Overwrite)`와 `Regenerate Core Loop Scenes (Overwrite)`(내부에서 `LedgeSetup.Generate(true)`)를 돌리면 이번 작업이 전부 사라진다.** 돌려야 한다면 먼저 `LedgeSetup.BuildPrefab`/`CreateLedge`를 아래 트리(조각 3개, `Step`에 `긴돌.png` 균등 스케일)에 맞춰 고친 뒤에 돌린다. `LedgeData.asset`은 「없을 때만」 만들므로 보존된다.

| 메뉴 | 메서드 | 동작 |
| --- | --- | --- |
| `Team1004 > Generate Ledge Assets` | `Game.Ledge.Editor.LedgeSetup.Generate()` | 없는 것만 만든다. 프리팹이 있으면 경고만 |
| `Team1004 > Regenerate Ledge Prefab (Overwrite)` | `LedgeSetup.RegeneratePrefab()` | 확인 창(배치에서는 건너뜀) 후 `Generate(true)` |
| — | `LedgeSetup.Generate(bool overwrite)` | 코어루프 생성기가 부르는 형태 |

경로 상수: `LedgeSetup.PrefabPath`, `LedgeSetup.DataPath`.

만드는 것:

| 경로 | 내용 |
| --- | --- |
| `Assets/GameAssets/Ledge/Ledge_Rock.png` | 128×64 바위 무늬. 가로 이음매 없음, Wrap Repeat, FullRect(Tiled용). 위쪽 강바닥에 그대로 쓴다 |
| `Assets/GameAssets/Ledge/Ledge_RockEdge.png` | 128×64 윗면이 밝은 바위. **더 이상 쓰지 않는다**(계단 벽 → 폭포). 파일은 남아 있다 |
| `Assets/GameAssets/Ledge/Ledge_Hint.png` | 64×64 `↑` (알파 0.82) |
| `Assets/GameAssets/Design/Ledge/LedgeData.asset` | 위 데이터 표의 기본값. **없을 때만** 만들며 값은 보존된다 |
| `Assets/GameAssets/Ledge/LedgeSet.prefab` | 아래 트리 |

현재 프리팹(손으로 넣은 것. 생성기 출력이 아니다):

```text
LedgeSet (LedgeDirector: data, ledges[3], environment=null, player=null)
  Ledge1 (LedgeThing, 비활성, parkX 60)          구간 1 · 5초 (힌트 있음)
    UpperBed  Tiled 30 × 5, 중심 (15, -2.75, z -0.005), 정렬 7    위쪽 강바닥(모래)
    Hint      1.0 × 1.2, (-2.2, 0.6), 정렬 -4                    ↑ (Ledge1만 연결)
    Step      Simple 0.9772 × 1.1, 중심 (0.4886, -1.1, z -0.01), 정렬 7, 균등 스케일 0.1412067
              긴돌.png. 윗변 -0.55 / 아랫변 -1.65. WaterInteractor 없음
  Ledge2 (LedgeThing, 비활성)   같은 구성, Hint 미연결      구간 1 · 12.5초
  Ledge3 (LedgeThing, 비활성)   같은 구성, Hint 미연결      구간 2 · 15초
```

`ledges[i]`는 `LedgeData.ledgeEntries[i]`와 1:1이다. 항목을 늘리면 조각도 같은 수만큼 있어야 한다(없으면 경고 후 그 항목만 건너뛴다).

오브젝트 로컬 원점 x = 0이 **단차 앞면(긴 돌의 왼쪽 변)**이다. `LedgeScrollModule`이 `localPosition.x = FrontX`를 그대로 쓴다. `environment`·`player`는 씬 참조라 프리팹에서 비고 코어루프 생성기가 채운다(비어 있으면 `Start`에서 `FindAnyObjectByType`으로 대체하고 경고를 남긴다).

정렬: `UpperBed`와 `Step`은 같은 정렬 **7**이고 z(-0.005 / -0.01)로 앞뒤가 갈린다. `Hint`는 -4로 배경 층과 물 사이다. 정렬 7은 사용자가 손으로 올린 값이라(예전에는 -5) 지금은 단차가 플레이어·장애물(0)보다 **앞에** 그려진다. 되돌리려면 두 값을 함께 내린다 — `Step`은 `UpperBed`와 같은 값이어야 강바닥에 묻히지 않는다.

## 통합 절차

**통합 완료(2026-09-06, roy0206 통합 패스).** 아래 1~4 전부 반영했고 2-(h) 스포너 정지 API(`ObstacleSpawner.SetSpawningEnabled`)도 들어갔다. 추가로 `PlayFlow.PrepareDebugJump`(디버그 단축키)가 `LedgeHandler.Stop()`을 부르고, `PlayFlow.Update`는 `Tick` 직후 상태가 `Running`이 아니면(`Failed` 이벤트로 `Fail()`이 불린 경우) 바로 돌아간다. `Game.Play.Tests`가 `Game.Ledge`를 참조하고 `FullLoopTests`가 단차가 있는 구간(1·2) 전부의 `Cleared`/`Finished`를 확인한다. 아래는 그때 넣은 줄의 기록이다.

`Game.Ledge`는 스스로 `PlayFlow`에 붙지 못한다(참조 방향이 반대면 순환이다). 아래는 통합 담당이 **타인 소유 파일**에 넣을 줄이다.

### 1. `Assets/Scripts/Play/Game.Play.asmdef`

`"references"` 배열에 추가:

```json
        "Game.Ledge",
```

### 2. `Assets/Scripts/Play/PlayFlow.cs`

(a) using 추가:

```csharp
using Game.Ledge;
```

(b) `[SerializeField] private EnvironmentThing environment;` 다음 줄:

```csharp
        [SerializeField] private LedgeDirector ledgeDirector;
```

(c) `public EnvironmentThing Environment => environment;` 근처에:

```csharp
        public LedgeDirector Ledge => ledgeDirector;
        public ILedgeHandler LedgeHandler { get; set; }
```

(d) `Start()`의 `SetState(PlayState.Ready);` **앞**:

```csharp
            if (LedgeHandler == null && ledgeDirector != null)
                LedgeHandler = ledgeDirector;

            if (LedgeHandler != null)
                LedgeHandler.Failed += OnLedgeFailed;
```

그리고 `OnUnregistering()`에:

```csharp
            if (LedgeHandler != null)
                LedgeHandler.Failed -= OnLedgeFailed;
```

새 메서드(`OnHit` 근처):

```csharp
        private void OnLedgeFailed(int section)
        {
            ReportImpact();
        }
```

`Fail()`이 아니라 `ReportImpact()`다. 마지막 폭포(`Docs/Boss.md`)와 같은 길로 들어가 0.1초 정지 + 흔들림 + 왼쪽 밀림을 거친 뒤 `Fail()`이 불린다(2026-09-06 QTE 전환).

(e) `Update()`를 아래로 바꾼다. **`Tick`이 먼저**여야 같은 프레임의 `WorldSpeedScale`이 최신이다.

```csharp
        private void Update()
        {
            if (State != PlayState.Running)
                return;

            var ledge = LedgeHandler;

            if (ledge != null)
                ledge.Tick(Time.deltaTime);

            if (State != PlayState.Running)
                return;

            var scale = ledge != null ? Mathf.Max(0f, ledge.WorldSpeedScale) : 1f;
            var moving = scale > 0f;

            ApplyWorldSpeedScale(scale, moving);   // scroller.SetSpeedScale / environment.Speed

            if (!moving)
                return;

            var step = Time.deltaTime * scale;

            Distance += GameConfig.Current.ScrollSpeed * step;
            sectionTime += step;

            if (spawnerReady && player != null)
            {
                spawner.SetSpawningEnabled(ledge == null || !ledge.IsSpawnSuspended);

                var playerState = SpawnPlayerState.FromPlayer(
                    player, player.JumpCooldownRemaining, player.AirborneRemaining, player.MoveRemaining);
                spawner.Advance(step, Distance, playerState);
            }
        }
```

**같은 배율을 다섯 곳에 똑같이 먹이는 것이 핵심이다**: `StageScroller.SetSpeedScale`(장애물이 얹혀 있는 스크롤 루트), `EnvironmentThing.Speed`(배경 층 + 강바닥), 단차 조각(`LedgeDirector`가 자기 `Tick`에서), `Distance`/`sectionTime`, `spawner.Advance`의 dt. 하나라도 빠지면 QTE 중에 배경만 흐르거나 HUD 남은 초만 정상 속도로 준다. `Running`이 아닌 상태로 나갈 때 `PlayFlow.SetState`가 `ResetWorldSpeedScale()`로 1로 되돌린다.

(f) `RunSectionAsync(int section)`의 `spawner.BeginSectionByDuration(section - 1, sectionDuration);` 다음 줄:

```csharp
            LedgeHandler?.BeginSection(section, sectionDuration);
```

그리고 같은 메서드의 `while (this != null && !IsTerminal && !IsSectionFinished()) ...` 루프 **다음**:

```csharp
            LedgeHandler?.Stop();
```

(g) `Fail()`의 `spawner.Stop();` 옆:

```csharp
            LedgeHandler?.Stop();
```

`Retry()`는 씬을 다시 로드하므로 별도 처리가 없다. `Stop()`은 단차를 화면 밖으로 되돌리고 배경 오프셋을 0으로 돌린다.

(h) 스포너 정지(선택, `Docs/Requests.md` 항목이 처리된 뒤). `spawner.Advance` 바로 앞:

```csharp
                spawner.SetSpawningEnabled(ledge == null || !ledge.IsSpawnSuspended);
```

이 API는 통합 패스에서 추가됐다(`SpawnModule.SpawningEnabled`, 구간 시작 시 true로 복귀). `spawner.Stop()`으로 대신하면 안 된다. `Stop()`은 `Advance`를 통째로 막아 이미 떠 있는 장애물이 그 자리에 얼어붙고 회수되지 않는다(`SpawnModule.Advance`가 `MoveAndRecycle`을 겸한다).

(i) `ILedgeWorld`를 쓰는 대안. `PlayFlow`가 `ILedgeWorld`를 구현하고 `Start`에서 `ledgeDirector.World = this;`를 하면 (e)의 polling 대신 상태가 바뀔 때만 호출이 온다. 스포너에 `Pause/Resume`이 생기면 그쪽이 더 깔끔하다.

### 3. `Assets/Scripts/Bootstrap/Editor/Game.Bootstrap.Editor.asmdef`

```json
        "Game.Ledge",
        "Game.Ledge.Editor",
```

### 4. `Assets/Scripts/Bootstrap/Editor/CoreLoopSetup.cs`

(a) using:

```csharp
using Game.Ledge;
using Game.Ledge.Editor;
```

(b) 경로 상수(`BossSetPrefabPath` 근처):

```csharp
        private const string LedgeSetPrefabPath = LedgeSetup.PrefabPath;
```

(c) `EnsureEnvironmentAssets();` 다음 줄(에셋 준비 구간):

```csharp
            LedgeSetup.Generate(false);
```

(d) Play 씬 빌드에서 `InstantiateBossSet(camera);` 다음 줄:

```csharp
            var ledgeDirector = InstantiateLedgeSet(environment, playerThing);
```

`playerThing`은 이미 만들어 둔 `LanePlayer` 인스턴스다(이름이 다르면 그 변수를 쓴다).

(e) 새 메서드:

```csharp
        private static LedgeDirector InstantiateLedgeSet(EnvironmentThing environment, LanePlayer player)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LedgeSetPrefabPath);

            if (prefab == null)
            {
                Debug.LogWarning($"[CoreLoopSetup] '{LedgeSetPrefabPath}' is missing. Upstream ledges are skipped until it exists.");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(null, false);
            instance.transform.localPosition = Vector3.zero;

            var director = instance.GetComponent<LedgeDirector>();

            if (director == null)
                return null;

            var serialized = new SerializedObject(director);
            serialized.FindProperty("environment").objectReferenceValue = environment;
            serialized.FindProperty("player").objectReferenceValue = player;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return director;
        }
```

(f) `PlayFlow` 직렬화 연결에서 `flowSerialized.FindProperty("environment").objectReferenceValue = environment;` 다음 줄:

```csharp
            flowSerialized.FindProperty("ledgeDirector").objectReferenceValue = ledgeDirector;
```

`LedgeSet.prefab`은 `ScrollRoot` **밖**, Play 씬 루트에 원점으로 둔다(`BossSet`과 같다). 스크롤 루트 안에 넣으면 `ScrollModule`이 x를 한 번 더 밀어 이중으로 움직인다.

### 5. 문서

`Docs/CoreLoop.md` 「다른 에이전트용 훅」에 한 줄, 「통합 지도」에 `LedgeSet` 행을 추가한다(통합 담당).

## 검증

| 단계 | 명령 | 결과 |
| --- | --- | --- |
| 오프라인 컴파일 | `python <scratchpad>/csccheck_all.py` | `assemblies compiled: 35 failed: 0`. `Game.Ledge`(12), `Game.Ledge.Editor`(2), `Game.Ledge.Tests`(1) |
| 생성 | `Unity.exe -batchmode -nographics -projectPath <프로젝트> -executeMethod Game.Ledge.Editor.LedgeSetup.Generate -quit -logFile Logs/batch_ledge.log` | `[LedgeSetup] Ledge assets are ready`, PNG 3장 + 데이터 에셋 + 프리팹 |
| EditMode | `... -runTests -testPlatform EditMode -testFilter "Game.Ledge.Tests" -testResults Logs/editmode_ledge.xml` (`-quit` 없이) | 17개 통과 |

작성 시에는 오프라인 컴파일만 확인했다. 통합 패스(2026-09-06)에서 `CoreLoopSetup.Regenerate`가 `LedgeSetup.Generate(true)`를 불러 PNG 3장·프리팹을 만들었고(`Logs/int_regen.log`의 `[LedgeSetup] Ledge assets are ready`), EditMode 전체(267개, `Game.Ledge.Tests` 17개 포함)와 PlayMode `FullLoopTests`(8배속에서 구간 1·2 단차를 막힘 → 재점프로 통과)가 통과했다. 결과는 `Docs/CoreLoop.md` 「통합 검증 결과 (2026-09-06)」.

### 2026-09-06 기획 답변 반영 패스

에디터가 열려 있어 배치(생성기·Unity 테스트)를 돌리지 않았다. 확인한 것은 오프라인 Roslyn 컴파일뿐이다.

| 실행 | 결과 |
| --- | --- |
| `python <scratchpad>/csccheck_all.py` | `assemblies compiled: 36  failed: 0` |

손으로 고친 것(생성기를 돌리지 않았다): `Assets/GameAssets/Design/Ledge/LedgeData.asset`(`ledgeTimes` → `ledgeEntries`), `Assets/GameAssets/Ledge/LedgeSet.prefab`(`StepWall` → `Cascade`, `Foam` 추가, `Ledge3` 추가, `ledges` 3개), `Assets/GameAssets/Placeholder/Ledge_Cascade.png`·`Ledge_Foam.png`(+ 손으로 쓴 `.meta`). `Assets/Scenes/Play.unity`는 건드리지 않았다(`LedgeSet` 인스턴스가 `player`·`environment`만 덮어쓰므로 프리팹 변경이 그대로 따라간다).

### 2026-09-06 긴 돌 패스 (아트 갱신, 가장 최근)

사용자 결정으로 모양이 「작은 폭포」 → 「긴 돌」이 되어 `LedgeSet.prefab` YAML을 손으로 다시 고쳤다(생성기는 여전히 얼어 있다).

| 한 일 | 내용 |
| --- | --- |
| 삭제 | 조각 3개의 `Cascade`·`Foam` GameObject 6개와 거기 붙어 있던 `Transform`·`SpriteRenderer`·`WaterInteractor` 블록 전부(24블록). 부모 `m_Children`에서도 뺐다 |
| 추가 | 조각마다 `Step`(GameObject + Transform + SpriteRenderer) 하나. fileID는 `7412903355618240011~13` / `…21~23` / `…31~33` |
| 스프라이트 | `Art/오브젝트/긴돌.png`(GUID `6393dc434a55a5f42b3729e235ad3cfe`), `{fileID: 21300000}` |
| 검증 | 앵커 39개 전부 유일, 로컬 `fileID` 참조 미아 0, `m_Children` ↔ `m_Father` 왕복 일치, `Play.unity`의 `LedgeSet` 인스턴스가 지워진 오브젝트를 참조하지 않음(`m_Modifications`는 루트 transform·이름뿐) |

`LedgeThing`이 참조하는 자식은 `hint` 하나뿐이라 코드 변경은 없었다. `Game.Ledge`의 asmdef·C#도 그대로다.

에디터에서 확인할 것: 프리팹 인스턴스의 조각 3개에 `Cascade`·`Foam`이 사라지고 `Step`이 하나씩 보이는지, `Step`의 스프라이트가 `긴돌`이고 스케일이 0.1412067인지, `LedgeDirector.ledges`가 3칸인지, `LedgeData` 인스펙터에 항목 3개가 보이는지, EditMode `Game.Ledge.Tests`, PlayMode `FullLoopTests`.

### 2026-09-06 QTE 전환 패스

에디터가 열려 있어 배치(생성기·Unity 테스트)를 돌리지 않았다. 확인한 것은 오프라인 Roslyn 컴파일뿐이다.

| 실행 | 결과 |
| --- | --- |
| `python <scratchpad>/csccheck_all.py` | `assemblies compiled: 40  failed: 0` (`Game.Qte` 6, `Game.Qte.Tests` 1 포함) |

손으로 고친 것: `Assets/GameAssets/Design/Qte/QteData.asset`(+ `.meta`, 새로 만듦), `Assets/GameAssets/Design/Ledge/LedgeData.asset`(`qteData`·`qteStartDistance`·`qteWorldSpeedScale` 세 줄 추가), `Assets/Scenes/Play.unity`(HUD `QtePanel` 오브젝트와 `PlayHud.qtePanel` 연결). `LedgeSet.prefab`은 건드리지 않았다 — `LedgeDirector.qtePanel`은 비워 두고 `QtePanel.TryGetCurrent`로 런타임에 찾는다(프리팹이 씬 오브젝트를 참조할 수 없다).

에디터에서 확인할 것: `LedgeData` 인스펙터에 `Qte Data`·`Qte Start Distance`·`Qte World Speed Scale`이 보이는지, `HudCanvas/QtePanel`이 계층에 있고 `PlayHud.qtePanel`이 연결됐는지, EditMode `Game.Qte.Tests` 17개 + `Game.Ledge.Tests` 23개, PlayMode `FullLoopTests`(QTE를 ↑↓ 교대로 푸는 쪽으로 고쳤다).

### 수동 플레이 체크 (기획서 24절)

1. 구간 1에서 **3.0초쯤** 장애물이 끊기고 오른쪽에서 **긴 돌**이 들어와 5.0초에 도착하는가. 조작 힌트(2.5초)와 겹쳐도 읽히는가.
2. 배경 `↑`가 **첫 단차에만** 보이고 HUD 팝업처럼 보이지 않는가.
3. 1번 레인에서 점프하면 단차를 넘는가(2레인 점프가 1레인 단차보다 확실히 높은가).
4. 넘은 뒤 0.7초 동안 위쪽 강바닥이 부드럽게 내려와 평소 강바닥과 이어지는가. 화면이 순간적으로 튀지 않는가.
5. **QTE**: 폭포 앞면이 2.5 unit(도착 0.625초 전)까지 오면 세계가 **멈추지 않고 아주 느려지는가**(배경·물결·강바닥·장애물·HUD 남은 초가 **같이** 느려지는가. 하나만 정상 속도면 배율이 빠진 곳이다). 화면에 큰 `↑`와 점 6개, 줄어드는 시간 막대가 뜨는가.
5-1. **QTE 입력**: ↑↓를 번갈아 6번 누르면 점이 하나씩 차고 화살표가 번쩍이며 방향이 바뀌는가. **같은 키를 두 번 누르면 아무 일도 없는가**(진행도 시간도 그대로). QTE 중 ↑↓로 레인이 움직이거나 점프가 나가지 않는가.
5-2. **QTE 성공**: 6번째에 연어가 곧바로 1번 레인으로 올라가 점프해 폭포를 넘는가. 그 순간 세계가 정상 속도로 돌아오는가. 이후 상승·배경 복귀가 예전과 같은가.
5-3. **QTE 실패**: 아무것도 누르지 않으면 시간 막대가 다 줄고 폭포가 연어에 닿으면서 **일반 피격 연출(정지·흔들림·왼쪽 밀림) 뒤 실패 패널**이 뜨는가. R로 구간 처음부터 다시 되는가.
5-4. **QTE 창 전 직접 점프**: 폭포가 2.5 unit에 들어오기 전에 미리 점프하면 QTE가 뜨지 않고 그대로 넘어가는가.
6. **같은 구간의 두 번째 단차(12.5초)가 첫 조각이 완전히 빠져나간 뒤에 들어오는가**(둘이 동시에 보이면 일정이 어긋난 것이다). 힌트가 없는가.
7. 구간 2의 단차(15초)는 힌트 없이 이해되는가.
8. 단차 중 Esc → 전부 멈추는가. 실패 후 재도전 → 단차가 화면 밖으로 돌아가고 처음부터 다시 나오는가.
9. 구간 3·4에는 단차가 나오지 않는가. 보스·컷신 중에는 나오지 않는가.
10. **긴 돌**: 앞면의 돌이 맨 아래 레인을 정확히 채우는가(윗변이 위쪽 모래 윗선과, 아랫변이 아래 모래 윗선과 맞는가). 위쪽 강바닥에 묻히지 않고 앞에 그려지는가. 폭이 너무 얇아 보이지 않는가.
10-1. **강바닥**: 단차 앞에서 모래가 두 단(−1.65 / −0.55)으로 보이는가. 넘은 뒤 0.7초에 두 단이 한 줄로 합쳐지는가. 합쳐진 뒤 이음매나 무늬 겹침이 보이는가.
11. **수면**: 단차가 지나갈 때 수면이 **더 이상 반응하지 않는다**(`Cascade`·`Foam`과 그 `WaterInteractor`를 지웠다). 넘은 뒤 0.7초 하강에서 수면이 튀지 않는지 다시 확인한다 — 이 구간이 예전 요동의 원인이었다.

## 확신 없는 지점

- **배경 세로 오프셋을 영구로 두지 않았다.** 지시는 "환경을 1.1 내린다"였지만, `NearLayer`(강바닥, y −3.8~−2.2)를 영구히 1.1 내리면 단차가 지나간 뒤 강바닥이 위쪽 강바닥보다 1.1 낮아 이음매가 보이고, 단차를 여러 번 넘으면 강바닥이 화면(−3.6) 밖으로 나간다. 기획서 05절이 "상류에서도 같은 사이드뷰·3레인"이라고 못 박았으므로 넘기 전후 화면이 같아야 한다고 읽었다. 그래서 영구 오프셋 대신 단차 조각만 1.1 내리고 배경은 0.35만 내렸다 돌아오게 했다. `EnvironmentThing.SetVerticalOffset` / `RaiseWorldAsync`는 요청대로 만들어 두었으므로, 영구 이동을 원하면 `environmentRiseAmount`를 1.1로 올리고 복귀를 끄면 된다(`environmentSettleDuration`을 0보다 크게 두면 반드시 돌아온다).
- **긴 돌에 콜라이더가 없다.** 막힘·QTE는 물리가 아니라 로직이다. 지금은 `Step`·`UpperBed` 정렬이 7이라 오히려 플레이어(0)가 돌 **뒤에** 그려진다. 예전 정렬(-5/-4)로 되돌리면 반대로 플레이어가 돌 앞에 겹쳐 보인다. 어느 쪽으로 갈지는 눈으로 정할 자리다.
- **너무 일찍 뛴 경우가 이제 실패다.** 도착 직전에 뛰어 도착 직후 착지하면 앞면이 아직 `playerX − clearMargin` 안이라 `Failed`가 된다(예전에는 다시 막혔다). QTE 성공 점프는 도착까지 최대 0.625초 남은 시점에 나가고 체공이 1.6초이므로 **정상 경로에서는 나올 수 없다**. 손으로 미리 뛴 경우에만 걸린다.
- **QTE 창 길이 5.2초.** `qteStartDistance` 2.5 ÷ 스크롤 4 = 0.625초(단차 시계) ÷ `qteWorldSpeedScale` 0.12 = **약 5.2초**(실시간)다. 6회 교대를 넣기에는 넉넉하고, 아무것도 안 하면 5.2초 동안 벽이 천천히 다가와 부딪히는 그림이 된다. 더 급하게 하려면 `qteWorldSpeedScale`을 올리거나(0.2 → 3.1초) `requiredPresses`를 늘린다.
- **감속 배율이 `GameConfig.ScrollSpeed`를 바꾸지 않는다.** `StageScroller.SpeedScale`과 `EnvironmentThing.Speed`(= `ScrollSpeed × scale`)로만 건다. `GameConfig.Changed`가 QTE 중에 오면 `EnvironmentThing.OnConfigChanged`가 `Speed`를 원래대로 되돌리는데, 다음 프레임에 `PlayFlow`가 다시 덮으므로 한 프레임만 튄다. 개발자 설정 패널을 QTE 중에 여는 경우뿐이라 그대로 뒀다.
- **감속 시작·해제 프레임의 한 프레임 어긋남.** `LedgeDirector`는 `Advance` **전에** 배율을 정하고 `PlayFlow`는 `Tick` **후에** 그 값을 읽으므로 둘은 같은 값을 쓴다. 다만 위상이 바뀌는 프레임에서는 새 위상이 아니라 옛 위상의 배율로 한 프레임이 지나간다(최대 0.06 unit).
- **배경 `↑` 힌트와 QTE 화살표가 둘 다 나온다.** 첫 단차에서 월드 배경의 `↑`(단차 왼쪽 2.2 unit)와 HUD의 큰 `↑`가 동시에 보인다. 배경 쪽은 「위로 올라가는 지형」 표시이고 HUD 쪽은 「지금 이 키」라 뜻이 다르지만, 헷갈리면 `hintSection`을 0으로 두어 배경 힌트를 끈다.
- **`ledgeEntries` 5 / 12.5 / 15초.** 2026-09-06 기획 답변으로 확정됐다. 접근 2초와 진입 2.925초가 앞에 붙으므로 `time ≥ 3`이어야 하고(5초는 2.075초에 등장), 구간 끝에서 `sectionEndMargin`(3초) 이상 떨어져야 한다. 같은 구간 안 두 항목은 **앞 항목 도착 + 3.95초 < 뒤 항목 도착 − 2.925초**여야 한다(지금 여유 0.625초).
- **긴 돌의 윗변이 맨 아래 레인 위 경계(−0.55)까지다.** 넘어야 할 턱을 「1레인 높이」로 읽어 아트 높이를 1.1에 맞췄다. 화면에서는 연어가 위쪽 레인을 지나 훨씬 높이 넘어가므로 돌이 장애물처럼 보이지 않을 수 있다. 「턱을 실제로 넘는」 그림을 원하면 돌을 `laneY[0] + stepHeight` = 2.2까지 키워야 하고, 그러려면 높이 3.85 unit(균등 스케일 0.4943, 폭 3.42)짜리가 되어 앞면이 너무 두꺼워진다. 그때는 가로로 긴 바위를 새로 받는 편이 낫다.
- **`긴돌.png`이 상류 단차용이라는 것은 사용자 결정이다**(2026-09-06). 파일이 `Art/오브젝트/`에 있어 처음에는 장애물 `Rock`의 두 번째 변종으로 읽었는데, 가로세로 비가 돌1(2.22:1)과 달라(0.89:1) 변종으로 바꿔 끼우면 3배 크기로 나온다. 변종 요청은 `Docs/ArtRequestList.md` 5번에 그대로 남아 있다.
- **`UpperBed`와 환경 강바닥이 겹치는 구간.** 앞면이 x −6.4를 지나면 폭 30짜리 `UpperBed`가 화면 전체를 덮어 환경 강바닥을 완전히 가린다. 높이·소스가 같아 선은 이어지지만 **타일 위상이 달라** 조각이 퇴장하며 사라지는 순간 모래 무늬만 한 번 바뀔 수 있다. 눈에 띄면 `retireX`를 −20 → −40쯤으로 내려 화면 밖에서 끈다.
- **상승 중 배경 0.35 하강과 강바닥.** 환경 강바닥도 `SetVerticalOffset`을 따라가므로 상승 0.7초 동안 환경 쪽 모래 윗선이 −2.0으로 내려간다. 그 구간은 `UpperBed`가 화면을 덮고 있어 거의 보이지 않지만, 상승 첫 0.25초쯤 화면 왼쪽 끝의 좁은 띠에서는 보일 수 있다. 완전히 없애려면 `environmentRiseAmount`를 0으로 둔다.
- **접근 2초 동안 이미 떠 있는 장애물.** 스폰만 멈추고 이미 나온 장애물은 그대로 온다. 기획서 05절 "급하게 막히는 상황 방지"를 스폰 중단으로만 읽었다. 접근 창에서 화면을 완전히 비우려면 `approachSafeTime`을 3.5초쯤으로 올려야 한다.
- **첫 단차 `↑`의 위치·크기**(앞면 왼쪽 2.2 unit, 1.0×1.2, 알파 0.82)는 임시값이다. 기획서는 "배경에 원래 그려져 있는 디자인처럼"만 말한다.
- **단차 사운드가 없다.** `AudioManifest.json`에 단차용 id가 없어 아무 소리도 내지 않는다. 필요하면 `Approaching`/`Cleared`에 붙일 id를 기획에 요청한다.
- **임시 아트는 `↑`뿐이다.** 앞면은 실제 아트(`긴돌.png`)로 바뀌었고 `Ledge_Hint.png`만 아직 생성기가 만든 임시 텍스처다. `Ledge_Rock.png`·`Ledge_RockEdge.png`·`Placeholder/Ledge_Cascade.png`·`Ledge_Foam.png`는 더 이상 아무 데서도 참조하지 않는다(파일은 남겨 뒀다). 남은 요청은 `Docs/ArtRequestList.md` 4-(b).
- **생성기가 얼어 있다.** 「생성기」의 경고 상자를 본다. `Regenerate` 계열을 돌리면 단차 3개·폭포 모양이 사라진다.
