# 환경 (Game.Environment)

무한 스크롤 배경과 수면이다. 스테이지 길이만큼 맵을 깔지 않고 타일 몇 장을 순환시킨다(사용자 결정). 기획서 3판 `Team1004/Assets/Documents/정리 1번/3번 정리.md` 3절(물 표현), 11절 구간 4(고향 풍경), 22절(단순 그래픽, 물속 배경 1~2종 반복)을 반영했다.

핵심 규칙은 3절이다. **물 흐름은 항상 왼쪽이고 구간이 바뀌어도 물살·파도가 변하지 않는다.** 그래서 이 모듈에는 구간별 세기 조절 API가 없다. 폭포 보스의 물줄기는 `Game.Boss`가 따로 그린다.

## 파일

| asmdef | 경로 | 타입 | 책임 |
| --- | --- | --- | --- |
| `Game.Environment` | `Assets/Scripts/Environment/` | `EnvironmentThing : MonoThing` | 프리팹 루트 호스트. 모듈을 붙이고 공개 API를 위임한다 |
| | | `EnvironmentLayer` | 층 하나의 직렬화 묶음(루트 Transform, 타일 폭, 속도 배율). `CreateRunner`가 자식들을 모아 `TileStripRunner`를 만든다 |
| | | `TileStrip` | 순환 계산 순수 함수. 할당·Unity 오브젝트 없음. 테스트 대상 |
| | | `TileStripRunner` | `Transform[]`에 `TileStrip` 결과를 적는다. 매 프레임 할당 없음 |
| | | `ParallaxLayerModule : Module` | `Ticks = Update`. 층 하나를 `Speed × SpeedScale`로 왼쪽으로 민다 |
| | | `WaterFlowModule : Module` | `Ticks = Update`. 수면 아래 물결 타일. 정지 중에도 `BaseFlowSpeed`만큼 계속 흐른다 |
| | | `HomelandModule : Module` | `Ticks = Update`. 고향 소품 그룹 표시·순환 |
| | | `WaterSplashModule : Module` | `Ticks = None`. 수면 노드를 직접 눌러 첨벙을 만든다 |
| `Game.Environment.Editor` | `Assets/Scripts/Environment/Editor/` | `EnvironmentSetup` | 생성기 메뉴. 텍스처 → SpriteShape 프로파일 → 프리팹 |
| | | `EnvironmentTextures` | PNG 생성과 임포트 설정 |
| | | `EnvironmentPatterns` | 임시 텍스처 무늬 함수(가로로 이음매 없음) |
| `Game.Environment.Tests` | `Assets/Scripts/Environment/Tests/` | `TileStripTests` | EditMode 10개. 순환 계산 검증 |

asmdef 참조: `Game.Environment` → `Core.Modules`, `Core.Foundation`, `Game.Config`, `Game.Water`, `Unity.2D.SpriteShape.Runtime`. **`Game.Play`는 참조하지 않는다.** 순환 없음.

## 오브젝트 트리 (`Assets/GameAssets/Environment/Environment.prefab`)

```text
Environment (EnvironmentThing)
  Sky                     단일 사각형, 순환 없음
  FarLayer   / Tile0..3
  MidLayer   / Tile0..3
  NearLayer  / Tile0..3
  Homeland   / Tile0..3   비활성. SetHomeland(true)로 켠다
  FlowLayer  / Tile0..4
  WaterSurface            Game.Water의 Water + SpriteShapeController + PolygonCollider2D
  CalmOverlay             비활성. SetCalm(true)로 켠다
```

층 타일은 `SpriteRenderer.drawMode = Tiled`(`SpriteTileMode.Continuous`)라 `size`만 주면 무늬가 반복된다. 임시 텍스처는 `spriteMeshType = FullRect`, Wrap Repeat으로 임포트한다(Tiled가 요구한다). Sky와 CalmOverlay만 `Simple` + `localScale`이다.

## 층 표

카메라 ortho 3.6, 16:9, 화면 x ±6.4, y ±3.6. 수면 y는 `GameConfig.Current.WaterSurfaceY`(2.0)이고 아래 y는 그 값 기준으로 생성기가 계산한다.

| 층 | 타일 폭 | 타일 수 | 타일 높이 | 층 y | 속도 배율 | sortingOrder |
| --- | --- | --- | --- | --- | --- | --- |
| Sky (하늘) | — | 1 | 2.6 (2.0 ~ 4.6) | 3.3 | — | -9 |
| FarLayer (원경) | 8 | 4 | 6.6 (-4.6 ~ 2.0) | -1.3 | 0.3 | -8 |
| MidLayer (중경 물결) | 6.4 | 4 | 4.4 | -0.2 | 0.6 | -7 |
| NearLayer (근경 바닥) | 6.4 | 4 | 1.6 (-3.8 ~ -2.2) | -3.0 | 1.0 | -6 |
| Homeland (고향 소품) | 8 | 4 | 1.5 (-3.3 ~ -1.8) | -2.55 | 1.0 | -4 |
| FlowLayer (수면 아래 흐름) | 4.8 | 5 | 0.7 (1.3 ~ 2.0) | 1.65 | 0.8 + 고정 0.6 | -3 |
| WaterSurface (수면) | 폭 16, 깊이 0.8 | — | 1.2 ~ 2.0 | 2.0 | 고정(x 이동 없음) | -2 |
| CalmOverlay | — | 1 | 화면 전체 | 0 | — | -1 |

정렬 순서를 전부 -10보다 크게 잡았다. Play 씬의 기존 `Background`(불투명 청록, -10)를 지우지 않아도 그 앞에 그려진다. `LaneGuides`(-5)는 NearLayer(-6) 뒤, Homeland(-4) 앞에 들어가는데 y가 겹치지 않아 문제되지 않는다. 장애물·플레이어는 기본 0이라 전부 앞이다.

레인 가이드와 싸우지 않도록 중경·흐름 타일의 알파는 0.16~0.22로 낮췄다.

## 공개 API (`EnvironmentThing`)

| 멤버 | 설명 |
| --- | --- |
| `float Speed { get; set; }` | 근경 기준 속도. `Awake`에서 `GameConfig.Current.ScrollSpeed`(4.0)로 시작하고 `GameConfig.Changed`를 구독해 갱신한다. 층별 실제 속도는 `Speed × SpeedScale` |
| `void SetScrolling(bool)` | 모든 층의 이동을 켜고 끈다. `bool IsScrolling` |
| `void SetHomeland(bool)` | 구간 4 소품 그룹을 켜고 끈다. 켤 때 순환 위치를 0으로 되돌린다. `bool IsHomelandVisible` |
| `void SetCalm(bool)` | 옵션. `CalmOverlay`를 켜고 물결 흐름을 `calmTimeScale`(0.6)배로 늦춘다. **원경·중경·근경 속도는 건드리지 않는다**(장애물과 어긋나면 안 된다). `bool IsCalm` |
| `bool Splash(float x, float strength)` | 월드 x에 첨벙을 만든다. 양수 = 아래로 눌림(착수), 음수 = 위로 솟음(도약). 수면이 시뮬레이션 범위 밖이면 `false` |
| `Water Surface { get; }`, `float SurfaceY { get; }` | 수면 오브젝트와 수면 y |

`SetCalm`은 컷신·엔딩 분위기용이며 **구간 난이도에 따라 부르면 안 된다**(3절 위반). 기본은 `false`다.

`Time.timeScale`이 0이면 `Time.deltaTime`이 0이라 모든 층이 멈춘다(일시정지·컷신 정지와 자동으로 맞는다).

## 순환 계산

`TileStrip`은 Unity 오브젝트 없이 계산만 한다.

```text
total   = tileWidth × tileCount
offset  = Wrap(offset + speed × dt, total)     프레임마다 [0, total)로 되감는다
slot_i  = Wrap(i × tileWidth − offset, total)
x_i     = −total/2 + slot_i + tileWidth/2
```

`offset` 하나만 누적하고 매 프레임 `[0, total)`로 되감으므로 오차가 쌓이지 않는다(테스트 `AdvanceDoesNotAccumulateError`: 6000프레임 후 오차 < 0.02). 타일은 왼쪽 끝을 지나는 순간 `total`만큼 오른쪽으로 점프한다.

점프가 화면에서 보이지 않으려면 `total ≥ 2 × tileWidth + 화면 폭`이어야 한다. `TileStrip.MinimumTileCount(tileWidth, 12.8)`이 그 최소 타일 수를 주고 생성기가 그대로 쓴다(폭 8 → 4장, 6.4 → 4장, 4.8 → 5장). 타일 폭을 바꾸면 타일 수도 이 함수로 다시 정한다.

`TileStripRunner.Advance`는 `Transform.localPosition`의 x만 고쳐 쓰며 배열·리스트를 새로 만들지 않는다. 층 자식 목록은 `Awake`에서 한 번만 모은다.

## 물 튀김

`Game.Water`의 `WaterBody`는 `Rigidbody2D.linearVelocityY`를 읽는데 플레이어는 Kinematic이고 `JumpModule`이 `transform.position`을 직접 쓰므로 속도가 항상 0이다. 그래서 물 시뮬레이션이 **Rigidbody 대신 모듈 값을 읽도록** `Game.Water`를 손봤다(사용자 결정). 자세한 것은 아래 「수면 상호작용」.

`EnvironmentThing.Splash(x, strength)`는 남겨 두었다. 컷신처럼 물리 없이 명시적으로 첨벙을 넣을 때 쓰는 연출 API이며, 점프·착수에는 쓰지 않는다(`PlayFlow`는 `Splash`를 부르지 않는다).

- `splashRadius` 0.55: 이 반경 안의 노드만 건드리고 거리에 따라 제곱으로 약해진다.
- `splashMaxDepth` 0.6: 변위 상한. `strength`는 이 값으로 잘린다.

## 수면 상호작용 (`Game.Water.WaterInteractorModule`)

`Game.Water`(가져온 포트, 수정 가능)에 다음을 더했다.

| 타입 | 위치 | 역할 |
| --- | --- | --- |
| `IWaterCollider` | `Assets/Scripts/Water/IWaterCollider.cs` | `Bounds`, `VerticalVelocity`, `OverlapPoint(point)`. 수면이 물체에게 묻는 것 전부 |
| `WaterSystem.Collide(Water, IWaterCollider)` | `WaterSystem.cs` | 기존 `Collide(Water, WaterBody)`를 인터페이스로 바꿨다. `body.bounds`→`Bounds`, `rigidbody.linearVelocityY`→`VerticalVelocity`, `collider.OverlapPoint`→`OverlapPoint`. 수면 노드를 건드렸으면 `true` |
| `WaterSystem.CollideAll(IWaterCollider)` | `WaterSystem.cs` | 시뮬레이션 중인 모든 `Water` 중 x 범위가 겹치고 수면 y가 `surfaceCollisionDistance` 안에 있는 것에 `Collide`를 돌린다 |
| `WaterBody` | `WaterBody.cs` | `IWaterCollider`를 Rigidbody로 구현. 기존 public 필드(`rigidbody`, `collider`, `bounds`)는 그대로라 옛 경로도 컴파일된다 |
| `VerticalVelocitySampler` | `VerticalVelocitySampler.cs` | 순수 C#. `Sample(y, dt)` = Δy/Δt, `Smoothing`(0~0.99)만큼 이전 값과 섞는다. 첫 샘플은 0, `dt ≤ 0`이면 직전 값 유지. EditMode 테스트 7개(`Game.Water.Tests`) |
| `WaterInteractorModule : Module` | `WaterInteractorModule.cs` | `Ticks = Update`. 매 틱 호스트 `transform.position.y`를 샘플러에 넣어 `VerticalVelocity`를 만들고 `WaterSystem.CollideAll(this)`를 부른다. `Bounds`는 형제 `BoxCollider2D`의 `size`/`offset`을 transform에 곱한 상자(물리 동기화와 무관)이고, 다른 `Collider2D`면 그 `bounds`/`OverlapPoint`를 쓴다. `IsTouchingWater`, `ResetVelocity()` |
| `WaterInteractor : MonoThing` | `WaterInteractor.cs` | 범용 호스트. `shape`(Collider2D, 비면 `GetComponent`)와 `size`/`offset`을 직렬화해 `Awake`에서 모듈을 붙인다. 보스 공격체(낚싯바늘, 바다코끼리 몸)에 생성기가 붙인다 |
| `WaterSettings.velocitySmoothing` | `WaterSettings.cs` | 새 필드(0~0.99, 기본 0). 세기는 기존 `collisionVelocityTransfer`(0.5)·`surfaceCollisionDistance`(1)로 `Assets/GameAssets/Water/Resources/WaterSettings.asset`에서 조정한다. 코드 상수 없음 |

`Game.Water` asmdef는 `Core.Modules`를 참조한다(`Module` 기반 클래스). `Game.Environment`와 `Game.Player`가 둘 다 `Game.Water`를 참조하며 순환은 없다.

플레이어: `LanePlayer.Awake`가 형제 `BoxCollider2D`(히트박스)로 `WaterInteractorModule`을 붙인다(`LanePlayer.Water`). `SnapToLane`은 `ResetVelocity()`로 순간이동 속도를 버린다. 점프로 수면(y 2.0)을 뚫고 올라갈 때 약 +5 unit/s, 떨어질 때 −5 unit/s가 노드 속도에 `collisionVelocityTransfer`배로 옮겨져 수면이 올라갔다 내려앉는다. 트리거 물리도 Rigidbody 속도도 필요 없다. 시뮬레이션은 `PostLateUpdate`에서 돌므로 `Update` 틱에서 쓴 속도가 같은 프레임에 반영된다.

PlayMode 검증: `FullLoopTests`가 구간 1에서 상단 레인으로 올라가 점프한 뒤 2초 동안 플레이어 x ±0.4 안 수면 노드의 |변위| 최댓값을 읽어 0.001보다 큰지 확인한다.

## 생성기

| 메뉴 | 메서드 | 동작 |
| --- | --- | --- |
| `Team1004 > Generate Environment Assets` | `Game.Environment.Editor.EnvironmentSetup.Generate()` | 없는 것만 만든다. 프리팹이 있으면 경고만 남기고 건드리지 않는다 |
| `Team1004 > Regenerate Environment Prefab (Overwrite)` | `EnvironmentSetup.RegeneratePrefab()` | 확인 창(배치 모드에서는 건너뜀) 후 `Generate(true)` |
| — | `EnvironmentSetup.Generate(bool overwrite)` | 코어루프 생성기가 부르는 형태 |

`EnvironmentSetup.PrefabPath` 상수로 프리팹 경로를 노출한다.

생성되는 것:

| 경로 | 내용 |
| --- | --- |
| `Assets/GameAssets/Placeholder/Env_Sky.png` | 64×64 하늘 그라데이션. Sprite, PPU 100 |
| `Env_Far.png` | 128×128 진한 청록 + 빛줄기. 가로 이음매 없음 |
| `Env_Mid.png` | 128×64 물결 무늬(알파 0.16) |
| `Env_Near.png` | 128×64 바닥 자갈 실루엣 |
| `Env_Flow.png` | 128×32 가로 물살 줄무늬(알파 0.22) |
| `Env_Homeland.png` | 128×64 자갈 더미 + 수초 3가닥 |
| `Env_Calm.png` | 32×32 옅은 백청색(알파 0.12) |
| `Env_WaterFill.png` | 32×32 반투명 파랑. **Default 텍스처**(Sprite 아님), Wrap Repeat. SpriteShape fill용 |
| `Assets/GameAssets/Environment/WaterSurfaceProfile.asset` | `UnityEngine.U2D.SpriteShape`. `angleRanges` 비어 있고 `fillTexture`만 쓴다(가장자리 스프라이트 없음) |
| `Assets/GameAssets/Environment/Environment.prefab` | 위 트리 |

PNG는 **없을 때만** 만든다. 무늬를 바꾸려면 PNG를 지우고 다시 생성한다. 임포트 설정은 매번 확인해 어긋나면 고친다.

수면의 `SpriteShapeRenderer`에는 URP `Sprite-Lit-Default.mat`을 두 슬롯 모두에 넣는다. Play 씬에 `Global Light 2D`가 있어야 검게 나오지 않는다(코어루프가 이미 둔다).

프리팹의 스플라인·콜라이더는 생성기가 직접 4점(수면 좌·우, 바닥 우·좌)으로 만든다. `Water.OnValidate`에 의존하지 않는다. 런타임에는 `Water.Update`가 매 프레임 다시 만든다.

## 통합 절차 (코어루프 생성기)

**완료.** `CoreLoopSetup`(코어루프 생성기)이 아래를 한다.

1. Play 씬을 만들기 전에 `EnvironmentSetup.Generate(false)`를 부른다(없는 것만. 프리팹을 다시 만들려면 `Team1004 > Regenerate Environment Prefab (Overwrite)`). `Game.Bootstrap.Editor` asmdef가 `Game.Environment`, `Game.Environment.Editor`를 참조한다.
2. `Environment.prefab`을 `PrefabUtility.InstantiatePrefab`으로 Play 씬 루트(ScrollRoot 밖, 원점)에 둔다. 프리팹이 없으면 경고만 남기고 `PlayFlow.environment`는 비어 있다(코어루프는 `null` 가드로 그대로 돈다).
3. 기존 `Background` 사각형은 **지웠다**(Sky가 위쪽, FarLayer가 아래쪽 화면을 전부 덮어 필요 없다). `LaneGuides`(-5)는 **남겼다**: NearLayer(-6)와 Homeland(-4) 사이에 들어가고 중경·흐름 타일 알파가 낮아 옅은 가로선이 그대로 읽힌다.
4. `Game.Play` asmdef가 `Game.Environment`를 참조하고 `PlayFlow`에 `[SerializeField] private EnvironmentThing environment;`가 있다. `SetState`에서 `environment.SetScrolling(next == Running || next == Boss)`. 보스 중에도 물은 흐르고(3절) `StageScroller`만 멈춘다.
5. `RunSectionAsync`가 `environment.SetHomeland(section >= SectionCount)`를 부른다. 재도전으로 앞 구간부터 시작하면 자동으로 꺼진다.
6. 점프·착수 첨벙은 `Splash`가 아니라 위 「수면 상호작용」의 `WaterInteractorModule`이 한다. `PlayFlow.Jumped/Landed`는 효과음만 낸다.
7. `SetCalm`은 아무도 부르지 않는다(엔딩용 선택지).

## 임시값

- 층 y·높이·타일 폭·속도 배율(0.3 / 0.6 / 1.0 / 0.8): 기획서에 없다. 화면 구성에서 정했다.
- `baseFlowSpeed` 0.6: 스크롤이 멈춰도 물이 왼쪽으로 흐르게 하는 최소 속도. 3절 "기본 물 흐름 표현은 항상 왼쪽"을 컷신 중에도 유지하려고 넣었다. 0으로 두면 정지 중 물이 완전히 멈춘다.
- `calmTimeScale` 0.6, `splashRadius` 0.55, `splashMaxDepth` 0.6.
- 수면 깊이 0.8(y 1.2~2.0): 상단 레인 1.1과 겹치지 않는 최소 폭으로 잡았다. 물 전체를 SpriteShape로 채우면 반투명 채움이 배경 층을 덮어 층 구분이 사라진다.
- 색: 원경 진한 청록(0.02,0.13,0.18 → 0.05,0.26,0.33), 바닥 (0.04,0.11,0.14), 수면 채움 반투명 (0.32,0.72,0.82,0.38). 아트 나오면 PNG만 교체한다.
- 임시 텍스처 해상도 128×64~128×128. 8 unit로 늘리면 흐리다. Tiled 반복이라 늘림이 아니라 반복이지만 unit당 픽셀은 여전히 낮다.

## 확신 없는 지점

- **보스 중 물 흐름.** 3절은 물 표현이 변하지 않는다고만 하고 보스전에서 배경이 흐르는지는 말하지 않는다. 기본을 "흐른다"로 두었다(통합 절차 4). 보스전이 제자리 전투처럼 보이길 원하면 한 줄로 바꾼다.
- **`Background` 사각형 처리.** 통합에서 지웠다(위 「통합 절차」 3).
- **`SetCalm` 사용처.** 기획서에 "잔잔한 배경"이라는 요구가 없다. 엔딩용으로 만들어 두었고 아무도 부르지 않으면 그냥 꺼진 채로 있다.
- **수면 시뮬레이션 비용.** `Water.Update`가 매 프레임 스플라인과 콜라이더를 다시 만들고 `BakeMesh`를 부른다. 폭 16 × `nodePerUnit` 3 = 49노드라 가볍다고 봤지만 실기 프로파일은 하지 않았다.
- **점프 첨벙은 `WaterInteractorModule`이 `WaterSystem.CollideAll`로 노드 속도를 쓴다.** 세기는 `WaterSettings.asset`의 `collisionVelocityTransfer`(0.5)·`surfaceCollisionDistance`(1)·`velocitySmoothing`(0)이며 실기에서 조정한다. 플레이어 히트박스 상자(1.33×0.71)로 겹침을 판정하므로 스프라이트보다 조금 안쪽에서 반응한다.
- **`Splash`가 `NativeArray`에 직접 쓰는 것.** `WaterSystem`의 공개 API가 위치 배열의 뷰를 그대로 돌려주기 때문에 가능하다. Job은 `UpdatePhysics` 안에서 동기 완료되므로 안전 핸들 충돌은 없다고 봤다. `Game.Water`가 나중에 비동기 Job으로 바뀌면 깨진다. 그때는 `Game.Water`에 `AddDisplacement(x, radius, amount)` 공개 API를 요청한다.
- **`Splash` 세기.** `-0.35`(도약) / `0.5`(착수)는 컷신에서 쓸 때의 참고값이다. 코어루프는 부르지 않는다.
- 물속 배경을 "1~2종 반복"(22절)으로 읽어 원경·중경 두 종으로 잡았다. 근경 바닥을 별도 종으로 셀지는 애매하다.

## 검증

| 단계 | 명령 | 결과 |
| --- | --- | --- |
| 컴파일 | `Unity.exe -batchmode -nographics -projectPath ... -quit -logFile Logs/batch_env.log` | `error CS` 0건, `Game.Environment{,.Editor,.Tests}.dll` 생성 |
| 생성 | `... -executeMethod Game.Environment.Editor.EnvironmentSetup.Generate -quit` | `[EnvironmentSetup] Environment assets are ready`, 프리팹·프로파일·PNG 8개 |
| EditMode | `... -runTests -testPlatform EditMode -testFilter "Game.Environment.Tests" -testResults Logs/editmode_env.xml` | 10개 전부 통과 |

프리팹 YAML에서 `EnvironmentThing`의 `far/mid/near/flow/homeland`의 `root`, `surface`, `calmOverlay`가 `{fileID: 0}`이 아닌지, 수면의 `m_SortingOrder: -2`와 스플라인 4점, 머티리얼 두 슬롯을 확인했다.

플레이 화면은 씬 배치가 끝난 뒤 사용자가 확인한다. 이 작업에서는 씬을 만들지 않았다.
