# 보스 (Game.Boss)

FSM 기반 보스 베이스와 세 보스(낚싯줄, **곰**, 폭포) 구현이다. 보스 2는 2026-09-06에 바다코끼리에서 곰으로 갈아 끼웠다(「보스 2 곰」·「바다코끼리(폐기)」). 보스는 "피한다, 버틴다, 통과한다"이며 조작은 ↑/↓/점프뿐이다. 세 보스는 같은 30초 형식(예고 1.2초 → 공격 0.6초 → 휴식 0.6초 → 반복 → 30초 종료 → 클리어, 3레인 전체 공격만 예고 1.5초)을 쓰고, 한 번 맞으면 실패해 해당 보스 처음부터 재시작한다.

## v4 반영 (2026-09-06)

기획서 v4 `10_보스전_공통규칙`·`11~13`·`14_충돌_실패_재시작_체크포인트`·`15_인게임HUD_히트박스_안내UI`로 바뀐 것.

| 항목 | 이전 | v4 |
| --- | --- | --- |
| 일반 예고 | 0.8초 | **1.2초** (`GameConfig.bossTelegraphDuration`) |
| 3레인 전체 예고 | 1.0초 | **1.5초** (`GameConfig.bossFullLaneTelegraphDuration`, 이제 `useGameConfigTiming`을 탄다) |
| 공격 / 휴식 | 0.6 / 0.6 | 그대로 |
| 판정 범위 | 공격체 콜라이더(바늘 0.5×0.6, 몸통 1.6×h, 급류 3×0.9, 돌 0.7) | **레인 띠와 같은 박스**. 레인마다 화면 폭 `bossLaneBandWidth`(12.8) × `bossLaneBandHeight`(0.9), 새 `LaneHazard`가 소유 |
| 히트박스 표시 | 레인 띠 + 공격체 프레임(`HitboxView`) | **레인 띠만**. `HitboxView`·`HitboxViewModule`·`HazardHitbox` 삭제 |
| 낚싯바늘 | 플레이어 x에서 수직 하강 | 오른쪽 → 왼쪽 트롤링 스윕. **2026-09-06 재설계로 폐기했다 — 지금은 고정된 배에서 던지는 캐스팅이다(「보스 1 낚싯줄」)** |
| 바다코끼리 이동 | 보스 루트 transform | **`Body` 자식만** 이동(루트가 움직이면 띠·판정이 같이 끌려간다) |

작성 시에는 오프라인 Roslyn 컴파일만 확인했다. 통합 패스(2026-09-06)에서 `CoreLoopSetup.Regenerate`(내부 `BossAssetSetup.Generate(true)`)로 프리팹 4개를 다시 만들고 EditMode 전체와 PlayMode(`DebugJumpTests` 3개 포함)를 통과했다(`Docs/CoreLoop.md` 「통합 검증 결과 (2026-09-06)」). 눈으로 보는 확인은 「검증하지 못한 위험 지점」과 코어루프 문서의 수동 체크리스트를 따른다.

## 구성

| 경로 | asmdef | 내용 |
| --- | --- | --- |
| `Assets/Scripts/Boss/` | `Game.Boss` (참조: `Core.Modules`, `Core.Foundation`, `Game.StateMachine`, `Game.Player`, `Game.Config`) | 베이스, 선택 헬퍼, 보스 3종 |
| `Assets/Scripts/Boss/Integration/` | `Game.Boss.Integration` (참조: `Game.Boss`, `Game.Play`, `Game.Player`, `Core.Foundation`, `Core.Modules`) | `BossDirector`. `PlayFlow.BossHandler`를 채우는 유일한 접점 |
| `Assets/Scripts/Boss/Editor/` | `Game.Boss.Editor` (Editor 전용) | `BossAssetSetup`: 메뉴 `Team1004/Generate Boss Assets`, `Regenerate Boss Prefabs (Overwrite)` |
| `Assets/Scripts/Boss/Tests/` | `Game.Boss.Tests` (EditMode) | 순수 C# 부분 테스트 |
| `Assets/GameAssets/Design/Boss/` | 데이터 | `FishingLineBossData.asset`, **`BearBossData.asset`**, `WaterfallBossData.asset`, `WalrusBossData.asset`(폐기) |
| `Assets/GameAssets/Boss/` | 프리팹 | `FishingLineBoss.prefab`, **`BearBoss.prefab`**, `WaterfallBoss.prefab`, `BossSet.prefab`, `WalrusBoss.prefab`(폐기) |

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
| `BearBoss.cs`, `BearBossStates.cs`, `BearBossData.cs`, `BearArmView.cs`, `BearSequenceTimeline.cs` | 보스 2 | 곰 |
| `WalrusBoss*.cs` | (폐기) | 바다코끼리. 컴파일만 되고 참조 없음 |
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
| `displayName` | 비움 | 타이머 UI 이름. 비우면 파생의 기본값(낚싯줄/곰/폭포) |
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

세 보스 공통: 프리팹 루트는 비활성. `Telegraph`(LaneTelegraph, 레인 3개 띠) 자식이 있다. 30초 타이머는 `Attack` 페이즈 진입 시 시작한다. 보스 본체는 `ScrollRoot` 밖(월드 고정)이다. 패턴 집합은 낚싯줄 1/2/3/12/23/13(v8 기본 공격), 곰 순차 123 + 1/2/3/12/23(13 없음, `Documents/정리 1번/6번 정리.md`), 폭포 1/2/3/12/23/123이다. 모든 공격 페이즈가 공격 0.2초 전 `BrightenTelegraph()`를 부른다.

### 보스 1 낚싯줄 (`FishingLineBoss`)

```text
Intro(FishingLineIntroState, IntroDuration 1.0초: 배 진입) ─▶ Attack(FishingLineAttackPhase, 30초) ─▶ Outro(FishingLineOutroState, OutroDuration 0.8초: 배 퇴장) ─▶ Complete(Passed)
```

#### 낚시처럼 만든다 (2026-09-06 재설계)

이전 패스는 **배가 바늘을 끌고** 화면을 가로질렀다(트롤링 스윕). 사용자 결정으로 버렸다. 지금은 **배가 한 자리에 떠 있고 바늘만 캐스팅된다.**

| 항목 | 트롤링 스윕(폐기) | 캐스팅(현재) |
| --- | --- | --- |
| 배 | 매 공격마다 `바늘 x + boatTrail`로 순간이동 | **`boatX`(2.5)에 고정.** 등장에 오른쪽에서 들어오고 CLEAR에 오른쪽으로 나간다 |
| 바늘 | 오른쪽 → 왼쪽으로 화면을 훑는다 | 낚싯대 끝에서 **예고된 레인으로 던져지고**, 못 잡으면 빠르게 감아 올린다 |
| 줄 | `낚시바늘.png`에 그려진 줄을 회전·스케일 | **`LineRenderer`로 매 프레임 그린다.** 그림의 줄은 안 쓴다 |
| 바늘 그림 | `낚시바늘.png` 전체(줄+바늘) | `Placeholder/Hook.png`(바늘 머리만 잘라낸 것) |
| 패턴 | 1 / 2 / 3 | **1 / 2 / 3 / 12 / 23 / 13**(v8 기본 공격 6종, Double = 바늘 2개) |

#### 공격 흐름

한 사이클 2.4초다. 예고·공격·휴식 길이는 v4 공통 규칙(`GameConfig`) 그대로이고, 릴은 휴식 안에 들어간다.

| 단계 | 길이 | 하는 일 |
| --- | --- | --- |
| 예고 `Telegraph` | **1.2초** | 패턴 선택 → `ShowTelegraph(mask)`(빨간 띠 + `LaneHazard.Prepare`). 바늘은 낚싯대 끝 `hookRestOffset`(0.3) 아래에 매달려 파도에 같이 흔들린다. 공격 `telegraphImminentLead`(0.2)초 전에 띠가 밝아진다 |
| 공격 `Attack` | **0.6초** | `ArmLaneHazard()` + `BeginCast(mask)`. 바늘이 낚싯대 끝에서 예고된 레인으로 날아가 **공격 창이 끝나는 순간 플레이어 x(−4.2)에 닿는다**(`castDuration` 0.6) |
| 릴 (휴식 앞) | **0.25초** | `DisarmLaneHazard()` + 띠 숨김 직후 시작. 바늘이 `reelDuration` 동안 ease-in(t²)으로 낚싯대 끝까지 튕겨 올라간다. 물 밖으로 나오면서 `WaterInteractor`가 물보라를 낸다. 줄은 팽팽해진다(`LineSlack` 1 → 0) |
| 대기 (휴식 뒤) | **0.35초** | 바늘이 낚싯대 끝에 매달린 채 쉰다 |

- **`recoveryDuration`은 0.6 그대로다.** 「릴 0.25 + 휴식 0.6」으로 늘리면 v4 공통 휴식(0.6, `GameConfig.bossRecoveryDuration`)에서 보스 1만 어긋난다. 그래서 릴을 휴식 앞 0.25초에 넣고 뒤 0.35초를 대기로 남겼다.
- 판정은 여전히 **레인 띠 전체**(`LaneHazard/Band{lane}`)다. 바늘의 정확한 위치는 판정에 관여하지 않는다. 그래서 바늘이 플레이어 x에 도달하기 전이어도 예고된 레인에 서 있으면 맞는다(공정성 규칙 1·2 준수: 띠 = 콜라이더).
- **연어를 잡으면**(띠 판정 적중) PlayFlow가 `Hit` → `Fail`로 가고 `BossDirector`가 `Abort()`를 부른다. 보스 쪽은 `OnPhaseExit` → `ResetHooks()`로 정리만 한다. 별도 「끌어올리기」 연출은 아직 없다.
- 캐스트 궤적: `x = Lerp(rodTipX, −4.2, 1−(1−t)²)`, `y = Lerp(rodTipY, laneY + 바늘 반높이, 1−(1−t)³) + castArcHeight·sin(πt)`. x는 감속하며 접근하고 y는 빠르게 떨어진다. `castArcHeight`(0.35)가 초반에 살짝 띄워 던지는 맛을 준다. 수면(2.0)은 t ≈ 0.2 부근에서 지나므로 캐스트마다 물보라가 한 번 난다.

#### 줄은 `LineRenderer`가 그린다

배가 고정이고 바늘만 움직이므로 줄의 길이와 각도가 매 프레임 바뀐다. 그림 안 줄(스프라이트 회전 + 스케일)로는 표현할 수 없어 **바늘마다 `LineRenderer` 하나**를 둔다.

- `useWorldSpace: true`, 정점 `lineSegments`(6)개, 폭 `lineWidth`(0.03), 색 `lineColor`(0.94, 0.96, 1, 0.85), sorting order **12**(배와 같은 층. 연어 10보다 앞).
- 재질은 프리팹의 다른 렌더러와 같은 URP `Sprite-Unlit-Default`(`a97c105638bdf8b4a8650670310a4cd3`)다. 내장 `Sprites-Default`(10754)가 아니라 이것을 쓴 이유는 이 프로젝트가 URP 2D이고 나머지 스프라이트가 전부 이 재질이라 렌더 경로가 확실하기 때문이다.
- 처짐(카테너리 근사): `p(u) = Lerp(낚싯대 끝, 바늘, u)`에서 `y −= 4·sag·u·(1−u)`. `sag = lineSag(0.08) × 줄 길이 × LineSlack`. 릴 중에는 `LineSlack`이 1 → 0으로 내려가 줄이 팽팽해진다.
- **`lineSag`가 0.15가 아니라 0.08인 이유**: 낚싯대 끝(3.468)과 바늘(레인 −1.1~1.1)의 높이 차가 커서 현 자체가 이미 가파르다. 처짐 깊이가 현 기울기(≈3.5/6)의 1/4를 넘으면 줄의 최저점이 바늘보다 아래로 내려가 줄이 바늘 밑으로 배를 내밀었다가 올라오는 모양이 된다. 0.08이면 상단·중단·하단 모두 단조 하강이다(처짐 0.50 / 0.53 / 0.58).
- **갱신 순서가 중요하다.** `BoatFloatModule`은 `ModuleTick.LateUpdate` 모듈이고 `MonoThing.LateUpdate`가 「모듈 → `OnThingLateUpdate`」 순으로 돈다. 그래서 배 y·기울기가 확정된 **뒤** `FishingLineBoss.OnThingLateUpdate`가 줄을 그린다. 줄 위 끝이 낚싯대 끝에서 떨어지지 않는다. 바늘 위치는 그 앞 `Update`(FSM 모듈)에서 정해진다.
- 줄에는 `WaterInteractor`를 붙이지 않았다(물결은 바늘만 낸다).

#### 바늘 그림 = `Placeholder/Hook.png`

`Art/보스/낚시바늘.png`는 **줄과 바늘이 한 장**이라 그대로 쓸 수 없다(줄을 `LineRenderer`가 그리므로 두 줄이 겹친다). 알파 박스 102×956 px 중 **아래 169 px(위 기준 787~955행)만이 바늘 머리**이고 그 위는 굵기 2 px짜리 줄 한 줄이다.

두 가지 길이 있었다.

| 방법 | 판단 |
| --- | --- |
| `낚시바늘.png`를 `spriteMode: Multiple`로 바꾸고 서브 스프라이트 2개(`_hook`, `_line`)를 손으로 쓴 `.meta`에 넣는다 | **안 골랐다.** ① `Art/`는 읽기 전용 경로다. ② `BossArtPostprocessor`가 `OnPreprocessTexture`에서 `ApplyImportSettings`로 Single 모드를 다시 씌우므로 후처리기까지 고쳐야 한다. ③ 서브 스프라이트 `internalID`를 손으로 쓴 뒤 Unity를 못 돌려 보고 확인할 방법이 없다(에디터 금지). 틀리면 스프라이트 참조가 끊겨 바늘이 아예 안 보인다 |
| PIL로 잘라 `Assets/GameAssets/Placeholder/Hook.png`를 새로 만든다 | **골랐다.** 새 파일이라 `Art/` 미러를 건드리지 않고 후처리기 대상도 아니다. Single 모드라 `fileID: 21300000` 한 개로 끝난다. 원본 픽셀을 그대로 잘랐으므로 그림 품질은 원본과 같다 |

- 잘라낸 상자: `낚시바늘.png`의 (984, 787)~(1086, 956) → **102 × 169 px = 1.02 × 1.69 unit**.
- pivot **(0.44117647, 1)** = 잘린 그림의 위쪽, 줄 기둥(원본 x 1028~1029) 한가운데. 즉 **줄이 붙는 점**이다.
- guid `921b6e14231e4dfba912b064e6b039f1`. `.meta`는 손으로 썼다(다른 `Placeholder/*.png.meta`와 같은 형식, `alignment: 9` + `spritePivot`).
- 아트 요청: 「바늘 머리만 있는 PNG」를 받으면 이 자리표시자를 갈아 끼운다. `Docs/ArtRequestList.md`에 남긴다.

#### 바늘 두 개 (Single / Double)

- 프리팹에 `Hook0`, `Hook1` 두 벌이 미리 들어 있다(런타임 `Instantiate` 없음). 각각 `Hook{i}`(Kinematic `Rigidbody2D` + trigger `BoxCollider2D` + `WaterInteractor`, **`Hazard` 없음**) / `Hook{i}/HookSprite`(`Hook.png`) / `Hook{i}/Line{i}`(`LineRenderer`)다.
- `BeginCast(laneMask)`는 **마스크에 든 레인을 낮은 번호부터 순서대로 남은 바늘에 하나씩 물린다.** 그래서 Single이면 `Hook0` 하나, Double이면 `Hook0`·`Hook1`이 같은 낚싯대 끝에서 **동시에** 던져진다(줄도 2개). 마스크의 레인이 바늘 수보다 많으면(3레인 패턴 등) 남는 레인은 띠·판정만 서고 바늘 그림이 없다. 보스 1에는 3레인 패턴이 없다.
- 캐스트가 아닌 동안 `Hook0`은 낚싯대 끝에 매달린 채 **보이고**, `Hook1`은 꺼져 있다. 배가 화면 밖이면 둘 다 꺼진다.
- **바늘은 회전하지 않는다.** 줄이 별도 렌더러가 되면서 회전시킬 이유가 없어졌고, 세워 둬야 레인 띠(0.9) 안에 들어온다.
- `Hook{i}` transform은 **줄이 붙는 점**(바늘 머리 위)이다. 그래서 캐스트 목표 y는 `laneY + 바늘 반높이`(0.465)이고, 그 결과 바늘 몸이 레인 한가운데에 온다. 콜라이더도 `offset (0.033, −0.46475)`로 같이 내려 놓았다(`ApplyHookVisuals`가 `hookScale`에서 다시 계산한다).
- `WaterInteractor.NotifyTeleport`는 **`ResetHooks()`에서만** 부른다. 캐스트와 릴은 연속 이동이라 부르면 안 된다(부르면 속도 이력이 끊겨 물보라가 죽는다).

#### 배와 방향

`Art/오브젝트/배.png`(1920×1080 캔버스, 알파 박스 677×418 px, x 429~1105 / 위에서 y 60~477)에 **배·사람·낚싯대·노가 한 장에** 그려져 있다. pivot은 알파 박스 중심 `(0.3997396, 0.75092593)`(`ObjectArtPostprocessor` 규칙).

| 값 | 계산 |
| --- | --- |
| `Boat` `localScale` | **0.41** 균등. 배 몸통(429~1029 px, 600 px)이 2.46 unit이 된다 |
| `rodTipOffset` | **(−1.825, 2.06)** 로컬 unit. 낚싯대 끝 픽셀 (585, 63)에서 pivot 픽셀 (767.5, 269)을 뺀 값 ÷ 100. `boat.TransformPoint`가 스케일·기울기를 같이 먹이므로 월드 오프셋은 (−0.748, +0.845)다 |
| 낚싯대 끝 | `boatX` 2.5에서 **(1.752, 3.468)**. 카메라 위쪽 3.6 안이다 |
| `boatFloatOffset` | **0.623**. 배 몸통 높이의 35%를 물에 담근 값(pivot 아래 152 px × 0.41). 수면 2.0에서 pivot y 2.623, 배 밑바닥 1.77 |
| `boatX` | **2.5**. 플레이어(−4.2) 오른쪽이고 화면(±6.4) 안쪽이다. 배가 차지하는 범위는 1.39~4.17 |
| sorting | 12(플레이어 10 위, 물 앞판 15·예고 띠 16 아래) |

**등장·퇴장.** `boatSlide` 0↔1을 `boatEnterDuration`(0.8초)에 걸쳐 `MoveTowards`로 옮기고 `smoothstep`을 먹여 x를 `boatX + boatEnterOffsetX`(8.5, 화면 밖) ↔ `boatX`(2.5) 사이에서 보간한다. Intro 상태가 `BeginBoatEnter()`, Outro 상태가 `BeginBoatExit()`를 부른다(Intro 1.0 > 0.8, Outro 0.8 = 0.8). DOTween을 쓰지 않았다 — `BoatFloatModule`이 매 LateUpdate에 배 위치를 다시 쓰므로 x만 직접 미는 편이 트윈 정리 없이 안전하다. **공격마다 배가 옮겨 다니는 일은 이제 없다.**

**수면에 뜬다.** `BoatFloatModule`(`Game.Boss`, `ModuleTick.LateUpdate`)이 매 프레임 `WaterSurfaceSampler.TrySampleHeight(water, x)`로 배 x의 수면 높이를 읽어 `y = 수면 + boatFloatOffset`으로 놓고, `x ± boatSlopeSpan`(0.8)의 기울기를 `Atan2`로 각도로 바꿔 `boatTiltScale`을 곱해 회전시킨다. 둘 다 `1 − exp(−boatFloatSmoothing·dt)`로 감쇠시킨다(순간이동 때는 `Snap()`). 단차 연출로 물이 통째로 내려가도 배가 따라 내려간다. 물이 없으면 `GameConfig.WaterSurfaceY`로 떨어진다. **배에는 `WaterInteractor`를 붙이지 않았다** — 배 y를 수면이 정하는데 배가 수면을 다시 밀면 되먹임이 생긴다.

`WaterSurfaceSampler`는 `Game.Water`의 정적 헬퍼다(`WaterSystem.GetPositions` + `Water.bounds` + `WaterSettings.nodePerUnit` 선형 보간). `Game.Boss`는 이미 `Game.Water`를 참조한다.

**뱃머리 방향 (2026-09-06 결정).** 사용자 규칙은 「배는 연어와 같은 방향(상류, 오른쪽)으로 가고, 낚싯대는 고물(왼쪽)로 흘러 연어 쪽을 향한다」이다. 그림을 뜯어보면 **선체는 좌우 대칭**이다(양 끝 모양이 같은 나룻배). 뱃머리를 정하는 단서는 사람과 노뿐인데, 사람은 왼쪽을 보고 낚싯대를 왼쪽 위로 뻗고 노는 오른쪽으로 나와 있다. 그래서:

- **`flipX`를 걸지 않았다.** 대칭 선체라 뒤집을 「뱃머리」가 없고, 뒤집으면 낚싯대가 오른쪽(연어 반대쪽)을 향해 줄이 화면 밖으로 나간다.
- 읽는 방식: **오른쪽 끝이 이물(상류 방향), 왼쪽 끝이 고물**이고 사람은 고물 너머 왼쪽을 보며 낚는다. 줄과 바늘은 배 뒤(왼쪽)에 걸린다. 사용자 규칙과 화면상 결과가 같다.
- `rodTipOffset`도 그대로 (−1.825, 2.06)이다.
- 아트 요청은 **남기지 않았다.** 대칭 선체라 미러 버전이 필요 없다. 나중에 이물·고물이 뚜렷한 배 그림이 오면 그때 `flipX`와 `rodTipOffset.x` 부호를 같이 뒤집는다.

#### 패턴 (v8 기본 공격)

`Assets/Documents/정리 1번/7번 정리/…/02_BOSS1_낚싯줄_패턴명세.md`의 BASIC 6종이다. Single 60% / Double 40%가 되도록 가중치를 3과 2로 두었다(3·3+3·3+3·3 = 9, 2+2+2 = 6, 합 15).

| `label` | 마스크 | 가중치 | 확률 | 바늘 |
| --- | --- | --- | --- | --- |
| `Top` | 1 | 3 | 20% | 1개 |
| `Middle` | 2 | 3 | 20% | 1개 |
| `Bottom` | 4 | 3 | 20% | 1개 |
| `TopMiddle` | 3 | 2 | 13.3% | 2개 |
| `MiddleBottom` | 6 | 2 | 13.3% | 2개 |
| `TopBottom` | 5 | 2 | 13.3% | 2개 |

`allowRepeatPattern: 0`이라 직전과 같은 패턴은 연속으로 안 나온다. 어느 순간에도 위험 레인이 최대 2개라 회피 레인이 항상 1개 남는다(공정성 규칙 4).

**v8 복합 패턴(`SEQUENTIAL_12`, `ROUND_TRIP_12321`, `CROSS_132`, `PRESSURE_123`, `SAFE_LANE_SHIFT_321`)과 30초 Phase 구분, 「5종 최소 1회 등장 보장」은 이번 작업 범위가 아니다. 별도 후속 작업이다.** 실행기는 그 작업을 받을 수 있게 「한 스텝 = 레인 마스크 하나를 켠다」 단위로 짜 두었다: `BeginCast(mask)` → `UpdateCast(0~1)` → `BeginReel()`/`UpdateReel(0~1)` → `EndCast()`가 전부이고 스텝 사이 간격·순서는 페이즈가 정한다. 복합 패턴은 `FishingLineAttackPhase`에 스텝 목록과 `stepInterval`을 얹으면 된다.

#### 데이터 (`Assets/GameAssets/Design/Boss/FishingLineBossData.asset`)

공통 `BossData` 필드(30초·1.2/0.6/0.6/1.5·0.2·등장 1.0·퇴장 0.8·`useGameConfigTiming: 1`·`allowRepeatPattern: 0`) 위에 아래가 붙는다.

| 필드 | 값 | 뜻 |
| --- | --- | --- |
| `boatX` | 2.5 | 배가 서 있는 x. 싸움 내내 안 바뀐다 |
| `boatEnterOffsetX` | 6 | 등장 전·퇴장 뒤 x = `boatX + 6` = 8.5(화면 밖) |
| `boatEnterDuration` | 0.8 | 등장·퇴장 슬라이드 길이 |
| `boatFloatOffset` | 0.623 | 수면 위로 띄우는 높이 |
| `boatTiltScale` | 1 | 수면 기울기 → 배 회전 배율 |
| `boatSlopeSpan` | 0.8 | 기울기를 재는 좌우 폭 |
| `boatFloatSmoothing` | 12 | 높이·기울기 감쇠 |
| `rodTipOffset` | (−1.825, 2.06) | 배 로컬 좌표의 낚싯대 끝 |
| `lineAnchorOffset` | 1.2 | 배 참조가 비었을 때만 쓰는 대비값(수면 + 1.2) |
| `hookScale` | 0.55 | 바늘 균등 스케일. 0.561 × 0.9295 unit이 된다(띠 높이 0.9와 거의 같다) |
| `hookRestOffset` | 0.3 | 캐스트 사이에 바늘이 낚싯대 끝에서 매달리는 깊이 |
| `castDuration` | 0.6 | 캐스트 길이. 공격 창(0.6)과 같게 두면 창 끝에 플레이어 x에 닿는다 |
| `castArcHeight` | 0.35 | 캐스트 중간에 살짝 띄우는 높이(`sin(πt)`) |
| `reelDuration` | 0.25 | 감아 올리는 길이(ease-in) |
| `lineSag` | 0.08 | 줄 처짐 = 이 값 × 줄 길이 × `LineSlack` |
| `lineWidth` | 0.03 | `LineRenderer` 폭 |
| `lineSegments` | 6 | `LineRenderer` 정점 수(2~32로 잘린다) |
| `lineColor` | (0.94, 0.96, 1, 0.85) | 줄 색 |

없앤 필드: `hookParkOffset`, `hookEnterX`, `hookExitX`, `hookSweepDuration`, `hookBobAmplitude`, `hookBobFrequency`, `lineDragOffset`, `hookScaleMin`, `hookScaleMax`, **`boatTrail`**. `GetPlayerCrossRatio`도 없앴다(스윕 전용이었다).

#### 프리팹 (`Assets/GameAssets/Boss/FishingLineBoss.prefab`)

손으로 쓴 YAML이다. **`BossAssetSetup`은 더 이상 이 프리팹을 만들지 않는다**(곰과 같은 취급). `EnsureData`로 데이터만 챙기므로 `Regenerate Boss Prefabs (Overwrite)`를 돌려도 덮이지 않는다.

```text
FishingLineBoss (비활성)
├─ Telegraph        LaneTelegraph + Lane0..2
├─ LaneHazard       Band0..2 (판정)
├─ Hook0            RB2D Kinematic + BoxCollider2D trigger 0.561×0.9295 (offset 0.033, −0.46475) + WaterInteractor
│  ├─ HookSprite    Hook.png, scale 0.55, sorting 6, 렌더러 꺼짐
│  └─ Line0         LineRenderer, worldSpace, 정점 6, sorting 12, 렌더러 꺼짐
├─ Hook1            (Hook0와 같은 구성)
│  ├─ HookSprite
│  └─ Line1
└─ Boat             배.png, scale 0.41, sorting 12, 렌더러 꺼짐, localPosition (8.5, 2.623)
```

| 오브젝트 | fileID |
| --- | --- |
| 루트 `FishingLineBoss` GameObject / Transform / `FishingLineBoss` | 5665178560697729962 / 2868802319008579105 / 5475234172747987966 |
| `Hook0` GameObject / Transform / Rigidbody2D / BoxCollider2D / WaterInteractor | 4785247571625021678 / 3503302809823417006 / 2748191825399904301 / 6238142428385681250 / 349804221969562732 |
| `Hook0/HookSprite` GameObject / Transform / SpriteRenderer | 1796114401043176312 / 1446687976779525206 / 4394572134021847918 |
| `Hook0/Line0` GameObject / Transform / **LineRenderer** | 7710000000000000010 / 7710000000000000011 / **7710000000000000012** |
| `Hook1` GameObject / Transform / Rigidbody2D / BoxCollider2D / WaterInteractor | 7710000000000000020 / 7710000000000000021 / 7710000000000000022 / 7710000000000000023 / 7710000000000000024 |
| `Hook1/HookSprite` GameObject / Transform / SpriteRenderer | 7710000000000000030 / 7710000000000000031 / 7710000000000000032 |
| `Hook1/Line1` GameObject / Transform / **LineRenderer** | 7710000000000000040 / 7710000000000000041 / **7710000000000000042** |
| `Boat` GameObject / Transform / SpriteRenderer | 3921004650001110001 / 3921004650001110002 / 3921004650001110003 |

스프라이트 guid: 바늘 `921b6e14231e4dfba912b064e6b039f1`(`Placeholder/Hook.png`), 배 `8b1c47d2e0a34f5488d7c9a1b6e2f403`. `낚시바늘.png`(`4fca2523f07885141bbcaa1480150719`)는 **이제 프리팹에서 참조하지 않는다.**

`BossSet.prefab`은 루트 transform만 수정(override)하므로 자식이 늘어도 손댈 것이 없다.

#### 코드

| 파일 | 역할 |
| --- | --- |
| `FishingLineBoss.cs` | 배 위치·등장/퇴장, 바늘 배열, 캐스트/릴 계산, 줄 그리기 |
| `FishingLineHookRig.cs` | `[Serializable]` 바늘 한 벌(`root`/`visual`/`sprite`/`line`/`body`). 표시, 스케일, 콜라이더, 줄 정점 계산 |
| `FishingLineBossStates.cs` | `FishingLineIntroState`(배 진입) / `FishingLineAttackPhase`(예고→캐스트→릴) / `FishingLineOutroState`(배 퇴장 + `Complete(Passed)`) |
| `FishingLineBossData.cs` | 위 데이터 표 |
| `BoatFloatModule.cs` | 수면 부유·기울기(그대로) |

`FishingLineBoss`의 공개 표면(다른 보스가 흉내 낼 때 참고): `RodTip`, `HookRestPosition`, `HookHalfHeight`, `LineSlack`, `HookCount`, `CastingHookCount`, `BeginBoatEnter/BeginBoatExit`, `ResetHooks`, `BeginCast(mask)`, `UpdateCast(t)`, `BeginReel`, `UpdateReel(t)`, `EndCast`, `SetBoatVisible`.

### 보스 2 곰 (`BearBoss`, 2026-09-06 교체)

기획서 `Assets/Documents/정리 1번/6번 정리.md`(「보스 2 — 곰 최신 Unity 구현 명세」)로 **바다코끼리를 곰으로 갈아 끼웠다.** `BossSet.prefab`의 `bosses[1]`이 이제 `BearBoss.prefab` 인스턴스다. 바다코끼리 파일은 지우지 않고 남겼지만 참조하는 곳이 없다(아래 「바다코끼리(폐기)」).

```text
Intro(BearIntroState, IntroDuration 1.0초: 그림자 페이드인) ─▶ Attack(BearAttackPhase, 30초) ─▶ Outro(BearOutroState, OutroDuration 0.8초: 그림자 페이드아웃 + 왼쪽 퇴장) ─▶ Complete(Passed)
```

#### 곰은 화면에 없다

기획 1절 「전면에 실제 곰 캐릭터를 배치하지 않는다」를 그대로 따랐다. 게임플레이 레이어에 나오는 것은 **팔(발) 뿐**이고, 곰의 존재는 **수면 전체에 드리운 어두운 그림자**(`BearShadow`)로만 읽힌다. 곰 몸통 스프라이트도, 몸에서 팔까지 이어지는 긴 스프라이트도 없다.

**사용자 결정(2026-09-06)**: 강가에 곰 실루엣을 두지 않고 **물 전체를 덮는 반투명 검정 오버레이**로 한다. `BearShadow`는 `Placeholder/Square.png` 한 장을 폭 16 × 높이 6.5로 늘린 사각형이고, 윗변이 수면(`WaterSurfaceY` 2.0), 아랫변이 물 뒤판 바닥(-4.5)에 맞는다(중심 y -1.25). 나중에 곰 실루엣 그림이 오면 `BearBoss.silhouette` 슬롯(지금 비어 있음)에 넣는다.

| 값 | 기본 | 뜻 |
| --- | --- | --- |
| `shadowAlpha` | 0.45 | 유지 알파 |
| `shadowFadeIn` | 0.8 | 보스 시작(월드가 선 뒤) 페이드인 |
| `shadowFadeOut` | 0.5 | CLEAR 페이드아웃 |
| `shadowPulseAmplitude` / `shadowPulsePeriod` | 0.05 / 2.0 | 페이드인이 끝나면 알파가 0.40↔0.50으로 2초 주기 「숨쉬기」(`Yoyo` 루프) |
| `shadowExitDistance` | 12 | 퇴장 때 `OutroDuration` 동안 왼쪽으로 이 거리만큼 흘러 나간다 |

**정렬 순서 -3.** 이유: 물 뒤판(-6)·강바닥 모래(-5)·`LaneGuides`(-5)·`Homeland`(-4)보다 **앞**이라 수면 아래 배경이 전부 어두워지고, 게임플레이(장애물 5, 곰 팔 6, 연어 10)와 물 앞판(15)·레인 띠(16)보다 **뒤**라 연어·팔·빨간 띠는 밝기가 그대로다. -5와 -4 사이에 정수 자리가 없어 레인 안내선도 같이 어두워지지만 물에 잠긴 선이므로 물과 같이 어두워지는 편이 자연스럽다.

**월드와 함께 흘러 나가는 처리**: 프리팹을 `ScrollRoot` 밖에 두는 규칙(띠·판정이 월드 좌표에 고정되어야 한다)을 깨지 않으려고 **리페어런트 하지 않았다.** 대신 `BearOutroState`가 그림자를 왼쪽으로 `shadowExitDistance`만큼 밀면서 알파를 0으로 내린다. 화면에서는 「곰 구역이 왼쪽으로 빠진다」와 같게 읽힌다.

#### 팔 두 종류

| 오브젝트 | 스프라이트 | 쓰는 패턴 | 회전 | 스케일 |
| --- | --- | --- | --- | --- |
| `ArmBack` | `Art/보스/-곰-/곰 발 위아래.png` (손등) | 손등형 연속 공격 전용 | z **-90°** | 균등, 보이는 높이 = `armHeight` |
| `ArmSide0`, `ArmSide1` | `Art/보스/-곰-/곰 발 양옆.png` (손 옆면) | 직선 스윕 전용. Double이면 두 개를 같은 속도로 동시에 | 0° | 같음 |

- 원본 그림은 손등형이 **손목이 위**(claw가 아래)고, 옆면형이 **claw가 왼쪽·손목 쪽이 오른쪽으로 흐릿**하다. 오른쪽에서 들어오는 공격이므로 옆면형은 그대로 두고, 손등형만 z -90°로 돌려 손목이 오른쪽·claw가 왼쪽을 보게 했다. **위에서 아래로 떨어뜨리면 안전 레인을 가로지르므로** 세로 낙하는 쓰지 않았다(기획 12절 금지 항목).
- 각 팔은 `Arm*`(위치만 움직인다, 스케일 1) / `Arm*/Visual`(회전·스케일·`SpriteRenderer`) 두 단계다. `WaterInteractor`가 루트에 있고 스케일이 1이라 물결 상자(`size`)를 월드 단위로 그대로 적을 수 있다.
- **높이는 항상 레인 띠 안이다.** `BearBoss.ResolveArmHeight()`가 `min(armHeight, GameConfig.bossLaneBandHeight)` = 0.9로 자르고 `BearArmView.ApplyHeight`가 스프라이트 bounds로 균등 스케일을 계산한다. 레인 간격 1.1, 팔 높이 0.9이므로 팔의 위·아래 끝과 이웃 레인 띠 사이에 0.2 unit이 남는다. **안전 레인에 팔 그림이 걸치지 않는다.**

#### 패턴

가중치·타이밍은 전부 `BearBossData`(에셋)에 있다. 직전과 같은 패턴은 연속으로 나오지 않는다(`allowRepeatPattern: 0`). 어느 순간에도 위험 레인이 최대 2개라 **회피 레인이 항상 최소 1개** 있다.

| ID (`label`) | 마스크 | 가중치 | 예고 | 공격 | 휴식 | 팔 |
| --- | --- | --- | --- | --- | --- | --- |
| `BEAR_BACKHAND_SEQUENCE_123` | 7 | **25** | 0.5 (=`sequentialStepInterval`) | **1.18** (계산값) | 0.6 | `ArmBack` 1개를 3번 재사용 |
| `BEAR_SIDE_1` | 1 | 15 | 1.2 | 0.6 | 0.6 | `ArmSide0` |
| `BEAR_SIDE_2` | 2 | 15 | 1.2 | 0.6 | 0.6 | `ArmSide0` |
| `BEAR_SIDE_3` | 4 | 15 | 1.2 | 0.6 | 0.6 | `ArmSide0` |
| `BEAR_SIDE_12` | 3 | 15 | 1.2 | 0.6 | 0.6 | `ArmSide0` + `ArmSide1` |
| `BEAR_SIDE_23` | 6 | 15 | 1.2 | 0.6 | 0.6 | 같음 |

`[1,3]` 조합은 없다(기획 4절). 합계 100. SIDE는 `useGameConfigTiming`을 타므로 1.2/0.6/0.6은 `GameConfig`에서 온다. 순차 공격만 `BearAttackPhase.OnTelegraphBegin`이 그 사이클 동안 `TelegraphedAttackPhase.Timing`을 갈아 끼운다(예고=`stepInterval`, 공격=`(레인수-1)×stepInterval + sequenceHitActiveTime`, 휴식=데이터의 휴식).

#### 손등형 연속 공격의 시간표

순수 C# `BearSequenceTimeline`이 시간표를 갖고 있고 `BearAttackPhase`가 `IBearSequenceListener`로 받는다. 사이클 시작(예고 시작)을 t=0으로 두면:

| t | 일 |
| --- | --- |
| 0.00 | 1번 레인 빨간 경고 (`ShowTelegraph`, 예고 효과음) |
| 0.30 | 1번 띠 밝아짐(`telegraphImminentLead` 0.2) — 베이스 페이즈가 낸다 |
| 0.38 | `ArmBack`이 1번 레인 y·`laneSpawnX`(7.5)에서 출발 (`sequenceArmLead` 0.12) |
| **0.50** | **1번 타격**: 1번 레인 밴드 무장(`ArmLane(0)`), 2번 레인 경고 추가, 공격 효과음. 팔이 `PlayerX`에 도착 |
| 0.68 | 1번 무장 해제(`sequenceHitActiveTime` 0.18), 띠는 2번만 남음 |
| **1.00** | **2번 타격** + 3번 경고 |
| 1.18 | 2번 무장 해제 |
| **1.50** | **3번 타격**(경고 추가 없음) |
| 1.68 | 3번 무장 해제 = 공격 구간 끝 → 공통 휴식 0.6초 |

- **순서는 고정이다**(1→2→3, 위→가운데→아래). 랜덤이 아니다(기획 12절 금지).
- **한 번에 한 레인만 치명적이다.** 밴드는 `LaneHazard.Prepare(그 레인) + Arm()`으로 레인 하나씩만 무장한다. 나머지 두 레인은 안전하다.
- 빨간 띠는 「무장된 레인 + 다음 경고 레인」을 함께 보여준다. 타격이 끝나면 그 레인만 지운다. 이때 남은 경고 레인의 펄스가 한 번 다시 시작되므로 알파가 0.35쯤에서 0.3으로 튀지만 눈에 띄지 않는다.
- 팔 애니메이션은 `armEnterDuration` 0.12(들어오며 타격, `InQuad`) + `armReturnDuration` 0.13(오른쪽 복귀, `OutQuad`) = 0.25초로 기획 「0.20~0.30초」 안이다. 복귀가 끝나면 렌더러를 끈다. 0.5초 간격이라 다음 단계와 겹치지 않아 팔 하나를 계속 재사용한다.

#### 손 옆면 직선 스윕

- 예고 1.2초 동안 띠 표시 + `LaneHazard.Prepare(mask)` + 해당 레인들에 팔을 배치(화면 밖 `laneSpawnX` 7.5, 렌더러 꺼짐).
- 공격 시작에 `ArmLaneHazard()` + 렌더러 켜기 + 0.6초 동안 `PlayerX - sidePassOffset`(= -4.2 - 1.5 = **-5.7**)까지 `Ease.Linear` 이동. 「플레이어 x를 약간 지나치는 정도」(기획 4절).
- Double은 같은 프리팹 오브젝트 두 개가 **같은 프레임·같은 속도**로 움직인다. Double 전용 그림은 만들지 않았다.
- 공격이 끝나면 `BearBoss.ResetArms()`가 밴드 무장을 풀고 팔을 즉시 숨겨 `armParkX`(9)로 되돌린다. **복귀 중 판정 없음**(기획 4절)은 「복귀를 보여주지 않는다」로 구현했다.

#### 판정

v4 「레인 띠 = 판정」 그대로다. 팔 스프라이트에는 `Hazard`도 판정 콜라이더도 없다. 팔에 붙은 것은 물결용 `WaterInteractor`(`ignoreColliderShape: 1`, `wakeOnly: 1`, `size` 손등 1.25×0.9 / 옆면 1.03×0.9)뿐이고, 배치할 때마다 `WaterInteractor.NotifyTeleport`로 예열한다(낚싯바늘과 같다). 판정은 `LaneHazard/Band{lane}`(12.8 × 0.9)이 전부다.

기획 8절은 「콜라이더를 손 그림보다 5~10% 작게」라고 하지만 **v4 결정이 우선한다**: 빨간 띠와 판정이 정확히 같아야 하므로 판정은 레인 띠이고 팔은 연출이다. 대신 팔 높이를 띠 높이(0.9) 이하로 잘라 「경고보다 판정이 커 보이는」 일이 없게 했다.

점프는 그대로다. 곰은 공중 공격을 하지 않고, 물 밖 연어는 `LanePlayer.IsVulnerable`이 false라 어차피 맞지 않는다(기획 7절).

#### 월드 정지

`holdsWorld: 1`이다. 시작 0.5초 감속 → 정지 → 배너 1초 → 30초, 끝나면 새 공격을 예약하지 않고 진행 중인 사이클만 마친 뒤 CLEAR 1초 → 0.5초에 걸쳐 정상 속도 복귀. **0.5초 감속·복귀 램프는 `PlayFlow` 몫이라 `Docs/Requests.md`에 올렸다**(지금은 즉시 정지/재개다). 배너 이름 `bossNames[1]`을 "곰"으로 바꾸는 것도 같은 요청이다(지금은 "바다코끼리"로 뜬다).

#### 데이터 (`Assets/GameAssets/Design/Boss/BearBossData.asset`)

| 필드 | 기본 | 뜻 |
| --- | --- | --- |
| `sequentialStepInterval` | 0.5 | 손등형 단계 간격. 경고·타격이 이 간격으로 밀린다 |
| `sequenceHitActiveTime` | 0.18 | 단계마다 밴드가 무장돼 있는 시간(기획 「0.15~0.20」) |
| `sequenceArmLead` | 0.12 | 타격 시각보다 이만큼 먼저 팔이 출발한다 |
| `armEnterDuration` / `armReturnDuration` | 0.12 / 0.13 | 손등형 진입·복귀 |
| `armHeight` | 0.9 | 팔의 보이는 높이. 런타임에 `bossLaneBandHeight`로 한 번 더 잘린다 |
| `backStrikeOffset` | 0 | 손등형이 내리치는 x = `PlayerX + 이 값` |
| `sidePassOffset` | 1.5 | 스윕 도착 x = `PlayerX − 이 값` |
| `armParkX` | 9 | 대기 위치(화면 밖) |
| `laneSpawnX` | 7.5 × 3 | 레인별 등장 x |
| `shadowAlpha` … `shadowExitDistance` | 위 표 | 물 위 그림자 |
| `holdsWorld` | 1 | 보스 중 월드 정지 |
| `displayName` | 비움 → "곰" | `BearBossData.DefaultDisplayName` |

#### 프리팹 (`Assets/GameAssets/Boss/BearBoss.prefab`)

손으로 쓴 YAML이다(에디터를 열지 않았다). 루트는 비활성.

| 오브젝트 | fileID | 내용 |
| --- | --- | --- |
| `BearBoss` (루트) | GO 8300000000000000001 / T ...002 / `BearBoss` ...003 | 원점 고정. 루트는 절대 움직이지 않는다 |
| `Telegraph` | GO ...010 / T ...011 / `LaneTelegraph` ...012 | 펄스 0.4, 알파 0.3↔0.4, 밝음 0.45 |
| `Telegraph/Lane0..2` | GO ...020/030/040, SR ...022/032/042 | `Square.png`, 정렬 **16**, 색 (1, 0.2, 0.2, 0.35), 스케일 20 × 1.40625 (= 12.8 × 0.9) |
| `LaneHazard` | GO ...050 / T ...051 / `LaneHazard` ...052 | |
| `LaneHazard/Band0..2` | GO ...060/070/080 | 비활성. Kinematic `Rigidbody2D`(`useFullKinematicContacts`) + trigger `BoxCollider2D` 12.8 × 0.9 + `Hazard(kind: Bear)` |
| `BearShadow` | GO ...090 / SR ...092 | `Square.png`, 정렬 **-3**, 색 (0.04, 0.05, 0.07, **0**), 위치 (0, -1.25), 스케일 25 × 10.15625 (= 16 × 6.5) |
| `ArmBack` / `ArmBack/Visual` | GO ...100 / SR ...112 | `WaterInteractor`(...102), 손등 스프라이트, 정렬 6, 렌더러 꺼짐, `Visual` z -90° |
| `ArmSide0` / `Visual` | GO ...120 / SR ...132 | 옆면 스프라이트 |
| `ArmSide1` / `Visual` | GO ...140 / SR ...152 | 같음 |

`BearBoss` 컴포넌트의 `backArm`·`sideArms`는 `[Serializable] BearArmView`(root / visual / sprite / zRotation)다.

#### 바다코끼리(폐기)

`WalrusBoss.cs`·`WalrusBossData.cs`·`WalrusBossStates.cs`·`WalrusBoss.prefab`·`WalrusBossData.asset`은 **디스크에 남아 있고 컴파일되지만 아무도 참조하지 않는다.** 예전 구현은 「몸통 1개가 레인 높이로 커졌다 작아졌다 하며 왼쪽으로 돌진」이었고, 판정은 지금과 같은 레인 띠였다. 되살리려면 `BossSet.prefab`의 `bosses[1]`을 `WalrusBoss.prefab` 인스턴스로 되돌리고 `BossAssetSetup.BuildBossSetPrefab`의 `InstantiateBoss(BearPrefabPath, …)`를 `WalrusPrefabPath`로 되돌리면 된다. PlayMode `DebugJumpTests.WalrusTwoLaneBand_…`는 이름만 바다코끼리이고 실제로는 「2레인 패턴의 띠 = 판정」을 보는 테스트라 곰의 `BEAR_SIDE_12`/`BEAR_SIDE_23`에도 그대로 유효하다.

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

프리팹: 루트 / `Telegraph` / `LaneHazard/Band0..2` / `Waterfall`(1.5 × 8 unit 사각형, 판정 없음, `OnInitialize`에서 숨김) / `Rapid0..2`, `Rock0..2`(스프라이트만, 대기 중 GameObject 비활성).

#### 강한 물줄기 아트 (2026-09-06)

`Rapid0..2`가 자리표시자 사각형에서 `Art/보스/물결/물결1.png`·`물결 2.png` 2프레임 루프로 바뀌었다.

| 값 | 계산 |
| --- | --- |
| `localScale` | **0.2686567** 균등. 2장 합집합 알파 박스 1887 × 335 px에서 335 px(3.35 unit)를 레인 띠 높이 0.9에 맞춘 값. 화면 크기 5.07 × 0.90 |
| `SpriteRenderer.m_Color` | 흰색(아트 원색). 예전 `(0.5, 0.75, 1, 0.6)`은 흰 사각형을 물색으로 물들이던 값이다 |
| `WaterInteractor.size` | **18.87 × 3.35**(로컬 단위. 균등 스케일이 곱해져 월드 5.07 × 0.90). 예전 0.64 × 0.64는 사각형 스프라이트 기준이었다 |
| 애니메이션 | `Design/Animations/Boss_Rapid.asset`(2프레임, fps 6, loop) |

fileID: 트랜스폼 `6791995714468344889` / `7984106699806663890` / `2736239336361295421`, 렌더러 `788908573841248655` / `885771975225354904` / `6180084667013711339`, 인터랙터 `6277505320994043527` / `2356755070914930296` / `8754860628708313266`.

`WaterfallBoss`에 `[SerializeField] SpriteRenderer[] rapidRenderers`와 `CustomAnimation rapidClip`이 생겼다. `SetLaneVisible(lane, visible)`이 `SetRapidPlaying`을 불러 **레인이 켜질 때** 그 레인 렌더러용 `SpriteAnimatorModule`을 (없으면 만들어) `Play(rapidClip)`하고, 꺼질 때 `Stop()`한다. 모듈은 보스 루트에 붙으므로 `Rapid` GameObject가 비활성이어도 목록이 흐트러지지 않고, 보스 루트가 비활성인 동안에는 `MonoThing.LateUpdate`가 돌지 않아 저절로 멈춘다. `Game.Boss.asmdef`에 `Game.Animation` 참조 한 줄을 더했다(`Game.Animation`은 `Core.Modules`만 참조하므로 순환 없음).

**돌(`Rock0..2`)과 마지막 큰 폭포(`Waterfall`)는 자리표시자 그대로다**(회색 원, 옅은 파란 사각형). 아트 요청은 `Docs/ArtRequestList.md` 9번에 남아 있다.

## 받은 보스 아트

| 파일 | 쓰는 곳 | 상태 |
| --- | --- | --- |
| `Art/보스/낚시바늘.png` | (지금은 안 쓴다) | 줄과 바늘이 한 장이다. 줄을 `LineRenderer`로 그리게 되면서 바늘 머리만 잘라 `Placeholder/Hook.png`로 두었다(「보스 1 낚싯줄」). 바늘만 그린 PNG를 받으면 갈아 끼운다 |
| `Art/오브젝트/배.png` | 보스 1 `Boat` | **붙였다**(2026-09-06). `boatX`(2.5)에 고정, `BoatFloatModule`로 수면에 뜨고 낚싯대 끝이 줄 앵커다. 선체가 좌우 대칭이라 `flipX`는 걸지 않았다 |
| `Art/보스/물결/물결1.png`, `물결 2.png` | 보스 3 `Rapid0..2` | **붙였다**(2026-09-06). `Boss_Rapid` 2프레임 루프 |
| `Art/보스/-곰-/곰 발 양옆.png`, `곰 발 위아래.png` | 보스 2 `BearBoss` 팔 | 붙어 있다(곰 교체 때) |
| 보스 3 돌, 마지막 큰 폭포 | `Rock0..2`, `Waterfall` | **자리표시자**(회색 원 / 옅은 파란 사각형). 요청 `Docs/ArtRequestList.md` 9번 |
| 보스 2 바다코끼리 3상태 | (폐기된 `WalrusBoss`) | 안 쓴다. 요청 8번은 남겨 두었다 |

## 생성기

에디터 콘솔에 컴파일 오류가 없는 상태에서 `Team1004 > Generate Boss Assets`.

| 경로 | 내용 | 덮어쓰기 |
| --- | --- | --- |
| `Assets/GameAssets/Placeholder/Square.png`, `Circle.png` | 없을 때만 생성(코어루프 생성기와 같은 규격) | 안 함 |
| `Assets/GameAssets/Design/Boss/{FishingLine,Walrus,Waterfall,Bear}BossData.asset` | 없으면 생성. 있으면 값 보존, 패턴이 비어 있을 때만 기본 패턴 채움 | 안 함 |
| `Assets/GameAssets/Boss/{Walrus,Waterfall}Boss.prefab` | 없으면 생성, 있으면 경고 후 유지 | `Regenerate Boss Prefabs (Overwrite)`, 또는 코어루프 `Regenerate Core Loop Scenes (Overwrite)`가 `BossAssetSetup.Generate(true)`를 부른다 |
| `Assets/GameAssets/Boss/BossSet.prefab` | `BossDirector` + 세 프리팹의 중첩 인스턴스(비활성). `bosses = [낚싯줄, **곰**, 폭포]` | 위와 같음 |

`BearBoss.prefab`과 `FishingLineBoss.prefab`은 **생성기가 만들지 않는다**(손으로 쓴 YAML). `BossAssetSetup`은 `BearBossData.asset`만 `EnsureData`로 챙기고 `BossSet`을 짤 때 `BearPrefabPath`를 넣는다. 그래서 `Regenerate Boss Prefabs (Overwrite)`를 돌려도 곰 프리팹은 덮이지 않는다.

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
    public void SetBoss(string name);          // "낚싯줄", "곰", "폭포"
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
- 낚싯줄: 배 x 2.5(등장 전 8.5), 낚싯대 끝 (1.752, 3.468), 바늘 스케일 0.55(0.561×0.9295), 대기 깊이 0.3, 캐스트 0.6·아치 0.35, 릴 0.25, 줄 처짐 0.08·두께 0.03·정점 6·sorting 12.
- 곰: 등장 x 7.5(레인별), 대기 x 9, 손등 타격 x = `PlayerX`, 스윕 도착 x = `PlayerX − 1.5`, 팔 높이 0.9, 단계 간격 0.5·무장 0.18·팔 리드 0.12·진입 0.12·복귀 0.13, 그림자 16×6.5 · 알파 0.45(±0.05, 2초) · 정렬 -3 · 퇴장 12.
- 바다코끼리(폐기): x 3/-7/9, 몸 1.6×0.9 / 1.6×2.0, 접근 비율 0.6, 색 `mouthColor`(주황)·`armsColor`(붉은색).
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
| `BearBossPatternTests` | 곰 기본 패턴 표(라벨·마스크·가중치, 합 100), SIDE에 3레인 조합 없음·`[1,3]` 없음, 가중치 분포(시드 12345, 6만 회, 오차 ±0.02), 즉시 반복 금지 |
| `BearSequenceTimelineTests` | 손등형 시간표: 예고 0.5·공격 1.18·전체 1.68, 경고/타격/해제 시각, **동시에 치명적인 레인이 1개 이하**, 표시 마스크(무장 + 다음 경고), 팔 리드 0.12, `Advance`가 이벤트를 순서대로 한 번씩, `Reset` 재생 |

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

## 검증 (2026-09-06 곰 교체 패스)

에디터가 열려 있어 생성기·Unity 테스트를 돌리지 않았다. 씬은 손대지 않았다.

| 실행 | 결과 |
| --- | --- |
| `python <scratchpad>/csccheck_all.py` | `assemblies compiled: 41  failed: 0` |

새 파일: `BearBoss.cs`, `BearBossStates.cs`, `BearBossData.cs`, `BearArmView.cs`, `BearSequenceTimeline.cs`, `Tests/BearBossTests.cs`, `Assets/GameAssets/Design/Boss/BearBossData.asset`, `Assets/GameAssets/Boss/BearBoss.prefab`(+ 각 `.meta`). 고친 파일: `Assets/GameAssets/Boss/BossSet.prefab`(`bosses[1]` → 곰), `Assets/Scripts/Boss/Editor/BossAssetSetup.cs`(상수 2개 + `EnsureData<BearBossData>` + `BossSet`에 곰), `Assets/Scripts/Animation/Editor/ObjectArtPostprocessor.cs`(`Art/보스/**` 중 `BossArtPostprocessor`가 안 가져가는 파일을 per-file pivot으로 처리). **`PatternBoss.cs`·`TelegraphedAttackPhase.cs`·`BossThing.cs`는 건드리지 않았다**(카메라 담당과 동시 작업).

곰 팔 아트는 이미 동기화돼 있고 `.meta`도 이미 알파 바운딩 박스 중심 pivot으로 들어와 있었다(PIL로 확인).

| 파일 | 텍스처 | 알파 bbox(α≥8) | unit(PPU 100) | `spritePivot` |
| --- | --- | --- | --- | --- |
| `Art/보스/-곰-/곰 발 양옆.png` | 1920×1080 | x 884~1415, y 42~507 (531×465) | 5.31 × 4.65 | (0.5986979, 0.2541667) |
| `Art/보스/-곰-/곰 발 위아래.png` | 1920×1080 | x 792~1207, y 108~683 (415×575) | 4.15 × 5.75 | (0.5205729, 0.3662037) |

pivot은 **손목 쪽이 아니라 알파 bbox 가운데**로 두었다(`ObjectArtPostprocessor`/`BossArtPostprocessor`가 쓰는 프로젝트 공통 규칙과 같다). 팔이 오른쪽에서 들어오는 연출은 pivot이 아니라 트윈이 만든다. 런타임 스케일은 `armHeight` 0.9 / 보이는 높이이므로 옆면 0.1935(가로 1.03 unit), 손등 0.2169(회전 뒤 가로 1.25 unit)다.

에디터에서 확인할 것:

1. `BearBoss.prefab`이 깨지지 않고 열리는지. 팔 3개의 `Visual`에 곰 발 스프라이트가 붙고 `BearBoss` 인스펙터의 `backArm`/`sideArms`/`shadow`/`telegraph`/`laneHazard`/`data`가 전부 채워져 있는지.
2. `BossSet.prefab`의 두 번째 자식이 `BearBoss`이고 `BossDirector.bosses[1]`이 그것을 가리키는지.
3. 디버그 키로 보스 2에 들어가 (a) 물이 어두워지는지, (b) 순차 공격이 위→가운데→아래로 0.5초 간격인지, (c) 팔이 안전 레인을 침범하지 않는지, (d) Double 스윕에서 두 팔이 같은 속도인지.
4. EditMode `BearBossPatternTests`·`BearSequenceTimelineTests`.

## 검증하지 못한 위험 지점

- **v4 변경분은 눈으로 미확인이다.** 배치 검증(프리팹 재생성, EditMode, PlayMode `WalrusTwoLaneBand_…`로 2레인 띠 = 판정 확인)은 통합 패스에서 통과했다. `WalrusTwoLaneBand` 테스트는 플레이어 피격 → `PlayFlow.Fail()` → `BossDirector.Abort()`가 같은 프레임에 일어나므로 루프를 나온 뒤 `HasHit`를 판정한다.
- **`LaneHazard`가 실제로 맞히는지**가 가장 큰 위험이다. 판정이 안 되면 (1) 밴드 GameObject가 `Arm()`에 켜지는지, (2) 켜진 다음 물리 스텝에 `OnTriggerEnter2D`가 오는지, (3) `Rigidbody2D.useFullKinematicContacts`가 밴드·플레이어 양쪽에 있는지 순으로 본다. 반대로 **과하게 맞으면** `Disarm()`이 불리는 경로(공격 끝·`HideTelegraph`·`OnPhaseExit`·`OnComplete`)를 본다.
- **띠와 판정의 좌표.** 둘 다 `ApplyLayout`이 **월드** (0, laneY)에 놓는다. 보스 루트를 움직이는 트윈을 새로 추가하면 다시 어긋난다(바다코끼리를 `Body`로 옮긴 이유). 새 보스는 루트를 고정하고 자식만 움직인다.
- **낚싯줄 캐스팅(2026-09-06 재설계)은 전부 눈으로 미확인이다.** 손으로 쓴 `LineRenderer` YAML이 Unity 6.3에서 그대로 읽히는지, `Placeholder/Hook.png`의 손으로 쓴 `.meta`(pivot 0.44117647, 1)가 그대로 임포트되는지가 가장 큰 위험이다. 줄이 아예 안 보이면 ① `Line0/Line1`의 `m_Parameters.widthCurve`가 살아 있는지(비면 `widthMultiplier`를 곱해도 폭 0), ② 재질(`a97c105638bdf8b4a8650670310a4cd3`)이 붙어 있는지, ③ `m_UseWorldSpace: 1`인지 순으로 본다. 바늘이 안 보이면 `Hook.png`의 임포트 pivot을 먼저 본다.
- **캐스트 타이밍**은 계산으로만 맞췄다. `castDuration`(0.6)이 공격 창(`GameConfig.bossAttackDuration`)보다 짧으면 바늘이 먼저 도착해 창 끝까지 멈춰 있고, 길면 창이 끝날 때 아직 플레이어 x에 못 간다. 판정은 띠라 맞고 틀림은 안 바뀌지만 눈으로는 어긋나 보인다.
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

곰(2026-09-06) 쪽:

- **`BearBoss.prefab`·`BearBossData.asset`은 손으로 쓴 YAML이라 Unity가 한 번도 읽지 않았다.** 필드 이름 오타나 `BearArmView` 중첩 직렬화가 틀리면 인스펙터가 비어 보인다. 그때는 값을 손으로 채워 넣고 이 문서를 고친다. 특히 `sideArms`(배열 안 `[Serializable]` 클래스)와 `laneSpawnX`(float 배열)를 먼저 본다.
- **팔 스프라이트 방향.** 옆면형은 claw가 왼쪽이라 회전 없이 오른쪽→왼쪽 스윕에 맞고, 손등형은 z -90°로 돌려 손목이 오른쪽을 보게 했다. 그림이 뒤집혀 보이면 `BearArmView.zRotation`(프리팹 값)이나 `SpriteRenderer.m_FlipX`로 잡는다.
- **손등형 팔이 x 7.5 → -4.2를 0.12초에 지난다**(약 98 u/s). 화면에서 잔상 없이 「번쩍」으로 읽힐 수 있다. 느리게 하려면 `sequenceArmLead`와 `armEnterDuration`을 같이 키운다(둘이 같아야 타격 순간에 도착한다).
- **그림자 정렬 -3**은 `LaneGuides`(-5)와 `Homeland`(-4)도 어둡게 만든다. 레인 안내선이 안 보이면 `BearShadow`의 `m_SortingOrder`를 -5로 내려(모래·안내선 뒤) 물만 어둡게 한다.
- **그림자 윗변은 직선**이고 수면은 출렁인다. 수면 y 2.0 근처에서 경계선이 보이면 `WaterOverlay`처럼 수면 스플라인을 복사하는 폴리곤으로 바꾼다.
- **순차 공격 중 예고 펄스 재시작.** 한 단계의 타격이 끝나 그 레인을 지울 때 `LaneTelegraph.ShowMask`를 다시 부르므로 남은 경고 레인의 알파 펄스가 처음부터 시작된다. 눈에 거슬리면 `LaneTelegraphModule`에 레인 하나만 끄는 API를 넣는다(공용 파일이라 지금은 안 건드렸다).
- **`bossNames[1]`이 아직 "바다코끼리"다.** `PlayFlow`는 다른 담당이라 `Docs/Requests.md`로 넘겼다. 배너에는 바다코끼리가, 타이머 UI(`BossThing.DisplayName`)에는 곰이 뜬다.
- **월드 0.5초 램프도 아직 없다**(즉시 정지/재개). 같은 요청 행이다.

## StateMachine 모듈에 필요한 변경 요청

없다. 전이 중 `Stop` 예외는 지연 분리로 우회했다. `StateMachineModule`에 "다음 안전한 시점에 Stop" API가 생기면 `BossThing<TSelf,TKey>`의 `detachPending` 처리를 지울 수 있다(선택).

## 보스 중 월드 정지 (2026-09-06)

`BossData.holdsWorld`가 켜진 보스는 전투 중 배경 스크롤을 멈춘다(`PlayFlow.SetBossHoldsWorld` → 환경 `SetScrolling(false)`). 길을 막고 선 상대 앞에서 연어가 제자리에서 버티는 그림이다. 낚싯줄은 거슬러 오르는 중이라 흐르고, 곰·폭포는 정지한다. 장애물 스크롤 루트는 보스 중 원래 정지한다.

| 보스 | `holdsWorld` |
| --- | --- |
| 낚싯줄 | 0 |
| 곰 | 1 |
| 폭포 | 1 |

`BossThing.HoldsWorld`(virtual, 기본 false)를 `PatternBoss`가 데이터로 구현하고, `BossDirector`가 보스 시작 시 `flow.SetBossHoldsWorld(boss.HoldsWorld)`, 종료 시 `false`로 되돌린다.

보스 시작 전 월드 감속(2026-09-06): `holdsWorld` 보스는 배너 전에 `PlayFlow.RampEnvironmentSpeedAsync(1→0, 0.5초)`로 배경이 멈춘 뒤 배너·타이머가 시작하고, CLEAR 배너 뒤 0.5초에 걸쳐 복귀한다. 시간은 `PlayFlow.bossWorldRampDuration`. 어느 보스가 멈추는지는 `BossDirector`가 `BossHoldsWorldQuery`로 알려 준다.

### 곰 보스 수정 (2026-09-06, 플레이 확인 후)

- **팔이 안 보이던 원인**: `BearBoss.prefab`의 팔 3개 `SpriteRenderer.m_Sprite`가 메타의 `internalID`(음수 fileID)를 가리키고 있었다. 단일 스프라이트는 `fileID: 21300000`으로 참조해야 한다(돌·연어·긴돌 전부 이 규약). 세 곳을 21300000으로 고쳤다.
- **히트박스 직후 사망**: 순차 공격(`BEAR_BACKHAND_SEQUENCE_123`)의 첫 예고가 `stepInterval` 0.5초뿐이었다. v8 01절 「일반 첫 Step 예고 = 1.2초」에 맞춰 `BearSequenceTimeline`에 `firstTelegraph`(=`Timing.Telegraph` 1.2)를 넣어 첫 타격이 1.2초 뒤, 이후 0.5초 간격으로 진행되게 했다. 순차 총 길이 = 1.2 + 2×0.5 + 0.18 = 2.38초. 옆면 공격은 원래 1.2초였다.
- **그림자 표현 변경(2026-09-06)**: 수면 전체 대신 **화면 오른쪽 절반**(x 0~8.8)에만 드리우고 왼쪽 가장자리는 가로 그라데이션(`Placeholder/BearShadowGradient.png`, 왼쪽 35%에서 0→1)으로 흐려진다. 알파 0.45 → **0.7**(`BearBossData.shadowAlpha`). 위치·폭은 프리팹 `BearShadow` 트랜스폼(x 4.4, 스케일 3.4375 × 10.15625).

