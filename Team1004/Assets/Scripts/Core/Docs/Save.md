# Save

`SaveService<TData>`는 저장 데이터 하나의 로드와 저장을 담당한다. 저장 위치는 `ISaveStorage`, 바이트 표현은 `ISaveCodec`, 스키마 변환은 `ISaveMigration`이 담당한다.

## 구성

- `Core.Save`: 계약, 봉투, 마이그레이션, 무결성, 복구
- `Core.Save.File`: 파일 저장소
- `Core.Save.PlayerPrefs`: PlayerPrefs 저장소

`Core.Save`는 Newtonsoft.Json에 의존한다. 저장 데이터의 스키마는 프로젝트가 정의하며 Core는 `TData`를 알지 않는다.

`TData`에는 `Vector3`, `Quaternion`, `Color` 같은 UnityEngine 구조체를 직접 두지 않는다. 자기 자신을 반환하는 프로퍼티(`normalized` 등) 때문에 Newtonsoft가 순환 참조로 실패해 모든 저장이 `Failed`가 된다. 기본 타입 필드로 풀어서 저장한다.

## 계층

```text
TData
  ↕ Newtonsoft 직렬화 · 마이그레이션(JObject)
JSON 문자열
  ↕ ISaveCodec
byte[]
  ↕ ISaveStorage
```

저장소 계약이 문자열이 아니라 `byte[]`인 것은 압축과 암호화를 나중에 얹을 수 있게 하기 위해서다. 문자열로 잡으면 기존 저장 데이터를 못 읽게 되므로 뒤늦게 바꿀 수 없는 축이다.

## 저장 형식

```json
{
  "version": 3,
  "savedAt": "2026-08-04T02:31:00+00:00",
  "data": { }
}
```

`version`과 `savedAt`은 Core가 소유하고 `data`는 프로젝트가 소유한다. 게임 데이터 모델에 버전 필드를 두지 않는다.

`version`이나 `data`가 없는 JSON은 문서 전체를 버전 0의 `data`로 취급한다. 봉투 없이 저장하던 기존 파일이 마이그레이션 파이프라인의 첫 단계로 들어온다.

## 슬롯과 스코프

저장 단위는 `SaveKey`로 지정한다.

```csharp
SaveKey.Shared("Settings")          // Settings
SaveKey.Slotted("Progress", 0)      // Progress.0
```

설정, 도전과제, 갤러리 언락처럼 슬롯이 늘어도 하나뿐인 데이터는 `Shared`, 슬롯마다 별개인 데이터는 `Slotted`를 쓴다. 슬롯이 필요 없는 게임도 진행도는 `Slotted(name, 0)`로 둔다. 나중에 슬롯이 늘어날 때 설정이 0번 슬롯에 갇히지 않는다.

`Shared`와 `Slotted`를 나누지 않고 전부 슬롯으로 만들면, 슬롯이 둘로 늘어나는 시점에 파일을 쪼개는 마이그레이션이 필요해진다.

이름에는 `.`을 쓸 수 없다. 슬롯 번호 구분자다.

## 사용

`SaveService`는 MonoBehaviour가 아니다. 전역 접근이 필요하면 프로젝트에서 Manager로 감싼다.

```csharp
public sealed class UserDataManager : Singleton<UserDataManager>
{
    private SaveService<UserData> progress;

    public UserData Data => progress.Data;
    public SaveLoadResult LastLoadResult => progress.LastLoadResult;

    protected override void OnRegistered()
    {
        progress = new SaveService<UserData>(
            new FileSaveStorage(),
            SaveKey.Slotted("Progress", 0),
            currentVersion: 1);
    }

    public Awaitable<SaveLoadResult> LoadAsync() => progress.LoadAsync();
    public Awaitable<SaveWriteResult> SaveAsync() => progress.SaveAsync();
}
```

`LoadAsync`는 Bootstrap에서 한 번 호출한다. `Data`는 로드 전에 접근하면 `InvalidOperationException`을 던진다. 로드 여부를 알 수 없는 곳에서는 `TryGetData(out var data)`를 쓴다. 로드 전에는 `false`와 `null`을 반환하고, 로드 뒤에는 `Data`와 같은 객체를 반환한다.

저장 시점은 Core가 정하지 않는다. 자동 저장, 종료 시 저장, 체크포인트 저장은 게임 흐름이므로 프로젝트가 `SaveAsync`를 부른다.

## 로드 결과

| 값 | 상황 | 데이터 |
| --- | --- | --- |
| `Created` | 저장 데이터가 없다 | 기본값 |
| `Loaded` | 정상 로드 | 저장된 값 |
| `Migrated` | 구버전을 변환해 로드 | 변환된 값 |
| `Recovered` | 주 데이터를 쓸 수 없어 백업에서 복구 | 백업 값 |
| `Unsupported` | 현재 버전보다 높은 버전에서 저장됨 | 기본값 |
| `Corrupted` | 해석 실패, 백업도 사용 불가 | 기본값 |
| `Unavailable` | 저장소가 주 데이터를 읽지 못함 | 기본값 |

`Recovered`, `Unsupported`, `Unavailable`은 사용자에게 알려야 하는 상태다. `Recovered`는 진행이 되돌아갔음을, `Unsupported`는 구버전 빌드로 실행 중임을, `Unavailable`은 이번 세션의 저장이 거부됨을 뜻한다.

`Unavailable`은 파일 잠금, 권한, 네트워크처럼 저장소 밖의 원인으로 읽지 못한 경우다. 내용이 손상된 것이 아니므로 백업 복구와 `.corrupt` 보존을 하지 않으며, `LoadAsync`가 성공하거나 `ResetAsync`를 호출할 때까지 `SaveAsync`는 `Failed`를 반환한다.

로드는 예외를 던지지 않는다. 저장 데이터가 어떤 상태든 게임은 시작된다.

## 저장 결과

| 값 | 의미 |
| --- | --- |
| `Success` | 저장됨 |
| `StorageFull` | 저장 공간 부족 |
| `AccessDenied` | 접근 거부 |
| `Failed` | 그 밖의 실패 |

콘솔 플랫폼은 저장 공간 부족과 접근 거부에 규정된 안내를 요구하므로 실패 이유를 구분해서 반환한다. 저장도 예외를 던지지 않는다.

`IsSaving`과 `SaveStarted` · `SaveCompleted` 이벤트로 저장 진행 상태를 알 수 있다. "저장 중 전원을 끄지 마십시오" 표시에 사용한다.

저장 중에 `SaveAsync`를 다시 호출하면 진행 중인 저장이 끝날 때까지 프레임 단위로 대기한 뒤 실행된다.

## 마이그레이션

역직렬화 이전의 `JObject` 단계에서 실행한다. 필드 이름 변경과 삭제를 다룰 수 있어야 하는데, 역직렬화 후에는 이전 구조가 이미 사라지기 때문이다.

```csharp
public sealed class AddLoadoutSlots : ISaveMigration
{
    public int TargetVersion => 2;

    public void Apply(JObject data)
    {
        data["loadout"] ??= new JArray();
    }
}
```

```csharp
new SaveService<UserData>(
    storage,
    SaveKey.Slotted("Progress", 0),
    currentVersion: 2,
    saveMigrations: new ISaveMigration[] { new AddLoadoutSlots() });
```

- `TargetVersion`은 변환을 마친 뒤의 버전이다. 1부터 시작하고 현재 버전을 넘을 수 없다.
- 적용 순서는 `TargetVersion` 오름차순이며 등록 순서와 무관하다.
- 변환기는 순수 함수여야 한다. 저장소에 접근하거나 다른 상태를 바꾸지 않는다. 같은 입력에 반복 적용해도 결과가 같아야 한다.
- 중복 `TargetVersion`, 범위를 벗어난 값, `null` 항목은 생성 시점에 예외로 걸러진다.

새로 추가한 필드는 모델의 기본값으로 채워지므로, 필드 추가만 하는 변경에는 변환기가 필요 없다. 현재 버전만 올리면 된다.

## 무결성과 복구

기본 코덱은 `ChecksumSaveCodec(new Utf8SaveCodec())`이다. 저장 시 본문 앞에 8바이트 FNV-1a 해시를 붙이고 로드 시 대조한다.

체크섬은 사고로 깨진 데이터를 탐지하기 위한 것이지 변조 방지가 아니다. 알고리즘이 공개되어 있으므로 값을 고친 뒤 해시를 다시 계산하면 통과한다. 치트 방지가 필요하면 서명 코덱을 만들어 `ISaveCodec`으로 주입한다.

복구는 두 단계로 동작한다.

1. 저장할 때 기존 정상 데이터를 `<키>.backup`으로 옮긴 뒤 새로 쓴다. 백업은 1세대이며 `keepBackupCopy: false`로 끌 수 있다. 원본이 복구·손상·상위 버전 판정을 받은 뒤에는 정상 저장이 한 번 성공할 때까지 백업을 만들지 않는다.
2. 로드에 실패하면 읽을 수 없던 데이터를 `<키>.corrupt`로 보존한 뒤 백업에서 복구를 시도한다.

`<키>.corrupt`는 손상된 데이터와 현재 버전보다 높은 버전의 데이터를 모두 보존한다. 덮어쓰기는 그대로 진행되지만 원본은 남는다.

파일 교체가 원자적이므로 저장 중 종료되어도 이전 파일이 깨지지 않는다. 백업은 그것과 다른 사고 — 저장은 성공했으나 내용이 잘못된 경우 — 를 위한 것이다.

## 저장소 구현 계약

`ISaveStorage`를 직접 구현할 때 지켜야 하는 것들이다.

- `ReadAsync`는 데이터가 없으면 `null`을 반환한다. 없는 것과 읽기 실패는 다르다.
- **`WriteAsync`가 완료되면 데이터가 영속되어 있어야 한다.** 버퍼에만 남아 있으면 안 된다. WebGL은 `FS.syncfs()`를 호출해야 IndexedDB로 넘어간다.
- 실패는 `SaveStorageException`으로 던지고 `SaveStorageError`로 원인을 분류한다. 저장 공간 부족과 접근 거부를 구분하지 않으면 플랫폼 요구 안내를 띄울 수 없다.
- `ListKeysAsync`는 저장된 모든 키를 반환한다. `.backup`과 `.corrupt`가 붙은 키도 포함되며 `SaveCatalog`가 걸러낸다.
- 백그라운드 스레드에서 작업했다면 반환 전에 메인 스레드로 돌아온다. 예외 경로도 마찬가지다.

## 슬롯 조회

```csharp
var catalog = new SaveCatalog(new FileSaveStorage());

IReadOnlyList<SaveSlotInfo> slots = await catalog.ListSlotsAsync("Progress");
var header = await catalog.ReadHeaderAsync<SaveHeader>(SaveKey.Slotted("Progress", 0));
await catalog.DeleteAsync(SaveKey.Slotted("Progress", 0));
```

`SaveSlotInfo`는 슬롯 번호, 버전, 저장 시각을 담는다. 슬롯 목록 화면에 표시할 요약은 `ReadHeaderAsync<THeader>`로 얻는다. `THeader`는 저장 데이터의 부분집합이면 되고, Core에 헤더 스키마를 등록할 필요가 없다.

`ListSlotsAsync`는 슬롯마다 데이터를 한 번씩 읽는다. 슬롯이 많고 데이터가 크면 비용이 든다.

`DeleteAsync`는 본체와 백업, 보존된 손상본을 함께 지운다.

## 저장소별 제한

`FileSaveStorage`

- 기본 경로는 `<persistentDataPath>/Saves`이며 확장자는 `.sav`다.
- 키는 파일 이름이므로 파일 이름에 쓸 수 없는 문자를 포함할 수 없다.
- 임시 파일에 쓰고 `Flush(true)` 후 교체한다.
- WebGL을 지원하지 않는다. 백그라운드 스레드를 쓰고 `FS.syncfs()`를 호출하지 않으므로 저장이 완료돼도 IndexedDB에 남지 않는다. WebGL에서는 `PlayerPrefsSaveStorage`나 프로젝트의 저장소 구현을 쓴다.

`PlayerPrefsSaveStorage`

- 바이트를 Base64 문자열로 저장한다. 원본보다 약 33% 커진다.
- **웹에서 PlayerPrefs 전체가 1MB로 제한된다.** 초과하면 `StorageFull`이 반환된다. 설정 저장에 적합하고 진행도 저장에는 적합하지 않다.
- PlayerPrefs에는 키 열거 API가 없어 별도 인덱스 키(`<접두사>.__index`)를 유지한다. 외부에서 PlayerPrefs를 직접 지우면 인덱스와 어긋난다. `__index`는 저장 키로 쓸 수 없다. 인덱스는 첫 사용 시 한 번 읽어 메모리에 두므로 다른 인스턴스가 같은 접두사로 쓰면 어긋난다.
- 모든 작업이 메인 스레드에서 즉시 끝난다.

## 플랫폼 제한

- WebGL의 `persistentDataPath`는 페이지 URL을 해시해 만든다. 빌드 URL이 바뀌면(itch.io 재업로드 등) 기존 저장 데이터에 접근할 수 없다.
- Safari의 추적 방지 기능은 iframe 안에서 IndexedDB를 예고 없이 비운다.
- 위 둘은 Core가 해결할 수 없다. 웹 배포 시 클라우드 저장소 구현을 함께 쓰는 편이 안전하다.

## 통합에서 제외한 기능

- 클라우드 동기화. `ISaveStorage` 구현으로 붙일 수 있으나 충돌 해결 정책은 게임마다 다르다. 봉투의 `savedAt`이 최신 우선 판정의 근거를 제공한다.
- 자동 저장 컴포넌트. 저장 시점은 게임 흐름의 문제다.
