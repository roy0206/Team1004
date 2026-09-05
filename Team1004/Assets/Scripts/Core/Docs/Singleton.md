# Singleton

`Core.Foundation`은 하나만 유지되는 `MonoBehaviour`의 기반 클래스를 두 가지 제공한다.

| | `Singleton<T>` | `DomainSingleton<T>` |
| --- | --- | --- |
| 수명 | 애플리케이션 전체 | 자신이 속한 GameObject |
| `DontDestroyOnLoad` | 적용한다 | 적용하지 않는다 |
| 부모 | 루트로 옮긴다 | 그대로 둔다 |
| 접근자 | `Instance` | `Current` |
| 인스턴스가 없을 때 | 오류를 한 번 출력하고 `null` | 경고를 한 번 출력하고 `null` |

전역 수명과 Unity 생명주기가 모두 필요한 Manager에만 사용한다. Provider, Mixer, Bridge 같은 일반 객체는 상속하지 않는다.

## 공통 규칙

- 인스턴스를 씬이나 프리팹에 직접 배치한다.
- 인스턴스를 자동으로 탐색하거나 생성하지 않는다.
- 중복 컴포넌트는 등록하지 않고 제거한다.
- 파생 클래스에서 `Awake`와 `OnDestroy`를 선언하지 않는다.
- 자체 초기화는 `OnRegistered`, 정리는 `OnUnregistering`에서 처리한다.
- 다른 서비스의 초기화 순서가 필요한 작업은 Bootstrap에서 처리한다. 구성은 `Bootstrap.md`를 따른다.
- 준비 과정이 필요한 Manager는 `Awake`가 아니라 `Initialize` 또는 `InitializeAsync`로 명시적으로 초기화한다.
- 정적 상태는 플레이 모드 진입마다 초기화되므로 에디터에서 도메인 리로드를 꺼도 이전 세션의 값이 남지 않는다.

`Awake`와 `OnDestroy`는 기반 클래스에서 `private`이다. 파생 클래스가 같은 이름을 선언하면 컴파일 오류나 경고 없이 기반 클래스의 것이 가려져 **등록 처리가 실행되지 않는다.** 언어가 막아 주지 않으므로 선언하지 않는 것을 규칙으로 지킨다. 증상은 컴포넌트를 배치했는데도 "No singleton instance is registered" 오류가 나는 것이다.

## Singleton

애플리케이션 생명주기 동안 하나만 유지된다. 등록된 오브젝트는 `DontDestroyOnLoad`로 유지되며, 부모가 있으면 루트로 옮긴다.

```csharp
public sealed class AudioManager : Singleton<AudioManager>
{
    protected override void OnRegistered()
    {
        LoadSettings();
    }
}
```

```csharp
AudioManager.Instance.Play();

if (AudioManager.TryGetInstance(out var audioManager))
    audioManager.Play();
```

`Instance`는 등록된 인스턴스가 없으면 오류를 출력하고 `null`을 반환한다. 전역 Manager가 없다는 것은 배치 실수이기 때문이다. 인스턴스 존재 여부만 확인할 때는 `HasInstance` 또는 `TryGetInstance`를 사용한다.

## DomainSingleton

씬이나 특정 컨텍스트 안에서만 하나로 유지된다. 자신이 속한 GameObject가 파괴되면 함께 사라지고 `Current`는 `null`이 된다. 인게임과 아웃게임처럼 도메인을 분리할 때 사용한다.

```csharp
public sealed class PlacementMode : DomainSingleton<PlacementMode>
{
    protected override void OnRegistered()
    {
        BuildPreview();
    }
}
```

`Current`는 등록된 인스턴스가 없으면 경고를 출력하고 `null`을 반환한다. 도메인 밖에 있는 것은 설정 실수가 아니므로 `Singleton`의 오류보다 한 단계 낮은 경고를 사용한다.

경고는 타입당 한 번만 출력하고, 새 인스턴스가 등록되면 다시 출력할 수 있는 상태로 돌아간다. 매 프레임 확인하는 코드에서 로그가 쌓이지 않게 하기 위한 것이다.

부재가 예상되는 경로에서는 `HasCurrent`나 `TryGetCurrent`를 사용한다. 둘 다 로그를 출력하지 않는다.

```csharp
if (PlacementMode.TryGetCurrent(out var placement))
    placement.Cancel();
```
