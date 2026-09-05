# StateMachine

`Game.StateMachine`은 특정 게임 타입에 의존하지 않는 범용 유한 상태 기계다. 순수 C# 코어(`StateMachine<TContext, TKey>`)와 `Core.Modules` 연동 계층(`StateMachineModule<TContext, TKey>`)으로 나뉘며, 씬 오브젝트에서는 `MonoThing` 호스트에 모듈로 붙이는 방식이 기본 사용 경로다. Unity 내장 Animator·StateMachineBehaviour는 쓰지 않는다.

## 구성

- 경로: `Assets/Scripts/StateMachine/`
- asmdef: `Game.StateMachine` (참조: `Core.Modules`만)
- 네임스페이스: `Game.StateMachine`
- 테스트: `Assets/Scripts/StateMachine/Tests/` (`Game.StateMachine.Tests`, EditMode, Editor 플랫폼)

| 파일 | 내용 | UnityEngine 의존 |
| --- | --- | --- |
| `IState.cs` | 상태 인터페이스 | 없음 |
| `State.cs` | 빈 가상 구현을 가진 추상 상태 | 없음 |
| `StateMachine.cs` | 코어 상태 기계 | 없음 |
| `TimedState.cs` | 지속 시간이 지나면 지정 상태로 넘어가는 상태 | 없음 |
| `StateMachineModule.cs` | `Module` 연동, `Time.deltaTime`으로 틱 | 있음 (이 파일만) |

네임스페이스 이름과 클래스 이름이 `StateMachine`으로 같지만 모든 타입이 제네릭이라 이름 조회가 충돌하지 않는다. 비제네릭 타입 `StateMachine`을 이 네임스페이스에 추가하면 `Game.*` 안에서 `StateMachine`이 네임스페이스로 해석되므로 추가하지 않는다.

## API

### `IState<TContext>`

```csharp
public interface IState<TContext>
{
    void OnEnter(TContext context);
    void OnUpdate(TContext context, float deltaTime);
    void OnFixedUpdate(TContext context, float fixedDeltaTime);
    void OnExit(TContext context);
}
```

### `State<TContext>`

```csharp
public abstract class State<TContext> : IState<TContext>
{
    public virtual void OnEnter(TContext context);
    public virtual void OnUpdate(TContext context, float deltaTime);
    public virtual void OnFixedUpdate(TContext context, float fixedDeltaTime);
    public virtual void OnExit(TContext context);
}
```

네 메서드 모두 빈 구현이다. 필요한 것만 오버라이드한다. 상태 객체는 상태 기계 인스턴스 하나에만 등록한다. 같은 인스턴스를 두 기계에 넣으면 `TimedState`의 내부 타이머가 공유된다.

### `StateMachine<TContext, TKey>`

```csharp
public sealed class StateMachine<TContext, TKey>
{
    public StateMachine(TContext context, IEqualityComparer<TKey> keyComparer = null);

    public event Action<TKey, TKey> StateChanged;

    public TContext Context { get; }
    public IState<TContext> Current { get; }
    public TKey CurrentKey { get; }
    public bool HasCurrent { get; }
    public IState<TContext> Previous { get; }
    public TKey PreviousKey { get; }
    public float TimeInState { get; }
    public bool IsTransitioning { get; }
    public int StateCount { get; }
    public string DebugLabel { get; }

    public void Add(TKey key, IState<TContext> state);
    public bool Contains(TKey key);
    public IState<TContext> Get(TKey key);
    public bool TryGet(TKey key, out IState<TContext> state);

    public void AddTransition(TKey from, TKey to, Func<TContext, bool> condition);
    public void AddAnyTransition(TKey to, Func<TContext, bool> condition);
    public void ClearTransitions();

    public void ChangeState(TKey key);
    public void Restart();
    public void Stop();

    public void Update(float deltaTime);
    public void FixedUpdate(float fixedDeltaTime);

    public override string ToString();
}
```

- `TContext`: 상태가 조작할 대상. 보스, 플레이어, 게임 흐름 컨트롤러 등 아무 타입이나 된다.
- `TKey`: 상태를 식별하는 키. enum을 권장하고 `string`, `Type`도 된다. 키 비교는 `EqualityComparer<TKey>.Default`이며 다른 비교자가 필요하면 생성자로 넘긴다. 키 조회는 `Add`·`AddTransition`·`ChangeState`에서만 일어나고 `Update`에서는 일어나지 않는다.
- `Current`·`CurrentKey`: 현재 상태. 없으면 `null`과 `default(TKey)`. 상태 유무는 `HasCurrent`로 본다.
- `Previous`·`PreviousKey`: 직전 상태. `Stop` 뒤에도 유지된다.
- `TimeInState`: 현재 상태에 들어온 뒤 `Update`로 누적된 시간(초). 전이·`Restart`·`Stop` 시 0이 된다.
- `DebugLabel`: 현재 키의 `ToString()`, 없으면 `(none)`. `ToString()`은 `StateMachine<Boss>[Phase1 12.30s]` 형식.
- `StateChanged(from, to)`: 전이가 끝난 뒤(새 상태의 `OnEnter` 이후) 발생. 첫 진입은 `from`이 `default(TKey)`, `Stop`은 `to`가 `default(TKey)`, `Restart`는 둘이 같다.

#### 등록

`Add`는 같은 키를 두 번 등록하면 `ArgumentException`, `state`가 `null`이면 `ArgumentNullException`을 던진다. 등록된 상태를 제거하는 API는 없다. 상태 집합은 생성 시점에 확정한다.

`Get`과 `ChangeState`, `AddTransition`, `AddAnyTransition`은 등록되지 않은 키에 `KeyNotFoundException`을 던진다. 로그 대신 예외를 쓰는 이유는 코어가 UnityEngine을 쓰지 않고, 잘못된 키는 데이터가 아니라 코드 버그이기 때문이다.

#### 전이 규칙

- `AddTransition(from, to, condition)`: `from`에 있을 때 `condition(context)`가 참이면 `to`로 간다. `from == to`는 `ArgumentException`.
- `AddAnyTransition(to, condition)`: 어느 상태에 있든 조건이 참이면 `to`로 간다. 이미 `to`에 있으면 평가 결과와 무관하게 무시한다.
- 전이 대상 상태는 `AddTransition` 호출 시점에 등록되어 있어야 한다. 그래야 `Update`에서 키 조회 없이 참조로 전이한다.
- 평가 순서: `Update` 안에서 `TimeInState`를 누적한 뒤, any 전이를 등록 순서대로, 그 다음 현재 상태의 전이를 등록 순서대로 평가한다. 처음 참인 것 하나만 실행하고, 그 프레임에는 더 평가하지 않는다.
- 전이가 일어나면 같은 `Update` 호출 안에서 새 상태의 `OnUpdate`가 같은 `deltaTime`으로 실행된다. 빈 프레임이 생기지 않는다.
- 조건 함수 안에서 `ChangeState`를 부르지 않는다. 조건은 읽기만 한다.
- 조건 없는 수동 `ChangeState`만 써도 된다. 전이 규칙은 선택이다.

#### 재진입 정책

`ChangeState`는 즉시 전이한다. 단 전이 도중(`OnExit`, `OnEnter`, `StateChanged` 핸들러 안)에 불리면 **큐잉**된다.

- 현재 전이가 끝난 뒤 큐에 있는 키로 이어서 전이한다. 여러 번 부르면 마지막 요청만 남는다.
- 큐잉된 키가 전이가 끝난 시점의 현재 상태와 같으면 무시한다.
- 예: `A.OnEnter`에서 `ChangeState(B)` → `A.OnEnter` 완료 → `StateChanged(none, A)` → `A.OnExit` → `B.OnEnter` → `StateChanged(A, B)`.
- 예: `A.OnExit`에서 `ChangeState(C)`를 부르는 상태에서 `ChangeState(B)` → `A.OnExit`(C 큐잉) → `B.OnEnter` → `B.OnExit` → `C.OnEnter`.
- `ChangeState(현재 키)`는 아무 일도 하지 않는다. 같은 상태를 다시 들어가려면 `Restart()`를 쓴다. `Restart`는 `OnExit` 뒤 `OnEnter`를 부르고 `TimeInState`를 0으로 만든다.
- `Update`, `FixedUpdate`, `Restart`, `Stop`을 전이 도중에 부르면 `InvalidOperationException`이다. `IsTransitioning`으로 확인할 수 있다.
- `OnEnter`·`OnExit`에서 예외가 나면 `IsTransitioning`은 `false`로 복구되지만 상태 참조는 예외 시점의 값으로 남는다.

`Stop()`은 현재 상태의 `OnExit`를 부르고 `Current`를 비운다. `OnExit` 안에서 `ChangeState`가 불렸다면 그 상태로 이어서 들어간다.

#### 틱

- `Update(deltaTime)`: `TimeInState` 누적 → 자동 전이 평가 → 현재 상태의 `OnUpdate`. 현재 상태가 없으면 아무것도 하지 않는다.
- `FixedUpdate(fixedDeltaTime)`: 현재 상태의 `OnFixedUpdate`만 부른다. 자동 전이는 평가하지 않는다.

### `TimedState<TContext, TKey>`

```csharp
public class TimedState<TContext, TKey> : State<TContext>
{
    public TimedState(StateMachine<TContext, TKey> machine, float duration, TKey next);

    public float Duration { get; set; }
    public TKey Next { get; set; }
    public float Elapsed { get; }
    public float Remaining { get; }
    protected StateMachine<TContext, TKey> Machine { get; }

    public sealed override void OnEnter(TContext context);
    public sealed override void OnUpdate(TContext context, float deltaTime);

    protected virtual void OnTimedEnter(TContext context);
    protected virtual void OnTimedUpdate(TContext context, float deltaTime);
    protected virtual void OnTimeout(TContext context);
    protected void Finish();
}
```

- `OnEnter`에서 `Elapsed`를 0으로 만들고 `OnTimedEnter`를 부른다. `OnUpdate`에서 `Elapsed`를 누적하고 `OnTimedUpdate`를 부른 뒤, 여전히 이 상태이고 `Elapsed >= Duration`이면 `OnTimeout`을 부른다. 기본 `OnTimeout`은 `Finish()`, 즉 `Machine.ChangeState(Next)`다.
- 지속 시간 전에 끝내려면(패턴 소진 등) `OnTimedUpdate`에서 `Finish()`를 부른다.
- `OnEnter`·`OnUpdate`는 `sealed`다. 하위 클래스는 `OnTimedEnter`·`OnTimedUpdate`·`OnTimeout`·`OnExit`·`OnFixedUpdate`를 오버라이드한다.
- `Duration`·`Next`는 런타임에 바꿀 수 있다. 값은 코드 상수가 아니라 `Assets/GameAssets/Design/` 데이터에서 읽어 넘긴다.
- 상태 기계 참조를 생성자로 받으므로 기계를 먼저 만들고 상태를 만든다. 모듈을 쓰면 `module.Machine`을 넘긴다.

### `StateMachineModule<TContext, TKey>`

```csharp
public sealed class StateMachineModule<TContext, TKey> : Module
{
    public StateMachineModule(
        TContext context,
        TKey initialKey,
        ModuleTick ticks = ModuleTick.Update,
        bool useUnscaledTime = false,
        bool exitOnDetach = true,
        IEqualityComparer<TKey> keyComparer = null);

    public StateMachine<TContext, TKey> Machine { get; }
    public TContext Context { get; }
    public TKey InitialKey { get; }
    public IState<TContext> Current { get; }
    public TKey CurrentKey { get; }
    public bool HasCurrent { get; }
    public IState<TContext> Previous { get; }
    public TKey PreviousKey { get; }
    public float TimeInState { get; }
    public string DebugLabel { get; }
    public event Action<TKey, TKey> StateChanged;

    public void Add(TKey key, IState<TContext> state);
    public void AddTransition(TKey from, TKey to, Func<TContext, bool> condition);
    public void AddAnyTransition(TKey to, Func<TContext, bool> condition);
    public void ChangeState(TKey key);
    public void Restart();
    public override string ToString();
}
```

- 모듈이 내부에 `StateMachine<TContext, TKey>`를 만든다. 자주 쓰는 멤버는 모듈이 그대로 노출하고, 나머지(`Get`, `TryGet`, `Contains`, `ClearTransitions`, `Stop`, `IsTransitioning`, `StateCount`)는 `Machine`으로 접근한다.
- `ticks`: `ModuleTick.Update`, `ModuleTick.FixedUpdate`, 또는 둘의 조합만 허용한다. 다른 값은 `ArgumentException`.
- `useUnscaledTime`: `true`면 `Time.unscaledDeltaTime`으로 틱한다. 일시정지 중에도 돌아야 하는 흐름용.
- `exitOnDetach`: `true`(기본)면 `RemoveModule`·`ClearModules`·호스트 파괴로 떨어질 때 현재 상태의 `OnExit`를 부른다. 파괴 시점에 `OnExit`가 이미 파괴된 오브젝트를 만지면 `false`로 둔다.
- 초기 상태는 **`AddModule`로 붙는 순간**(`OnAttached`) 들어간다. 따라서 `AddModule` 전에 `Add`로 상태를 전부 등록한다. 초기 키가 등록되지 않았으면 `AddModule`이 `KeyNotFoundException`을 던진다. 풀링으로 떼었다 다시 붙이면 초기 상태로 다시 들어간다.
- `IsEnabled = false`면 틱이 멈추고 `TimeInState`도 멈춘다. 수동 `ChangeState`는 계속 된다.
- `MonoThing`의 틱 스냅샷 규칙에 따라 `OnUpdate` 안에서 호스트의 모듈을 추가·제거해도 안전하다.

## 할당 정책

- `Update`·`FixedUpdate`·자동 전이·`ChangeState`·`StateChanged` 호출에서 힙 할당이 없다. 전이 목록은 `List<Transition>`(struct)이고 `for` 루프로 순회한다. LINQ·`foreach`·클로저 생성이 없다.
- `Func<TContext, bool>` 조건은 `AddTransition` 시점에 한 번 만들어 보관한다. 람다가 지역 변수를 캡처해도 그 시점의 한 번뿐이다.
- `DebugLabel`·`ToString()`은 문자열을 만들므로 매 프레임 부르지 않는다.
- 예외 메시지의 `$"..."`는 예외 경로에서만 실행된다.
- `TKey`가 enum이면 `Dictionary` 조회에서 박싱이 일어날 수 있는 런타임이 있다. 조회는 `ChangeState`·`Add` 등 드문 호출에서만 일어나므로 무시해도 되고, 신경 쓰이면 `keyComparer`로 전용 비교자를 넘긴다.
- `ClearTransitions`만 `foreach`(Dictionary.ValueCollection 열거)를 쓴다. 설정 단계 API다.

## 예제

### 보스 매크로 FSM (MonoThing 호스트, 기본 경로)

등장 → 페이즈 1 → 페이즈 2 → 퇴장. 페이즈는 지속 시간이 끝나거나 패턴이 소진되면 넘어간다. 보스 로직은 `Game.Boss` 쪽에 있고 이 모듈은 상태 흐름만 제공한다.

```csharp
using Game.StateMachine;
using UnityEngine;

namespace Game.Boss
{
    public enum BossPhase
    {
        Intro,
        Phase1,
        Phase2,
        Outro
    }

    public sealed class Boss : MonoThing
    {
        [SerializeField] private BossData data;

        private StateMachineModule<Boss, BossPhase> fsm;

        public BossData Data => data;
        public int Health { get; private set; }

        public void Initialize()
        {
            Health = data.MaxHealth;

            fsm = new StateMachineModule<Boss, BossPhase>(this, BossPhase.Intro);
            var machine = fsm.Machine;

            fsm.Add(BossPhase.Intro, new TimedState<Boss, BossPhase>(machine, data.IntroDuration, BossPhase.Phase1));
            fsm.Add(BossPhase.Phase1, new BossPatternPhase(machine, data.Phase1, BossPhase.Phase2));
            fsm.Add(BossPhase.Phase2, new BossPatternPhase(machine, data.Phase2, BossPhase.Outro));
            fsm.Add(BossPhase.Outro, new BossOutroState());

            fsm.AddAnyTransition(BossPhase.Outro, boss => boss.Health <= 0);
            fsm.StateChanged += OnPhaseChanged;

            AddModule(fsm);
        }

        public void TakeDamage(int amount)
        {
            Health -= amount;
        }

        private void OnPhaseChanged(BossPhase from, BossPhase to)
        {
            Debug.Log($"{name}: {from} -> {to}");
        }
    }

    public sealed class BossPatternPhase : TimedState<Boss, BossPhase>
    {
        private readonly BossPhaseData phase;
        private int patternIndex;
        private float cooldown;

        public BossPatternPhase(StateMachine<Boss, BossPhase> machine, BossPhaseData phase, BossPhase next)
            : base(machine, phase.Duration, next)
        {
            this.phase = phase;
        }

        protected override void OnTimedEnter(Boss boss)
        {
            patternIndex = 0;
            cooldown = 0f;
        }

        protected override void OnTimedUpdate(Boss boss, float deltaTime)
        {
            cooldown -= deltaTime;

            if (cooldown > 0f)
                return;

            if (patternIndex >= phase.Patterns.Count)
            {
                Finish();
                return;
            }

            var pattern = phase.Patterns[patternIndex++];
            boss.GetModule<BossAttackModule>().Fire(pattern);
            cooldown = pattern.Interval;
        }
    }

    public sealed class BossOutroState : State<Boss>
    {
        public override void OnEnter(Boss boss)
        {
            boss.GetModule<BossAttackModule>().IsEnabled = false;
        }
    }
}
```

`BossData`, `BossPhaseData`, `BossAttackModule`은 `Game.Boss` 영역에서 만든다. 지속 시간과 패턴 목록은 `Assets/GameAssets/Design/`의 데이터에서 온다. `AddAnyTransition`으로 체력 0을 어느 페이즈에서든 잡고, 이미 `Outro`면 무시된다.

### 플레이어 상태 (MonoThing 호스트)

물속 / 이동 / 점프 / 착수. 전이 규칙으로 물 감지를 자동화하고, 점프는 입력에서 수동으로 넘긴다.

```csharp
using Game.StateMachine;
using UnityEngine;

namespace Game.Player
{
    public enum SwimmerState
    {
        Underwater,
        Moving,
        Jumping,
        Landing
    }

    public sealed class Swimmer : MonoThing
    {
        private StateMachineModule<Swimmer, SwimmerState> fsm;

        public bool IsInWater { get; set; }
        public bool JumpPressed { get; set; }
        public Rigidbody2D Body { get; private set; }

        public void Initialize()
        {
            Body = GetComponent<Rigidbody2D>();

            fsm = new StateMachineModule<Swimmer, SwimmerState>(
                this, SwimmerState.Moving, ModuleTick.Update | ModuleTick.FixedUpdate);

            fsm.Add(SwimmerState.Underwater, new UnderwaterState());
            fsm.Add(SwimmerState.Moving, new MovingState());
            fsm.Add(SwimmerState.Jumping, new JumpingState());
            fsm.Add(SwimmerState.Landing, new TimedState<Swimmer, SwimmerState>(fsm.Machine, 0.2f, SwimmerState.Moving));

            fsm.AddTransition(SwimmerState.Moving, SwimmerState.Underwater, s => s.IsInWater);
            fsm.AddTransition(SwimmerState.Underwater, SwimmerState.Moving, s => !s.IsInWater);
            fsm.AddTransition(SwimmerState.Moving, SwimmerState.Jumping, s => s.JumpPressed);
            fsm.AddTransition(SwimmerState.Jumping, SwimmerState.Landing, s => s.Body.linearVelocityY <= 0f && s.IsInWater);

            AddModule(fsm);
        }

        public string CurrentStateName => fsm.DebugLabel;
    }

    public sealed class UnderwaterState : State<Swimmer>
    {
        public override void OnEnter(Swimmer swimmer)
        {
            swimmer.Body.gravityScale = 0.2f;
        }

        public override void OnFixedUpdate(Swimmer swimmer, float fixedDeltaTime)
        {
            swimmer.Body.AddForce(Vector2.up * 2f);
        }

        public override void OnExit(Swimmer swimmer)
        {
            swimmer.Body.gravityScale = 1f;
        }
    }

    public sealed class MovingState : State<Swimmer>
    {
        public override void OnUpdate(Swimmer swimmer, float deltaTime)
        {
        }
    }

    public sealed class JumpingState : State<Swimmer>
    {
        public override void OnEnter(Swimmer swimmer)
        {
            swimmer.JumpPressed = false;
            swimmer.Body.AddForce(Vector2.up * 8f, ForceMode2D.Impulse);
        }
    }
}
```

`IsInWater`·`JumpPressed`는 다른 모듈(충돌 감지, 입력)이 컨텍스트에 써 두고 전이 조건이 읽는다. 조건은 상태를 바꾸지 않는다.

### MonoThing이 아닌 MonoBehaviour (UI 한정 보조 예)

씬 오브젝트는 `MonoThing` + `Module`로 만드는 것이 팀 규칙이다. UI 컴포넌트처럼 `MonoBehaviour`를 직접 상속하는 경우에만 코어 클래스를 직접 틱한다.

```csharp
using Game.StateMachine;
using UnityEngine;

namespace Game.Title
{
    public enum MenuState
    {
        Idle,
        Opening,
        Open
    }

    public sealed class MenuPanel : MonoBehaviour
    {
        private StateMachine<MenuPanel, MenuState> machine;

        public void Initialize()
        {
            machine = new StateMachine<MenuPanel, MenuState>(this);
            machine.Add(MenuState.Idle, new IdleState());
            machine.Add(MenuState.Opening, new TimedState<MenuPanel, MenuState>(machine, 0.3f, MenuState.Open));
            machine.Add(MenuState.Open, new OpenState());
            machine.ChangeState(MenuState.Idle);
        }

        public void Open()
        {
            machine.ChangeState(MenuState.Opening);
        }

        private void Update()
        {
            machine.Update(Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            machine.Stop();
        }
    }
}
```

## 권장 설계에서 바꾼 점

- **키 타입을 제네릭 `TKey`로 했다.** 문자열 고정 대신 enum을 권장한다. 오타가 컴파일 오류가 되고, `switch`로 다룰 수 있다. `string`이 필요하면 `TKey = string`으로 쓰면 되므로 별도의 문자열 전용 파생 클래스(`StateMachine<TContext>`)는 두지 않았다. 타입 하나당 개념 하나를 유지하고 `sealed`로 둘 수 있기 때문이다.
- **`ChangeState(현재 키)`는 무시한다.** 참고 구현은 같은 상태로도 Exit/Enter를 다시 돌았다. 조건 전이와 섞이면 매 프레임 재진입하는 사고가 나기 쉬워, 명시적 재진입은 `Restart()`로 분리했다.
- **재진입은 큐잉이다.** 예외 대신 큐잉을 골랐다. `OnEnter`에서 곧바로 다른 상태로 넘어가는 패턴(조건 불충족 시 즉시 복귀)은 흔하고, 큐잉이면 Exit/Enter 순서가 항상 지켜진다. 대신 `Update`·`Stop`·`Restart`의 전이 중 호출은 예외로 막았다.
- **모듈이 상태 기계를 소유하고 초기 상태를 `OnAttached`에서 들어간다.** 생성자에서 컨텍스트와 초기 키를 받고, 자주 쓰는 API를 모듈이 직접 노출한다. `MonoThing` 호스트가 `AddModule` 한 번으로 시작하도록 하기 위해서다. 별도 초기화 호출은 없지만 `AddModule` 시점이 곧 명시적 시작 시점이다.
- **자동 전이 뒤 같은 프레임에 새 상태를 갱신한다.** 전이 조건은 이전 프레임까지의 데이터로 평가하고, 새 상태는 빈 프레임 없이 바로 돈다.
- **`FixedUpdate`에서는 자동 전이를 평가하지 않는다.** 전이는 `Update` 한 곳에서만 일어나야 순서를 추론하기 쉽다.
- **등록 해제 API를 두지 않았다.** 전이 목록이 상태 참조를 들고 있어 제거 시 정리가 필요한데, 첫 사용처인 보스에 필요 없다.
- **계층·하위 상태 기계는 만들지 않았다.** 필요해지면 상태 클래스 안에 별도의 `StateMachine<TContext, TSubKey>`를 두고 `OnEnter`에서 시작, `OnUpdate`에서 틱, `OnExit`에서 `Stop`하면 충분하다.
- **참고 구현의 전역 상태(`SetGlobalState`)는 `AddAnyTransition`으로 대체했다.** 매 프레임 실행되는 전역 상태가 필요하면 별도 모듈로 두는 편이 `MonoThing` 구조에 맞는다.

## 테스트

`Assets/Scripts/StateMachine/Tests/StateMachineTests.cs`. Test Runner의 EditMode 탭에서 실행한다. 등록·전이·Enter/Exit 순서, 자동 전이(source 한정, any 우선, 등록 순서), 전이 중 재진입 큐잉, `Update` 전이 중 호출 예외, `Stop`·`Restart`, `StateChanged` 인자, `TimeInState` 누적·초기화, `TimedState` 만료·조기 종료·재진입 초기화, 모듈 생성자 검증을 다룬다. `MonoThing` 부착은 GameObject가 필요해 테스트하지 않는다.

## 컴파일 검증을 못 한 위험 지점

작성 시점에 에디터가 열려 있어 컴파일을 돌리지 못했다. 처음 컴파일할 때 아래를 확인한다.

- `Game.StateMachine.Tests.asmdef`의 `UnityEngine.TestRunner`·`UnityEditor.TestRunner` 참조와 `nunit.framework.dll` precompiled 참조. Test Framework 1.6.0 기준이며 2.x로 올리면 참조 이름이 바뀔 수 있다.
- `StateMachineModule`의 `const ModuleTick AllowedTicks = ModuleTick.Update | ModuleTick.FixedUpdate`와 `(ticks & ~AllowedTicks) != 0`. enum 상수 조합과 enum-리터럴 0 비교는 C# 표준이지만 실제 확인은 안 했다.
- `StateMachine.cs`의 `var fromKey = from != null ? from.Key : default;` (C# 7.1 target-typed default). 프로젝트 언어 버전은 9.0이다.
- `TimedState.OnUpdate`의 `ReferenceEquals(machine.Current, this)`. `Current`는 `IState<TContext>`라 참조 비교로 썼다.
- 테스트의 `StateChanged` 인자 검증은 튜플 리터럴을 `Assert.AreEqual`에 넘기지 않고 `changes[i].From`·`.To`를 따로 비교한다. 튜플 리터럴에 `null`이 섞이면 `AreEqual(object, object)` 인자로 변환되지 않아 CS1503이 난다(첫 컴파일에서 확인).
- 테스트의 `StateMachine<FsmContext, string>` 형식 인자와 람다 `c => c.Flag`의 타입 추론. `Func<FsmContext, bool>` 매개변수에 직접 넘기므로 문제없어야 한다.
- `Module` 전역 타입과 `System.Reflection.Module` 충돌: 이 모듈의 파일은 `using`을 파일 최상단에만 두고 `System.Reflection`을 임포트하지 않는다.
