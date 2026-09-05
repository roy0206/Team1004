# 파티클 (Game.Particles)

물속 분위기용 가벼운 스프라이트 파티클이다. 물 전체에 흐르는 **흐름선·기포**와 움직이는 물체 뒤의 **기포 꼬리**를 만든다. 기획서 3판 3절(물 흐름은 항상 왼쪽), 6번 정리 2절(곰 보스 30초 동안 지형은 멈추지만 수면 파동·물속 흐름선·기포·작은 반복 물 효과는 계속 움직인다)을 반영했다.

**Unity `ParticleSystem`을 쓰지 않는다.** 프리팹 YAML을 손으로 고치는 작업 방식(에디터가 열려 있어 배치 생성기를 돌리지 않는다)에서 `ParticleSystem`의 YAML은 수백 줄에 커브 곡선까지 들어가 손으로 다룰 수 없다. 대신 프리팹에 **미리 놓아 둔 `SpriteRenderer` 자식 몇 개**를 모듈 하나가 돌려 쓰는 방식이다. 파티클 수가 고정이므로 런타임 생성이 없고(사용자 규칙: 풀 외 런타임 생성 금지) 프레임당 할당도 없다.

## 파일

| asmdef | 경로 | 타입 | 책임 |
| --- | --- | --- | --- |
| `Game.Particles` | `Assets/Scripts/Particles/` | `ParticleProfile : ScriptableObject` | 방출기 하나의 모든 수치. `Assets/GameAssets/Design/Particles/*.asset` |
| | | `ParticleSpawnMode` | `RateOverTime` / `RateOverDistance` / `BurstOnly` |
| | | `ParticleMath` | 순수 정적 계산(알파·크기·드리프트·방출 개수·경계). Unity 오브젝트 없음. 테스트 대상 |
| | | `ParticleRandom` | xorshift32 구조체. 할당 없음. 같은 시드면 같은 수열 |
| | | `SpriteEmitterModule : Module` | `Ticks = Update`. 자식 `SpriteRenderer` 배열을 파티클 풀로 굴린다 |
| | | `BubbleTrailThing : MonoThing` | 범용 꼬리 호스트. 장애물·플레이어가 쓴다 |
| `Game.Particles.Tests` | `Assets/Scripts/Particles/Tests/` | `ParticleMathTests` | EditMode 18개 |
| `Game.Environment` | `Assets/Scripts/Environment/` | `UnderwaterFlowThing : MonoThing` | 물 전체 흐름선·기포 호스트. 방출기 2개를 만든다 |
| | | `UnderwaterFlowModule : Module` | `Ticks = Update`. 스크롤 속도·수면 y를 두 방출기에 밀어 넣는다 |

asmdef 참조: `Game.Particles` → `Core.Modules`, `Core.Foundation`, `Game.Config`. **아무 게임 어셈블리도 참조하지 않는다.** 반대로 `Game.Environment`·`Game.Player`·`Game.Spawner`가 `Game.Particles`를 참조한다(한 방향, 순환 없음).

## `SpriteEmitterModule`

```csharp
new SpriteEmitterModule(Transform emitRoot, ParticleProfile profile, SpriteRenderer[] renderers, uint seed)
```

| 멤버 | 설명 |
| --- | --- |
| `bool IsEmitting` | 새 파티클을 낼지. 끄면 이미 떠 있는 것은 수명대로 사라진다 |
| `float SpeedScale` | 시간 배율(기본 1). 파티클 나이·속도·가속·드리프트 전부에 곱한다. 단차 QTE 슬로모나 보스 월드 정지에서 밖에서 낮춘다 |
| `Vector2 ExternalVelocity` | 호스트가 주는 바깥 흐름(월드 u/s). 각 파티클에 `profile.flowFactor`를 곱해 더한다 |
| `float SurfaceY` | 수면 y. `popAtSurface`가 켜진 프로파일은 여기서 터진다 |
| `int Capacity` / `int AliveCount` | 슬롯 수 / 살아 있는 수 |
| `void Burst(int count)` | 즉시 `count`개 방출(빈 슬롯까지만) |
| `void Clear()` | 전부 죽이고 렌더러를 끈다 |
| `void ResetMovementSample()` | 이동 거리 샘플러를 리셋한다(순간이동 뒤) |
| `void Step(float deltaTime)` | 한 프레임. `OnUpdate`가 `Step(Time.deltaTime)` |

동작 순서는 `이동 샘플 → 나이·이동 적분 → 방출 → 렌더` 한 루프다.

```text
scaled   = deltaTime × SpeedScale
속도     += acceleration × scaled
위치     += (속도 + constantVelocity + ExternalVelocity × flowFactor + (drift, 0)) × scaled
drift    = driftAmplitude × sin(2π × (time × driftFrequency + phase))
알파     = alpha × min(t/fadeIn, (1−t)/fadeOut, 1)        t = 나이 / 수명
가로     = size × lerp(1, endSizeScale, t)
localScale = 가로 / (스프라이트 원본 가로 × 방출기 lossyScale)
```

- **파티클 위치는 월드 좌표다.** 자식이지만 `transform.position`으로 직접 놓는다. 그래서 장애물이 왼쪽으로 흘러가도 이미 나온 기포는 그 자리에 남아 「꼬리」로 읽힌다. `emitCenter`·`emitSize`도 방출기 **월드 위치 기준 월드 단위**이지 로컬 단위가 아니다(부모 스케일이 0.22인 장애물에서도 수치를 그대로 읽을 수 있다).
- **`localScale`은 부모 스케일을 상쇄한다.** `OnAttached`에서 방출기의 `lossyScale.x`와 각 렌더러 스프라이트의 원본 가로(`sprite.bounds.size.x`)를 캐시하므로, 프로파일의 `sizeRange`는 **화면에서 보이는 가로(월드 unit)**다. 임시 스프라이트는 기포 0.16, 흐름선 0.32 × 0.04 unit이다.
- **`RateOverDistance`는 방출기 월드 이동 거리로 센다.** 한 프레임에 1.5 unit 넘게 움직이면 순간이동으로 보고 그 프레임 거리를 0으로 친다(풀 스폰·레인 스냅에서 기포가 터지지 않는다).
- 슬롯이 다 차면 방출을 조용히 건너뛴다. 누적기는 남은 슬롯 수를 넘으면 0으로 버린다.
- 프레임당 할당이 없다. 파티클은 구조체 배열, 렌더러·트랜스폼은 생성 시 캐시한다.

## `ParticleProfile` (`Assets/GameAssets/Design/Particles/*.asset`)

| 필드 | 뜻 |
| --- | --- |
| `sprite` | 비어 있지 않으면 `OnAttached`에서 모든 렌더러에 넣는다. 프리팹 스프라이트를 덮는다 |
| `color` | RGB만 쓴다. 알파는 `alpha`와 수명 곡선이 정한다 |
| `sortingOrder` | 모든 렌더러에 적용 |
| `spawnMode` | 0 `RateOverTime`(초당) · 1 `RateOverDistance`(이동 1 unit당) · 2 `BurstOnly` |
| `rate` | 위 단위당 개수 |
| `emitCenter`, `emitSize` | 방출 사각형. 방출기 월드 위치 기준 **월드 단위** |
| `lifetimeRange` | 수명(초) |
| `speedRange`, `directionRange` | 초기 속도 크기와 각도(도, 0 = +x, 90 = +y). 왼쪽은 180 |
| `sizeRange` | 화면 가로(unit) |
| `endSizeScale` | 수명 끝에서의 크기 배율. 기포는 1보다 크게(부풀며 올라간다) |
| `alpha`, `fadeIn`, `fadeOut` | 최고 알파와 페이드 구간(수명 비율) |
| `acceleration` | 부력·중력(u/s²). 기포는 +y |
| `constantVelocity` | 프로파일 고정 흐름(u/s). 플레이어 꼬리처럼 호스트가 화면에 고정된 경우 여기에 왼쪽 값을 준다 |
| `flowFactor` | 호스트가 주는 `ExternalVelocity`를 얼마나 탈지 |
| `driftAmplitude`, `driftFrequency` | 가로 흔들림(u/s, Hz) |
| `popAtSurface`, `surfaceMargin` | 켜면 `SurfaceY − surfaceMargin` 위로 올라간 순간 사라진다 |
| `killOutsideBounds`, `boundsMargin` | 켜면 방출 사각형을 `boundsMargin`만큼 넓힌 밖으로 나가면 사라진다 |
| `burstSmall`, `burstLarge` | `BubbleTrailThing.BurstSmall()` / `BurstLarge()`가 쓰는 개수 |

지금 있는 프로파일 6개.

| 에셋 | 쓰는 곳 | 스프라이트 | 정렬 | 방출 | 수명 | 크기(가로) | 알파 | 가속 | 흐름 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `Flow_Streak` | 물 전체 흐름선 | Streak | **-4** | 시간 3.5/s | 2.4~3.6 | 0.55~1.30 | 0.22 | 0 | `flowFactor` **0.6** |
| `Flow_Bubble` | 물 전체 기포 | Bubble | **-4** | 시간 2.5/s | 2.5~4.5 | 0.05~0.11 | 0.30 | (0, 0.10) | `flowFactor` **0.35** |
| `Trail_Fish` | `Fish.prefab` | Bubble | 4 | 거리 2.2/u | 0.45~0.75 | 0.035~0.075 | 0.45 | (0, 0.25) | — |
| `Trail_Log` | `Log.prefab` | Bubble | 4 | 거리 1.1/u | 0.50~0.85 | 0.040~0.085 | 0.40 | (0, 0.22) | — |
| `Trail_Rock` | `Rock.prefab` | Bubble | 4 | 거리 0.5/u | 0.40~0.70 | 0.030~0.065 | 0.32 | (0, 0.30) | — |
| `Trail_Player` | `Play.unity` Player | Bubble | 9 | 시간 2.5/s | 0.60~1.00 | 0.040~0.090 | 0.50 | (0, 0.30) | `constantVelocity` (-1.2, 0) |

방출 밀도는 슬롯 수와 맞춘다. 평균 동시 개수 = `rate × 평균 수명`(거리 방출은 `rate × 속도 × 평균 수명`)이고, 이 값이 슬롯 수보다 작아야 꼬리가 끊기지 않는다.

| 방출기 | 슬롯 | 속도 | 평균 동시 개수 |
| --- | --- | --- | --- |
| `Flow_Streak` | 14 | — | 3.5 × 3.0 = **10.5** |
| `Flow_Bubble` | 10 | — | 2.5 × 3.5 = **8.8** |
| `Trail_Fish` | 8 | 5.0 u/s | 2.2 × 5.0 × 0.60 = **6.6** |
| `Trail_Log` | 8 | 3.2 u/s | 1.1 × 3.2 × 0.68 = **2.4** |
| `Trail_Rock` | 8 | 4.0 u/s | 0.5 × 4.0 × 0.55 = **1.1** |
| `Trail_Player` | 12 | — | 2.5 × 0.8 = **2.0** + 착수 버스트 10 |

## 임시 스프라이트

| 파일 | 크기 | 내용 |
| --- | --- | --- |
| `Assets/GameAssets/Placeholder/Particle_Bubble.png` | 16 × 16 | 흰색 소프트 원. 가운데는 옅고 테두리(반지름 80% 부근)가 밝은 「기포 림」. PPU 100이라 0.16 unit |
| `Assets/GameAssets/Placeholder/Particle_Streak.png` | 32 × 4 | 흰색 가로 줄기. 양끝·위아래로 부드럽게 사라진다. 0.32 × 0.04 unit |

둘 다 흰색이고 색은 프로파일의 `color`가 준다. `.meta`는 `Ledge_Cascade.png.meta`를 본떠 손으로 썼다(Sprite / Single / PPU 100 / pivot 중앙 / Wrap Repeat / 밉맵 없음 / 무압축). 아트가 오면 PNG만 갈아 끼우면 되고, 원본 가로가 바뀌어도 `sizeRange`가 월드 가로라 프로파일은 그대로다. 아트 요청은 `Docs/Requests.md`에 남겼다.

## 프리팹에 방출기를 붙이는 법 (YAML)

에디터 생성기가 없다. 프리팹 YAML을 직접 쓴다.

1. **호스트 노드**를 만든다. `GameObject`(이름 `Trail`) + `Transform` + `MonoBehaviour`(`BubbleTrailThing`, 스크립트 guid `b6c8abc6c9bcf29c161045ca6358a3ca`).
   ```yaml
   m_EditorClassIdentifier: Game.Particles::Game.Particles.BubbleTrailThing
   profile: {fileID: 11400000, guid: <프로파일 asset guid>, type: 2}
   particleRoot: {fileID: 0}      # 비우면 자기 Transform의 자식들을 쓴다
   emitOnAwake: 1
   ```
2. **`Transform.m_LocalPosition`은 부모 로컬 단위다.** 부모(장애물 루트)의 `localScale`이 1이 아니면 원하는 월드 오프셋을 스케일로 나눈다. `x = 월드 오프셋 / 부모 스케일`.
3. **파티클 자식**을 N개 만든다. 각각 `GameObject`(`P00`…) + `Transform`(위치 0, 스케일 1) + `SpriteRenderer`. 렌더러는 **`m_Enabled: 0`**으로 둔다(모듈이 살아 있는 동안만 켠다). `m_Sprite`는 프로파일과 같은 것을 넣어 두면 에디터에서도 보인다.
4. 호스트 `Transform`을 부모의 `m_Children`에, 파티클 `Transform`들을 호스트의 `m_Children`에 넣는다. `m_Father`도 같이 맞춘다.
5. 호스트를 부르는 쪽(예: `ObstacleThing.trail`)의 `MonoBehaviour`에 `trail: {fileID: <BubbleTrailThing fileID>}`를 넣는다.

`BubbleTrailThing`은 `Initialize()`에서 **직계 자식만** 훑어 `SpriteRenderer`를 모은다(손자는 보지 않는다). 슬롯 수를 늘리려면 자식을 더 만들면 되고 코드는 손대지 않는다.

## 호스트 연결

### 장애물 (`ObstacleThing`)

`[SerializeField] private BubbleTrailThing trail;` 한 줄과 풀 훅 두 개뿐이다.

| 시점 | 하는 일 |
| --- | --- |
| `OnSpawned` | `trail.Clear()` → `trail.SetEmitting(true)`. `Clear`가 이동 샘플러까지 리셋하므로 풀에서 나오며 순간이동한 거리가 방출로 잡히지 않는다 |
| `OnReleased` | `trail.SetEmitting(false)` → `trail.Clear()`. 재사용 때 이전 기포가 딸려 오지 않는다 |

`ObstacleThing.ClearModules()`는 자기 모듈만 지운다. 꼬리는 자식 `MonoThing`이라 영향이 없다.

꼬리 노드는 장애물의 **뒤쪽(오른쪽) 끝**에 둔다. 장애물은 왼쪽으로 흐르므로 오른쪽이 후미다.

| 프리팹 | 부모 스케일 | 몸길이 | 월드 오프셋 | `Trail.m_LocalPosition.x` |
| --- | --- | --- | --- | --- |
| `Rock.prefab` | 0.4365079 | 2.4444 | +1.2222 | **2.8** |
| `Fish.prefab` | 0.1287554 | 0.90 | +0.45 | **3.495** |
| `Log.prefab` | 0.338983 | 1.80 | +0.90 | **2.655** |

### 연어 (`LanePlayer`, `Play.unity`)

| 시점 | 하는 일 |
| --- | --- |
| `Awake` | `trail?.Initialize()` |
| `OnModuleLaneChanged` | `trail?.BurstSmall()` (프로파일 `burstSmall` 6) |
| `OnModuleJumped` | `trail?.SetEmitting(false)` — 공중(수면 위)에서는 기포가 안 난다 |
| `OnModuleLanded` | `trail?.SetEmitting(true)` + `trail?.BurstLarge()` (프로파일 `burstLarge` 10) |
| `CancelJump` | `trail?.SetEmitting(true)` (점프를 도중에 끊는 경로) |

꼬리 노드는 연어 **꼬리쪽(왼쪽)**이다. Player `localScale` 0.29, 월드 오프셋 -0.725 → `m_LocalPosition.x = -2.5`. 연어는 화면에 고정되어 있으므로 기포가 뒤로 밀리는 그림은 프로파일의 `constantVelocity` (-1.2, 0)이 만든다.

## 물 전체 흐름 (`UnderwaterFlowThing`)

`Environment.prefab`의 새 자식 `UnderwaterFlow`다. 자세한 배치·정렬은 `Docs/Environment.md` 「물속 흐름 파티클」.

```csharp
void Initialize();                    // Awake와 EnvironmentThing.Initialize가 부른다. 두 번 불러도 안전
SpriteEmitterModule Streaks { get; }
SpriteEmitterModule Bubbles { get; }
float FlowSpeed { get; }              // 이번 프레임에 두 방출기에 넣은 왼쪽 흐름 속도
float HoldSpeedScale { get; }
float AmbientSpeedScale { get; set; } // 바깥에서 낮추는 시간 배율. 기본 1
void SetEmitting(bool);
void Clear();
```

`EnvironmentThing.AmbientSpeedScale`이 그대로 위임한다(프리팹의 `underwaterFlow` 참조). `PlayFlow`는 `environment`를 이미 들고 있으므로 코드 한 줄로 닿는다.

```text
scroll  = EnvironmentThing.Speed × ScrollScale                       (월드 정지 중에는 0)
floor   = GameConfig.ScrollSpeed × ScrollScale × holdSpeedScale(0.35)
FlowSpeed = max(scroll, floor)
ExternalVelocity = (−FlowSpeed, 0)
```

**바닥값(`floor`)이 핵심이다.** `PlayFlow.ApplyWorldSpeedScale`이 월드를 세울 때 `EnvironmentThing.Speed`를 0으로 만들지만, 6번 정리 2절은 곰 보스 30초 동안 물속 흐름선·기포는 계속 움직이라고 한다. `max`를 쓰면 `SetScrolling(false)`와 무관하게 물살이 기본 속도의 35%로 남는다. 단차 QTE 슬로모(`WorldSpeedScale` 0.12)에서도 같은 바닥값이 걸린다.

## 성능

- 파티클 총합은 화면에 최대 24(흐름) + 활성 장애물 15 × 평균 2~7 + 연어 12 정도다. 전부 그냥 `SpriteRenderer`이고 같은 머티리얼·같은 두 텍스처라 배칭된다.
- 매 프레임 하는 일은 파티클마다 `Vector2` 덧셈 몇 번과 `transform.position`/`localScale`/`color` 쓰기다. 할당은 0이다.
- 죽은 파티클은 `renderer.enabled = false`라 드로우 콜이 없다.
- 프로파일링은 하지 않았다(에디터가 열려 있어 배치 실행을 하지 않았다).

## 검증

| 단계 | 방법 | 결과 |
| --- | --- | --- |
| 컴파일 | 오프라인 `csc` 전 어셈블리 빌드 | `assemblies compiled: 43  failed: 0` |
| 테스트 | `ParticleMathTests` 18개(EditMode) | 작성만 했고 실행하지 않았다(에디터가 열려 있다) |
| 프리팹 YAML | 스크립트 검사 | `Environment.prefab` 179 오브젝트 / `Rock`·`Fish`·`Log` 33 / `Play.unity` 287. fileID 중복 0, `m_Father`↔`m_Children` 왕복 일치, 컴포넌트 ↔ GameObject 역참조 일치, 새 GUID 전부 해석 |

## 임시값과 확신 없는 지점

- 방출량·수명·크기·알파는 전부 화면 구성에서 정한 임시값이다. 기획서에 물속 파티클 수치가 없다.
- **기포 꼬리는 물리적으로 맞지 않는다.** 돌·통나무는 물살에 실려 떠내려가므로 실제로는 기포도 같이 움직여 꼬리가 생기지 않는다. 월드 좌표에 기포를 남기는 쪽이 「지나갔다」는 느낌을 주므로 연출로 골랐다. 어색하면 `Trail_Rock`·`Trail_Log`의 `rate`를 0으로 두면 된다.
- **연어 꼬리의 `constantVelocity` -1.2**는 흐름선(1.2 u/s)과 같은 값이다. 물살(장애물 4 u/s)과는 다르다. 기포가 너무 빨리 뒤로 빠지면 줄이고, 연어에 붙어 있는 것처럼 보이면 늘린다.
- **`holdSpeedScale` 0.35**는 6번 정리 2절의 「계속 움직임」을 수치로 옮긴 것이다. 기획서에 배율이 없다.
- 정렬 -4는 강바닥(-5) **앞**이다. `Docs/Environment.md` 「물속 흐름 파티클」의 설명을 본다.
- `Time.timeScale`이 0이면 `Time.deltaTime`도 0이라 파티클이 멈춘다(일시정지와 자동으로 맞는다).
- 에디터에서 실행 확인은 하지 못했다.
