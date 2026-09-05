# Pool

`PoolManager`는 프리팹 복제본의 생성, 재사용, 파괴를 담당한다. 원본 프리팹은 `ResourceManager`가 소유하며 풀은 리소스를 로드하지도 해제하지도 않는다.

## 구성

- `Core.Pool`: 풀 등록, 인스턴스 수명, 지연 반납, Scene 이벤트 연동

`Core.Resource`와 `Core.Scene`을 참조한다. 라벨 등록과 Scene 연동을 쓰지 않으면 두 모듈 없이도 `Register`만으로 동작한다.

## 초기화

`PoolManager`를 시작 Scene 또는 Bootstrap Scene에 배치한다. 기본값으로 충분하면 `Initialize` 호출은 필요 없다.

```csharp
PoolManager.Instance.Initialize(new PoolSettings
{
    MaxPooledPerKey = 64,
    UseUnscaledTime = false
});
```

| 설정 | 의미 |
| --- | --- |
| `MaxPooledPerKey` | 키마다 대기 상태로 보관할 최대 인스턴스 수. `0`이면 무제한이며, 초과한 반납분은 풀에 넣지 않고 파괴한다. 음수는 `Initialize`에서 예외를 던진다 |
| `UseUnscaledTime` | 지연 반납이 `Time.unscaledTime`을 기준으로 하는지 여부. `false`면 `Time.time`을 쓰므로 `timeScale`이 `0`일 때 반납이 멈춘다 |

풀이 하나라도 등록된 뒤에는 `Initialize`를 호출할 수 없다. 두 번째 `Initialize`도 예외다. 이미 초기화했는지는 다른 Manager와 같이 `IsInitialized`로 읽는다. `Initialize`를 부르지 않아도 기본값으로 동작하므로 `IsInitialized`가 `false`인 것이 곧 사용 불가를 뜻하지는 않는다.

## 등록

프리팹을 직접 등록하거나, `ResourceManager`가 이미 로드한 라벨을 통째로 등록한다.

```csharp
PoolManager.Instance.Register("Bullet", bulletPrefab, prewarmCount: 32);

await ResourceManager.Instance.LoadLabelAsync("Enemies");
PoolManager.Instance.RegisterLabel("Enemies", prewarmCount: 4);
```

`Register`의 원본은 프리팹 에셋일 필요가 없다. 런타임에 만든 GameObject도 등록할 수 있으며, 비활성 상태로 만들어 두면 원본 자체가 씬에 보이지 않는다.

`RegisterLabel`은 라벨이 로드되지 않았거나 프리팹이 하나도 없으면 실패시킨다. 풀의 키는 리소스 키와 같다. 라벨의 키 하나라도 다른 프리팹에 이미 등록되어 있으면 아무것도 등록하지 않고 실패시킨다.

같은 키에 다른 프리팹을 등록하면 실패시킨다. 같은 키에 같은 프리팹을 다시 등록하면 `prewarmCount`만 추가로 적용된다.

## 획득과 반납

`Get`은 `PooledInstance`를 반환한다. `GameObject`, `Transform`, `Key`를 함께 담으므로 인스턴스에 별도 컴포넌트를 붙이지 않는다.

```csharp
var bullet = PoolManager.Instance.Get("Bullet", muzzle.position, muzzle.rotation);

if (bullet.TryGetComponent(out Projectile projectile))
    projectile.Launch(direction);

PoolManager.Instance.Release(bullet);
```

부모만 지정하는 형태는 부모 기준 원점에 배치한다. 부모를 지정하지 않으면 활성 Scene의 루트에 놓인다.

```csharp
var icon = PoolManager.Instance.Get("SlotIcon", slotTransform);
```

등록되지 않은 키는 `Get`이 경고를 한 번 남기고 무효한 `PooledInstance`를 반환한다. 조회 실패가 정상 흐름이면 `TryGet`을 쓴다.

```csharp
if (PoolManager.Instance.TryGet("Optional", position, rotation, out var instance))
    instance.Transform.SetParent(container, true);
```

`GameObject`만 알고 있는 곳에서는 `Release(GameObject)`를 쓴다.

```csharp
private void OnTriggerEnter(Collider other)
{
    PoolManager.Instance.Release(other.gameObject);
}
```

## 낡은 핸들

`PooledInstance`는 값 타입이며 획득 시점의 세대 번호를 함께 담는다. 이미 반납되어 다른 용도로 재사용 중인 인스턴스를 낡은 핸들로 반납하려 하면 조용히 무시된다.

```csharp
var handle = PoolManager.Instance.Get("Bullet", position, rotation);
PoolManager.Instance.Release(handle);

PoolManager.Instance.Release(handle);      // 무시된다
PoolManager.Instance.IsActive(handle);     // false
```

`Release(GameObject)`에는 이 보호가 없다. 같은 `GameObject`가 이미 재사용 중이면 그 사용처가 반납된다. 핸들을 보관할 수 있으면 항상 핸들로 반납한다. `Release(GameObject)`는 충돌 콜백처럼 핸들을 얻을 수 없는 곳에서만 쓴다.

## 지연 반납

```csharp
PoolManager.Instance.Release(explosion, 2f);
```

대기 시간은 `Update`에서 처리하며 Coroutine을 쓰지 않는다. 대기 중에 인스턴스가 반납되고 다시 획득되면 예약은 취소된다.

## IPooledObject

재사용 전후로 상태를 초기화해야 하는 프리팹만 구현한다. 인스턴스 생성 시 자식까지 한 번만 수집해 캐싱하므로 구현하지 않은 프리팹에는 비용이 없다.

```csharp
public sealed class Projectile : MonoBehaviour, IPooledObject
{
    public void OnSpawned()
    {
        trail.Clear();
    }

    public void OnReleased()
    {
        body.linearVelocity = Vector3.zero;
    }
}
```

`OnSpawned`는 `SetActive(true)` 직후에, `OnReleased`는 `SetActive(false)` 직전에 호출된다. 두 콜백 모두 오브젝트가 활성 상태일 때 실행된다.

## Scene 연동

`PoolSceneBridge`를 연결하면 Scene을 떠나기 전에 활성 인스턴스를 모두 회수한다. 회수하지 않으면 Scene에 재배치된 인스턴스가 Scene 언로드와 함께 파괴된다.

```csharp
var bridge = new PoolSceneBridge(
    PoolManager.Instance,
    SceneController.Instance);

bridge.Attach();
```

## 정리

```csharp
PoolManager.Instance.ReleaseActive();
PoolManager.Instance.Clear("Bullet");
PoolManager.Instance.ClearLabel("Enemies");
PoolManager.Instance.ClearAll();
```

`ReleaseActive`는 활성 인스턴스를 풀로 되돌린다. `Clear` 계열은 대기 인스턴스와 활성 인스턴스를 모두 파괴하고 등록을 해제한다.

`Clear`는 원본 프리팹을 해제하지 않는다. 리소스까지 회수하려면 풀을 먼저 비우고 `ResourceManager.ReleaseLabel`을 호출한다.

```csharp
PoolManager.Instance.ClearLabel("Enemies");
ResourceManager.Instance.ReleaseLabel("Enemies");
```

## 제한

인스턴스는 비활성 `PoolRoot` 아래에서 생성되므로 생성 시점에 `OnEnable`이 실행되지 않는다. 첫 활성화는 `Get`이 위치를 지정한 뒤에 일어난다.

반납 시 부모는 `PoolRoot`로, 크기는 프리팹 값으로 되돌린다. 위치와 회전은 되돌리지 않으므로 위치를 지정하지 않는 `Get`을 쓸 때만 부모 기준 원점으로 초기화된다.

활성 인스턴스를 외부에서 `Destroy`하면 `ReleaseActive`나 `Clear` 계열이 호출될 때까지 내부 목록에 남는다. Scene 전환마다 `PoolSceneBridge`가 정리한다.
