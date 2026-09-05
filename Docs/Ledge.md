# 상류 단차 (Game.Ledge)

일반 구간 중간에 한 번씩 나오는 1레인 높이의 지형 단차다. 연어가 화면 오른쪽으로만 가는 게 아니라 실제로 상류로 올라간다는 것을 플레이로 표현한다. 기획서 4판 `05_상류단차_시스템.md`(전문), `04_점프_시스템.md`, `06_물흐름_월드스크롤_카메라.md`(단차 카메라), `09_일반구간_난이도_스폰.md`(구간 1·2 중간), `14_충돌_실패_재시작_체크포인트.md`(단차 실패는 죽지 않음), `15_인게임HUD_히트박스_안내UI.md`(첫 단차 배경 `↑`), `19_그래픽_애니메이션_리소스.md`(아래 강·지형·위쪽 강), `25_미확정_후순위_항목.md`(정확한 초는 미확정)를 반영했다.

**2026-09-06 기획 답변 반영**: 진행 1에 5초 단차가 추가되어 구간 1은 5초·12.5초 두 번, 구간 2는 15초 한 번(전부 세 조각)이 되었고, 막힘 처리는 지금 구현(전부 정지 후 재도전) 유지, 넘은 뒤는 (가) 화면 복귀 유지, 모양은 바위 계단 벽이 아니라 **작은 폭포**로 바뀌었다. 「일정 (데이터)」·「단차 모양 (작은 폭포)」·「생성기」를 본다.

`Game.Ledge`는 `Game.Play`를 참조하지 않는다. `PlayFlow`는 `ILedgeHandler` 델리게이트 표면만 알고 단차 내부를 모른다(`Docs/Boss.md`의 `BossDirector`/`BossHandler`와 같은 형태).

## 동작 요약

| 단계 | 조건 | 하는 일 |
| --- | --- | --- |
| `Waiting` | 구간 시작 ~ `StartTime` | 단차는 화면 밖 오른쪽에 비활성 대기 |
| `Incoming` | `StartTime` ~ `ApproachTime` | 단차가 `spawnX`(7.5)에서 스크롤 속도로 들어온다 |
| `Approaching` | `ledgeTime - approachSafeTime` | **장애물 스폰 중단**(`IsSpawnSuspended`). 플레이어가 단차를 보고 1번 레인으로 이동하고 점프 쿨타임을 회복할 2초를 얻는다. 쿨타임을 강제로 0으로 만들지 않는다 |
| 도착 | `ledgeTime`(단차 앞면 x = `playerX`) | 공중이면 `Passing`, 아니면 `Blocked` |
| `Blocked` | 착지 상태로 도착 | **월드 정지**(`IsHoldingWorld`). 스크롤·구간 시간·거리가 멈추고 단차는 플레이어 앞에 선다. 죽지 않는다. 입력은 살아 있어 다시 점프할 수 있다 |
| `Passing` | 공중 | 단차가 플레이어 아래를 지나간다. 착지했는데 앞면이 아직 `playerX - clearMargin`보다 오른쪽이면 다시 `Blocked`(너무 일찍 뛴 경우) |
| `Rising` | 착지 + 앞면이 완전히 지나감 | `Cleared`. `cameraMoveDuration`(0.7초) 동안 단차 조각이 `stepHeight`(1.1)만큼 내려와 **위쪽 강바닥이 평소 강바닥 높이에 맞춰진다**. 배경도 `environmentRiseAmount`만큼 같이 내려간다 |
| `Retiring` | 상승 완료 | `Finished`. 장애물 스폰 재개. 배경 오프셋은 `environmentSettleDuration` 동안 0으로 돌아오고 단차는 왼쪽으로 빠져나간다 |
| `Done` | 앞면 x ≤ `retireX` | 단차 비활성 |
| `Failed` | `instantFail`이 켜져 있고 착지 상태로 도착 | `Failed` 이벤트. 통합이 `PlayFlow.Fail()`을 부른다. **기본은 꺼짐** |

플레이어의 레인 로직은 손대지 않는다. 착지는 항상 1번 레인(y 1.1)이고 "위쪽 강에 올라간 것"은 순수하게 월드가 내려가는 연출이다. 상류에서도 레인은 3개 그대로다(기획서 05절).

`↑` 스프라이트는 **맨 처음 나오는 단차 하나에만** 붙는다(`hintSection`(1) 구간의 첫 항목 = `LedgeData.HintEntryIndex`, 기본 진행 1의 5초 단차). 앞면 왼쪽 2.2 unit 지점이고 HUD가 아니라 월드 배경 오브젝트이며 정렬 -4로 배경 층과 물 사이에 들어간다. 같은 구간의 두 번째 단차(12.5초)와 구간 2(15초)는 힌트가 없다.

## 왜 "월드가 내려간다"인가

플레이어·레인·스포너·카메라는 고정 좌표에 있어야 한다(레인 y 1.1/0/−1.1, `playerX` −4.2, ortho 3.6, 수면 y 2.0). 카메라를 실제로 올리면 레인·스포너 좌표가 전부 어긋난다. 그래서 카메라 대신 지형이 내려온다.

단차를 넘기 전후의 화면은 **같아야** 한다(상류에서도 같은 사이드뷰, 같은 3레인). 그래서 영구적인 배경 y 오프셋은 두지 않는다. 실제로 움직이는 것은 단차 조각뿐이다.

- 단차 앞면(계단 벽)의 윗변은 `laneY[0] + stepHeight` = 2.2. 1번 레인(1.1)보다 1레인 높고 점프 정점(1.1 + `jumpHeight` 2.2 = 3.3)보다 낮아 기본 점프 하나로 넘어간다.
- 위쪽 강바닥의 윗선은 평소 강바닥(**−1.65**, `Docs/Environment.md` 「강바닥」)보다 `stepHeight`만큼 높은 **−0.55**다.
- 넘은 뒤 0.7초 동안 조각 전체가 1.1 내려오면 위쪽 강바닥이 −1.65가 되어 환경 `RiverbedLayer`와 정확히 이어진다. 이때 낙수 띠는 이미 화면 왼쪽 밖이다(도착 후 0.55초면 x < −6.4).
- 배경(`EnvironmentThing`)은 `environmentRiseAmount`(0.35, 1레인보다 작다)만큼만 같이 내려갔다가 `environmentSettleDuration`(1.2초) 동안 0으로 돌아온다. 카메라가 한 번 올라갔다 자리 잡는 느낌만 주고 강바닥 높이를 영구히 바꾸지 않는다. 0으로 두면 배경은 전혀 움직이지 않는다.

## 파일

| asmdef | 경로 | 타입 | 책임 |
| --- | --- | --- | --- |
| `Game.Ledge` | `Assets/Scripts/Ledge/` | `LedgePlan` | 순수 struct. 도착 시각·접근 시각·등장 시각·등장 x를 계산하고 `FrontXAt(t)`를 준다. 테스트 대상 |
| | | `LedgeClock` | 순수 C#. 위상 전이(대기→등장→접근→도착→막힘/통과→상승→퇴장)와 막힘/통과 판정. 테스트 대상 |
| | | `LedgePhase`, `LedgeSignal` | 위상 enum, 이번 틱에 일어난 일의 비트 플래그 |
| | | `LedgeData : ScriptableObject` | 조정값 전부. `Create > Team1004 > Ledge Data` |
| | | `ILedgeHandler` | `PlayFlow`가 보는 표면. `BeginSection`/`Tick`/`Stop` + 이벤트 6개 |
| | | `ILedgeWorld` | 단차가 월드에 요청하는 것(스크롤 정지, 스폰 정지). 통합이 구현하면 push, 안 하면 `IsHoldingWorld`/`IsSpawnSuspended` polling |
| | | `LedgeThing : MonoThing` | 단차 조각 하나. 모듈 3개를 붙이고 `Schedule`/`Advance`/`Retire` |
| | | `LedgeScrollModule : Module` | `Ticks = None`. 앞면 x와 세로 오프셋을 `localPosition`에 적는다 |
| | | `LedgeGateModule : Module` | `Ticks = None`. `LedgeClock` 보관. `Ease`(SmoothStep) 제공 |
| | | `LedgeHintModule : Module` | `Ticks = Update`. `↑` 표시 on/off와 위아래 흔들림 |
| | | `LedgeScheduleEntry` | `[Serializable]`. `section` + `time` 한 쌍 |
| | | `LedgeDirector : MonoThing, ILedgeHandler` | 단차 **3개** 소유. 구간 → 항목 큐 계산, 한 번에 하나씩 활성화, 이벤트 발생, 배경 오프셋, 월드 정지 요청 |
| `Game.Ledge.Editor` | `Assets/Scripts/Ledge/Editor/` | `LedgeSetup` | 생성기 메뉴. 텍스처 → 데이터 에셋 → 프리팹 |
| | | `LedgePatterns` | 임시 텍스처 무늬(바위, 바위 윗면, `↑`) |
| `Game.Ledge.Tests` | `Assets/Scripts/Ledge/Tests/` | `LedgeScheduleTests` | EditMode 20개. 일정 계산, 막힘/통과 판정, 항목 목록, 구간 시각 프라이밍, 5초 조각이 12.5초 조각보다 먼저 퇴장하는지 |

asmdef 참조: `Game.Ledge` → `Core.Modules`, `Core.Foundation`, `Game.Config`, `Game.Environment`, `Game.Player`(읽기 전용: `IsAirborne`만 읽는다). **`Game.Play`·`Game.Spawner`·`Game.Boss`는 참조하지 않는다.** 순환 없음. DOTween은 쓰지 않는다(상승·흔들림은 모듈 틱 계산이라 `Time.timeScale = 0`에서 자동으로 멈춘다).

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
| `clearMargin` | `1.2` | 계단 벽 폭. 착지 시점에 앞면이 `playerX − 1.2`보다 오른쪽이면 아직 못 넘은 것으로 보고 다시 막는다 |
| `instantFail` | `false` | 기획서 14절 "현재 테스트안: 죽지 않고 재도전". `true`면 도착 시 착지 상태 = 즉시 실패로 바뀐다 |
| `environmentRiseAmount` | `0.35` | 상승 중 배경이 같이 내려가는 양. 0이면 배경 고정 |
| `environmentSettleDuration` | `1.2` | 배경 오프셋이 0으로 돌아오는 시간 |
| `sectionEndMargin` | `3.0` | `ledgeTime`이 `구간 시간 − 이 값`보다 크면 구간 안에 못 들어간다고 보고 경고 후 건너뛴다 |

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

1. `IsHoldingWorld`(막힘)면 `sectionElapsed`를 올리지 않는다. 아니면 `deltaTime`만큼 올린다.
2. 활성 조각이 없고 큐의 다음 항목이 `sectionElapsed ≥ StartTime`이면 그 조각을 `Schedule(plan, data, showHint, startTime: sectionElapsed)`로 켠다. `LedgeClock`은 `time`을 0이 아니라 **구간 시각으로 프라이밍**해서 시작하므로, 도착은 항상 정확히 `ledgeTime`이다. 켠 프레임에는 `Advance(0)`만 해서 같은 `deltaTime`을 두 번 세지 않는다.
3. `Advance` 뒤 `sectionElapsed = active.Time`으로 되맞춘다(`LedgeClock`이 막힘 시 `time`을 `ledgeTime`으로 고정하므로 한 프레임만큼도 어긋나지 않는다).
4. 조각이 `Done`/`Failed`면 퇴장시키고 큐를 한 칸 민다.

`PlayFlow.sectionTime`도 막힘 중에는 멈추므로 둘은 같은 시각을 본다.

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

성공(공중으로 도착):

```text
BeginSection(1, 25)
 t=9.575  Spawned      단차 활성, x 7.5에서 진입
 t=10.5   Approaching  → 장애물 스폰 중단
 t=12.5   (도착, 공중)  단차가 플레이어 아래를 지나감
 t=12.5+  Cleared      착지 + 앞면 통과 → 상승 시작
 t=+0.7   Finished     → 장애물 스폰 재개, 배경 복귀 시작
 t=+3.5   Retired      비활성
```

막힘(착지 상태로 도착):

```text
 t=12.5   Blocked      → 스크롤·구간 시간·거리 정지, 단차가 플레이어 앞에 섬
 (플레이어가 1번 레인으로 올라가 점프)
          Resumed      → 스크롤 재개
          Cleared / Finished / Retired  (위와 같음)
```

`instantFail = true`면 `Blocked` 자리에 `Failed`가 오고 그 뒤는 없다.

이벤트는 전부 `Action<int>`이고 인자는 구간 번호다. 통합은 이벤트를 안 써도 되고 `IsHoldingWorld`/`IsSpawnSuspended`만 매 프레임 읽어도 된다(권장).

## 단차 모양 (작은 폭포, 2026-09-06)

기획 답변이 단차 모양을 「작은 폭포」로 정했다. 계단 벽 = 바위 대신 **위쪽 강물이 단차를 타고 쏟아지는 낙수 띠 + 바닥 거품**이다. 판정과 일정은 하나도 바뀌지 않았고 스프라이트와 크기만 바뀐다(막힘은 여전히 물리가 아니라 로직이다).

| 오브젝트 | 스프라이트 | 크기(unit) | 중심 | 정렬 | 뜻 |
| --- | --- | --- | --- | --- | --- |
| `UpperBed` | `Placeholder/Env_Riverbed.png` | Tiled **30 × 5** | (15, **−2.85**, z **−0.005**) | −5 | 위쪽 강바닥. **모래 윗선 −0.55**, 아랫변 −5.35 |
| `Cascade` (옛 `StepWall`) | `Placeholder/Ledge_Cascade.png` | Tiled **1.2 × 6.9** | (0.6, **−0.35**) | **−4** | 낙수 띠. **윗변 3.1 = 수면 2.0 + 단차 1.1**, 아랫변 −3.8 |
| `Foam` (새로 추가) | `Placeholder/Ledge_Foam.png` | Tiled 2.4 × 0.9 | (0.25, 2.0, z −0.01) | **−4** | 폭포가 아래 강 수면(y 2.0)에 떨어지는 자리의 거품 |
| `Hint` | `Ledge/Ledge_Hint.png` | 1.0 × 1.2 | (−2.2, 0.6) | −4 | `↑`. 맨 처음 조각에만 연결 |

### 강바닥 맞춤 (2026-09-06)

환경에 실제 강바닥 층(`RiverbedLayer`, 모래, 정렬 −5, 윗선 y **−1.65**)이 생겨서 단차의 위쪽 강바닥을 거기에 맞췄다. `Docs/Environment.md` 「강바닥」을 같이 본다.

| 값 | 이전 | 지금 | 근거 |
| --- | --- | --- | --- |
| `UpperBed` 스프라이트 | `Ledge_Rock.png` (바위) | `Env_Riverbed.png` (모래) | 환경 강바닥과 **같은 소스**여야 이어붙었을 때 재질이 안 튄다 |
| `UpperBed` `m_Size` | 30 × 2.7 | **30 × 5** | 소스가 400×500 px / PPU 100 = 4.0 × 5.0 unit. 세로를 5로 맞춰야 **세로 반복이 정확히 1회**라 모래 윗선 실루엣이 아래에서 되풀이되지 않는다 |
| `UpperBed` 중심 y | −2.45 | **−2.85** | 스프라이트 윗변 −0.35, 아랫변 −5.35. 소스의 실루엣 평균선이 윗변에서 0.20 아래라 **모래 윗선 = −0.55** |
| `UpperBed` z | 0 | **−0.005** | 정렬이 −5로 같은 환경 강바닥(z 0)보다 **앞**에 그리기 위한 것. 겹치는 동안 z-fighting을 막는다 |
| `Cascade` 정렬 | −5 | **−4** | 낙수 띠는 강바닥 **앞**에 와야 한다. 정렬이 같으면 위쪽 강바닥이 물줄기를 덮는다 |
| `Foam` 정렬 | −5 | **−4** | 같은 이유. z −0.01은 그대로 두어 `Cascade`(z 0)보다 앞이라는 관계가 유지된다 |

```text
넘기 전   모래 윗선 −0.55  (= 평소 −1.65 + stepHeight 1.1)
하강      LedgeScrollModule이 조각 전체를 0.7초 동안 1.1 내린다
넘은 뒤   모래 윗선 −1.65  = 환경 RiverbedLayer 윗선. 정확히 이어진다
```

- 아랫변도 맞는다. 하강 뒤 `UpperBed`는 −1.45 ~ −6.45로 환경 강바닥(−1.45 ~ −6.45)과 **세로 범위가 완전히 같다**. 화면 아래(−3.6)보다 2.85 깊어서 배경 오프셋 0.35가 걸려도 아래가 비지 않는다.
- **아래쪽 강바닥은 조각에 없다.** 앞면(local x 0) 왼쪽은 환경 `RiverbedLayer`가 그대로 맡는다. 조각에 낮은 바닥을 하나 더 두면 두 장이 같은 자리에 겹쳐 무늬가 깨진다. `UpperBed`는 local x 0 ~ 30(중심 15, 폭 30)이라 언제나 앞면 오른쪽만 덮는다.
- 그래서 화면은 「모래(−1.65) → 낙수 띠 → 모래(−0.55)」로 읽히고, 넘고 나면 오른쪽 모래가 내려와 왼쪽과 한 장이 된다.
- `Ledge_Rock.png`는 이제 아무 데서도 참조하지 않는다(`Ledge_RockEdge.png`와 같은 처지다). 파일은 남겨 뒀다 — `LedgeSetup`(동결)이 아직 만든다.

- **낙수 띠의 윗변이 3.1인 이유**: 폭포의 물은 「위쪽 강의 수면」에서 시작한다. 아래 강 수면이 `GameConfig.WaterSurfaceY`(2.0)이고 단차가 1레인(1.1)이므로 위쪽 강 수면은 3.1이다. 예전 계단 벽의 윗변(2.2 = `laneY[0] + stepHeight`)은 **넘어야 할 턱의 높이**이지 물의 높이가 아니었다.
- 그래서 화면상 낙수 띠는 점프 정점(3.3)보다 0.2 낮다. 연어는 폭포의 **꼭대기 물을 뚫고** 넘어가는 그림이 된다. 판정에는 영향이 없다(막힘은 `LedgeClock`이 정한다).
- `Foam`의 `z`가 −0.01인 것은 같은 정렬 −5에서 낙수 띠보다 앞에 그리기 위한 것이다(2D 투명 정렬 축이 z).
- 임시 텍스처는 PIL로 만들었다. 아트 요청은 `Docs/Requests.md`의 「상류 단차 = 작은 폭포」 행.

### 수면 상호작용 (2026-09-06 수정)

`Cascade`·`Foam`에는 `WaterInteractor`가 붙어 있다. **스프라이트 크기와 상호작용 바운즈는 서로 다르다.** `ignoreColliderShape`가 켜져 있어 바운즈는 `size`/`offset`이 정한다.

| 오브젝트 | 스프라이트 | 상호작용 `size` | `offset` | 월드 바운즈 y | `wakeOnly` | `wakeScale` |
| --- | --- | --- | --- | --- | --- | --- |
| `Cascade` | 1.2 × 6.9 | **1.2 × 1.0** | **(0, 2.35)** | 1.5 ~ 2.5 | **켬** | **0.5** |
| `Foam` | 2.4 × 0.9 | 2.4 × 0.9 | (0, 0) | 1.55 ~ 2.45 | **켬** | **0.3** |

- **왜 낙수 띠 바운즈가 스프라이트보다 짧은가.** 물이 수면을 흔드는 것은 물이 **닿는 자리**뿐이다. 세로 6.9 전체를 바운즈로 쓰면 수면이 늘 바운즈 한가운데에 있어 항적 근접도(proximity)가 언제나 1이 되고, 수면 아래 −3.8까지 판정이 내려가 아무 의미 없이 비용만 든다. `offset`은 `Transform.TransformPoint`로 곱해지므로 `Cascade`의 로컬 y −0.35에 2.35를 더해 중심이 정확히 수면 2.0에 온다.
- **왜 `wakeOnly`인가.** `Cleared` 뒤 0.7초 동안 `LedgeScrollModule`이 조각 전체를 1.1 내린다. SmoothStep의 최대 기울기가 1.5/0.7이라 조각의 세로 속도가 순간 **−2.36 u/s**까지 간다. 이것은 「물에 뛰어드는 물체」가 아니라 **월드가 내려가는 연출**인데, `WaterInteractorModule`의 속도 샘플러는 그냥 `transform.position` 델타를 읽으므로 구분하지 못하고 착수 첨벙으로 주입했다. 낙수 띠·거품이 수면을 세로로 품고 있어 아래 노드 전부를 매 프레임 −1.18로 눌렀고, 0.7초 뒤 풀리면서 수면이 폭발했다(사용자 보고 「층 바뀔 때 물 연산이 엄청 요동친다」). `wakeOnly`를 켜면 착수 갈래를 아예 건너뛰고 **항적만** 남는다. 자세한 분석은 `Docs/Environment.md` 「층이 바뀔 때 수면이 요동치던 버그」.
- **왜 `wakeScale`인가.** 항적 노드 속도의 상한은 `|vx| × wakeVelocityTransfer × wakeScale`이다. 배율이 1이면 스크롤 4 u/s에서 0.6으로 상단 레인 Log와 같은데, 단차는 폭이 넓고 근접도가 1이라 훨씬 세게 읽힌다. 0.5 / 0.3으로 낮춰 상한을 0.30 / 0.18로 만들었다. **「물이 쏟아져 들어온다」 정도이지 폭풍이 아니다.**
- 세게 하고 싶으면 `wakeScale`을 올린다. `surfaceInfluence`(1.0)는 노드가 바운즈 **윗변보다 위**일 때만 쓰이므로, 수면이 바운즈 안에 있는 지금은 세기 조절 손잡이가 아니다.
- `UpperBed`와 `Hint`에는 붙이지 않았다.

| 파일 | 크기 | 반복 | 내용 |
| --- | --- | --- | --- |
| `Assets/GameAssets/Placeholder/Ledge_Cascade.png` | 64×128 | 가로·세로 | 세로 물줄기 9가닥 + 가로 잔물결. 위가 밝고 아래로 갈수록 푸르다 |
| `Assets/GameAssets/Placeholder/Ledge_Foam.png` | 128×64 | 가로 | 흰 거품 방울. 아래가 짙고 위로 알파가 빠진다 |

`.meta`는 `Ledge_Rock.png.meta`를 본떠 손으로 썼다(Sprite / Single / PPU 100 / Wrap Repeat / FullRect / pivot Center).

## 생성기

> **생성기는 얼어 있다 (2026-09-06).** 에디터가 열린 상태에서 작업했기 때문에 이번 변경(단차 3개, 폭포 모양, `ledgeEntries`)은 `LedgeSet.prefab`과 `LedgeData.asset`의 **YAML을 직접 고쳐서** 넣었고 `LedgeSetup`은 **손대지 않았다**. 그래서 `LedgeSetup`은 아직 「바위 계단 벽 + 단차 2개」를 만든다.
>
> - `Team1004 > Generate Ledge Assets`(`Generate(false)`)는 프리팹이 이미 있으면 경고만 남기므로 **안전하다**. 코어루프 생성기가 부르는 것도 이 형태다.
> - **`Regenerate Ledge Prefab (Overwrite)`와 `Regenerate Core Loop Scenes (Overwrite)`(내부에서 `LedgeSetup.Generate(true)`)를 돌리면 이번 작업이 전부 사라진다.** 돌려야 한다면 먼저 `LedgeSetup.BuildPrefab`/`CreateLedge`를 아래 트리(조각 3개, `Cascade`, `Foam`)에 맞춰 고친 뒤에 돌린다. `LedgeData.asset`은 「없을 때만」 만들므로 보존된다.

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
    UpperBed  Tiled 30 × 5, 중심 (15, -2.85, z -0.005), 정렬 -5   위쪽 강바닥(모래 윗선 -0.55)
    Cascade   Tiled 1.2 × 6.9, 중심 (0.6, -0.35), 정렬 -4   낙수 띠(윗변 3.1, 아랫변 -3.8)
              + WaterInteractor(size 1.2 × 1.0, offset (0, 2.35), wakeOnly, wakeScale 0.5)
    Foam      Tiled 2.4 × 0.9, 중심 (0.25, 2.0, z -0.01), 정렬 -4  아래 강 수면의 거품
              + WaterInteractor(size 2.4 × 0.9, wakeOnly, wakeScale 0.3)
    Hint      1.0 × 1.2, (-2.2, 0.6), 정렬 -4               ↑ (Ledge1만 연결)
  Ledge2 (LedgeThing, 비활성)   같은 구성, Hint 미연결      구간 1 · 12.5초
  Ledge3 (LedgeThing, 비활성)   같은 구성, Hint 미연결      구간 2 · 15초
```

`ledges[i]`는 `LedgeData.ledgeEntries[i]`와 1:1이다. 항목을 늘리면 조각도 같은 수만큼 있어야 한다(없으면 경고 후 그 항목만 건너뛴다).

오브젝트 로컬 원점 x = 0이 **낙수 띠의 앞면**이다. `LedgeScrollModule`이 `localPosition.x = FrontX`를 그대로 쓴다. `environment`·`player`는 씬 참조라 프리팹에서 비고 코어루프 생성기가 채운다(비어 있으면 `Start`에서 `FindAnyObjectByType`으로 대체하고 경고를 남긴다).

정렬(2026-09-06 갱신): `UpperBed`는 **-5**로 환경 `RiverbedLayer`(-5)와 같은 대역이고 z -0.005로 그보다 앞이다. `Cascade`·`Foam`은 **-4**로 두 강바닥 모두보다 앞이다. 셋 다 불투명 물 뒤판(-6)보다 앞, 반투명 물 앞판(15)보다 뒤라 물빛이 얹힌다. 플레이어·장애물(0 이상)은 언제나 단차 앞에 그려진다.

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
            Fail();
        }
```

`instantFail`이 기본값(false)이면 `Failed`는 절대 오지 않는다. 나중에 기획이 즉시 실패로 바꿀 때 이 줄만 살아 있으면 된다.

(e) `Update()`를 아래로 바꾼다. **`Tick`이 먼저**여야 같은 프레임의 `IsHoldingWorld`가 최신이다.

```csharp
        private void Update()
        {
            if (State != PlayState.Running)
                return;

            var ledge = LedgeHandler;

            if (ledge != null)
                ledge.Tick(Time.deltaTime);

            var holding = ledge != null && ledge.IsHoldingWorld;

            if (scroller != null)
                scroller.SetScrolling(!holding);

            if (environment != null)
                environment.SetScrolling(!holding);

            if (holding)
                return;

            Distance += GameConfig.Current.ScrollSpeed * Time.deltaTime;
            sectionTime += Time.deltaTime;

            if (spawnerReady && player != null)
            {
                var playerState = SpawnPlayerState.FromPlayer(
                    player, player.JumpCooldownRemaining, player.AirborneRemaining, player.MoveRemaining);
                spawner.Advance(Time.deltaTime, Distance, playerState);
            }
        }
```

`holding`일 때 `Distance`·`sectionTime`을 올리지 않는 것이 핵심이다. 올리면 플레이어가 단차에 막혀 있는 사이에 구간이 끝난다.

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

에디터에서 확인할 것: 프리팹 인스턴스에 `Ledge3`가 보이는지, `LedgeDirector.ledges`가 3칸인지, `LedgeData` 인스펙터에 항목 3개가 보이는지, 낙수 띠·거품 스프라이트가 깨지지 않았는지(손으로 쓴 `.meta`), EditMode `Game.Ledge.Tests` 20개, PlayMode `FullLoopTests`.

### 수동 플레이 체크 (기획서 24절)

1. 구간 1에서 **3.0초쯤** 장애물이 끊기고 오른쪽에서 **작은 폭포**가 들어와 5.0초에 도착하는가. 조작 힌트(2.5초)와 겹쳐도 읽히는가.
2. 배경 `↑`가 **첫 단차에만** 보이고 HUD 팝업처럼 보이지 않는가.
3. 1번 레인에서 점프하면 단차를 넘는가(2레인 점프가 1레인 단차보다 확실히 높은가).
4. 넘은 뒤 0.7초 동안 위쪽 강바닥이 부드럽게 내려와 평소 강바닥과 이어지는가. 화면이 순간적으로 튀지 않는가.
5. 못 넘으면 죽지 않고 그 자리에서 세계가 멈추는가. 다시 점프하면 이어서 진행되는가. 막혀 있는 동안 HUD의 남은 초가 멈추는가.
6. **같은 구간의 두 번째 단차(12.5초)가 첫 조각이 완전히 빠져나간 뒤에 들어오는가**(둘이 동시에 보이면 일정이 어긋난 것이다). 힌트가 없는가.
7. 구간 2의 단차(15초)는 힌트 없이 이해되는가.
8. 단차 중 Esc → 전부 멈추는가. 실패 후 재도전 → 단차가 화면 밖으로 돌아가고 처음부터 다시 나오는가.
9. 구간 3·4에는 단차가 나오지 않는가. 보스·컷신 중에는 나오지 않는가.
10. **폭포 모양**: 낙수 띠가 수면 위(2.0~3.1)에도 보이는가. 거품이 낙수 띠 앞에 그려지는가. 타일 이음매가 눈에 띄는가.
10-1. **강바닥**: 단차 앞에서 모래가 두 단(−1.65 / −0.55)으로 보이는가. 넘은 뒤 0.7초에 두 단이 한 줄로 합쳐지는가. 합쳐진 뒤 이음매나 무늬 겹침이 보이는가.
11. **수면**: 폭포가 들어올 때 수면이 은은하게 밀리는가(폭풍이 아니라 「쏟아져 들어오는」 정도). **넘은 뒤 0.7초 동안 수면이 튀지 않는가.** 튀면 `WaterSettings.maxInjectedVelocity`를 1.5 → 1.0으로 내리고, 그래도 남으면 `Cascade`/`Foam`의 `wakeScale`을 내린다.

## 확신 없는 지점

- **배경 세로 오프셋을 영구로 두지 않았다.** 지시는 "환경을 1.1 내린다"였지만, `NearLayer`(강바닥, y −3.8~−2.2)를 영구히 1.1 내리면 단차가 지나간 뒤 강바닥이 위쪽 강바닥보다 1.1 낮아 이음매가 보이고, 단차를 여러 번 넘으면 강바닥이 화면(−3.6) 밖으로 나간다. 기획서 05절이 "상류에서도 같은 사이드뷰·3레인"이라고 못 박았으므로 넘기 전후 화면이 같아야 한다고 읽었다. 그래서 영구 오프셋 대신 단차 조각만 1.1 내리고 배경은 0.35만 내렸다 돌아오게 했다. `EnvironmentThing.SetVerticalOffset` / `RaiseWorldAsync`는 요청대로 만들어 두었으므로, 영구 이동을 원하면 `environmentRiseAmount`를 1.1로 올리고 복귀를 끄면 된다(`environmentSettleDuration`을 0보다 크게 두면 반드시 돌아온다).
- **계단 벽에 콜라이더가 없다.** 막힘은 물리가 아니라 로직(월드 정지)이다. 그래서 플레이어 스프라이트(정렬 0)가 벽 앞에 겹쳐 보인다. 아트가 나오면 벽 앞면에 플레이어를 살짝 밀어내는 연출이 필요할 수 있다.
- **너무 일찍 뛴 경우.** 도착 0.1초 전에 뛰어 도착 직후 착지하면 앞면이 아직 `playerX − clearMargin` 안이라 다시 `Blocked`가 된다. 기획서에 이 경우가 없어 "다시 막힌다"로 정했다. 1.6초 체공이라 실제로는 거의 나오지 않는다.
- **`ledgeEntries` 5 / 12.5 / 15초.** 2026-09-06 기획 답변으로 확정됐다. 접근 2초와 진입 2.925초가 앞에 붙으므로 `time ≥ 3`이어야 하고(5초는 2.075초에 등장), 구간 끝에서 `sectionEndMargin`(3초) 이상 떨어져야 한다. 같은 구간 안 두 항목은 **앞 항목 도착 + 3.95초 < 뒤 항목 도착 − 2.925초**여야 한다(지금 여유 0.625초).
- **낙수 띠가 점프 정점보다 0.2 낮다.** 윗변 3.1, 점프 정점 3.3. 기획 답변의 「폭포」와 「수면 + 1레인」을 그대로 따랐지만, 눈으로 보면 연어가 물줄기를 뚫고 지나가는 그림이다. 「턱만 넘는」 그림을 원하면 낙수 띠의 윗변을 2.2로 되돌린다(`Cascade` 크기 1.2 × 6.0, 중심 y −0.8).
- **거품과 수면 정렬.** `Foam`은 2026-09-06에 −4로 올라가 물 뒤판(−6)보다 확실히 앞이다. 물 앞판(15)에는 여전히 옅게 덮인다(의도).
- **`UpperBed`와 환경 강바닥이 겹치는 구간.** 앞면이 x −6.4를 지나면 폭 30짜리 `UpperBed`가 화면 전체를 덮어 환경 강바닥을 완전히 가린다. 높이·소스가 같아 선은 이어지지만 **타일 위상이 달라** 조각이 퇴장하며 사라지는 순간 모래 무늬만 한 번 바뀔 수 있다. 눈에 띄면 `retireX`를 −20 → −40쯤으로 내려 화면 밖에서 끈다.
- **상승 중 배경 0.35 하강과 강바닥.** 환경 강바닥도 `SetVerticalOffset`을 따라가므로 상승 0.7초 동안 환경 쪽 모래 윗선이 −2.0으로 내려간다. 그 구간은 `UpperBed`가 화면을 덮고 있어 거의 보이지 않지만, 상승 첫 0.25초쯤 화면 왼쪽 끝의 좁은 띠에서는 보일 수 있다. 완전히 없애려면 `environmentRiseAmount`를 0으로 둔다.
- **낙수 띠 항적 세기 0.5 / 거품 0.3.** 실기에서 정한 값이 아니라 「상단 레인 Log(1.0)보다 약하게」라는 기준으로 잡은 임시값이다. 사용자가 에디터에서 보고 조정할 자리다.
- **접근 2초 동안 이미 떠 있는 장애물.** 스폰만 멈추고 이미 나온 장애물은 그대로 온다. 기획서 05절 "급하게 막히는 상황 방지"를 스폰 중단으로만 읽었다. 접근 창에서 화면을 완전히 비우려면 `approachSafeTime`을 3.5초쯤으로 올려야 한다.
- **첫 단차 `↑`의 위치·크기**(앞면 왼쪽 2.2 unit, 1.0×1.2, 알파 0.82)는 임시값이다. 기획서는 "배경에 원래 그려져 있는 디자인처럼"만 말한다.
- **단차 사운드가 없다.** `AudioManifest.json`에 단차용 id가 없어 아무 소리도 내지 않는다. 필요하면 `Approaching`/`Cleared`에 붙일 id를 기획에 요청한다.
- **임시 아트.** `Ledge_Rock.png`·`Ledge_Hint.png`는 생성기가 만든 임시 텍스처이고 `Placeholder/Ledge_Cascade.png`·`Ledge_Foam.png`는 이번에 PIL로 만든 임시 텍스처다. `Ledge_RockEdge.png`는 더 이상 쓰이지 않는다. 아트 요청은 `Docs/Requests.md`에 등록했다.
- **생성기가 얼어 있다.** 「생성기」의 경고 상자를 본다. `Regenerate` 계열을 돌리면 단차 3개·폭포 모양이 사라진다.
