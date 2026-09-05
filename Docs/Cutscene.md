# 컷신

`Game.Cutscene`은 플레이 씬 안에서 배우 이동·표정(스프라이트 교체)·카메라·대사를 DOTween으로 재생한다. 상호작용은 Space로 넘기기뿐이다. Timeline·Animator·Playable·Cinemachine은 쓰지 않는다. 기획서 정본은 `Team1004/Assets/Documents/정리 1번/3번 정리.md`(컷신은 보스 직전, 재도전 시 스킵, 대사 5문장 이내·임시)이고 2판 5·19·21·23·27절이 연출 원문이다.

**컷신 하나는 클래스 하나다.** 스텝 목록 ScriptableObject(`CutsceneAsset`)는 없앴다. 규모가 작아 데이터로 빼는 이득보다 코드로 읽는 이득이 커서 사용자가 하드코딩을 결정했다. 공통 기능만 `CutsceneBase`에 둔다. 대사 문장만은 코드 밖 CSV에 있고 컷신은 id로만 참조한다(코드에 대사 문자열 리터럴 0개).

- 경로: `Assets/Scripts/Cutscene/`, 컷신 본체: `Assets/Scripts/Cutscene/Scenes/`, 에디터: `Editor/`, 테스트: `Tests/`
- asmdef: `Game.Cutscene`(참조 `Core.Foundation`, `Core.Modules`, `Core.Input`, `Game.Dialogue`, `UnityEngine.UI`), `Game.Cutscene.Editor`, `Game.Cutscene.Tests`
- 네임스페이스: `Game.Cutscene`, `Game.Cutscene.Scenes`, `Game.Cutscene.Editor`, `Game.Cutscene.Tests`
- `Game.Play`·`Game.Player`를 참조하지 않는다. 플레이 쪽이 컷신을 참조한다(순환 방지).

## 파일

| 파일 | 타입 | 역할 |
| --- | --- | --- |
| `CutsceneBase.cs` | 추상 일반 C# 클래스 | 컷신 하나의 베이스. `Id`, `LineIds`, `Run()`, 헬퍼, 스킵, 카메라 복원 |
| `CutsceneContext.cs` | 일반 C# 클래스 | 재생기가 주입하는 무대 참조. 카메라·배우 사전·대사 뷰·링크 오브젝트 |
| `CutsceneCatalog.cs` | static | id → 팩토리. `PlayFlow`와 CSV 검증이 여기로만 컷신을 만든다 |
| `CutsceneStageLayout.cs` | static | 배우·카메라 좌표 중 여러 컷신이 공유하는 임시값 |
| `CutsceneActorIds.cs` | static | 배우 id 상수 `player`, `boss`, `landmark` |
| `CutsceneActor.cs` | `MonoThing` 호스트 | 씬 배우. `Awake`에서 `CutsceneActorViewModule`을 붙인다 |
| `CutsceneActorViewModule.cs` | `Module` (`Ticks = None`) | 표시/숨김, 스프라이트 교체, 알파 |
| `CutscenePlayer.cs` | `MonoThing` 호스트 | 플레이 씬 재생기. 카메라·배우 목록·대사 뷰·입력 이름을 SerializeField로 갖는다 |
| `CutscenePlaybackModule.cs` | `Module` (`Ticks = None`) | 컨텍스트 생성, `PlayAsync`, 입력 맵 전환, 스킵 위임 |
| `CutsceneDialogueView.cs` | `MonoBehaviour` (Canvas UI) | 하단 대사 패널, 타자 효과, "Space ▶" 힌트, 전체 화면 페이드 이미지 |
| `Scenes/IntroCutscene.cs` 외 4개 | `CutsceneBase` 파생 | 컷신 5개의 연출 그 자체 |
| `Editor/CutsceneSetup.cs` | 에디터 | 메뉴 `Team1004/Generate Cutscene Dialogue Prefab`, `Regenerate …(Overwrite)` |
| `Tests/CutsceneCatalogTests.cs`, `Tests/CutsceneSkipTests.cs` | EditMode 테스트 | 카탈로그, 스킵·취소·재사용 |

## 팀 규칙 적용

- 씬 오브젝트는 `MonoThing` 호스트 + `Module`. `CutscenePlayer`와 `CutsceneActor`가 호스트다. 컷신 본체(`CutsceneBase` 파생)는 **씬 오브젝트가 아니라 일반 C# 객체**라 `MonoBehaviour`도 `Module`도 아니다. 재생할 때마다 `new`로 만든다(런타임 GameObject 생성 금지 규칙은 일반 객체에 해당하지 않는다).
- `CutsceneDialogueView`는 Canvas UI라 `MonoBehaviour` 그대로다.
- 모듈 틱은 쓰지 않는다(`ModuleTick.None`). 시간 진행은 전부 DOTween이 맡고, 완료는 `AwaitableCompletionSource`로 기다린다. `Coroutine`·`Task`는 없다.
- 런타임 `Instantiate`·`new GameObject`·씬 탐색 없음. 배우·대사 프리팹 인스턴스는 씬에 미리 둔다. 예외: `CutscenePlayer.stageCamera`가 비어 있으면 `Awake`에서 `Camera.main`을 쓰고 경고를 남긴다.

## `CutsceneBase`

```csharp
public abstract class CutsceneBase
{
    public abstract string Id { get; }
    public virtual IReadOnlyList<string> LineIds { get; }

    public bool IsRunning { get; }
    public bool IsSkipping { get; }
    public bool IsCancelled { get; }

    public void Begin(CutsceneContext context);
    public Awaitable ExecuteAsync();
    public void Advance();
    public void Skip();
    public void Cancel();

    protected virtual float DefaultCharsPerSecond => 30f;
    protected virtual bool RestoreCameraOnFinish => true;

    protected abstract Awaitable Run();
}
```

| 멤버 | 동작 |
| --- | --- |
| `Id` | 스킵 기록(`PlayFlow.SeenCutscenes`)과 카탈로그 키. 대사 id의 앞자리와 같게 둔다 |
| `LineIds` | 이 컷신이 쓰는 대사 id 전부. `Validate Dialogue CSV`가 이것만 본다. **`Say`를 추가하면 여기에도 넣는다** |
| `Begin` | 재생기가 무대 참조를 주입한다. 카메라 시작값을 기록하고 상태를 `Running`으로 만든다. 이미 실행 중이면 `InvalidOperationException` |
| `ExecuteAsync` | `Run()`을 돌리고 끝나면 정리한다. `Begin` 전에 부르면 `InvalidOperationException`(동기 예외) |
| `Advance` | Space와 같다. 타자 중이면 전부 표시, 아니면 지금 대기 중인 헬퍼를 전부 끝값으로 완료하고 다음으로 |
| `Skip` | 아래 「스킵」 |
| `Cancel` | 씬을 떠날 때. 트윈을 끝값 없이 죽이고, 이후 헬퍼는 **아무것도 하지 않고** 반환한다. 파괴된 오브젝트를 만지지 않기 위함이다 |
| `RestoreCameraOnFinish` | 끝날 때 카메라 위치·orthoSize를 `Begin` 시점으로 되돌린다. 기본 `true`. `Cancel`이면 복원하지 않는다 |

`Run()`이 반환하면 대사 패널을 즉시 숨기고, 카메라를 복원하고, 상태를 비운다. 같은 인스턴스를 다시 `Begin`할 수 있다.

## 헬퍼

전부 `Awaitable`을 반환한다. `await` 없이 값만 받아 `Together`에 넘길 수 있다(호출 즉시 트윈이 시작되고, 반환된 `Awaitable`이 완료를 나타낸다).

| 헬퍼 | 하는 일 | 스킵 시 |
| --- | --- | --- |
| `Say(string lineId)` | CSV에서 화자·본문·자동진행·cps를 가져와 패널에 타자로 출력. `auto_advance`가 비면 Space 대기(힌트 표시), 값이 있으면 그 초 뒤 자동 진행 | 즉시 패널을 닫고 반환 |
| `Move(actor, target, duration = 0, ease = InOutSine, relative = false)` | `transform.DOMove`. `duration` 0이면 즉시 이동 | 목표 위치를 즉시 적용 |
| `Show(actor)` / `Hide(actor)` | 표시/숨김 | 그대로 적용 |
| `SetSprite(actor, sprite)` | 스프라이트 교체(표정 자리) | 그대로 적용 |
| `FadeActor(actor, alpha, duration = 0, ease = Linear)` | `SpriteRenderer.color` 알파 | 목표 알파 |
| `Animate(actor, clip, duration = 0)` | 배우의 `SpriteAnimatorModule`(`CutsceneActor.Animator`, `SpriteRenderer`가 있을 때 지연 생성)로 `PlayAsync(clip, duration)`. 원샷은 마지막 프레임까지, 루프는 한 바퀴 뒤 반환하고 계속 돈다 | 원샷은 마지막 프레임을 즉시 표시, 루프는 그대로 재생 시작. `Cancel`이면 `Stop()`만 |
| `SetPose(actor, clip, frameIndex)` | `SetFrame(clip, index)`. 정지 포즈 | 그대로 적용 |
| `CameraTo(position, orthoSize = 0, duration = 0, ease = InOutSine, relative = false)` | 카메라 `DOMove`(z 유지) + `orthoSize > 0`이면 `DOOrthoSize` | 목표 위치·크기 |
| `Shake(duration, strength)` | `Camera.DOShakePosition(duration, strength, 10, 90, fadeOut: true)` | 아무것도 하지 않는다(흔들림은 원위치로 끝난다) |
| `FadeScreen(alpha, duration = 0, ease = Linear)` | 대사 프리팹의 전체 화면 검은 이미지 알파 | 목표 알파. 컷신이 끝나도 남으므로 되돌리려면 마지막에 `FadeScreen(0f)` |
| `Wait(seconds)` | 대기 | 아무것도 하지 않는다 |
| `Together(params Awaitable[])` | 이미 시작된 헬퍼들을 전부 기다린다. 걸리는 시간은 가장 긴 것 하나 | 전부 즉시 완료 |
| `Play(Sequence)` | 저수준. 직접 조립한 `Sequence`를 링크·완료 연결하고 기다린다 | 시퀀스를 `Complete(true)`로 끝낸다 |

컨텍스트 접근자: `Player`, `Boss`, `Landmark`, `Actor(string id)`, `Clip(string id)`, `PlayerJumpClip`, `PlayerSwimClip`, `PlayerLaneUpClip`, `PlayerLaneDownClip`, `StageCamera`, `Dialogue`, `Context`. 배우가 없으면 id당 한 번 경고하고 `null`을 돌려주며, 헬퍼는 `null` 배우를 조용히 건너뛴다. 클립은 `CutscenePlayer.clips`(`CutsceneClipEntry[]`: id + `CustomAnimation`)로 주입되고 id 상수는 `CutsceneClipIds`(`player_jump`, `player_swim`, `player_lane_up`, `player_lane_down`)다. 코어루프 생성기가 `Design/Animations/Player_Jump`·`Player_Swim`·`Player_LaneUp`·`Player_LaneDown` 넷을 연결한다. `Game.Cutscene` → `Game.Animation` 참조.

`Animate`는 대기 항목(`Pending`)에 `Release` 콜백을 달아 두어 `Advance`/`Skip`이 트윈과 같은 방식으로 정리한다(스킵이면 끝 포즈 적용, 취소면 `Stop`). 플레이어 오브젝트의 `LanePlayer` 애니메이터는 컷신 중(`InputEnabled=false`) 틱을 멈추므로 두 애니메이터가 같은 `SpriteRenderer`를 두고 싸우지 않는다. `SetSprite`와 `Animate`를 같은 배우에 섞지 않는다(다음 프레임 전환 때 덮인다).

`Play`에 넘긴 시퀀스에는 `SetLink`, `OnComplete`, `OnKill`이 덧씌워진다. 시퀀스 자체의 `OnComplete`가 필요하면 마지막 트윈에 걸어라.

## 새 컷신 추가

1. `Assets/Scripts/Cutscene/Scenes/<이름>Cutscene.cs`를 만들고 `CutsceneBase`를 상속한다.
2. `Id`를 `CutsceneCatalog`의 상수로, `LineIds`를 그 컷신이 쓰는 대사 id 배열로 둔다. 대사 id 앞자리는 `Id`와 같게 한다(`Validate`가 규약을 검사한다).
3. 좌표·시간은 클래스 `private const`/`static readonly`로 둔다. 여러 컷신이 공유하는 값만 `CutsceneStageLayout`에 있다.
4. `Run()`에 연출을 순서대로 쓴다. 동시 연출은 `Together`.
5. `CutsceneCatalog`에 `{ 상수, () => new <이름>Cutscene() }`를 등록한다.
6. 대사를 `Assets/GameAssets/Design/Dialogue/dialogue.csv`에 넣고 `Team1004/Validate Dialogue CSV`를 돌린다.
7. 구간 컷신이면 `CutsceneCatalog.SectionIds`(와 `PlayFlow.sectionCutsceneIds`)에 넣는다.

```csharp
protected override async Awaitable Run()
{
    await Move(Boss, BossStart);
    await Show(Boss);

    await Together(
        Move(Boss, BossEnd, DescendDuration, Ease.OutQuad),
        Shake(ShakeDuration, ShakeStrength));

    await Say("cutscene1.01");
    await Move(Player, StepBack, StepBackDuration, Ease.OutQuad, true);
    await Say("cutscene1.02");
}
```

## 스킵

`Skip()`은 진행 중인 트윈을 `Complete(true)`로 끝값에 두고, 대사를 즉시 닫고, 대기 중인 `Awaitable`을 전부 완료시킨다. **그 뒤 `Run()`은 멈추지 않고 끝까지 흐르며, 모든 헬퍼가 최종 상태를 즉시 적용하고 반환한다.** 코드가 그대로 실행되므로 별도의 "최종 상태 표"를 유지할 필요가 없다. 이것이 예전 `ApplyFinalState(CutsceneAsset)`를 대체한다.

- 재도전으로 이미 본 컷신을 건너뛸 때: `CutscenePlayer.ApplyFinalState(cutscene)` → `Begin` → `Skip` → `ExecuteAsync`. 입력 맵과 `Started`/`Finished`는 건드리지 않는다. 스킵된 실행은 중간에 한 번도 멈추지 않으므로 동기적으로 끝난다(EditMode 테스트가 이것을 검사한다).
- 재생 중 스킵: `CutscenePlayer.Skip()`. 종료 처리(대사 숨김·카메라 복원·입력 복구·`Finished`)는 평소와 같다.
- `Shake`와 `Wait`는 스킵 시 아무 흔적도 남기지 않는다. `FadeScreen`은 남는다.

## 대사 id

대사 문장은 `Game.Dialogue`가 관리하는 CSV(`Assets/GameAssets/Design/Dialogue/dialogue.csv`)에 있다. 기획서 3판 20절이 문장을 다음 기획 단계에서 확정한다고 했으므로 기획이 코드 없이 고칠 수 있게 분리했다. 자세한 것은 `Docs/Dialogue.md`.

- `Say(lineId)`는 `DialogueService.Get(lineId)`로 화자·본문·자동 진행·타자 속도를 가져온다. 코드에는 id만 있다.
- `cps`는 CSV 값이 `0`보다 크면 CSV, 아니면 `DefaultCharsPerSecond`(기본 30). `auto_advance`는 비면 `-1`(Space 대기), 값이 있으면 그 초.
- id를 찾지 못하면 id당 한 번 경고하고 본문이 `[intro.01]`처럼 보이는 대체 줄을 쓴다. 재생은 멈추지 않는다.
- 문장만 고칠 때는 CSV만 고치면 된다. 컴파일도 생성기도 필요 없다.
- 끊어진 참조와 안 쓰는 id는 메뉴 `Team1004/Validate Dialogue CSV`로 확인한다. 이 메뉴는 `CutsceneCatalog`의 컷신을 전부 만들어 `LineIds`를 모은다.
- `DialogueService.Load`는 `GameBootstrap`이 부른다.

## 배우 id 규약

| id | 용도 | 씬 오브젝트 |
| --- | --- | --- |
| `player` | 연이 | `Player` 오브젝트에 `CutsceneActor` 추가(`LanePlayer`와 같은 오브젝트). 컷신이 위치를 옮기므로 종료 후 `LanePlayer.SnapToLane(player.CurrentLane)`으로 되돌린다(`PlayFlow`가 한다) |
| `boss` | 포식자·수문·폭포 등장 연출 | 컷신 전용 스프라이트 오브젝트. 시작 시 숨김, 컷신이 `Show`로 켠다. 실제 보스 게임플레이 오브젝트와 별개 |
| `landmark` | 자갈밭 등 배경 소품 | 컷신 전용 스프라이트 오브젝트. 시작 시 숨김 |

id는 `CutsceneActorIds` 상수로 참조한다. 새 배우가 필요하면 상수를 추가하고 `CutscenePlayer.actors`에 넣고 `CutsceneContext`에 접근자를 더한다.

## 재생기

```csharp
public sealed class CutscenePlayer : MonoThing
{
    public bool IsPlaying { get; }
    public CutsceneBase Current { get; }
    public Camera StageCamera { get; }
    public CutsceneDialogueView DialogueView { get; }
    public IReadOnlyList<CutsceneActor> Actors { get; }
    public event Action Started;
    public event Action Finished;

    public Awaitable PlayAsync(CutsceneBase cutscene);
    public Awaitable PlayAsync(string cutsceneId);
    public void Advance();
    public void Skip();
    public void ApplyFinalState(CutsceneBase cutscene);
    public void ApplyFinalState(string cutsceneId);
    public bool TryGetActor(string id, out CutsceneActor actor);
}
```

`PlayAsync`는 입력 맵을 전환하고 `Started`를 낸 뒤 `Begin` → `ExecuteAsync`를 돌리고, 끝나면 입력을 복구하고 `Finished`를 낸다. 재생 중 다시 부르면 `InvalidOperationException`, `cutscene`이 `null`이면 `ArgumentNullException`. 씬을 떠나 모듈이 떨어지면 `OnDetached`가 `Cancel()`을 불러 대기 중인 `Awaitable`을 완료시키므로 `PlayAsync`가 반환된다.

## 입력

Space는 Input System 액션 `Cutscene/Advance`로 읽는다(`GameInput.inputactions`, 코어루프 소유. 이미 들어 있다).

1. `PlayAsync` 시작 시 `mapsToDisable`(기본 `["Player"]`) 중 켜져 있는 맵을 끄고 기억한다.
2. `Cutscene` 맵이 꺼져 있으면 켜고, `Cutscene/Advance` `Performed` 리스너를 등록한다.
3. 종료·스킵 시 리스너 해제, 이 모듈이 켠 `Cutscene` 맵을 끄고, 1에서 끈 맵을 다시 켠다.
4. `InputManager`가 없거나 맵·액션이 없으면 경고 한 번 남기고 입력 없이 재생한다(`Advance()`는 코드로 호출 가능).
5. `Time.timeScale <= 0`(일시정지)이면 Space를 무시한다. DOTween은 기본 scaled time이므로 일시정지 중 트윈과 타자도 멈춘다.

## 대사 UI 규칙

`CutsceneDialogue.prefab`은 자체 Canvas를 가진다.

- `Canvas`: Screen Space Camera, `worldCamera`는 씬 메인 카메라, `planeDistance` 10, `sortingOrder` 100(HUD 위).
- `CanvasScaler`: Scale With Screen Size, 기준 해상도 1920x1080, Match 0.5.
- 프리팹은 씬 카메라를 참조할 수 없으므로 `CutscenePlayer.Awake`가 `dialogueView.SetWorldCamera(stageCamera)`로 연결한다.
- 폰트는 `Assets/GameAssets/Placeholder/Fonts/MonaS12.ttf`. 없으면 내장 `LegacyRuntime.ttf`로 대체하고 한글이 안 보일 수 있다고 경고한다.
- 구조: `CutsceneDialogue`(Canvas, `CutsceneDialogueView`) → `ScreenFade`(검은 Image, 알파 0, 전체) / `Panel`(CanvasGroup 알파 0, 반투명 검은 배경, 하단 250px) → `Speaker`, `Body`, `Hint`("Space ▶", 기본 비활성).

## 생성기

`Team1004/Generate Cutscene Dialogue Prefab`은 `Assets/GameAssets/UI/CutsceneDialogue.prefab`이 없을 때만 만든다. `Regenerate …(Overwrite)`는 확인 창 뒤 덮어쓴다. 컷신 자체는 코드라 생성할 것이 없다. 코어루프 생성기(`CoreLoopSetup`)가 프리팹이 없으면 이것을 먼저 부른다.

## 컷신 5개

좌표는 카메라 ortho size 3.6, 16:9(가로 ±6.4), 플레이어 X −4.2, 중단 레인 Y 0 기준의 임시값이며 `GameConfig`와 연동되지 않는다.

| 클래스 | `Id` | 대사 | 연출 |
| --- | --- | --- | --- |
| `IntroCutscene` | `intro` | `intro.01`~`06` | 암전에서 시작 → 어린 시절 자갈밭(landmark) 노출 → 페이드 인 → 2줄 → 암전 → 성체가 화면 왼쪽 밖에서 등장, 페이드 인과 걸어 들어오기 동시 → 3줄, 중간에 카메라 우측 상대 이동과 대사 동시 → 작은 흔들림 → 마지막 줄. 약 18초 |
| `FishingLineCutscene` | `cutscene1` | `cutscene1.01`~`04` | boss를 화면 위(y 6)에 두고 표시 → (1.5, 1)로 하강 + 작은 흔들림 동시 → 대사, 연이가 살짝 물러났다 돌아옴(복귀는 대사와 동시). 약 12초. 보스 1 낚싯줄 직전 |
| `WalrusCutscene` | `cutscene2` | `cutscene2.01`~`04` | boss를 오른쪽 화면 밖에 두고 표시 → (3.2, 0)으로 접근 + 큰 흔들림 동시 → 컷신 1과 같은 물러남/복귀 리듬. 약 13초. 보스 2 바다코끼리 직전 |
| `WaterfallCutscene` | `cutscene3` | `cutscene3.01`~`03` | 자갈(landmark) 발견 → 1줄 → 폭포(boss) 오른쪽 등장 + 1초 흔들림 동시 → 2줄. 약 12초. 보스 3 폭포 직전 |
| `EndingCutscene` | `ending` | `ending.01`~`04` | 자갈밭 도착(이동과 카메라 축소 동시) → 도착 점프 한 번(`Animate(Player, PlayerJumpClip)`, 0.8초) → 반투명 암전 위 과거 대사 → 암전 해제 후 2줄 → 완전 암전 뒤 마지막 줄. 끝에 화면이 검게 남는다. 약 15초 |

## 테스트

`Assets/Scripts/Cutscene/Tests/`, EditMode(`Game.Cutscene.Tests`).

| 파일 | 다루는 것 |
| --- | --- |
| `CutsceneCatalogTests.cs` | 5개 등록, 구간 id 순서, `Create`가 매번 새 인스턴스, 없는 id 거부, `LineIds` 비지 않음·중복 없음·`Id` 접두사 규약·구간 컷신 5문장 이내 |
| `CutsceneSkipTests.cs` | 스킵 예약 후 실행이 **동기로** 끝나고 모든 최종 상태가 남는지, `Cancel`이 무대를 건드리지 않는지, `Begin` 두 번·`Begin` 없는 `ExecuteAsync` 거부, 스킵한 인스턴스 재사용 |

PlayMode 스모크(`Play/Tests/CoreLoopSmokeTests`)는 부트스트랩→플레이 진입 시 인트로 컷신이 실제로 돌고 `Skip()` 뒤 `PlayFlow.SeenCutscenes`에 `intro`가 남는지까지 본다.

## 플레이 연결

`PlayFlow`는 컷신 에셋을 참조하지 않고 **id 문자열**만 가진다.

```csharp
[SerializeField] private string introCutsceneId = CutsceneCatalog.Intro;
[SerializeField] private string[] sectionCutsceneIds = { … };
[SerializeField] private string endingCutsceneId = CutsceneCatalog.Ending;
```

`PlayCutsceneAsync(id)`가 `CutsceneCatalog.TryCreate`로 인스턴스를 만들고, 이미 본 컷신(`SeenCutscenes`)이면 `ApplyFinalState(instance)`로 최종 상태만 남긴다. 자세한 흐름은 `Docs/CoreLoop.md` 「컷신 연결」.

## DOTween 참조 방식

코어 `DOTween.dll` API만 쓴다. `Plugins/Demigiant` 안의 `Modules/*.cs`는 asmdef가 없어 `Assembly-CSharp-firstpass`로 컴파일되므로 asmdef 기반 `Game.Cutscene`이 참조할 수 없다(`DOFade` 같은 확장 금지). `DOTween.dll`은 Auto Reference DLL이라 asmdef에 적지 않아도 참조된다. 쓰는 API: `Transform.DOMove`, `Camera.DOOrthoSize`, `Camera.DOShakePosition`, `DOTween.Sequence/To/ToAlpha`, `Sequence.Append/Join/AppendInterval`, `SetEase/SetLink/OnComplete/OnKill`, `Complete(bool)/Kill(bool)/IsActive`. 테스트 asmdef는 `overrideReferences`라 `DOTween.dll`을 `precompiledReferences`에 적었다.

## 없어진 것

| 삭제 | 대체 |
| --- | --- |
| `CutsceneAsset.cs` (`ScriptableObject`) | `CutsceneBase` 파생 클래스 |
| `CutsceneStep.cs`, `CutsceneStepType.cs` | 베이스 헬퍼 메서드 |
| `Editor/CutsceneStepDrawer.cs` | 없음(Inspector에서 편집하지 않는다) |
| `Assets/GameAssets/Design/Cutscenes/*.asset` 5개 | `Scenes/*.cs` 5개 |
| `CutsceneSetup`의 에셋 빌더 5개 | 없음. 대사 프리팹 생성만 남았다 |
| `CutscenePlayer.ApplyFinalState(CutsceneAsset)` | `ApplyFinalState(CutsceneBase)` / `ApplyFinalState(string)` |
| `CutsceneStep.RunWithNext` 그룹 | `Together(...)` |

## 임시값

| 항목 | 값 | 비고 |
| --- | --- | --- |
| 타자 속도 | 30자/초 | `CutsceneBase.DefaultCharsPerSecond`. CSV `cps`가 이긴다 |
| 자동 진행 대기 | 1.0~1.2초 | CSV `auto_advance` |
| 카메라 흔들림 | 0.06~0.12 unit, 0.3~1초, vibrato 10, randomness 90 | 세기·길이는 컷신 클래스 상수, 나머지는 `CutsceneBase` |
| 패널 페이드 | 0.15초 | `CutsceneDialogueView.panelFadeDuration` |
| 무대 좌표 | 플레이어 X −4.2, 레인 Y 0, 화면 밖 좌 −7.5 / 우 8.5 / 위 6 | `CutsceneStageLayout` |
| 대사 패널 | 하단 250px, 좌우 여백 60px, 폰트 32/32/24 | 1920x1080 기준 |
| Canvas sortingOrder | 100 | HUD 위 |

## 검증

에디터가 닫힌 상태에서 배치로 돌렸다.

| 실행 | 결과 |
| --- | --- |
| `-batchmode … -quit` 컴파일 | `error CS` 0, `warning CS` 0. `Game.Cutscene`, `Game.Cutscene.Editor`, `Game.Cutscene.Tests` dll 생성 |
| `-executeMethod Game.Bootstrap.Editor.CoreLoopSetup.Regenerate` | 오류·예외 0. `Play.unity`에 `introCutsceneId: intro`, `sectionCutsceneIds: cutscene1/2/3`, `endingCutsceneId: ending` 기록 확인 |
| `-executeMethod Game.Dialogue.Editor.DialogueCsvTools.Validate` | 21줄, 파싱 경고 0, 끊어진 참조 0, 안 쓰는 id 0. 새 `LineIds` 수집이 CSV와 정확히 일치 |
| `-runTests -testPlatform EditMode` | `Game.Cutscene.Tests` 10/10 통과. 프로젝트 전체는 129개 중 128개 통과이고 남은 1개는 컷신과 무관한 `Game.Environment.Tests.TileStripTests.StripAlwaysCoversTheSameSpan`(작업 중 다른 사람이 새로 넣은 어셈블리, 손대지 않았다) |
| `-runTests -testPlatform PlayMode` | 1/1 통과(스모크). 인트로 컷신 재생 → `Skip()` → `Running` |

## 확신이 없는 지점

- 스킵하지 않은 **정상 재생**을 사람 눈으로 본 적이 없다. 배치는 스킵 경로만 지난다. 타자 속도·대기·흔들림의 체감과 `Together`로 겹친 연출의 타이밍은 플레이로 확인해야 한다.
- `Sequence.Complete(true)`가 자식 트윈을 전부 끝값에 두는지는 DOTween 문서에 의존한다. 그래서 `Advance`·`Skip`은 `Complete` 뒤 `TrySetResult`를 직접 부른다.
- `Camera.DOShakePosition`이 완료 시 원위치로 돌아오는지. 돌아오지 않으면 `RestoreCameraOnFinish`가 종료 시 복원한다.
- `ApplyFinalState`는 `async void`로 실행하고 실제로는 동기로 끝난다(테스트가 검사). 파생 클래스가 `Run()` 안에서 `Awaitable.NextFrameAsync()` 같은 것을 직접 기다리면 이 가정이 깨진다. 헬퍼만 쓰면 안전하다.
- `Together`는 넘긴 `Awaitable`을 순서대로 `await`한다. 이미 전부 시작된 뒤라 총 대기 시간은 가장 긴 것과 같지만, 앞의 것이 예외를 던지면 뒤의 것은 기다리지 않는다.
- `CutsceneActor`를 `Player` 오브젝트에 붙이면 `LanePlayer`(MonoThing)와 두 호스트가 한 오브젝트에 있게 된다. 모듈 목록이 각각 따로라 충돌은 없지만, 팀이 오브젝트당 호스트 하나를 원하면 구조를 바꿔야 한다.
- 대사 `Text`는 uGUI 레거시 `Text`다. 한글은 `Placeholder/Fonts/MonaS12.ttf`가 있어야 보인다.
