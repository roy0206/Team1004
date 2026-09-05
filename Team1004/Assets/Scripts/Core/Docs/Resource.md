# Resource

`ResourceManager`는 원본 에셋을 라벨 단위로 로드하고 캐싱한다. 복제본은 `PoolManager` 등 사용하는 모듈에서 관리한다.

## 구성

- `Core.Resource`: Provider 계약, 원본 캐시, 라벨 수명 관리
- `Core.Resource.Resources`: Resources 디렉터리 로딩 구현

ResourceManager는 Prefab 복제, 풀링, JSON 변환을 담당하지 않는다. 구체적인 로딩과 실제 해제는 `IResourceProvider` 구현체가 담당한다.

## 초기화

`ResourceManager`를 시작 Scene 또는 Bootstrap Scene에 배치하고 Provider를 주입한다.

```csharp
ResourceManager.Instance.Initialize(
    new ResourcesResourceProvider());
```

## Resources Provider

라벨은 `Resources` 폴더 아래의 디렉터리 경로다.

```text
Assets/Resources/Enemies/Goblin.prefab
Assets/Resources/Enemies/Boss/Ogre.prefab
```

```csharp
await ResourceManager.Instance.LoadLabelAsync("Enemies");
```

`Enemies` 디렉터리와 모든 하위 디렉터리의 에셋을 로드한다. 경로에는 `Resources`와 확장자를 포함하지 않으며 `/`를 사용한다. 프리팹의 Component 하위 오브젝트는 제외한다.

`Resources.LoadAll`은 동기 API이므로 디렉터리 일괄 로드가 실행되는 프레임에는 작업이 멈출 수 있다.

## 조회

Resources Provider의 에셋 키는 에셋 이름이다. 이름이 같고 타입이 다른 에셋(Sprite와 그 Texture2D 등)은 나란히 보관하며 `Get<T>`와 `TryGet<T>`의 타입 인자로 고른다.

로드 대기 중에 `ReleaseAll`이 호출되거나 Manager가 파괴되면 그 로드의 결과는 등록하지 않고 Provider에 되돌린다. Provider가 `null`을 반환하면 실패시킨다.

같은 라벨을 다시 로드하면 추가 로드나 참조 횟수 증가 없이 완료된다. 같은 키와 같은 타입이 다른 에셋을 가리키거나 한 라벨에 두 번 나타나면 로드를 실패시킨다.

```csharp
var prefab = ResourceManager.Instance.Get<GameObject>("Goblin");

if (ResourceManager.Instance.TryGet("Ogre", out GameObject ogre))
{
    var instance = Object.Instantiate(ogre);
}

var keys = ResourceManager.Instance.GetKeys("Enemies");
var prefabs = ResourceManager.Instance.GetAll<GameObject>("Enemies");
```

## 해제

```csharp
ResourceManager.Instance.ReleaseLabel("Enemies");
ResourceManager.Instance.ReleaseAll();
```

같은 에셋이 여러 라벨에 포함되면 내부 참조 횟수로 관리하며 모든 라벨이 해제된 뒤 캐시에서 제거된다.

원본을 직접 `Instantiate`한 복제본은 Provider의 수명 관리에 포함되지 않는다. 복제본 사용이 끝난 뒤 라벨을 해제한다. Scene 종료로 복제본이 파괴되는 경우에는 Scene 종료 후 해제하고, 영속 Pool에 남아 있다면 Pool을 먼저 비운다.

Prefab과 Component는 `Resources.UnloadAsset`으로 개별 해제할 수 없으므로 캐시에서만 제거된다. 실제 메모리 회수는 Unity의 사용 여부 판단에 따른다.
