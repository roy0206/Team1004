# Gestures

`GestureManager`는 화면 포인터의 움직임을 탭, 드래그, 스와이프, 핀치, 회전으로 해석해 이벤트로 알린다.

## 구성

- `Core.Gestures`: 포인터 추적과 제스처 판정

`Core.Input`에 의존하지 않는다. 멀티터치는 InputAction으로 표현되지 않으므로 EnhancedTouch를 직접 읽는다. 판정 결과는 `<Gesture>` 가상 디바이스로 Input System에 공급하므로 액션 에셋에서 키보드와 나란히 묶을 수 있다.

| 모듈 | 담당 | 소비자가 보는 것 |
| --- | --- | --- |
| `Core.Gestures` | 포인터(터치·마우스) 궤적을 제스처로 판정하고 `<Gesture>` 디바이스로 Input System에 공급 | 궤적·손가락 수가 필요할 때만 이벤트 직접 구독 |
| `Core.Input` | 액션 이름으로 조회·구독 | 키보드, 패드, 제스처를 구분하지 않는 단일 API |

`MousePosition`이나 `ScrollDelta` 같은 디바이스 원시 값은 제공하지 않는다. 그런 값은 `Core.Input`에서 액션으로 읽는다.

## 포인터 모델

터치와 마우스를 같은 포인터로 다룬다.

| 입력 | 포인터 |
| --- | --- |
| 터치 | 손가락 하나가 포인터 하나 |
| `PointerButtons`에 포함된 마우스 버튼 | 눌려 있으면 포인터 하나 |
| 마우스 휠 | 두 포인터 핀치로 변환 |

터치가 하나라도 있으면 마우스는 무시한다. 에디터에서 터치 게임을 그대로 조작할 수 있다.

`PointerButtons`는 어느 마우스 버튼을 포인터로 볼지 정하는 `[Flags]`이며 기본값은 `Left`다. 우클릭 드래그 팬처럼 데스크톱 관용구가 필요하면 켠다. 여러 버튼을 켜면 그중 하나라도 눌려 있는 동안 포인터 하나로 다룬다. 버튼별로 서로 다른 제스처가 동시에 진행되지는 않는다.

```csharp
GestureManager.Instance.Initialize(new GestureSettings
{
    PointerButtons = GesturePointerButtons.Left | GesturePointerButtons.Right
});
```

`GesturePointerButtons.None`으로 두면 마우스는 포인터가 되지 않고 터치와 휠만 남는다.

손가락 수는 이벤트마다 `PointerCount`로 전달된다. 손가락 수별로 따로 구독하지 않고 콜백 안에서 값을 비교한다.

```csharp
void OnDragged(GestureContext context)
{
    if (context.PointerCount == 2)
        Pan(context.Delta);
}
```

제스처가 어디에서 왔는지는 `Source`와 `Buttons`로 구분한다. `Source`는 `Touch`나 `Mouse`이고, `Buttons`는 마우스일 때 그 순간 눌려 있던 버튼이며 터치에서는 `None`이다. 휠 핀치는 `Source`가 `Mouse`, `Buttons`가 `None`이다.

```csharp
void OnDragged(GestureContext context)
{
    if (context.Source == GestureSource.Mouse && context.Buttons == GesturePointerButtons.Right)
        Pan(context.Delta);
}
```


## 초기화

`GestureManager`를 시작 Scene 또는 Bootstrap Scene에 직접 배치한다.

```csharp
GestureManager.Instance.Initialize(new GestureSettings());
```

판정 기준값을 바꿀 때만 객체 초기화자로 지정한다.

```csharp
GestureManager.Instance.Initialize(new GestureSettings { DragThreshold = 20f });
```

| 설정 | 기본값 | 의미 |
| --- | --- | --- |
| `DragThreshold` | `10` | 드래그로 판정할 최소 이동 거리(px) |
| `TapMaxDuration` | `0.2` | 더블탭으로 이어질 수 있는 최대 누름 시간(초). 탭 자체는 이 값에 걸리지 않는다 |
| `LongPressDuration` | `0.5` | 롱프레스로 판정할 누름 시간(초) |
| `DoubleTapMaxInterval` | `0.3` | 두 탭 사이 최대 간격(초) |
| `DoubleTapMaxDistance` | `40` | 두 탭 사이 최대 거리(px) |
| `SwipeMinDistance` | `50` | 스와이프로 판정할 최소 이동 거리(px) |
| `SwipeMinSpeed` | `300` | 스와이프로 판정할 최소 속도(px/초) |
| `RotationThreshold` | `0.5` | 회전 이벤트를 낼 최소 누적 각도(도). 이보다 작은 변화는 누적했다가 넘는 순간 한 번에 보고한다 |
| `ScrollPinchStep` | `1.1` | 휠 한 칸의 핀치 배율. 한 프레임에 여러 칸이 들어오면 거듭제곱으로, 트랙패드의 소수 칸은 그 비율만큼 적용한다 |
| `ScrollPinchMaxNotches` | `3` | 한 프레임에 반영할 최대 휠 칸 수. 이 값을 넘는 스크롤은 잘라낸다 |
| `PointerButtons` | `Left` | 포인터로 볼 마우스 버튼(`Left`/`Right`/`Middle` 플래그) |
| `PublishDevice` | `true` | 판정 결과를 `<Gesture>` 가상 디바이스로 Input System에 공급한다. 끄면 이벤트 직접 구독만 가능하다 |

`LongPressDuration`은 `TapMaxDuration`보다 커야 한다. 값이 잘못되면 초기화가 예외를 던진다. 설정은 `Initialize` 시점에 읽으며 이후 객체를 변경하는 것은 지원하지 않는다.

### 휠 단위 전제

휠 핀치는 Input System의 스크롤 값이 `ScrollDeltaBehavior.UniformAcrossAllPlatforms`, 즉 한 노치가 `1.0`이라는 전제로 계산한다. Unity 6.3의 기본값이 이 모드다. `KeepPlatformSpecific`으로 바꾸면 Windows에서 한 노치가 `120`으로 들어와 `ScrollPinchStep`의 지수가 그대로 `120`이 된다.

`ScrollPinchMaxNotches`가 프레임당 노치 수를 잘라 그런 설정에서도 배율이 한 번에 튀지 않게 한다. 다만 그 상태에서는 매 프레임 최대치가 들어오므로, 프로젝트가 `KeepPlatformSpecific`을 쓴다면 `ScrollPinchStep`을 그 단위에 맞춰 다시 정한다.

### `PublishDevice` 비용

`PublishDevice`가 켜져 있으면 제스처마다 `InputSystem.QueueStateEvent`가 호출된다. 순간 제스처는 누름과 뗌으로 두 번, 드래그·핀치·회전은 값과 복귀로 두 번, 포인터 위치는 움직인 프레임마다 한 번이다. 드래그 중에는 프레임당 최소 세 번의 상태 이벤트가 큐에 들어간다.

액션 바인딩을 쓰지 않고 이벤트만 구독하는 프로젝트는 `PublishDevice = false`로 둔다. 기본값이 `true`인 것은 액션 바인딩이 권장 소비 방식이기 때문이다.

## 이벤트

```csharp
private void OnEnable()
{
    GestureManager.Instance.Tapped += OnTapped;
    GestureManager.Instance.Pinched += OnPinched;
}

private void OnDisable()
{
    if (GestureManager.TryGetInstance(out var gestures))
    {
        gestures.Tapped -= OnTapped;
        gestures.Pinched -= OnPinched;
    }
}
```

| 이벤트 | 시점 | 사용하는 값 |
| --- | --- | --- |
| `Tapped` | 드래그도 롱프레스도 아닌 상태로 뗀 순간 | `Position` |
| `DoubleTapped` | 두 번째 탭이 누름 시간, 간격, 거리 조건을 모두 만족한 순간 | `Position` |
| `LongPressed` | 드래그 없이 `LongPressDuration`을 넘긴 순간 | `Position` |
| `DragStarted` | 이동이 `DragThreshold`를 넘긴 순간 | `Position` |
| `Dragged` | `DragStarted` 직후부터 드래그 중 매 프레임 | `Position`, `Delta` |
| `DragEnded` | 드래그 상태로 뗀 순간 | `Position` |
| `Swiped` | 드래그가 거리와 속도 조건을 만족하며 끝난 순간 | `Delta`, `Speed` |
| `Pinched` | 두 포인터 사이 거리가 바뀐 순간 | `PinchScale` |
| `Rotated` | 두 포인터를 잇는 각도가 바뀐 순간 | `RotationDelta` |

`Delta`는 이번 프레임 이동량이다. 첫 `Dragged`는 `DragStarted`와 같은 프레임에 이어서 발생하며 그 `Delta`는 누른 지점부터의 전체 이동이므로, `Dragged`의 `Delta`를 모두 더하면 누른 지점부터의 경로가 된다. `Swiped`의 `Delta`는 시작점부터의 전체 이동 벡터다. `PinchScale`은 직전 프레임 대비 배율이며 `1`이 변화 없음이다. `RotationDelta`는 마지막 `Rotated` 이후 누적된 각도 변화이고 반시계 방향이 양수다.

이벤트는 서로를 대체하지 않고 겹쳐서 발생한다.

- 스와이프는 `DragEnded` 다음에 `Swiped`가 이어서 발생한다.
- 더블탭은 두 번의 `Tapped` 뒤에 `DoubleTapped`가 이어서 발생한다.
- 롱프레스가 발생한 누름은 뗄 때 `Tapped`를 발생시키지 않는다.
- 핀치와 회전은 드래그와 동시에 발생한다.

### 탭과 누름 시간

누름이 드래그로도 롱프레스로도 판정되지 않았다면 뗄 때 항상 `Tapped`가 발생한다. 누름 시간에는 상한이 없다. 데스크톱 클릭은 0.3초를 쉽게 넘기므로 상한을 두면 배치나 선택이 무반응이 된다.

`TapMaxDuration`은 그 탭이 더블탭으로 이어질 수 있는지만 정한다. 누름이 `TapMaxDuration`을 넘으면 `Tapped`는 그대로 발생하지만 더블탭 후보에서 빠지고, 직전에 쌓아 둔 탭도 함께 버린다. 짧은 두 번의 탭만 `DoubleTapped`가 된다.

## 조회

```csharp
GestureManager.Instance.IsDragging
GestureManager.Instance.PointerCount
GestureManager.Instance.PointerSource
GestureManager.Instance.PointerButtons
GestureManager.Instance.Position
GestureManager.Instance.DragOrigin
GestureManager.Instance.DragDelta
```

`DragDelta`는 드래그 시작점부터 현재까지의 누적 이동이며, 드래그 중이 아니면 `Vector2.zero`다.

## 액션 바인딩

`PublishDevice`가 켜져 있으면 `Initialize`가 `<Gesture>` 레이아웃의 가상 디바이스를 Input System에 추가하고, 매니저가 제거될 때 디바이스도 제거된다. 액션 에셋에서 다른 디바이스와 같은 방식으로 묶는다.

```text
Player/Jump  Button  <Keyboard>/space
                     <Gesture>/swipeUp
```

| 컨트롤 | 종류 | 값 |
| --- | --- | --- |
| `<Gesture>/tap` | Button | `Tapped` 순간 한 번 눌렀다 뗀다 |
| `<Gesture>/doubleTap` | Button | `DoubleTapped` 순간 한 번 눌렀다 뗀다 |
| `<Gesture>/longPress` | Button | `LongPressed` 순간 한 번 눌렀다 뗀다 |
| `<Gesture>/swipeUp` `swipeDown` `swipeLeft` `swipeRight` | Button | `Swiped` 순간 전체 이동 벡터의 큰 축 방향 하나가 한 번 눌렸다 뗀다 |
| `<Gesture>/dragging` | Button | `DragStarted`부터 `DragEnded`까지 눌린 상태 |
| `<Gesture>/drag` | Vector2 | `Dragged`의 `Delta`(px). 첫 값은 누른 지점부터의 전체 이동이다. 이벤트 직후 0으로 돌아간다 |
| `<Gesture>/pinch` | Axis | `Pinched`의 `PinchScale - 1`. 이벤트 직후 0으로 돌아간다 |
| `<Gesture>/rotate` | Axis | `Rotated`의 `RotationDelta`(도). 이벤트 직후 0으로 돌아간다 |
| `<Gesture>/position` | Vector2 | 누르고 있는 동안의 포인터 무게중심(px) |

순간 제스처는 누름과 뗌 두 상태 이벤트를 연달아 큐에 넣는다. Button 액션은 같은 Input System 갱신 안에서 `performed` 뒤 `canceled`가 이어지고, `WasPressedThisFrame`은 참이지만 `IsPressed`는 거짓이다.

제스처는 `GestureManager`의 `Update`에서 판정되고 상태 이벤트는 다음 Input System 갱신에서 처리된다. 액션은 이벤트 직접 구독보다 한 프레임 늦다. 마우스는 포인터 하나로 계속 읽으므로 에디터에서 마우스로 그린 스와이프도 `<Gesture>/swipeUp`에 도달한다.

`<Gesture>`에 묶인 맵을 `InputManager.EnableMap`할 때 디바이스가 없으면 경고가 난다. `GestureManager.Initialize`를 먼저 호출한다.

## 손가락 수 변화

누르고 있는 동안 손가락 수가 바뀌어도 드래그는 끊기지 않는다. 무게중심이 튀는 것을 막기 위해 기준점만 갱신하고, 드래그 중이면 `DragOrigin`은 유지한다.

탭과 롱프레스의 `PointerCount`는 누르고 있던 동안의 최대 손가락 수로 보고한다. 두 손가락을 한 프레임 차이로 떼도 두 손가락 탭으로 판정된다.

## 제한

포인터 위치는 스크린 좌표다. 월드 좌표 변환과 UI 히트 테스트는 호출자가 처리한다.

핀치와 회전은 처음 두 포인터만 사용한다. 세 개 이상이어도 계산 대상은 두 개다.
