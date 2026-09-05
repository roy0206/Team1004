# 보스 (Game.Boss)

FSM 기반 보스 베이스와 기획서 3판(`Team1004/Assets/Documents/정리 1번/3번 정리.md` 3·12~18·19.2·19.4절)의 세 보스(낚싯줄, 바다코끼리, 폭포) 구현이다. 보스는 "피한다, 버틴다, 통과한다"이며 조작은 ↑/↓/점프뿐이다. 세 보스는 같은 30초 형식(예고 0.8초 → 공격 0.6초 → 휴식 0.6초 → 반복 → 30초 종료 → 클리어, 3레인 전체 공격만 예고 1.0초)을 쓰고, 한 번 맞으면 실패해 해당 보스 처음부터 재시작한다.

컴파일은 에디터가 열려 있는 상태에서 파일이 임포트되어 콘솔로 확인했다(작성 중 `SpriteRenderer.DOFade` 오류 한 건이 있었고 `DOTween.ToAlpha`로 고쳤다). 실행(Play)은 확인하지 못했다. 「검증하지 못한 위험 지점」을 먼저 본다.

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
| `LaneTelegraph.cs`, `LaneTelegraphModule.cs` | `MonoThing` + `Module` | 레인 위험 표시(빨간 띠, 알파 펄스) |
| `HitboxView.cs`, `HitboxViewModule.cs`, `HazardHitbox.cs` | `MonoThing` + `Module`, static | 공격체 콜라이더 모양의 반투명 빨간 프레임. 「히트박스 표시」 |
| `PatternBoss.cs` | `PatternBoss<TSelf, TKey, TData>` | 데이터·타이머·텔레그래프·선택기를 묶은 선택 계층 |
| `CompleteOnTimeoutState.cs` | `TimedState` 파생 | 시간이 지나면 `Complete(outcome)` |
| `FishingLineBoss*.cs` | 보스 1 | 낚싯줄 |
| `WalrusBoss*.cs` | 보스 2 | 바다코끼리 |
| `WaterfallBoss*.cs` | 보스 3 | 폭포 |
| `Integration/BossDirector.cs` | `MonoThing` 호스트 | 구간 번호 → 보스 매핑, `PlayFlow.BossHandler` 연결, 타이머 UI 갱신 |

## 팀 규칙 적용

- 씬 오브젝트는 전부 `MonoThing` 호스트다. `BossThing`, `LaneTelegraph`, `BossDirector`가 호스트이고 `StateMachineModule`, `LaneTelegraphModule`이 모듈이다. 호스트는 `Update`/`LateUpdate`/`FixedUpdate`/`OnDestroy`를 선언하지 않는다(`BossThing<TSelf,TKey>`가 `OnThingLateUpdate`를 쓴다. 파생이 오버라이드하면 `base`를 부른다).
- 런타임 `Instantiate`·`new GameObject` 없음. 공격체(낚싯바늘, 바다코끼리 몸, 급류·돌 각 레인 1개)는 프리팹 안에 미리 자식으로 두고 `Hazard.enabled`와 위치로 켜고 끈다. 개수가 고정(레인 수)이라 풀은 쓰지 않았다.
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
| `useGameConfigTiming` | true | true면 아래 네 값 대신 `GameConfig.Current.BossDuration/BossTelegraphDuration/BossAttackDuration/BossRecoveryDuration`을 읽는다. CONFIG 한 곳 원칙. 보스마다 다르게 하려면 false |
| `duration` | 30 | 보스 시간 |
| `telegraphDuration` / `attackDuration` / `recoveryDuration` | 0.8 / 0.6 / 0.6 | 예고/공격/휴식 (3판 13절) |
| `fullLaneTelegraphDuration` | 1.0 | 3레인 전체 공격 예고(3판 13절). **`GameConfig`에 `BossFullLaneTelegraphDuration`이 아직 없어 이 필드만 쓴다.** 코어루프가 추가하면 `BossData.FullLaneTelegraphDuration`을 `useGameConfigTiming` 분기로 바꾼다 |
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

## 히트박스 표시 (`HitboxView`)

기획 4번 11절 "반투명 빨간 직사각형, 30~40% 투명도, 실제 공격 판정과 정확히 일치". 레인 띠(`LaneTelegraph`)와 별개로 **공격체의 `BoxCollider2D` 모양**을 그대로 보여 준다. 둘 중 무엇이 "히트박스 표시"인지는 `Docs/DesignQuestions.md`에 질문했고 지금은 둘 다 표시한다.

- `HitboxView : MonoThing`이 공격체(`Hazard`가 있는 오브젝트)에 붙는다. `frame`(자식 `HitboxFrame`의 `SpriteRenderer`, `SpriteDrawMode.Sliced`), `box`(형제 `BoxCollider2D`), `color`(빨강).
- `HitboxViewModule`(`Ticks = LateUpdate`)이 켜져 있는 동안 매 프레임 `frame.size = box.size × 부모 lossyScale`, `frame.localScale = 1/부모 lossyScale`(테두리 두께가 비율에 관계없이 같게), `frame.localPosition = box.offset`, 알파 = `GameConfig.bossHitboxAlpha`(0.35)로 맞춘다. 콜라이더 크기가 바뀌면(바다코끼리 입/팔) 자동으로 따라간다.
- `HazardHitbox.SetVisible(hazard, bool)`이 `TryGetComponent<HitboxView>`로 켜고 끈다. 낚싯줄: 예고 시작 `SetHookHitboxVisible(true)` → 공격 끝·`ResetHook`에서 끔. 바다코끼리: 예고 시작 `SetBodyHitboxVisible(true)` → `ResetStance`에서 끔. 폭포: 급류·돌은 공격 시작에 활성화되므로 `SetLaneHazard`가 판정과 함께 켜고 끈다(예고 중에는 화면 밖 대기라 띠만 보인다).
- 스프라이트는 `Assets/GameAssets/Placeholder/HitboxFrame.png`(64×64, 테두리 8px 불투명, 안쪽 25% 알파, 임포터 border 8px, FullRect)를 `BossAssetSetup`이 만든다. 아트 요청은 `Docs/Requests.md`. 정렬 순서 9(플레이어 10 바로 아래).
- 수면 상호작용: 바늘과 바다코끼리 몸에는 `Game.Water.WaterInteractor`도 붙어 수면을 뚫을 때 물이 반응한다(`Docs/Environment.md` 「수면 상호작용」).

## 파생 작성 절차

1. 상태 enum을 정의한다. 예: `enum MyBossState { Intro, Attack, Outro }`.
2. `PatternBoss<MyBoss, MyBossState, MyBossData>`(또는 형식이 다르면 `BossThing<MyBoss, MyBossState>`)를 상속한다. 씬에 필요한 참조(공격체 Transform, `Hazard`, SpriteRenderer)는 `[SerializeField] private`로 둔다.
3. `InitialKey`를 지정하고 `BuildStates(fsm)`에서 `fsm.Add(key, state)`로 상태를 등록한다. `TimedState`·`TelegraphedAttackPhase`·`CompleteOnTimeoutState`를 조합하거나 `State<MyBoss>`를 직접 쓴다. 생성자에 `fsm.Machine`을 넘긴다.
4. 공격 페이즈는 `TelegraphedAttackPhase<MyBoss, MyBossState>`를 파생해 `OnTelegraphBegin`(패턴 선택 + `boss.ShowTelegraph`), `OnAttackBegin`(공격체 이동 + `Hazard.enabled = true`), `OnAttackEnd`(판정 끄기), `OnPhaseExit`(트윈 Kill, 원위치)를 채운다.
5. 마지막 상태에서 `boss.Complete(BossOutcome.Passed)`를 부른다. 실패는 PlayFlow가 플레이어 `Hit`로 판정하므로 보스는 `Abort()`가 언제든 올 수 있다는 것만 대비한다(`OnExit`/`OnComplete`에서 정리).
6. `OnInitialize`에서 위치·판정·표시를 초기 상태로 되돌린다(재시작 시 다시 불린다). `Awake`에 의존하지 않는다(프리팹 루트가 비활성이라 `Begin`까지 `Awake`가 오지 않는다).
7. 데이터는 `BossData` 파생 SO로 두고 `CreateDefaultPatterns`에 기본 패턴을 적는다. 생성기(`BossAssetSetup`)에 데이터·프리팹 생성을 추가한다.

## 패턴 표현의 자유도

베이스는 FSM 뼈대만 준다. 공격 표현은 파생이 고른다.

| 방식 | 언제 | 예 |
| --- | --- | --- |
| 데이터 기반 레인 점유 | 위험 레인만 정하면 되는 보스 | 세 보스 전부 `BossPattern.laneMask` + `LaneTelegraph` |
| DOTween 시퀀스 | 공격체의 움직임·연출 | 낚싯바늘 하강/복귀, 바다코끼리 돌진, 급류·돌 통과, 폭포 돌파 |
| 코드로 짠 페이즈 | 조건 분기가 있는 흐름 | 폭포 `Breakthrough`(안전 구간 → 점프 대기 → 연출) |
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

한 사이클: 예고 시작에 레인 띠 표시, 바늘은 수면 위 대기 위치(`WaterSurfaceY + hookParkOffset`, x = `PlayerX`). 공격 시작에 바늘이 `Attack × hookDescendRatio`초 동안 목표 레인 y까지 내려오고 도착 순간 `Hazard`(kind `FishingHook`) 켜짐 → 공격 끝에 판정 끄고 띠 숨김 → 회복 시간 동안 대기 위치로 복귀. 바늘이 지나치는 위 레인은 안전하다(낚싯줄 스프라이트는 시각만).

`FishingLineBossData`: `hookParkOffset` 1.5, `hookDescendRatio` 0.4.

프리팹 `FishingLineBoss.prefab`: 루트(`FishingLineBoss`) / `Telegraph` / `Hook`(Rigidbody2D Kinematic, BoxCollider2D trigger 0.5×0.6, `Hazard` 비활성) / `Hook/HookSprite`(원, 0.5) / `Hook/Line`(세로 8 unit 흰 선).

### 보스 2 바다코끼리 (`WalrusBoss`)

```text
Intro(WalrusIntroState: ExitX→RestX 이동) ─▶ Attack(WalrusAttackPhase, 30초) ─▶ Outro(WalrusOutroState: ExitX로 퇴장) ─▶ Complete(Passed)
```

| 패턴 | 마스크 | 몸 | 설명 |
| --- | --- | --- | --- |
| MouthTop / MouthMiddle / MouthBottom | 1 / 2 / 4 | 높이 `singleBodyHeight`(0.9), `mouthColor` | 입 벌리고 1레인 돌진 (3판 15절 "1레인 = 입") |
| ArmsTopMiddle | 3 | 높이 `doubleBodyHeight`(2.0), `armsColor` | 팔+입, 상+중 |
| ArmsMiddleBottom | 6 | 같음 | 팔+입, 중+하 (3판 15절 "2레인 = 입+팔") |

한 사이클: 예고 시작에 띠 표시 + 몸 크기·색 전환 + `Telegraph × approachRatio`초 동안 마스크 중심 y로 이동(대기 x=`restX`). 공격 시작에 `Hazard`(kind `Walrus`) 켜고 `Attack`초 동안 `dashX`까지 왼쪽 돌진(플레이어를 지나친다). 공격 끝에 판정 끄고 몸을 원래대로, 회복 시간 동안 `restX`로 복귀. 플레이어와 같은 방향으로 가는 중이라 화면상 정지한 것으로 취급하며 추격 게임이 아니다.

`WalrusBossData`: `restX` 3, `dashX` -7, `exitX` 9, `bodyWidth` 1.6, `singleBodyHeight` 0.9, `doubleBodyHeight` 2.0, `approachRatio` 0.6.

프리팹: 루트(`WalrusBoss`, 위치 (restX, 중단 y)) / `Telegraph` / `Body`(스프라이트, Rigidbody2D, BoxCollider2D, `Hazard` 비활성). 몸 크기는 `SpriteRenderer.sprite.bounds`로 스케일을 계산하므로 스프라이트를 바꿔도 unit 크기가 유지된다.

### 보스 3 폭포 (`WaterfallBoss`)

```text
Intro(TimedState) ─▶ Attack(WaterfallAttackPhase, 30초) ─▶ Breakthrough(안전 구간 → 점프 대기 → 돌파 연출) ─▶ Complete(Passed)
```

| 패턴 | 마스크 | 점프 필수 |
| --- | --- | --- |
| Top / Middle / Bottom | 1 / 2 / 4 | |
| TopMiddle | 3 | |
| MiddleBottom | 6 | |
| All | 7 | ○ (상단 이동 후 점프, **예고 1.0초**) |

한 사이클: 예고 시작에 패턴 선택(`requiresJump` 패턴은 `!Player.IsAirborne && JumpCooldownRemaining <= FullLaneTelegraph`일 때만 후보. 3판 16절 "쿨타임 중이면 전체 공격을 내지 않는다") + 띠 표시. 전체 레인 패턴은 `GetTelegraphDuration`이 `Timing.FullLaneTelegraph`(1.0)를 돌려준다. 공격 시작에 마스크의 레인마다 `Rapid{lane}`(옅은 파란 사각형 = 강한 물줄기, `Hazard` kind `Rapid`)과 `Rock{lane}`(회색 원, `Hazard` kind `Rock`, `rockTrail`만큼 뒤)을 **활성화**해 `spawnX`에 놓고 `Attack`초 동안 `exitX`까지 선형 이동, 판정 켜짐. 공격 끝에 판정 끄고 **비활성화**해 `spawnX`로 되돌린다. 즉 물줄기 연출은 공격 구간에만 존재하고 그 외에는 배경 물 흐름만 남는다(3판 3절).

`Breakthrough`(3판 16절 "30초 종료 후"): 공격 생성 중지, 띠 숨김. 진입 즉시 **마지막 거대한 폭포**(`Waterfall` 스프라이트, 그 전까지 숨겨져 있음)가 `finalWaterfallEnterX`(9.5)에서 `waterfallX`(5.5)로 `finalWaterfallEnterDuration`(1초) 동안 들어온다. `safeWindowDuration`(2초)은 1번 레인으로 이동할 시간이다. 그 뒤 플레이어가 점프 가능(`!IsAirborne && JumpCooldownRemaining <= 0`)해지면 폭포가 깜박여 점프를 유도한다. **점프 쿨타임 0 보정은 `JumpModule`에 API가 없어 못 했다**(`Docs/Requests.md`에 `ResetCooldown` 요청). 대신 쿨타임이 끝날 때까지 안전 구간이 자연히 연장된다(공격이 없으므로 위험하지 않다). 이 상태에서 `LanePlayer.Jumped`가 오면(언제든) 폭포를 `JumpDuration` 동안 `exitX`로 흘려보내고 `Complete(Passed)`. 점프 자체는 `JumpModule`이 하고 보스는 연출만 얹는다. 플레이어가 없으면 안전 구간 뒤 바로 통과.

`WaterfallBossData`: `waterfallX` 5.5, `finalWaterfallEnterX` 9.5, `finalWaterfallEnterDuration` 1.0, `spawnX` 7.5, `exitX` -8, `rockTrail` 1.0, `safeWindowDuration` 2, `jumpCuePulseDuration` 0.35.

프리팹: 루트 / `Telegraph` / `Waterfall`(세로 8 unit 사각형, 판정 없음, `OnInitialize`에서 숨김) / `Rapid0..2`, `Rock0..2`(각 Rigidbody2D + trigger + `Hazard` 비활성, 대기 중 GameObject 비활성).

## 생성기

에디터 콘솔에 컴파일 오류가 없는 상태에서 `Team1004 > Generate Boss Assets`.

| 경로 | 내용 | 덮어쓰기 |
| --- | --- | --- |
| `Assets/GameAssets/Placeholder/Square.png`, `Circle.png` | 없을 때만 생성(코어루프 생성기와 같은 규격) | 안 함 |
| `Assets/GameAssets/Design/Boss/{FishingLine,Walrus,Waterfall}BossData.asset` | 없으면 생성. 있으면 값 보존, 패턴이 비어 있을 때만 기본 패턴 채움 | 안 함 |
| `Assets/GameAssets/Placeholder/HitboxFrame.png` | 없을 때만 생성. 임포터 border 8px는 매번 확인 | 안 함 |
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

플레이어 `HurtboxModule`은 트리거로 겹친 `Hazard`(`GetComponentInParent<Hazard>`)를 모아 두고, 매 프레임 `IsVulnerable`이고 `hazard.isActiveAndEnabled`인 것이 있으면 `Hit`를 한 번 낸다. 보스 공격체는 이 규칙에 맞춰 **`Hazard` 컴포넌트를 켜고 끄는 것**으로 위험 구간을 만든다. 콜라이더를 끄지 않아도 되고, 겹친 채로 `enabled = true`가 되면 그 프레임에 맞는다. 트리거 콜백에는 Rigidbody2D가 한쪽에 필요하므로 공격체에 Kinematic Rigidbody2D(`useFullKinematicContacts`)를 붙였다(플레이어도 Kinematic이라 양쪽 모두 필요).

`BossThing`은 `Hazard`를 상속하지 않는다(`MonoThing`을 두 번 상속할 수 없다). 본체가 곧 공격체인 보스(바다코끼리)도 자식 `Body`에 `Hazard`를 둔다.

## 임시값

- 등장 1.0초·퇴장 0.8초. 30초/0.8/0.6/0.6은 `GameConfig`(`useGameConfigTiming`; 코어루프가 0.6/0.6으로 바꿀 예정, 지금 에셋은 0.7/0.5). 전체 레인 예고 1.0과 밝아짐 0.2초는 `BossData`.
- 낚싯바늘: 대기 높이 수면+1.5, 하강 비율 0.4, 콜라이더 0.5×0.6, 줄 길이 8.
- 바다코끼리: x 3/-7/9, 몸 1.6×0.9 / 1.6×2.0, 접근 비율 0.6, 색 `mouthColor`(주황)·`armsColor`(붉은색).
- 폭포: x 5.5/9.5/7.5/-8, 급류 3×0.9, 돌 0.7, 돌 간격 1.0, 안전 구간 2초, 마지막 폭포 1.5×8 등장 1초.
- 레인 띠 높이 0.9, 알파 0.3~0.4 펄스 0.4초, 밝아짐 0.45, 색 빨강. 콜라이더 프레임 알파 `bossHitboxAlpha` 0.35.
- 폭포 3레인 패턴 조건 `JumpCooldownRemaining <= FullLaneTelegraph`.
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

PlayMode가 필요한 것(작성하지 않음): `BossThing.Begin`의 활성화·FSM 부착, `Complete` 뒤 비활성화, 전이 중 `Complete`의 지연 분리, 세 보스의 트윈·판정 타이밍, `BossDirector`와 `PlayFlow`의 왕복, `LaneTelegraph` 표시.

## 검증하지 못한 위험 지점

- **눈으로 미확인.** PlayMode `FullLoopTests`가 무적으로 세 보스를 통과(폭포는 점프로 돌파)하는 것까지 확인했다. 트윈 타이밍(바늘 도착 순간 판정, 돌진 중 판정)과 콜라이더 크기·히트박스 프레임 모양이 실제로 맞는지는 Play에서 본다. 판정이 안 되면 `Hazard.enabled`가 켜지는 시점과 `Rigidbody2D.useFullKinematicContacts`를 먼저 본다.
- `BossThing<TSelf,TKey>.AttachBehaviour`의 `(TSelf)this` 캐스트. 형식 매개변수의 유효 기반 클래스에서의 명시 변환이라 컴파일되는 것으로 보았고 콘솔 오류는 없었다.
- `private protected sealed override` 조합. 콘솔 오류 없음.
- `PatternBoss<…>`의 제네릭 기반 클래스 `[SerializeField] TData data`. Unity 2020.1+에서 제네릭 기반 필드 직렬화가 되지만 Inspector에 안 보이면 파생 클래스로 필드를 내린다.
- `BossDirector.RunBossAsync`가 `async Awaitable<bool>`. `PlayFlow.BossHandler` 시그니처(`Func<int, Awaitable<bool>>`)와 같다.
- `BossDirector`의 `[DefaultExecutionOrder(-50)]`이 `PlayFlow.Start`보다 앞선다는 가정. 체크포인트가 보스 직전일 때 첫 프레임에 보스가 건너뛰어지면 이 순서를 본다.
- 체크포인트로 보스 직전에서 시작할 때 `PlayFlow`가 컷신 최종 상태를 적용한 뒤 바로 `RunBossAsync`를 부르는지는 PlayFlow 쪽 구현에 달려 있다.
- `BossDirector`가 `BossTimerView.SetBoss/SetVisible`을 호출한다(연결됨).
- 폭포 3레인 패턴 조건(`JumpCooldownRemaining <= FullLaneTelegraph`)은 보수적이지 않다. 플레이어가 하단에서 상단까지 0.4초 이동 후 점프해야 하므로 실제로는 급류가 플레이어 x에 닿는 시각(공격 시작 + 약 0.5초)까지 여유가 있다. 회피 불가가 나오면 조건에 `LaneMoveDuration * 2`를 더한다.
- `WaterfallBreakthroughState`에서 플레이어가 점프하지 않으면 무한 대기. 기획 질문 등록. 점프 쿨타임 0 보정은 `JumpModule` API 요청 중(`Docs/Requests.md`).
- `LaneTelegraph` 띠는 화면 폭 12.8 unit 기준(카메라 ortho 3.6, 16:9). 카메라가 바뀌면 생성기 상수를 고친다.
- 생성기 `EnsureData`가 기존 데이터 에셋에 `EnsureDefaultPatterns`를 부른다. 기획이 패턴을 전부 지워 비워 두면 다음 생성 때 기본 패턴이 다시 채워진다.

## StateMachine 모듈에 필요한 변경 요청

없다. 전이 중 `Stop` 예외는 지연 분리로 우회했다. `StateMachineModule`에 "다음 안전한 시점에 Stop" API가 생기면 `BossThing<TSelf,TKey>`의 `detachPending` 처리를 지울 수 있다(선택).
