# Scene

## 구성

- `Core.Scene`: 씬 정의, 로드, 설정, 이벤트
- `Core.Scene.Transition`: CanvasGroup 화면 전환
- `Core.Scene.Editor`: ScenePath 편집, 경로 동기화, Bootstrap 시작 플레이

기본 Scene 모듈은 구체적인 화면 전환 구현에 의존하지 않는다. `ISceneTransition` 구현체를 연결해서 연출을 교체한다.

## 데이터

| 타입 | 형태 | 역할 |
| --- | --- | --- |
| `ScenePath` | 직렬화 값 | `.unity` 에셋 하나를 경로와 GUID로 가리킨다 |
| `SceneReference` | ScriptableObject | 씬 하나의 참조. 씬 경로, 전환 값 재정의, 준비와 정리 코드 |
| `SceneSettings` | ScriptableObject | 전역 전환 값, Bootstrap 씬 경로, `SceneReference` 목록 |
| `SceneTransitionValues` | 직렬화 값 | 로딩 씬 사용 여부, 최소 로딩 시간, Cover와 Reveal 시간 |

씬은 문자열이 아니라 `SceneReference` 에셋으로 지정한다. `SceneController`의 모든 로드 API가 이 에셋을 받는다.

## 설정

1. 모든 씬을 활성 Build Profile의 Scene List에 추가한다.
2. `Create > Core > Scene Reference`로 씬마다 참조 에셋을 만들고 씬을 지정한다.
3. `Create > Core > Scene Settings`로 설정 에셋을 하나 만들고, 전역 전환 값을 정한 뒤 참조 에셋을 `Scenes` 목록에 등록한다. `Bootstrap Scene`에는 시작 씬을 지정한다. 같은 씬이나 같은 씬 이름을 두 번 등록하면, 또는 Bootstrap 씬을 `Scenes`에도 등록하면 오류가 표시되고 `SceneController` 등록 시 예외가 발생한다.
4. Bootstrap 씬의 영속 오브젝트에 `SceneController`를 추가하고, `CanvasGroupFadeTransition`은 같은 GameObject 또는 그 자식에 둔다. `DontDestroyOnLoad`는 `SceneController`의 계층에만 적용되므로 밖에 두면 첫 전환에서 파괴된다. `CanvasGroupFadeTransition`은 `Time.unscaledDeltaTime`으로 진행하므로 `Time.timeScale`이 0이어도 전환된다.
5. `SceneController`의 Settings와 Transition Source를 연결한다.

전환 값을 씬마다 다르게 하려면 참조 에셋의 `Override Transition`을 켜고 값을 채운다. 켜지 않으면 전역 값을 쓴다.

씬 파일을 옮기거나 이름을 바꾸면 `SceneSettings`와 `SceneReference` 에셋의 씬 경로는 자동으로 갱신된다. 프리팹이나 씬 오브젝트의 `ScenePath` 필드는 Inspector에서 한 번 열어야 갱신된다.

XR 프로젝트에서는 `CanvasGroupFadeTransition`의 Render Mode를 `Screen Space Camera`로 설정한다.

## 씬별 준비

씬이 들어가기 전에 갖춰야 할 것은 `SceneReference`를 상속한 프로젝트 클래스에 쓴다. 준비는 화면이 덮인 상태에서 씬이 로드되기 **전에** 실행되므로, 씬 오브젝트는 `Awake`에서부터 준비된 리소스와 풀을 쓸 수 있다.

```csharp
[CreateAssetMenu(menuName = "Game/Battle Scene")]
public sealed class BattleScene : SceneReference
{
    [SerializeField] private string enemyLabel;

    public override async Awaitable PrepareAsync(SceneTransitionContext context)
    {
        await ResourceManager.Instance.LoadLabelAsync(enemyLabel);
        PoolManager.Instance.RegisterLabel(enemyLabel, prewarmCount: 4);
    }

    public override Awaitable TeardownAsync(SceneTransitionContext context)
    {
        PoolManager.Instance.ClearLabel(enemyLabel);
        ResourceManager.Instance.ReleaseLabel(enemyLabel);
        return base.TeardownAsync(context);
    }
}
```

준비가 필요 없는 씬은 기본 `SceneReference` 에셋을 그대로 쓴다. `Core.Scene`은 기반 클래스만 알며 Resource, Pool, Audio를 참조하지 않는다.

`TeardownAsync`는 그 씬을 떠날 때 실행된다. Single 로드로 떠나면 현재 씬과 Additive로 올라와 있던 모든 씬의 정리가 순서대로 실행된다.

## 일반 씬 전환

```csharp
[SerializeField] private SceneReference targetScene;

public async Awaitable MoveAsync()
{
    await SceneController.Instance.LoadAsync(targetScene);
}
```

실행 순서는 다음과 같다.

```text
OnSceneLeaving
→ Cover
→ 현재 씬 TeardownAsync
→ 선택적 Loading Scene
→ 대상 씬 PrepareAsync
→ 대상 씬 로드 및 활성화
→ Reveal
→ OnSceneEntered
```

`CurrentScene`으로 활성 씬의 참조를 읽을 수 있다.

등록된 참조는 두 가지로 찾는다.

| API | 인자 | 쓰는 곳 |
| --- | --- | --- |
| `TryGetScene(scenePath, out scene)` | `Assets/Scenes/Main.unity` 같은 전체 경로 | `Scene.path`, `EditorPlayback.TryGetReturnScenePath`처럼 경로가 이미 있는 경우 |
| `TryGetSceneByName(sceneName, out scene)` | `Main` 같은 확장자 없는 파일 이름 | 저장 데이터, 설정 파일, 코드 상수처럼 사람이 적는 경우 |

이름 조회가 있으므로 소비 코드는 씬 경로 문자열을 복제하지 않는다. 이름은 `SceneSettings` 안에서 유일해야 하며, 같은 이름의 씬을 두 개 등록하면 `Validate`가 예외를 던진다. 같은 두 API가 `SceneController`와 `SceneSettings` 양쪽에 있고, `SceneController.Settings`로 설정 에셋 자체를 읽을 수도 있다.

## Additive 로드

기본적으로 화면 전환을 적용한다. `PrepareAsync`는 로드 전에, `TeardownAsync`는 언로드 전에 실행된다.

```csharp
Scene scene = await SceneController.Instance.LoadAdditiveAsync(targetScene);
await SceneController.Instance.UnloadAdditiveAsync(scene);
```

로드한 씬을 활성 씬으로 지정할 수 있다.

```csharp
var options = new AdditiveLoadOptions(makeActive: true);
Scene scene = await SceneController.Instance.LoadAdditiveAsync(targetScene, options);
```

호출자가 별도 화면 연출을 처리할 때만 자동 전환을 끈다.

```csharp
var loadOptions = new AdditiveLoadOptions(useTransition: false);
Scene scene = await SceneController.Instance.LoadAdditiveAsync(targetScene, loadOptions);

var unloadOptions = new AdditiveUnloadOptions(useTransition: false);
await SceneController.Instance.UnloadAdditiveAsync(scene, unloadOptions);
```

Additive 작업은 `OnSceneLeaving`과 `OnSceneEntered`를 호출하지 않는다.

## 이벤트

```csharp
public sealed class SceneListener : MonoBehaviour, ISceneTransitionListener
{
    private void OnEnable()
    {
        if (SceneController.TryGetInstance(out var controller))
            controller.RegisterListener(this);
    }

    private void OnDisable()
    {
        if (SceneController.TryGetInstance(out var controller))
            controller.UnregisterListener(this);
    }

    public void OnSceneLeaving(SceneTransitionContext context)
    {
    }

    public void OnSceneEntered(SceneTransitionContext context)
    {
    }
}
```

- `OnSceneLeaving`: Cover 전, 현재 씬 정리
- `OnSceneEntered`: 대상 씬 활성화와 Reveal 완료 후 처리

리스너는 모든 전환에 반응하는 전역 훅이다. 씬마다 다른 준비는 `SceneReference`에 쓴다.

## 상태

```csharp
SceneController.Instance.IsTransitioning
SceneController.Instance.LoadingProgress
SceneController.Instance.CurrentScene
```

씬 작업은 동시에 하나만 실행된다. 동일한 `Awaitable` 반환값은 한 번만 `await`한다.

씬 로드가 실패하거나 참조의 준비 코드가 예외를 던지면 화면을 열고 전환 상태를 정리한 뒤 예외를 호출자에게 그대로 던진다. 로드 실패는 설정 오류이므로 복구 대상이 아니다. 참조에 씬이 없거나 Build Profile에 없는 씬이면 `LoadAsync`가 즉시 예외를 던진다.

## 에디터에서 Bootstrap부터 시작

기본으로 켜져 있다. 어떤 씬이 열려 있든 플레이는 Bootstrap 씬에서 시작한다. 열려 있던 씬의 경로는 `EditorPlayback.TryGetReturnScenePath`로 읽을 수 있으며, Bootstrap 스크립트가 `TryGetScene`으로 그 씬의 참조를 찾아 첫 씬 대신 그리로 이동한다. 설정에 등록되지 않은 씬이면 첫 씬으로 간다. 빌드에서는 항상 `false`를 반환한다.

Bootstrap 씬 경로는 `SceneSettings` 에셋의 `Bootstrap Scene`에 있다. 저장소에 커밋되므로 새 머신에서 클론해도 그대로 동작한다. Inspector에서 직접 지정하거나 `Core > Play From Bootstrap > Use Current Scene As Bootstrap`으로 열려 있는 씬을 써 넣는다.

`PlayFromBootstrap`은 프로젝트 전체에서 `AssetDatabase.FindAssets("t:SceneSettings")`로 설정 에셋을 찾는다. 에셋이 둘 이상이면 경로 순으로 첫 번째를 쓰고 한 번 경고한다. 설정 에셋은 프로젝트에 하나만 둔다.

켜짐 여부(`Enabled`)만 `EditorPrefs`에 남는다. 사람마다 끄고 싶을 수 있고 저장소로 옮길 값이 아니기 때문이다.

명령줄에 `-runTests`가 있으면 `Enabled`와 무관하게 적용하지 않고 `playModeStartScene`을 비운다. 배치 테스트는 항상 테스트 러너가 고른 씬에서 시작하므로, 소비 프로젝트가 이 기능을 켠 채로 두어도 `Tools/run_tests.py`와 같은 배치 실행이 막히지 않는다.
