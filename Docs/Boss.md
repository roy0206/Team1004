# 보스 (Game.Boss)

FSM 기반 보스 베이스와 세 보스(낚싯줄, 바다코끼리, 폭포) 구현이다. 보스는 "피한다, 버틴다, 통과한다"이며 조작은 ↑/↓/점프뿐이다. 세 보스는 같은 30초 형식(예고 1.2초 → 공격 0.6초 → 휴식 0.6초 → 반복 → 30초 종료 → 클리어, 3레인 전체 공격만 예고 1.5초)을 쓰고, 한 번 맞으면 실패해 해당 보스 처음부터 재시작한다.

## v4 반영 (2026-09-06)

기획서 v4 `10_보스전_공통규칙`·`11~13`·`14_충돌_실패_재시작_체크포인트`·`15_인게임HUD_히트박스_안내UI`로 바뀐 것.

| 항목 | 이전 | v4 |
| --- | --- | --- |
| 일반 예고 | 0.8초 | **1.2초** (`GameConfig.bossTelegraphDuration`) |
| 3레인 전체 예고 | 1.0초 | **1.5초** (`GameConfig.bossFullLaneTelegraphDuration`, 이제 `useGameConfigTiming`을 탄다) |
| 공격 / 휴식 | 0.6 / 0.6 | 그대로 |
| 판정 범위 | 공격체 콜라이더(바늘 0.5×0.6, 몸통 1.6×h, 급류 3×0.9, 돌 0.7) | **레인 띠와 같은 박스**. 레인마다 화면 폭 `bossLaneBandWidth`(12.8) × `bossLaneBandHeight`(0.9), 새 `LaneHazard`가 소유 |
| 히트박스 표시 | 레인 띠 + 공격체 프레임(`HitboxView`) | **레인 띠만**. `HitboxView`·`HitboxViewModule`·`HazardHitbox` 삭제 |
| 낚싯바늘 | 플레이어 x에서 수직 하강 | **오른쪽 → 왼쪽 트롤링 스윕**(줄이 뒤로 기울고 위아래로 흔들린다) |
| 바다코끼리 이동 | 보스 루트 transform | **`Body` 자식만** 이동(루트가 움직이면 띠·판정이 같이 끌려간다) |

작성 시에는 오프라인 Roslyn 컴파일만 확인했다. 통합 패스(2026-09-06)에서 `CoreLoopSetup.Regenerate`(내부 `BossAssetSetup.Generate(true)`)로 프리팹 4개를 다시 만들고 EditMode 전체와 PlayMode(`DebugJumpTests` 3개 포함)를 통과했다(`Docs/CoreLoop.md` 「통합 검증 결과 (2026-09-06)」). 눈으로 보는 확인은 「검증하지 못한 위험 지점」과 코어루프 문서의 수동 체크리스트를 따른다.

## 구성

| 경로 | asmdef | 내용 |
| --- | --- | --- |
| `Assets/Scripts/Boss/` | `Game.Boss` (참조: `Core.Modules`, `Core.Foundation`, `Game.StateMachine`, `Game.Player`, `Game.Config`) | 베이스, 선택 헬퍼, 보스 3종 |
| `Assets/Scripts/Boss/Integration/` | `Game.Boss.Integration` (참조: `Game.Boss`, `Game.Play`, `Game.Player`, `Core.Foundation`, `Core.Modules`) | `BossDirector`. `PlayFlow.BossHandler`를 채우는 유일한 접점 |
| `Assets/Scripts/Boss/Editor/` | `Game.Boss.Editor` (Editor 전용) | `BossAssetSetup`: 메뉴 `Team1004/Generate Boss Assets`, `Regenerate Boss Prefabs (Overwrite)` |
| `Assets/Scripts/Boss/Tests/` | `Game.Boss.Tests` (EditMode) | 순수 C# 부분 테스트 |
| `Assets/GameAssets/Design/Boss/` | 데이터 | `FishingLineBossData.asset`, `WalrusBossData.asset`, `WaterfallBossData.asset` |
| `Assets/GameAssets/Boss/` | 프리팹 | `FishingLineBoss.prefab`, `WalrusBoss.prefab`, `WaterfallBoss.prefab`, `BossSet.prefab` |

의존 방향: `Game.Boss`는 `Game.Play`·`Game.Spawner`를 참조하지 않는다. `Game.Boss.Integration`만 `Game.Play`를 참조하고, `Game.Play`는 보스를 모른다(`BossHandler` 델리게이트만 노출). 순환 없음.

DOTween: `Assets/Plugins/Demigiant/DOTween/DOTween.dll`은 자동 참조 플러그인이라 asmdef에 적지 않아도 `using DG.Tweening;`으로 쓴다. 단 **`SpriteRenderer.DOFade`/`DOColor` 같은 모듈 확장은 `DOTween/Modules/*.cs`(Assembly-CSharp-firstpass)에 있어 asmdef 어셈블리에서 컴파일되지 않는다.** `Transform.DOMoveX` 등 코어 확장과 `DOTween.To`/`DOTween.ToAlpha`만 쓴다. 자세한 것은 `Docs/Cutscene.md` 「DOTween 참조 방식」.

### 파일

| 파일 | 타입 | 역할 |
| --- | --- | --- |
| `BossOutcome.cs` | enum | `None`, `Passed`, `Failed` |
| `BossStage.cs` | enum | `Idle`, `Ready`, `Active`, `Finished` |
| `BossLifecycle.cs` | 순수 C# | 생명주기 가드. `BossThing`이 내부에서 쓰고 테스트 대상 |
| `BossContext.cs` | 순수 C#(Unity 타입 보유) | `Player`(필수), `ScrollRoot`, `Camera`. PlayFlow 쪽이 채워 넘긴다 |
| `BossThing.cs` | `MonoThing` 호스트(abstract) | 비제네릭 공통 표면. 생명주기 API·이벤트·헬퍼 |
| `BossThingOfT.cs` | `BossThing<TSelf, TKey>` | CRTP. `StateMachineModule<TSelf, TKey>` 소유, `BuildStates` |
| `BossTimer.cs` | 순수 C# | 30초 카운트다운. `Remaining`을 HUD가 읽는다 |
| `BossAttackTiming.cs` | 순수 C# struct | 예고/공격/회복 초 |
| `BossAttackStep.cs` | enum | `Idle`, `Telegraph`, `Attack`, `Recovery` |
| `TelegraphedAttackPhase.cs` | `TimedState` 파생(abstract, 순수 C#) | 예고→공격→회복 반복. 타이머 만료 뒤 사이클 경계에서 `Next`로 |
| `BossLanes.cs` | static | 레인 비트마스크 헬퍼 |
| `BossPattern.cs` | `[Serializable]` | 패턴 하나: 라벨, 레인 마스크, 가중치, 점프 필수 |
| `BossPatternSelector.cs` | 순수 C# | 시드 가능한 가중치 랜덤. 같은 패턴 연속 허용 여부 |
| `BossData.cs` | `ScriptableObject`(abstract) | 공통 데이터: 30초·예고/공격/회복·등장/퇴장·반복 허용·시드·패턴 목록 |
| `LaneTelegraph.cs`, `LaneTelegraphModule.cs` | `MonoThing` + `Module` | 레인 위험 표시(빨간 띠, 알파 펄스). `ApplyLayout`으로 띠 크기·위치를 `GameConfig`에서 다시 잡는다 |
| `LaneHazard.cs` | `MonoThing` (모듈 없음) | **레인 띠와 같은 크기의 실제 판정.** 레인마다 `Hazard` + `BoxCollider2D`. 「레인 띠 = 판정」 |
| `PatternBoss.cs` | `PatternBoss<TSelf, TKey, TData>` | 데이터·타이머·텔레그래프·선택기를 묶은 선택 계층 |
| `CompleteOnTimeoutState.cs` | `TimedState` 파생 | 시간이 지나면 `Complete(outcome)` |
| `FishingLineBoss*.cs` | 보스 1 | 낚싯줄 |
| `WalrusBoss*.cs` | 보스 2 | 바다코끼리 |
| `WaterfallBoss*.cs` | 보스 3 | 폭포 |
| `Integration/BossDirector.cs` | `MonoThing` 호스트 | 구간 번호 → 보스 매핑, `PlayFlow.BossHandler` 연결, 타이머 UI 갱신 |

## 팀 규칙 적용

- 씬 오브젝트는 전부 `MonoThing` 호스트다. `BossThing`, `LaneTelegraph`, `BossDirector`가 호스트이고 `StateMachineModule`, `LaneTelegraphModule`이 모듈이다. 호스트는 `Update`/`LateUpdate`/`FixedUpdate`/`OnDestroy`를 선언하지 않는다(`BossThing<TSelf,TKey>`가 `OnThingLateUpdate`를 쓴다. 파생이 오버라이드하면 `base`를 부른다).
- 런타임 `Instantiate`·`new GameObject` 없음. 공격체(낚싯바늘, 바다코끼리 몸, 급류·돌 각 레인 1개)와 판정 밴드(`LaneHazard/Band{lane}`)는 프리팹 안에 미리 자식으로 두고 위치·활성화로 켜고 끈다. 개수가 고정(레인 수)이라 풀은 쓰지 않았다.
- 비동기는 `Awaitable`(`BossDirector.RunBossAsync`)뿐. 보스 내부 시간 진행은 `StateMachineModule` 틱과 DOTween이다.
- 조정값은 `Assets/GameAssets/Design/Boss/*.asset`과 `GameConfig.asset`에 있다. 코드 상수는 없다(생성기의 임시 크기·색만 예외).
- 소스 주석 없음. 설명은 이 문서.

## 베이스 API

### `BossThing : MonoThing` (abstract, 비제네릭)

PlayFlow·Director가 보는 공통 표면이다. 패턴 형식, 데이터 형식, `Attack()` 같은 것을 강제하지 않는다.

```csharp
public abstract class BossThing : MonoThing
{
    public BossContext Context { get; }
    public BossStage Stage { get; }
    public BossOutcome Outcome { get; }
    public bool IsInitialized { get; }      // Stage != Idle
    public bool IsActive { get; }           // Stage == Active
    public bool IsFinished { get; }         // Stage == Finished
    public bool DeactivateOnFinish { get; set; }   // [SerializeField] 기본 true
    public virtual BossTimer Timer => null; // 30초 타이머를 쓰는 보스가 오버라이드
    public virtual string DisplayName => name;   // 타이머 UI에 표시. PatternBoss는 BossData.DisplayName

    public LanePlayer Player { get; }       // Context.Player
    public GameConfigValues Config { get; } // GameConfig.Current
    public int LaneCount { get; }
    public int MiddleLane { get; }          // LaneCount / 2
    public int PlayerLane { get; }          // 플레이어 없으면 MiddleLane
    public bool IsPlayerVulnerable { get; }
    public float GetLaneY(int lane);

    public event Action Began;
    public event Action<BossOutcome> Finished;

    public void Initialize(BossContext context);
    public bool Begin();
    public bool Complete(BossOutcome outcome);
    public bool Abort();
    public void ResetBoss();

    protected virtual void OnInitialize();
    protected virtual void OnBegin();
    protected virtual void OnComplete(BossOutcome outcome);
    protected virtual void OnReset();
}
```

생명주기와 규칙:

| 호출 | 허용 단계 | 동작 |
| --- | --- | --- |
| `Initialize(context)` | Idle·Ready·Finished | `context` null이면 `ArgumentNullException`. Active면 `InvalidOperationException`(먼저 `Abort`/`ResetBoss`). 이미 초기화돼 있으면 `ResetBoss()`를 먼저 돌린다. 이후 Stage=Ready, `OnInitialize()`. **GameObject는 활성화하지 않는다**(비활성 상태로 호출해도 된다) |
| `Begin()` | Ready | Idle이면 `InvalidOperationException`, Active·Finished면 `false`(무시). Stage=Active → `gameObject.SetActive(true)` → `OnBegin()` → FSM 부착(초기 상태 `OnEnter`) → `Began`. `OnBegin`이나 초기 상태에서 `Complete`하면 그 뒤 단계는 건너뛰고 `Began`도 발생하지 않는다 |
| `Complete(outcome)` | Active | `None`이면 `ArgumentException`. Active가 아니면 `false`. Stage=Finished → FSM 분리(현재 상태 `OnExit`) → `OnComplete` → `Finished(outcome)` → `DeactivateOnFinish`면 `SetActive(false)` |
| `Abort()` | Ready·Active | `Complete(Failed)`와 같은 경로. Ready에서 부르면 `Began` 없이 `Finished(Failed)`가 난다 |
| `ResetBoss()` | 아무 때나 | FSM 분리, Stage=Idle, Context=null, `OnReset()`, 비활성화. **이벤트를 내지 않는다**(체크포인트 재시작용) |

`Began`·`Finished` 핸들러의 예외는 잡아서 `Debug.LogException`하고 보스 정리는 계속한다. `Initialize` 두 번 호출은 명시적 재초기화다(같은 컨텍스트라도 `ResetBoss`가 돈다).

### `BossThing<TSelf, TKey> : BossThing`

```csharp
public abstract class BossThing<TSelf, TKey> : BossThing where TSelf : BossThing<TSelf, TKey>
{
    protected StateMachineModule<TSelf, TKey> Fsm { get; }   // Begin~Complete 사이에만 non-null
    protected StateMachine<TSelf, TKey> Machine { get; }
    public bool HasState { get; }
    public TKey CurrentKey { get; }
    public float TimeInState { get; }
    public string StateLabel { get; }
    public event Action<TKey, TKey> StateChanged;

    protected abstract TKey InitialKey { get; }
    protected abstract void BuildStates(StateMachineModule<TSelf, TKey> fsm);
    protected virtual ModuleTick FsmTicks => ModuleTick.Update;
    protected virtual bool UseUnscaledTime => false;

    public void ChangeState(TKey key);   // Active가 아니면 InvalidOperationException
}
```

- `Begin`마다 `StateMachineModule`을 **새로 만든다**. `BuildStates`에서 `fsm.Add`로 상태를 전부 등록하고 전이 규칙을 붙인 뒤 베이스가 `AddModule`한다(초기 상태는 `AddModule` 시점에 들어가므로 순서가 이렇다). 상태 객체는 매 `Begin`마다 새로 만들어지니 상태 안의 필드 초기화를 `OnEnter`에 의존하지 않아도 된다.
- 종료 시 `RemoveModule` → `exitOnDetach`로 현재 상태의 `OnExit`가 불린다. 트윈 정리는 `OnExit`에 둔다.
- `Complete`가 전이 도중(`OnEnter`, `OnExit`, `StateChanged` 안)에 불리면 `Stop`을 즉시 부를 수 없어(`InvalidOperationException`) 모듈을 `IsEnabled=false`로 멈춰 두고 다음 `OnThingLateUpdate`나 다음 `ResetBoss`/`Begin`에서 뗀다. `Finished` 이벤트 자체는 즉시 난다.
- 일시정지(`Time.timeScale = 0`)와 호환: 기본 틱이 `Time.deltaTime`이고 DOTween도 기본이 scaled time이라 같이 멈춘다. 일시정지 중에도 돌아야 하면 `UseUnscaledTime`을 `true`로 하고 트윈에 `SetUpdate(true)`를 건다.

### `BossContext`

```csharp
public sealed class BossContext
{
    public BossContext(LanePlayer player, Transform scrollRoot = null, Camera camera = null);
    public LanePlayer Player { get; }
    public Transform ScrollRoot { get; }
    public Camera Camera { get; }
}
```

`player`가 null이면 `ArgumentNullException`. 필요한 참조가 늘면 여기에 필드를 더한다(생성자 기본값으로 호환 유지).

## 선택 헬퍼

베이스는 강제하지 않는다. 세 보스가 전부 쓰고 있으니 같은 형식의 보스라면 그대로 쓰는 편이 빠르다.

### `BossTimer`

```csharp
public sealed class BossTimer
{
    public BossTimer(float duration);
    public float Duration { get; set; }
    public float Elapsed { get; }
    public float Remaining { get; }
    public float Remaining01 { get; }
    public float Progress01 { get; }
    public bool IsRunning { get; }
    public bool IsExpired { get; }
    public event Action Expired;      // 한 번만
    public void Start();              // Elapsed=0, 시작(재시작)
    public void Stop();
    public void Reset();
    public void Tick(float deltaTime);
}
```

`BossThing.Timer`로 노출하면 `BossDirector`가 매 프레임 `Remaining`을 `BossTimerView`에 쓴다.

### `TelegraphedAttackPhase<TContext, TKey> : TimedState<TContext, TKey>`

```csharp
protected TelegraphedAttackPhase(StateMachine<TContext, TKey> machine, BossTimer timer, BossAttackTiming timing, TKey next);

public BossTimer Timer { get; }
public BossAttackTiming Timing { get; set; }        // Telegraph, Attack, Recovery, FullLaneTelegraph
public float ImminentLead { get; set; } = 0.2f;     // 공격 직전 OnTelegraphImminent까지의 여유
public BossAttackStep Step { get; }
public float StepElapsed { get; }
public float StepDuration { get; }
public float CurrentTelegraph { get; }              // 이번 사이클의 예고 시간
public float Step01 { get; }
public int AttackCount { get; }
public bool IsAttacking { get; }

protected virtual void OnPhaseEnter(TContext c);
protected virtual bool CanStartAttack(TContext c);   // false면 Idle에서 대기
protected abstract void OnTelegraphBegin(TContext c); // 패턴 고르고 위험 레인 표시
protected virtual float GetTelegraphDuration(TContext c); // 기본 Timing.Telegraph. 3레인 전체면 Timing.FullLaneTelegraph
protected virtual void OnTelegraphImminent(TContext c);   // 공격 ImminentLead초 전 한 번. 띠 밝히기
protected abstract void OnAttackBegin(TContext c);    // 공격체 이동·판정 켜기
protected abstract void OnAttackEnd(TContext c);      // 판정 끄기, 회복 시작
protected virtual void OnRecoveryEnd(TContext c);
protected virtual void OnStepUpdate(TContext c, float dt);
protected virtual void OnPhaseExpired(TContext c);    // 타이머 만료 + 사이클 경계
protected virtual void OnPhaseExit(TContext c);       // 정리
```

- 진입 시 `timer.Start()`. 매 틱 `timer.Tick`. 사이클은 Idle → Telegraph(`GetTelegraphDuration`, 기본 `Timing.Telegraph`) → Attack(`Timing.Attack`) → Recovery(`Timing.Recovery`) → Idle. 예고 시간은 `OnTelegraphBegin`이 패턴을 고른 뒤 `GetTelegraphDuration`으로 정해진다. Idle에서 타이머가 만료돼 있으면 `OnPhaseExpired` 뒤 `Finish()`(= `Next`로 전이). **공격 도중에 30초가 끝나도 그 사이클은 끝까지 돈다**(회복 뒤 종료).
- `OnTimedEnter`/`OnTimedUpdate`/`OnTimeout`/`OnExit`는 `sealed`. 파생은 위 훅만 쓴다.
- UnityEngine 의존 없음. 테스트는 가짜 컨텍스트로 돈다.

### `BossData : ScriptableObject`

| 필드 | 기본 | 뜻 |
| --- | --- | --- |
| `displayName` | 비움 | 타이머 UI 이름. 비우면 파생의 기본값(낚싯줄/바다코끼리/폭포) |
| `useGameConfigTiming` | true | true면 아래 다섯 값 대신 `GameConfig.Current.BossDuration/BossTelegraphDuration/BossAttackDuration/BossRecoveryDuration/BossFullLaneTelegraphDuration`을 읽는다. CONFIG 한 곳 원칙. 보스마다 다르게 하려면 false |
| `duration` | 30 | 보스 시간 |
| `telegraphDuration` / `attackDuration` / `recoveryDuration` | 1.2 / 0.6 / 0.6 | 예고/공격/휴식 (v4 10절) |
| `fullLaneTelegraphDuration` | 1.5 | 3레인 전체 공격 예고(v4 10·13절). `useGameConfigTiming`이면 `GameConfig.BossFullLaneTelegraphDuration`을 쓴다 |
| `telegraphImminentLead` | 0.2 | 공격 직전 띠가 한 번 더 밝아지는 시점(3판 19.4) |
| `introDuration` / `outroDuration` | 1.0 / 0.8 | 등장/퇴장 연출 (30초에 미포함) |
| `allowRepeatPattern` | false | 같은 패턴 연속 허용 |
| `seed` | 0 | 0이면 매번 다른 난수, 아니면 고정 시드 |
| `patterns` | 파생별 기본 | `BossPattern` 목록. 비어 있으면 생성기가 `EnsureDefaultPatterns()`로 채운다 |

`BossPattern`: `label`, `laneMask`(비트: 상단=1, 중단=2, 하단=4), `weight`(0이면 안 나옴), `requiresJump`(폭포 3레인용).

`BossPatternSelector(patterns, seed, allowRepeat)`: `TryPick(filter, out pattern)`. 가중치 랜덤. `allowRepeat=false`면 직전 패턴을 빼고 뽑되 후보가 없으면 직전 패턴을 허용한다. 필터가 전부 거르면 `false`.

### `LaneTelegraph : MonoThing`

`laneRenderers[lane]`(레인마다 화면 폭 빨간 띠)를 `Show(lane)`/`ShowMask(mask)`/`Hide()`로 켜고 끈다. 켜진 띠는 `minAlpha(0.3)↔maxAlpha(0.4)`로 펄스하고(4번 11절: 30~40% 투명도) `Brighten()`이 오면 펄스를 멈추고 `brightAlpha(0.45)`로 밝아진다(공격 0.2초 전, `TelegraphedAttackPhase.OnTelegraphImminent`에서 호출). 값은 프리팹의 직렬화 필드이며 생성기가 같은 값을 넣는다. 파티클 없음. `DOTween.ToAlpha` 사용. 모듈은 첫 호출에 지연 생성하므로 보스 GameObject가 아직 비활성이어도(`OnInitialize` 시점) 불러도 된다.

### `PatternBoss<TSelf, TKey, TData> : BossThing<TSelf, TKey>`

효과음: `ShowTelegraph`가 `boss_telegraph`, 세 공격 페이즈의 `OnAttackBegin`이 `PlayAttackSfx()`(`boss_attack`)를 부른다. `AudioManager.TryGetInstance` 가드, `Game.Boss` → `Core.Audio`.

`[SerializeField] TData data`, `[SerializeField] LaneTelegraph telegraph`를 갖고 `Timer`(data.Duration), `DisplayName`(data), `Patterns`(선택기), `Timing`, `TelegraphImminentLead`, `TryPickPattern`, `ShowTelegraph/BrightenTelegraph/HideTelegraph`, `GetLaneCenterY(mask)`, `GetLaneSpanHeight(mask, laneHeight)`, `LaneSpacing`을 제공한다. `OnInitialize/OnBegin/OnComplete/OnReset`을 구현하므로 파생이 오버라이드할 때 `base`를 부른다. `data`가 없으면 `OnBegin`에서 오류 로그 후 `Complete(Passed)`(보스를 건너뛴다).

## 레인 띠 = 판정 (`LaneTelegraph` + `LaneHazard`)

v4 10·14·15절: "빨간 반투명 직사각형이 위험한 레인 전체를 덮고 **실제 공격 판정과 반드시 일치**한다". 그래서 판정을 공격체에서 떼어 내 띠와 같은 박스로 옮겼다. 공격체 스프라이트(바늘·몸통·급류·돌)는 연출만 하고 판정에 관여하지 않는다.

- **크기의 정본은 `GameConfig`다.** `bossLaneBandWidth`(12.8 = 카메라 ortho 3.6 · 16:9 화면 폭)와 `bossLaneBandHeight`(0.9). `PatternBoss.OnInitialize`가 `ApplyLaneBandLayout()`으로 `LaneTelegraph.ApplyLayout`과 `LaneHazard.ApplyLayout`에 같은 값을 넣으므로 띠와 판정이 어긋날 수 없다. 두 `ApplyLayout`은 레인 오브젝트를 **월드 좌표 (0, laneY)** 에 놓으므로 보스 루트가 어디에 있든 같은 자리다.
- `LaneHazard : MonoThing`은 레인마다 자식 `Band{lane}`을 갖는다. 각각 Kinematic `Rigidbody2D`(`useFullKinematicContacts`) + trigger `BoxCollider2D`(폭×높이) + `Hazard`. **평소에는 GameObject가 꺼져 있고 공격 구간에만 켜진다.**
- 왜 `Hazard.enabled` 토글이 아니라 GameObject 토글인가: `HurtboxModule`은 겹침 목록을 훑을 때 `isActiveAndEnabled`가 false인 항목을 목록에서 **지운다**. 예고 중 겹친 채로 꺼 두면 목록에서 빠져 공격 시작에 `enabled = true`로 되돌려도 맞지 않는다. GameObject를 켜면 다음 물리 스텝에 `OnTriggerEnter2D`가 다시 와서 목록에 들어간다(공격 0.6초 중 약 0.02초 지연, 무시 가능).
- API: `Prepare(mask)`(예고 때 위험 레인 기억, 무장 해제) → `Arm()`(공격 시작) → `Disarm()`(공격 끝) → `Clear()`. `IsArmed`, `Mask`, `IsLaneArmed(lane)`, `TryGetLaneBand(lane, out Rect)`.
- `PatternBoss`가 감싼다: `ShowTelegraph(mask)`가 띠 표시 + `Prepare(mask)`, `HideTelegraph()`가 띠 숨김 + `Clear()`, `ArmLaneHazard()`/`DisarmLaneHazard()`를 세 공격 페이즈의 `OnAttackBegin`/`OnAttackEnd`가 부른다. 읽기용 `DangerMask`, `IsHazardArmed`, `LaneHazard`.
- 2레인 패턴은 **레인마다 별도 박스 2개**다(하나의 큰 박스가 아니다). 그래서 레인 사이 빈 틈(띠 높이 0.9 < 레인 간격 1.1)이 띠와 판정 양쪽에서 똑같이 안전하다.
- 띠 자체는 그대로다. `minAlpha` 0.3 ↔ `maxAlpha` 0.4 펄스, 공격 `telegraphImminentLead`(0.2)초 전 `brightAlpha` 0.45.
- 수면 상호작용: 바늘과 바다코끼리 몸에는 trigger 콜라이더 + `Game.Water.WaterInteractor`가 남아 있다(판정용이 아니라 수면 반응용). `Hazard`가 없으므로 `HurtboxModule`은 무시한다.
- `GameConfig.bossHitboxAlpha`와 `Assets/GameAssets/Placeholder/HitboxFrame.png`는 통합 패스(2026-09-06)에서 삭제했다(공격체 프레임이 없어져 참조가 없었다).

## 파생 작성 절차

1. 상태 enum을 정의한다. 예: `enum MyBossState { Intro, Attack, Outro }`.
2. `PatternBoss<MyBoss, MyBossState, MyBossData>`(또는 형식이 다르면 `BossThing<MyBoss, MyBossState>`)를 상속한다. 씬에 필요한 참조(공격체 Transform, `Hazard`, SpriteRenderer)는 `[SerializeField] private`로 둔다.
3. `InitialKey`를 지정하고 `BuildStates(fsm)`에서 `fsm.Add(key, state)`로 상태를 등록한다. `TimedState`·`TelegraphedAttackPhase`·`CompleteOnTimeoutState`를 조합하거나 `State<MyBoss>`를 직접 쓴다. 생성자에 `fsm.Machine`을 넘긴다.
4. 공격 페이즈는 `TelegraphedAttackPhase<MyBoss, MyBossState>`를 파생해 `OnTelegraphBegin`(패턴 선택 + `boss.ShowTelegraph` — 띠와 판정 마스크가 함께 잡힌다), `OnAttackBegin`(공격체 이동 + `boss.ArmLaneHazard()`), `OnAttackEnd`(`boss.DisarmLaneHazard()`), `OnPhaseExit`(트윈 Kill, 원위치)를 채운다. 공격체에 별도 `Hazard`를 붙이지 않는다.
5. 마지막 상태에서 `boss.Complete(BossOutcome.Passed)`를 부른다. 실패는 PlayFlow가 플레이어 `Hit`로 판정하므로 보스는 `Abort()`가 언제든 올 수 있다는 것만 대비한다(`OnExit`/`OnComplete`에서 정리).
6. `OnInitialize`에서 위치·판정·표시를 초기 상태로 되돌린다(재시작 시 다시 불린다). `Awake`에 의존하지 않는다(프리팹 루트가 비활성이라 `Begin`까지 `Awake`가 오지 않는다).
7. 데이터는 `BossData` 파생 SO로 두고 `CreateDefaultPatterns`에 기본 패턴을 적는다. 생성기(`BossAssetSetup`)에 데이터·프리팹 생성을 추가한다.

## 패턴 표현의 자유도

베이스는 FSM 뼈대만 준다. 공격 표현은 파생이 고른다.

| 방식 | 언제 | 예 |
| --- | --- | --- |
| 데이터 기반 레인 점유 | 위험 레인만 정하면 되는 보스 | 세 보스 전부 `BossPattern.laneMask` + `LaneTelegraph` |
| DOTween 시퀀스 | 공격체의 움직임·연출 | 낚싯바늘 하강/복귀, 바다코끼리 돌진, 급류·돌 통과, 폭포 돌파 |
| 코드로 짠 페이즈 | 조건 분기가 있는 흐름 | 폭포 `Breakthrough`(마지막 폭포 3초 접근 → 통과/충돌 판정) |
| 스포너 패턴 재사용 | 일반 장애물을 보스가 쏟아내는 형식 | `Game.Boss`는 `Game.Spawner`를 참조하지 않으므로 그런 보스는 `Game.Boss.Integration`(또는 새 asmdef)에 두고 `ObstacleSpawner`를 `BossContext` 확장이나 SerializeField로 받는다 |

`BossThing`을 직접 상속하고 `StateMachineModule`만 쓰면 위 어느 것과도 무관한 보스를 만들 수 있다.

## 보스별 구현

세 보스 공통: 프리팹 루트는 비활성. `Telegraph`(LaneTelegraph, 레인 3개 띠) 자식이 있다. 30초 타이머는 `Attack` 페이즈 진입 시 시작한다. 보스 본체는 `ScrollRoot` 밖(월드 고정)이다. 패턴 집합은 3판 14~16절과 일치한다: 낚싯줄 1/2/3, 바다코끼리 1/2/3/12/23(13·123·원거리 없음), 폭포 1/2/3/12/23/123. 모든 공격 페이즈가 공격 0.2초 전 `BrightenTelegraph()`를 부른다.

### 보스 1 낚싯줄 (`FishingLineBoss`)

```text
Intro(TimedState, IntroDuration) ─▶ Attack(FishingLineAttackPhase, 30초) ─▶ Outro(CompleteOnTimeoutState, OutroDuration) ─▶ Complete(Passed)
```

| 패턴 | 마스크 | 설명 |
| --- | --- | --- |
| Top | 1 | 상단 레인 예고 → 상단으로 바늘 |
| Middle | 2 | 중단 |
| Bottom | 4 | 하단 |

**v4: 수직 낙하가 아니라 트롤링 스윕이다.** 바늘은 배가 끌고 가는 낚싯줄처럼 오른쪽에서 들어와 예고된 레인 높이를 그대로 유지한 채 왼쪽으로 훑고 지나간다.

한 사이클:

1. 예고 시작(`OnTelegraphBegin`): 패턴 선택 → 레인 띠 표시 + `LaneHazard.Prepare` → `StageHook(laneY)`가 바늘을 `hookEnterX`(6, 화면 오른쪽 안쪽)의 **레인 높이**에 놓고 렌더러를 켠다. 예고 내내 "저기서 온다"는 추가 단서가 된다.
2. 스윕 시작: `sweepLead = 크로스비율 × hookSweepDuration − 공격/2`초 전에 시작한다. 크로스비율 = `(hookEnterX − PlayerX) / (hookEnterX − hookExitX)`. 기본값(6 / −7.5 / 1.5초 / 공격 0.6)이면 `sweepLead ≈ 0.83`초라 **예고 1.2초의 0.37초 지점**부터 움직인다. `sweepLead ≤ 0`이면 공격 시작에 출발한다(예외 안전장치).
3. 공격 시작(`OnAttackBegin`): `ArmLaneHazard()`. 판정은 레인 띠 전체이므로 바늘의 정확한 x와 무관하다. 스윕 설계상 바늘은 **공격 0.6초의 한가운데에 플레이어 x를 지난다.**
4. 공격 끝: `DisarmLaneHazard()` + 띠 숨김. 바늘은 계속 흘러 휴식 0.6초 안에 `hookExitX`로 화면을 벗어난다.
5. 휴식 끝(`OnRecoveryEnd`): `ResetHook()` — 렌더러를 끄고 `hookExitX`·대기 높이로 되돌린다.

연출:

- **줄 기울기**: `Line`은 `Hook`의 자식이 아니라 루트의 형제이고, 매 프레임 앵커 `(hookX + lineDragOffset, WaterSurfaceY + lineAnchorOffset)`에서 바늘까지 잇는다(중점 배치 + `Atan2` 회전 + 길이만큼 y 스케일). 앵커가 바늘보다 항상 `lineDragOffset`만큼 오른쪽(진행 반대편)에 있어 줄이 **뒤로 기운 채 바늘을 따라간다**. 기획 문구 "수면의 점에 매달려 뒤로 기운다"를 고정 앵커가 아니라 따라오는 앵커로 읽었다(고정 앵커면 스윕 끝에 줄 길이가 17 unit이 되고 거의 수평이 된다).
- **위아래 흔들림**: `y = laneY + hookBobAmplitude · sin(2π · hookBobFrequency · t)`. 진폭 0.12는 띠 높이 0.9의 13%라 판정 레인을 벗어나 보이지 않는다.

`FishingLineBossData`: `hookParkOffset` 1.5, `hookEnterX` 6, `hookExitX` −7.5, `hookSweepDuration` 1.5, `hookBobAmplitude` 0.12, `hookBobFrequency` 1.8, `lineAnchorOffset` 1.2, `lineDragOffset` 0.9, `lineWidth` 0.06. (`hookDescendRatio`는 없앴다.)

프리팹 `FishingLineBoss.prefab`: 루트(`FishingLineBoss`) / `Telegraph` / `LaneHazard/Band0..2` / `Hook`(Rigidbody2D Kinematic + BoxCollider2D trigger 0.5×0.6 + `WaterInteractor`, **`Hazard` 없음**) / `Hook/HookSprite`(원 0.5, 렌더러 꺼짐) / `Line`(루트 자식, 렌더러 꺼짐).

### 보스 2 바다코끼리 (`WalrusBoss`)

```text
Intro(WalrusIntroState: ExitX→RestX 이동) ─▶ Attack(WalrusAttackPhase, 30초) ─▶ Outro(WalrusOutroState: ExitX로 퇴장) ─▶ Complete(Passed)
```

| 패턴 | 마스크 | 몸 | 설명 |
| --- | --- | --- | --- |
| MouthTop / MouthMiddle / MouthBottom | 1 / 2 / 4 | 높이 `singleBodyHeight`(0.9), `mouthColor` | 입 벌리고 1레인 돌진 (3판 15절 "1레인 = 입") |
| ArmsTopMiddle | 3 | 높이 `doubleBodyHeight`(2.0), `armsColor` | 팔+입, 상+중 |
| ArmsMiddleBottom | 6 | 같음 | 팔+입, 중+하 (3판 15절 "2레인 = 입+팔") |

한 사이클: 예고 시작에 띠 표시 + `LaneHazard.Prepare` + 몸 크기·색 전환 + `Telegraph × approachRatio`초 동안 마스크 중심 y로 이동(대기 x=`restX`). 공격 시작에 `ArmLaneHazard()` 후 `Attack`초 동안 `dashX`까지 왼쪽 돌진(플레이어를 지나친다). 공격 끝에 판정 끄고 몸을 원래대로, 회복 시간 동안 `restX`로 복귀. 플레이어와 같은 방향으로 가는 중이라 화면상 정지한 것으로 취급하며 추격 게임이 아니다.

**v4에서 바뀐 것**: 인트로·접근·돌진·퇴장 트윈이 전부 **`Body` 자식**을 움직인다(예전에는 보스 루트 `transform`). 루트가 움직이면 자식인 `Telegraph`와 `LaneHazard`가 같이 끌려가 띠·판정이 돌진을 따라 화면을 벗어난다. 프리팹 루트는 원점에 고정하고 `ResetPosition`/`PlaceAtX`가 `Body`만 옮긴다. 2레인 돌진은 몸이 2.0 unit로 커지지만 **판정은 몸 크기가 아니라 두 레인 띠**다.

`WalrusBossData`: `restX` 3, `dashX` -7, `exitX` 9, `bodyWidth` 1.6, `singleBodyHeight` 0.9, `doubleBodyHeight` 2.0, `approachRatio` 0.6.

프리팹: 루트(`WalrusBoss`, 원점) / `Telegraph` / `LaneHazard/Band0..2` / `Body`(스프라이트, Rigidbody2D + BoxCollider2D + `WaterInteractor`, **`Hazard` 없음**, 초기 위치 (restX, 중단 y)). 몸 크기는 `SpriteRenderer.sprite.bounds`로 스케일을 계산하므로 스프라이트를 바꿔도 unit 크기가 유지된다.

### 보스 3 폭포 (`WaterfallBoss`)

```text
Intro(TimedState) ─▶ Attack(WaterfallAttackPhase, 30초) ─▶ Breakthrough(마지막 폭포 3초 접근 → 판정) ─▶ Complete(Passed / Failed)
※ 체크포인트 재도전이면 InitialKey가 Breakthrough라 Intro·Attack을 건너뛴다
```

| 패턴 | 마스크 | 점프 필수 |
| --- | --- | --- |
| Top / Middle / Bottom | 1 / 2 / 4 | |
| TopMiddle | 3 | |
| MiddleBottom | 6 | |
| All | 7 | ○ (상단 이동 후 점프, **예고 1.5초**) |

한 사이클: 예고 시작에 패턴 선택(`requiresJump` 패턴은 `!Player.IsAirborne && JumpCooldownRemaining <= FullLaneTelegraph`일 때만 후보. v4 13절 "쿨타임 중이면 전체 공격을 내지 않는다") + 띠 표시 + `LaneHazard.Prepare`. 전체 레인 패턴은 `GetTelegraphDuration`이 `Timing.FullLaneTelegraph`(1.5)를 돌려준다. 공격 시작에 `ArmLaneHazard()` 후 마스크의 레인마다 `Rapid{lane}`(옅은 파란 사각형 = 강한 물줄기)과 `Rock{lane}`(회색 원, `rockTrail`만큼 뒤)을 **활성화**해 `spawnX`에 놓고 `Attack`초 동안 `exitX`까지 선형 이동한다. 공격 끝에 판정 끄고 **비활성화**해 `spawnX`로 되돌린다. 즉 물줄기 연출은 공격 구간에만 존재하고 그 외에는 배경 물 흐름만 남는다. **급류·돌에는 `Hazard`도 콜라이더도 없다**(순수 연출). 급류 스프라이트 폭은 화면 폭의 25%(3.2)다.

#### 마지막 폭포 = 닿으면 실패 (2026-09-06 기획 답변)

기획 답변 7항 「마지막 폭포는 닿으면 실패(접근 약 3초), 실패 시 마지막 폭포부터 재시도」를 반영해 `Breakthrough`를 다시 썼다. **무한 대기와 깜박임 유도는 없어졌다.**

```text
Attack(30초 종료) ─▶ Breakthrough
  진입: LanePlayer.ResetJumpCooldown(), 띠 숨김, 급류·돌 정리
        마지막 폭포를 finalWaterfallEnterX(9.5)에 놓고 표시
        finalContactX = PlayerX + finalContactHalfWidth = -4.2 + 0.75 = -3.45 로
        finalApproachDuration(3초) 동안 Ease.Linear 이동 (약 4.32 unit/s)
  판정(진입 후 정확히 3초, FinalApproachRemaining == 0):
    공중이고 y ≥ finalClearHeight(2.2)  → 통과
    그 외(착지·레인 이동 중·너무 낮은 점프) → 충돌
  통과: 폭포를 finalPassDuration(0.8초) 동안 exitX(-8)로 흘려보내고 Complete(Passed)
  충돌: BossThing.Impact 발생 → Complete(Failed)
```

- **레인과 무관하다.** 마지막 폭포는 세로 8 unit이라 3레인을 전부 덮는다. 그래서 `LaneHazard`(레인 띠)를 쓰지 않고 `WaterfallBreakthroughState`가 시각으로 직접 판정한다.
- **`Hazard` 콜라이더를 달지 않은 이유**: `LanePlayer.IsVulnerable`은 `!IsMoving && !IsAirborne && InputEnabled`라 **공중이면 판정 자체가 오지 않는다.** 콜라이더로 만들면 「공중이지만 너무 낮은 점프」를 절대 잡을 수 없고, 레인 이동 중에도 통과해 버린다. 그래서 판정을 상태 코드에 두었다.
- **그래도 `Hit` 연출은 나온다.** 충돌하면 `BossThing.Impact` → `BossDirector.OnBossImpact` → `PlayFlow.ReportImpact()` → `Hit` 상태 → 0.1초 정지·흔들림·밀림 → `Fail()`. 일반 장애물과 같은 경로다(`PlayFlow.RunHitReactionAsync`를 `OnHit`와 공유한다). 보스가 `Complete(Failed)`로 돌려주는 `false`는 `PlayFlow`가 이미 `IsTerminal`이라 두 번 `Fail()`하지 않는다.
- 3초는 **쿨타임이 0으로 리셋된 순간부터**다. 게다가 접근하는 동안에는 **착지해 있는 매 프레임 쿨타임을 다시 0으로 만든다.** 30초가 끝나는 순간 플레이어가 공중이면(최대 1.6초) 착지 후 쿨타임 1.2초를 기다려야 해서 남는 시간이 0.2초뿐이라 회피가 불가능해지기 때문이다. 지금은 착지만 하면 언제든 뛸 수 있고, 남은 시간이 최소 1.4초라 통과 창(궤적상 1.13초) 안에 반드시 한 번은 들어간다.
- 레인 이동(0.2초 × 최대 2회)까지 더해도 남는 시간은 1.0초 이상이다.
- `finalClearHeight` 2.2를 넘는 구간은 점프 궤적 `1.1 + 4·2.2·t(1−t)` 기준 `t ∈ [0.146, 0.854]`, 즉 1.6초 중 **약 1.13초**다. 너무 이르거나 늦게 뛰면 맞는다.
- 플레이어가 없으면(`Player == null`) `ClearsFinalWaterfall()`이 `true`라 그냥 통과한다.

| 필드 | 기본 | 뜻 |
| --- | --- | --- |
| `finalWaterfallEnterX` | 9.5 | 마지막 폭포의 시작 x(화면 오른쪽 밖) |
| `finalApproachDuration` | **3.0** | 접근 시간. 이 시각에 판정한다. 기획 「약 3초, 개발 기본값」 |
| `finalClearHeight` | **2.2** | 이 높이 이상에 있어야 넘은 것으로 본다(상단 레인 1.1 + 1레인) |
| `finalContactHalfWidth` | **0.75** | 폭포 스프라이트 폭 1.5의 절반. 앞면이 `PlayerX`에 닿는 순간이 판정 시각이 되도록 이동 목표를 `PlayerX + 이 값`으로 둔다 |
| `finalPassDuration` | **0.8** | 통과 뒤 폭포가 `exitX`까지 빠지는 시간 |
| `waterfallX` | 5.5 | 프리팹의 초기 배치 x(생성기용). 런타임 이동에는 쓰지 않는다 |
| `spawnX` / `exitX` / `rockTrail` | 7.5 / −8 / 1.0 | 급류·돌 연출 |

없어진 필드: `finalWaterfallEnterDuration`, `safeWindowDuration`, `jumpCuePulseDuration`. 깜박임(`ShowJumpCue`/`HideJumpCue`)과 `LanePlayer.Jumped` 구독도 지웠다.

읽기용 프로퍼티: `WaterfallBoss.IsFinalApproach`, `FinalApproachElapsed`, `FinalApproachRemaining`(접근 중이 아니면 −1). `FullLoopTests`가 이 값을 보고 점프 시각을 맞춘다.

#### 마지막 폭포 체크포인트

`BossThing`에 두 가지가 생겼다.

| 멤버 | 뜻 |
| --- | --- |
| `string EntryCheckpoint { get; set; }` | 이 보스를 **어디서부터** 시작할지. `BossDirector`가 `Initialize` 전에 `PlayFlow.CheckpointTag`를 넣는다 |
| `virtual string ActiveCheckpoint => null` | 지금 진행 중인 지점의 이름. 매 프레임 `BossDirector`가 읽어 `PlayFlow.SetBossCheckpoint`로 올린다 |
| `event Action Impact` / `protected RaiseImpact()` | 「보스가 플레이어를 때렸다」. `BossDirector`가 `PlayFlow.ReportImpact()`로 잇는다 |

`WaterfallBoss`가 구현한 것:

- `ActiveCheckpoint`는 `CurrentKey == Breakthrough`일 때 `BossCheckpoints.FinalWaterfall`("FinalWaterfall").
- `StartsAtFinalWaterfall`(= `EntryCheckpoint == BossCheckpoints.FinalWaterfall`)이면 `InitialKey`가 `Intro`가 아니라 `Breakthrough`다. 등장 연출과 30초 공격을 통째로 건너뛴다.
- 그때 `OnBegin`이 `Timer.Start(); Timer.Tick(Timer.Duration);`으로 **타이머를 만료 상태로 만든다.** HUD의 보스 타이머가 정상 진행에서 30초가 끝났을 때와 똑같이 `0`을 보인다.

문자열 상수는 `Game.Boss`의 `BossCheckpoints`와 `Game.Play`의 `PlayFlow.FinalWaterfallCheckpoint` 양쪽에 있다(`Game.Play`는 `Game.Boss`를 참조하지 않으므로 값이 같은 두 상수를 두고 `Game.Boss.Integration`이 잇는다). 흐름은 `Docs/CoreLoop.md` 「체크포인트」.

프리팹: 루트 / `Telegraph` / `LaneHazard/Band0..2` / `Waterfall`(1.5 × 8 unit 사각형, 판정 없음, `OnInitialize`에서 숨김) / `Rapid0..2`, `Rock0..2`(스프라이트만, 대기 중 GameObject 비활성). **프리팹은 이번 변경으로 바뀌지 않았다**(데이터 에셋과 코드만 바뀐다).

## 생성기

에디터 콘솔에 컴파일 오류가 없는 상태에서 `Team1004 > Generate Boss Assets`.

| 경로 | 내용 | 덮어쓰기 |
| --- | --- | --- |
| `Assets/GameAssets/Placeholder/Square.png`, `Circle.png` | 없을 때만 생성(코어루프 생성기와 같은 규격) | 안 함 |
| `Assets/GameAssets/Design/Boss/{FishingLine,Walrus,Waterfall}BossData.asset` | 없으면 생성. 있으면 값 보존, 패턴이 비어 있을 때만 기본 패턴 채움 | 안 함 |
| `Assets/GameAssets/Boss/{FishingLine,Walrus,Waterfall}Boss.prefab` | 없으면 생성, 있으면 경고 후 유지 | `Regenerate Boss Prefabs (Overwrite)`, 또는 코어루프 `Regenerate Core Loop Scenes (Overwrite)`가 `BossAssetSetup.Generate(true)`를 부른다 |
| `Assets/GameAssets/Boss/BossSet.prefab` | `BossDirector` + 위 세 프리팹의 중첩 인스턴스(비활성). `bosses = [낚싯줄, 바다코끼리, 폭포]` | 위와 같음 |

레인 y·플레이어 x·수면 y는 `Assets/GameAssets/Design/GameConfig.asset`에서 읽는다(없으면 코드 기본값). 런타임 `OnInitialize`가 `GameConfig.Current`로 다시 배치하므로 생성 시 값이 달라도 문제없다. `Hazard.kind`는 내가 만든 프리팹 안의 컴포넌트이므로 SerializedObject로 채웠다.

## 통합 절차 (PlayFlow)

`PlayFlow`는 이미 구간 사이에 `Boss` 상태를 두고 `Func<int, Awaitable<bool>> BossHandler`를 노출한다(`RunBossAsync(index)`: 핸들러가 `false`를 돌려주면 `Fail()`, null이면 보스를 건너뜀). `index`는 직전 일반 구간 번호(1부터)이고 보스 n은 구간 n 뒤에 온다. `Boss` 상태에서 플레이어 입력은 켜져 있고 배경 스크롤은 멈춘다. 플레이어 피격은 PlayFlow가 받아 `Hit` → `Fail`로 간다.

1. `Generate Boss Assets`로 `BossSet.prefab`을 만든다.
2. Play 씬 루트(ScrollRoot 밖)에 `BossSet.prefab` 인스턴스를 하나 둔다. **완료**: 코어루프 생성기 `CoreLoopSetup.InstantiateBossSet`이 둔다(`BossSet`은 활성, 자식 보스 3개는 비활성 그대로).
3. `BossDirector`는 `[DefaultExecutionOrder(-50)]`이라 `PlayFlow.Start`보다 먼저 `Start`에서 `PlayFlow.Current.BossHandler = RunBossAsync`를 건다. 체크포인트가 보스 직전이라 첫 스텝이 보스여도 늦지 않는다.
4. `RunBossAsync(section)`: `bosses[section-1]`에 `Initialize(new BossContext(flow.Player, flow.Scroller.transform, stageCamera ?? Camera.main))` → `Begin()` → 매 프레임 `BossTimerView.Show/SetRemaining(boss.Timer.Remaining)` → `IsFinished`까지 대기(`Awaitable.NextFrameAsync`). 대기 중 `flow.IsTerminal`(Hit/Failed/Cleared)이 되면 `Abort()`. 끝나면 `BossTimerView.Hide()`, `Outcome == Passed`를 반환.
5. 실패 → `PlayFlow.Fail()` → 결과 화면 → `Retry()`는 씬을 다시 로드하고 체크포인트(보스 직전)에서 시작하므로 보스는 처음부터(30초 타이머 포함) 다시 돈다(3판 18절). 씬을 다시 로드하지 않는 재시작을 만들 때는 `boss.ResetBoss()`(FSM 분리·Idle·비활성, 이벤트 없음) 뒤 `Initialize`/`Begin`한다. `Initialize`가 초기화된 보스에 대해 알아서 `ResetBoss`를 부르므로 `Initialize`만 다시 불러도 된다. `OnInitialize`가 타이머·선택기·위치·판정·표시를 전부 초기 상태로 되돌린다.
6. 컷신과의 순서(컷신 n → 보스 n)는 PlayFlow의 스텝 목록이 정한다. 컷신 배우 `boss`(`CutsceneActorIds.Boss`)와 보스 프리팹은 별개 오브젝트다. 컷신 마지막에 보스 배우를 숨기고 보스 프리팹이 등장하는 식으로 맞추면 된다.
7. 타이머 UI(3판 19.2: 상단 중앙에 보스 이름 + 정수 초): `BossTimerView`(`Game.Play`, 코어루프 소유)는 `PlayFlow.BossTimer`로 노출되고 Director가 갱신한다. Director가 기대하는 인터페이스는 아래와 같다. 보스 시작 시 `SetBoss(boss.DisplayName)`, 타이머가 있으면 `SetVisible(true)` 뒤 매 프레임 `SetRemaining(boss.Timer.Remaining)`, 끝나면 `SetVisible(false)`. 타이머가 없는 보스(`Timer == null`)는 `SetVisible(false)`.

```csharp
public sealed class BossTimerView : MonoBehaviour
{
    public void SetBoss(string name);          // "낚싯줄", "바다코끼리", "폭포"
    public void SetRemaining(float seconds);   // 정수 초 내림 표시는 뷰가 한다
    public void SetVisible(bool visible);
}
```

   `BossTimerView`에 `SetBoss/SetVisible/SetRemaining`이 있어 연결되어 있다. PlayMode `FullLoopTests`가 보스 3개 모두에서 타이머 뷰가 켜지는지 확인한다.

BGM(보스용 1곡)은 아직 없다. `BossDirector`에서 `Began`/`Finished`에 맞춰 `AudioManager.PlayBgm`을 넣을 자리다(`Core.Audio` 참조 추가 필요).

## Hazard 연동

플레이어 `HurtboxModule`은 트리거로 겹친 `Hazard`(`GetComponentInParent<Hazard>`)를 모아 두고, 매 프레임 `IsVulnerable`이고 `hazard.isActiveAndEnabled`인 것이 있으면 `Hit`를 한 번 낸다. **보스에서 `Hazard`를 가진 것은 `LaneHazard/Band{lane}`뿐이고**, 공격 구간에 그 GameObject를 켜는 것으로 위험 구간을 만든다(위 「레인 띠 = 판정」의 GameObject 토글 이유 참고). 트리거 콜백에는 Rigidbody2D가 한쪽에 필요하므로 밴드에 Kinematic Rigidbody2D(`useFullKinematicContacts`)를 붙였다(플레이어도 Kinematic이라 양쪽 모두 필요).

주의: `HurtboxModule.OnUpdate`는 `isActiveAndEnabled`가 false인 `Hazard`를 겹침 목록에서 제거한다. 그래서 "겹친 채로 `enabled`만 되돌리면 맞는다"는 예전 설명은 **사실이 아니다**(`Docs/Requests.md`에 올릴 만한 버그는 아니고 이 문서의 계약을 고쳤다).

`BossThing`은 `Hazard`를 상속하지 않는다(`MonoThing`을 두 번 상속할 수 없다). 본체가 곧 공격체인 보스(바다코끼리)도 자식 `Body`에 `Hazard`를 둔다.

## 임시값

- 등장 1.0초·퇴장 0.8초. 30초/1.2/0.6/0.6/1.5는 `GameConfig`(`useGameConfigTiming`). 밝아짐 0.2초는 `BossData`.
- 낚싯바늘: 대기 높이 수면+1.5, 진입 x 6, 퇴장 x −7.5, 스윕 1.5초, 흔들림 0.12/1.8Hz, 줄 앵커 수면+1.2·뒤로 0.9, 줄 두께 0.06, 콜라이더(수면용) 0.5×0.6.
- 바다코끼리: x 3/-7/9, 몸 1.6×0.9 / 1.6×2.0, 접근 비율 0.6, 색 `mouthColor`(주황)·`armsColor`(붉은색).
- 폭포: x 5.5/9.5/7.5/-8, 급류 3.2×0.9, 돌 0.7, 돌 간격 1.0, 마지막 폭포 1.5×8. 접근 3초·통과 높이 2.2·접촉 반폭 0.75·통과 0.8초는 기획 답변(2026-09-06)의 개발 기본값이다.
- 레인 띠·판정 12.8×0.9는 `GameConfig.bossLaneBandWidth`/`bossLaneBandHeight`. 알파 0.3~0.4 펄스 0.4초, 밝아짐 0.45, 색 빨강.
- 폭포 3레인 패턴 조건 `JumpCooldownRemaining <= FullLaneTelegraph`(1.5초로 늘어 예전보다 관대하다).
- 모든 색·크기·정렬 순서.

## 권장 설계에서 바꾼 점

- **생명주기를 순수 C# `BossLifecycle`로 분리했다.** `BossThing`은 이를 감싸고 훅·FSM·활성화만 담당한다. `MonoThing` 없이 Initialize→Begin→Finished, Abort, 중복 Begin을 테스트하기 위해서다.
- **`Begin` 두 번은 무시(`false`), `Initialize` 없는 `Begin`은 예외.** 전자는 흐름 코드에서 흔한 중복 호출이고 후자는 배선 실수다.
- **`Begin`이 GameObject를 활성화하고 `Complete`가 비활성화한다**(`DeactivateOnFinish`). 프리팹을 비활성으로 두는 규칙과 맞추기 위해서다. `Initialize`는 활성화하지 않으므로 `Awake`에 기대지 않는다.
- **FSM 모듈을 `Begin`마다 새로 만든다.** `StateMachineModule`은 상태 제거 API가 없고 초기 상태를 `AddModule` 시점에 들어가므로, 재시작을 깨끗하게 하려면 새로 만드는 것이 단순하다.
- **`PatternBoss`/`TelegraphedAttackPhase`/`BossData`를 베이스가 아닌 선택 계층으로 뒀다.** 기획서 2판의 30초 공통 형식은 이 계층이 담당하고, 형식이 다른 보스는 `BossThing<TSelf,TKey>`부터 만든다.
- **타이밍 값의 정본을 `GameConfig`로 뒀다.** 코어루프 쪽이 이미 `BossDuration` 등을 추가했고 개발자 설정 패널로 빌드에서도 바꿀 수 있다. 보스별로 다르게 할 때만 `useGameConfigTiming=false`.
- **`Game.Boss.Integration`을 추가했다.** 권장안은 통합을 문서 절차로만 두는 것이었지만 `PlayFlow.BossHandler`가 델리게이트라 `Game.Play`를 고치지 않고도 연결할 수 있어 `BossDirector`를 만들었다.
- **보스 실패는 보스가 감지하지 않는다.** PlayFlow가 `Hit`를 받아 `Hit`/`Failed`로 가고 Director가 그때 `Abort()`한다. 보스가 `Player.Hit`를 구독하지 않으므로 이중 처리가 없다.
- **`Complete`가 전이 도중 불리는 경우를 지연 분리로 처리했다.** `StateMachine.Stop`이 전이 중 예외를 던지기 때문이다.

## 테스트

`Assets/Scripts/Boss/Tests/` (EditMode, Test Runner).

| 파일 | 내용 |
| --- | --- |
| `BossLifecycleTests` | Idle 초기값, Initialize 전 Begin 예외, Initialize→Begin→Complete 이벤트 순서, Begin 중복 무시, Abort(Active/Ready/Idle/Finished), Complete(None) 예외, Active 중 Initialize 거부, Finished 뒤 재초기화, Reset, `BossContext(null)` 예외 |
| `BossTimerTests` | 시작 전 무시, 만료 한 번·Elapsed 클램프, 재시작, 정규화, 0초 |
| `BossPatternSelectorTests` | 빈 목록, 시드 결정성, 연속 금지, 후보 하나면 반복 허용, 필터, 가중치 0, `BossLanes` |
| `TelegraphedAttackPhaseTests` | 예고→공격→회복 순서와 만료 뒤 경계 종료, 0초 즉시 종료, `CanStartAttack` 대기, `Restart` 초기화, `Step01`, 전체 레인 예고 1.0초와 공격 0.2초 전 `OnTelegraphImminent` 한 번 |

PlayMode(`Assets/Scripts/Play/Tests/DebugJumpTests.cs`, `Game.Play.Tests`가 `Game.Boss`를 참조):

| 테스트 | 내용 |
| --- | --- |
| `WalrusTwoLaneBand_MissesUncoveredLane_AndHitsCoveredLane` | 디버그 키 4로 보스 2로 간 뒤, 2레인 패턴이 나올 때마다 예고 중에 플레이어를 옮긴다. 무장 순간 `LaneHazard.IsLaneArmed(lane)`이 예고 마스크와 정확히 일치하는지 확인하고, 덮이지 않은 레인(2레인 패턴이면 남은 1레인)에서는 맞지 않고 덮인 레인에서는 맞는지 본다 |

아직 PlayMode로 못 본 것: `BossThing.Begin`의 활성화·FSM 부착, `Complete` 뒤 비활성화, 전이 중 `Complete`의 지연 분리, 낚싯바늘 스윕이 공격 한가운데에 플레이어 x를 지나는지(눈으로 확인), `LaneTelegraph` 표시.

## 검증 (2026-09-06 마지막 폭포 패스)

에디터가 열려 있어 생성기·Unity 테스트를 돌리지 않았다.

| 실행 | 결과 |
| --- | --- |
| `python <scratchpad>/csccheck_all.py` | `assemblies compiled: 36  failed: 0` |

바뀐 파일: `WaterfallBossData.cs`, `WaterfallBoss.cs`, `WaterfallBossStates.cs`, `BossThing.cs`, 새 `BossCheckpoints.cs`, `Integration/BossDirector.cs`, `Assets/GameAssets/Design/Boss/WaterfallBossData.asset`(필드 교체). **프리팹은 바뀌지 않았다.**

에디터에서 확인할 것: `WaterfallBossData` 인스펙터에 `finalApproachDuration`·`finalClearHeight`·`finalContactHalfWidth`·`finalPassDuration`이 보이고 없어진 세 필드가 사라졌는지. PlayMode `DebugJumpTests`·`FullLoopTests`(보스 3 통과), 그리고 「검증하지 못한 위험 지점」.

## 검증하지 못한 위험 지점

- **v4 변경분은 눈으로 미확인이다.** 배치 검증(프리팹 재생성, EditMode, PlayMode `WalrusTwoLaneBand_…`로 2레인 띠 = 판정 확인)은 통합 패스에서 통과했다. `WalrusTwoLaneBand` 테스트는 플레이어 피격 → `PlayFlow.Fail()` → `BossDirector.Abort()`가 같은 프레임에 일어나므로 루프를 나온 뒤 `HasHit`를 판정한다.
- **`LaneHazard`가 실제로 맞히는지**가 가장 큰 위험이다. 판정이 안 되면 (1) 밴드 GameObject가 `Arm()`에 켜지는지, (2) 켜진 다음 물리 스텝에 `OnTriggerEnter2D`가 오는지, (3) `Rigidbody2D.useFullKinematicContacts`가 밴드·플레이어 양쪽에 있는지 순으로 본다. 반대로 **과하게 맞으면** `Disarm()`이 불리는 경로(공격 끝·`HideTelegraph`·`OnPhaseExit`·`OnComplete`)를 본다.
- **띠와 판정의 좌표.** 둘 다 `ApplyLayout`이 **월드** (0, laneY)에 놓는다. 보스 루트를 움직이는 트윈을 새로 추가하면 다시 어긋난다(바다코끼리를 `Body`로 옮긴 이유). 새 보스는 루트를 고정하고 자식만 움직인다.
- **낚싯바늘 스윕 타이밍**은 계산으로만 맞췄다(`sweepLead ≈ 0.83`초). 예고나 공격 시간을 JSON으로 바꾸면 자동으로 다시 계산되지만, `hookSweepDuration`이 너무 짧으면 `sweepLead ≤ 0`이 되어 공격 시작에 출발한다(플레이어를 늦게 지난다). 눈으로 볼 때 이 지점을 먼저 본다.
- `BossThing<TSelf,TKey>.AttachBehaviour`의 `(TSelf)this` 캐스트. 형식 매개변수의 유효 기반 클래스에서의 명시 변환이라 컴파일되는 것으로 보았고 콘솔 오류는 없었다.
- `private protected sealed override` 조합. 콘솔 오류 없음.
- `PatternBoss<…>`의 제네릭 기반 클래스 `[SerializeField] TData data`. Unity 2020.1+에서 제네릭 기반 필드 직렬화가 되지만 Inspector에 안 보이면 파생 클래스로 필드를 내린다.
- `BossDirector.RunBossAsync`가 `async Awaitable<bool>`. `PlayFlow.BossHandler` 시그니처(`Func<int, Awaitable<bool>>`)와 같다.
- `BossDirector`의 `[DefaultExecutionOrder(-50)]`이 `PlayFlow.Start`보다 앞선다는 가정. 체크포인트가 보스 직전일 때 첫 프레임에 보스가 건너뛰어지면 이 순서를 본다.
- 체크포인트로 보스 직전에서 시작할 때 `PlayFlow`가 컷신 최종 상태를 적용한 뒤 바로 `RunBossAsync`를 부르는지는 PlayFlow 쪽 구현에 달려 있다.
- `BossDirector`가 `BossTimerView.SetBoss/SetVisible`을 호출한다(연결됨).
- 폭포 3레인 패턴 조건(`JumpCooldownRemaining <= FullLaneTelegraph`)은 보수적이지 않다. 플레이어가 하단에서 상단까지 0.4초 이동 후 점프해야 하므로 실제로는 급류가 플레이어 x에 닿는 시각(공격 시작 + 약 0.5초)까지 여유가 있다. 회피 불가가 나오면 조건에 `LaneMoveDuration * 2`를 더한다.
- **마지막 폭포 판정은 눈으로 확인하지 못했다.** 시각 판정(진입 3초)과 스프라이트 이동(선형 트윈)이 같은 시간축을 쓰므로 화면상 앞면이 플레이어에 닿는 순간과 판정 순간이 일치해야 하지만, `Ease.Linear`와 `Time.timeScale` 변화(일시정지·8배속 테스트)에서 한두 프레임 어긋날 수 있다.
- **충돌하면 보스 오브젝트가 곧바로 비활성이 된다**(`DeactivateOnFinish`). `Hit` 연출 0.35초 동안 폭포 그림이 사라져 있다. 필요하면 `WaterfallBoss.DeactivateOnFinish`를 끄고 `PlayFlow.Fail()` 뒤에 정리한다.
- 마지막 폭포에는 콜라이더가 없다. 「닿았다」는 순전히 시각 판정이다(위 「마지막 폭포 = 닿으면 실패」의 이유 참고).
- 띠·판정 폭 12.8 unit은 카메라 ortho 3.6·16:9 기준이고 이제 `GameConfig.bossLaneBandWidth`다. 카메라가 바뀌면 이 값을 고친다(생성기와 런타임 `ApplyLayout`이 같은 값을 본다).
- 생성기 `EnsureData`가 기존 데이터 에셋에 `EnsureDefaultPatterns`를 부른다. 기획이 패턴을 전부 지워 비워 두면 다음 생성 때 기본 패턴이 다시 채워진다.

## StateMachine 모듈에 필요한 변경 요청

없다. 전이 중 `Stop` 예외는 지연 분리로 우회했다. `StateMachineModule`에 "다음 안전한 시점에 Stop" API가 생기면 `BossThing<TSelf,TKey>`의 `detachPending` 처리를 지울 수 있다(선택).

## 보스 중 월드 정지 (2026-09-06)

`BossData.holdsWorld`가 켜진 보스는 전투 중 배경 스크롤을 멈춘다(`PlayFlow.SetBossHoldsWorld` → 환경 `SetScrolling(false)`). 길을 막고 선 상대 앞에서 연어가 제자리에서 버티는 그림이다. 낚싯줄은 거슬러 오르는 중이라 흐르고, 바다코끼리·폭포는 정지한다. 장애물 스크롤 루트는 보스 중 원래 정지한다.

| 보스 | `holdsWorld` |
| --- | --- |
| 낚싯줄 | 0 |
| 바다코끼리 | 1 |
| 폭포 | 1 |

`BossThing.HoldsWorld`(virtual, 기본 false)를 `PatternBoss`가 데이터로 구현하고, `BossDirector`가 보스 시작 시 `flow.SetBossHoldsWorld(boss.HoldsWorld)`, 종료 시 `false`로 되돌린다.

