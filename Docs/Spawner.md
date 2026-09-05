# 장애물 스포너 (Game.Spawner)

3레인 물속 코스에 돌·다른 물고기·통나무 패턴을 놓는 모듈이다. 완전 랜덤이 아니라 검증된 패턴 라이브러리에서 뽑고, 순수 C# 시뮬레이터가 "지금 이 플레이어가 이 패턴을 확실히 피할 수 있는가"를 매번 검사한 뒤에만 스폰한다. 기획서 정본은 4번 분할본 `Team1004/Assets/Documents/정리 1번/4번 정리/05_일반장애물.md`, `06_일반구간_난이도_스폰.md`(구간 시간 확정, 스폰 간격 추천 후보, 미확정 목록), `16_미확정_검토중_아이디어.md` 4항, `17_현재_남은기획_체크리스트.md` B·G이며, 3판 `3번 정리.md` 9~11절·25절과 내용이 같다. 패턴 예시와 점프 필수 안전 규칙은 1판 PDF 13~16절(2판 13~14절과 동일)을 따른다. 3판 3절·25절대로 물살 세기는 난이도 요소가 아니며 스포너는 물 표현을 건드리지 않는다.

컴파일과 실행을 에디터에서 확인하지 못한 채 작성했다. 처음 열 때 콘솔 오류를 먼저 본다(「검증하지 못한 위험 지점」 참고).

## 구성

| 경로 | asmdef | 내용 |
| --- | --- | --- |
| `Assets/Scripts/Spawner/Simulation/` | `Game.Spawner` | UnityEngine에 의존하지 않는 순수 C#. 명세(`ObstacleSpec`, `PatternSpec`), 시간축 배치(`PlacementBuilder`, `ObstacleTiming`, `PatternPlacement`), 도달 가능성(`StateLayout`, `StateSet`, `BlockTimeline`, `Reachability`), 패턴 판정(`PatternAnalyzer`, `PatternAnalysis`), 난이도(`DifficultySample`, `SectionProfile`, `DifficultyProfile`), 코스 생성(`CourseGenerator`, `CourseRunner`, `CourseResult`, `GenerationStats`, `RejectReason`, `DeterministicRandom`), 기본값(`SpawnerDefaults`) |
| `Assets/Scripts/Spawner/` | `Game.Spawner` | ScriptableObject 데이터(`ObstacleDefinition`, `SpawnPattern`, `PatternLibrary`, `DifficultyCurve`, `ObstacleSpawnerSettings`), 호스트와 모듈(`ObstacleSpawner : MonoThing`, `SpawnModule : Module`, `ObstacleThing : Hazard, IPooledObject`, `ObstacleRuntimeModule : Module`), `SpawnPlayerState`, `Lane`/`LaneFlags` |
| `Assets/Scripts/Spawner/Editor/` | `Game.Spawner.Editor` | `SpawnerAssetGenerator`(메뉴 `Team1004/Generate Spawner Assets`, `Regenerate Spawner Assets (Overwrite)`), `SpawnerBalanceReport`(메뉴 `Team1004/Spawner Balance Report`) |
| `Assets/Scripts/Spawner/Tests/` | `Game.Spawner.Tests` | EditMode 테스트 `PatternAnalyzerTests`, `CourseGeneratorTests` |
| `Assets/GameAssets/Design/Spawner/` | 데이터 | 생성기가 만드는 Design 에셋 |
| `Assets/GameAssets/Obstacles/` | 프리팹 | `Rock.prefab`, `Fish.prefab`, `Log.prefab` (임시). 구판의 `Shark`/`Whale`은 Overwrite 생성 시 삭제된다 |

asmdef 참조: `Game.Spawner` → `Core.Foundation`, `Core.Modules`, `Core.Pool`, `Game.Config`, `Game.Player`. `Game.Play`는 참조하지 않는다(PlayFlow가 스포너를 참조하는 방향만 허용해 순환을 막는다).

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
| `Obstacles/<Id>.asset` `ObstacleDefinition` | `id`, `prefab`, `laneSpan`, `allowedStartLanes`(LaneFlags), `bodyLength`(unit), `collisionLength`(unit, 0이면 몸길이), `speedMultiplier`, `usableInNormalPatterns`, `usableInJumpPatterns` | 장애물 한 종류. `id`가 풀 키다. 3판은 전부 1레인이지만 `laneSpan`은 남겨 두었다(후순위 장애물이 여러 레인을 차지해도 코드 수정 없음). 새 종류(수초, 어망…)는 이 에셋과 프리팹만 추가하고 패턴에 넣으면 된다 |
| `Patterns/<Id>.asset` `SpawnPattern` | `id`, `entries[]`(obstacle, startLane, arrivalOffset 초), `manualJumpRequired`, `enabled` | 패턴 하나. `startLane`은 장애물이 차지하는 레인 중 가장 위. `arrivalOffset`은 패턴의 첫 장애물이 플레이어에 닿는 시각 기준으로 이 항목이 몇 초 뒤에 닿는지. `manualJumpRequired`는 표기일 뿐 판정은 시뮬레이터가 하며 다르면 경고를 남긴다 |
| `PatternLibrary.asset` | `patterns[]` | 사용할 패턴 목록 |
| `DifficultyCurve.asset` | `sections[4]`, 각각 AnimationCurve `speedMultiplier`, `patternGap`, `minReactionMargin`, `jumpRequiredRatio`, `tier1Weight`, `tier2Weight`, `tier3Weight`와 `spawnStopProgress` | 구간별 곡선. x는 구간 진행도 0~1 |
| `ObstacleSpawnerSettings.asset` | `library`, `difficulty`, `screenRightX` 6.4, `screenLeftX` -6.4, `playerHalfWidth` 0.35, `tickSeconds` 0.05, `safetyPadding` 0.1, `minJumpPatternGap` 2, `sectionStartGrace` 2, `retryInterval` 0.2, `maxCandidateAttempts` 4, `despawnMargin` 1, `prewarmPerObstacle` 4, `defaultSeed` 12345 | 스포너 설정. 레인 y, 플레이어 x, 스크롤 속도, 이동·점프·쿨타임은 `GameConfig`에서 읽는다 |

기본 장애물(3판 10절, 수치는 임시값):

| id | 3판 이름 | 레인 수 | 시작 레인 | 몸길이 | 충돌 길이 | 속도 배율 | 의도 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Rock | 돌 | 1 | 상/중/하 | 1.0 | 0.87 | 1.0 (물살과 같이 흐름) | 기본 회피 |
| Fish | 다른 물고기 | 1 | 상/중/하 | 1.4 | 1.2 | 1.8 (자체 속도로 연어 반대 방향으로 지나감) | 반응 시간이 짧음 |
| Log | 통나무 | 1 | 상/중/하 | 3.0 | 2.6 | 1.0 (떠내려옴) | 레인을 오래(약 0.83초) 막음 |

충돌 길이는 4번 기획서 17절 G(히트박스는 스프라이트보다 10~15% 작게)대로 몸길이의 0.85~0.90배(돌 0.87, 물고기 0.86, 통나무 0.87)로 두었고 테스트가 범위를 지킨다. 프리팹 생성기의 `BoxCollider2D` 가로 크기가 이 값이다. 세로는 레인 폭 1.1에서 0.3을 뺀 0.8이다(이웃 레인 오염 방지).

4번 기획서 05절에서 **미확정**으로 남긴 것(세 장애물의 속도 차이, 통나무 점유 시간, 장애물별 등장 확률, 조합 패턴, 등장 레인 제한, 점프 필수 사용 여부)은 전부 위 표와 패턴 목록의 임시값이다. 확정되면 코드가 아니라 `ObstacleDefinition`·`SpawnPattern`·`DifficultyCurve` 에셋만 바꾼다.

기본 패턴 28개(전부 1레인 장애물 조합):

| 묶음 | 패턴 | 티어(속도 1.0) |
| --- | --- | --- |
| 단일 | `Rock_Top/Middle/Bottom`, `Log_Top/Middle/Bottom` | 1 |
| | `Fish_Top/Middle/Bottom` | 2 (반응 여유 1.25초 < 1.5초라 승급) |
| 두 레인 동시 | `Rocks_TopBottom/TopMiddle/MiddleBottom`, `RockMiddle_LogBottom`, `LogTop_RockMiddle` | 1~2 |
| | `RockTop_FishBottom`, `FishTop_RockBottom`, `Fishes_TopMiddle`, `FishTop_LogMiddle` | 2 (물고기가 늦게 보임) |
| 시간차·연속 레인 변경 | `Rocks_TopThenBottom/BottomThenTop`(0.7초), `Fish_TopThenBottom`(0.5초) | 1~2 |
| | `LogBottom_ThenRockMiddle`, `LogTop_ThenRockMiddle`(통나무가 한 레인을 막는 동안 0.5초 뒤 돌이 가운데) | 2 |
| | `Weave_MiddleThenTopBottom`(가운데 돌, 0.9초 뒤 위·아래 돌 → 나갔다가 돌아옴) | 2 |
| 점프 필수 | `Jump_RocksAllLanes`, `Jump_FishTop_RocksMiddleBottom`, `Jump_LogBottom_RockMiddle_FishTop`(통나무 하단, 0.2초 뒤 돌 중단 + 물고기 상단), `Jump_LogMiddle_RockTop_RockBottom`(통나무 중단, 0.2/0.3초 뒤 돌 상·하단) | 3 |

점프 필수 패턴은 세 레인이 동시에 막히는 구간이 이동 잠금(0.2초)보다 길고 점프(0.8초)보다 짧아야 한다. 착수 레인(상단)은 착수 시점에 비어 있어야 하므로 통나무는 상단에 두지 않았다. 각 패턴의 격자 점프 창은 속도 1.0에서 2~4틱이다(`Jump_RocksAllLanes`가 2틱으로 가장 좁다).

## 알고리즘

### 1. 시간축 레인 점유

장애물 하나는 "레인 집합을 시각 [BlockStart, BlockEnd] 동안 막음"이다. 장애물 중심이 `spawnCenterX`에서 `speed = ScrollSpeed × 장애물 배율 × 구간 배율`로 왼쪽으로 가고, 플레이어는 `PlayerX ± PlayerHalfWidth`에 있으므로

- `BlockStart` = 장애물 충돌 앞면이 플레이어 오른쪽 면에 닿는 시각(= 도착 시각)
- `BlockEnd` = 충돌 뒷면이 플레이어 왼쪽 면을 지나는 시각
- `AppearTime` = 몸 앞면이 `ScreenRightX`를 넘는 시각(스폰 시 이미 화면 안이면 스폰 시각)

패턴은 항목마다 "도착 시각 = 패턴 도착 시각 + arrivalOffset"으로 배치한다. 스폰은 항상 지금(`now`) 하되 도착 시각에서 거꾸로 x를 계산하므로, 느린 돌은 화면 끝에, 빠른 물고기는 더 오른쪽(화면 밖)에 놓여 같은 순간에 도착한다. 패턴의 가장 이른 도착 시각은 모든 항목이 화면 밖에서 시작할 수 있는 시각이다(`PlacementBuilder.EarliestArrivalDelay`).

### 2. 플레이어 상태와 도달 가능성 전파

플레이어 상태 = (레인 0..2 또는 공중, 잠금 남은 틱, 쿨타임 남은 틱). 격자 0.05초. 규칙은 기획서 6~9절 그대로다.

- 잠금 0이고 물속이며 그 레인이 막혀 있으면 죽는다. 잠금 중(이동·공중)이면 무적.
- 잠금 0일 때 입력: 위/아래(레인은 즉시 바뀌고 0.2초 잠금, 예약 없음), 상단에서 쿨타임 0이면 점프(0.8초 공중, 착수 시 상단·쿨타임 1.2초 시작).
- 착수 순간부터 판정 재개.

`Reachability.Propagate`는 상태 집합(`StateSet`, 상태마다 "여기까지 오는 데 필요한 최소 입력 수")을 틱 단위로 전파한다. 집합이 비면 어떤 입력열로도 살 수 없다는 뜻이다. 상태 수는 4 × 16 × 25 = 1600이라 한 검사가 수만 번의 정수 연산이다.

### 3. 안전 여유 두 가지

- **입력 허용 오차 = `safetyPadding`(0.1초).** 모든 점유 구간을 앞뒤로 0.1초 늘려서 검사한다(격자 올림·내림까지 더하면 0.1~0.15초). 늘린 구간에서 길이 있으면 실제 게임에서는 그 입력을 ±0.1초 틀려도 산다. 0.15로 올리면 돌 한 줄(0.4초 봉쇄)을 점프(0.8초)로 넘는 창이 격자에서 사라져 점프 필수 패턴이 전부 해결 불가로 판정되니, 점프 창 = 점프 시간 − 봉쇄 시간 − 2×오차가 0.15초 이상 남게 둔다. "정확히 0.1초 창에 입력해야 하는" 해법은 이 단계에서 걸러진다.
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
3. 후보 = 해당 종류로 판정된 패턴 중 장애물의 사용 여부 플래그가 맞는 것. 일반 패턴은 티어 가중치로 뽑고, 가중치가 전부 0이면 티어를 무시한다.
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

| 곡선 | 의미 | 임시값 (구간 1 → 4, 시작→끝) |
| --- | --- | --- |
| `speedMultiplier` | 장애물 접근 속도 배율. 스크롤·물살은 그대로고 장애물만 빨라진다(3판 3·25절). **임시값** | 1.00→1.05, 1.10→1.25, 1.30→1.50, 1.10→1.00 |
| `patternGap` | 이전 패턴이 지나간 뒤 다음 패턴 도착까지의 여유(초). 등장 빈도. **4번 기획서 06절 추천 후보 범위(1: 2.0~2.5, 2: 1.6~2.0, 3: 1.3~1.7, 4: 2.5~3초) 안의 값이며 아직 최종 확정은 아님** | 2.5→2.0, 2.0→1.6, 1.7→1.3, 2.5→3.0 |
| `minReactionMargin` | 패턴이 다 보인 뒤 무입력으로 버텨야 하는 최소 시간(초) | 0.6, 0.5, 0.4, 0.6 |
| `jumpRequiredRatio` | 점프 필수 패턴 비율. 일반 구간에서 점프 필수 패턴을 쓸지는 **미정**(4번 16절 4항, 17절 B)이라 이 값이 스위치다. 기본은 켬이고 구간별로 0으로 두면 점프 필수 패턴이 전혀 나오지 않는다(테스트 `ZeroJumpChance_NeverSpawnsJumpRequiredPatterns`) | 0.10→0.20, 0.25, 0.30→0.35, 0.10→0.00 |
| `tier1/2/3Weight` | 일반 패턴 티어별 추출 가중치 | 1: (1.0, 0.3→0.5, 0→0.1) 단순 회피 위주 · 2: (0.6→0.5, 1.0, 0.3→0.5) 물고기·통나무·연속 레인 변경 증가 · 3: (0.3, 1.0, 1.0) 연속 회피 · 4: (1.0, 0.3→0.1, 0) |
| `spawnStopProgress` | 이 진행도부터 스폰 중단 | 1, 1, 1, 0.7 (구간 4는 15초 중 10.5초 이후 장애물 없음) |

3판 11절대로 조작 속도는 건드리지 않고 속도·간격·배치만 바꾼다. 구간 1은 간격이 넓고 티어 1(단순 1레인 회피) 위주, 2는 간격이 줄고 티어 2(물고기·통나무 조합, 연속 레인 변경) 비중이 오르며, 3은 간격이 더 줄고 티어 2·3과 점프 필수 비율이 오르고, 4는 장애물이 매우 적다가 70% 지점부터 없어진다. 물살 세기 변화는 없다.

## 통합 절차 (코어루프 에이전트가 할 일)

1. **에셋 생성.** 컴파일 오류가 없는 상태에서 `Team1004 > Generate Spawner Assets`. `Assets/GameAssets/Placeholder/Square.png`가 있으면 재사용한다. 프리팹은 `SpriteRenderer`(색: 돌 회색, 물고기 은청색, 통나무 갈색, 크기 몸길이 × 레인 폭 1.1 − 0.1) + `BoxCollider2D`(trigger, 충돌 길이 × 높이 − 0.3) + `ObstacleThing`(`Hazard.kind` = id)이다. 플레이어에 Kinematic `Rigidbody2D`가 있으므로 장애물에는 없다.
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

- **`SpawnerDefaults`의 기본값이 바뀌면(4번 기획서 반영: 간격 곡선, 충돌 길이) 이미 생성된 `Design/Spawner` 에셋에는 자동 반영되지 않는다.** `Regenerate Spawner Assets (Overwrite)`를 한 번 돌리거나 Inspector에서 `DifficultyCurve.patternGap`·`ObstacleDefinition.collisionLength`를 손으로 맞춘다. 밸런스 리포트와 테스트는 에셋이 아니라 `SpawnerDefaults`(리포트는 에셋이 있으면 에셋)를 읽으므로 둘이 어긋나면 리포트 수치가 게임과 다르다.
- `Team1004 > Generate Spawner Assets`: 없는 것만 만든다. `Regenerate Spawner Assets (Overwrite)`: 확인 후 프리팹과 Design 에셋을 기본값으로 다시 쓰고(GUID 유지), 기본값에 없는 장애물 정의·프리팹·패턴 에셋(구판 Shark/Whale과 그 패턴)은 삭제한다.
- `Team1004 > Spawner Balance Report`: `Docs/SpawnerBalance.md`를 쓴다. 패턴 표(종류·티어·최악 입력 수·반응 여유·도착·막는 시간), 구간별 코스 표(구간당 시드 20개, 3판 구간 시간 25/30/35/15초: 평균 패턴 수, 점프 필수 비율, 평균 도착 간격, 검증 거절 수, 빈 결정 수, 해결 가능 코스 수), 구간별 패턴 사용 횟수. Design 에셋이 없으면 `SpawnerDefaults`로 돈다.
- 테스트(Test Runner > EditMode > Game.Spawner.Tests): 기본 일반 패턴이 모든 레인에서 점프 없이 해결됨, 점프 패턴이 점프 필수로 판정됨, 반응 여유 ≥ 0.3초, 최대 속도에서도 해결 가능, 통나무 셋으로 세 레인을 점프보다 길게 막는 패턴 거절, 기본 장애물 전부 1레인, 쿨타임 중 전 레인 봉쇄는 생존 불가, 4구간(25/30/35/15초) × 13시드 코스 전부 해결 가능, 구간 시작 첫 패턴이 세 레인 × {잠금 없음, 이동 잠금 중} 어느 시작 상태에서도 채택되고 유예 시간 뒤에 도착하며 반응 여유 동결로도 생존 가능, 점프 필수 규칙 준수, 같은 시드 같은 코스, 다른 시드 다른 코스, 구간 4 끝에 스폰 없음, 쿨타임 중 점프 필수 미출현.

## 임시값 목록

- 장애물 몸길이·충돌 길이(몸길이 × 0.87)·속도 배율(위 표: 물고기 1.8배, 통나무 3.0 unit), 등장 레인 제한(현재 없음), 장애물별 등장 확률(패턴 티어 가중치로만 조절) — 4번 05절 미확정. 플레이어 반폭 0.35(코어루프 히트박스 0.9 × 0.5 기준), 레인 폭 1.1.
- 패턴 간격 곡선은 4번 06절 추천 후보 범위 안의 값(최종 확정 아님), 속도 배율 곡선은 임시값. 점프 필수 패턴 사용 여부 미정(기본 켬, `jumpRequiredRatio`로 끔).
- 화면 오른쪽 6.4, 왼쪽 -6.4, 회수 여유 1 unit.
- 격자 0.05초, 입력 허용 오차 0.1초, 점프 필수 최소 간격 2초, 구간 시작 유예 2.0초(`sectionStartGrace`, 요구 하한 1.5초), 재시도 0.2초, 후보 시도 4개, 프리웜 4개, 기본 시드 12345.
- 난이도 곡선 전부(위 표), 패턴 간격 정의, 점프 필수 비율 정의, 반응 여유 정의(`Docs/DesignQuestions.md`에 질문 등록).
- 패턴 시간차 0.5/0.7/0.9초, 점프 필수 패턴의 0.2/0.3초 오프셋, 티어 승급 기준 반응 여유 1.5초.
- 프리팹 색과 정렬 순서 5. 물고기 스프라이트 방향(현재 사각형이라 없음).

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

컴파일을 돌리지 못했다. 열자마자 볼 곳:

- `ObstacleThing : Hazard` — `Game.Player.Hazard`가 `public class Hazard : MonoThing`(non-sealed)일 때만 컴파일된다. `Hazard.kind`는 프리팹 생성기가 `SerializedObject.FindProperty("kind")`로 채우므로 필드 이름이 바뀌면 조용히 빈 값이 된다.
- `PoolManager.TryGetInstance(out var pool)` — `Singleton<T>`의 정적 메서드 이름을 코어루프 코드(`InputManager.TryGetInstance`)와 같게 가정했다.
- `PoolManager.TryGet(key, position, rotation, out instance, parent)`와 `Release(PooledInstance)`, `IsRegistered`, `Register(key, prefab, prewarm)` — `Core/Pool/PoolManager.cs`의 현재 시그니처에 맞췄다.
- `GameConfigValues.LaneCount`, `GetLaneY`, `PlayerX`, `ScrollSpeed`, `LaneMoveDuration`, `JumpDuration`, `JumpCooldown`, `StageLength` — 현재 `Config/GameConfigValues.cs`와 일치한다.
- `Module`이 전역 타입이라 `System.Reflection`을 네임스페이스 안에서 임포트하면 안 된다(현재 안 함).
- `Math.Clamp`, `Array.Fill`은 .NET Standard 2.1 API다. 프로젝트 API 레벨이 `.NET Standard 2.1`(ProjectSettings `apiCompatibilityLevel: 6`)인 것을 확인했다.
- `[SerializeField] private LaneFlags allowedStartLanes` — `[Flags]` enum은 Inspector에 마스크 드롭다운으로 나온다. `(int)` 캐스트로 마스크를 그대로 쓴다.
- `DifficultySample`/`SectionDifficulty`의 `in` 매개변수 생성자 — 람다 안에서 `new SectionDifficulty(start, end, stop)`으로 호출한다.
- 생성기의 `sprite.bounds.size`는 PPU 100 기준 0.64 unit이다. 프리팹은 이 값을 나눠 스케일을 맞추므로 다른 PPU여도 크기는 맞는다.
- 런타임 성능: `Initialize`에서 패턴 15개 × 시작 상태 6개 × 이진 탐색을 돌린다(수십 ms 예상). 프레임 중 결정 한 번은 전파 1~4회(각 수만 연산)라 문제없어야 하지만, 히치가 보이면 `maxCandidateAttempts`를 줄이거나 분석을 에디터 베이크로 옮긴다.
- 자체 속도 이동은 `ObstacleRuntimeModule.Advance`가 `transform.position`을 직접 옮긴다. `ScrollModule`은 `localPosition`을 옮기므로 둘이 더해진다. `ScrollRoot`가 회전·스케일되어 있으면 어긋난다(현재 없음).
- 1레인 장애물의 콜라이더 높이는 1.1 − 0.3 = 0.8이라 이웃 레인의 히트박스(높이 0.5)와 0.45 여유가 있다. `laneSpan` 2 이상은 두 레인 y의 평균에 놓이며 코드 경로는 남아 있지만 3판 데이터에는 없다.

## 가정

- `Game.Player.Hazard : MonoThing`(non-sealed), `LanePlayer.CurrentLane/IsMoving/IsAirborne` — 현재 코드와 일치.
- `PlayFlow`가 `Initialize`/`BeginSection`/`Advance`/`Stop`을 부른다. `Game.Play`가 `Game.Spawner`를 참조한다(역방향 없음).
- 플레이 씬의 `ScrollRoot`는 위치 (0,0,0), 회전·스케일 없음.
- 점프 모듈은 착수 시 `LanePlayer.SetAirborne(false)`와 `SnapToLane(0)`을 한다(기획서 8절). 공중 판정은 `IsAirborne`으로 받는다.
- 구간(게임 진행 1~4)의 시작·끝은 PlayFlow가 판단한다. 스포너는 `BeginSection`으로 통보받고 `IsSectionExhausted`로 답한다.
