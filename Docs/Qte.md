# QTE (Game.Qte)

순간 입력 이벤트(Quick Time Event) 공용 부품이다. 「정해진 키를 정해진 횟수만큼, 제한 시간 안에 눌러라」만 담당하고 그 결과로 무엇이 일어나는지는 모른다. 지금 쓰는 곳은 상류 단차 하나뿐이고(`Docs/Ledge.md`), 보스·컷신·마지막 폭포가 나중에 같은 부품을 쓸 수 있게 `Game.Ledge` 밖으로 뺐다.

`Game.Qte`는 `Game.Ledge`·`Game.Play`를 참조하지 않는다. 참조 방향은 `Game.Play` → `Game.Ledge` → `Game.Qte` 한 방향이다.

## 구성

| asmdef | 경로 | 타입 | 책임 |
| --- | --- | --- | --- |
| `Game.Qte` | `Assets/Scripts/Qte/` | `QteSequence` | 순수 C#. 기대 키 교대, 진행 수, 수락/거절, 남은 시간, 완료/실패. **테스트 대상** |
| | | `QteKey` | `None` / `Up` / `Down` |
| | | `QteState` | `Idle` / `Running` / `Completed` / `Failed` |
| | | `QteData : ScriptableObject` | 누를 횟수·첫 키·번쩍임 길이·액션 이름. `Create > Team1004 > Qte Data` |
| | | `QteModule : Module` | `Ticks = Update`. `InputManager` 액션 두 개를 `QteSequence`에 잇는다. MonoThing 호스트에 붙인다 |
| | | `QtePanel : MonoBehaviour` | HUD 뷰. 화살표·점·시간 막대만 그린다. 판정하지 않는다 |
| `Game.Qte.Tests` | `Assets/Scripts/Qte/Tests/` | `QteSequenceTests` | EditMode 17개 |

asmdef 참조: `Game.Qte` → `Core.Foundation`, `Core.Modules`, `Core.Input`, `UnityEngine.UI`. 순환 없음.

## `QteSequence`

```csharp
sequence.Begin(presses: 6, firstKeyUp: true, budgetSeconds: 5.2f);
sequence.Press(QteKey.Up);      // true = 수락, false = 거절
sequence.Tick(Time.deltaTime);  // 남은 시간을 스스로 줄인다
sequence.SetRemaining(2.4f);    // 바깥이 남은 시간을 직접 준다(단차가 쓰는 쪽)
sequence.Fail();
sequence.Cancel();
```

| 멤버 | 뜻 |
| --- | --- |
| `State` | `Idle` → `Begin` → `Running` → `Completed` / `Failed` |
| `Expected` | 다음에 눌러야 할 키. 완료·실패하면 `None` |
| `Progress` / `RequiredPresses` / `Progress01` | 지금까지 맞은 수 / 필요한 수 / 비율 |
| `Remaining` / `TimeBudget` / `Remaining01` | 남은 시간(초) / 시작 때 받은 예산 / 비율. 예산이 0 이하면 **시간 제한 없음**이고 `Remaining01`은 항상 1 |
| `Accepted(int progress)` | 맞는 키를 눌렀다. 인자는 누적 진행 수 |
| `Rejected(QteKey key)` | 틀린 키(같은 키 연타 포함). **벌점은 없다.** 진행도 시간도 변하지 않는다 |
| `ExpectedChanged(QteKey)` | 기대 키가 바뀌었다. `Begin` 직후와 완료 시(`None`)에도 온다 |
| `Completed` / `Failed` | 완료 / 실패 |

규칙은 두 개뿐이다.

- **교대**: `Begin`의 `firstKeyUp`이 첫 기대 키를 정하고, 수락될 때마다 `Up`↔`Down`이 뒤집힌다.
- **틀리면 아무 일도 없다**: 기대 키가 아니면 `Rejected`만 나가고 진행·시간은 그대로다. 연타로 뚫는 것을 막되 벌하지는 않는다(기획 지시).

`Press`는 `Running`이 아니면 항상 `false`다. 완료 뒤에 남은 입력이 들어와도 안전하다.

## `QteModule`

MonoThing 호스트에 `AddModule(new QteModule())`로 붙인다. 액션 이름의 기본값은 `Player/LaneUp`·`Player/LaneDown`(`QteModule.DefaultUpAction`/`DefaultDownAction`)이고 `QteData`나 `SetActions`로 바꾼다.

```csharp
qte = AddModule(new QteModule());
qte.AutoTick = false;                  // 시간을 바깥이 준다
qte.Completed += OnCompleted;
qte.Begin(qteData, budgetSeconds);     // 여기서 InputManager 리스너를 건다
...
qte.SetRemaining(seconds);             // 매 프레임
qte.Stop();                            // 리스너 해제 + 시퀀스 취소
```

- **리스너는 `Begin`에서 걸고 `Stop`·완료·실패에서 뗀다.** `OnEnable`에 걸지 않으므로 `InputManager` 초기화 순서에 의존하지 않는다(`Assets/Scripts/Core/Docs/Input.md`).
- `AutoTick`(기본 `true`)이면 `OnUpdate`가 `Time.deltaTime`으로 예산을 깎는다. 남은 시간이 월드 상태에서 나오는 경우(단차)에는 끄고 `SetRemaining`을 쓴다.
- 모듈이 호스트에서 떨어지면(`OnDetached`) 리스너를 뗀다. 씬 이탈에 새는 구독이 없다.

## `QtePanel`

HUD Canvas(Screen Space Camera, 1920×1080) 아래의 뷰다. **상태를 갖지 않고** 호출된 대로만 그린다.

| 호출 | 하는 일 |
| --- | --- |
| `Show(requiredPresses, expected)` | `content`를 켜고 점 개수를 맞추고 화살표·막대를 초기화 |
| `Hide()` | `content`를 끈다 |
| `SetExpected(QteKey)` | 화살표 글자를 `↑`/`↓`로 |
| `SetProgress(int)` | 채워진 점 개수 |
| `SetRemaining01(float)` | 시간 막대(`fillAmount`). `dangerThreshold01`(0.3) 아래면 색이 `barDangerColor`로 바뀐다 |
| `Punch()` | 화살표를 `flashDuration` 동안 `punchScale`까지 키우고 색을 번쩍인다. `Time.unscaledDeltaTime`으로 감쇠한다 |

`QtePanel` 컴포넌트는 **항상 켜져 있는** 루트에 붙고 껐다 켜는 것은 자식 `Content`다. 그래야 `Awake`에서 정적 `Current` 등록이 되고, 프리팹(`LedgeSet`)에서 씬 오브젝트를 직접 참조하지 못하는 문제를 `QtePanel.TryGetCurrent`로 우회할 수 있다. 씬 배치·색·크기는 `Docs/CoreLoop.md` 「HUD」를 본다.

색·번쩍임 길이·점 색은 전부 `[SerializeField]`다. 상수로 박지 않았다.

## 다른 이벤트에 재사용하기

1. 호스트 MonoThing에서 `EnsureQte()` 꼴로 `QteModule`을 한 번만 붙인다.
2. `Assets/GameAssets/Design/Qte/`에 `QteData`를 하나 더 만들고(횟수·첫 키가 다르면) 이벤트 데이터에서 참조한다.
3. 시작 조건이 오면 `qte.Begin(data, budget)` + `QtePanel.Show`, 플레이어 입력을 뺏는다(`LanePlayer.QteCaptureInput = true`).
4. `Completed`에 성공 연출을, 실패는 이벤트 쪽 시계가 판정하거나(단차) `Failed`를 그대로 쓴다.
5. 끝나면 반드시 `qte.Stop()` + `QtePanel.Hide()` + `QteCaptureInput = false`.

`QteCaptureInput`은 `LanePlayer`의 스위치다. 켜져 있으면 `LanePlayer`가 `Player/LaneUp`·`LaneDown`을 무시하므로(레인 이동·점프 모두) 같은 키를 QTE가 독점한다. 리스너를 떼는 것이 아니라 무시만 하므로 켜고 끄는 비용이 없다. `InputEnabled`와는 별개다(`InputEnabled`는 배너·상태 전이가 쓴다).

## 확신 없는 지점

- **연타 방지 규칙이 「벌점 없음」이다.** 틀린 키를 아무리 눌러도 손해가 없으니 ↑↓를 빠르게 번갈아 누르는 것 자체가 요구 조건이다. 실기에서 너무 쉬우면 `requiredPresses`를 올리거나 「틀리면 진행 −1」을 `QteSequence`에 옵션으로 넣는다.
- **입력 원은 키보드뿐이다.** 액션 에셋(`GameInput.inputactions`)의 `Player/LaneUp`·`LaneDown`에 패드·제스처를 묶으면 QTE도 자동으로 따라간다. 코드는 손대지 않는다.
- **`QtePanel.Current`는 하나만 가정한다.** 화면에 QTE 패널이 둘 이상 있으면 먼저 `Awake`한 것이 이긴다. 지금은 Play 씬에 하나뿐이다.
- **사운드가 없다.** 수락·거절·완료에 붙일 id가 `AudioManifest.json`에 없다. 필요하면 기획에 요청한다.
