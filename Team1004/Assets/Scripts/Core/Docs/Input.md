# Input

`InputManager`는 액션 이름으로 입력을 조회하고 구독한다. 어떤 키, 버튼, 디바이스가 그 액션에 연결되는지는 알지 않는다.

## 구성

- `Core.Input`: 액션 에셋 등록, 맵 활성화, 조회, 리스너

Input System 패키지에 직접 의존한다. 입력에는 교체 대상이 없으므로 Provider 인터페이스를 두지 않는다.

`Core.Gestures`와의 경계는 다음과 같다. 두 모듈은 서로를 참조하지 않는다.

| 모듈 | 담당 | 소비자가 보는 것 |
| --- | --- | --- |
| `Core.Input` | 액션 이름으로 조회·구독 | 키보드, 패드, 제스처를 구분하지 않는 단일 API |
| `Core.Gestures` | 포인터(터치·마우스) 궤적을 제스처로 판정하고 `<Gesture>` 디바이스로 Input System에 공급 | 궤적·손가락 수가 필요할 때만 이벤트 직접 구독 |

포인터 누름·뗌과 위치 같은 원시 값은 액션 에셋에서 `<Pointer>`, `<Touchscreen>`, `<Mouse>`에 직접 묶어 `Core.Input`으로 읽는다. 탭·스와이프·드래그 같은 판정 결과는 `<Gesture>`에 묶는다.

`KeyCode`와 디바이스 타입은 공개 API에 나타나지 않는다. 키보드 전용 프로젝트, 터치 전용 프로젝트, XR 프로젝트가 같은 `InputManager`를 사용하기 위한 조건이다.

## 액션 에셋

프로젝트마다 `InputActionAsset`을 하나 만들고 Core에 주입한다. Core는 에셋을 포함하지 않는다.

```text
Player/Move      Value    Vector2
Player/Look      Value    Vector2
Player/Interact  Button
Player/Jump      Button
UI/Cancel        Button
```

## 초기화

`InputManager`를 Bootstrap Scene에 직접 배치하고 Bootstrap 스크립트에서 초기화한다. 구성은 `Bootstrap.md`를 따른다.

```csharp
InputManager.Instance.Initialize(inputActions);
InputManager.Instance.EnableMap("Player");
```

리스너를 `OnEnable`에서 등록하는 소비자는 Bootstrap 씬 뒤에 로드되는 씬에 둔다. 같은 씬에 있으면 `OnEnable`이 `Start`보다 먼저 실행되어 `AddListener`가 초기화 전 예외를 던진다.

맵은 자동으로 활성화되지 않는다. `EnableMap`을 호출하기 전에는 모든 조회가 기본값을 반환하고 리스너가 호출되지 않는다.

```csharp
InputManager.Instance.DisableMap("Player");
InputManager.Instance.EnableMap("UI");
```

## 이름 규칙

액션은 `맵/액션` 전체 경로로 지정한다. 액션 이름이 에셋 전체에서 유일하면 짧은 이름으로도 지정할 수 있다.

```csharp
InputManager.Instance.IsPressed("Player/Interact");
InputManager.Instance.IsPressed("Interact");
```

같은 이름의 액션이 여러 맵에 있으면 짧은 이름은 등록되지 않는다. 이때 짧은 이름으로 조회하면 전체 경로를 사용하라는 경고가 출력된다.

## 조회

```csharp
if (InputManager.Instance.WasPressedThisFrame("Interact"))
    Interact();

if (InputManager.Instance.IsPressed("Player/Attack"))
    Attack();

Vector2 move = InputManager.Instance.ReadValue<Vector2>("Move");

if (InputManager.Instance.TryReadValue("Look", out Vector2 look))
    Rotate(look);
```

`ReadValue<T>`의 `T`는 액션에 설정된 값 타입과 일치해야 한다. 일치하지 않으면 예외가 발생한다.

없는 액션 이름으로 조회하면 이름당 한 번만 경고를 출력하고 기본값을 반환한다. 매 프레임 호출되는 API이므로 예외를 던지지 않는다.

맵을 활성화하지 않은 채로 조회해도 같은 방식으로 경고한다. 액션 자체는 존재하므로 값만 보면 눌리지 않은 것과 구별되지 않기 때문이다. 경고 기록은 `EnableMap`을 호출할 때 초기화된다.

```text
[InputManager] Input action is not enabled: 'Interact'. Call EnableMap("Player") before reading it.
```

## 리스너

```csharp
private void OnEnable()
{
    InputManager.Instance.AddListener("Jump", InputPhase.Started, OnJump);
}

private void OnDisable()
{
    if (InputManager.TryGetInstance(out var input))
        input.RemoveListener("Jump", InputPhase.Started, OnJump);
}
```

| 단계 | 시점 |
| --- | --- |
| `Started` | 입력이 시작된 순간 |
| `Performed` | 액션 조건이 충족된 순간 |
| `Canceled` | 입력이 끝나거나 취소된 순간 |

콜백은 인자를 받지 않는다. 값이 필요하면 콜백 안에서 `ReadValue<T>`로 읽는다.

등록은 설정 오류를 즉시 드러내야 하므로 없는 액션 이름이면 예외를 던진다. 해제는 이미 해제된 상태에서 다시 호출해도 아무 일도 하지 않는다.

## 제스처 바인딩

`GestureManager`가 초기화되면 `<Gesture>` 레이아웃의 가상 디바이스가 Input System에 등록된다. 액션 에셋에서 키보드와 제스처를 같은 액션에 나란히 묶으면 게임 코드는 입력원을 구분하지 않는다.

```text
Player/Jump  Button  <Keyboard>/space
                     <Gesture>/swipeUp
```

```csharp
InputManager.Instance.AddListener("Jump", InputPhase.Performed, OnJump);
```

컨트롤 목록은 `Gestures.md`의 "액션 바인딩"을 따른다. 제스처는 `GestureManager`의 `Update`에서 판정되어 다음 Input System 갱신에 액션으로 반영되므로 이벤트 직접 구독보다 한 프레임 늦다.

`<Gesture>`에 묶인 바인딩이 있는 맵을 `EnableMap`할 때 디바이스가 없으면 맵 이름을 담아 경고를 한 번 남긴다. Bootstrap에서 `GestureManager.Initialize`가 `EnableMap`보다 앞에 와야 한다.

## 제한

매 프레임 호출되는 `Held` 단계는 제공하지 않는다. Input System은 눌려 있는 동안의 프레임 콜백을 제공하지 않으며, 이를 흉내 내려면 모든 액션을 매 프레임 순회해야 한다. 눌려 있는 동안의 처리는 호출자의 `Update`에서 `IsPressed`로 처리한다.

```csharp
private void Update()
{
    if (InputManager.Instance.IsPressed("Sprint"))
        Sprint();
}
```

터치 제스처와 XR 입력은 포함하지 않는다. 각각 별도 모듈로 추가한다.
