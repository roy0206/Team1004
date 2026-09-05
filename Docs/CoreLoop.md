# 코어루프

3레인 상하 이동·점프로 장애물을 피하며 구간 4개를 지나 엔딩까지 가는 루프다. 환경(배경·수면)·플레이어 애니메이션·컷신 애니메이션·보스 히트박스 표시가 통합되어 타이틀→엔딩→타이틀이 한 번에 돈다(「통합 지도」, 「수동 플레이 체크리스트」). 기획서 2판 `Team1004/Assets/Documents/정리 1번/2번 정리.md`(4, 15~18, 26, 28~30, 33, 36절)와 3판 `3번 정리.md`(3, 9, 12~13, 18~19, 24~25절)를 반영했다. 점프·스포너·보스·컷신은 이 문서의 「다른 에이전트용 훅」에 끼어든다. 기획서 정본은 `Team1004/Assets/Documents/정리 1번/초지일관 귀향 게임 프로토타입 제작 프롬프트.pdf`이며 기획 내용만 따르고 HTML 구현 지시와 우선순위는 무시했다.

## 생성기 사용법

씬·프리팹·에셋은 손으로 만들지 않고 에디터 메뉴가 만든다. Inspector에서 손으로 연결할 것은 없다.

1. 컴파일 오류가 없어야 한다. 배치 검증: `"C:/Program Files/Unity/Hub/Editor/6000.3.6f1/Editor/Unity.exe" -batchmode -nographics -projectPath "D:/Unity/Team1004/Team1004" -quit -logFile "D:/Unity/Team1004/Team1004/Logs/batch_compile.log"` 뒤 로그에서 `error CS`를 찾는다. `Team1004/Temp/UnityLockfile`이 있으면 에디터가 열려 있거나(배치 금지) 직전 배치가 남긴 잔재다(`Unity.exe` 프로세스가 없으면 지운다).
2. 생성 순서. 코어루프 생성기가 아래 둘을 자동 호출하므로 보통 3만 실행하면 된다.
   1. `Team1004 > Generate Spawner Assets` (`Game.Spawner.Editor.SpawnerAssetGenerator.GenerateMissing`)
   2. `Team1004 > Generate Cutscene Dialogue Prefab` (`Game.Cutscene.Editor.CutsceneSetup.Generate`)
   2-1. `Team1004 > Generate Boss Assets` (`Game.Boss.Editor.BossAssetSetup.Generate`). **Overwrite면 `Generate(true)`로 프리팹을 다시 만든다**(히트박스 프레임·띠 알파 갱신)
   2-2. `Team1004 > Generate Animation Assets` (`Game.Animation.Editor.AnimationAssetGenerator.GenerateMissing`) — `Art/물고기 애니메이팅/` 11장 임포트 설정(공유 pivot) + 클립 4개
   2-3. `Team1004 > Generate Environment Assets` (`Game.Environment.Editor.EnvironmentSetup.Generate`) — 없을 때만
   3. `Team1004 > Regenerate Core Loop Scenes (Overwrite)` (`Game.Bootstrap.Editor.CoreLoopSetup.Regenerate`, 배치에서는 확인 창을 건너뛴다). `Generate Core Loop Scenes`는 **없는 것만** 만들며 기존 씬·프리팹은 경고만 남기고 건드리지 않는다. 스크립트·필드가 바뀐 뒤에는 반드시 Overwrite.
   배치 실행: `Unity.exe -batchmode -nographics -projectPath ... -executeMethod Game.Bootstrap.Editor.CoreLoopSetup.Regenerate -quit -logFile ...`.
3. 콘솔(또는 로그)에 `[CoreLoopSetup] Core loop scenes are ready`가 뜨면 Play. `Core > Play From Bootstrap`이 기본으로 켜져 있어 어떤 씬이 열려 있든 `Bootstrap.unity`에서 시작해 열려 있던 씬(Start/Play)으로 돌아온다.
4. 빌드는 `EditorBuildSettings`에 `[Bootstrap, Start, Play]` 순으로 등록된다. SampleScene은 목록에서 빠지지만 파일은 그대로다.

생성기가 만드는 것:

| 경로 | 내용 |
| --- | --- |
| `Assets/GameAssets/Placeholder/Square.png`, `Circle.png` | 64x64 흰 스프라이트(PPU 100). 없을 때만 |
| `Assets/GameAssets/Placeholder/Fonts/MonaS12.ttf` | 생성기가 만들지 않고 저장소에 둔 TTF. 없으면 오류 로그 후 내장 `LegacyRuntime.ttf`(한글 미표시) |
| `Assets/GameAssets/Design/GameConfig.asset` | `GameConfigAsset`. 없을 때만 만들며 값은 보존된다 |
| `Assets/GameAssets/UI/SettingsPanel.prefab` | 설정 패널(볼륨 슬라이더 3개, 닫기·타이틀로·게임 종료·개발자 설정) + 안쪽 `ConfigPanel` |
| `Assets/Scenes/Start.unity` | 시작 씬 |
| `Assets/Scenes/Play.unity` | 플레이 씬 |
| `Assets/Scenes/Bootstrap.unity` | Manager 배치 씬. 게임 씬이 아니며 `SceneSettings.scenes`에 넣지 않는다 |
| `Assets/Scripts/Bootstrap/StartScene.asset`, `PlayScene.asset` | 기본 `SceneReference` |
| `Assets/Scripts/Bootstrap/SceneSettings.asset` | `scenes = [Start, Play]`, `bootstrapScene = Bootstrap.unity` |
| (스포너 생성기) `Assets/GameAssets/Design/Spawner/*`, `Assets/GameAssets/Obstacles/{Rock,Fish,Log}.prefab` | 없으면 코어루프 생성기가 먼저 호출. **Overwrite면 스포너 생성기도 Overwrite로 호출**해 기본값에 없는 구판 Shark/Whale 프리팹·정의·패턴을 지운다 |
| (컷신 생성기) `Assets/GameAssets/UI/CutsceneDialogue.prefab` | 없으면 코어루프 생성기가 먼저 호출. 컷신 자체는 코드(`Assets/Scripts/Cutscene/Scenes/`)라 만들 에셋이 없다 |
| (보스 생성기) `Assets/GameAssets/Boss/BossSet.prefab` 등, `Assets/GameAssets/Design/Boss/*.asset`, `Placeholder/HitboxFrame.png` | 없으면 코어루프 생성기가 먼저 호출. Overwrite면 프리팹 재생성(데이터 에셋은 보존) |
| (애니메이션 생성기) `Assets/GameAssets/Design/Animations/Player_Swim/LaneUp/LaneDown/Jump.asset`, `Art/물고기 애니메이팅/**/*.png.meta` | 항상 호출(없는 것과 프레임 수가 어긋난 것만 만들고 임포트 설정은 매번 맞춘다. 옛 `Player_Idle.asset`은 지운다) |
| (환경 생성기) `Assets/GameAssets/Environment/Environment.prefab`, `Placeholder/Env_*.png` | 없으면 만든다. 프리팹은 `Regenerate Environment Prefab (Overwrite)`로만 덮는다 |

## UI 규칙 (모든 Canvas, 모든 에이전트 공통)

| 항목 | 값 |
| --- | --- |
| Canvas Render Mode | **Screen Space Camera**. `worldCamera` = 그 씬의 Main Camera, `planeDistance` 1, `sortingOrder`로 겹침 순서 |
| CanvasScaler | Scale With Screen Size, 기준 해상도 **1920x1080**, Match Width Or Height 0.5 |
| 화면비 | **16:9 고정**. Main Camera 오브젝트에 `GameCamera : MonoThing` 호스트 + `AspectModule`이 `Camera.rect`를 레터박스/필러박스로 맞춘다(창 크기 변경 시 갱신). `Letterbox Camera`(cullingMask 0, depth -100, Solid Color 검정)가 바깥을 검게 칠한다 |
| 폰트 | `Assets/GameAssets/Placeholder/Fonts/MonaS12.ttf`(OFL 1.1, 한글 완성형 전부). 출처는 같은 폴더 `README.md` |
| 텍스트 | uGUI 레거시 `Text`. 폰트 non-null, 색 알파 1, RectTransform 크기 > 0을 생성기가 보장 |

Core의 `CanvasGroupFadeTransition`(Bootstrap 씬)은 Overlay 그대로 둔다(Core 읽기 전용, 전체 화면 페이드라 상관없음).

생성기 헬퍼(`CoreLoopSetup`, `Assets/Scripts/Bootstrap/Editor/`): `CreateCamera()`가 Letterbox Camera + Main Camera(`GameCamera` 호스트)를 만들고, `CreateCanvas(name, camera, sortingOrder)`가 위 규칙의 Canvas를 만든다. UI 요소는 `CreateText`, `CreateButton`, `CreateImage`, `CreateSlider`, `CreateInputField`, `Place`, `Stretch`를 쓴다. **헬퍼 인자의 픽셀 값은 1280x720 기준**이고 `Place`/`Stretch`/`CreateText`가 `UiScale`(1.5)을 곱해 1920x1080에 맞춘다. 새 Canvas를 만드는 에이전트는 이 헬퍼를 호출하거나 같은 값을 쓴다. 프리팹(`SettingsPanel.prefab`) 안에는 Canvas가 없고 부모 Canvas를 따른다.

`Game.View`(`Assets/Scripts/View/`, Core.Modules만 참조)에 `GameCamera`, `AspectModule`이 있다. 카메라를 새로 두는 씬은 `GameCamera`를 붙인다.

## 씬 구성

### Bootstrap.unity

`Core/Docs/Bootstrap.md`의 Playground 구성에서 `GestureManager`를 뺀 것이다.

```text
SceneController (SceneSettings, Transition 연결)
  Transition (CanvasGroupFadeTransition)
ResourceManager
PoolManager
AudioManager + AudioListener
InputManager
Bootstrap (GameBootstrap: inputActions=GameInput.inputactions, audioManifest, configAsset, dialogueCsv, firstScene=StartScene)
```

`GameBootstrap.Start` 순서: ResourceManager → PoolManager → InputManager → AudioManager(매니페스트) → Audio/Pool Bridge → `GameConfig.Load` → `DialogueService.Load` → `SettingsService.LoadAsync` + `Apply` → `RecordService.LoadAsync` → `EnableMap("Player")`, `EnableMap("UI")` → 첫 씬 로드. 게임 씬 카메라에는 `AudioListener`를 두지 않는다.

### Start.unity

```text
Letterbox Camera (검정, cullingMask 0)
Main Camera (orthographic 3.6, GameCamera[AspectModule], AudioListener 없음)
Canvas (Screen Space Camera, 1920x1080)
  Title "초지일관 : 귀향 (임시)"
  StartButton / SettingsButton / QuitButton
  Hint "↑↓ : 레인 이동 / 가장 위에서 ↑ : 점프"
  Record "기록: -" (자리만)
  SettingsPanel (프리팹 인스턴스, 비활성, titleScene 비움 → "타이틀로" 숨김)
TitleScreen (버튼·패널·PlayScene 참조)
EventSystem (InputSystemUIInputModule)
```

### Play.unity

```text
Letterbox Camera
Main Camera (GameCamera[AspectModule])
Global Light 2D (Global, intensity 1) ── 기본 스프라이트 머티리얼이 Lit이라 없으면 검게 나온다
Environment (프리팹 인스턴스, EnvironmentThing 호스트: Sky/Far/Mid/Near/Homeland/Flow 층 + WaterSurface(Game.Water) + CalmOverlay. 정렬 -9~-1. 옛 Background 사각형은 없앴다)
LaneGuides/LaneGuide0..2 (레인 y에 옅은 가로선 -5. 환경 층 사이에 들어가 그대로 둠)
ScrollRoot (StageScroller 호스트[ScrollModule]) ── 스포너가 풀 오브젝트를 이 아래에 둔다
ObstacleSpawner (ObstacleSpawner 호스트[SpawnModule], settings=Design/Spawner/ObstacleSpawnerSettings, scrollRoot=ScrollRoot)
Player (Art/물고기 애니메이팅/기본/물고기 기본-1 스프라이트, localScale playerScale 0.29, LanePlayer 호스트[LaneMoveModule, JumpModule, HurtboxModule, HitReactionModule, SpriteAnimatorModule, WaterInteractorModule] + CutsceneActor(player), Rigidbody2D Kinematic + useFullKinematicContacts, BoxCollider2D trigger = 11장 합집합 알파 박스×0.87. 클립 swim/laneUp/laneDown/jump 연결)
CutsceneBoss / CutsceneLandmark (숨김 스프라이트, CutsceneActor)
CutsceneDialogue (프리팹 인스턴스, Canvas sortingOrder 100)
CutscenePlayer (MonoThing 호스트[CutscenePlaybackModule], clips = player_jump/player_swim)
BossSet (프리팹 인스턴스: BossDirector + FishingLineBoss/WalrusBoss/WaterfallBoss 비활성. 공격체마다 HitboxView + HitboxFrame 자식, 바늘·바다코끼리 몸에 WaterInteractor)
PlayFlow (DomainSingleton, environment 참조)
HudCanvas (Screen Space Camera, PlayHud)
  ProgressBar/Fill (fillAmount = Progress01)
  Distance "구간 {0} · 남은 {1:0}초"
  PauseButton "일시정지"
  ControlHint "↑↓ : 레인 이동 · 가장 위에서 ↑ : 점프" (구간 1 시작 시 controlHintDuration 초, 기본 비활성)
  JumpCooldown (JumpCooldownView: Gauge, Icon)
  BossTimer (BossTimerView, 상단 중앙, 비활성 자리)
  PausePanel (비활성: 계속 / 설정 / 타이틀로)
  ResultPanel (비활성: "처음 품은 뜻을, 끝까지. 初志一貫" 또는 "부딪혔다…", 다시 / 타이틀로)
  SettingsPanel (프리팹 인스턴스, 비활성, titleScene=StartScene)
EventSystem
```

## 스크립트 책임

| asmdef | 경로 | 타입 | 책임 |
| --- | --- | --- | --- |
| `Game.Config` | `Assets/Scripts/Config/` | `GameConfigValues` | 직렬화 값. private `[SerializeField]` + public 프로퍼티, `Clone`, `Validate` |
| | | `GameConfigAsset` | `Create > Team1004 > Game Config` ScriptableObject |
| | | `GameConfig` | 정적. `Load(asset)`, `Current`, `ToJson`, `TryApplyJson`, `SaveOverride`, `ResetOverride`, `Changed` |
| `Game.Player` | `Assets/Scripts/Player/` | `LanePlayer : MonoThing` | 플레이어 호스트. `Awake`에서 `LaneMoveModule`·`JumpModule`·`HurtboxModule`·`HitReactionModule`·`SpriteAnimatorModule`·`WaterInteractorModule`을 붙이고 `Player/LaneUp`·`LaneDown` 리스너를 `OnEnable`/`OnDisable`에서 등록·해제한다. 공개 API `CurrentLane`, `IsMoving`, `IsAirborne`, `JumpCooldownRemaining`, `CanJump`, `InputEnabled`, `IsVulnerable`, `SnapToLane`, `PlaySwim`, `Animator`, `Water`, `JumpRequested`, `Jumped`, `Landed`, `LaneChanged`, `Hit`는 모듈에 위임한다. `InputEnabled=false`면 `LaneMoveModule`·`JumpModule`·`SpriteAnimatorModule`의 `IsEnabled`도 꺼진다. 클립 `swimClip/jumpClip/idleClip`은 생성기가 연결 |
| | | `LaneMoveModule : Module` | `Ticks = Update`. `TryMoveUp`/`TryMoveDown`/`SnapToLane`, `OnUpdate`에서 `LaneMoveDuration` 동안 y 보간. `IsAtTop`, `LaneChanged` |
| | | `HitReactionModule : Module` | `Ticks = Update`. `PlayAsync(정지, 지속, 밀림)`: `HitStopDuration`만큼 정지 → 흔들림+색 깜빡임 → x −`HitPushDistance`. `Awaitable` 반환 |
| | | `JumpModule : Module` | `Ticks = Update`. `TryJump`(상단 y에서 포물선, `JumpDuration`·`JumpHeight`), 착수 시 `CooldownRemaining = JumpCooldown`, `ResetCooldown()`. `IsAirborne`, `AirborneRemaining`, `CanJump`, `Cooldown01`, `Jumped`, `Landed` |
| | | `HurtboxModule : Module` | `Ticks = Update`. 겹친 `Hazard`를 HashSet에 보관하고 `IsVulnerable`일 때 `Hit` 한 번 발생. `GameConfig.debugInvincible`이면 판정하지 않는다. 트리거 콜백은 호스트 `LanePlayer`의 `OnTriggerEnter2D/Exit2D`가 받아 `AddOverlap/RemoveOverlap`으로 넘긴다. `LanePlayer.Hit`, `HasHit`, `ResetHit`가 이를 노출 |
| | | `Hazard : MonoThing` | 장애물 마커 호스트. `Kind` 문자열. 스포너 프리팹 루트에 붙이며 판정은 `GetComponentInParent<Hazard>()` |
| `Game.Settings` | `Assets/Scripts/Settings/` | `SettingsData` | master/bgm/sfx 볼륨. Newtonsoft가 읽도록 `[JsonProperty]` 프로퍼티 |
| | | `SettingsService` | 정적. `SaveService<SettingsData>(PlayerPrefsSaveStorage, Shared("Settings"), 1)`. `LoadAsync`/`SaveAsync`/`Apply`(AudioManager 볼륨) |
| | | `SettingsPanel` | 슬라이더 즉시 반영, 닫을 때 저장. 타이틀로(참조 없으면 숨김), 게임 종료, 개발자 설정 |
| | | `ConfigPanel` | `GameConfig.ToJson()` 편집 → 적용(`TryApplyJson`+`SaveOverride`) / 초기화(`ResetOverride`) |
| | | `RecordData`, `RecordService` | 메타 기록. `SaveService<RecordData>(PlayerPrefs, Shared("Records"), 1)`. `ClearCount`만 저장. `ReportClearAsync()`, `Changed` |
| | | `UiSound` | `ui_click` 재생 헬퍼 |
| `Game.Play` | `Assets/Scripts/Play/` | `PlayFlow` | 구간 프레임(시작 스토리→진행 1→컷신 1→보스 1→…→진행 4→엔딩)을 데이터(`FlowStep` 목록)로 돌린다. `Distance`(총 거리), `Section`, `Progress01`(구간 진행도), 체크포인트, 충돌 연출→실패, 기록 갱신, 스포너 호출, 일시정지, 재도전/타이틀 |
| | | `StageScroller : MonoThing` | `ScrollRoot` 호스트. `Awake`에서 `ScrollModule`을 붙이고 `GameConfig.Changed`에 따라 `Speed` 갱신. `SetScrolling(bool)`은 `PlayFlow`가 상태 전이마다 호출 |
| | | `ScrollModule : Module` | `Ticks = Update`. `IsScrolling`이면 자식들을 `x -= Speed * dt`. `Speed`, `IsScrolling`, `Scrolled` 프로퍼티 |
| | | `PlayHud`, `PausePanel`, `ResultPanel` | UI. 버튼 핸들러는 코드에서 `onClick.AddListener`로 연결. `PlayHud.ShowControlHint(초)`가 조작 힌트를 켜고 scaled 시간으로 끈다(`IsControlHintVisible`) |
| | | `BossTimerView` | UI. 상단 중앙 30초 카운트다운 자리. `Show(초)`, `SetRemaining(초)`, `Hide()`. 값은 보스 모듈이 준다. 지금은 비활성 |
| | | `JumpCooldownView` | UI. 좌측 하단 원형 게이지(Radial360, 시계 방향)와 아이콘 밝기. `PlayFlow.Current.Player.Jump`를 매 프레임 읽는다 |
| `Game.Title` | `Assets/Scripts/Title/` | `TitleScreen` | 시작/설정/종료, 타이틀 BGM |
| `Game.Bootstrap` | `Assets/Scripts/Bootstrap/` | `GameBootstrap` | Manager 초기화 → `GameConfig.Load` → `DialogueService.Load(dialogueCsv)` → 설정·기록 로드 → 첫 씬 이동. `GameInput.inputactions`, `AudioManifest.json` 동거 |
| `Game.Bootstrap.Editor` | `Assets/Scripts/Bootstrap/Editor/` | `CoreLoopSetup` | 생성기 메뉴 |

의존 방향: `Config ← Player ← Play`, `Config ← Settings ← Play/Title`, `Play → Cutscene, Spawner, Environment`, `Player → Animation, Water`, `Cutscene → Animation`, `Environment → Water`, `Boss → Core.Audio`, `Bootstrap → Config, Settings`. 순환 없음. Core 타입은 전역 네임스페이스라 `using` 없이 쓴다.

입력 맵(`GameInput.inputactions`): `Player/LaneUp`(↑, W), `Player/LaneDown`(↓, S), `UI/Pause`(Esc), `UI/Retry`(R, 실패 화면에서만), `Cutscene/Advance`(Space). 기존 `Assets/InputSystem_Actions.inputactions`는 쓰지 않는다.

오디오 ID(`AudioManifest.json`): `ui_click`, `lane_move`, `jump`, `land`, `hit`, `stage_clear`, `stage_fail`, `boss_telegraph`, `boss_attack`, `boss_clear`, `cutscene_transition`, `ending`, `bgm_title`, `bgm_play`. 보스 3종은 미리 등록만 했다(보스 모듈이 재생). 클립은 `Assets/GameAssets/Audio/Resources/Audio/`에 복사본으로 두었다(원본 유지).

## 상태 흐름

```text
Ready ─시작 스토리(Intro)─▶ 진행 1 ─컷신 1─▶ 낚싯줄(Boss 1) ─▶ 진행 2 ─컷신 2─▶ 바다코끼리(Boss 2) ─▶ 진행 3 ─컷신 3─▶ 폭포(Boss 3) ─▶ 진행 4 ─엔딩 스토리─▶ Cleared
                            Running                  Boss                                                                                          ResultPanel "처음 품은 뜻을, 끝까지."
                   │ Hit ─0.1초 정지·흔들림·밀림─▶ Failed ─▶ ResultPanel "부딪혔다…"
                   └ Esc/버튼 ─▶ Paused(timeScale 0, Running·Cutscene·Boss에서) ─Esc/계속─▶ 직전 상태
```

`PlayState`: `Ready`, `Running`, `Cutscene`, `Boss`, `Hit`, `Paused`, `Cleared`, `Failed`.

- `PlayFlow.Start`: BGM 전환 → `Ready` → `RunFlowAsync()`. 흐름은 `BuildSteps()`가 `GameConfig.SectionCount`(4)로 만든다: `Intro, Section1, Cutscene1, Boss1, Section2, Cutscene2, Boss2, Section3, Cutscene3, Boss3, Section4, Ending`.
- `Section n`: `sectionTime = 0`, `sectionDuration = GetSectionDuration(n)`(25/30/35/15초), 스포너 `BeginSectionByDuration(n−1, sectionDuration)`(GameConfig의 구간 시간을 그대로 넘긴다. 스포너 기본값 `SpawnerDefaults.GetSectionDuration`과 같은 값이어야 하며 `GameConfig.asset`에 25/30/35/15로 넣어 두었다), `Running`. 종료 조건은 `SectionTime ≥ 시간`이고 스포너가 있으면 추가로 `IsSectionExhausted`(스폰 종료 + 활성 장애물 0). 구간 시작 유예 `sectionStartGrace`(2초) 동안은 장애물이 없고 HUD의 남은 초는 그대로 줄어든다.
- `Boss n`: `Boss` 상태(입력 허용, 스크롤 정지). `BossSet.prefab`의 `BossDirector`가 `Start`에서 `PlayFlow.Current.BossHandler = RunBossAsync`를 걸어 두므로 `PlayFlow`는 `await BossHandler(n)`만 한다(`Docs/Boss.md` 통합 절차). `true`면 `boss_clear` 효과음 후 다음 구간, `false`면 `Fail()`(체크포인트는 보스 직전이라 재도전 시 보스부터). 핸들러가 없으면 즉시 통과. 보스 타이머는 `BossDirector`가 `PlayFlow.BossTimer`로 갱신한다.
- `Ending`: `Ending.asset` 재생 → `Clear()` → `ending` 효과음, 기록 갱신(엔딩 도달), 결과 패널.
- 상태가 `Running`/`Boss`가 아니면 `LanePlayer.InputEnabled = false`(플레이어 애니메이터도 멈춤). `Running`이 아니면 `StageScroller.SetScrolling(false)`. 환경(`EnvironmentThing`)은 `Running`·`Boss`에서만 흐른다(보스 중 물은 흐르고 장애물 스크롤만 멈춘다. 결정: `Docs/Environment.md`). `Cutscene`이면 HUD Canvas를 끈다.
- 구간 1이 시작되면 HUD가 조작 힌트를 `controlHintDuration`(2.5초) 동안 보인다(4번 11절: 2~3초, 별도 튜토리얼 없음). 구간 4에 들어가면 `environment.SetHomeland(true)`.
- 충돌: `LanePlayer.Hit` → `Hit` 상태(스포너 `Stop`, 스크롤·입력 정지) → `hit` 효과음 → `HitReactionModule.PlayAsync(HitStopDuration 0.1, 0.25, HitPushDistance 0.4)` → `Fail()` → `stage_fail`, 기록 갱신, 결과 패널. 피격 후 무적 없음.
- Esc: Running/Cutscene/Boss→Pause, Paused→직전 상태로 Resume. 설정 패널이 열려 있으면 Esc는 설정을 닫는다. 컷신 중 일시정지는 `timeScale 0`이라 DOTween 트윈·타자·Space가 함께 멈춘다.
- 실패 화면(3판 18절): "실패" + "다시하기"만(타이틀 버튼 숨김). `R` 키(`UI/Retry`)로도 다시하기. `Retry()`는 플레이 씬을 다시 로드하고 체크포인트(현재 구간 시작 또는 현재 보스 시작)부터 시작한다. `GoTitle()`은 체크포인트를 지운다(`SettingsPanel`의 "타이틀로"는 `SceneController`를 직접 부르므로 지우지 않는다. 알려진 제한).

레인 이동 규칙(기획서 7~8): 이동 중 입력 무시, 예약 없음, 하단에서 ↓ 무시, 상단에서 ↑는 점프. 이동 중과 공중에서는 `IsVulnerable == false`라 충돌 판정을 하지 않는다.

## 팀 규칙: 씬 오브젝트는 MonoThing 호스트 + Module

| 대상 | 기반 클래스 |
| --- | --- |
| 씬에 배치되는 게임 오브젝트(플레이어, 장애물, 스크롤 루트, 보스, 연출 오브젝트) | `MonoThing` 호스트 + 기능별 `Module` (`Core/Docs/Modules.md`) |
| UI(Canvas 아래 패널, HUD, 버튼 바인딩) | `MonoBehaviour` 그대로 |
| 앱 전역 매니저 | `Singleton<T>` (Bootstrap 씬 배치) |
| 씬 범위 흐름(`PlayFlow`) | `DomainSingleton<T>` |

호스트 규칙:

- 호스트는 `Update`/`LateUpdate`/`FixedUpdate`/`OnDestroy`를 직접 선언하지 않는다. `MonoThing`의 것이 가려져 모듈이 틱을 받지 못한다. 필요하면 `OnThingUpdate`/`OnThingLateUpdate`/`OnThingFixedUpdate`/`OnThingDestroy`를 오버라이드한다. `Awake`, `Start`, `OnEnable`, `OnDisable`, 물리 콜백은 호스트가 선언해도 된다.
- 기능은 `Awake`에서 `AddModule`로 붙인다. 모듈은 일반 C# 객체라 의존성은 생성자로 받고 `Ticks`로 필요한 틱만 선언한다. `OnUpdate()`는 인자가 없으므로 `Time.deltaTime`을 모듈 안에서 읽는다.
- Unity 콜백(트리거·충돌·입력 리스너)은 MonoBehaviour에만 오므로 **호스트가 받아 모듈 메서드로 위임**한다. 판정·상태는 모듈에 둔다.
- 다른 시스템이 구독할 이벤트는 호스트가 노출한다(모듈 이벤트를 호스트가 다시 발생).
- `IsEnabled=false`로 모듈 틱만 멈출 수 있다. 컷신·결과 화면에서 입력을 끌 때 함께 끈다.
- 호스트가 속한 asmdef는 `Core.Modules`를 참조한다. 호스트 타입을 쓰는 다른 asmdef(예: `Game.Play`, `Game.Bootstrap.Editor`)도 기반 클래스 해석을 위해 `Core.Modules`를 참조해야 한다.

현재 적용: `LanePlayer`(LaneMoveModule, JumpModule, HurtboxModule), `StageScroller`(ScrollModule), `Hazard`(마커 호스트, 모듈 없음). 새 오브젝트를 추가할 때의 형태:

```csharp
public sealed class Shark : Hazard
{
    [SerializeField] private float speed = 6f;

    private SharkMoveModule move;

    private void Awake()
    {
        move = AddModule(new SharkMoveModule(transform, speed));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        move.OnTouched(other);
    }

    protected override void OnThingDestroy()
    {
        move = null;
    }
}

public sealed class SharkMoveModule : Module
{
    private readonly Transform target;
    private readonly float speed;

    public SharkMoveModule(Transform target, float speed)
    {
        this.target = target;
        this.speed = speed;
    }

    protected override ModuleTick Ticks => ModuleTick.Update;

    protected override void OnUpdate()
    {
        target.position += Vector3.left * (speed * Time.deltaTime);
    }

    public void OnTouched(Collider2D other)
    {
    }
}
```

### 플레이어

플레이어 기능은 컴포넌트가 아니라 `Module`로 추가한다. 레인 이동은 `LaneMoveModule`, 점프는 `JumpModule`, 피격 판정은 `HurtboxModule`이다. `LanePlayer`가 `OnTriggerEnter2D/Exit2D`를 받아 `HurtboxModule.AddOverlap/RemoveOverlap`으로 넘기고, 모듈의 `Hit`/`Jumped`/`Landed`/`LaneChanged`를 같은 이름의 호스트 이벤트로 다시 발생시킨다. 별도의 Hurtbox MonoBehaviour는 없다.

`JumpModule`은 이미 구현되어 있다(아래 「점프」). 물속/이동/점프/착수 상태가 늘어나면 `Game.StateMachine`의 `StateMachineModule<TContext>`(다른 에이전트가 `Assets/Scripts/StateMachine/`에 제작 중)로 상태를 정리할 수 있다.

`InputEnabled`를 끄면 `LaneMoveModule.IsEnabled`와 `JumpModule.IsEnabled`도 함께 꺼져 이동 중이던 보간·공중 궤적·쿨타임이 그 자리에서 멈춘다. 결과 화면·컷신처럼 플레이가 끝난 뒤의 상태이므로 문제 삼지 않았다.

## 점프 (기획서 8~10절)

- 상단 레인에서 ↑(`Player/LaneUp`)를 누르면 점프한다. 별도 키는 없다. `LanePlayer.OnLaneUp`이 `laneMove.IsAtTop`이면 `JumpRequested`를 발생시킨 뒤 `JumpModule.TryJump()`를 직접 호출한다(모듈이 이벤트를 구독하는 대신 호스트가 호출: 순서가 명확하고 구독 해제가 필요 없다). `JumpRequested`는 알림용으로 남겼다.
- 이동 중, 공중, 쿨타임 중이면 아무 일도 없다. 입력 예약은 없다. 공중에서는 `CanAcceptInput()`이 `false`라 `LaneMoveModule.TryMove*`가 호출되지 않는다.
- 궤적: `y = 상단 레인 y + 4 · JumpHeight · t · (1 − t)`, `t = elapsed / JumpDuration`. `t ≥ 1`이면 착수해 y를 상단 레인으로 되돌리고 `CooldownRemaining = JumpCooldown`을 세팅한다. 쿨타임은 착수 순간부터 줄어든다.
- 공중에서는 `IsAirborne`이 `true`라 `IsVulnerable`이 `false`이고 `HurtboxModule`이 판정하지 않는다. 착수한 프레임부터 판정이 재개된다.
- 효과음: `Jumped` → `jump`, `Landed` → `land` (`PlayFlow`가 재생).
- 물 튀김: `LanePlayer`의 `WaterInteractorModule`(`Game.Water`)이 매 프레임 y 속도를 수면 시뮬레이션에 넘긴다. 점프로 수면(`WaterSurfaceY` 2.0)을 뚫으면 솟고 착수하면 눌린다. `Docs/Environment.md` 「수면 상호작용」. `EnvironmentThing.Splash`는 컷신용.

```text
Underwater(top lane) ──↑, CanJump──▶ Airborne(JumpDuration) ──t≥1──▶ Landed → Cooldown(JumpCooldown) ──0──▶ Underwater
```

쿨타임 UI(`JumpCooldownView`, HUD 좌측 하단 96px 원): 배경 원(반투명 검정) / 게이지 원(`Image.Type.Filled`, `Radial360`, `Origin360.Top`, 시계 방향) / 아이콘 원(임시 흰 원 36px). 점프 가능이면 게이지 100%·아이콘 밝음, 공중이면 게이지 0%·아이콘 어두움, 쿨타임 중이면 `1 − CooldownRemaining / JumpCooldown`만큼 차오르고 완료되면 아이콘이 다시 밝아진다. 숫자는 표시하지 않는다.

## 구간 프레임과 체크포인트 (2판 4·15·17·26절)

| 항목 | 구현 |
| --- | --- |
| 구간 수·시간 | **3판 9절 확정**: `GameConfig.SectionDurations` `[25, 30, 35, 15]`초. `SectionCount`가 흐름의 구간 수. 구간은 `SectionTime ≥ 시간`으로 끝난다(거리 아님). `GetSectionLength = 시간 × ScrollSpeed`는 스포너 호환용 |
| 난이도 | 스포너 `DifficultyCurve`가 구간 인덱스(0~3)별로 속도·빈도를 올린다. `PlayFlow`는 인덱스만 넘긴다 |
| 진행 4 | 스포너 곡선이 70% 지점부터 스폰을 멈추고 속도를 내린다. `PlayFlow` 별도 처리 없음. 구간 4 끝에 `Ending` 컷신 |
| 체크포인트 | 정적 `PlayFlow.CheckpointStep`(세션 메모리). `Section`·`Boss` 단계에 들어갈 때 그 단계 인덱스를 기록한다. 재도전(`Retry`) 시 `RunFlowAsync`가 그 단계부터 시작하고, 건너뛴 구간 길이만큼 `Distance`를 미리 더하고 건너뛴 컷신은 `ApplyFinalState`로 최종 상태만 적용한다. 앱 재시작·타이틀 복귀(`GoTitle`)면 처음부터. 저장하지 않는다(사용자 결정) |
| 본 컷신 | 정적 `PlayFlow.SeenCutscenes`(세션 메모리). 재도전 시 스킵 |

## 충돌 연출 (2판 16절)

`HitReactionModule`(플레이어 호스트)이 한다. 순서: `HitStopDuration`(0.1초) 동안 플레이어·스크롤·스포너 정지(상태 `Hit`, `timeScale`은 건드리지 않음) → 0.25초 동안 y 흔들림(진폭 0.08, 60Hz 감쇠) + 스프라이트 흰색 깜빡임(30Hz) + x를 `HitPushDistance`(0.4 unit)만큼 왼쪽으로 밀림 → `Fail()`. 체력·목숨·무적 없음. DOTween을 쓰지 않고 모듈 틱으로 계산한다(Game.Player가 DOTween을 참조하지 않게).

## HUD (2판 30절)

| 위치 | 내용 | 스크립트 |
| --- | --- | --- |
| 좌측 상단 | `구간 {Section} · 남은 {SectionRemaining:0}초` | `PlayHud` |
| 상단 중앙 | 진행 바(구간 시간 진행도 `Progress01`, 3판 19.3절 선택 UI) 아래에 보스 타이머: 보스 이름 한 줄("낚싯줄"·"바다코끼리"·"폭포", `PlayFlow.bossNames`) + 큰 정수 초(30→29…). HP바 없음. `BossDirector`가 `SetBoss(BossThing.DisplayName)`, `SetVisible`, `SetRemaining`으로 갱신한다(`PlayFlow.bossNames`는 핸들러가 없을 때의 대체 이름) | `BossTimerView` |
| 좌측 하단 | 원형 점프 쿨타임 UI | `JumpCooldownView` |
| 우측 상단 | 일시정지 버튼 | `PlayHud` |

## 기록 (2판 28~29절, roy0206 결정으로 축소)

게임 길이가 고정이라 최고 거리·최고 구간·최근 기록은 무한 러너의 잔재로 판단해 **클리어 횟수만** 저장한다. `Game.Settings.RecordService`(PlayerPrefs, `Shared("Records")`, 버전 1): `RecordData.ClearCount` 하나. `GameBootstrap`이 `LoadAsync`, `PlayFlow.Clear()`(엔딩 컷신 완료 뒤 한 번)가 `ReportClearAsync()`(`ClearCount++` 후 저장). 실패 시에는 아무것도 저장하지 않는다. 엔딩 도달 여부는 `ClearCount > 0`(`RecordService.EndingReached`). 읽기 프로퍼티 `ClearCount`와 `Changed` 이벤트가 있어 나중에 시각화에 쓴다. 타이틀은 "클리어 n회" 또는 "아직 클리어 없음"을 표시한다. 체크포인트·본 컷신은 저장하지 않고 세션 메모리로 둔다.

## 스포너 연결 (`Docs/Spawner.md` 통합 절차 반영)

| 항목 | 내용 |
| --- | --- |
| asmdef | `Game.Play` → `Game.Spawner`, `Game.Bootstrap.Editor` → `Game.Spawner`, `Game.Spawner.Editor` |
| 씬 | `ObstacleSpawner` 호스트(`settings` = `Design/Spawner/ObstacleSpawnerSettings.asset`, `scrollRoot` = `ScrollRoot`). 테스트 바위 3개는 제거 |
| 초기화 | 첫 구간 시작 시 한 번 `Initialize(settings, ScrollRoot, seed)`. seed는 `GameConfig.SpawnSeed`(0이면 UTC 틱 기반) |
| 구간 | `BeginSectionByDuration(section − 1, GetSectionDuration(section))`. `Running`인 프레임마다 `Advance(dt, Distance, SpawnPlayerState.FromPlayer(player, JumpCooldownRemaining, AirborneRemaining, MoveRemaining))`. 남은 시간 프로퍼티는 `JumpModule.AirborneRemaining`, `LaneMoveModule.MoveRemaining`으로 추가했다 |
| 종료 | `SectionDistance ≥ 길이 && IsSectionExhausted` |
| 실패·피격 | `Stop()`(장애물은 남긴다). 씬 이탈 시 스포너와 `PoolSceneBridge`가 회수 |
| `StageLength` | `[Obsolete]` 별칭(구간 1 길이 = 시간 × ScrollSpeed)으로 남겼다. 코어루프는 더 쓰지 않는다 |
| 장애물 | 3판 10절대로 스포너 생성기가 돌(Rock)·다른 물고기(Fish)·통나무(Log) 프리팹·에셋을 만든다. Shark/Whale은 Overwrite 생성 시 정리된다(스포너 담당) |

## 대사 연결 (`Game.Dialogue`)

컷신 대사는 `Assets/GameAssets/Design/Dialogue/dialogue.csv`(`Team1004 > Import Dialogue CSV`가 `Assets/Documents`의 CSV에서 가져옴)에 있고 컷신 클래스는 `Say("intro.01")`처럼 id로만 참조한다. `GameBootstrap`이 `[SerializeField] TextAsset dialogueCsv`를 `DialogueService.Load`로 읽으며(`GameConfig.Load` 직후), 생성기가 Bootstrap 씬에 CSV를 연결한다. CSV가 없으면 생성기·부트스트랩이 경고를 남기고 컷신 대사는 비어 나온다. `Game.Bootstrap` → `Game.Dialogue` 참조.

## 컷신 연결 (`Docs/Cutscene.md`)

| 항목 | 내용 |
| --- | --- |
| 입력 | `GameInput.inputactions`에 `Cutscene/Advance`(Space) 맵 추가. Bootstrap은 `EnableMap("Cutscene")`을 하지 않는다. 재생기가 켜고 끈다 |
| asmdef | `Game.Play` → `Game.Cutscene`, `Game.Bootstrap.Editor` → `Game.Cutscene`, `Game.Cutscene.Editor`, `Game.Dialogue.Editor` → `Game.Cutscene`(CSV 검증이 `CutsceneCatalog`를 읽는다) |
| 씬 | `Player`에 `CutsceneActor`(id `player`), `CutsceneBoss`(id `boss`, 숨김 사각형), `CutsceneLandmark`(id `landmark`, 숨김 원), `CutsceneDialogue.prefab` 인스턴스(Canvas.worldCamera = Main Camera), `CutscenePlayer`(stageCamera, dialogueView, actors 3) |
| PlayFlow | `cutscene`(`CutscenePlayer`)와 **id 문자열** `introCutsceneId`(`intro`), `sectionCutsceneIds[3]`(`cutscene1`, `cutscene2`, `cutscene3`), `endingCutsceneId`(`ending`) SerializeField. 에셋 참조는 없다. 생성기가 `CutsceneCatalog`의 상수로 채운다. `PlayCutsceneAsync(id)`가 `CutsceneCatalog.TryCreate`로 인스턴스를 만든다. 컷신 시작 시 `cutscene_transition` 효과음 |
| 본 컷신 | 정적 `HashSet<string> PlayFlow.SeenCutscenes`(`CutsceneBase.Id`)에 세션 동안만 기억. 재도전·타이틀 왕복 시 `ApplyFinalState(instance)`(= `Begin` → `Skip` → `ExecuteAsync`)로 건너뛴다. 앱 재시작이면 다시 본다. **저장은 기록 저장(SaveService Progress 슬롯) 작업에서 `List<string> SeenCutscenes`로 옮긴다** |
| 종료 후 | `LanePlayer.SnapToLane(CurrentLane)`으로 위치 복원 |
| 생성기 | `CoreLoopSetup.Run`이 `CutsceneDialogue.prefab`이 없으면 `CutsceneSetup.Generate()`를 먼저 호출한다. 컷신 생성기의 폰트 경로는 `Fonts/MonaS12.ttf`로 맞췄다(사소한 참조 수정) |

씬을 떠나면 `CutscenePlayer` 파괴로 모듈 `OnDetached`가 `CutsceneBase.Cancel()`을 불러 `PlayAsync`가 반환되므로 `PlayFlow`의 `await` 뒤에는 `this == null` 가드가 있다.

## 다른 에이전트용 훅

| 기능 | 붙는 곳 |
| --- | --- |
| 보스 | 연결됨. `BossSet.prefab`(`BossDirector` + 보스 3개)을 생성기가 Play 씬 루트에 둔다. `BossDirector`가 `BossHandler`를 채우고 `PlayFlow.BossTimer`를 갱신한다. 예고·공격 효과음(`boss_telegraph`/`boss_attack`)과 히트박스 표시(`HitboxView`)는 보스 모듈이 한다 |
| 환경 | 연결됨. `Environment.prefab`을 루트에 두고 `PlayFlow.environment`로 스크롤·고향 전환 |
| 애니메이션 | 연결됨. `LanePlayer`(수영 루프 / 레인 위·아래 이동 / 점프)와 `CutsceneBase.Animate/SetPose`(`player_jump`, `player_swim`, `player_lane_up`, `player_lane_down`) |
| 점프 연출 | `LanePlayer.Jumped`/`Landed`, `JumpModule.Progress01`, `GameConfig.WaterSurfaceY`. 수면 반응은 `WaterInteractorModule`이 이미 한다 |
| 보스 3 마지막 점프 | `LanePlayer.ResetJumpCooldown()`(→ `JumpModule.ResetCooldown()`). `WaterfallBreakthroughState.OnEnter`가 호출해 30초 종료 시 점프가 막히지 않는다 |
| 기록 시각화 | `RecordService.ClearCount`, `Changed` 구독 |
| 컷신 | 이미 연결됨. 새 컷신은 `CutsceneBase` 파생 클래스를 `Assets/Scripts/Cutscene/Scenes/`에 만들고 `CutsceneCatalog`에 등록한 뒤 id를 `sectionCutsceneIds`에 넣는다(`Docs/Cutscene.md` 「새 컷신 추가」) |

## GameConfig

값은 코드 상수가 아니라 `Assets/GameAssets/Design/GameConfig.asset`에 있다. 기획이 Inspector에서 바꾼다.

| 필드 | 기본값 | 근거 |
| --- | --- | --- |
| `laneY` | `[1.1, 0, -1.1]` | 기획서 250/360/470px를 카메라 ortho 3.6·1 unit = 100px로 환산 |
| `playerX` | `-4.2` | 화면 폭 12.8의 약 17% 지점(기획서 x≈220px) |
| `laneMoveDuration` | `0.2` | 기획서 |
| `scrollSpeed` | `4.0` | **임시값**. 기획서에 없음 |
| `sectionDurations` | `[25, 30, 35, 15]` | 3판 9절 확정(초) |
| `jumpDuration` | `0.8` | 기획서 |
| `jumpCooldown` | `1.2` | 기획서 |
| `jumpHeight` | `1.6` | **임시값** |
| `waterSurfaceY` | `2.0` | **임시값**. 연출 없이 값만 |
| `hitStopDuration` | `0.1` | 2판 16절 |
| `hitPushDistance` | `0.4` | **임시값**. 2판 16절 "살짝 밀림" |
| `bossDuration` / `bossTelegraphDuration` / `bossFullLaneTelegraphDuration` / `bossAttackDuration` / `bossRecoveryDuration` | `30 / 0.8 / 1.0 / 0.6 / 0.6` | 3판 13절 확정. 3레인 전체 공격 예고는 1.0초. 보스 모듈이 읽는다 |
| `spawnSeed` | `0` | 0이면 실행마다 다른 시드. 고정하면 코스 재현 |
| `playerScale` | `0.29` | 연어 아트(527×280px, PPU 100) 스케일. 몸 높이 약 0.81 unit. **임시값** |
| `playerHitboxScale` | `0.87` | 히트박스 = 알파 박스 × 이 값(4번 17절 G: 스프라이트보다 10~15% 작게) |
| `bossHitboxAlpha` | `0.35` | 보스 공격체 히트박스 프레임 알파(4번 11절 30~40%) |
| `controlHintDuration` | `2.5` | 구간 1 시작 시 조작 힌트 표시 초(4번 11절 2~3초) |
| `debugInvincible` | `false` | **디버그.** true면 `HurtboxModule`이 판정하지 않는다. 개발자 설정 패널 JSON으로 켠다 |

`LaneCount`는 `laneY` 길이, `SectionCount`는 `sectionDurations` 길이다. `StageLength`는 `[Obsolete]`(구간 1 길이 = 시간 × ScrollSpeed).

직렬화는 **Newtonsoft**다(사용자 결정, 프로젝트 통일). `GameConfigJson`의 필드 기반 ContractResolver가 `[SerializeField]` private 필드를 camelCase 필드명 그대로 읽고 쓴다(필드가 늘어도 규칙 유지, 기존 `config_override.json`과 키 호환). `ToJson`은 들여쓰기 출력, `TryApplyJson`은 `PopulateObject`이며 Newtonsoft 예외 메시지를 개발자 패널 상태 텍스트에 그대로 보여 준다. `GameConfig.asset`은 생성기가 없을 때만 만들므로, 에셋이 이미 있는 상태에서 필드가 추가되면 그 필드는 코드의 직렬화 기본값으로 들어간다. Inspector에서 확인한다.

### 빌드에서 값 바꾸기 (개발자 설정 패널)

설정 → 개발자 설정을 누르면 `GameConfig.ToJson()`이 멀티라인 InputField에 표시된다. 값을 고치고 적용을 누르면 `TryApplyJson`이 검증 후 `Current`를 교체하고 `SaveOverride`가 `Application.persistentDataPath/config_override.json`에 저장한다. 다음 실행부터 `GameConfig.Load`가 에셋 값을 복사한 뒤 이 파일로 덮어쓴다. 초기화는 파일을 지우고 에셋 값으로 되돌린다. 실패 사유는 상태 텍스트에 나온다. 값 변경은 `GameConfig.Changed` 이벤트로 알린다.

Windows에서 `persistentDataPath`는 `%USERPROFILE%\AppData\LocalLow\<회사>\<제품>\`이다.

다른 방법과의 비교: 필드별 슬라이더 패널은 보기 쉽지만 필드가 늘 때마다 UI를 고쳐야 하고, 명령줄 인자는 실행 환경을 바꿔야 하며 값 변경마다 재시작이 필요하고, 원격 설정(Remote Config)은 서버와 계정이 필요해 프로토타입에 과하다. JSON 패널은 `GameConfigValues`에 필드를 추가하면 UI 수정 없이 바로 편집·저장·복원이 되고 빌드 중에도 즉시 반영되므로 이 단계에 맞다. 필드가 확정되면 슬라이더 패널로 바꿀 수 있다.

## 통합 지도

Play 씬에서 무엇이 무엇을 들고 있는지.

| 오브젝트 | 호스트 / 모듈 | 참조·연결 | 상태 연결 |
| --- | --- | --- | --- |
| `PlayFlow` | `DomainSingleton` | `player`, `scroller`, `spawner`, `hud`, `bossTimer`, `pausePanel`, `resultPanel`, `cutscene`, `environment`, `playScene`, `startScene`, 컷신 id 5개 | 상태 전이마다 입력·스크롤·환경·HUD를 켜고 끈다. 구간 시작 → 스포너·고향·힌트, 보스 → `BossHandler` |
| `Environment` | `EnvironmentThing`[Parallax×3, WaterFlow, Homeland, WaterSplash] | 프리팹(`Assets/GameAssets/Environment/Environment.prefab`) | `SetScrolling(Running\|Boss)`, `SetHomeland(구간 4)` |
| `Player` | `LanePlayer`[LaneMove, Jump, Hurtbox, HitReaction, SpriteAnimator, WaterInteractor] | 클립 4개(swim/laneUp/laneDown/jump), `BoxCollider2D`(히트박스) | `InputEnabled`가 이동·점프·애니메이터를 함께 멈춤. `Hit` → `PlayFlow.OnHit`. 레인 이동 시작(`LaneMoveModule.MoveStarted`)에 방향 클립 0.2초, 끝(`MoveFinished`)에 수영 루프. 점프 중이면 점프 클립이 이긴다 |
| `ObstacleSpawner` | `ObstacleSpawner`[SpawnModule] | `settings`, `scrollRoot` | `Initialize`(첫 구간) / `BeginSectionByDuration` / `Advance`(Running) / `Stop`(Hit·Failed) |
| `ScrollRoot` | `StageScroller`[ScrollModule] | — | `SetScrolling(Running)` |
| `BossSet` | `BossDirector` + `FishingLineBoss`/`WalrusBoss`/`WaterfallBoss`[StateMachineModule], 자식 `Telegraph`[LaneTelegraphModule], 공격체 `Hazard`+`HitboxView`[HitboxViewModule](+ `WaterInteractor`) | `bosses[3]`, 각 보스 `data`(Design/Boss) | `BossDirector.Start`가 `PlayFlow.BossHandler`를 채움. 예고 → 띠+프레임+`boss_telegraph`, 공격 → 판정+`boss_attack` |
| `CutscenePlayer` | `CutscenePlayer`[CutscenePlaybackModule] | `stageCamera`, `dialogueView`, `actors[3]`, `clips[2]` | `PlayFlow.PlayCutsceneAsync` → `Cutscene` 상태. Space = `Cutscene/Advance` |
| `CutsceneDialogue` | `CutsceneDialogueView`(MonoBehaviour, Canvas 100) | 프리팹 | 대사·화면 페이드 |
| `HudCanvas` | `PlayHud`, `JumpCooldownView`, `BossTimerView`, `PausePanel`, `ResultPanel`, `SettingsPanel`(→`ConfigPanel`) | — | `Cutscene`이면 Canvas 비활성. 힌트는 `ShowControlHint` |
| `Main Camera` | `GameCamera`[AspectModule] | — | 16:9 레터박스. 컷신이 위치·orthoSize를 빌렸다 돌려준다 |

Bootstrap 씬: `GameBootstrap`(`inputActions`, `audioManifest`, `configAsset`, `dialogueCsv`, `firstScene`) + Manager 5개.

## 수동 플레이 체크리스트

배치 테스트가 보지 못하는 것(모양·타이밍·체감)을 사람이 본다. `Core > Play From Bootstrap`이 켜진 상태에서 Play.

1. **타이틀**: 제목, "아직 클리어 없음", 힌트 문구. 설정 → 볼륨 슬라이더 3개, 개발자 설정 → JSON 패널에 `playerScale`, `debugInvincible`, `bossHitboxAlpha`, `controlHintDuration`이 보이는지.
2. **시작 → 인트로 컷신**: 암전 → 자갈밭 → 성체 등장. Space로 넘김. 대사 한글이 보이는지.
3. **구간 1 시작**: 화면 아래쪽에 조작 힌트가 약 2.5초 나왔다 사라지는지. 배경 층(하늘·원경·중경·바닥·물결·수면)이 왼쪽으로 흐르고 레인 가이드 선이 읽히는지. 연어가 가운데 레인에 레인 간격 안에 들어오는 크기인지.
4. **이동·점프**: ↑↓로 레인 이동, 상단에서 ↑로 점프. **레인 이동 중 올라가기/내려가기 2프레임이 0.2초에 넘어가고 도착하면 수영 루프로 돌아오는지**, 클립이 바뀔 때 연어가 위아래로 튀지 않는지(공유 pivot). 점프 중 5프레임 애니메이션이 0.8초에 걸쳐 넘어가고 착수 후 수영 포즈로 돌아오는지. **수면이 점프 때 솟고 착수 때 눌리는지**(세기는 `WaterSettings.asset`). 좌측 하단 쿨타임 원이 차오르는지.
5. **장애물**: 돌·물고기·통나무가 레인에 맞게 오고, 히트박스가 연어 몸보다 살짝 작아 보이는지(스치는 느낌이 과하면 `playerHitboxScale`).
6. **컷신 → 보스 1(낚싯줄)**: 컷신 뒤 상단 중앙에 "낚싯줄 30". 예고 시 레인 띠(30~40%)와 바늘 주변 빨간 프레임, 예고음 → 공격 0.2초 전 띠가 밝아짐 → 공격음과 함께 바늘 하강. 바늘이 수면을 뚫을 때 물이 반응하는지. 30초 뒤 통과음.
7. **보스 2(바다코끼리)**: 1레인 입/2레인 팔 크기에 맞춰 프레임이 늘어나는지, 돌진 후 복귀.
8. **보스 3(폭포)**: 급류·돌이 지나갈 때 프레임이 같이 움직이는지. 30초 뒤 거대한 폭포가 들어오고 점프 가능해지면 깜박임. 점프하면 통과.
9. **피격 → 실패**: 부딪히면 0.1초 정지·흔들림·왼쪽 밀림 → "실패" + "다시하기". `R` 또는 버튼 → 같은 구간(또는 같은 보스) 처음부터, 본 컷신은 건너뜀.
10. **구간 4 → 엔딩**: 고향 소품 층이 켜지는지, 70% 지점부터 장애물이 없는지. 엔딩 컷신에서 도착 직후 연어가 한 번 점프하는지(컷신 애니메이션). "처음 품은 뜻을, 끝까지."
11. **타이틀 복귀**: "클리어 1회". 다시 시작하면 처음부터.
12. **일시정지**: Esc/버튼 → 물·애니메이션·보스·타자가 전부 멈추는지. 설정 열린 채 Esc는 설정만 닫는지.

## 디버그 플래그

| 위치 | 이름 | 효과 |
| --- | --- | --- |
| `GameConfig` JSON(개발자 설정 패널 또는 `persistentDataPath/config_override.json`) | `debugInvincible` | `HurtboxModule`이 판정하지 않는다. 기본 `false`. 에셋 기본값은 건드리지 않고 오버라이드로만 켠다 |
| `GameConfig` JSON | `spawnSeed` | 0이 아니면 코스 재현 |
| `GameConfig` JSON | `bossHitboxAlpha`, `controlHintDuration`, `playerScale`, `playerHitboxScale` | 즉시 반영(스케일·히트박스는 씬 생성 시 값이라 재생성 필요) |
| `WaterSettings.asset` | `collisionVelocityTransfer`, `surfaceCollisionDistance`, `velocitySmoothing` | 수면 반응 세기·범위·속도 평활 |
| 테스트 | `PlayFlow.ResetSession()` | 본 컷신·체크포인트 정적 초기화 |

## 검증 (배치)

| 단계 | 명령 | 결과 확인 |
| --- | --- | --- |
| 컴파일 | `Unity.exe -batchmode -nographics -projectPath <프로젝트> -quit -logFile Logs/batch_compile.log` | `grep "error CS"` 0건 |
| 생성 | `… -executeMethod Game.Bootstrap.Editor.CoreLoopSetup.Regenerate -quit -logFile Logs/batch_generate.log` | `[CoreLoopSetup] Core loop scenes are ready`, `Assets/Scenes/*.unity` 갱신 |
| EditMode 테스트 | `… -runTests -testPlatform EditMode -testResults Logs/editmode.xml -logFile Logs/batch_editmode.log` (`-quit` 없이) | XML `failed="0"` |
| PlayMode 스모크 | `… -runTests -testPlatform PlayMode -testResults Logs/playmode.xml -logFile Logs/batch_playmode.log` | `Game.Play.Tests.CoreLoopSmokeTests` 통과 |

`Assets/Scripts/Play/Tests/CoreLoopSmokeTests.cs`(asmdef `Game.Play.Tests`, `UNITY_INCLUDE_TESTS`)는 Bootstrap → Start → Play 로드 후 `PlayFlow.Current`가 `Running`에 도달하는지, 가상 키보드 `↑` 이벤트로 레인이 바뀌는지 확인한다. Unity Test Framework가 `Awaitable`을 지원하지 않아 **테스트 파일에 한해** `IEnumerator` 코루틴 반환을 허용한다(코드 규칙의 예외).

### 전체 루프 테스트 (`FullLoopTests`)

`Assets/Scripts/Play/Tests/FullLoopTests.cs`. 실행: `Unity.exe -batchmode -nographics -projectPath <프로젝트> -runTests -testPlatform PlayMode -testResults Logs/playmode.xml -logFile Logs/playmode.log`(`-quit` 없이). 하나만 돌리려면 `-testFilter Game.Play.Tests.FullLoopTests`.

| 테스트 | 하는 일 | 확인 |
| --- | --- | --- |
| `FullLoop_ClearsGame_AndTitleShowsClearCount` | PlayerPrefs 비움 → Bootstrap→Start → JSON `{"spawnSeed":12345,"debugInvincible":true}` 적용 → Play. `Time.timeScale = 8`을 매 프레임 유지. 컷신은 Space 연타(20초 안 끝나면 `Skip()`), 구간 1에서 ↑↑↑로 점프해 2초 동안 수면 노드 변위를 샘플, 보스 중에는 24프레임마다 ↑(폭포 돌파 점프) | 상태 순서 = 컷신·구간1·컷신·보스·구간2·컷신·보스·구간3·컷신·보스·구간4·컷신·Cleared, `SectionStarted` 1~4, 보스 3개 모두 타이머 뷰 표시, 조작 힌트 표시, 수면 변위 > 0.001, `ClearCount` 0→1, `EndingReached`, 결과 패널 표시, `GoTitle` 뒤 타이틀에 "클리어 1회", 체크포인트 −1 |
| `FailurePath_HitShowsResult_AndRetryRestartsFromCheckpoint` | 같은 시드, 무적 끔. 구간 1 진입 후 풀에서 `Rock`을 꺼내 플레이어 위치에 놓는다 | `Hit` → `Failed` 순서, 결과 패널 표시, 체크포인트 = 구간 1, `R`로 씬 재로드 후 새 `PlayFlow`가 컷신 없이 구간 1 `Running`, 거리 0부터 |

두 테스트 다 `[SetUp]`/`[TearDown]`에서 `PlayerPrefs.DeleteAll()`과 `PlayFlow.ResetSession()`(본 컷신·체크포인트 정적 초기화)을 한다. 8배속 기준 전체 루프는 실시간 1~2분이다.

씬을 생성한 뒤에는 Play 씬 YAML에서 `Game.*` 컴포넌트의 참조 필드가 `{fileID: 0}`이 아닌지 훑는다(생성기 회귀 확인). 이번 통합에서 확인한 목록은 아래 「통합 검증 결과」. 알려진 함정: 씬을 저장하면 Core의 `SceneReferencePostprocessor`가 `SceneReference` 에셋을 다시 임포트해 C#에서 들고 있던 에셋 참조가 무효(fake null)가 된다. 생성기는 그래서 에셋을 쓰기 직전에 경로로 다시 로드한다(`LoadRequired`, `RefreshContext`).

## 임시값 목록

- `scrollSpeed 4`, `jumpHeight 1.6`, `waterSurfaceY 2.0`, `hitPushDistance 0.4`: 기획서에 없음. 물 흐름·스크롤 속도는 3판 3절대로 구간과 무관하게 일정하다(구간별 물살 변화 없음).
- 컷신 대사는 `Design/Dialogue/dialogue.csv` 데이터이며 2판 문장을 임시로 쓴다. 3판 20절대로 최종본은 기획 확정 후 CSV에서 교체한다. 연출(좌표·시간)은 `Assets/Scripts/Cutscene/Scenes/`의 클래스 상수다.
- 보스 히트박스: 레인 띠 알파 0.3~0.4(공격 직전 0.45), 공격체 프레임 알파 `bossHitboxAlpha` 0.35. 둘 중 무엇이 기획의 "히트박스 표시"인지 질문 등록(`Docs/DesignQuestions.md`).
- 거리 표기 단위 `m`: 1 unit = 1 m로 표기.
- 충돌 연출 지속 0.25초, 흔들림 진폭 0.08. 플레이어 히트박스는 연어 알파 박스 × 0.87 = 월드 약 1.33×0.71(스포너 `playerHalfWidth` 0.35와 어긋남, 질문 등록).
- 카메라 배경·물 색, 레인 가이드 투명도, UI 색.
- 오디오: `Interface Click 1-1`, `Dashing`, `Positive 05`, `Negative 05`, `StartBGM`, `Tavern/BGM`, 점프 `Special 1-1`, 착수 `Diving`, 충돌 `Bite`, 보스 예고 `Hint 1-1`, 보스 공격 `Stab`, 보스 클리어 `Positive 01`, 컷신 전환 `Interface Click 13-1`, 엔딩 `Positive 16`.
- 폰트: `MonaS12.ttf`(픽셀 폰트, OFL). 배포 시 아트 폰트로 교체.
- 제목 "초지일관 : 귀향 (임시)".

## 알려진 제한

- TextMeshPro 필수 리소스가 임포트되어 있지 않아 레거시 `Text`를 쓴다.
- 첫 생성본에서 글씨가 전혀 안 보였던 원인: `Font.CreateDynamicFontFromOSFont`로 만든 Font를 `AssetDatabase.CreateAsset`으로 저장하면 `m_Texture`/`m_DefaultMaterial`이 비어 렌더링되지 않는다. TTF 임포트 에셋으로 바꿨다.
- `EventSystem`은 `InputSystemUIInputModule` 기본 액션을 쓴다(마우스 클릭만 확인).
- 충돌은 플레이어 `Rigidbody2D`(Kinematic, `useFullKinematicContacts`)의 trigger 콜백에 의존한다. 장애물은 `Collider2D`(trigger)만 있으면 되고 `Rigidbody2D`는 없어도 된다.
- 보스 예고·공격 효과음은 보스 모듈이 재생해야 한다(요청 등록). `BossSet.prefab`이 없으면 보스 단계는 즉시 통과한다.
- 체크포인트·본 컷신은 세션 메모리(정적)다. 앱을 다시 켜면 처음부터. `SettingsPanel`의 "타이틀로"는 체크포인트를 지우지 않는다(`PausePanel`·`ResultPanel`의 "타이틀로"는 지운다).
- 클리어 횟수만 저장하고 체크포인트·본 컷신은 저장하지 않는다.
- `GameConfig.asset`이 이미 있으면 새 필드(`sectionDurations`, `hit*`, `boss*`, `spawnSeed`)는 직렬화 기본값으로 들어간다. `sectionLengths`가 있던 에셋은 그 값이 버려진다.
- 컴파일·생성·EditMode·PlayMode(스모크 + 전체 루프 + 실패 경로)는 배치로 검증했다. 화면 모양(연어 크기·히트박스 프레임·수면 반응 세기·컷신 점프)은 사람이 Play로 본다(「수동 플레이 체크리스트」).

## 통합 검증 결과 (2026-09-05)

에디터를 닫고 배치로 돌렸다. 로그는 `Team1004/Logs/`.

| 단계 | 로그 | 결과 |
| --- | --- | --- |
| 컴파일 | `batch_compile.log` | `error CS` 0 |
| 생성 | `batch_regen.log` | `[CoreLoopSetup] Core loop scenes are ready`. 스포너·보스 프리팹 Overwrite 재생성, 애니메이션 에셋·환경 프리팹은 기존 유지 |
| EditMode | `editmode.log`, `editmode.xml` | 170/170 통과(`Game.Water.Tests` 7개 포함) |
| PlayMode | `playmode.log`, `playmode.xml` | 3/3 통과: 스모크, `FailurePath_…`(3초), `FullLoop_…`(28초, 8배속) |

씬·프리팹 참조 점검(`{fileID: 0}` 검색): `PlayFlow`, `LanePlayer`, `CutscenePlayer`, `CutsceneActor`×3, `ObstacleSpawner`, `PlayHud`, `GameBootstrap`(dialogueCsv·configAsset 포함), `SceneController`, `EnvironmentThing`(층 root 5개·surface·calmOverlay), `LaneTelegraph`, `HitboxView`(8개), `WaterInteractor`(2개), 보스 3종 전부 0 없음. 유일하게 `BossSet.prefab`의 `BossDirector.stageCamera`가 0이다(프리팹은 씬 카메라를 못 가리키며 런타임에 `Camera.main`으로 대체). 이번 통합 뒤 생성기가 씬 인스턴스에서 `stageCamera`를 Main Camera로 덮어쓰도록 고쳤으므로 다음 `Regenerate`부터 채워진다.

문서에 없어 통합에서 정한 것: 보스 중 배경은 흐른다(`Docs/Environment.md`), `Background` 사각형 삭제·`LaneGuides` 유지, `playerScale` 0.29, 점프 첨벙은 `Splash`가 아니라 `WaterInteractorModule`(세기 `WaterSettings.asset`), 히트박스 표시는 레인 띠 + 공격체 프레임 둘 다(질문 등록), 플레이어 애니메이터는 `InputEnabled`를 따라 멈춤.

