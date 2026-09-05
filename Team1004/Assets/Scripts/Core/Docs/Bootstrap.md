# Bootstrap

Bootstrap은 Core가 제공하는 타입이 아니라 프로젝트가 작성하는 시작 씬과 스크립트다. Core의 Manager는 자동으로 생성되거나 초기화되지 않으므로, 배치와 초기화 순서를 아는 곳이 하나 필요하다.

Playground의 `Assets/Bootstrap`이 참조 구현이다. `Playground > Regenerate Bootstrap Assets` 메뉴가 씬과 에셋을 다시 만들고, `Assets/Tests/PlayMode/BootstrapTests.cs`가 초기화부터 첫 씬 진입까지를 검증한다.

## 구성

1. 첫 번째로 로드되는 Bootstrap 씬을 만들고 Build Profile의 맨 앞에 둔다.
2. 사용하는 Manager를 이 씬에 직접 배치한다. `Singleton<T>`는 등록 시 `DontDestroyOnLoad`로 유지된다. `AudioListener`는 `AudioManager` 오브젝트에 하나만 두고 게임 씬의 카메라에는 두지 않는다. 씬 전환 중 리스너가 0개나 2개가 되는 프레임을 막는다.
3. `SceneController`에 `SceneSettings`와 전환 컴포넌트를 연결한다. 전환 컴포넌트는 `SceneController`의 자식에 둔다.
4. Bootstrap 스크립트 하나가 아래 순서로 초기화하고 첫 게임 씬으로 이동한다.

게임 오브젝트와 소비자 스크립트는 Bootstrap 씬에 두지 않는다. 같은 씬에 있으면 소비자의 `OnEnable`이 Bootstrap의 `Start`보다 먼저 실행되어 초기화 전 API를 호출하게 된다.

씬마다 다른 준비는 Bootstrap이 아니라 그 씬의 `SceneReference`에 쓴다. 자세한 내용은 `Scene.md`를 따른다.

## 초기화 순서

Playground의 `Bootstrap.cs`에서 검증 필드를 뺀 것이다.

```csharp
public sealed class Bootstrap : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private TextAsset audioManifest;
    [SerializeField] private SceneReference firstScene;

    private async void Start()
    {
        ResourceManager.Instance.Initialize(new ResourcesResourceProvider());
        PoolManager.Instance.Initialize(new PoolSettings());
        InputManager.Instance.Initialize(inputActions);
        GestureManager.Instance.Initialize(new GestureSettings());

        var manifest = JsonConvert.DeserializeObject<AudioManifest>(audioManifest.text);
        await AudioManager.Instance.InitializeAsync(
            new ResourcesAudioClipProvider(), manifest.Sounds, manifest.Settings);

        new AudioSceneBridge(AudioManager.Instance, SceneController.Instance).Attach();
        new PoolSceneBridge(PoolManager.Instance, SceneController.Instance).Attach();

        var progress = new SaveService<PlaygroundSaveData>(
            new FileSaveStorage(),
            SaveKey.Slotted("Progress", 0),
            currentVersion: 1);
        await progress.LoadAsync();

        InputManager.Instance.EnableMap("Player");
        await SceneController.Instance.LoadAsync(ResolveFirstScene());
    }

    private SceneReference ResolveFirstScene()
    {
        if (EditorPlayback.TryGetReturnScenePath(out var returnScenePath) &&
            SceneController.Instance.TryGetScene(returnScenePath, out var returnScene))
            return returnScene;

        return firstScene;
    }
}
```

순서의 근거는 다음과 같다.

| 단계 | 이유 |
| --- | --- |
| `ResourceManager` 먼저 | 씬 참조의 `PrepareAsync`가 라벨 로드와 풀 등록을 요구한다 |
| `PoolManager.Initialize`는 첫 `Register` 앞 | 풀이 하나라도 등록된 뒤에는 초기화가 예외를 던진다 |
| Bridge 연결은 첫 `LoadAsync` 전 | 첫 씬 전환부터 씬 사운드 정지와 풀 회수가 동작해야 한다 |
| `SaveService.LoadAsync`는 한 번 | `Data`는 로드 전에 접근하면 예외를 던진다 |
| `EnableMap`은 마지막 | 초기화가 끝나기 전에는 입력을 받지 않는다 |
| `GestureManager.Initialize`는 `EnableMap` 앞 | `<Gesture>`에 묶인 액션은 제스처 디바이스가 있어야 반응한다. 없으면 `EnableMap`이 경고한다 |

쓰지 않는 Manager는 배치와 초기화를 생략한다. 사용하는 Manager 사이에 위 표에 없는 의존은 없다.

## 재진입

Manager의 `Initialize`와 `InitializeAsync`는 한 번만 호출할 수 있다. 두 번째 호출은 `InvalidOperationException`을 던진다. Manager는 `Singleton<T>`라 `DontDestroyOnLoad`로 살아남으므로, Bootstrap 씬을 다시 로드하면 이미 초기화된 Manager에 `Initialize`가 다시 들어간다.

Bootstrap 씬은 애플리케이션당 한 번만 로드한다. 타이틀로 돌아가기, 게임 재시작 같은 흐름은 Bootstrap 씬이 아니라 게임 씬을 `SceneController.LoadAsync`로 다시 로드해서 만든다.

Bootstrap 씬을 다시 로드해야 한다면 초기화를 소비자가 가드한다. Manager마다 초기화 여부를 읽는 API가 있다.

| Manager | 초기화 | 조회 |
| --- | --- | --- |
| `ResourceManager` | `Initialize(provider)` | `IsInitialized` |
| `PoolManager` | `Initialize(settings)` | `IsInitialized` |
| `InputManager` | `Initialize(asset)` | `IsInitialized` |
| `GestureManager` | `Initialize(settings)` | `IsInitialized` |
| `AudioManager` | `InitializeAsync(provider, sounds, settings)` | `IsInitialized` |
| `SceneController` | 없음 | `Settings`가 연결되어 있으면 사용 가능 |
| `SaveService<T>` | Manager가 아니라 일반 객체 | 매번 새로 만든다 |

```csharp
if (!ResourceManager.Instance.IsInitialized)
    ResourceManager.Instance.Initialize(new ResourcesResourceProvider());
```

`IsInitialized`는 Manager가 파괴될 때 함께 내려간다. `Managers.Destroy` 같은 방식으로 Manager 오브젝트를 지우고 새로 배치하면 초기화도 다시 해야 한다.

## 에디터에서 원하는 씬으로 플레이

`Core > Play From Bootstrap`은 기본으로 켜져 있다. 어떤 씬이 열려 있든 Bootstrap 씬에서 시작하고, `ResolveFirstScene`이 열려 있던 씬의 참조를 찾아 그리로 돌아간다. 전역 초기화와 씬별 준비가 빌드와 같은 순서로 실행된다. 열려 있던 씬이 `SceneSettings`에 등록되지 않았으면 첫 씬으로 간다.

Bootstrap 씬 경로는 `SceneSettings` 에셋의 `Bootstrap Scene`에 둔다. 저장소에 커밋되므로 새 머신에서 클론해도 같은 씬에서 시작한다. Playground에서는 `Playground > Regenerate Bootstrap Assets`가 이 필드까지 채운다. Bootstrap 씬은 `Scenes` 목록에 등록하지 않는다. 등록하면 `ResolveFirstScene`이 Bootstrap 씬으로 되돌아가 초기화가 두 번 실행된다.

## 제한

Core는 Bootstrap 구현을 포함하지 않는다. Provider, 매니페스트, 저장 데이터 타입이 프로젝트마다 다르기 때문이다. Playground의 구현을 복사해 프로젝트에 맞게 줄인다.
