# 카메라 연출 (Game.View / Game.CameraFx)

흔들림·줌·펀치·팬을 한 곳에서 합성해 카메라 트랜스폼과 `orthographicSize`를 매 `LateUpdate` 한 번만 쓰는 리그다. 게임플레이 큐(레인 이동·점프·피격·보스·단차·구간)와 컷신의 `CameraTo`/`Shake`가 같은 리그를 지나가므로 서로 덮어쓰지 않고 겹친다. 값은 전부 `Assets/GameAssets/Design/Camera/`의 에셋에 있어 Inspector에서 고친다.

## 합성 규칙

```text
position = basePosition + panOffset + shakeOffset
rotationZ = baseRoll + shakeRoll
orthographicSize = clamp(baseSize × zoomFactor, minZoomIn, maxZoomOut 적용 후)
```

- `basePosition`·`baseSize`는 **연출이 아닌 카메라의 원래 자리**다. 컷신만 이걸 옮기고, 게임플레이 큐는 절대 건드리지 않는다.
- 흔들림·줌·팬은 각자 모듈이 계산하고 리그가 마지막에 합친다. 모듈은 등록 순서대로 틱하므로 `GameCamera.Awake`에서 `Aspect → Shake → Zoom → Pan → Rig` 순으로 붙인다. 리그가 마지막이라 그 프레임의 최신 값으로 합성한다.
- `AspectModule`은 `Camera.rect`(레터박스/필러박스)만 만지고 `orthographicSize`는 건드리지 않는다. 그래서 줌은 레터박스와 싸우지 않고 `baseSize`에 배율로만 곱한다.
- 시간은 전부 `Time.deltaTime`(스케일 적용)이다. `Time.timeScale = 0`인 일시정지에서 흔들림·줌·팬이 그 자리에 멈춘다. UI는 영향이 없다.

## 줌 한계와 월드 커버리지

기준 화면은 ortho 3.6 · 16:9라 화면 절반이 **가로 6.4 × 세로 3.6**이다. 줌아웃은 그만큼 월드 가장자리를 드러내므로 상한이 있다.

| 줌 | 화면 절반 (가로 × 세로) | 흔들림 최대(±0.36) 포함 |
| --- | --- | --- |
| 1.00 | 6.40 × 3.60 | 6.76 × 3.96 |
| 1.06 | 6.78 × 3.82 | 7.14 × 4.18 |
| **1.10 (`maxZoomOut`)** | **7.04 × 3.96** | **7.40 × 4.32** |
| 1.17 | 7.49 × 4.21 | 7.85 × 4.57 |

월드가 실제로 덮는 범위와 그래서 정한 상한:

| 경계 | 좌표 | 줌 몇에서 깨지나 |
| --- | --- | --- |
| 하늘(`Environment.prefab`의 `Sky`) 윗변 | y **4.6** (이번에 3.8에서 올렸다) | 1.28 (흔들림 포함 1.18) |
| 하늘 좌우 | x **±8.0** (이번에 ±7.2에서 넓혔다) | 1.25 |
| 물 폴리곤 | x ±8.0 | 1.25 |
| 모래(강바닥) 아랫변 | y −6.45 | 문제 없음 |
| 타일 층(구름·산·언덕·바닥) | 화면 + 타일 1장 | 문제 없음 |
| **장애물·단차 스폰 x** | **7.5** | **1.172** — 여기가 가장 낮다. 스폰 순간이 화면에 들어와 팝인으로 보인다 |

가장 낮은 경계가 1.172(스폰 x)라서 여유를 두고 **`maxZoomOut = 1.1`**로 잡았다. 흔들림 가로 최대까지 겹쳐도 7.40 < 7.5라 팝인이 보이지 않는다. 이 값을 올리려면 스포너의 `spawnX`와 단차 `spawnX`(7.5)를 같이 밀어야 한다.

줌인 쪽은 화면이 좁아져 커버리지 문제가 없지만 **점프 정점**이 잘린다. 상단 레인 y 1.1 + `jumpHeight` 2.2 = **y 3.3**이므로 화면 절반 세로가 3.3 아래로 내려가면(= 줌 0.9167 미만) 연어가 화면 위로 사라진다. 하한 `minZoomIn = 0.8`은 안전벽일 뿐이고, **실제 큐는 0.92 아래로 내리지 않는다**(단차 QTE 큐를 0.9 대신 0.94로 잡은 이유).

흔들림 진폭 상한도 여기서 나온다. `shakeMaxOffset 0.36 = (1.1 − 1) × 3.6`이라 흔들림만으로는 줌아웃 1.1이 드러내는 것보다 더 바깥을 보여주지 않는다.

## 구성

| 경로 | asmdef | 내용 |
| --- | --- | --- |
| `Assets/Scripts/View/` | `Game.View` (참조: `Core.Modules`, `Core.Foundation`) | 리그·모듈·프리셋·디렉터. 게임 로직을 모른다 |
| `Assets/Scripts/View/Tests/` | `Game.View.Tests` (EditMode) | 순수 계산 테스트 25개 |
| `Assets/Scripts/CameraFx/` | `Game.CameraFx` (참조: `Game.View`, `Game.Play`, `Game.Player`, `Game.Boss`, `Game.Boss.Integration`, `Game.Ledge`, `Core.*`) | `CameraCueBinder`. 게임 이벤트 → 프리셋을 잇는 **유일한** 접점 |
| `Assets/GameAssets/Design/Camera/` | 데이터 | `CameraFxSettings.asset` + 큐 프리셋 17개 |

의존 방향: `Game.View`는 아무 게임 기능도 참조하지 않는다. `Game.Cutscene`이 `Game.View`를 참조해 리그를 쓰고, `Game.CameraFx`가 모두를 참조한다. `Game.CameraFx`를 참조하는 어셈블리는 없다. 순환 없음.

| 파일 | 타입 | 역할 |
| --- | --- | --- |
| `GameCamera.cs` | `MonoThing` 호스트 | 모듈 5개를 붙이고 `fxSettings`를 리그에 넘긴다. `OnEnable`에서 효과를 지운다(씬 재시작) |
| `AspectModule.cs` | `Module`(Update) | 16:9 레터박스. `Camera.rect`만 만진다 |
| `CameraShakeModule.cs` | `Module`(LateUpdate) | trauma 기반 흔들림 + 펀치 |
| `CameraZoomModule.cs` | `Module`(LateUpdate) | `zoomFactor` 트윈. 상·하한 클램프 |
| `CameraPanModule.cs` | `Module`(LateUpdate) | 화면 오프셋 트윈. `Follow`는 기본 꺼짐(연어 x가 고정이라 필요 없다) |
| `CameraRig.cs` | `Module`(LateUpdate, 마지막) | 합성. `BasePosition`/`BaseSize` 소유 |
| `CameraComposition.cs`, `ShakeMath.cs`, `CameraEasing.cs`, `CameraEase.cs` | static / enum | 순수 계산. 테스트 대상 |
| `CameraCue.cs` | `ScriptableObject` | 프리셋 하나. `Apply(rig)`가 모듈 3개를 친다 |
| `CameraFxSettings.cs` | `ScriptableObject` | 한계값(줌 상·하한, 흔들림 최대, 감쇠, 주파수, 팬 최대) |
| `CameraFxDirector.cs` | `DomainSingleton` | 밖에서 쓰는 API. Main Camera에 붙어 있다 |
| `CameraFx/CameraCueBinder.cs` | `MonoThing` 호스트 | `Play.unity`의 `CameraCues` 오브젝트. `Start`에서 구독, 파괴 시 해제 |

## API

밖에서 쓰는 것은 `CameraFxDirector`뿐이다. `Game.View`를 참조하면 어디서든 쓸 수 있다.

```csharp
if (CameraFxDirector.TryGetCurrent(out var camera))
{
    camera.Shake(0.4f);                                  // trauma를 더한다(감쇠는 설정값)
    camera.Shake(0.3f, 0.5f);                            // 0.3초 동안 trauma 0.5가 0으로
    camera.Punch(Vector2.up, 0.05f, 0.2f);               // 방향 · 세기(unit) · 시간
    camera.Zoom(1.06f, 0.6f);                            // 목표 배율로 가서 머문다
    camera.ZoomPunch(0.95f, 0.1f, 0.05f, 0.2f);          // 들어가서 · 머물다 · 1.0으로 복귀
    camera.Pan(new Vector2(0f, 0.2f), 0.4f);
    camera.PanPulse(new Vector2(0f, 0.2f), 0.7f, 0.15f, 0.5f);
    camera.PlayCue(cueAsset);                            // 프리셋 하나
    camera.PlayCue(cueAsset, Vector2.down);              // 펀치 방향만 갈아끼운다
    camera.ResetEffects();                               // 흔들림·줌·팬만 0으로
    camera.ResetAll();                                   // 카메라 원래 자리까지 복귀
}

CameraFxDirector.TryPlay("boss_clear");   // id로 재생. 디렉터가 없으면 false
CameraFxDirector.TryShake(0.2f);
```

`PlayCue(string)`은 `CameraFxDirector.cues` 배열(= `Play.unity`의 Main Camera에 꽂힌 프리셋 17개)에서 `cueId`로 찾는다. **다른 기능이 카메라 연출을 붙이고 싶으면 자기 코드에서 `TryPlay("id")`만 부르면 된다.** `Game.CameraFx`를 참조할 필요도, 바인더를 고칠 필요도 없다.

`CameraEase`는 DOTween과 무관한 자체 enum이다(`Linear`, `SineIn/Out/InOut`, `QuadIn/Out/InOut`, `CubicOut`, `BackOut`). `Game.View`는 DOTween을 쓰지 않는다.

## 프리셋

`Assets/GameAssets/Design/Camera/*.asset`. 값은 전부 임시이며 사람이 보고 조정할 자리다. `trauma`는 0~1, `punchStrength`·`panOffset`은 월드 unit, `zoomFactor`는 배율(1 = 기본, 클수록 멀어짐), 시간은 초.

| 에셋 | `cueId` | 흔들림 | 펀치 | 줌 (배율 / 들어가기 / 머무름 / 돌아오기) | 팬 |
| --- | --- | --- | --- | --- | --- |
| `LaneChange` | `lane_change` | — | 이동 방향 0.03 / 0.18 | — | — |
| `JumpStart` | `jump_start` | — | — | 0.97 / 0.10 / 0.05 / 0.10 | — |
| `Landing` | `landing` | 0.25 | 아래 0.05 / 0.2 | — | — |
| `Hit` | `hit` | 0.7 | — | 0.92 / 0.06 / 0.18 / 0.16 | — |
| `SectionStart` | `section_start` | — | — | 1.03 / 0.25 / 0.10 / 0.50 | — |
| `BossBanner` | `boss_banner` | — | — | 1.06 / 0.6 / — / 유지 | — |
| `BossStart` | `boss_start` | — | — | 1.00 / 0.4 / — / 유지 | — |
| `TelegraphImminent` | `boss_telegraph` | 0.15 (0.2초) | — | — | — |
| `BossAttack1` (보스 1 냚싫줄) | `boss_attack_1` | — | 왼쪽 0.08 / 0.18 | — | — |
| `BossAttack2` (보스 2 돌진형) | `boss_attack_2` | 0.45 (0.35초) | — | — | — |
| `BossAttack3` (보스 3 폭포) | `boss_attack_3` | 0.35 (0.35초) | — | — | — |
| `FinalWaterfall` | `boss_final_waterfall` | 0.25 (2.4초, 저주파 럼블) | — | 1.08 / 2.4 / — / 유지 | — |
| `BossClear` | `boss_clear` | — | — | 1.05 / 0.12 / 0.06 / 0.35 (`CubicOut`) | — |
| `LedgeApproach` | `ledge_approach` | — | — | 1.04 / 0.5 / — / 유지 | — |
| `LedgeQte` | `ledge_qte` | — | — | 0.94 / 0.3 / — / 유지 | — |
| `LedgeRise` | `ledge_rise` | — | — | 1.00 / 0.7 / — / 유지 | 위 0.22 / 0.7 · 0.15 · 0.5 |
| `LedgeFinished` | `ledge_finished` | — | — | 1.00 / 0.35 / — / 유지 | — |

`CameraFxSettings.asset`

| 항목 | 값 | 뜻 |
| --- | --- | --- |
| `minZoomIn` | 0.8 | 줌 하한(안전벽). 실제 큐는 0.92 아래로 안 간다 |
| `maxZoomOut` | 1.1 | 줌 상한. 근거는 위 「줌 한계와 월드 커버리지」 |
| `shakeMaxOffset` | 0.36 | trauma 1일 때 최대 오프셋(unit). (1.1 − 1) × 3.6 |
| `shakeMaxRoll` | 1.2 | trauma 1일 때 최대 회전(도) |
| `shakeDecayPerSecond` | 1.6 | trauma 감쇠. `AddTrauma(0.7)`이 약 0.44초에 0이 된다 |
| `shakeFrequency` | 22 | 노이즈 속도(Hz 비슷). 낮추면 큰 흔들림, 높이면 자글자글 |
| `panMaxOffset` | 0.8 | 팬 오프셋 절대값 상한 |

읽는 값의 정의: 흔들림 오프셋은 `trauma² × shakeMaxOffset × Perlin(−1~1)`이다. trauma가 제곱이라 0.7은 0.49배(0.18 unit), 0.15는 0.02배(0.008 unit)로 급격히 작아진다. **작은 값은 생각보다 훨씬 작다.**

## 큐 바인딩

`CameraCueBinder`(`Play.unity`의 `CameraCues`)가 `Start`에서 구독한다. 이벤트를 내는 쪽은 아무것도 모른다.

| 순간 | 이벤트 | 프리셋 |
| --- | --- | --- |
| 레인 이동 | `LanePlayer.LaneChanged(lane)` — 직전 레인과 비교해 방향을 만든다 | `lane_change` (펀치 방향을 위/아래로 갈아끼움) |
| 점프 시작 | `LanePlayer.Jumped` | `jump_start` |
| 착수 | `LanePlayer.Landed` | `landing` |
| 장애물·레인 띠 피격 | `LanePlayer.Hit(hazard)` | `hit` |
| 최종 폭포 충돌 | `BossThing.Impact` | `hit` |
| 구간 시작 | `PlayFlow.SectionStarted(section)` | `section_start` |
| 보스 배너 | `PlayFlow.StateChanged(Boss)` | `boss_banner` |
| 보스 시작(타이머 시작) | `BossThing.Began` | `boss_start` (1.0으로 복귀) |
| 예고 임박 | `BossThing.TelegraphImminent` | `boss_telegraph` |
| 공격 시작 | `BossThing.AttackBegan` | 보스별 `bossAttackCues[구간−1]`. **구간 번호로만 고르므로 보스 구현체가 바뀜도(예: 보스 2 교체) 그대로 따라간다** |
| 최종 폭포 접근 | `WaterfallBoss.FinalApproachBegan` | `boss_final_waterfall` |
| 보스 클리어 | `BossThing.Finished(Passed)` | `boss_clear` |
| 단차 접근 | `ILedgeHandler.Approaching(section)` | `ledge_approach` |
| 단차 QTE 시작 | `ILedgeHandler.QteStarted(section)` | `ledge_qte` |
| 단차 통과(지형 하강) | `ILedgeHandler.Cleared(section)` | `ledge_rise` (팬 업 0.7초, 환경 하강과 같은 길이) |
| 단차 종료 | `ILedgeHandler.Finished(section)` | `ledge_finished` |
| 컷신 진입 · Ready · 클리어 · 실패 | `PlayFlow.StateChanged` | 프리셋 아님. `ResetEffects()` |
| 보스 실패 | `BossThing.Finished(Failed)` | 프리셋 아님. `ResetEffects()` |

바인더는 `BossThing`의 공통 이벤트만 구독하고 보스 구체 타입을 모른다(예외는 최종 폭포 접근 하나라 `WaterfallBoss`만 타입으로 찾는다). 보스를 갈아끼워도 카메라 쪽은 고칠 게 없다.

보스 쪽에 새로 넣은 이벤트는 셋뿐이다. `BossThing.TelegraphImminent`·`AttackBegan`은 이미 모든 보스가 부르던 `PatternBoss.BrightenTelegraph()`·`PlayAttackSfx()` 안에서 나가고, `WaterfallBoss.FinalApproachBegan`은 `BeginFinalApproach()`에서 나간다. 상태 클래스는 하나도 고치지 않았다.

`bossDirector`와 `director` 참조가 비어 있으면 각각 `FindAnyObjectByType<BossDirector>`(비활성 포함, `Start`에서 한 번)과 `CameraFxDirector.Current`로 채운다. 그래서 씬 YAML에 프리팹 인스턴스 안쪽을 가리키는 참조를 쓰지 않았다.

## 컷신 연동

`CutsceneBase`의 `CameraTo`/`Shake`는 리그가 있으면 리그를 지난다.

- `CameraTo(position, orthoSize, duration, ease)` → `DOTween.To`로 **`rig.BasePosition`·`rig.BaseSize`**를 트윈한다. 카메라 트랜스폼을 직접 만지지 않으므로 그 사이에 게임플레이 흔들림이 들어와도 서로 지우지 않는다.
- `Shake(duration, strength)` → `rig.Shake.ShakeUnits(duration, strength)`. `strength`는 예전처럼 월드 unit이고, 리그가 `trauma = √(strength / shakeMaxOffset)`로 바꿔 같은 진폭을 낸다. 대기는 같은 길이의 빈 시퀀스라 Skip·Cancel 동작이 그대로다.
- `Begin`에서 `rig.BasePosition`/`BaseSize`를 기억하고 효과를 지운다. `Cleanup`(`RestoreCameraOnFinish`)에서 `rig.SetBase(...)` + `ResetEffects()`로 되돌린다.
- 리그가 없는 씬(카메라에 `GameCamera`가 없을 때)에서는 예전 경로(`Transform.DOMove`, `Camera.DOShakePosition`)를 그대로 쓴다.

`CutsceneContext.Rig`는 `StageCamera.GetComponent<GameCamera>().Rig`를 지연 조회한다. 컷신 클래스(`Scenes/*.cs`)는 고칠 게 없다.

## 일시정지·재시작

- 일시정지: 모듈이 `Time.deltaTime`을 쓰므로 `timeScale = 0`에서 그대로 얼어붙고 재개하면 이어진다.
- 재시도(`PlayFlow.Retry` → 씬 다시 로드): 새 `GameCamera.Awake`가 리그를 새로 만들고 `OnEnable`이 효과를 지운다.
- 디버그 점프(`PlayFlow.DebugJumpTo`)는 `SetState(Ready)`를 거치므로 바인더가 `ResetEffects()`를 부른다.
- 보스 체크포인트 재시작도 `Ready`를 지난다.

## 큐를 하나 더 넣기

1. `Assets/GameAssets/Design/Camera/`에서 기존 `.asset`을 복제하고 이름과 `cueId`를 바꾼다(Project 창에서 Ctrl+D).
2. 값을 채운다. 안 쓸 항목은 0으로 둔다. **`zoomFactor` 0은 「줌을 건드리지 않음」**이고, 1은 「기본 배율로 되돌림」이다.
3. Main Camera의 `CameraFxDirector.cues`에 새 에셋을 추가한다(id로 부를 거라면 필수).
4. 부르는 쪽:
   - 다른 기능이면 `CameraFxDirector.TryPlay("새id")`.
   - 플레이어·보스·단차·PlayFlow의 이벤트면 `CameraCueBinder`에 `[SerializeField] private CameraCue 새Cue;`와 구독 한 줄을 더하고 `CameraCues` 오브젝트에 꽂는다.
5. 이벤트가 없으면 만들지 말고 `Docs/Requests.md`에 요청을 남긴다.

## 튜닝 가이드

전부 Play 중에도 Inspector에서 바꿀 수 있다(에셋이라 값이 남는다).

| 증상 | 손댈 곳 |
| --- | --- |
| 흔들림이 너무 약하다 | 큐의 `trauma`. 제곱이라 0.4 → 0.6이 2.25배다. 그래도 부족하면 `CameraFxSettings.shakeMaxOffset`(상한 0.36을 넘기면 화면 밖이 보일 수 있다) |
| 흔들림이 오래 남는다 | `shakeDecayPerSecond`를 올리거나 큐에 `shakeDuration`을 넣는다(그 시간에 정확히 0이 된다) |
| 흔들림이 지저분하다 / 멀미난다 | `shakeFrequency`를 15~18로 낮추고 `shakeMaxRoll`을 0으로 |
| 줌아웃에서 화면 밖이 보인다 | 큐의 `zoomFactor`를 1.06 이하로. 상한 자체를 올리려면 「줌 한계와 월드 커버리지」의 경계를 먼저 넓혀야 한다 |
| 점프 정점이 잘린다 | 줌인 큐가 0.92보다 작지 않은지 본다 |
| 피격이 밋밋하다 | `hit`의 `zoomHold`를 `GameConfig.hitStopDuration`(0.1)에 맞춰 늘리거나 `trauma`를 올린다 |
| 레인 이동이 튄다 | `lane_change`의 `punchStrength`(0.03)를 0.015로. 0.06을 넘으면 눈에 띄게 흔들린다 |
| 단차에서 화면이 두 번 움직인다 | `ledge_rise`의 팬과 `LedgeData.environmentRiseAmount`(0.35)가 겹친다. 둘 중 하나만 남긴다 |

## 씨 배치 (`Play.unity`)

에디터가 열려 있어 YAML을 손으로 넣었다. fileID는 다음과 같다.

| 대상 | fileID | 내용 |
| --- | --- | --- |
| `Main Camera` GameObject | 875367493 | 컴포넌트 목록 끝에 `1704300003` 추가 |
| `GameCamera` 컴포넌트 | 875367495 | `fxSettings`, `shakeSeed: 12.7` 두 줄 추가 |
| `CameraFxDirector` | **1704300003** | 새 컴포넌트. `gameCamera` = 875367495, `cues` 17개 |
| `CameraCues` GameObject | **1704300001** | 새 루트 오브젝트 |
| └ Transform | **1704300002** | `m_Roots` 맨 뒤에 추가 |
| └ `CameraCueBinder` | **1704300004** | 프리셋 17개 연결. `bossDirector`는 비워 둔다(런타임 조회) |

**주의**: `CoreLoopSetup.Regenerate`는 이 둘을 모른다. 지금 상태에서 생성기를 다시 돌리면 카메라 연출이 씨에서 사라진다. `Docs/Requests.md`에 생성기 반영 요청을 남겼다.

## 검증

- 오프라인 Roslyn 컴파일(`csccheck_all.py`): 어셈블리 40개, `error CS` 0.
- EditMode 테스트 `Game.View.Tests`(25개): 흔들림 감쇠·진폭·펀치 포락선·trauma↔진폭 환산, 이징 9종의 경계와 대칭, 합성(위치·크기 클램프·가시 범위), 줌/팬 트윈의 도달·복귀·클램프. **에디터가 열려 있어 실행하지 않았다.** 사람이 Test Runner에서 돌린다.
- 눈으로 볼 것은 `Docs/CoreLoop.md` 「수동 플레이 체크리스트」에 붙였다.
