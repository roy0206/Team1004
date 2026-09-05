# 장애물 스포너 (Game.Spawner)

3레인 물속 코스에 돌·다른 물고기·통나무 패턴을 놓는 모듈이다. 완전 랜덤이 아니라 검증된 패턴 라이브러리에서 뽑고, 순수 C# 시뮬레이터가 "지금 이 플레이어가 이 패턴을 확실히 피할 수 있는가"를 매번 검사한 뒤에만 스폰한다. 기획서 정본은 4번 분할본 `Team1004/Assets/Documents/정리 1번/4번 정리/05_일반장애물.md`, `06_일반구간_난이도_스폰.md`(구간 시간 확정, 스폰 간격 추천 후보, 미확정 목록), `16_미확정_검토중_아이디어.md` 4항, `17_현재_남은기획_체크리스트.md` B·G이며, 3판 `3번 정리.md` 9~11절·25절과 내용이 같다. 패턴 예시와 점프 필수 안전 규칙은 1판 PDF 13~16절(2판 13~14절과 동일)을 따른다. 3판 3절·25절대로 물살 세기는 난이도 요소가 아니며 스포너는 물 표현을 건드리지 않는다.

**생성기는 얼렸다.** 2026-09-06 기획자 답변 반영 판부터 `SpawnerAssetGenerator`·`SpawnerBalanceReport`를 돌리지 않는다(에디터가 열려 있어 배치 실행을 강제하지 않는다는 규칙). `Design/Spawner/**`의 `.asset`, `GameAssets/Obstacles/*.prefab`, `Play.unity`의 Player 콜라이더는 **YAML을 직접 고쳐** `SpawnerDefaults`와 값을 맞췄고, 밸런스 표는 같은 순수 C# 시뮬레이터를 오프라인 .NET 하네스로 돌려 `Docs/SpawnerBalance.md`에 적었다. 생성기 코드는 컴파일되고 값도 새 규칙에 맞춰 두었지만 이번 판에서 실행하지는 않았다.

컴파일은 오프라인 Roslyn으로 확인했다. 에디터에서 실행 확인은 하지 못했다(「검증하지 못한 위험 지점」 참고).

## 구성

| 경로 | asmdef | 내용 |
| --- | --- | --- |
| `Assets/Scripts/Spawner/Simulation/` | `Game.Spawner` | UnityEngine에 의존하지 않는 순수 C#. 명세(`ObstacleSpec`, `PatternSpec`), 시간축 배치(`PlacementBuilder`, `ObstacleTiming`, `PatternPlacement`), 도달 가능성(`StateLayout`, `StateSet`, `BlockTimeline`, `Reachability`), 패턴 판정(`PatternAnalyzer`, `PatternAnalysis`), 난이도(`DifficultySample`, `SectionProfile`, `DifficultyProfile`), 등장 비율 보정(`ObstacleMix`), 코스 생성(`CourseGenerator`, `CourseRunner`, `CourseResult`, `GenerationStats`, `RejectReason`, `DeterministicRandom`), 기본값(`SpawnerDefaults`) |
| `Assets/Scripts/Spawner/` | `Game.Spawner` | ScriptableObject 데이터(`ObstacleDefinition`, `SpawnPattern`, `PatternLibrary`, `DifficultyCurve`, `ObstacleSpawnerSettings`), 호스트와 모듈(`ObstacleSpawner : MonoThing`, `SpawnModule : Module`, `ObstacleThing : Hazard, IPooledObject`, `ObstacleRuntimeModule : Module`), `SpawnPlayerState`, `ObstacleVisual`(아트 스케일·변종 선택 계산), `Lane`/`LaneFlags`. `ObstacleThing`은 선택적 `swimClip`(`Game.Animation`의 `CustomAnimation`)을 들고 스폰 시 `SpriteAnimatorModule`로 루프 재생한다 |
| `Assets/Scripts/Spawner/Editor/` | `Game.Spawner.Editor` | `SpawnerAssetGenerator`(메뉴 `Team1004/Generate Spawner Assets`, `Regenerate Spawner Assets (Overwrite)`), `SpawnerBalanceReport`(메뉴 `Team1004/Spawner Balance Report`). **현재 얼려 둠 — 돌리지 않는다** |
| `Assets/Scripts/Spawner/Tests/` | `Game.Spawner.Tests` | EditMode 테스트 `PatternAnalyzerTests`, `CourseGeneratorTests`, `ObstacleVisualTests`(돌·통나무·물고기 스케일 계산과 변종 추출) |
| `Assets/GameAssets/Design/Spawner/` | 데이터 | 생성기가 만드는 Design 에셋 |
| `Assets/GameAssets/Obstacles/` | 프리팹 | `Rock.prefab`, `Log.prefab`, `Fish.prefab`(전부 오브젝트 아트). 구판의 `Shark`/`Whale`은 Overwrite 생성 시 삭제된다 |
| `Assets/GameAssets/Art/오브젝트/` | 아트 미러 | `돌1.png`, `돌 2.png`, `통나무.png`, 하위 폴더 `장애물 물고기/`의 `장애물 물고기1.png`·`장애물 물고기2.png`. 읽기 전용 — `.meta`는 `ObjectArtPostprocessor`가 만든다(하위 폴더까지 본다) |

asmdef 참조: `Game.Spawner` → `Core.Foundation`, `Core.Modules`, `Core.Pool`, `Game.Animation`, `Game.Config`, `Game.Player`. `Game.Play`는 참조하지 않는다(PlayFlow가 스포너를 참조하는 방향만 허용해 순환을 막는다).

씬에 놓이는 오브젝트는 전부 `MonoThing` 호스트 + `Module`이다. `ObstacleSpawner`는 호스트이고 로직은 `SpawnModule`(`Ticks = None`)에 있다. 장애물 프리팹 루트는 `ObstacleThing : Game.Player.Hazard`이며 `IPooledObject`를 구현해 `OnSpawned`/`OnReleased`에서 `ClearModules`를 한다. 스폰 직후 `SpawnModule`이 `ObstacleRuntimeModule`(타이밍, 풀 핸들, 자체 속도 이동)을 붙인다. `Hazard`가 `MonoThing`을 상속한 마커 호스트(`public class Hazard : MonoThing`)라는 현재 형태를 전제로 한다. `Hazard`가 `sealed`로 바뀌면 `ObstacleThing`이 컴파일되지 않으므로, 그때는 `ObstacleThing : MonoThing`으로 바꾸고 프리팹에 `Hazard`를 별도 컴포넌트로 붙인다.

## 공개 API

### `ObstacleSpawner : MonoThing`

```csharp
public void Initialize(ObstacleSpawnerSettings settings, Transform scrollRoot, int seed);
public void Initialize(int seed);                       // Inspector에 연결된 settings/scrollRoot 사용
public void BeginSection(int sectionIndex);             // 길이는 GameConfig.Current.StageLength (거리)
public void BeginSection(int sectionIndex, float sectionLength);      // 거리(unit) 기준 진행도
public void BeginSectionByDuration(int sectionIndex, float durationSeconds); // 시간(초) 기준 진행도. 3판 9절 구간 시간용
public void Advance(float deltaTime, float distance, in SpawnPlayerState player);
public void Stop();
public void ReleaseAll();

public bool IsInitialized { get; }
public bool IsRunning { get; }
public bool IsSectionExhausted { get; }                 // 스폰이 끝났고 활성 장애물이 없다
public int ActiveObstacleCount { get; }
public int SectionIndex { get; }                        // 0 = 게임 진행 1
public float SectionTime { get; }                       // BeginSection 이후 누적 deltaTime
public float SectionProgress { get; }                   // 거리 기준: (distance - 시작 distance) / sectionLength, 시간 기준: SectionTime / duration
public IReadOnlyList<PatternAnalysis> Analyses { get; }
public SpawnModule Spawn { get; }
public event Action<PatternPlacement> PatternSpawned;
```

- `sectionIndex`는 0부터다. 기획서의 "게임 진행 1~4"가 0~3이다. `DifficultyCurve`의 `sections` 배열 인덱스와 같다.
- 자체 `Update`가 없다. `Advance`를 부르는 쪽(PlayFlow)이 일시정지·정지를 통제한다. `Advance`는 `IsRunning`일 때만 동작한다.
- `Initialize`는 시뮬레이션 설정을 `GameConfig.Current`에서 읽고, 라이브러리의 모든 패턴을 분석하며, 장애물 프리팹을 `PoolManager`에 등록한다(`IsRegistered`면 건너뜀). 두 번 불러도 된다(재분석·재등록).
- `BeginSection`/`BeginSectionByDuration`은 남아 있는 장애물을 모두 회수하고 코스를 새로 시작한다. 같은 `seed`·같은 `sectionIndex`면 난수열이 같다. 3판은 구간을 시간(25/30/35/15초)으로 정하므로 `BeginSectionByDuration(i, SpawnerDefaults.GetSectionDuration(i))`를 쓴다. 난이도 곡선은 구간 진행도(0~1) 기준이라 거리/시간 어느 쪽이든 같은 모양이다.
- `Stop`은 스폰과 자체 이동을 멈춘다. 장애물은 그대로 남는다(실패 연출용). 치우려면 `ReleaseAll`.
- `OnDisable`과 모듈 분리 시 `ReleaseAll`이 자동으로 돈다.

### `SpawnPlayerState` (readonly struct)

```csharp
public SpawnPlayerState(int lane, bool isAirborne, float lockRemaining, float jumpCooldownRemaining);
public static SpawnPlayerState Grounded(int lane, float moveRemaining, float jumpCooldownRemaining);
public static SpawnPlayerState Airborne(float airborneRemaining, float jumpCooldownRemaining);
public static SpawnPlayerState FromPlayer(LanePlayer player, float jumpCooldownRemaining, float airborneRemaining = -1f, float moveRemaining = -1f);
```

`lane`은 `LanePlayer.CurrentLane`(0 = 상단). `lockRemaining`은 이동 중이면 남은 이동 시간, 공중이면 남은 공중 시간(초). `FromPlayer`는 남은 시간을 모르면(-1) `GameConfig`의 전체 지속 시간을 가정한다. 이 가정은 보수적이다(입력 불가 시간을 실제보다 길게 봄). 쿨타임도 모르면 `JumpCooldown` 전체를 넘기는 것이 안전하다.

### 순수 시뮬레이터 (테스트·에디터 도구용)

```csharp
PatternAnalysis PatternAnalyzer.Analyze(PatternSpec pattern, SimulationConfig config, float speedMultiplier);
List<PatternAnalysis> PatternAnalyzer.AnalyzeAll(IReadOnlyList<PatternSpec>, SimulationConfig, float speedMultiplier);

var generator = new CourseGenerator(config, patterns, analyses, profile, seed);
generator.BeginSection(sectionIndex);
bool generator.TryPlaceNext(float now, float progress, StateSet frontier, out PatternPlacement placement, out RejectReason reason);

var runner = new CourseRunner(config, patterns, analyses, profile);
CourseResult runner.Run(int sectionIndex, float sectionLength, int seed, float stepSeconds = 0.1f);
CourseResult runner.RunSeconds(int sectionIndex, float durationSeconds, int seed, float stepSeconds = 0.1f);

SpawnerDefaults.CreateConfig() / CreateObstacles() / CreatePatterns(obstacles) / CreateProfile() / GetSectionDuration(i)
```

`ObstacleSpawnerSettings.CreateSimulationConfig(GameConfigValues)`, `PatternLibrary.BuildSpecs(List<PatternSpec>)`, `DifficultyCurve.ToProfile()`가 에셋을 순수 타입으로 바꾼다.

## 데이터 (`Assets/GameAssets/Design/Spawner/`)

전부 ScriptableObject이고 `Create > Team1004 > Spawner > ...` 메뉴가 있다. 생성기가 기본값을 채운다.

| 에셋 | 필드 | 의미 |
| --- | --- | --- |
| `Obstacles/<Id>.asset` `ObstacleDefinition` | `id`, `prefab`, `laneSpan`, `allowedStartLanes`(LaneFlags), `bodyLength`(unit), `collisionLength`(unit, 0이면 몸길이), `collisionHeight`(unit, 아트 높이 × 0.87), `speedMultiplier`, `spawnWeight`(등장 비중), `usableInNormalPatterns`, `usableInJumpPatterns` | 장애물 한 종류. `id`가 풀 키다. 3판은 전부 1레인이지만 `laneSpan`은 남겨 두었다(후순위 장애물이 여러 레인을 차지해도 코드 수정 없음). 새 종류(수초, 어망…)는 이 에셋과 프리팹만 추가하고 패턴에 넣으면 된다 |
| `Patterns/<Id>.asset` `SpawnPattern` | `id`, `entries[]`(obstacle, startLane, arrivalOffset 초), `manualJumpRequired`, `enabled` | 패턴 하나. `startLane`은 장애물이 차지하는 레인 중 가장 위. `arrivalOffset`은 패턴의 첫 장애물이 플레이어에 닿는 시각 기준으로 이 항목이 몇 초 뒤에 닿는지. `manualJumpRequired`는 표기일 뿐 판정은 시뮬레이터가 하며 다르면 경고를 남긴다 |
| `PatternLibrary.asset` | `patterns[]` | 사용할 패턴 목록 |
| `DifficultyCurve.asset` | `sections[4]`, 각각 AnimationCurve `speedMultiplier`, `patternGap`, `minReactionMargin`, `jumpRequiredRatio`, `tier1Weight`, `tier2Weight`, `tier3Weight`와 `spawnStopProgress` | 구간별 곡선. x는 구간 진행도 0~1 |
| `ObstacleSpawnerSettings.asset` | `library`, `difficulty`, `screenRightX` 6.4, `screenLeftX` -6.4, `playerHalfWidth` 0.46, `tickSeconds` 0.05, `safetyPadding` 0.1, `minJumpPatternGap` 2, `sectionStartGrace` 2, `retryInterval` 0.2, `maxCandidateAttempts` 4, `despawnMargin` 1, `prewarmPerObstacle` 4, `defaultSeed` 12345 | 스포너 설정. 레인 y, 플레이어 x, 스크롤 속도, 이동·점프·쿨타임은 `GameConfig`에서 읽는다 |

기본 장애물(3판 10절, 수치는 임시값):

| id | 3판 이름 | 레인 수 | 시작 레인 | 몸길이 | 충돌 길이 | 충돌 높이 | 속도 배율 | 등장 비중 | 의도 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Rock | 돌(바위) | 1 | **하단만** | 2.44 | 2.13 | 0.96 | 1.0 (물살과 같이 흐름) | 5 | 기본 회피. **강바닥에 얹힌 바위**라 맨 아랫줄을 높이까지 꽉 채운다 |
| Fish | 다른 물고기 | 1 | 상/중/하 | 0.9 | 0.78 | 0.39 | 1.25 (자체 속도로 연어 반대 방향으로 지나감) | 3 | 반응 시간이 짧음 |
| Log | 통나무 | 1 | 상/중/하 | 1.8 | 1.566 | 0.85 | 0.8 (느리게 떠내려옴) | 2 | 레인을 오래(약 0.78초) 막음 |

기획 4판이 세 장애물의 상대값을 돌 기준으로 확정했다(돌 속도 1.0, 물고기 속도 1.25·길이 0.9, 통나무 속도 0.8·길이 1.8). 3판의 임시값(물고기 1.4/1.2/x1.8, 통나무 3.0/2.6/x1.0)은 이 표로 대체되었다.

### 돌은 높이 기준으로 맞춘다 (2026-09-06 사용자 결정)

**돌은 강바닥에 얹힌 바위이고 맨 아랫줄을 통째로 메운다.** 그래서 다른 둘과 스케일 기준이 다르다.

| | 물고기 · 통나무 | 돌 |
| --- | --- | --- |
| 맞추는 축 | **길이** (`ObstacleFitAxis.Length`) | **높이** (`ObstacleFitAxis.Height`) |
| 스케일 | `몸길이 / 알파가로` | `visualHeight / 알파세로` |
| `bodyLength`의 뜻 | **입력**. 여기서 스케일이 나온다 | **결과**. 높이를 맞춘 뒤 나온 가로다 |
| `visualHeight` | 0 (안 쓴다) | 1.1 = 레인 높이 |

`id`로 특수 분기하지 않는다. `ObstacleSpec.FitAxis`·`VisualHeight`(→ `ObstacleDefinition`의 직렬화 필드 `fitAxis`·`visualHeight`)가 정하고 `ObstacleVisual.UniformScale(spec, 알파가로, 알파세로)` 하나가 두 경우를 모두 처리한다. `fitAxis`가 `Height`인데 `visualHeight`가 0 이하면 `ObstacleSpec` 생성자가 조용히 `Length`로 되돌린다(에셋에 키가 없는 예전 데이터 = `Length` = 기존 동작).

돌의 수치는 전부 알파 박스 560 × 252 px 하나에서 나온다.

```text
스케일       = 1.1 / (252/100)        = 0.4365079
화면 크기    = (560/100) × 0.4365079  = 2.4444 × 1.1      → bodyLength 2.44 (반올림)
콜라이더     = 2.4444 × 0.87, 1.1 × 0.87 = 2.1267 × 0.957 → 2.13 × 0.96 (반올림)
콜라이더 로컬 = 2.13 / 0.4365079, 0.96 / 0.4365079        = 4.8796363 × 2.1992726
```

**세로 위치.** 맨 아랫줄 중심은 y −1.1이고 스포너는 `LaneCenterY`로 거기에 놓는다. pivot이 알파 박스 중심이라 그림 중심도 −1.1이고, 높이가 정확히 레인 높이 1.1이므로 **바닥 −1.65 = 맨 아랫줄 아래 경계, 윗면 −0.55 = 위 경계**가 저절로 맞는다. 그래서 `SpriteRenderer`·`BoxCollider2D` 모두 `offset`이 (0, 0)이다(`ObstacleVisualTests.Rock_SitsOnTheBottomLaneFloor`가 확인).

**밸런스에 준 영향.** 몸길이가 1.0 → 2.44로 늘어 봉쇄 시간이 4 u/s에서 0.61초 늘었다(`Rock_Bottom`의 「막는 시간」 0.45 → 0.76초). 반응 여유는 **한 패턴도 바뀌지 않았다**(돌은 맨 아랫줄 전용이라 위·중간 줄이 항상 비어 있고, 반응 여유는 마지막으로 비는 줄까지 가는 시간이 정한다). 오프라인 하네스에서 네 구간 × 시드 20개 전부 생존했고 요구 반응 여유 1.3 / 1.0 / 0.8 / 1.3도 그대로 지켰다. 패턴 간격은 손대지 않았다. 숫자는 `Docs/SpawnerBalance.md`.

**히트박스(2026-09-06 기획자 답변 1·3 반영).** 4번 기획서 17절 G가 "히트박스는 스프라이트보다 10~15% 작게"이므로 가로세로 모두 **아트 실측치 × 0.87**(`ObstacleVisual.HitboxScale`)이다.

- 가로(`collisionLength`) = 보이는 가로 × 0.87 (돌 2.13, 물고기 0.78 = 0.867배, 통나무 1.566). 테스트가 0.85~0.90 범위를 지킨다.
- 세로(`collisionHeight`) = **화면에 보이는 아트 높이 × 0.87**. 예전처럼 레인 폭에서 0.3을 빼던 고정값 0.8이 아니다. 물고기는 보이는 높이가 0.45라 0.3915 ≈ **0.39**, 통나무는 0.976이라 0.849 ≈ **0.85**, 돌은 레인 높이 1.1을 채우므로 0.957 ≈ **0.96**이다. `ObstacleVisual.VisualWorldSize(spec, 알파 가로, 알파 세로)`에 0.87을 곱한 값이고, `ObstacleDefinition.collisionHeight`가 정본이며, 프리팹 생성기는 데이터를 쓰되 아트에서 나온 값과 0.02 이상 어긋나면 경고를 남긴다.
- 이웃 레인 오염은 고정값 대신 계산으로 막는다. 레인 간격 1.1, 플레이어 히트박스 세로 0.71이므로 장애물 세로가 1.49 미만이면 이웃 레인의 플레이어에 닿을 수 없다(`ObstacleSpec.ReachesNeighbourLane`, 테스트가 세 장애물 모두 확인). **레인을 꽉 채우는 돌(0.96)도 여유가 0.53 남는다**((0.96 + 0.71)/2 = 0.835 < 1.1).

**등장 레인 제한.** 돌은 `allowedStartLanes = Bottom`(마스크 4)이라 맨 아랫줄에만 나온다. 물고기와 통나무는 세 줄 모두 쓴다. `PatternSpec.Validate`가 규칙을 어긴 패턴을 해결 불가로 판정하므로 라이브러리에 잘못된 배치가 들어가면 스폰되지 않고 경고가 남는다.

4번 기획서 05절에서 미확정이던 세 항목(장애물별 등장 확률, 조합 패턴, 등장 레인 제한)은 2026-09-06 기획자 답변으로 확정되어 위 표와 아래 「등장 비율」·패턴 목록에 반영했다. 앞으로 바뀌면 코드가 아니라 `ObstacleDefinition`·`SpawnPattern`·`DifficultyCurve` 에셋만 고친다.

### 등장 비율 5 : 3 : 2 (`ObstacleMix`)

기획자 답변 3의 "돌 5 : 물고기 3 : 통나무 2"는 **장애물 개수 기준**이다. 패턴 안에서 종류를 뽑는 구조가 아니라 패턴마다 종류가 고정되어 있으므로, 비율은 패턴 추첨 가중치로 맞춘다.

- `ObstacleDefinition.spawnWeight`(돌 5, 물고기 3, 통나무 2)를 정규화해 목표 점유율 0.5 / 0.3 / 0.2를 만든다.
- `ObstacleMix`가 구간 안에서 종류별 **빚**(`목표 점유율 × 지금까지 나온 총 개수 − 그 종류가 나온 개수`)을 들고 있다. 패턴 후보의 가중치에 `exp(Strength × 패턴 안 항목들의 빚 합)`을 곱한다(`Strength` 1, 배율은 0.02~24로 자른다).
- 빚은 비율이 아니라 개수 오차라서 적분기처럼 동작한다. 한 종류가 계속 모자라면 빚이 커져 그 종류가 뽑힐 때까지 가중치가 계속 오르므로, 티어 가중치가 어떻게 기울어도 장기 비율이 목표에서 멀어지지 않는다.
- `BeginSection`마다 초기화한다. 구간 첫 패턴은 빚이 0이라 보정이 없다.
- 점프 필수 후보에는 보정을 걸지 않는다(어차피 비율 0).

오프라인 20시드 결과는 돌 47.8% / 물고기 31.9% / 통나무 20.3%다(구간별 표는 `Docs/SpawnerBalance.md`). 구간 4는 패턴이 4개뿐이라 표본이 작아 43.2 / 34.8 / 22.0으로 흔들린다. 테스트는 네 구간 13시드 합계에 ±0.07 허용치를 둔다.

### 점프 필수 패턴 (일반 구간에서 쓰지 않는다)

기획 4판은 점프 강제를 보스 3(폭포)만의 것으로 정했다. 그래서 네 구간 전부 `jumpRequiredRatio = 0`이다(`SpawnerDefaults.NormalSectionJumpRatio`). 손잡이 자체는 남겨 두어 기획이 되돌리면 값만 올리면 된다. `Jump_*` 패턴 3개도 라이브러리에 남아 있지만 비율이 0이라 뽑히지 않는다(`Docs/SpawnerBalance.md`의 사용 횟수 표가 전부 0인 것으로 확인된다). 일반 패턴은 세 레인을 동시에 막지 않으며 `PatternAnalyzerTests.DefaultNormalPatterns_NeverBlockEveryLaneAtOnce`가 이를 지킨다.

점프 체공 시간도 4판에서 0.8 → **1.6초**(점프 높이 1.6 → 2.2, 약 두 레인)로 바뀌었다. 도달 가능성 시뮬레이터는 `SimulationConfig.JumpDuration`으로 이 값을 쓰고, 기본값은 `SpawnerDefaults.JumpDurationSeconds = 1.6f`다. 런타임은 `GameConfig`의 `jumpDuration`을 그대로 쓴다. 둘이 어긋나면 `SpawnerBalanceReport`가 경고를 남기고 리포트에도 한 줄을 적은 뒤 기획값 1.6으로 돌린다.

기본 패턴 33개(전부 1레인 장애물 조합, 돌은 언제나 하단). 티어는 최악 입력 수와 반응 여유로 자동 결정되며 아래 값은 오프라인 판정 결과다.

| 묶음 | 패턴 | 티어 |
| --- | --- | --- |
| 단일 | `Rock_Bottom`, `Fish_Top/Middle/Bottom`, `Log_Top/Middle/Bottom` | 1 |
| 돌 반복(하단 봉쇄 연장) | `Rocks_BottomTwice`(0.9초 간격) | 1 |
| | `Rocks_BottomThrice`(0.8초 간격 3개, 하단을 약 2.0초 막음) | 2 |
| 두 레인 동시 | `FishTop_RockBottom`, `LogTop_RockBottom`, `Fishes_TopBottom`, `FishTop_LogBottom` | 1 |
| | `FishMiddle_RockBottom`, `LogMiddle_RockBottom`, `Fishes_TopMiddle`, `FishTop_LogMiddle`, `LogTop_FishMiddle` | 2 |
| 시간차·연속 레인 변경 | `FishTop_ThenRockBottom`(0.7초), `LogTop_ThenRockBottom`, `RockBottom_ThenLogTop`(0.6초) | 1 |
| | `RockBottom_ThenFishTop`(0.7초), `LogMiddle_ThenRockBottom`, `RockBottom_ThenLogMiddle`(0.6초), `Fish_TopThenBottom`, `Fish_BottomThenTop`(0.5초) | 2 |
| | `Rocks_BottomTwice_FishTopBetween`(하단 돌 → 0.7초 뒤 상단 물고기 → 1.4초 뒤 하단 돌, 가운데로 밀어 넣는다) | 2 |
| | `RockBottom_ThenFishMiddle`(0.6초) | 3 |
| 위빙(나갔다 돌아옴) | `Weave_RockBottom_ThenFishTopLogMiddle`(하단 돌, 0.9초 뒤 상단 물고기 + 중단 통나무 → 하단으로 복귀) | 3 |
| | `Weave_LogMiddle_ThenFishTopRockBottom`(중단 통나무, 1.2초 뒤 상단 물고기 + 하단 돌 → 중단으로 복귀) | 3 |
| 점프 필수 (비율 0) | `Jump_FishTop_FishMiddle_RockBottom`, `Jump_LogTop_FishMiddle_RockBottom`, `Jump_FishTop_LogMiddle_RockBottom`(0.2초 시차) | 3 |

구판 패턴 중 돌을 상단·중단에 두던 17개(`Rock_Top`, `Rock_Middle`, `Rocks_TopBottom/TopMiddle/MiddleBottom`, `RockTop_FishBottom`, `RockMiddle_LogBottom`, `LogTop_RockMiddle`, `Rocks_TopThenBottom/BottomThenTop`, `LogBottom_ThenRockMiddle`, `LogTop_ThenRockMiddle`, `Weave_MiddleThenTopBottom`, `Jump_*` 4개)는 에셋까지 지웠다.

점프 필수 패턴은 세 레인이 동시에 막히는 구간이 이동 잠금(0.2초)보다 길고 점프(1.6초)보다 짧아야 한다. 돌이 하단 전용이 되어 세 레인 봉쇄는 반드시 「상단·중단 = 물고기/통나무 + 하단 = 돌」 꼴이다. 세 패턴 모두 반응 여유 1.50초로 판정되며 시뮬레이터가 점프 필수로 분류한다.

## 알고리즘

### 1. 시간축 레인 점유

장애물 하나는 "레인 집합을 시각 [BlockStart, BlockEnd] 동안 막음"이다. **레인은 이산이다.** 도달 가능성 계산은 y좌표를 쓰지 않고 `laneSpan`으로 만든 비트마스크만 본다. 그래서 히트박스 **세로 크기는 시뮬레이션 결과에 영향을 주지 않는다**. 다만 이 가정이 성립하려면 장애물 상자가 이웃 레인의 플레이어에 닿지 않아야 하고, 그 조건은 `ObstacleSpec.ReachesNeighbourLane(레인 간격 1.1, 플레이어 세로 0.71)`로 검사한다(테스트 `DefaultObstacles_NeverReachTheNeighbourLane`). 세로가 1.49 이상으로 커지는 장애물이 생기면 그때는 시뮬레이터도 레인 점유를 기하로 계산하도록 고쳐야 한다. 가로(충돌 길이)와 플레이어 반폭은 봉쇄 시간을 직접 정하므로 시뮬레이션에 그대로 들어간다. 장애물 중심이 `spawnCenterX`에서 `speed = ScrollSpeed × 장애물 배율 × 구간 배율`로 왼쪽으로 가고, 플레이어는 `PlayerX ± PlayerHalfWidth`에 있으므로

- `BlockStart` = 장애물 충돌 앞면이 플레이어 오른쪽 면에 닿는 시각(= 도착 시각)
- `BlockEnd` = 충돌 뒷면이 플레이어 왼쪽 면을 지나는 시각
- `AppearTime` = 몸 앞면이 `ScreenRightX`를 넘는 시각(스폰 시 이미 화면 안이면 스폰 시각)

패턴은 항목마다 "도착 시각 = 패턴 도착 시각 + arrivalOffset"으로 배치한다. 스폰은 항상 지금(`now`) 하되 도착 시각에서 거꾸로 x를 계산하므로, 느린 돌은 화면 끝에, 빠른 물고기는 더 오른쪽(화면 밖)에 놓여 같은 순간에 도착한다. 패턴의 가장 이른 도착 시각은 모든 항목이 화면 밖에서 시작할 수 있는 시각이다(`PlacementBuilder.EarliestArrivalDelay`).

### 2. 플레이어 상태와 도달 가능성 전파

플레이어 상태 = (레인 0..2 또는 공중, 잠금 남은 틱, 쿨타임 남은 틱). 격자 0.05초. 규칙은 기획서 6~9절 그대로다.

- 잠금 0이고 물속이며 그 레인이 막혀 있으면 죽는다. 잠금 중(이동·공중)이면 무적.
- 잠금 0일 때 입력: 위/아래(레인은 즉시 바뀌고 0.2초 잠금, 예약 없음), 상단에서 쿨타임 0이면 점프(1.6초 공중, 착수 시 상단·쿨타임 1.2초 시작).
- 착수 순간부터 판정 재개.

`Reachability.Propagate`는 상태 집합(`StateSet`, 상태마다 "여기까지 오는 데 필요한 최소 입력 수")을 틱 단위로 전파한다. 집합이 비면 어떤 입력열로도 살 수 없다는 뜻이다. 상태 수는 4 × 16 × 25 = 1600이라 한 검사가 수만 번의 정수 연산이다.

### 3. 안전 여유 두 가지

- **입력 허용 오차 = `safetyPadding`(0.1초).** 모든 점유 구간을 앞뒤로 0.1초 늘려서 검사한다(격자 올림·내림까지 더하면 0.1~0.15초). 늘린 구간에서 길이 있으면 실제 게임에서는 그 입력을 ±0.1초 틀려도 산다. 0.15로 올리면 돌 한 줄(0.76초 봉쇄)을 점프(1.6초)로 넘는 창이 격자에서 좁아지니, 점프 창 = 점프 시간 − 봉쇄 시간 − 2×오차가 0.15초 이상 남게 둔다. "정확히 0.1초 창에 입력해야 하는" 해법은 이 단계에서 걸러진다.
- **반응 여유 = 동결 창.** 패턴의 마지막 장애물이 화면에 나타난 시각 + `minReactionMargin`까지 **아무 입력도 하지 않고** 살 수 있어야 한다. 사람은 보고 나서 반응하므로, 보이기 전에 미리 움직여야만 사는 배치를 거른다. 동결은 스폰 결정 시각부터 시작하므로, 이전 장애물 때문에 지금 당장 입력해야 하는 상황(예: 점프 필수 패턴을 처리하는 중)이면 후보가 거절되고 조금 뒤에 다시 시도한다. 즉 플레이어가 급한 조작 중일 때는 새 패턴을 내지 않는다.

### 4. 패턴 판정 (`PatternAnalyzer`)

패턴을 시각 0에 가장 이른 도착으로 놓고, 시작 상태 6개(세 레인 × {잠금 없음, 이동 잠금 최대})마다 따로 전파한다.

- 점프 금지 + 쿨타임 최대로 모든 시작 상태가 살면 **일반 패턴**(`SolvableWithoutJump`).
- 아니고, 점프 허용 + 쿨타임 0으로 모두 살면 **점프 필수**(`JumpRequired`).
- 둘 다 아니면 해결 불가. `Initialize`가 경고하고 절대 스폰하지 않는다.
- `WorstCaseInputs` = 시작 상태별 최소 입력 수의 최댓값. 티어 = 1(≤1 입력), 2(2), 3(≥3). 반응 여유가 1.5초(`PatternAnalysis.ShortMarginThreshold`) 미만이면 한 단계 올린다. 물고기(빠름)와 물고기 조합이 이 규칙으로 티어 2가 되어, 3판 11절의 "진행 2부터 물고기·통나무 비중 증가"를 티어 가중치로 만든다.
- `ReactionMargin` = 동결 창을 이진 탐색해 얻은, 시작 상태 전부가 견디는 최대 무입력 시간.

"모든 패턴에 확실한 해법이 하나 이상"(안전 규칙 6)은 여기서 보장된다. "하단에 있어도 상단까지 올라가 점프할 시간"(규칙 4)은 시작 상태에 하단·이동 잠금 최대가 들어 있으므로 함께 보장된다.

### 5. 코스 생성 (`CourseGenerator.TryPlaceNext`)

매 프레임 `Advance`가 `SectionTime ≥ NextDecisionTime`이면 호출한다.

1. 난이도 샘플을 구간 진행도로 뽑는다. `spawnEnabled`가 아니면 `SpawnStopped`.
2. 점프 필수를 낼지 정한다: 직전 패턴이 점프 필수가 아니고(규칙 2), 플레이어가 쿨타임 중이 아니고(규칙 1, `frontier.AllInCooldown`), 난수 < `jumpRequiredRatio`.
3. 후보 = 해당 종류로 판정된 패턴 중 장애물의 사용 여부 플래그가 맞는 것. 일반 패턴은 **티어 가중치 × `ObstacleMix` 비율 보정**으로 뽑고, 가중치가 전부 0이면 티어를 무시한다(비율 보정은 그대로 건다).
4. 후보를 최대 `maxCandidateAttempts`개까지 비복원 추출한다. 후보마다 배치를 만든다: 도착 시각 = max(지금 + 가장 이른 도착, **구간 시작 유예 `sectionStartGrace`**, 이전 패턴 BlockEnd + `patternGap`, 점프 필수면 이전 점프 필수 BlockEnd + `minJumpPatternGap`)(규칙 3). 유예는 `BeginSection`마다 다시 적용되므로 체크포인트 재시작에도 첫 장애물은 유예(임시 2.0초) 뒤에 도착한다. 유예가 없으면 빠른 물고기가 구간 3 속도에서 약 1.1초 만에 도착해, 아직 반응하지 않은 플레이어가 시작 직후 맞을 수 있다(코어루프 PlayMode 테스트에서 무작위 시드로 재현된 실패).
5. **생존 검사**: 지금 시각의 플레이어 상태(런타임은 실제 상태 하나, 오프라인은 도달 가능 집합)에서 이미 놓인 장애물 + 후보를 모두 포함해 전파한다. 동결 창 [지금, 마지막 등장 + `minReactionMargin`). 집합이 비면 거절.
6. 점프 필수 후보가 전부 거절되면 일반으로 내려간다. 일반도 전부 거절되면 아무것도 내지 않고 `retryInterval` 뒤 다시 결정한다(빈 구간은 항상 안전).
7. 채택하면 다음 결정 시각 = BlockEnd + gap − (라이브러리에서 가장 느린 패턴의 도착 지연). 즉 다음 패턴이 정확히 gap 뒤에 도착할 수 있도록 미리 결정한다.

### 클리어 보장 논리

- 라이브러리 수준: 모든 패턴은 어느 레인·어느 잠금 상태에서 시작해도 해법이 있다(4).
- 코스 수준: 새 패턴은 **지금 이 플레이어 상태**에서 이미 놓인 장애물과 합쳐도, 반응 여유만큼 가만히 있다가 움직여도, 입력을 ±0.1초 틀려도 살아남는 길이 있을 때만 놓인다(5). 플레이어가 그 길을 안 따라가도 스포너 탓은 아니다.
- 규칙 1~3은 명시적으로, 4~6은 시뮬레이터로 지킨다.
- 오프라인(`CourseRunner`)은 초기 상태(중단, 잠금 0, 쿨타임 0)에서 도달 가능 집합을 코스 끝까지 끌고 가며 같은 검사를 하므로 "어떤 입력열이 존재해 클리어 가능"을 자동으로 검증한다. 테스트가 이를 4구간 × 13시드에 대해 확인한다.

시드: 같은 `seed`·`sectionIndex`면 난수열이 같다. 다만 런타임은 실제 플레이어 상태로 검사하므로, 플레이어가 다르게 움직이면 거절/채택이 달라져 코스가 달라질 수 있다. 오프라인 도구와 테스트는 결정적이다.

## 난이도 손잡이 (`DifficultyCurve`)

3판 9절 구간 시간: 진행 1 25초, 2 30초, 3 35초, 4 15초(`SpawnerDefaults.GetSectionDuration`). 곡선의 x는 구간 진행도 0~1이다.

| 곡선 | 의미 | 값 (구간 1 → 4, 시작→끝) |
| --- | --- | --- |
| `speedMultiplier` | 장애물 접근 속도 배율. **2026-09-06 기획자 답변 4로 구간 속도 증가는 없앴다.** 네 구간 전부 1.0 고정이다. 장애물별 배율(물고기 1.25, 통나무 0.8)은 그대로 남아 종류 간 속도 차이만 만든다 | 1.0, 1.0, 1.0, 1.0 |
| `patternGap` | 이전 패턴이 지나간 뒤 다음 패턴 도착까지의 여유(초). 등장 빈도이자 **이번 판의 주 난이도 손잡이**. 4번 기획서 06절 추천 후보 범위(1: 2.0~2.5, 2: 1.6~2.0, 3: 1.3~1.7, 4: 2.5~3초) 안이다 | 2.5→2.0, 2.0→1.6, 1.7→1.3, 2.5→3.0 |
| `minReactionMargin` | 패턴이 다 보인 뒤 무입력으로 버텨야 하는 최소 시간(초). 검증 문턱이다. **기획자 답변 5** | 1.3, 1.0, 0.8, 1.3 |
| `jumpRequiredRatio` | 점프 필수 패턴 비율. 4판에서 점프 강제는 보스 3만의 것으로 확정되어 네 구간 전부 0이다. 스위치는 남겨 두었다(테스트 `ZeroJumpChance_NeverSpawnsJumpRequiredPatterns`, `DefaultProfile_NeverRequestsJumpPatterns`) | 0, 0, 0, 0 |
| `tier1/2/3Weight` | 일반 패턴 티어별 추출 가중치. 속도가 고정되었으므로 **두 레인 동시·시간차·위빙 비율이 두 번째 난이도 손잡이**다 | 1: (1.0, 0.2→0.5, 0→0.15) 단순 회피 위주 · 2: (0.6→0.4, 1.0, 0.3→0.6) 조합·연속 레인 변경 증가 · 3: (0.3→0.2, 1.0, 0.8→1.2) 위빙까지 · 4: (1.0, 0.3→0.1, 0) |
| `spawnStopProgress` | 이 진행도부터 스폰 중단 | 1, 1, 1, 0.7 (구간 4는 15초 중 10.5초 이후 장애물 없음) |

3판 11절대로 조작 속도는 건드리지 않는다. 이번 판부터 접근 속도도 건드리지 않으므로 난이도는 **간격**과 **패턴 종류 비중** 둘로만 만든다. 구간 1은 간격이 넓고 티어 1(단순 1레인 회피) 위주, 2는 간격이 줄고 티어 2 비중이 오르며, 3은 간격이 더 줄고 티어 3(위빙·3항목 시간차)까지 올라오고, 4는 장애물이 매우 적다가 70% 지점부터 없어진다. 점프 필수 비율은 네 구간 모두 0이다. 물살 세기 변화는 없다.

**반응 여유가 난이도 필터로도 작동한다.** 구간별 `minReactionMargin`이 검증 문턱이라, 자체 반응 여유가 문턱보다 짧은 패턴은 그 구간에서 사실상 뽑히지 않는다. 구간 1·4(1.3초)에서는 `Rocks_BottomThrice`(0.80), `RockBottom_ThenFishTop`(1.20), `Rocks_BottomTwice_FishTopBetween`(1.00), 위빙 2개(1.00 / 0.70)가 빠지고, 구간 3(0.8초)에서는 `Weave_LogMiddle_ThenFishTopRockBottom`만 빠진다. 남는 일반 패턴은 어느 구간에서도 25개 이상이라 단조로워지지 않는다(테스트 `EverySection_HasPatternsThatMeetItsReactionMarginTarget`).

기획자 답변 5의 목표치는 "패턴이 다 보인 뒤 가만히 있어도 되는 시간"이고, 시뮬레이터의 동결 창 정의와 같다. 속도 배율이 사라져 장애물이 화면에 오래 보이므로 1.3초 문턱도 여유 있게 충족된다(가장 이른 도달이 돌 2.55초, 물고기 2.04초, 통나무 3.21초).

## 프리팹 비주얼

`SpawnerAssetGenerator.EnsurePrefab`이 장애물마다 프리팹 하나를 만든다. 아트가 있는 장애물과 없는 장애물의 경로가 다르다.

**아트가 있을 때(돌, 통나무).** 스프라이트는 `Assets/GameAssets/Art/오브젝트/`의 PNG다. 이 PNG는 1920×1080 캔버스에 오브젝트가 작게 들어 있으므로 `sprite.bounds`(캔버스 전체 19.2 × 10.8 unit)로 크기를 맞추면 안 된다. 생성기는 `ObjectArtPostprocessor.TryGetAlphaSizePixels`로 **알파 바운딩 박스**를 픽셀로 구한 뒤 `ObstacleVisual.UniformScale(spec, 알파 가로, 알파 세로)`로 **균등 스케일**을 계산한다(가로세로 같은 값이라 아트가 찌그러지지 않는다. 축은 위 「돌은 높이 기준으로 맞춘다」의 `fitAxis`가 정한다). pivot이 알파 박스 중심이라 오브젝트 원점이 곧 장애물 중심이고, `BoxCollider2D.offset`은 0이다. 콜라이더는 `ObstacleVisual.LocalSize(스케일, 충돌 길이, 충돌 높이)`라 스케일을 곱하면 정확히 명세 크기(월드)가 된다. `SpriteRenderer.color`는 흰색(아트 원색), `drawMode`는 Simple, `sortingOrder`는 5다.

| 장애물 | 아트 | 알파 박스(px) | 균등 스케일 | 화면 크기(월드) | 콜라이더 로컬 | 콜라이더 월드 |
| --- | --- | --- | --- | --- | --- | --- |
| Rock | `돌1.png`(기본), `돌 2.png` | 560 × 252 | 0.4365079 (**높이 맞춤**) | 2.4444 × 1.10 | 4.8796363 × 2.1992726 | 2.13 × 0.96 |
| Fish | `장애물 물고기1.png`(기본), `장애물 물고기2.png` | 410 × 205 (두 장 합집합) | 0.219512 | 0.90 × 0.45 | 3.5533333 × 1.7766666 | 0.78 × 0.39 |
| Log | `통나무.png` | 531 × 288 | 0.338983 | 1.80 × 0.976 | 4.6197 × 2.5075 | 1.566 × 0.85 |

세 프리팹의 `BoxCollider2D.m_Size`는 위 「콜라이더 로컬」 값으로 **YAML을 직접 고쳤다**(생성기를 돌리지 않았다). `m_Offset`은 pivot이 알파 박스 중심이라 (0, 0)이다.

물고기는 두 프레임이 pivot 하나를 쓰므로 스케일 계산에 쓰는 알파 박스도 **두 장 합집합**이다(`ObjectArtPostprocessor.TryGetAlphaSizePixels`가 폴더 묶음이면 합집합을 돌려준다). 프레임별로는 `장애물 물고기1`이 410 × 202, `장애물 물고기2`가 410 × 205라 가로는 같고 세로만 3 px 다르다. 가로가 같으니 어느 쪽을 써도 균등 스케일은 0.219512로 같고, 합집합을 쓰면 pivot과 스케일이 같은 박스에서 나와 어긋나지 않는다.

돌이 보이는 높이 0.45인데 콜라이더 높이가 0.80이던 문제(그림보다 위아래로 0.175씩 넓어 스치는 판정이 났다)는 **2026-09-06 기획자 답변 1로 (b) 안이 채택되어** 해결했다. 세로는 아트 높이 × 0.87이다. 그 뒤 **같은 날 사용자 결정으로 돌 자체가 「강바닥 바위」가 되어 몸길이 1.0 → 2.44, 세로 0.45 → 1.10으로 커졌다**(위 「돌은 높이 기준으로 맞춘다」). 히트박스는 여전히 그림보다 13% 작고((1.1 − (0.96 + 0.71)/2) = 0.265의 여유가 남는다) 실제 판정은 가로가 지배한다.

통나무는 아트 비율 1.84:1이고 4판 몸길이가 1.8이라 균등 스케일만으로 레인 높이 1.0 안에 들어간다(0.976). 3판 몸길이 3.0일 때 검토했던 9-슬라이스(`drawMode = Sliced` + border)는 더 이상 필요 없어 쓰지 않는다.

**아트가 없을 때(지금은 해당 없음).** `Assets/GameAssets/Placeholder/Square.png`(64px, PPU 100 → 0.64 unit)를 비균등 스케일로 늘리고 `ColorFor`의 임시색을 칠한다. 예전 세 장애물 전부가 이 경로였다. 세 장애물 모두 아트가 왔으므로 지금은 아무도 이 경로를 타지 않는다. 새 장애물이 늘면 `SpawnerAssetGenerator.ArtPathsFor`에 경로를 추가하는 것만으로 아트 경로로 넘어간다.

**물고기 방향.** 장애물 물고기는 아트가 이미 **왼쪽을 보고 있다**. 장애물은 오른쪽에서 왼쪽으로 오므로 진행 방향과 그림 방향이 맞고, 스케일 x를 음수로 두거나 `SpriteRenderer.flipX`를 켜지 않는다(균등 스케일이 깨지면 콜라이더 계산도 어긋난다).

**장애물 애니메이션.** `ObstacleThing`에 `[SerializeField] private CustomAnimation swimClip`이 있다. 클립이 있으면 `OnSpawned`가 `SpriteAnimatorModule`을 만들어 루프 재생하고, `OnReleased`가 `Stop()` 뒤 `ClearModules`로 뗀다. 클립이 없으면(돌·통나무) 모듈 자체를 만들지 않아 비용이 0이다. 클립이 있는 장애물은 `variants`를 비워 둔다 — `ApplyVariant`가 `SpriteRenderer.sprite`를 덮으면 애니메이션과 싸우기 때문이다(생성기의 `VariantPathsFor`가 돌·통나무만 돌려준다).

| 장애물 | 클립 | 프레임 | fps | loop | 변종 |
| --- | --- | --- | --- | --- | --- |
| Rock | 없음 | — | — | — | `돌1`, `돌 2` |
| Log | 없음 | — | — | — | `통나무` |
| Fish | `Design/Animations/Obstacle_Fish.asset` | 2 (`장애물 물고기1/2.png`) | 6 | true | 없음 |

클립은 `Game.Animation.Editor`의 `ObstacleClipTable`이 정본이고 `AnimationAssetGenerator`가 굽는다(`Docs/Animation.md` 「생성되는 클립」). `SpawnerAssetGenerator.Generate`는 프리팹을 만들기 전에 클립 에셋이 없으면 `AnimationAssetGenerator.Generate()`를 한 번 부르므로 메뉴 순서를 신경 쓰지 않아도 된다.

**돌 변종.** `ObstacleThing`에 `[SerializeField] private Sprite[] variants`가 있고 생성기가 `돌1`, `돌 2`를 순서대로 넣는다(`variants[0]`이 기본 스프라이트). `SpawnModule`은 시드(`Initialize(…, seed)`)와 구간 번호로 만든 `DeterministicRandom`을 따로 하나 들고, 스폰마다 `thing.ApplyVariant(random.NextUInt())`를 부른다. `ObstacleVisual.PickVariant(seed, count)`가 인덱스를 고른다. `UnityEngine.Random`을 쓰지 않으므로 같은 시드는 같은 변종 배열을 만든다. 변종 배열이 비면(`Fish`) `ApplyVariant`는 아무것도 하지 않는다. 지금 `돌1.png`와 `돌 2.png`는 바이트가 같아 화면상 차이는 없다(`Docs/Requests.md`에 남겼다). 2026-09-06 아트 갱신으로 온 `오브젝트/긴돌.png`은 **이 변종이 아니다** — 사용자 결정으로 상류 단차 앞면에 쓴다(`Docs/Ledge.md` 「단차 모양 (긴 돌)」). 알파 박스가 692 × 779 px(0.89:1)이라 돌1(560 × 252, 2.22:1) 자리에 끼우면 프리팹의 균등 스케일 0.4365가 그대로 곱해져 3.02 × 3.40 unit, 레인 3칸 높이로 나온다. `ObstacleThing.ApplyVariant`는 `SpriteRenderer.sprite`만 갈아끼우고 `localScale`은 건드리지 않으므로 **변종끼리는 알파 박스 비가 같아야 한다.**

**임포트 설정.** `Assets/GameAssets/Art/오브젝트/`는 아트 미러라 `.meta`를 손으로 고치지 않는다. `Game.Animation.Editor`의 `ObjectArtPostprocessor`가 하위 폴더까지 훑어 임포트마다 Sprite / Single / PPU 100 / Tight / 밉맵 없음 / 무압축 / pivot을 다시 건다. pivot 규칙은 **폴더 단위**다: 루트에 바로 놓인 `돌1`·`돌 2`·`통나무`는 **파일별** 알파 박스 중심, 하위 폴더 `장애물 물고기/`의 2장은 **폴더 합집합** 알파 박스 중심 하나를 공유한다(프레임마다 pivot이 다르면 2프레임 애니메이션이 튄다). 자세한 것은 `Docs/Animation.md`의 「후처리기 범위」에 있다.

## 플레이어 히트박스

연어 판정도 2026-09-06 기획자 답변 1로 좁혔다. 연어 아트 12장 합집합 알파 박스는 531 × 280 px, `Play.unity`의 Player `localScale`은 0.29다.

| | 몸통(월드) | 배율 | 히트박스(월드) | `Play.unity` 로컬(`m_Size`) |
| --- | --- | --- | --- | --- |
| 가로 | 1.5399 | **0.6** (`hitboxWidthScale`) | **0.92394** | 3.186 |
| 세로 | 0.812 | 0.87 (`hitboxHeightScale`) | **0.70644** | 2.4359999 |

- 가로만 몸통의 60%로 줄였다. 세로는 예전과 같은 87%다. 상자는 몸통 중심에 두므로 `m_Offset`은 (0, 0)이다(예전의 −0.00000061은 반올림 찌꺼기였다).
- 런타임에 스프라이트 알파 박스를 읽을 수 없어 `LanePlayer`가 콜라이더를 다시 계산하지는 않는다. **`Play.unity`의 `BoxCollider2D.m_Size`가 정본**이고 위 표가 그 계산 과정이다. 값이 맞는지는 `ObstacleVisualTests.PlayerHitbox_*`가 `ObstacleVisual.HitboxWorldSize`로 검증한다.
- 시뮬레이터의 `playerHalfWidth`는 0.92394 / 2 ≈ **0.46**이다(`SpawnerDefaults.PlayerHalfWidth`, `ObstacleSpawnerSettings.playerHalfWidth`). 예전에는 0.35로 두어 실제 판정이 시뮬레이션보다 가로로 0.63 unit 넓었는데(`Docs/DesignQuestions.md` 질문), 이제 어긋남이 0.004 unit뿐이다.
- 세로 0.71은 이웃 레인 검사(`ObstacleSpec.ReachesNeighbourLane`)의 입력으로도 쓴다.

## 통합 절차 (코어루프 에이전트가 할 일)

1. **에셋 생성.** 컴파일 오류가 없는 상태에서 `Team1004 > Generate Spawner Assets`. 프리팹은 `SpriteRenderer` + `BoxCollider2D`(trigger) + `ObstacleThing`(`Hazard.kind` = id)이다. 플레이어에 Kinematic `Rigidbody2D`가 있으므로 장애물에는 없다. 크기 계산은 아래 「프리팹 비주얼」에 있다.
2. **씬 배치.** `Play.unity`에 빈 오브젝트 `ObstacleSpawner`를 두고 `ObstacleSpawner` 컴포넌트를 붙인다. Inspector에서 `settings` = `Assets/GameAssets/Design/Spawner/ObstacleSpawnerSettings.asset`, `scrollRoot` = `ScrollRoot`(StageScroller). 코어루프 생성기(`CoreLoopSetup`)에 넣으면 좋다. 테스트용 `Rock0~2`는 지운다.
3. **풀 등록.** `Initialize`가 `PoolManager.Instance.Register(id, prefab, prewarm)`를 한다. 씬 준비 단계에서 하고 싶으면 `SceneReference.PrepareAsync`에서 같은 키로 먼저 등록해도 된다(이미 등록된 키는 건너뜀). 풀 회수는 `PoolSceneBridge`가 씬 전환마다 하고, 스포너도 `BeginSection`/`ReleaseAll`/`OnDisable`에서 한다.
4. **PlayFlow 호출 순서.**

```csharp
[SerializeField] private ObstacleSpawner spawner;

private void Start()
{
    spawner.Initialize(spawner.Settings, scroller.transform, seed);   // seed: 고정값 또는 시각 기반
    spawner.BeginSectionByDuration(0, SpawnerDefaults.GetSectionDuration(0)); // 구간 시작마다. 0 = 게임 진행 1, 25초
    ...
}

private void Update()
{
    if (State != PlayState.Running)
        return;

    Distance += config.ScrollSpeed * Time.deltaTime;
    spawner.Advance(Time.deltaTime, Distance, BuildSpawnPlayerState());

    if (spawner.IsSectionExhausted) { /* 구간 끝 → 컷신/보스 */ }
}

private SpawnPlayerState BuildSpawnPlayerState()
{
    return SpawnPlayerState.FromPlayer(player, jump != null ? jump.CooldownRemaining : 0f,
        jump != null ? jump.AirborneRemaining : -1f, laneMove != null ? laneMove.MoveRemaining : -1f);
}

// 실패: spawner.Stop();  다음 구간: spawner.BeginSectionByDuration(n, seconds);  씬 이탈: 자동 회수
```

**코어루프가 현재 `BeginSection(index, duration × ScrollSpeed)`로 거리를 환산해 넘기고 있다면 `BeginSectionByDuration(index, duration)`으로 바꾸고 환산 코드를 걷어낸다.** 두 호출은 진행도 계산 방식만 다르고(시간/거리) 나머지는 같다.

`Advance`는 Running일 때만 부른다. 일시정지는 `timeScale = 0`이라 `deltaTime`이 0이지만, 호출 자체를 끊는 편이 명확하다. `Distance`는 `PlayFlow.Distance`(누적 unit)를 그대로 넘긴다. 시간 기준 구간(`BeginSectionByDuration`)은 `SectionTime / duration`으로 진행도를 재고, 거리 기준(`BeginSection`)은 첫 `Advance`의 `distance`를 시작점으로 잰다. 구간 종료 자체는 PlayFlow가 타이머로 판단하고, 스포너는 `IsSectionExhausted`(스폰 끝 + 화면에 장애물 없음)로 "깨끗하게 끝났는지"만 답한다.

5. **점프 에이전트 연동.** 점프 모듈이 `CooldownRemaining`, `AirborneRemaining`(초)을 노출하면 `FromPlayer`에 넘긴다(`Docs/Requests.md`에 요청). 노출 전에는 `SpawnPlayerState.FromPlayer(player, player.IsAirborne ? 0f : GameConfig.Current.JumpCooldown)`처럼 최악값을 넘겨도 안전하다(점프 필수 패턴이 덜 나올 뿐이다). `LanePlayer.IsAirborne`은 이미 있으므로 공중 여부는 자동으로 반영된다.
6. **구간 길이.** 3판은 시간 기준(25/30/35/15초)이므로 `BeginSectionByDuration`을 쓴다. `BeginSection(int)`은 `GameConfig.Current.StageLength`(거리) 기준으로 남겨 두었다. 25초 구간 1에서는 패턴이 8~10개쯤 나온다.

## 에디터 도구

- **생성기와 리포트는 지금 얼려 두었다.** 에디터가 열려 있는 동안에는 돌리지 않는다. 2026-09-06 판은 `Design/Spawner/**`, `GameAssets/Obstacles/*.prefab`, `Play.unity`의 Player 콜라이더를 YAML로 직접 고쳐 `SpawnerDefaults`와 값을 맞췄고, 스크립트로 두 쪽이 같은지 확인했다. 다시 돌릴 수 있게 되면 `Regenerate Spawner Assets (Overwrite)` 한 번으로 같은 결과가 나와야 한다.
- **`SpawnerDefaults`의 기본값이 바뀌면 이미 생성된 `Design/Spawner` 에셋에는 자동 반영되지 않는다.** 생성기를 돌리거나 에셋 값을 손으로 맞춘다. 밸런스 리포트와 테스트는 에셋이 아니라 `SpawnerDefaults`(리포트는 에셋이 있으면 에셋)를 읽으므로 둘이 어긋나면 리포트 수치가 게임과 다르다.
- `Team1004 > Generate Spawner Assets`: 없는 것만 만든다. `Regenerate Spawner Assets (Overwrite)`: 확인 후 프리팹과 Design 에셋을 기본값으로 다시 쓰고(GUID 유지), 기본값에 없는 장애물 정의·프리팹·패턴 에셋(구판 Shark/Whale과 그 패턴)은 삭제한다.
- `Team1004 > Spawner Balance Report`: `Docs/SpawnerBalance.md`를 쓴다. 이번 판의 리포트는 같은 시뮬레이터 코드를 `Simulation/*.cs`만 모아 컴파일한 오프라인 .NET 콘솔로 돌려 적었고, 표에 「요구 반응 여유」·「여유 미달 패턴 수」·「장애물 등장 비율」 세 항목이 더 있다. 패턴 표(종류·티어·최악 입력 수·반응 여유·도착·막는 시간), 구간별 코스 표(구간당 시드 20개, 3판 구간 시간 25/30/35/15초: 평균 패턴 수, 점프 필수 비율, 평균 도착 간격, 검증 거절 수, 빈 결정 수, 해결 가능 코스 수), 구간별 패턴 사용 횟수. Design 에셋이 없으면 `SpawnerDefaults`로 돈다.
- 테스트(Test Runner > EditMode > Game.Spawner.Tests): 기본 일반 패턴이 모든 레인에서 점프 없이 해결됨, 점프 패턴이 점프 필수로 판정됨, 반응 여유 ≥ 0.3초, 구간마다 문턱을 넘는 일반 패턴이 8개 이상, 최대 속도에서도 해결 가능, 세 레인을 점프보다 길게 막는 패턴 거절, 기본 장애물 전부 1레인, 쿨타임 중 전 레인 봉쇄는 생존 불가, 4구간(25/30/35/15초) × 13시드 코스 전부 해결 가능, 구간 시작 첫 패턴이 세 레인 × {잠금 없음, 이동 잠금 중} 어느 시작 상태에서도 채택되고 유예 시간 뒤에 도착하며 반응 여유 동결로도 생존 가능, 점프 필수 규칙 준수, 같은 시드 같은 코스, 다른 시드 다른 코스, 구간 4 끝에 스폰 없음, 쿨타임 중 점프 필수 미출현.
- 2026-09-06에 늘어난 테스트: 돌이 하단에서만 시작함(`Rock_StartsOnlyInTheBottomLane`, `RockAboveTheBottomLane_IsReportedNotThrown`, `GeneratedCourses_OnlyPutRocksInTheBottomLane`), 물고기·통나무는 세 레인 전부, 모든 기본 패턴이 레인 규칙을 지킴, 충돌 높이가 아트 높이 × 0.87임, 장애물이 이웃 레인에 닿지 않음, `spawnWeight`가 5/3/2이고 `ObstacleMix`의 목표 점유율이 0.5/0.3/0.2임, 모자란 종류의 가중치가 실제로 올라감, 생성 코스의 종류 비율이 5:3:2에서 ±0.07 안(`GeneratedCourses_ApproachTheDesignObstacleRatio`), 구간 속도 배율이 전부 1.0(`DefaultProfile_HasNoSectionSpeedMultiplier`), 구간 반응 여유가 1.3/1.0/0.8/1.3(`DefaultProfile_UsesTheDesignReactionMargins`), 연어 히트박스가 60% × 87%이고 `playerHalfWidth`와 맞음(`ObstacleVisualTests.PlayerHitbox_*`).

## 임시값 목록

- 확정된 것(2026-09-06 기획자 답변): 등장 레인 제한(돌 하단 전용), 등장 비율 5:3:2, 히트박스 세로 = 아트 높이 × 0.87, 연어 히트박스 가로 60%, 구간 속도 배율 없음(전부 1.0), 구간별 반응 여유 1.3/1.0/0.8/1.3. 몸길이·충돌 길이·속도 배율은 4판에서 확정.
- 아직 임시값: 레인 폭 1.1, 패턴 간격 곡선(4번 06절 추천 후보 범위 안), 티어 가중치 곡선, `ObstacleMix.Strength` 1.0과 배율 상하한 0.02~24, 구간 4의 반응 여유 1.3(기획자가 「개발 기본값」으로 넘김). 점프 필수 패턴은 4판에서 일반 구간 사용 금지로 확정되어 `jumpRequiredRatio`가 네 구간 모두 0이다.
- 화면 오른쪽 6.4, 왼쪽 -6.4, 회수 여유 1 unit.
- 격자 0.05초, 입력 허용 오차 0.1초, 점프 필수 최소 간격 2초, 구간 시작 유예 2.0초(`sectionStartGrace`, 요구 하한 1.5초), 재시도 0.2초, 후보 시도 4개, 프리웜 4개, 기본 시드 12345.
- 난이도 곡선 전부(위 표), 패턴 간격 정의, 점프 필수 비율 정의, 반응 여유 정의(`Docs/DesignQuestions.md`에 질문 등록).
- 패턴 시간차 0.5/0.6/0.7/0.8/0.9/1.2/1.4초, 점프 필수 패턴의 0.2초 오프셋, 티어 승급 기준 반응 여유 1.5초.
- 정렬 순서 5. 장애물 물고기 클립의 fps 6(2프레임 루프 → 한 바퀴 0.333초). 굼떠 보이면 `ObstacleClipTable`의 값을 올린다. 물고기 스프라이트 방향은 아트가 왼쪽을 보고 있어 확정이다(반전 없음).

## 권장 설계에서 바꾼 점

| 권장 | 실제 | 이유 |
| --- | --- | --- |
| 패턴 항목의 x 오프셋 | 도착 시각 오프셋(`arrivalOffset`, 초) | 속도가 다른 장애물을 섞으면 x 오프셋은 도착 순서를 보장하지 못하고 구간 배율마다 모양이 달라진다. 시간 오프셋은 "물고기와 돌이 동시에 닿는다"를 배율과 무관하게 유지한다. 스폰 x는 도착 시각에서 역산한다 |
| 코스를 시드로 미리 생성 | 런타임에 한 패턴씩 생성, 검사는 실제 플레이어 상태로 | 규칙 1(쿨타임 중 점프 필수 금지)은 실제 플레이어 상태가 필요하다. 미리 만든 코스를 런타임에 고치면 시드 재현성이 어차피 깨진다. 대신 오프라인 도구·테스트는 도달 가능 집합으로 돌려 결정적이다 |
| 반응 여유 = 등장부터 최종 입력까지 간격 계산 | 동결 창(등장 뒤 N초 무입력 생존)으로 검사 | "필요한 입력의 최종 시작 시각"은 해법이 여러 개면 정의가 흔들린다. 동결 창은 단조라 이진 탐색이 되고, 런타임 검사와 같은 코드로 돈다 |
| 손잡이 4개 | 4개 + 입력 허용 오차(`safetyPadding`, 전역) + 스폰 중단 진행도 | 타이밍 정밀도 요구를 반응 여유와 분리해야 "창이 0.1초인 해법"을 거를 수 있다. 구간 4의 "끝에서 장애물 완전 소멸"은 별도 값이 명확하다 |
| 티어 가중치 + 점프 필수 비율 | 같음. 티어는 최악 입력 수로 자동 | 점프 패턴은 전부 티어 3이라 점프 풀에는 가중치를 쓰지 않는다 |
| 원하는 티어 없으면 낮은 티어로 | 가중치 풀 → 전체 풀 → 일반 풀 → 빈 구간 | 같은 취지 |

MonoBehaviour 대신 `MonoThing` + `Module` 구조를 쓴 것은 프로젝트 규칙이다.

## 검증하지 못한 위험 지점

작성 시 컴파일을 돌리지 못했던 항목이다. 통합 패스(2026-09-06)에서 스포너 에셋 Overwrite 재생성·EditMode·PlayMode(`FullLoopTests`, `DebugJumpTests`)가 통과했으므로 아래는 열어 볼 때의 참고 목록이다. 상류 단차용 `ObstacleSpawner.SetSpawningEnabled(bool)`(`SpawnModule.SpawningEnabled`: 이동·회수는 그대로, 스폰 판단만 건너뜀. 구간 시작 시 true)도 그때 들어갔다.

- `ObstacleThing : Hazard` — `Game.Player.Hazard`가 `public class Hazard : MonoThing`(non-sealed)일 때만 컴파일된다. `Hazard.kind`는 프리팹 생성기가 `SerializedObject.FindProperty("kind")`로 채우므로 필드 이름이 바뀌면 조용히 빈 값이 된다.
- `PoolManager.TryGetInstance(out var pool)` — `Singleton<T>`의 정적 메서드 이름을 코어루프 코드(`InputManager.TryGetInstance`)와 같게 가정했다.
- `PoolManager.TryGet(key, position, rotation, out instance, parent)`와 `Release(PooledInstance)`, `IsRegistered`, `Register(key, prefab, prewarm)` — `Core/Pool/PoolManager.cs`의 현재 시그니처에 맞췄다.
- `GameConfigValues.LaneCount`, `GetLaneY`, `PlayerX`, `ScrollSpeed`, `LaneMoveDuration`, `JumpDuration`, `JumpCooldown`, `StageLength` — 현재 `Config/GameConfigValues.cs`와 일치한다.
- `Module`이 전역 타입이라 `System.Reflection`을 네임스페이스 안에서 임포트하면 안 된다(현재 안 함).
- `Math.Clamp`, `Array.Fill`은 .NET Standard 2.1 API다. 프로젝트 API 레벨이 `.NET Standard 2.1`(ProjectSettings `apiCompatibilityLevel: 6`)인 것을 확인했다.
- `[SerializeField] private LaneFlags allowedStartLanes` — `[Flags]` enum은 Inspector에 마스크 드롭다운으로 나온다. `(int)` 캐스트로 마스크를 그대로 쓴다.
- `DifficultySample`/`SectionDifficulty`의 `in` 매개변수 생성자 — 람다 안에서 `new SectionDifficulty(start, end, stop)`으로 호출한다.
- Placeholder 경로에서만 `sprite.bounds.size`(PPU 100 기준 0.64 unit)를 쓴다. 아트 경로는 알파 바운딩 박스를 쓴다 — 1920×1080 캔버스라 `sprite.bounds`가 오브젝트가 아니라 캔버스 전체다.
- `Game.Spawner.Editor`가 `Game.Animation.Editor`를 참조한다(알파 박스 계산과 오브젝트 아트 임포트 설정 공유). `Game.Animation.Editor`는 `Game.Spawner*`를 참조하지 않으므로 순환은 없다.
- 런타임 성능: `Initialize`에서 패턴 15개 × 시작 상태 6개 × 이진 탐색을 돌린다(수십 ms 예상). 프레임 중 결정 한 번은 전파 1~4회(각 수만 연산)라 문제없어야 하지만, 히치가 보이면 `maxCandidateAttempts`를 줄이거나 분석을 에디터 베이크로 옮긴다.
- 자체 속도 이동은 `ObstacleRuntimeModule.Advance`가 `transform.position`을 직접 옮긴다. `ScrollModule`은 `localPosition`을 옮기므로 둘이 더해진다. `ScrollRoot`가 회전·스케일되어 있으면 어긋난다(현재 없음).
- 1레인 장애물의 콜라이더 높이는 이제 아트에서 나온다(돌·물고기 0.39, 통나무 0.85). 플레이어 히트박스 세로 0.71과 레인 간격 1.1로 계산하면 이웃 레인까지 0.55(통나무는 0.32) 남는다. `laneSpan` 2 이상은 두 레인 y의 평균에 놓이며 코드 경로는 남아 있지만 3판 데이터에는 없다.
- 프리팹·씬·Design 에셋을 YAML로 직접 고쳤다. Unity가 다시 임포트할 때 값이 그대로 읽히는지, Inspector에서 `collisionHeight`·`spawnWeight` 새 필드가 제대로 보이는지는 에디터를 열어 봐야 안다.

## 가정

- `Game.Player.Hazard : MonoThing`(non-sealed), `LanePlayer.CurrentLane/IsMoving/IsAirborne` — 현재 코드와 일치.
- `PlayFlow`가 `Initialize`/`BeginSection`/`Advance`/`Stop`을 부른다. `Game.Play`가 `Game.Spawner`를 참조한다(역방향 없음).
- 플레이 씬의 `ScrollRoot`는 위치 (0,0,0), 회전·스케일 없음.
- 점프 모듈은 착수 시 `LanePlayer.SetAirborne(false)`와 `SnapToLane(0)`을 한다(기획서 8절). 공중 판정은 `IsAirborne`으로 받는다.
- 구간(게임 진행 1~4)의 시작·끝은 PlayFlow가 판단한다. 스포너는 `BeginSection`으로 통보받고 `IsSectionExhausted`로 답한다.
