# 환경 (Game.Environment)

무한 스크롤 배경과 수면이다. 스테이지 길이만큼 맵을 깔지 않고 타일 몇 장을 순환시킨다(사용자 결정). 기획서 3판 `Team1004/Assets/Documents/정리 1번/3번 정리.md` 3절(물 표현), 11절 구간 4(고향 풍경), 22절(단순 그래픽, 물속 배경 1~2종 반복)을 반영했다.

핵심 규칙은 3절이다. **물 흐름은 항상 왼쪽이고 구간이 바뀌어도 물살·파도가 변하지 않는다.** 그래서 이 모듈에는 구간별 세기 조절 API가 없다. 폭포 보스의 물줄기는 `Game.Boss`가 따로 그린다.

2026-09-06에 임시 텍스처 배경을 걷어내고 임포트한 에셋 `Assets/Free 2D Cartoon Parallax Background/!_Moutain/`(산 세트)으로 바꿨다. 물은 스프라이트 층을 쓰지 않고 폴리곤 두 장(뒤 = 불투명 물속, 앞 = 반투명 물너머)만 쓴다.

## 생성기는 동결됐다

**`EnvironmentSetup`·`CoreLoopSetup`을 다시 돌리면 안 된다.** 지금의 정본은 `Assets/GameAssets/Environment/Environment.prefab`(프리팹 YAML)이며 생성기가 아니다.

- `Assets/Scripts/Environment/Editor/`의 `EnvironmentSetup`·`EnvironmentTextures`·`EnvironmentPatterns`는 컴파일만 되는 상태로 남겨 뒀다. 이제 없는 직렬화 필드(`far`/`mid`/`near`/`flow`/`calmOverlay`/`baseFlowSpeed`/`calmTimeScale`)를 `FindProperty`로 찾으므로 **실행하면 NullReference로 죽거나 프리팹을 옛 구조로 덮어쓴다.**
- 메뉴 `Team1004 > Generate Environment Assets`, `Team1004 > Regenerate Environment Prefab (Overwrite)`, `CoreLoopSetup.Regenerate`를 쓰지 않는다.
- 배경을 바꿀 때는 프리팹을 에디터에서 직접 고치거나 프리팹 YAML을 손으로 고친다. Play 씬의 `Environment`는 프리팹 인스턴스이고 오버라이드가 이름·Transform뿐이라 프리팹 수정이 그대로 씬에 반영된다.

## 파일

| asmdef | 경로 | 타입 | 책임 |
| --- | --- | --- | --- |
| `Game.Environment` | `Assets/Scripts/Environment/` | `EnvironmentThing : MonoThing` | 프리팹 루트 호스트. 모듈을 붙이고 공개 API를 위임한다 |
| | | `EnvironmentLayer` | 층 하나의 직렬화 묶음(루트 Transform, 타일 폭, 속도 배율). `CreateRunner`가 자식들을 모아 `TileStripRunner`를 만든다 |
| | | `TileStrip` | 순환 계산 순수 함수. 할당·Unity 오브젝트 없음. 테스트 대상 |
| | | `TileStripRunner` | `Transform[]`에 `TileStrip` 결과를 적는다. 매 프레임 할당 없음 |
| | | `ParallaxLayerModule : Module` | `Ticks = Update`. 층 하나를 `Speed × SpeedScale`로 왼쪽으로 민다 |
| | | `HomelandModule : Module` | `Ticks = Update`. 고향 소품 그룹 표시·순환 |
| | | `WaterSplashModule : Module` | `Ticks = None`. 수면 노드를 직접 눌러 첨벙을 만든다 |
| | | `WaterOverlayModule : Module` | `Ticks = LateUpdate`. `WaterSurface`의 스플라인을 `WaterOverlay`로 복사하고 `BakeMesh` |
| `Game.Environment.Editor` | `Assets/Scripts/Environment/Editor/` | `EnvironmentSetup` 등 | **동결.** 위 「생성기는 동결됐다」 |
| `Game.Environment.Tests` | `Assets/Scripts/Environment/Tests/` | `TileStripTests` | EditMode 10개. 순환 계산 검증 |

`WaterFlowModule`은 지웠다(물결 스프라이트 층 전용이었다).

asmdef 참조: `Game.Environment` → `Core.Modules`, `Core.Foundation`, `Game.Config`, `Game.Water`, `Unity.2D.SpriteShape.Runtime`. **`Game.Play`는 참조하지 않는다.** 순환 없음.

## 오브젝트 트리 (`Assets/GameAssets/Environment/Environment.prefab`)

```text
Environment (EnvironmentThing)
  Sky                       Layer_0 한 장, 순환 없음, 세로만 눌러 화면 띠에 맞춘다
  CloudLayer    / Tile0..3  Layer_1
  MountainLayer / Tile0..3  Layer_2
  HillLayer     / Tile0..3  Layer_3
  GroundLayer   / Tile0..3  Layer_4
  RiverbedLayer / Tile0..3  Env_Riverbed (강바닥 모래 띠, Tiled)
  WaterSurface              Water + SpriteShapeController + SpriteShapeRenderer + PolygonCollider2D (물 뒤판, 불투명)
  Homeland      / Tile0..3  비활성. SetHomeland(true)로 켠다
  WaterOverlay              SpriteShapeController + SpriteShapeRenderer (물 앞판, 반투명)
```

자연 층 타일은 `SpriteRenderer.drawMode = Simple`이고 크기는 `Transform.localScale`로 준다. 소스 PNG는 4320×2160, 임포트 PPU 108이라 스케일 1에서 40×20 unit이다(가로 이음매가 원본에서 이미 맞는다). Homeland만 예전대로 `Tiled` + `size`다.

## 원근 (층 표)

카메라 ortho 3.6, 16:9, 화면 x ±6.4, y ±3.6. 수면 y는 `GameConfig.Current.WaterSurfaceY`(2.0). **물 위로 보이는 띠는 y 2.0~3.6, 겨우 1.6 unit**이라 배경은 이 띠 안에서 원근을 만들어야 한다.

깊이를 세 가지로 동시에 표현한다.

1. **시차**: 먼 층일수록 느리게 흐른다(`speedScale`).
2. **스케일과 높이**: 먼 층일수록 작고 화면 위쪽(수평선 쪽), 가까운 층일수록 크고 아래쪽(수면 쪽).
3. **대기 원근**: 먼 층일수록 하늘색 쪽으로 밝기·채도를 낮춘다(`SpriteRenderer.m_Color`).

| 층 | 소스 | 스케일 | 타일 폭 (=40×스케일) | 타일 수 | 타일 x | 시차 | 루트 y | 화면에서 읽히는 y | 틴트 (r,g,b,a) | sortingOrder |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Sky | `Layer_0` | x 0.36 / y 0.12 | — | 1 | 0 | 0 (고정) | 2.6 | 1.4 ~ 3.8 | 1, 1, 1, 1 | -12 |
| CloudLayer | `Layer_1` | 0.28 | 11.2 | 4 | ±16.8, ±5.6 | 0.10 | 1.25 | 구름 띠 1.62 ~ 3.58 | 0.96, 0.98, 1, 0.90 | -11 |
| MountainLayer | `Layer_2` | 0.30 | 12.0 | 4 | ±18.0, ±6.0 | 0.30 | 2.67 | 봉우리 3.50, 능선 바닥 1.73 | 0.82, 0.87, 0.95, 1 | -10 |
| HillLayer | `Layer_3` | 0.34 | 13.6 | 4 | ±20.4, ±6.8 | 0.60 | 3.36 | 언덕 마루 3.05, 바닥선 2.23 | 0.91, 0.95, 0.99, 1 | -9 |
| GroundLayer | `Layer_4` | 0.38 | 15.2 | 4 | ±22.8, ±7.6 | 1.00 | 4.75 | 나무 우듬지 4.54, 풀 윗선 2.50, 흙 1.72 ~ 0.95 | 1, 1, 1, 1 | -8 |
| WaterSurface (뒤판) | 폴리곤 | — | 폭 16, 깊이 6.5 | — | — | 고정 | 2.0 | 2.0 ~ -4.5 | 1, 1, 1, 1 | -6 |
| RiverbedLayer | `Env_Riverbed` | — | 8 (Tiled 8 × 5) | 4 | ±12, ±4 | 2.00 | -3.95 | 모래 윗선 **-1.65**(요철 -1.57 ~ -1.73), 아랫변 -6.45 | 1, 1, 1, 1 | **-5** |
| Homeland | `Env_Homeland` | — | 8 | 4 | ±12, ±4 | 1.00 | -2.55 | -3.3 ~ -1.8 | 1, 1, 1, 1 | -4 |
| WaterOverlay (앞판) | 폴리곤 | — | 수면 스플라인 복사 | — | — | 고정 | 2.0 | 2.0 ~ -4.5 | 0.55, 0.8, 0.95, **0.15** | **15** |

층 안의 어떤 원본 픽셀 행 `r`(위가 0, 높이 2160)이 어느 y에 오는지는 이렇게 계산했다.

```text
y(r) = 루트y + 10 × 스케일 − r × (20 × 스케일 / 2160)
```

원본 층별 내용 행(알파를 재서 확인):

| 소스 | 내용 | 행 |
| --- | --- | --- |
| `Layer_0` | 하늘 그라데이션. 가로 색차 0(순수 세로 그라데이션) | 0 ~ 2159 |
| `Layer_1` | 구름 | 180 ~ 939 |
| `Layer_2` | 먼 산. 봉우리 시작 780, 능선 아래는 통짜 | 780 ~ 2159 |
| `Layer_3` | 언덕. 마루의 잔나무 760~, 덩어리 1180~, 통짜 1440~ | 760 ~ 2159 |
| `Layer_4` | 나무 1140~1720, 풀 띠 1720~1940, 흙 둔덕 1940~2159 | 1140 ~ 2159 |

수평선은 y 2.2~2.5 근처로 모았다. 산 능선(1.73)과 구름 아랫단(1.62)은 수면 아래로 내려가 불투명한 물에 가려지고, 언덕 바닥선(2.23)과 풀 윗선(2.50)이 수면 바로 위에서 만난다. 물 위 띠에서 위에서부터 구름(3.58) → 산봉우리(3.50) → 언덕 마루(3.05) → 나무·풀(2.50) 순으로 낮아진다.

**GroundLayer 나무 우듬지는 화면 위(3.6)에서 잘린다**(4.54까지 올라간다). 물 위 띠가 1.6 unit뿐이고 원본 나무는 스케일 0.38에서 2.0 unit이라 어쩔 수 없다. 나무 사이 빈 곳(원본 커버리지 50~60%)으로 산·언덕·하늘이 보이는 구성이다. 잘림을 줄이려면 `GroundLayer`의 루트 y를 낮추거나(풀 윗선이 같이 내려간다) 타일 `localScale`을 0.30 근처로 줄인다. 스케일을 바꾸면 `EnvironmentThing.ground.tileWidth`도 `40 × 스케일`로 같이 고쳐야 이음매가 맞는다.

Sky만 세로를 따로 눌렀다(x 0.36 / y 0.12). `Layer_0`은 가로 색차가 정확히 0인 세로 그라데이션이라 비균등 스케일로도 일그러지지 않고, 2160행 그라데이션 전체를 물 위 띠에 담을 수 있다.

`LaneGuides`(-5)는 물 뒤판(-6) 앞, Homeland(-4) 뒤에 그대로 들어간다. 장애물(5)·보스(4~6)·플레이어(10)는 물 앞판(15)보다 뒤라 옅게 덮인다. **보스 텔레그래프만 16으로 올려 앞판 위에 온다**(아래 「물 앞판」).

`RiverbedLayer`도 -5다. **`LaneGuides`는 -5 그대로 두었다**(2026-09-06). 레인 안내선은 y 1.1 / 0 / -1.1에 두께 0.06으로 그려지고 강바닥은 -1.65 아래에만 있어 화면에서 겹치는 픽셀이 하나도 없다. 정렬이 같아도 서로 가릴 일이 없으므로 `Assets/Scenes/Play.unity`는 손대지 않았다.

## 강바닥 (`RiverbedLayer`, sortingOrder -5, 2026-09-06)

맨 아랫줄 장애물(`Rock`)이 「바닥에 놓인 바위」로 읽히려면 바닥이 그려져 있어야 한다. 예전에는 수면 아래가 불투명 물 폴리곤 한 장뿐이라 돌이 물속에 떠 있었다.

- **정렬 -5**다. 불투명 물 뒤판(-6)보다 **앞**이라 물에 가려지지 않고, 반투명 물 앞판(15)보다 **뒤**라 앞판의 물빛(0.55, 0.8, 0.95, 0.15)이 모래 위에 그대로 얹힌다. 「물 너머로 보이는 바닥」이 된다. 게임플레이(0 이상)보다는 뒤라 플레이어·장애물·보스는 언제나 모래 앞에 그려진다.
- **윗선이 정확히 y -1.65**다. 맨 아랫줄 레인 띠의 아랫변(`laneY[2] - 0.55` = -1.65)이고 `Rock`의 아랫변과 같다. 그래서 돌이 모래 위에 얹혀 보인다.
- 실루엣 요철은 **평균선 위로 최대 0.08**(-1.57)까지만 올라간다. 더 크게 하면 돌 아랫변이 모래에 파묻힌 것처럼 보인다.
- 아랫변 -6.45. 화면 아래(-3.6)보다 2.85 깊어서 단차 연출로 월드가 0.35 내려가도(-6.80) 아래가 비지 않는다.
- 타일은 `SpriteRenderer.drawMode = Tiled`, `size` 8 × 5다. 소스가 400×500 px / PPU 100 = 4.0 × 5.0 unit이라 **가로 정확히 2회, 세로 정확히 1회** 반복한다. 세로가 딱 한 번이어야 윗선 실루엣이 아래에서 되풀이되지 않는다. 타일 4장이면 `total = 32 ≥ 2 × 8 + 12.8`이라 점프가 화면에 보이지 않는다.
- 시차 `speedScale`은 **2.0**이다. 아래 「배경 속도 배율」을 본다.

`Assets/GameAssets/Placeholder/Env_Riverbed.png`(400×500)는 PIL로 만든 임시 모래 텍스처다. 따뜻한 베이지에서 아래로 갈수록 어두워지고, 잔물결(dune ripple) 선 세 겹과 고운 그레인이 얹혀 있다. 윗선은 주기가 폭을 정확히 나누는 사인 합이라 **가로로 이음매가 없고 평균이 정확히 20 px(= 0.20 unit)**이다. 스프라이트 윗변이 -1.45일 때 평균선이 -1.65가 된다. `.meta`는 `Ledge_Cascade.png.meta`를 본떠 손으로 썼다(Sprite / Single / PPU 100 / Wrap Repeat / FullRect / mipmap 없음).

## 배경 속도 배율 (`scrollScale`, 2026-09-06)

사용자 요청으로 **배경 전체가 절반 속도로 흐른다**. `EnvironmentThing.scrollScale`(직렬화, 기본 **0.5**)이 층 속도에 곱해진다.

```text
층 실제 속도 = Speed × ScrollScale × 층 speedScale
             = GameConfig.ScrollSpeed(4.0) × 0.5 × speedScale
```

`GameConfig.scrollSpeed`는 **건드리지 않았다**. 장애물·단차·거리(`PlayFlow.Distance`)는 그대로 4 u/s다. 층별 상대 시차는 그대로다(구름 0.10, 산 0.30, 언덕 0.60, 지면 1.00, 고향 1.00).

**강바닥만 `speedScale`이 2.0이다.** 강바닥은 장애물이 그 위를 지나가는 층이라 배경처럼 느려지면 안 된다. 돌은 4 u/s로 오는데 모래가 2 u/s로 흐르면 돌이 모래 위를 미끄러지는 것처럼 보인다. `2.0 × 0.5 × 4.0 = 4.0 u/s`로 맞춰 두었다.

- 배경만 더 느리게/빠르게 하려면 `Environment.prefab`의 `scrollScale`을 바꾼다. 이때 **강바닥 `speedScale`도 `1 / scrollScale`로 같이 바꿔야** 장애물과 맞는다.
- 월드 전체를 느리게 하고 싶다면 `scrollScale`을 1로 되돌리고 `GameConfig`의 `scrollSpeed`를 내리는 쪽이 맞다. 그러면 장애물·단차·거리가 모두 같이 느려지고 강바닥 `speedScale`도 1.0으로 되돌리면 된다.
- 런타임에서는 `EnvironmentThing.ScrollScale`로 바꾼다(즉시 반영).

## 물

물은 스프라이트 층을 쓰지 않는다. 같은 수면 스플라인을 공유하는 폴리곤 두 장이다.

### 뒤판 (`WaterSurface`, sortingOrder -6)

`Game.Water.Water` + `SpriteShapeController`. 시뮬레이션한 수면을 매 프레임 스플라인으로 굽는다(기존과 같다).

- **불투명**하다. 채움 텍스처는 `Assets/GameAssets/Placeholder/Env_Underwater.png`(64×512, 위 밝은 청록 → 아래 짙은 감청, 알파 1)이고 `WaterSurfaceProfile.asset`의 `m_FillTexture`가 이것을 가리킨다.
- 이 한 장이 **물속 배경**이다. 자연 층(-12~-8)은 전부 뒤판보다 뒤라 수면 아래는 통째로 가려진다. 산·언덕·나무의 밑동이 물에 잠기는 것을 신경 쓰지 않아도 되는 이유다.
- 깊이(`Water.size.y`)를 0.8 → **6.5**로 늘렸다. 수면 2.0 기준으로 바닥이 -4.5라 단차 연출로 월드가 내려가도 화면 아래(-3.6) 밖을 유지한다. `PolygonCollider2D`와 초기 스플라인도 같은 4점(±8, 0 / ±8, -6.5)이다.
- `SpriteShapeController.m_StretchUV = 1`, `m_StretchTiling = 1`로 두었다. 이 모드에서 채움 UV는 `(정점 − 바운즈최소) / 바운즈크기`라 그라데이션이 폴리곤 세로에 **정확히 한 번** 매핑된다(v=0 = 폴리곤 바닥 = 텍스처 아래쪽 = 짙은 색). 타일링 모드로 두면 UV가 월드 좌표를 쓰기 때문에 물이 오르내릴 때 그라데이션이 미끄러진다.

### 앞판 (`WaterOverlay`, sortingOrder 15)

`SpriteShapeController` + `SpriteShapeRenderer`만 있는 새 오브젝트다. `Water`도 콜라이더도 없다.

- `WaterOverlayModule`(LateUpdate)이 매 프레임 `WaterSurface`의 `SpriteShapeController.spline` 제어점(위치·높이·탄젠트·모드)을 그대로 복사하고 `BakeMesh()`를 부른다. 두 오브젝트의 `localPosition`이 같아서(둘 다 (0, 2, 0)) 로컬 좌표를 그대로 옮기면 된다.
- 채움은 `Env_Overlay.png`(4×4 흰색)이고 색은 `m_Color` (0.55, 0.8, 0.95, **0.15**)다. 알파는 `EnvironmentThing.overlayAlpha`(직렬화, 기본 0.15)가 `Initialize`에서 덮어쓴다. RGB는 프리팹 값을 그대로 둔다.
- **왜 두 장인가.** 뒤판만 불투명하게 하면 물속이 배경이 되지만 플레이어·장애물·보스는 여전히 "물 위에 얹힌" 그림이다. 앞판을 같은 모양으로 한 장 더 덮으면 물속에 있는 것들이 전부 얇은 물빛을 통과해 보여 "물 너머"가 된다. 알파를 0.15로 잡은 것은 실루엣 판독을 해치지 않는 상한이다. 0.25를 넘으면 장애물 색 구분이 흐려진다.
- **보스 텔레그래프는 덮으면 안 된다.** 경고 띠가 흐려지면 회피 판단이 늦는다. `Assets/GameAssets/Boss/FishingLineBoss.prefab`·`WalrusBoss.prefab`·`WaterfallBoss.prefab`의 `Telegraph/Lane0..2` `SpriteRenderer`를 3 → **16**으로 올려 앞판(15) 위에 둔다.

### 지운 것

| 대상 | 이유 |
| --- | --- |
| `FlowLayer`(Env_Flow 타일 5장) | 물속이 불투명 폴리곤이라 물결 스프라이트가 보이지 않는다 |
| `CalmOverlay` | 같은 이유. `SetCalm`/`IsCalm`은 `[Obsolete]` 무동작으로 남겼다(호출처 없음, `grep` 확인) |
| `EnvironmentThing.flow`, `calmOverlay`, `baseFlowSpeed`, `calmTimeScale` | 위 두 개의 직렬화 참조 |
| `WaterFlowModule` | `FlowLayer` 전용이었다 |
| `Env_Flow.png`, `Env_WaterFill.png`, `Env_Calm.png`, `Env_Sky.png`, `Env_Far.png`, `Env_Mid.png`, `Env_Near.png` (+`.meta`) | GUID를 `Assets/` 전체에서 검색해 참조 0을 확인하고 지웠다 |

`Env_Homeland.png`는 남는다(고향 소품). 새로 만든 것은 `Env_Underwater.png`, `Env_Overlay.png`, `WaterOverlayProfile.asset`이다.

## 공개 API (`EnvironmentThing`)

| 멤버 | 설명 |
| --- | --- |
| `void Initialize()` | 모듈 부착·오버레이 알파 적용·오프셋 기준 y 캐시·`SetScrolling(scrollingOnAwake)`. `Awake`가 부르고 두 번 불러도 안전하다. `bool IsInitialized` |
| `float Speed { get; set; }` | 근경(GroundLayer) 기준 속도. `GameConfig.Current.ScrollSpeed`(4.0)로 시작하고 `GameConfig.Changed`를 구독해 갱신한다. 층별 실제 속도는 `Speed × SpeedScale` |
| `float ScrollScale { get; set; }` | 배경 전체 속도 배율(직렬화, 기본 **0.5**). 층 실제 속도는 `Speed × ScrollScale × SpeedScale`. `GameConfig.ScrollSpeed`는 건드리지 않는다 |
| `void SetScrolling(bool)` | 모든 층(강바닥 포함)의 이동을 켜고 끈다. `bool IsScrolling` |
| `void SetHomeland(bool)` | 구간 4 소품 그룹을 켜고 끈다. 켤 때 순환 위치를 0으로 되돌린다. `bool IsHomelandVisible` |
| `void SetOverlayAlpha(float)` | 물 앞판 알파(0~1). `float OverlayAlpha`, `bool IsOverlayLinked` |
| `bool Splash(float x, float strength)` | 월드 x에 첨벙을 만든다. 양수 = 아래로 눌림(착수), 음수 = 위로 솟음(도약) |
| `Water Surface { get; }`, `float SurfaceY { get; }` | 수면 오브젝트와 수면 y |
| `void SetVerticalOffset(float)` | `CloudLayer`·`MountainLayer`·`HillLayer`·`GroundLayer`·`RiverbedLayer`·`Homeland`·`WaterSurface`·`WaterOverlay`의 y를 `Initialize` 때 위치 + offset으로 옮긴다. **`Sky`만 고정**이다. `float VerticalOffset` |
| `Awaitable RaiseWorldAsync(float amount, float duration)` | `duration` 동안 오프셋을 `현재 − amount`로 SmoothStep 보간한다(= 시점이 `amount`만큼 올라간 느낌) |
| `void SetCalm(bool)`, `bool IsCalm` | `[Obsolete]` 무동작. 예전 `CalmOverlay` 잔재 |

`SetVerticalOffset`이 **물까지 같이 내리도록 바뀌었다**(예전에는 지형 층만). 풀 윗선(2.50)과 수면(2.0)이 0.5 unit 차이로 붙어 있어서, 물을 두고 지형만 내리면 단차 연출 동안 강둑이 물속으로 잠겼다 나온다. 대신 `Water.bounds`가 `transform.position`을 쓰므로 시뮬레이션 수면도 같이 내려간다. 단차가 쓰는 값은 `environmentRiseAmount` 0.35뿐이고(`Docs/Ledge.md` 「확신 없는 지점」) 0.35 내려간 수면 1.65는 상단 레인 플레이어 윗변(약 1.45)보다 위라 물 밖으로 나오지 않는다. **이 값을 1.1로 올리면 상단 레인이 물 밖으로 나온다.**

**강바닥도 오프셋을 따라간다**(2026-09-06). 그래서 단차 상승 0.7초 동안 강바닥 윗선이 -1.65 → -2.0으로 내려가고, 고정 좌표인 돌 아랫변(-1.65)과 0.35 벌어진다. 실제로는 거의 보이지 않는다. 상승이 시작되는 순간 단차 조각의 `UpperBed`(폭 30)가 앞면 오른쪽 전부를 덮고 있고, 앞면이 x -6.4를 지나면 화면 전체가 `UpperBed`(정렬 -5, z -0.005 → 환경 강바닥보다 앞)라 환경 강바닥이 아예 보이지 않기 때문이다. 게다가 접근 2초부터 장애물 스폰이 멈추므로 그 구간에 돌이 거의 없다. 남는 것은 상승 첫 0.25초쯤 화면 왼쪽 끝(앞면 왼쪽)의 좁은 띠뿐이다. 완전히 없애려면 `LedgeData.environmentRiseAmount`를 0으로 두면 된다(배경이 아예 안 내려간다).

`Time.timeScale`이 0이면 `Time.deltaTime`이 0이라 모든 층이 멈춘다(일시정지·컷신 정지와 자동으로 맞는다).

## 순환 계산

`TileStrip`은 Unity 오브젝트 없이 계산만 한다.

```text
total   = tileWidth × tileCount
offset  = Wrap(offset + speed × dt, total)     프레임마다 [0, total)로 되감는다
slot_i  = Wrap(i × tileWidth − offset, total)
x_i     = −total/2 + slot_i + tileWidth/2
```

`offset` 하나만 누적하고 매 프레임 `[0, total)`로 되감으므로 오차가 쌓이지 않는다(테스트 `AdvanceDoesNotAccumulateError`: 6000프레임 후 오차 < 0.02).

점프가 화면에서 보이지 않으려면 `total ≥ 2 × tileWidth + 화면 폭(12.8)`이어야 한다. 층별로 타일 4장이면 `total = 4w ≥ 2w + 12.8` → `w ≥ 6.4`이고 지금 가장 좁은 타일이 11.2라 여유가 있다.

**모듈은 시작할 때 `Apply()`를 부르지 않는다.** 프리팹의 타일 x가 이미 `TileStrip.TileX(w, 4, 0, i)` = `−1.5w, −0.5w, 0.5w, 1.5w`여야 한다. 층 스케일을 바꿔 `tileWidth`를 바꾸면 타일 4장의 x도 같이 고친다.

## 물 튀김 / 수면 상호작용

`EnvironmentThing.Splash(x, strength)`는 컷신처럼 물리 없이 명시적으로 첨벙을 넣을 때 쓰는 연출 API다(`splashRadius` 0.55, `splashMaxDepth` 0.6). 나머지 상호작용은 전부 `Game.Water`가 한다.

**2026-09-06에 수면 반응이 두 갈래가 됐다.** 예전에는 수직 속도만 전달해서, 수면(2.0) 아래를 가로로만 미끄러지는 것들(레인 y 1.1/0/−1.1의 장애물, 낚싯바늘 훑기, 바다코끼리 돌진)은 수면에 아무 흔적도 남기지 않았다. 이제 가로 속도가 만드는 **항적(wake)**이 더해져서 움직이는 모든 것이 수면을 흔든다.

| 갈래 | 조건 | 계산 |
| --- | --- | --- |
| 착수/도약 (기존) | `TransfersVerticalVelocity`가 참이고, 노드가 콜라이더 안(`OverlapPoint`)이고 수면 변위 `\|py\| < surfaceCollisionDistance` | 노드 속도를 `VerticalVelocity × collisionVelocityTransfer × (1 − \|py\|/surfaceCollisionDistance)`로 **덮어쓴다**. 쓰기 전에 `maxInjectedVelocity`로 캡 |
| 항적 (신규) | 노드 x가 바운즈 가로 범위 안이고, 노드 y가 바운즈 윗변보다 `SurfaceInfluence` 이내로 위 | 노드 속도에서 기여분을 **뺀 뒤 클램프한다**. 세기는 `wakeVelocityTransfer × WakeScale`, 결과는 다시 `maxInjectedVelocity`로 캡 |

### 주입 캡 (`maxInjectedVelocity`, 2026-09-06)

두 갈래 모두 노드 속도를 쓰기 전에 `WaterWake.CapInjection(이전 속도, 주입값, cap)`을 통과한다.

```text
limit = max(cap, |이전 속도|)
결과  = clamp(주입값, −limit, +limit)
```

`limit`에 `|이전 속도|`가 들어가므로 **이미 자연스럽게 커진 속도(큰 첨벙, 파도 마루)는 잘리지 않고**, 새로 주입되는 값만 `cap`을 넘지 못한다. 어떤 물체 하나가 프레임마다 큰 세로 속도를 밀어 넣어 수면을 터뜨리는 일을 막는 마지막 방어선이다. `cap`이 0 이하면 캡이 꺼진다(예전 동작).

### 등장 워밍업 (2026-09-06)

**물체가 처음 나타나는 순간에도 수면이 터졌다.** 낚싯바늘이 진입 위치로 순간이동할 때, 바다코끼리가 `RestX`로 돌아갈 때, 폭포 급류·바위가 `SpawnX`로 되돌아갈 때, 풀에서 장애물이 나올 때, 단차 조각이 `parkX` 60에서 7.5로 켜질 때, 보스 세트가 활성화될 때다. 원인은 같다. `VelocitySampler`는 `Δposition / Δt`만 보므로 **한 프레임에 13 unit 순간이동하면 속도 780 u/s가 된다.** 그 값이 그대로 노드에 주입된다.

`WaterInteractorModule`이 두 겹으로 막는다.

1. **순간이동 감지.** `Step`이 매 프레임 먼저 `|현재 위치 − 샘플러 위치| > teleportDistance`(1.0)를 본다. 걸리면 **샘플하지 않고** 샘플러를 그 자리로 리셋하고 워밍업을 다시 걸고 그 프레임 주입을 건너뛴다. 스크롤 4 u/s는 프레임당 0.067이라 절대 걸리지 않는다.
2. **워밍업.** `OnAttached` / `ResetVelocity` / `NotifyTeleport` / 순간이동 감지가 `warmupRemaining = injectionWarmupFrames`(3)로 만든다. 그동안 `Step`은 **샘플은 계속 하되 주입(착수·항적 둘 다)을 하지 않고** `IsTouchingWater`를 false로 둔다. 3프레임 뒤 속도가 실제 이동으로 안정된 다음에야 물을 건드린다.

호출 지점(전부 한 줄):

| 위치 | 무엇 |
| --- | --- |
| `WaterInteractor.OnEnable` / `OnSpawned` / `OnReleased` | 기존 `ResetVelocity()`가 이제 워밍업까지 건다. 풀 스폰·보스 세트 활성화는 여기서 자동으로 잡힌다 |
| `WaterInteractorModule.OnAttached` | 부착 즉시 |
| `LanePlayer.SnapToLane` | 이미 `water?.ResetVelocity()`를 부르고 있었다. 코드 수정 없이 워밍업이 걸린다(`PlayFlow`의 디버그 점프·재시작도 이 경로다) |
| `FishingLineBoss.ResetHook` / `StageHook` | `WaterInteractor.NotifyTeleport(hook)` |
| `WalrusBoss.ResetPosition` | `WaterInteractor.NotifyTeleport(body)` |
| `WaterfallBoss.ParkLane` / `HideWaterfall` | `WaterInteractor.NotifyTeleport(rapid/rock/waterfall)` |
| `LedgeThing.Schedule` | `WaterInteractor.NotifyTeleport(transform)` |

`static WaterInteractor.NotifyTeleport(Transform)`는 자기와 자식의 `WaterInteractor`를 전부(비활성 포함) 찾아 알린다. 순간이동은 드문 사건이라 `GetComponentsInChildren` 비용을 그대로 둔다. 연속 이동 경로(`PlaceHook`·`PlaceAtX`·`PlaceWaterfall`)에는 **넣지 않았다** — 넣으면 매 프레임 워밍업이 걸려 항적이 아예 사라진다.

`Game.Boss`·`Game.Ledge` asmdef에 `Game.Water`를 추가했다(`Game.Water`는 아무것도 참조하지 않으므로 순환 없음).

### 항적 계산 (`WaterWake.cs`)

`WaterWake`는 Unity 오브젝트를 만지지 않는 순수 정적 함수 묶음이다(EditMode 테스트 **21개**, `Tests/WaterWakeTests.cs`. 캡 관련 5개가 2026-09-06에 붙었다). `wakeOnly`·`wakeScale`·오프셋 바운즈·등장 워밍업은 `Tests/WaterInteractorModuleTests.cs`(**12개**)가 본다. 워밍업 테스트는 `WaterInteractorModule.Step(deltaTime)`을 직접 돌린다(`Module.InvokeUpdate`가 `internal`이라 테스트 어셈블리에서 부를 수 없다). `OnUpdate`는 `Step(Time.deltaTime)` 한 줄이다.

```text
proximity = 노드y < 바운즈아랫변            → 0            (물체가 수면 위에 떠 있다)
            바운즈아랫변 ≤ 노드y ≤ 윗변    → 1            (물체가 수면을 뚫고 있다)
            노드y > 윗변                    → 1 − (노드y − 윗변) / SurfaceInfluence, 0으로 클램프

s         = 선두 모서리에서 잰 정규화 위치. vx < 0(왼쪽 진행)이면 s = t, vx > 0이면 s = 1 − t
shape(s)  = s < 0.25 → −0.35 × (1 − s/0.25)      선두 앞은 살짝 들린다 (BowLift)
            s ≥ 0.25 → (s − 0.25) / 0.75          뒤로 갈수록 눌린다, 꼬리에서 최대

기여분    = |vx| × wakeVelocityTransfer × proximity × shape(s)
limit     = max(|기존 속도|, |vx| × wakeVelocityTransfer)
새 속도   = clamp(기존 속도 − 기여분, −limit, +limit)
```

`limit`이 핵심이다. 매 프레임 빼기만 하면 발산하는데, **이미 있는 속도보다 큰 쪽으로는 절대 키우지 않으므로**(`limit`에 `|기존 속도|`가 들어간다) 착수 첨벙 같은 큰 값은 항적이 깎지 않고, 항적만 있는 노드는 `|vx| × wakeVelocityTransfer`에서 정확히 멈춘다. `deltaTime`을 곱하지 않는 대신 클램프가 안정성을 보장한다. 기존 `damping`(0.6)·`tension`(10)은 그대로 복원력으로 작동한다.

### 파일

| 타입 | 위치 | 역할 |
| --- | --- | --- |
| `IWaterCollider` | `Assets/Scripts/Water/IWaterCollider.cs` | `Bounds`, `VerticalVelocity`, **`HorizontalVelocity`**, **`SurfaceInfluence`**, **`TransfersVerticalVelocity`**, **`WakeScale`**, `OverlapPoint(point)` |
| `WaterWake` | `WaterWake.cs` | **신규.** 항적 순수 계산. `Proximity`/`Shape`/`Contribution`/`NodeVelocity`/`Apply(float[] …)` + **`CapInjection`** |
| `WaterSystem.Collide(Water, IWaterCollider)` / `CollideAll(IWaterCollider)` | `WaterSystem.cs` | 수면 노드 밀기. 한 루프에서 착수와 항적을 같이 처리한다 |
| `WaterBody` | `WaterBody.cs` | `IWaterCollider`를 Rigidbody로 구현. 가로 속도는 `linearVelocityX`라 **Kinematic이면 항적이 안 난다** |
| `VelocitySampler` | `VelocitySampler.cs` | 순수 C#. `Sample(position, dt)` = Δposition/Δt를 `Vector2`로. `Smoothing`만큼 이전 값과 섞는다. `Position`은 마지막 샘플 위치(순간이동 감지용). EditMode 테스트 9개. 예전 `VerticalVelocitySampler`를 대체한다(외부 사용처 0을 `grep`으로 확인하고 지웠다) |
| `WaterInteractorModule : Module` | `WaterInteractorModule.cs` | `Ticks = Update`. 호스트의 **월드 위치**를 샘플러에 넣어 `VerticalVelocity`·`HorizontalVelocity`를 만들고 `CollideAll(this)`. 한 프레임 = `Step(deltaTime)`(공개, 테스트용). `WakeOnly`/`WakeScale`/`NotifyTeleport()`/`IsWarmingUp`/`WarmupRemaining` |
| `WaterInteractor : MonoThing` | `WaterInteractor.cs` | 범용 호스트. `IPooledObject`라 풀 재사용 때 `ResetVelocity`가 돈다. `NotifyTeleport()`와 `static NotifyTeleport(Transform)`(자식까지) |

### `WaterSettings` (`Assets/GameAssets/Water/Resources/WaterSettings.asset`)

| 값 | 현재 | 뜻 |
| --- | --- | --- |
| `tension` | 10 | 수면 복원력 |
| `damping` | 0.6 | 감쇠 |
| `spread` | 5 | 옆 노드로 번지는 정도 |
| `iterationsPerFrame` | 3 | 프레임당 시뮬 반복 |
| `surfaceCollisionDistance` | 1 | 착수 판정이 먹는 수면 변위 한계 |
| `collisionVelocityTransfer` | 0.5 | 착수 시 세로 속도 전달률 |
| `velocitySmoothing` | 0 | 샘플러 평활(0~0.99). 올리면 속도가 늦게 따라온다 |
| **`maxInjectedVelocity`** | **1.5** | 한 물체가 한 노드에 한 프레임에 새로 넣을 수 있는 속도의 절대 상한. 0 이하면 캡이 꺼진다 |
| **`injectionWarmupFrames`** | **3** | 등장·순간이동 뒤 주입을 건너뛰는 프레임 수. 0이면 워밍업이 꺼진다 |
| **`teleportDistance`** | **1.0** | 한 프레임 이동이 이 값을 넘으면 순간이동으로 보고 샘플러를 리셋한다. 0이면 감지가 꺼진다 |
| **`wakeVelocityTransfer`** | **0.15** | 항적 세기. 노드 속도 상한이 `\|vx\| × 이 값 × WakeScale`이다 |
| **`wakeInfluenceDistance`** | **1.0** | `SurfaceInfluence`를 따로 주지 않은 물체(플레이어·보스)의 기본 도달 거리 |

`WaterInteractor.surfaceInfluence`가 0 이하면 `wakeInfluenceDistance`를 쓴다. `WaterInteractor.ignoreColliderShape`를 켜면 `Collider2D`(히트박스)를 무시하고 `size`/`offset`(로컬 단위, `lossyScale`이 곱해진다)으로 바운즈를 만든다. 장애물은 히트박스가 그림보다 작아서 이 쪽을 쓴다.

`WaterInteractor`에 2026-09-06에 두 필드가 붙었다. 둘 다 프리팹에 키가 없으면 기본값이 그대로 들어가므로 기존 프리팹은 손대지 않아도 동작이 같다.

| 필드 | 기본값 | 뜻 |
| --- | --- | --- |
| `wakeOnly` | `false` | 켜면 이 물체는 **착수/도약 갈래를 아예 쓰지 않는다**. 항적만 남는다. 자기 세로 이동이 「물에 뛰어드는 것」이 아니라 「월드가 내려가는 연출」인 물체에 쓴다 |
| `wakeScale` | `1` | 이 물체만의 항적 세기 배율. 전역 `wakeVelocityTransfer`에 곱해진다. 0이면 항적 없음 |

런타임에서는 `WaterInteractor.WakeOnly` / `WakeScale`, 모듈 쪽은 `WaterInteractorModule.WakeOnly` / `WakeScale`로 바꾼다.

### 지금 반응하는 것

| 대상 | 붙은 것 | 바운즈 | `SurfaceInfluence` | 관찰 |
| --- | --- | --- | --- | --- |
| 플레이어 | `LanePlayer`가 `WaterInteractorModule` 직접 부착 | `BoxCollider2D` | 기본(1.0) | 점프·착수는 세로 전달. x가 고정이라 항적은 없다 |
| 장애물 Rock | `Rock.prefab` `WaterInteractor` | 2.44 × 1.10 (로컬 5.6 × 2.52 × 스케일 0.43650794) | 1.0 | **강바닥 바위**라 맨 아랫줄 고정이다. 윗변 −0.55, 수면 2.0까지 2.55라 항적이 나지 않는다(`CollideAll` 세로 조기 탈출). 다른 것들과 형태를 맞추려고 컴포넌트는 남겨 뒀고 비용은 0이다 |
| 장애물 Fish | `Fish.prefab` `WaterInteractor` | 0.90 × 0.45 (로컬 4.1 × 2.05 × 스케일 0.2195122) | 1.0 | 상단 레인에서 윗변 1.325, proximity 0.325 |
| 장애물 Log | `Log.prefab` `WaterInteractor` | 1.80 × 0.976 (로컬 5.31 × 2.8792 × 스케일 0.33898306) | 1.0 | 상단 레인에서 윗변 1.588, proximity 0.588. **셋 중 가장 잘 보인다** |
| 낚싯대 보스 갈고리 | `FishingLineBoss.prefab` `WaterInteractor`(콜라이더 사용) | 콜라이더 | 기본(1.0) | 훑기(가로 이동)에서 항적이 생긴다 |
| 바다코끼리 | `WalrusBoss.prefab` `WaterInteractor`(콜라이더 사용) | 콜라이더 | 기본(1.0) | 돌진 속도가 커서 항적이 세다 |
| 폭포 보스 | `WaterfallBoss.prefab` `WaterInteractor` ×7 (`Rapid0/1/2`·`Rock0/1/2`·`Waterfall`, `ignoreColliderShape`) | 로컬 0.64 × 0.64 × 스케일 (급류 3.2 × 0.9 / 바위 0.7 × 0.7 / 폭포 1.5 × 8.0) | 1.0 | 상단 `Rapid0` proximity 0.55, 상단 `Rock0` 0.45, `Waterfall`은 윗변 4.0이라 proximity 1. 중·하단 급류·바위는 반응 없음(정상). 예고 띠(`Telegraph/Lane0..2`)와 `Band0..2`에는 붙이지 않았다 |
| 상류 단차 | `LedgeSet.prefab` `WaterInteractor` ×6 (`Cascade` ×3 · `Foam` ×3, `ignoreColliderShape`, **둘 다 `wakeOnly`**) | `Cascade` **1.2 × 1.0, offset (0, 2.35)** → 월드 y 1.5~2.5 · `Foam` 2.4 × 0.9 → 월드 y 1.55~2.45 | 1.0 | 둘 다 **항적만** 낸다. `wakeScale`은 `Cascade` **0.5**, `Foam` **0.3**이라 노드 속도 상한이 0.30 / 0.18로 상단 레인 Log(0.6)보다 약하다. 「물이 쏟아져 들어오는」 정도이지 폭풍이 아니다. `IsHoldingWorld`로 세계가 멈추면 항적도 멈춘다. `UpperBed`(30 × 2.7)에는 붙이지 않았다 |

중간·하단 레인(Fish·Log 기준 윗변 0.225 / −0.875)은 수면 2.0에서 1.775 / 2.875 떨어져 있어 `SurfaceInfluence` 1.0을 넘는다. **항적이 안 나는 게 정상**이고, `CollideAll`의 세로 조기 탈출에서 노드 루프 자체를 건너뛴다. 돌은 맨 아랫줄 전용이라 항상 여기에 해당한다.

### 조정

에디터에서 `WaterSettings.asset`을 보며 만진다. 상단 레인 Rock이 기준이다.

- **항적이 안 보인다** → `wakeVelocityTransfer`를 0.15 → 0.25~0.35로 올린다. 노드 속도 상한이 비례해서 커진다(스크롤 4.0 기준 0.6 → 1.0~1.4).
- **상단 레인만 더 세게** → `wakeInfluenceDistance`를 1.0 → 1.4로 올린다. proximity가 0.325 → 0.518로 오른다. 1.8을 넘기면 **중간 레인(1.775)까지 반응하기 시작하므로** 그 전에 멈춘다.
- **개별 물체만 세게** → 그 프리팹 `WaterInteractor.surfaceInfluence`를 올린다. 전역 값은 그대로 둔다.
- **수면이 부글거린다** → `wakeVelocityTransfer`를 내리거나 `damping`(0.6)을 올린다. 클램프 때문에 발산하지는 않지만 값이 크면 상시 물결이 된다.
- **한 물체만 약하게** → 그 프리팹 `WaterInteractor.wakeScale`을 내린다(단차 낙수 0.5, 거품 0.3이 지금 값).
- **어떤 물체가 수면을 터뜨린다** → 먼저 그 물체가 세로로 「연출 때문에」 움직이는지 본다. 그렇다면 `wakeOnly`를 켠다. 그래도 크면 `maxInjectedVelocity`(1.5)를 내린다.
- **물체가 나타나는 순간 수면이 터진다** → `injectionWarmupFrames`(3)를 5~6으로 올리거나 `teleportDistance`(1.0)를 낮춘다. 반대로 **빠른 물체의 첫 항적이 늦게 뜨면** 워밍업을 1~2로 내린다.
- **캡 1.5는 플레이어 착수 첨벙도 조금 깎는다.** 점프는 `y = baseY + 4H·t(1−t)`(H 2.2, 1.6초)라 수면 2.0을 내려오며 지날 때(t ≈ 0.885) 속도가 −4.24 u/s이고 주입값은 `× collisionVelocityTransfer 0.5` = −2.12다. 캡 1.5가 이것을 −1.5로 자른다(약 29% 약해진다). 착수가 밋밋하면 **`maxInjectedVelocity`를 2.5로 올린다** — 그래도 단차 하강(−1.18)은 이미 `wakeOnly`로 막혀 있어 이 버그는 돌아오지 않는다.
- **항적 클램프는 위치가 아니라 속도만 막는다.** 한 노드 위에 오래 머무는 물체는 노드 속도가 상한에 붙은 채 유지되므로 위치가 계속 내려가 골이 깊어진다. 지나가는 물체(장애물 0.45초, 단차 0.3~0.6초)는 문제가 없지만 **정지한 물체에 항적을 붙이면 안 된다**. 단차는 막힘 중에 `vx = 0`이 되어 저절로 항적이 꺼진다.
- **뱃머리 파형을 바꾸고 싶다** → `WaterWake.BowFront`(0.25, 들리는 구간 비율)·`BowLift`(0.35, 들리는 세기)가 상수다. 테스트가 두 값을 참조하므로 바꿔도 테스트는 통과한다.

### 세로 오프셋과의 관계

`EnvironmentThing.SetVerticalOffset`이 물을 내려도 **샘플러는 물체 자기 `transform.position`만 읽으므로** 물의 이동이 물체 속도로 오해되지 않는다. 대신 수면이 내려가면 노드 y가 바운즈 윗변에 가까워져 proximity가 커진다(단차 0.35에서 상단 레인 Rock proximity 0.325 → 0.675). 물리적으로 맞는 방향이라 그대로 뒀다.

#### 층이 바뀔 때 수면이 요동치던 버그 (2026-09-06 수정)

증상은 「단차를 넘어 월드가 내려가는 0.7초 동안 수면이 폭발한다」였다. 원인은 물이 움직이는 것이 **아니었다**.

**원인 1 (주범). 단차 조각의 하강 속도가 착수 첨벙으로 주입됐다.**

`LedgeThing.Advance`가 `scroll.Place(FrontX, -stepHeight × Ease(RiseProgress01))`로 조각을 0.7초 동안 1.1 내린다. `Ease`는 SmoothStep이고 그 최대 기울기는 1.5/duration이므로 조각의 세로 속도는 **최대 −1.1 × 1.5 / 0.7 = −2.36 u/s**다. `Cascade`(옛 바운즈 y −3.8~3.1)와 `Foam`(y 1.55~2.45)은 둘 다 수면 2.0을 세로로 품고 있어 `OverlapPoint`가 그 아래 모든 노드에서 참이었고, `WaterSystem.Collide_Internal`의 착수 갈래가 노드 속도를 매 프레임 `−2.36 × 0.5 × (1 − |py|)` ≈ **−1.18로 덮어썼다**. 노드가 0.7초 내내 아래로 끌려 내려가 `|py|`가 `surfaceCollisionDistance`(1.0)에 닿는 순간 갈래가 풀리고, 그때 `tension`(10)이 한꺼번에 되밀어 `spread`(5)로 옆까지 번진다. 이것이 요동이다.

**원인 2 (거들었다). 낙수 띠가 프로젝트에서 가장 센 항적이었다.** `Cascade`가 세로 6.9라 노드가 항상 바운즈 안(proximity 1)이었고 `|vx| = 4`, 상한이 `4 × 0.15 = 0.6`으로 상단 레인 Log와 같은 값이 폭 1.2 전 구간에 걸렸다.

**아니었던 것 (확인함).**

- **노드 재매핑이 아니다.** `WaterSystem.WaterNodeMappingData.Add`가 `y = water.bounds.yMax − simulationCenter.y`로 범위를 다시 잡지만, `simulationDistance` 20 / 카메라 y 0 / 수면 2.0 → 1.65에서 `√(400 − y²)`는 19.90 → 19.93이고, `Water.OuterIndexRange`가 `[0, maxNodeCount]`(물 폭 16, `nodePerUnit` 3 → 49)로 클램프하므로 범위는 **오프셋 전후 모두 `[0, 49)`로 같다.** 설령 바뀌어도 `SimulationData.Update`가 노드 인덱스 기준으로 값을 옮긴다.
- **`positions[]`는 물이 세로로 움직여도 그대로다.** `Water.GetSurface`가 `yMax + positions[i]`로 읽으므로 변위는 언제나 수면 상대값이다. 가로 매핑(`IndexToX = index / nodePerUnit + bounds.xMin`)은 `transform.position.x`만 쓰는데 오프셋은 y만 건드린다.
- **오프셋에 실려 움직이는 상호작용체는 없다.** `CacheOffsetRoots`가 옮기는 7개(구름·산·언덕·지면·고향·`WaterSurface`·`WaterOverlay`) 중 `WaterInteractor`가 붙은 것은 하나도 없다(`Environment.prefab`·`Play.unity`에서 `WaterInteractor` GUID 검색 결과 0). 단차는 `Environment`의 자식이 아니라 별도 루트라 오프셋이 아니라 자기 모듈로 내려간다.

**고친 방법.** 단차의 낙수 띠·거품을 `wakeOnly`로 돌려 착수 갈래를 끄고(원인 1), `wakeScale`로 항적을 낮추고 낙수 띠 바운즈를 수면 근처 1.0 띠로 좁혔다(원인 2). 마지막으로 어떤 물체든 한 프레임에 넣을 수 있는 속도를 `maxInjectedVelocity`(1.5)로 캡했다.

### 풀링

장애물은 `PoolManager`가 최대 15개 안팎을 돌려 쓴다. 순간이동이 속도로 잡히면 스폰할 때마다 수면이 터진다. 세 겹으로 막는다.

1. `PoolManager.Spawn`이 위치를 먼저 넣고 `SetActive(true)` → `WaterInteractor.OnEnable`이 `ResetVelocity`.
2. `WaterInteractor : IPooledObject`의 `OnSpawned`/`OnReleased`도 `ResetVelocity`.
3. `WaterInteractorModule.OnAttached`가 부착 즉시 `ResetVelocity`.

`ResetVelocity`는 `VelocitySampler.Reset(position)`이라 다음 프레임 속도가 0이 아니라 정상 델타가 된다. **2026-09-06부터 `ResetVelocity`는 워밍업까지 건다**(위 「등장 워밍업」). 세 겹은 그대로 두었다 — 셋 다 결국 `ResetVelocity`로 모이지만 순서 보장을 위해 남긴다.

### 비용

`CollideAll`은 물 한 장에 대해 (1) x 범위 겹침 (2) 세로 도달 거리 두 번 조기 탈출하고, 통과하면 `InnerIndexRange`가 고른 노드만 돈다. `nodePerUnit` 3이라 Rock 4개, Log 6개 노드다. 활성 장애물 15개면 프레임당 노드 방문이 100회 미만이다. `Collide_Internal`이 `List.IndexOf`를 세 번 하던 것을 인덱스 한 번으로 줄였고, 노드가 0개면 `NativeArray` 서브어레이를 만들기 전에 빠진다.

물 깊이가 0.8 → 6.5로 늘어 `PolygonCollider2D`(트리거) 범위도 커졌다. 이 트리거를 읽는 코드는 없다(`Water`는 자기 콜라이더를 `SetPath`로 갱신만 하고, 충돌 판정은 `IWaterCollider.OverlapPoint`가 각자 콜라이더로 한다). 노드 수는 `size.x`만 쓰므로 그대로 49개다.

## 통합 (코어루프)

씬 배치는 이미 끝나 있고 **다시 생성하지 않는다**.

1. Play 씬 루트(ScrollRoot 밖, 원점)에 `Environment.prefab` 인스턴스가 있다. 오버라이드는 이름과 Transform뿐이라 프리팹 수정이 그대로 반영된다.
2. `PlayFlow.SetState`가 `environment.SetScrolling(next == Running || next == Boss)`. 보스 중에도 물은 흐른다(3절).
3. `RunSectionAsync`가 `environment.SetHomeland(section >= SectionCount)`.
4. 단차(`Game.Ledge`)가 `SetVerticalOffset` / `RaiseWorldAsync`를 쓴다.
5. 점프·착수 첨벙은 `WaterInteractorModule`이 한다. `PlayFlow.Jumped/Landed`는 효과음만 낸다.

## 임시값

- 층 스케일(0.28 / 0.30 / 0.34 / 0.38)과 루트 y: 기획서에 없다. 물 위 띠 1.6 unit 안에서 수평선이 하나로 모이도록 화면 구성에서 정했다.
- 시차(0.10 / 0.30 / 0.60 / 1.00): 사용자 지시.
- 틴트: 대기 원근용. 산이 가장 어둡고 푸르며(0.82, 0.87, 0.95) 지면이 흰색이다. 구름 알파 0.90.
- `overlayAlpha` 0.15.
- 물 깊이 6.5, `splashRadius` 0.55, `splashMaxDepth` 0.6.
- `Env_Underwater.png` 색: 위 (92,176,198) → 중간 (36,106,142) → 아래 (7,26,52). 아트가 나오면 PNG만 교체한다.
- Homeland는 아직 임시 텍스처(`Env_Homeland.png`)다. 불투명 물 뒤판 앞(-4)에 있어 수면 아래 강바닥 소품으로 그대로 읽힌다.
- 강바닥 소스 `Env_Riverbed.png` 색: 위 (228,207,168) → (182,155,113) → (104,86,64) → 아래 (48,40,33). 잔물결 주기 23 / 37 / 14 px. 아트가 나오면 PNG만 교체한다(크기·평균 윗선 20 px 규칙만 지키면 프리팹은 그대로다).
- 강바닥 타일 폭 8, 세로 5.0, 루트 y -3.95. 캔버스 400×500은 「가로 2회 · 세로 1회 반복」이 되는 값으로 정한 것이다.
- `scrollScale` 0.5와 강바닥 `speedScale` 2.0: 사용자 지시(배경 절반 속도) + 장애물 속도(4 u/s) 맞춤.

## 확신 없는 지점

- **GroundLayer 나무 잘림.** 물 위 띠가 1.6 unit이라 스케일 0.38의 나무(2.0 unit)는 화면 위에서 0.9 잘린다. 사용자가 에디터에서 보고 루트 y나 스케일을 조정할 자리다. 위 「원근 (층 표)」에 바꾸는 법을 적었다.
- **`m_StretchUV` 채움.** 패키지 소스(`SpriteShapeGenerator.cs` 1370~1381행)를 읽고 `(pos − min)/ext` 매핑을 확인했지만 실행해 보지는 않았다. 그라데이션이 뒤집혀 보이면 `Env_Underwater.png`의 위아래를 뒤집는다.
- **물까지 세로 오프셋을 따라가게 한 것.** 시뮬레이션 수면이 같이 움직인다. `environmentRiseAmount`(0.35)에서는 문제없다고 봤지만 값을 올리면 상단 레인이 물 밖으로 나온다.
- **앞판 알파 0.15.** 실기에서 장애물·보스 실루엣이 흐려지면 낮춘다. `SetOverlayAlpha`로 런타임에서도 바꿀 수 있다.
- **오버드로.** 물 앞판이 화면 전체를 한 번 더 그린다(반투명). 자연 층 타일 16장은 대부분 화면 밖이지만 뒤판 아래는 전부 가려지므로 자연 층을 컬링하지는 않았다. 실기 프로파일은 하지 않았다.
- **텔레그래프 16.** 앞판(15)보다 위다. 나중에 앞판보다 위에 놓을 UI성 월드 오브젝트가 더 생기면 이 대역(16 이상)을 쓴다.
- 물속 배경을 "1~2종 반복"(22절)으로 읽었는데 지금은 그라데이션 한 장뿐이다. 아트 요청을 `Docs/Requests.md`에 남겼다.
- **강바닥 윗선 -1.65.** 기획서에 강바닥 높이가 없다. 맨 아랫줄 레인 띠의 아랫변이자 `Rock`의 아랫변이라는 이유로 정했다. 돌이 모래에 살짝 박힌 그림을 원하면 강바닥 루트 y를 올리고(예: -3.90), 떠 보이면 내린다.
- **강바닥이 세로 오프셋을 따라가는 것.** 위 「공개 API」 아래 문단을 본다. 단차 상승 0.7초 동안 윗선이 -2.0으로 내려가지만 그 구간은 단차 조각의 `UpperBed`가 화면을 덮고 있어 거의 보이지 않는다. 에디터에서 확인할 항목이다.
- **`scrollScale` 0.5.** 배경이 장애물보다 느리게 흐르면 "물살이 느려진" 느낌이 난다. 기획서 3절은 "물 흐름은 항상 왼쪽이고 구간이 바뀌어도 물살이 변하지 않는다"만 말하고 속도값이 없어 사용자 지시를 그대로 넣었다. 물살 자체를 느리게 하려는 것이라면 `GameConfig.scrollSpeed`를 내리는 쪽이 맞다.
- **단차 조각이 퇴장할 때 모래 무늬가 한 번 튈 수 있다.** `LedgeThing`은 앞면 x가 -20이 되면 조각을 통째로 비활성화하는데, 그때 `UpperBed`(폭 30)는 아직 화면을 덮고 있다. 사라지는 순간 그 자리를 환경 강바닥이 이어받는다. 높이·소스가 같아 선은 안 튀지만 타일 위상이 달라 무늬만 순간 바뀐다. 눈에 띄면 `LedgeData.retireX`를 -20 → -40쯤으로 내려 화면 밖에서 끄면 된다.

## 검증

| 단계 | 방법 | 결과 |
| --- | --- | --- |
| 컴파일 | 오프라인 `csc` 전 어셈블리 빌드 | `Game.Environment` 포함 35개 성공. 실패 1개(`Game.Spawner.Tests`)는 이 작업과 무관한 기존 오류(`ObstacleSpec` 생성자 인자) |
| 프리팹 YAML | 스크립트 검사 | 오브젝트 86개, fileID 중복 0, `m_Father`↔`m_Children` 왕복 일치, 컴포넌트 ↔ GameObject 역참조 일치, 참조 GUID 전부 해석(패키지 2개 제외) |
| 미참조 에셋 | GUID 전체 검색 | 지운 PNG 7개 참조 0 확인 후 삭제 |

배치 모드 실행은 하지 않았다(에디터가 열려 있다). 화면은 사용자가 에디터에서 확인한다.

### 강바닥 패스 (2026-09-06)

| 단계 | 방법 | 결과 |
| --- | --- | --- |
| 컴파일 | `python <scratchpad>/csccheck_all.py` | `assemblies compiled: 36  failed: 0` |
| 프리팹 YAML | 스크립트 검사 | `Environment.prefab` 오브젝트 100개, fileID 중복 0, `m_Father`↔`m_Children` 왕복 일치, 컴포넌트 ↔ GameObject 역참조 일치 |
| 텍스처 | 생성 로그 | 윗선 평균 20.000 px, 최소 13.96 / 최대 28.00(= 평균 ±8 px 안), 좌우 끝 위상 연속 |
| 씬 | `Play.unity` | **손대지 않았다.** `LaneGuides`는 -5 그대로다(위 「원근 (층 표)」 아래 문단) |

에디터에서 확인할 것: 맨 아랫줄 돌이 모래 위에 얹혀 보이는지, 단차를 넘은 뒤 위쪽 강바닥이 환경 강바닥과 이어지는지, 상승 0.35 dip 동안 바닥선 아래로 물이 보이지 않는지, 모래가 장애물과 같은 속도(4 u/s)로 흐르는지, 나머지 배경이 절반 속도로 흐르는지.

## 물 깊이 셰이더 (2026-09-06)

물 뒤판(`WaterSurface`)의 채움 슬롯(머티리얼 배열 2번째, 1번째는 가장자리)은 `Assets/GameAssets/Environment/WaterDepth.mat`(셰이더 `Assets/Shaders/WaterDepth.shader`, `Team1004/WaterDepth`)을 쓴다. URP 2D `Sprite-Unlit-Default`를 바탕으로, 정점의 로컬 y(수면 = 0, 아래로 음수)를 깊이로 보고 `lerp(_ShallowColor, _DeepColor, pow(saturate(-y / _Depth), _Falloff))`로 색을 만든다. 파도·단차 오프셋은 오브젝트가 통째로 움직이므로 그라데이션이 항상 수면에서 시작한다. `Env_Underwater.png`는 삭제했고 채움·형태 텍스처는 흰색 `Env_Overlay.png`다.

| 프로퍼티 | 기본값 | 뜻 |
| --- | --- | --- |
| `_ShallowColor` | (0.36, 0.72, 0.92) | 수면 바로 아래 색 |
| `_DeepColor` | (0.03, 0.14, 0.32) | 깊이 `_Depth`에서의 색 |
| `_Depth` | 6.5 | 폴리곤 깊이와 같게 둔다 |
| `_Falloff` | 1.3 | 1보다 크면 얕은 색이 오래 남고 아래에서 급히 어두워진다 |

조정은 머티리얼 인스펙터에서 한다. 앞판(`WaterOverlay`)은 그대로 `Sprite-Lit-Default` + 렌더러 색이다.

