# 코어루프

3레인 상하 이동·점프로 장애물을 피하며 구간 4개를 지나 엔딩까지 가는 루프다. 환경(배경·수면)·플레이어 애니메이션·컷신 애니메이션·보스 레인 띠 판정·상류 단차(`Docs/Ledge.md`)·컷신 아이 배우가 통합되어 타이틀→엔딩→타이틀이 한 번에 돈다(「통합 지도」, 「수동 플레이 체크리스트」). 기획서 2판 `Team1004/Assets/Documents/정리 1번/2번 정리.md`(4, 15~18, 26, 28~30, 33, 36절)와 3판 `3번 정리.md`(3, 9, 12~13, 18~19, 24~25절)를 반영했다. 점프·스포너·보스·컷신은 이 문서의 「다른 에이전트용 훅」에 끼어든다. 기획서 정본은 `Team1004/Assets/Documents/정리 1번/초지일관 귀향 게임 프로토타입 제작 프롬프트.pdf`이며 기획 내용만 따르고 HTML 구현 지시와 우선순위는 무시했다.

## 생성기 사용법

씬·프리팹·에셋은 손으로 만들지 않고 에디터 메뉴가 만든다. Inspector에서 손으로 연결할 것은 없다.

1. 컴파일 오류가 없어야 한다. 배치 검증: `"C:/Program Files/Unity/Hub/Editor/6000.3.6f1/Editor/Unity.exe" -batchmode -nographics -projectPath "D:/Unity/Team1004/Team1004" -quit -logFile "D:/Unity/Team1004/Team1004/Logs/batch_compile.log"` 뒤 로그에서 `error CS`를 찾는다. `Team1004/Temp/UnityLockfile`이 있으면 에디터가 열려 있거나(배치 금지) 직전 배치가 남긴 잔재다(`Unity.exe` 프로세스가 없으면 지운다).
2. 생성 순서. 코어루프 생성기가 아래 둘을 자동 호출하므로 보통 3만 실행하면 된다.
   1. `Team1004 > Generate Spawner Assets` (`Game.Spawner.Editor.SpawnerAssetGenerator.GenerateMissing`)
   2. `Team1004 > Generate Cutscene Dialogue Prefab` (`Game.Cutscene.Editor.CutsceneSetup.Generate`)
   2-1. `Team1004 > Generate Boss Assets` (`Game.Boss.Editor.BossAssetSetup.Generate`). **Overwrite면 `Generate(true)`로 프리팹을 다시 만든다**(히트박스 프레임·띠 알파 갱신)
   2-2. `Team1004 > Generate Animation Assets` (`Game.Animation.Editor.AnimationAssetGenerator.GenerateMissing`) — `Art/물고기 애니메이팅/` 11장 + `Art/오브젝트/장애물 물고기/` 2장 임포트 설정(폴더별 공유 pivot) + 클립 5개(연어 4 + `Obstacle_Fish`). **Overwrite면 `Generate(true)`로 클립을 다시 쓴다**
   2-3. `Team1004 > Generate Environment Assets` (`Game.Environment.Editor.EnvironmentSetup.Generate`) — 없을 때만. **Overwrite면 `Generate(true)`로 프리팹을 다시 만든다**(텍스처·물 프로필 보존)
   2-4. `Team1004 > Generate Ledge Assets` (`Game.Ledge.Editor.LedgeSetup.Generate`) — 없을 때만. **Overwrite면 `Generate(true)`로 `LedgeSet.prefab`을 다시 만드는데, 생성기가 얼어 있어 단차 3개·작은 폭포가 사라진다**(`LedgeData.asset`은 보존). `Docs/Ledge.md` 「생성기」
   2-5. 컷신 대사 프리팹도 Overwrite면 `CutsceneSetup.Generate(true)`로 다시 만든다(힌트 문구 `Enter / 클릭 ▶`)
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
| (보스 생성기) `Assets/GameAssets/Boss/BossSet.prefab` 등, `Assets/GameAssets/Design/Boss/*.asset` | 없으면 코어루프 생성기가 먼저 호출. Overwrite면 프리팹 재생성(데이터 에셋은 보존). 보스마다 `Telegraph` + `LaneHazard`, `HitboxView`는 없다 |
| (애니메이션 생성기) `Assets/GameAssets/Design/Animations/Player_Swim/LaneUp/LaneDown/Jump.asset`, `Art/물고기 애니메이팅/**/*.png.meta` | 항상 호출(없는 것과 프레임 수가 어긋난 것만 만들고 임포트 설정은 매번 맞춘다. 옛 `Player_Idle.asset`은 지운다) |
| (환경 생성기) `Assets/GameAssets/Environment/Environment.prefab`, `Placeholder/Env_*.png` | 없으면 만든다. 코어루프 Overwrite 또는 `Regenerate Environment Prefab (Overwrite)`가 프리팹을 덮는다 |
| (단차 생성기) `Assets/GameAssets/Ledge/LedgeSet.prefab`, `Ledge_Rock/RockEdge/Hint.png`, `Assets/GameAssets/Design/Ledge/LedgeData.asset` | 없으면 만든다. **⚠ Overwrite면 프리팹을 다시 만드는데 `LedgeSetup`이 2026-09-06 변경(단차 3개 + 작은 폭포)을 아직 모른다. 돌리기 전에 `Docs/Ledge.md` 「생성기」의 경고를 읽는다** |
| (컷신 배우) Play 씬 `CutsceneChild` | 씬 생성 시 `CutsceneActorSetup.EnsureActors(cutscenePlayer)`가 id `child` 배우를 만들어 `CutscenePlayer.actors`에 덧붙인다(멱등) |

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
CutsceneBoss / CutsceneLandmark / CutsceneChild (숨김 스프라이트, CutsceneActor. child는 CutsceneActorSetup이 만든다)
CutsceneDialogue (프리팹 인스턴스, Canvas sortingOrder 100)
CutscenePlayer (MonoThing 호스트[CutscenePlaybackModule], actors 4(player/boss/landmark/child), clips = player_jump/player_swim/player_lane_up/player_lane_down)
BossSet (프리팹 인스턴스: BossDirector + FishingLineBoss/WalrusBoss/WaterfallBoss 비활성. 보스마다 Telegraph(레인 띠) + LaneHazard(레인마다 같은 크기의 판정 밴드), 바늘·바다코끼리 몸에 WaterInteractor)
LedgeSet (프리팹 인스턴스, ScrollRoot 밖: LedgeDirector[environment·player 연결] + Ledge1/Ledge2/Ledge3 비활성. 상류 단차 = 작은 폭포. 구간 1 5초·12.5초, 구간 2 15초. Docs/Ledge.md)
PlayFlow (DomainSingleton, environment·ledgeDirector 참조)
HudCanvas (Screen Space Camera, PlayHud)
  ProgressBar/Fill (fillAmount = Progress01)
  Distance "구간 {0} · 남은 {1:0}초"
  PauseButton "일시정지"
  ControlHint "↑↓ : 레인 이동 · 가장 위에서 ↑ : 점프" (구간 1 시작 시 controlHintDuration 초, 기본 비활성)
  Banner "BOSS 1 — 낚싯줄" / "CLEAR" (화면 중앙, 기본 비활성)
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
| | | `JumpModule : Module` | `Ticks = Update`. `TryJump`(상단 y에서 포물선, `JumpDuration`·`JumpHeight`), 착수 시 `CooldownRemaining = JumpCooldown`, `ResetCooldown()`, `CancelJump()`(공중 취소, `Landed` 없음). `IsAirborne`, `AirborneRemaining`, `CanJump`, `Cooldown01`, `Jumped`, `Landed` |
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
| | | `PlayHud`, `PausePanel`, `ResultPanel` | UI. 버튼 핸들러는 코드에서 `onClick.AddListener`로 연결. `PlayHud.ShowControlHint(초)`가 조작 힌트를 켜고 `Time.deltaTime` 누적으로 끈다. 남은 시간이 0이 된 프레임까지는 보이고 그 다음 `Update`에서 끈다(8배속·긴 프레임에서도 최소 한 프레임은 보인다. `IsControlHintVisible`). `ShowBannerAsync(문구, 초)`가 중앙 배너를 켜고 `Time.deltaTime` 누적으로 끈다(`IsBannerVisible`, `BannerText`) |
| | | `BossTimerView` | UI. 상단 중앙 30초 카운트다운 자리. `Show(초)`, `SetRemaining(초)`, `Hide()`. 값은 보스 모듈이 준다. 지금은 비활성 |
| | | `JumpCooldownView` | UI. 좌측 하단 원형 게이지(Radial360, 시계 방향)와 아이콘 밝기. `PlayFlow.Current.Player.Jump`를 매 프레임 읽는다 |
| `Game.Title` | `Assets/Scripts/Title/` | `TitleScreen` | 시작/설정/종료, 타이틀 BGM |
| `Game.Bootstrap` | `Assets/Scripts/Bootstrap/` | `GameBootstrap` | Manager 초기화 → `GameConfig.Load` → `DialogueService.Load(dialogueCsv)` → 설정·기록 로드 → 첫 씬 이동. `GameInput.inputactions`, `AudioManifest.json` 동거 |
| `Game.Bootstrap.Editor` | `Assets/Scripts/Bootstrap/Editor/` | `CoreLoopSetup` | 생성기 메뉴 |

의존 방향: `Config ← Player ← Play`, `Config ← Settings ← Play/Title`, `Play → Cutscene, Spawner, Environment, Ledge`, `Ledge → Config, Environment, Player`, `Player → Animation, Water`, `Cutscene → Animation`, `Environment → Water`, `Boss → Core.Audio`, `Bootstrap → Config, Settings`. 순환 없음. Core 타입은 전역 네임스페이스라 `using` 없이 쓴다.

입력 맵(`GameInput.inputactions`): `Player/LaneUp`(↑, W), `Player/LaneDown`(↓, S), `UI/Pause`(Esc), `UI/Retry`(R, 실패 화면에서만), `Cutscene/Advance`(Enter, 숫자패드 Enter, 마우스 왼쪽 클릭. v4 21절, Space는 쓰지 않는다). 기존 `Assets/InputSystem_Actions.inputactions`는 쓰지 않는다.

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
- `Section n`: `sectionTime = 0`, `sectionDuration = GetSectionDuration(n)`(25/30/35/15초), 스포너 `BeginSectionByDuration(n−1, sectionDuration)`(GameConfig의 구간 시간을 그대로 넘긴다. 스포너 기본값 `SpawnerDefaults.GetSectionDuration`과 같은 값이어야 하며 `GameConfig.asset`에 25/30/35/15로 넣어 두었다), `Running`. 종료 조건은 `SectionTime ≥ 시간`이고 스포너가 있으면 추가로 `IsSectionExhausted`(스폰 종료 + 활성 장애물 0). 구간 시작 유예 `sectionStartGrace`(2초) 동안은 장애물이 없고 HUD의 남은 초는 그대로 줄어든다. 구간 시작 시 `LedgeHandler.BeginSection(n, 시간)`, 구간 종료·실패·디버그 점프에 `Stop()`.
- **상류 단차(`Docs/Ledge.md`)**: 구간 1에 **두 개**(5초·12.5초), 구간 2에 하나(15초). `Running`인 프레임마다 `PlayFlow.Update`가 먼저 `LedgeHandler.Tick(dt)`를 돌리고, `IsHoldingWorld`(착지 상태로 단차에 막힘)면 그 프레임은 스크롤·환경 흐름·`Distance`·`sectionTime`·스포너 `Advance`를 전부 멈춘다(HUD 남은 초도 멈춘다). `IsSpawnSuspended`(접근 2초 ~ 상승 완료)면 `spawner.SetSpawningEnabled(false)`로 새 스폰만 멈추고 이미 떠 있는 장애물은 계속 흐른다. 일시정지는 `Running`이 아니라 `Tick`이 오지 않는다. `instantFail`이면 `Failed` 이벤트 → `Fail()`.
- `Boss n`: `Boss` 상태(입력 허용, 스크롤 정지). `BossSet.prefab`의 `BossDirector`가 `Start`에서 `PlayFlow.Current.BossHandler = RunBossAsync`를 걸어 두므로 `PlayFlow`는 `await BossHandler(n)`만 한다(`Docs/Boss.md` 통합 절차). `true`면 `boss_clear` 효과음 후 다음 구간, `false`면 `Fail()`(체크포인트는 보스 직전이라 재도전 시 보스부터). 핸들러가 없으면 즉시 통과. 보스 타이머는 `BossDirector`가 `PlayFlow.BossTimer`로 갱신한다.
- **보스 배너(v4 17절)**: `Boss` 상태에 들어간 직후 `BossHandler`를 부르기 **전에** HUD 중앙에 `BOSS n — 이름`(이름은 `PlayFlow.bossNames`)을 `bossBannerDuration`(1초) 보여 준다. 보스가 살아남으면 `boss_clear` 뒤 `CLEAR`를 `bossClearBannerDuration`(1초) 보여 주고 다음 구간으로 간다. **배너 동안 `Time.timeScale`은 건드리지 않고** 플레이어 입력만 잠근다(`PlayFlow.bannerHold` → `ApplyInputEnabled`). 배너 시간은 `Time.deltaTime` 누적이라 일시정지하면 함께 멈춘다. 보스 타이머는 배너가 끝난 뒤 `BossDirector`가 켠다.
- `Ending`: `Ending.asset` 재생 → `Clear()` → `ending` 효과음, 기록 갱신(엔딩 도달), 결과 패널.
- 상태가 `Running`/`Boss`가 아니거나 **배너가 떠 있으면** `LanePlayer.InputEnabled = false`(플레이어 애니메이터도 멈춤). 한 곳(`ApplyInputEnabled`)에서만 정한다. `Running`이 아니면 `StageScroller.SetScrolling(false)`. 환경(`EnvironmentThing`)은 `Running`·`Boss`에서만 흐른다(보스 중 물은 흐르고 장애물 스크롤만 멈춘다. 결정: `Docs/Environment.md`). `Cutscene`이면 HUD Canvas를 끈다.
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
| 체크포인트 | 정적 `PlayFlow.CheckpointStep`(단계 인덱스) + 정적 `PlayFlow.CheckpointTag`(단계 **안의** 지점, 지금은 마지막 폭포뿐). 아래 「체크포인트 표」 |
| 본 컷신 | 정적 `PlayFlow.SeenCutscenes`(세션 메모리). 재도전 시 스킵 |

### 체크포인트 표

| 무엇 | 어디에 기록 | 언제 기록 | 재도전(`Retry`)하면 |
| --- | --- | --- | --- |
| 진행 n | `CheckpointStep` = 그 `Section` 단계 인덱스. 동시에 `CheckpointTag`를 비운다 | `RunFlowAsync`가 `Section` 단계에 들어갈 때 | 그 구간을 처음부터. 건너뛴 구간 길이만큼 `Distance`를 미리 더하고 건너뛴 컷신은 `ApplyFinalState`로 최종 상태만 적용한다 |
| 보스 n | `CheckpointStep` = 그 `Boss` 단계 인덱스 | `RunFlowAsync`가 `Boss` 단계에 들어갈 때. **`CheckpointTag`는 건드리지 않는다** | 그 보스를 처음부터(등장 연출 + 30초 타이머 전부). 컷신은 건너뛴다 |
| **마지막 폭포** | `CheckpointStep` = 보스 3 단계, `CheckpointTag` = `PlayFlow.FinalWaterfallCheckpoint`("FinalWaterfall") | `BossDirector`가 매 프레임 `boss.ActiveCheckpoint`를 읽어 `PlayFlow.SetBossCheckpoint`를 부른다. `WaterfallBoss`는 `Breakthrough` 상태일 때만 이 값을 준다 | **마지막 폭포 접근부터.** `BossDirector`가 `Initialize` 전에 `boss.EntryCheckpoint = PlayFlow.CheckpointTag`를 넣고, `WaterfallBoss.InitialKey`가 `Breakthrough`가 된다. 30초 재생 없음, 컷신 없음, 보스 타이머는 만료(0) 상태로 표시된다 |

`CheckpointTag`를 비우는 곳: `Section` 단계 진입, 보스를 살아남았을 때(`CLEAR` 배너 직전), `PrepareDebugJump`(디버그 단축키), `ResetCheckpoint`/`ResetSession`/`GoTitle`, 도메인 리로드. 보스에서 실패해도 태그는 남아 있어야 다음 재도전이 마지막 폭포에서 시작한다.

결과 패널과 `R` 키는 그대로다. 마지막 폭포에서 죽어도 화면은 일반 실패와 같다("부딪혔다…" + 다시하기).

## 충돌 연출 (2판 16절)

`HitReactionModule`(플레이어 호스트)이 한다. 순서: `HitStopDuration`(0.1초) 동안 플레이어·스크롤·스포너 정지(상태 `Hit`, `timeScale`은 건드리지 않음) → 0.25초 동안 y 흔들림(진폭 0.08, 60Hz 감쇠) + 스프라이트 흰색 깜빡임(30Hz) + x를 `HitPushDistance`(0.4 unit)만큼 왼쪽으로 밀림 → `Fail()`. 체력·목숨·무적 없음. DOTween을 쓰지 않고 모듈 틱으로 계산한다(Game.Player가 DOTween을 참조하지 않게).

## HUD (2판 30절)

| 위치 | 내용 | 스크립트 |
| --- | --- | --- |
| 좌측 상단 | `구간 {Section} · 남은 {SectionRemaining:0}초` | `PlayHud` |
| 상단 중앙 | 진행 바(구간 시간 진행도 `Progress01`, 3판 19.3절 선택 UI) 아래에 보스 타이머: 보스 이름 한 줄("낚싯줄"·"바다코끼리"·"폭포", `PlayFlow.bossNames`) + 큰 정수 초(30→29…). HP바 없음. `BossDirector`가 `SetBoss(BossThing.DisplayName)`, `SetVisible`, `SetRemaining`으로 갱신한다(`PlayFlow.bossNames`는 핸들러가 없을 때의 대체 이름) | `BossTimerView` |
| 좌측 하단 | 원형 점프 쿨타임 UI | `JumpCooldownView` |
| 우측 상단 | 일시정지 버튼 | `PlayHud` |
| 화면 중앙 | 보스 시작·클리어 배너(`BOSS 1 — 낚싯줄`, `CLEAR`). 기본 비활성. `ShowBanner`/`HideBanner`/`ShowBannerAsync(text, 초)`. 읽기용 `IsBannerVisible`, `BannerText` | `PlayHud` |

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
| 스폰 중단 | `SetSpawningEnabled(bool)`(`SpawnModule.SpawningEnabled`, 구간 시작 시 true). `Advance`가 이동·회수는 그대로 하고 스폰 판단만 건너뛴다. 단차 접근 창에서 `PlayFlow`가 매 프레임 넘긴다 |
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
| 보스 | 연결됨. `BossSet.prefab`(`BossDirector` + 보스 3개)을 생성기가 Play 씬 루트에 둔다. `BossDirector`가 `BossHandler`를 채우고 `PlayFlow.BossTimer`를 갱신한다. 예고·공격 효과음(`boss_telegraph`/`boss_attack`)과 히트박스 표시 겸 판정(`LaneTelegraph` + `LaneHazard`)은 보스 모듈이 한다. `PlayFlow.IsBossInterrupted`가 true면 `BossDirector`가 보스를 `Abort()`한다(디버그 점프) |
| 환경 | 연결됨. `Environment.prefab`을 루트에 두고 `PlayFlow.environment`로 스크롤·고향 전환. 단차가 `SetVerticalOffset`으로 배경을 잠깐 내렸다 되돌린다 |
| 상류 단차 | 연결됨. `LedgeSet.prefab`(`LedgeDirector` + 단차 3개)을 생성기가 Play 씬 루트에 두고 `PlayFlow.ledgeDirector`·`LedgeHandler`(`ILedgeHandler`)로 붙는다. `Docs/Ledge.md` 「통합 절차」 전부 반영 |
| 애니메이션 | 연결됨. `LanePlayer`(수영 루프 / 레인 위·아래 이동 / 점프)와 `CutsceneBase.Animate/SetPose`(`player_jump`, `player_swim`, `player_lane_up`, `player_lane_down`) |
| 점프 연출 | `LanePlayer.Jumped`/`Landed`, `JumpModule.Progress01`, `GameConfig.WaterSurfaceY`. 수면 반응은 `WaterInteractorModule`이 이미 한다 |
| 보스 3 마지막 폭포 | `LanePlayer.ResetJumpCooldown()`(→ `JumpModule.ResetCooldown()`). `WaterfallBreakthroughState.OnEnter`가 호출한다. 30초 뒤 마지막 폭포가 3초에 걸쳐 다가오고 닿으면 실패, 넘으면 통과다(`Docs/Boss.md` 「마지막 폭포 = 닿으면 실패」). 실패는 `BossThing.Impact` → `BossDirector` → `PlayFlow.ReportImpact()`로 일반 피격과 같은 연출을 탄다. 체크포인트는 `PlayFlow.CheckpointTag` |
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
| `jumpDuration` | `1.6` | **v4 04절**(기존 0.8의 2배) |
| `jumpCooldown` | `1.2` | v4 04절 |
| `jumpHeight` | `2.2` | **v4 04절** "약 2레인 높이". 레인 간격 1.1 × 2. 궤적 `4·h·t·(1−t)`의 최고점이 정확히 `h`라 상단 레인 1.1 → 최고 3.3(수면 2.0 위로 1.3) |
| `waterSurfaceY` | `2.0` | **임시값**. 연출 없이 값만 |
| `hitStopDuration` | `0.1` | 2판 16절 |
| `hitPushDistance` | `0.4` | **임시값**. 2판 16절 "살짝 밀림" |
| `bossDuration` / `bossTelegraphDuration` / `bossFullLaneTelegraphDuration` / `bossAttackDuration` / `bossRecoveryDuration` | `30 / 1.2 / 1.5 / 0.6 / 0.6` | **v4 10절**. 예고만 50% 느려졌다(공격체 이동속도가 아니라 반응 시간). 보스 모듈이 읽는다 |
| `bossLaneBandWidth` / `bossLaneBandHeight` | `12.8 / 0.9` | v4 10·15절. 위험 레인 띠 하나의 크기이며 **실제 공격 판정 박스와 같은 값**이다(`Docs/Boss.md` 「레인 띠 = 판정」). 12.8 = 카메라 ortho 3.6 · 16:9 화면 폭 |
| `bossBannerDuration` / `bossClearBannerDuration` | `1.0 / 1.0` | v4 17절. 보스 시작 배너("BOSS n — 이름")와 클리어 배너("CLEAR") 표시 시간 |
| `spawnSeed` | `0` | 0이면 실행마다 다른 시드. 고정하면 코스 재현 |
| `playerScale` | `0.29` | 연어 아트(527×280px, PPU 100) 스케일. 몸 높이 약 0.81 unit. **임시값** |
| `playerHitboxScale` | `0.87` | 히트박스 = 알파 박스 × 이 값(4번 17절 G: 스프라이트보다 10~15% 작게) |
| `controlHintDuration` | `2.5` | 구간 1 시작 시 조작 힌트 표시 초(4번 11절 2~3초) |
| `debugEnabled` | `false` | **디버그.** true여야 숫자키 1~8 구간 점프가 동작한다(v4 23절 `DEBUG` 플래그). 꺼진 상태에서 누르면 경고 로그만 남는다 |
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
| `PlayFlow` | `DomainSingleton` | `player`, `scroller`, `spawner`, `hud`, `bossTimer`, `pausePanel`, `resultPanel`, `cutscene`, `environment`, `ledgeDirector`, `playScene`, `startScene`, 컷신 id 5개 | 상태 전이마다 입력·스크롤·환경·HUD를 켜고 끈다. 구간 시작 → 스포너·단차·고향·힌트, 보스 → `BossHandler`. `Running` 중 `LedgeHandler.Tick` → 월드 정지/스폰 중단 |
| `LedgeSet` | `LedgeDirector` + `Ledge1`/`Ledge2`/`Ledge3`(`LedgeThing`[LedgeScroll, LedgeGate, LedgeHint]) | `data`(Design/Ledge/LedgeData, `ledgeEntries` 3개), `ledges[3]`, `environment`, `player`(생성기가 씬 인스턴스에 연결) | `PlayFlow.LedgeHandler`. `BeginSection`/`Tick`/`Stop`, 이벤트 `Approaching`/`Blocked`/`Resumed`/`Cleared`/`Finished`/`Failed`. 한 구간에 여러 단차가 있으면 도착 시각 순으로 하나씩 |
| `Environment` | `EnvironmentThing`[Parallax×3, WaterFlow, Homeland, WaterSplash] | 프리팹(`Assets/GameAssets/Environment/Environment.prefab`) | `SetScrolling(Running\|Boss)`, `SetHomeland(구간 4)` |
| `Player` | `LanePlayer`[LaneMove, Jump, Hurtbox, HitReaction, SpriteAnimator, WaterInteractor] | 클립 4개(swim/laneUp/laneDown/jump), `BoxCollider2D`(히트박스) | `InputEnabled`가 이동·점프·애니메이터를 함께 멈춤. `Hit` → `PlayFlow.OnHit`. 레인 이동 시작(`LaneMoveModule.MoveStarted`)에 방향 클립 0.2초, 끝(`MoveFinished`)에 수영 루프. 점프 중이면 점프 클립이 이긴다 |
| `ObstacleSpawner` | `ObstacleSpawner`[SpawnModule] | `settings`, `scrollRoot` | `Initialize`(첫 구간) / `BeginSectionByDuration` / `Advance`(Running) / `Stop`(Hit·Failed) |
| `ScrollRoot` | `StageScroller`[ScrollModule] | — | `SetScrolling(Running)` |
| `BossSet` | `BossDirector` + `FishingLineBoss`/`WalrusBoss`/`WaterfallBoss`[StateMachineModule], 자식 `Telegraph`[LaneTelegraphModule], `LaneHazard/Band0..2`(`Hazard` + trigger), 공격체 스프라이트(+ `WaterInteractor`) | `bosses[3]`, 각 보스 `data`(Design/Boss) | `BossDirector.Start`가 `PlayFlow.BossHandler`를 채움. 예고 → 띠+`boss_telegraph`, 공격 → 밴드 활성화+`boss_attack` |
| `CutscenePlayer` | `CutscenePlayer`[CutscenePlaybackModule] | `stageCamera`, `dialogueView`, `actors[4]`(player/boss/landmark/child), `clips[4]` | `PlayFlow.PlayCutsceneAsync` → `Cutscene` 상태. Enter/클릭 = `Cutscene/Advance` |
| `CutsceneDialogue` | `CutsceneDialogueView`(MonoBehaviour, Canvas 100) | 프리팹 | 대사·화면 페이드 |
| `HudCanvas` | `PlayHud`, `JumpCooldownView`, `BossTimerView`, `PausePanel`, `ResultPanel`, `SettingsPanel`(→`ConfigPanel`) | `Banner`(중앙 Text, 기본 비활성) | `Cutscene`이면 Canvas 비활성. 힌트는 `ShowControlHint`, 보스 배너는 `ShowBannerAsync` |
| `Main Camera` | `GameCamera`[AspectModule] | — | 16:9 레터박스. 컷신이 위치·orthoSize를 빌렸다 돌려준다 |

Bootstrap 씬: `GameBootstrap`(`inputActions`, `audioManifest`, `configAsset`, `dialogueCsv`, `firstScene`) + Manager 5개.

## 수동 플레이 체크리스트

배치 테스트가 보지 못하는 것(모양·타이밍·체감)을 사람이 본다. `Core > Play From Bootstrap`이 켜진 상태에서 Play.

1. **타이틀**: 제목, "아직 클리어 없음", 힌트 문구. 설정 → 볼륨 슬라이더 3개, 개발자 설정 → JSON 패널에 `playerScale`, `debugEnabled`, `debugInvincible`, `bossLaneBandWidth`, `bossBannerDuration`, `controlHintDuration`이 보이는지.
2. **시작 → 인트로 컷신**: 암전 → 자갈밭 → 성체 등장 → 인간 아이(`CutsceneChild`, 임시 살구색 사각형). Enter 또는 클릭으로 넘김(Space는 반응 없음). 대사 힌트가 `Enter / 클릭 ▶`인지, 대사 한글이 KOTRA HOPE 폰트로 보이는지.
3. **구간 1 시작**: 화면 아래쪽에 조작 힌트가 약 2.5초 나왔다 사라지는지. 배경 층(하늘·원경·중경·바닥·물결·수면)이 왼쪽으로 흐르고 레인 가이드 선이 읽히는지. 연어가 가운데 레인에 레인 간격 안에 들어오는 크기인지.
4. **이동·점프**: ↑↓로 레인 이동, 상단에서 ↑로 점프. **레인 이동 중 올라가기/내려가기 2프레임이 0.2초에 넘어가고 도착하면 수영 루프로 돌아오는지**, 클립이 바뀔 때 연어가 위아래로 튀지 않는지(공유 pivot). 점프 중 5프레임 애니메이션이 **1.6초**에 걸쳐 넘어가고 착수 후 수영 포즈로 돌아오는지. 점프 최고점이 **상단 레인에서 2레인(2.2 unit) 위**로 수면을 확실히 뚫는지. **수면이 점프 때 솟고 착수 때 눌리는지**(세기는 `WaterSettings.asset`). 좌측 하단 쿨타임 원이 차오르는지.
5. **장애물**: 돌·물고기·통나무가 레인에 맞게 오고, 히트박스가 연어 몸보다 살짝 작아 보이는지(스치는 느낌이 과하면 `playerHitboxScale`). 돌 변종 2개가 번갈아 나오는지(지금은 그림이 같아 구분이 안 된다), 장애물 물고기 2프레임이 튀지 않는지.
5-1. **상류 단차(구간 1 5초·12.5초, 구간 2 15초 — 전부 「작은 폭포」)**: 구간 1은 시작 3초 만에 장애물이 끊기고 오른쪽에서 폭포가 들어와 5초에 닿는지. **첫 단차에만** 배경 `↑`가 보이는지. 1번 레인에서 점프하면 넘어가고 0.7초 동안 위쪽 강바닥이 내려와 평소 강바닥과 이어지는지(화면이 튀지 않는지). **착지 상태로 닿으면 죽지 않고 세계·HUD 남은 초가 멈추고**, 다시 점프하면 이어지는지. **첫 폭포가 완전히 빠져나간 뒤에 두 번째(12.5초)가 들어오는지**(둘이 겹쳐 보이면 안 된다). 막힌 동안 Esc → 전부 멈추는지. 구간 3·4·보스·컷신에는 단차가 없는지. 세부는 `Docs/Ledge.md` 「수동 플레이 체크」.
6. **컷신 → 보스 1(낚싯줄)**: 컷신 뒤 화면 중앙에 "BOSS 1 — 낚싯줄"이 1초 → 사라진 뒤 상단 중앙에 "낚싯줄 30". 예고 1.2초 동안 레인 띠(30~40%)가 그 레인 **전체 폭**을 덮고, 바늘이 오른쪽에 나타났다가 예고 중반부터 왼쪽으로 훑어 온다. 공격 0.2초 전 띠가 밝아짐 → 공격음과 함께 바늘이 플레이어 x를 지난다(줄이 뒤로 기울고 바늘이 위아래로 살짝 흔들리는지). 바늘이 수면을 뚫을 때 물이 반응하는지. 띠 밖에서는 절대 맞지 않는지. 30초 뒤 "CLEAR" 1초.
7. **보스 2(바다코끼리)**: 1레인 입/2레인 팔 크기 구분, 돌진 후 복귀. **돌진할 때 레인 띠가 따라 움직이지 않는지**(루트가 아니라 `Body`만 움직여야 한다). 2레인 공격에서 남은 1레인이 진짜 안전한지.
8. **보스 3(폭포)**: 급류·돌은 연출뿐이고 판정은 띠인지. 3레인 전체 공격 예고가 1.5초로 더 긴지. **30초 뒤 거대한 폭포가 오른쪽에서 3초에 걸쳐 다가오는지. 가만히 있으면(또는 너무 이르거나 늦게 뛰면) 부딪혀 실패하는지, 알맞게 뛰면 넘어가는지.** 폭포 앞면이 연어에 닿는 순간과 판정 순간이 눈으로 맞는지.
8-2. **마지막 폭포 재도전**: 마지막 폭포에서 실패 → 다시하기 → **30초 공격 없이 곧바로 폭포가 다가오는지**, 보스 타이머가 0으로 떠 있는지, 컷신 3이 다시 나오지 않는지. 반대로 30초 공격 중에 맞아 실패하면 보스가 **처음부터**(등장 + 30초) 다시 도는지.
8-1. **디버그 단축키**: 개발자 설정에서 `{"debugEnabled": true}` 적용 후 1~8로 각 구간·보스·엔딩으로 건너뛰어지는지, 건너뛴 뒤 장애물이 남아 있지 않은지.
9. **피격 → 실패**: 부딪히면 0.1초 정지·흔들림·왼쪽 밀림 → "실패" + "다시하기". `R` 또는 버튼 → 같은 구간(또는 같은 보스) 처음부터, 본 컷신은 건너뜀.
10. **구간 4 → 엔딩**: 고향 소품 층이 켜지는지, 70% 지점부터 장애물이 없는지. 엔딩 컷신에서 도착 직후 연어가 한 번 점프하는지(컷신 애니메이션). "처음 품은 뜻을, 끝까지."
11. **타이틀 복귀**: "클리어 1회". 다시 시작하면 처음부터.
12. **일시정지**: Esc/버튼 → 물·애니메이션·보스·타자가 전부 멈추는지. 설정 열린 채 Esc는 설정만 닫는지.

## 디버그 플래그

| 위치 | 이름 | 효과 |
| --- | --- | --- |
| `GameConfig` JSON(개발자 설정 패널 또는 `persistentDataPath/config_override.json`) | `debugEnabled` | 숫자키 1~8 구간 점프를 켠다. 기본 `false`. 아래 「디버그 단축키」 |
| `GameConfig` JSON(개발자 설정 패널 또는 `persistentDataPath/config_override.json`) | `debugInvincible` | `HurtboxModule`이 판정하지 않는다. 기본 `false`. 에셋 기본값은 건드리지 않고 오버라이드로만 켠다 |
| `GameConfig` JSON | `spawnSeed` | 0이 아니면 코스 재현 |
| `GameConfig` JSON | `controlHintDuration`, `playerScale`, `playerHitboxScale` | 즉시 반영(스케일·히트박스는 씬 생성 시 값이라 재생성 필요) |
| `LedgeData.asset` | `ledgeEntries`(구간 + 도착 초 쌍), `approachSafeTime`, `instantFail`, `environmentRiseAmount` | 단차 일정·접근 창·즉시 실패·배경 흔들림. `Docs/Ledge.md` |
| `WaterSettings.asset` | `collisionVelocityTransfer`, `surfaceCollisionDistance`, `velocitySmoothing` | 수면 반응 세기·범위·속도 평활 |
| 테스트 | `PlayFlow.ResetSession()` | 본 컷신·체크포인트 정적 초기화 |

## 디버그 단축키 (v4 23절)

`debugEnabled`가 true일 때 숫자키 1~8이 흐름의 해당 단계로 바로 넘어간다. 꺼져 있으면 아무 일도 없고 `[PlayFlow] Debug jump n was ignored because debugEnabled is false.` 경고만 남는다.

| 키 | 단계 | 키 | 단계 |
| --- | --- | --- | --- |
| `1` | 진행 1 | `5` | 진행 3 |
| `2` | 보스 1 | `6` | 보스 3 |
| `3` | 진행 2 | `7` | 진행 4 |
| `4` | 보스 2 | `8` | 엔딩 |

- 입력은 `GameInput.inputactions`를 거치지 않고 `PlayFlow.Update`가 `UnityEngine.InputSystem.Keyboard.current`를 직접 읽는다(디버그 전용 키를 액션 에셋에 넣지 않기 위해서. `Game.Play` → `Unity.InputSystem` 참조 추가).
- 대상 단계는 `steps` 목록에서 종류·번호로 찾는다(구간 수가 바뀌어도 매핑이 따라간다).
- 점프 시 `PlayFlow.PrepareDebugJump()`가 한 번에 정리한다: `Time.timeScale = 1`, 일시정지·결과·보스 타이머·배너 숨김, 스포너 `Stop()` + `ReleaseAll()`(풀 장애물 전부 회수), 컷신 전부 "본 것"으로 표시 + 재생 중이면 `Skip()`, 플레이어 `ResetHit`·`ResetJumpCooldown`·`SnapToLane`, 구간 시각 0, 상태 `Ready`.
- 보스는 `PlayFlow.IsBossInterrupted`로 끊는다. `BossDirector`의 대기 루프가 이 플래그를 보고 `boss.Abort()`한다. 중단으로 돌아온 `false`는 `Fail()`로 이어지지 않는다(점프 요청이 걸려 있으면 건너뛴다).
- 흐름 루프가 이미 끝난 뒤(클리어·실패 화면)에 눌러도 새 루프를 그 단계부터 시작한다(`flowRunning` 가드).
- `Distance`·`Section`은 `ApplySkippedSteps(대상)`가 건너뛴 구간 길이로 채운다.
- 점프 중(공중)에 눌러도 `PrepareDebugJump`가 `LanePlayer.CancelJump()`(→ `JumpModule.CancelJump()`, `Landed` 없이 공중 상태 해제)를 먼저 부르므로 `SnapToLane`이 그대로 먹는다. 단차도 `LedgeHandler.Stop()`으로 화면 밖으로 돌아가고 배경 오프셋이 0이 된다.

## 검증 (배치)

| 단계 | 명령 | 결과 확인 |
| --- | --- | --- |
| 컴파일 | `Unity.exe -batchmode -nographics -projectPath <프로젝트> -quit -logFile Logs/batch_compile.log` | `grep "error CS"` 0건 |
| 생성 | `… -executeMethod Game.Bootstrap.Editor.CoreLoopSetup.Regenerate -quit -logFile Logs/batch_generate.log` | `[CoreLoopSetup] Core loop scenes are ready`, `Assets/Scenes/*.unity` 갱신 |
| EditMode 테스트 | `… -runTests -testPlatform EditMode -testResults Logs/editmode.xml -logFile Logs/batch_editmode.log` (`-quit` 없이) | XML `failed="0"` |
| PlayMode | `… -runTests -testPlatform PlayMode -testResults Logs/playmode.xml -logFile Logs/batch_playmode.log` | `Game.Play.Tests` 6개(스모크 1, `DebugJumpTests` 3, `FullLoopTests` 2) 전부 통과. 실시간 약 1분 |

`Assets/Scripts/Play/Tests/CoreLoopSmokeTests.cs`(asmdef `Game.Play.Tests`, `UNITY_INCLUDE_TESTS`)는 Bootstrap → Start → Play 로드 후 `PlayFlow.Current`가 `Running`에 도달하는지, 가상 키보드 `↑` 이벤트로 레인이 바뀌는지 확인한다. Unity Test Framework가 `Awaitable`을 지원하지 않아 **테스트 파일에 한해** `IEnumerator` 코루틴 반환을 허용한다(코드 규칙의 예외).

### 전체 루프 테스트 (`FullLoopTests`)

`Assets/Scripts/Play/Tests/FullLoopTests.cs`. 실행: `Unity.exe -batchmode -nographics -projectPath <프로젝트> -runTests -testPlatform PlayMode -testResults Logs/playmode.xml -logFile Logs/playmode.log`(`-quit` 없이). 하나만 돌리려면 `-testFilter Game.Play.Tests.FullLoopTests`.

| 테스트 | 하는 일 | 확인 |
| --- | --- | --- |
| `FullLoop_ClearsGame_AndTitleShowsClearCount` | PlayerPrefs 비움 → Bootstrap→Start → JSON `{"spawnSeed":12345,"debugInvincible":true}` 적용 → Play. `Time.timeScale = 8`을 매 프레임 유지. 컷신은 Enter 연타(20초 안 끝나면 `Skip()`), 구간 1에서 ↑↑↑로 점프해 2초 동안 수면 노드 변위를 샘플, **단차에 막히면(`LedgeHandler.IsHoldingWorld`) 8프레임마다 ↑**로 다시 점프, 보스 중에는 24프레임마다 ↑(폭포 돌파 점프) | 상태 순서 = 컷신·구간1·컷신·보스·구간2·컷신·보스·구간3·컷신·보스·구간4·컷신·Cleared, `SectionStarted` 1~4, 보스 3개 모두 타이머 뷰 표시, 조작 힌트 표시, 수면 변위 > 0.001, **`LedgeData.HasLedge`인 구간(1·2) 전부 `Cleared`·`Finished`**, 루프 뒤 단차 비활성·스폰 재개, `ClearCount` 0→1, `EndingReached`, 결과 패널 표시, `GoTitle` 뒤 타이틀에 "클리어 1회", 체크포인트 −1 |
| `FailurePath_HitShowsResult_AndRetryRestartsFromCheckpoint` | 같은 시드, 무적 끔. 구간 1 진입 후 풀에서 `Rock`을 꺼내 플레이어 위치에 놓는다 | `Hit` → `Failed` 순서, 결과 패널 표시, 체크포인트 = 구간 1, `R`로 씬 재로드 후 새 `PlayFlow`가 컷신 없이 구간 1 `Running`, 거리 0부터 |

두 테스트 다 `[SetUp]`/`[TearDown]`에서 `PlayerPrefs.DeleteAll()`과 `PlayFlow.ResetSession()`(본 컷신·체크포인트 정적 초기화)을 한다. 8배속 기준 전체 루프는 실시간 1~2분이다.

### 디버그 점프·보스 띠 테스트 (`DebugJumpTests`, v4)

`Assets/Scripts/Play/Tests/DebugJumpTests.cs`. `Game.Play.Tests`가 `Game.Boss`·`Game.Spawner`·`Game.Ledge`를 참조한다. 통합 패스(2026-09-06)에서 3개 모두 통과했다(「통합 검증 결과 (2026-09-06)」).

| 테스트 | 하는 일 | 확인 |
| --- | --- | --- |
| `DebugKeyTwo_JumpsToFirstBoss_WithTimerAndNoObstacles` | `{"debugEnabled":true,"debugInvincible":true}` 적용 → 구간 1에서 장애물이 생길 때까지 4배속 대기 → 숫자키 `2` | 상태 `Boss`, `Section == 1`, 중앙 배너에 `BOSS 1`, 스포너 `ActiveObstacleCount == 0`, 배너가 사라진 뒤 보스 타이머 표시, 플레이어 `HasHit == false` |
| `DebugKey_WithDebugDisabled_IsIgnored` | `debugEnabled:false`로 숫자키 `2` | 경고 로그 1건, 상태가 `Boss`로 가지 않음 |
| `WalrusTwoLaneBand_MissesUncoveredLane_AndHitsCoveredLane` | 숫자키 `4`로 보스 2 → 2레인 패턴 예고마다 플레이어를 옮긴다(먼저 안 덮인 레인, 그 다음 덮인 레인) | 무장 순간 `LaneHazard.IsLaneArmed(lane)`이 예고 마스크와 일치, 3번째 레인에서는 안 맞고 덮인 레인에서는 맞는다 |

`Assets/Scripts/Config/Tests/`(새 asmdef `Game.Config.Tests`, EditMode): `GameConfigValuesTests`가 v4 기본값(점프 1.6/2.2, 보스 예고 1.2/1.5, 배너 1.0/1.0, 띠 12.8×0.9, `debugEnabled` false), `Validate` 통과, JSON 왕복(`debugEnabled`·배너·띠), 배너 음수·띠 0·점프 음수 거부를 본다.

씬을 생성한 뒤에는 Play 씬 YAML에서 `Game.*` 컴포넌트의 참조 필드가 `{fileID: 0}`이 아닌지 훑는다(생성기 회귀 확인). 이번 통합에서 확인한 목록은 아래 「통합 검증 결과」. 알려진 함정: 씬을 저장하면 Core의 `SceneReferencePostprocessor`가 `SceneReference` 에셋을 다시 임포트해 C#에서 들고 있던 에셋 참조가 무효(fake null)가 된다. 생성기는 그래서 에셋을 쓰기 직전에 경로로 다시 로드한다(`LoadRequired`, `RefreshContext`).

## 임시값 목록

- `scrollSpeed 4`, `waterSurfaceY 2.0`, `hitPushDistance 0.4`: 기획서에 없음. (`jumpHeight`는 v4 04절 "약 2레인"으로 2.2 확정.) 물 흐름·스크롤 속도는 3판 3절대로 구간과 무관하게 일정하다(구간별 물살 변화 없음).
- 컷신 대사는 `Design/Dialogue/dialogue.csv` 데이터다. 2026-09-06 기획 답변으로 인트로 7줄·엔딩 5줄(각 15~20초)로 줄였다(`Docs/Cutscene.md` 「길이」). 연출(좌표·시간)은 `Assets/Scripts/Cutscene/Scenes/`의 클래스 상수다.
- 보스 히트박스: v4 10·15절대로 **레인 띠 하나**로 정리했고 판정도 같은 박스다(`Docs/Boss.md` 「레인 띠 = 판정」). 띠 알파 0.3~0.4(공격 직전 0.45). `bossHitboxAlpha`·`Placeholder/HitboxFrame.png`는 삭제했다.
- 거리 표기 단위 `m`: 1 unit = 1 m로 표기.
- 충돌 연출 지속 0.25초, 흔들림 진폭 0.08. 플레이어 히트박스는 2026-09-06 기획자 답변 1로 **가로만 몸통의 60%로 좁혔다**: 연어 알파 박스(531×280 px, 스케일 0.29) 기준 가로 ×0.6 · 세로 ×0.87 = 월드 약 **0.92×0.71**(`Play.unity`의 `BoxCollider2D` `m_Size` 3.186 × 2.4359999, `m_Offset` 0). 스포너 `playerHalfWidth`도 0.46으로 맞췄다(`Docs/Spawner.md` 「플레이어 히트박스」).
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

## v4 반영 (2026-09-06)

기획서 v4 `04_점프_시스템`·`10_보스전_공통규칙`·`17_Pause_실패_구간전환_UI`·`23_디버그_테스트기능` 반영분. 작성 시점에는 오프라인 Roslyn 컴파일만 확인했고, 통합 패스(2026-09-06)에서 `Regenerate Core Loop Scenes (Overwrite)`(내부에서 `BossAssetSetup.Generate(true)`)와 EditMode/PlayMode를 돌려 전부 통과했다(「통합 검증 결과 (2026-09-06)」).

| 항목 | 내용 |
| --- | --- |
| 값 | `jumpDuration` 0.8→1.6, `jumpHeight` 1.6→2.2, `bossTelegraphDuration` 0.8→1.2, `bossFullLaneTelegraphDuration` 1.0→1.5. 새 필드 `bossLaneBandWidth`·`bossLaneBandHeight`·`bossBannerDuration`·`bossClearBannerDuration`·`debugEnabled` |
| 배너 | `PlayHud.banner`(중앙 Text) + `ShowBannerAsync`. `PlayFlow.RunBossAsync`가 보스 전후로 띄운다 |
| 디버그 | `PlayFlow.HandleDebugInput`/`DebugJumpTo`/`PrepareDebugJump`, `PlayFlow.IsBossInterrupted`를 `BossDirector`가 본다 |
| 보스 판정 | `Game.Boss`의 새 `LaneHazard`. `Docs/Boss.md` |
| 생성기 | `CoreLoopSetup`에 `Banner` 텍스트 추가, `BossAssetSetup`이 보스마다 `LaneHazard`를 만들고 공격체에서 `Hazard`·`HitboxView`를 뺐다 |
| 재생성 | 통합 패스에서 Play 씬(HUD Banner), 보스 프리팹 3개 + `BossSet.prefab`을 다시 만들었다. `GameConfig.asset`·보스 데이터 3개는 직접 고친 값 그대로 |

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

## 통합 검증 결과 (2026-09-06)

다섯 기능 에이전트(보스 v4·컷신·스포너 아트·애니메이션 공유 pivot·상류 단차) 뒤의 통합 패스. 에디터를 닫고 배치로 돌렸다. 로그는 `Team1004/Logs/int_*.log`.

| 단계 | 명령 | 로그 | 결과 |
| --- | --- | --- | --- |
| 생성 | `-executeMethod Game.Bootstrap.Editor.CoreLoopSetup.Regenerate -quit` | `int_regen.log` | 종료 코드 0, `error CS` 0. 스포너(패턴 28)·컷신 대사 프리팹·보스 프리팹 4개·애니메이션(pivot (0.53, 0.38), 연어 11장 + 장애물 물고기 2장)·환경 프리팹·`LedgeSet.prefab`(PNG 3장) 전부 Overwrite, `[CoreLoopSetup] Core loop scenes are ready` |
| EditMode | `-runTests -testPlatform EditMode` | `int_editmode.log`, `int_editmode.xml` | **267/267 통과**(`Game.Ledge.Tests` 17, `SharedPivotFolderTests`, `SalmonArtAssetTests`, `ObstacleClipTableTests`, `Game.Config.Tests` 포함) |
| PlayMode 1차 | `-runTests -testPlatform PlayMode` | `int_playmode_run1.log`, `int_playmode_run1.xml` | 5/6. `WalrusTwoLaneBand_…` 실패(아래) |
| PlayMode 2차 | 같음 | `int_playmode.log`, `int_playmode.xml` | **6/6 통과**, 57.6초. 스모크 3.2초, `DebugKey_WithDebugDisabled` 1.1초, `DebugKeyTwo` 3.5초, `WalrusTwoLaneBand` 15.2초, `FailurePath` 4.5초, `FullLoop` 30.1초(8배속, 단차 2개 통과 포함) |

고친 것:

| 문제 | 원인 | 조치 |
| --- | --- | --- |
| `FullLoopTests` "The control hint was not shown when section 1 started"(이전 통합에서 한 번) | `PlayHud`가 `Time.time + 초`로 힌트를 껐다. 컷신이 끝난 프레임에 스포너 `Initialize`(패턴 분석) 등이 몰려 다음 프레임의 `Time.deltaTime`이 `maximumDeltaTime × 8 = 2.67초 > controlHintDuration 2.5초`가 되면 테스트 코루틴이 보기 전에 꺼졌다 | `PlayHud`가 `Time.deltaTime` 누적으로 남은 시간을 줄이고, 0이 된 프레임까지는 보인 뒤 **다음 `Update`에서** 끈다(최소 한 프레임 보장). 단언은 그대로 |
| `FullLoopTests`가 컷신을 Space로 넘겼다 | 컷신 담당이 `Cutscene/Advance`를 Enter·클릭으로 바꿨다. 컷신마다 20초 대체 `Skip()`을 기다려 100초를 버렸다 | Enter 연타로 변경 |
| `FullLoopTests`가 단차에서 멈출 위험 | 8배속 루프에서 플레이어가 착지 상태로 단차에 닿으면 월드가 멈춘다 | `LedgeHandler.IsHoldingWorld`면 8프레임마다 ↑(재점프). 구간 1·2의 `Cleared`/`Finished`, 루프 뒤 단차 비활성·스폰 재개를 단언에 추가 |
| `WalrusTwoLaneBand_…` "No two lane attack was observed with the player inside the band" | 덮인 레인에서 맞으면 `PlayFlow.OnHit` → `Hit` 상태 → `BossDirector`가 같은 프레임에 `boss.Abort()` → 테스트 루프 조건 `boss.IsActive`가 먼저 꺼져 `HasHit`를 못 봤다(경쟁). 게임 동작은 정상 | 루프를 나온 뒤 `watching && expectHit && player.HasHit`면 판정 성공으로 처리, 실패 메시지에 상태 요약 추가 |
| `DebugKeyTwo`가 62.8초, 로그에 "There are 2 audio listeners" 17.7만 줄 | 앞 테스트(`DebugKey_WithDebugDisabled`)가 Play 씬 페이드 중에 끝나고, 다음 테스트가 바로 `Bootstrap.unity`를 로드해 `GameBootstrap`의 Start 로드가 `[SceneController] Another scene operation is already active`로 무시됐다. 새 Bootstrap 씬(두 번째 `AudioListener`)이 60초 타임아웃까지 남았다 | 세 테스트 파일 모두 Bootstrap 로드 전에 이전 `SceneController.IsTransitioning`이 끝나길 기다린다. `DebugJumpTests`는 Start 씬 진입도 단언한다. 남은 "2 audio listeners" 약 370줄/테스트는 Bootstrap→Start 전환 동안의 일시 상태(Core `Singleton` 중복 처리) |

YAML 점검(생성 뒤):

| 대상 | 확인 |
| --- | --- |
| `Play.unity` `PlayFlow` | `player`·`scroller`·`spawner`·`hud`·`bossTimer`·`pausePanel`·`resultPanel`·`cutscene`·`environment`·`ledgeDirector`·`playScene`·`startScene` 전부 non-zero, 컷신 id `intro`/`cutscene1~3`/`ending` |
| `LanePlayer` | 클립 4개 non-zero, 스프라이트 = `물고기 기본-1`(guid `0c496a74…`) |
| `PlayHud` | `progressFill`·`distanceText`·`pauseButton`·`controlHint`·`banner` non-zero |
| `CutscenePlayer` | `stageCamera`·`dialogueView` non-zero, `CutsceneActor` 4개(`CutsceneChild` 포함) |
| `LedgeSet` 인스턴스 | 프리팹 수정값 `player`·`environment` 연결 |
| `BossSet` 인스턴스 | `stageCamera` = Main Camera |
| 보스 프리팹 3개 | `LaneHazard` 1 + `Band0..2`, `HitboxView` 0 |
| `Fish.prefab` | 스프라이트 = `장애물 물고기1`(guid `84f698fb…`), `swimClip` = `Obstacle_Fish`, `m_DrawMode: 0` |
| 아트 `.meta` | 연어 11장 pivot 전부 `(0.52994794, 0.37592593)`, 장애물 물고기 2장 전부 `(0.52708334, 0.4912037)` |
| 폰트 | `Play.unity`의 `m_Font` 13개 전부 KOTRA HOPE(`743451263bb3aa44b84a9ed9ebc76680`), 다른 폰트 0 |
| `CutsceneDialogue.prefab` | 힌트 `Enter / 클릭 ▶` |

정리한 것: `Assets/GameAssets/Placeholder/HitboxFrame.png`(+meta) 삭제, `GameConfigValues.bossHitboxAlpha`·`GameConfig.asset`의 값 삭제(참조 0건 확인). `Requests.md`의 9-slice 히트박스 프레임 아트 요청은 철회로 표시했다.

사람이 볼 것: 「수동 플레이 체크리스트」 전부, 특히 2(인간 아이·Enter 넘기기), 5-1(단차 두 개의 모양·타이밍·막힘 체감), 6~8(보스 v4 레인 띠 = 판정), 8-1(공중에서 숫자키). 임시 아트(단차 바위·`↑`, 아이 사각형)는 `Requests.md`의 아트 요청이 처리되면 교체한다.
