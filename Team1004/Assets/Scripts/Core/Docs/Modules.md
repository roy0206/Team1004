# Modules

`MonoThing`은 일반 C# 객체인 `Module`을 담고 틱을 분배하는 `MonoBehaviour`다. 하나의 오브젝트가 가진 기능을 컴포넌트 대신 모듈로 나눠 런타임에 붙이고 뗀다.

## 구성

- `Core.Modules`: `MonoThing`, `Module`, `ModuleTick`

다른 Core 모듈에 의존하지 않는다.

## 모듈 작성

모듈은 필요한 틱을 `Ticks`로 선언한다. 선언하지 않은 틱은 순회 대상에서 제외되므로 쓰지 않는 콜백의 비용이 없다. `Ticks`는 추상 멤버라서 선언을 빠뜨릴 수 없다.

```csharp
public sealed class HealthModule : Module
{
    private readonly int maxHealth;
    private readonly float regenPerInterval;

    private int current;

    public int Current => current;

    public HealthModule(int maxHealth, float regenPerInterval)
    {
        this.maxHealth = maxHealth;
        this.regenPerInterval = regenPerInterval;
        current = maxHealth;
    }

    protected override ModuleTick Ticks => ModuleTick.Interval;

    protected override void OnInterval()
    {
        current = Mathf.Min(maxHealth, current + Mathf.RoundToInt(regenPerInterval));
    }
}
```

의존성은 생성자로 받는다. 여러 틱이 필요하면 플래그를 조합한다.

```csharp
protected override ModuleTick Ticks => ModuleTick.Update | ModuleTick.FixedUpdate;
```

틱이 필요 없는 상태 보관용 모듈은 `ModuleTick.None`을 선언한다.

## 호스트 작성

```csharp
public sealed class Enemy : MonoThing
{
    private HealthModule health;

    private void Awake()
    {
        health = AddModule(new HealthModule(100, 2f));
        AddModule(new PatrolModule(waypoints));
    }
}
```

`AddModule`은 넘긴 타입을 그대로 반환하므로 캐스팅이 필요 없고, 붙이는 즉시 틱을 받는다. 별도의 초기화 호출이 없다.

## 조회와 제거

```csharp
var health = thing.GetModule<HealthModule>();

if (thing.TryGetModule(out PatrolModule patrol))
    patrol.IsEnabled = false;

thing.RemoveModule<PatrolModule>();
thing.RemoveModule(health);
thing.RemoveModules<WeaponModule>();
thing.ClearModules();
```

같은 타입 모듈을 여러 개 다룰 때는 버퍼를 받는 형태를 쓴다. 매 프레임 호출해도 할당이 없다.

```csharp
private readonly List<WeaponModule> weapons = new();

private void Fire()
{
    thing.GetModules(weapons);

    for (var i = 0; i < weapons.Count; i++)
        weapons[i].Fire();
}
```

`IsEnabled`를 `false`로 두면 모듈을 떼지 않고 틱만 멈춘다. 기본값은 `true`다.

## 주기 틱

`ModuleTick.Interval`은 `IntervalDuration` 간격으로 호출한다. 기본값은 1초이며 호스트마다 설정한다.

```csharp
thing.IntervalDuration = 0.5f;
```

한 프레임에 최대 한 번만 호출한다. `IntervalDuration`이 프레임 시간보다 짧아도 프레임당 한 번이다. 값은 `0.01` 미만으로 내려가지 않는다. 긴 프레임 뒤에 밀린 호출은 한 번까지만 따라잡고 나머지는 버리며, `IntervalDuration`을 바꾸면 누적 시간이 초기화된다.

## Unity 콜백

`MonoThing`이 `Update`, `LateUpdate`, `FixedUpdate`, `OnDestroy`를 사용한다. 하위 클래스에서 이 네 개를 직접 선언하면 `MonoThing`의 것이 가려져 **모듈이 틱을 받지 못한다.** 대신 아래를 오버라이드한다.

| Unity 콜백 | 오버라이드할 것 |
| --- | --- |
| `Update` | `OnThingUpdate` |
| `LateUpdate` | `OnThingLateUpdate` |
| `FixedUpdate` | `OnThingFixedUpdate` |
| `OnDestroy` | `OnThingDestroy` |

`Awake`, `Start`, `OnEnable`, `OnDisable`은 `MonoThing`이 쓰지 않으므로 하위 클래스가 그대로 선언한다.

모듈 틱은 호스트 자신의 훅보다 먼저 실행된다. `OnDestroy`에서는 `ClearModules`가 먼저 돌아 모든 모듈의 `OnDetached`가 호출된 뒤 `OnThingDestroy`가 실행된다.

## 틱 중 모듈 변경

틱은 목록의 스냅샷을 순회하므로 `OnUpdate` 안에서 모듈을 추가하거나 제거해도 안전하다.

- 틱 도중 추가한 모듈은 그 프레임의 남은 순회에 참여하지 않고 다음 틱부터 받는다.
- 틱 도중 제거한 모듈은 이미 스냅샷에 있더라도 호출되지 않는다.

## 풀링과 함께 쓰기

`MonoThing`은 `Core.Pool`에 의존하지 않는다. 풀링 대상이면 하위 클래스가 `IPooledObject`를 구현해 재사용 시점을 직접 연결한다.

```csharp
public sealed class Bullet : MonoThing, IPooledObject
{
    public void OnSpawned()
    {
        AddModule(new BulletMoveModule(speed));
    }

    public void OnReleased()
    {
        ClearModules();
    }
}
```

## 이름 주의

`Module`은 전역 네임스페이스 타입이다. `using` 지시문을 네임스페이스 블록 **안**에 두고 그 안에서 `System.Reflection`을 임포트하면 `Module`이 `System.Reflection.Module`로 해석된다.

```csharp
namespace Game
{
    using System.Reflection;

    public class Broken
    {
        private void M(Module module) { }
    }
}
```

`using`을 파일 최상단에 두면 전역 선언이 우선하므로 문제가 없다.
