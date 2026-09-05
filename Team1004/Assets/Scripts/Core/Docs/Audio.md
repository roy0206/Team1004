# Audio

`AudioManager`는 문자열 ID로 사운드를 재생한다. 사운드 파일 로딩은 `IAudioClipProvider`가 담당한다.

## 구성

- `Core.Audio`: 데이터, Provider 계약, 재생, 채널 풀, 볼륨, Scene 이벤트 연동
- `Core.Audio.Resources`: Resources 로딩 구현

데이터 흐름은 다음과 같다.

```text
JSON
→ IAudioClipProvider
→ IReadOnlyDictionary<string, AudioClip>
→ AudioManager
```

AudioManager는 로딩 방식과 JSON 저장 위치를 알지 않는다. AudioClip 목록을 Inspector에 보관하지 않으며 `AudioReference`나 Catalog를 사용하지 않는다.

## JSON

```json
{
  "settings": {
    "initialPoolSize": 12,
    "maxSfxChannels": 32,
    "spatialBlend": 1.0,
    "minDistance": 1.0,
    "maxDistance": 15.0,
    "masterVolume": 1.0,
    "sfxVolume": 1.0,
    "bgmVolume": 1.0
  },
  "sounds": [
    { "id": "button_click", "loadKey": "Audio/UI/ButtonClick" },
    { "id": "battle_bgm", "loadKey": "Audio/BGM/Battle" }
  ]
}
```

`ResourcesAudioClipProvider`의 `loadKey`는 `Resources` 폴더 아래 경로이며 확장자를 제외한다.

매니페스트는 Newtonsoft로 파싱한다. `AudioManifest`, `AudioEntryData`, `AudioRuntimeSettings`는 PascalCase 프로퍼티(`Settings`, `Sounds`, `Id`, `LoadKey`, `InitialPoolSize` 등)를 가지며 `[JsonProperty]`로 위의 JSON 키에 대응되므로 JSON 파일은 바뀌지 않는다. 설정 객체가 `GestureSettings`와 같은 형태(프로퍼티 초기화자, ADR 0004)가 되어 코드에서 만들 때도 같은 규칙을 쓴다.

```csharp
new AudioRuntimeSettings { InitialPoolSize = 8, MaxSfxChannels = 24, SpatialBlend = 0f }
```

## 초기화

```csharp
var manifest = JsonConvert.DeserializeObject<AudioManifest>(jsonText);
var provider = new ResourcesAudioClipProvider();

var initialized = await AudioManager.Instance.InitializeAsync(
    provider,
    manifest.Sounds,
    manifest.Settings);
```

`AudioManager`는 시작 Scene 또는 Bootstrap Scene에 직접 배치한다. 초기화가 끝난 뒤 재생 API를 사용한다.

`AudioListener`는 `AudioManager`와 같은 오브젝트에 하나만 둔다. 게임 씬의 카메라마다 두면 씬 전환 중에 리스너가 0개 또는 2개인 프레임이 생겨 경고와 무음이 발생한다.

초기화는 한 번만 한다. 이미 초기화됐거나 진행 중이면 예외를 던진다. 항목 목록(빈 ID, 빈 loadKey, 중복 ID)과 설정값(채널 수, 거리, 0~1 범위의 볼륨)은 `AudioManager`가 Provider를 부르기 전에 검증하고 잘못되면 예외를 던진다. Provider는 로드만 담당하며, 항목마다 클립을 돌려주지 않으면 초기화가 실패한다.

## 재생

```csharp
AudioManager.Instance.PlayGlobal("button_click");
AudioManager.Instance.PlayAt("explosion", position);

var handle = AudioManager.Instance.PlayAttached("engine", target);
AudioManager.Instance.SetVolume(handle, 0.5f);
AudioManager.Instance.Stop(handle);

AudioManager.Instance.PlayBgm("battle_bgm");
AudioManager.Instance.StopBgm("battle_bgm");
```

반복 SFX는 `PlayLoopGlobal`, `PlayLoopAt`, `PlayLoopAttached`를 사용한다. `PlayGlobal`, `PlayAt`, `PlayAttached`의 `repeatCount`로 정해진 횟수만 반복할 수도 있다.

SFX는 `maxSfxChannels`를 넘지 않는 AudioSource 풀에서 재생한다. 한도에 도달하면 가장 오래된 비반복 SFX를 회수하며 반복 SFX는 자동 회수 대상에서 제외한다.

`AudioHandle`은 내부 재생 ID를 감싸며 특정 SFX의 정지와 볼륨 변경에 사용한다. `IsPlaying`은 남은 반복이 있으면 반복 사이에도 `true`다.

같은 ID의 BGM을 다시 호출하면 해당 레이어를 갱신한다. 서로 다른 ID의 BGM은 동시에 재생할 수 있다.

## 조회

```csharp
AudioManager.Instance.IsBgmPlaying("battle_bgm");
AudioManager.Instance.ActiveSfxCount;
AudioManager.Instance.LastSfxId;
```

`ActiveSfxCount`는 재생 중이거나 반복 대기 중인 SFX 채널 수다. `LastSfxId`는 마지막으로 재생을 시작한 SFX의 ID이며, 알 수 없는 ID나 채널 부족으로 재생되지 않은 호출은 반영하지 않는다. 소비 프로젝트의 소리 회귀 테스트는 내부 `AudioSource`를 뒤지지 않고 이 API로 검사한다.

## 볼륨

```csharp
AudioManager.Instance.MasterVolume = 0.8f;
AudioManager.Instance.SfxVolume = 0.7f;
AudioManager.Instance.BgmVolume = 0.5f;
AudioManager.Instance.SfxMuted = true;
```

최종 출력 볼륨은 `재생 볼륨 × Master × SFX/BGM`으로 계산된다.

Unity AudioMixer를 사용하지 않는다. 필터나 DSP가 필요해지면 별도 모듈로 확장한다.

## Scene 연동

```csharp
var bridge = new AudioSceneBridge(
    AudioManager.Instance,
    SceneController.Instance);

bridge.Attach();
```

Scene을 떠날 때 `sceneBound`가 `true`인 SFX만 정지한다. BGM은 유지된다. 연동을 끝낼 때 `bridge.Dispose()`를 호출한다.

Addressables Audio Provider는 포함하지 않는다. DataManager 설계 이후 별도 모듈로 추가한다.
