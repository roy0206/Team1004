# Animation

`Game.Animation`은 `SpriteRenderer.sprite`를 시간에 따라 갈아끼우는 스프라이트 플립북 재생기다. `Assets/Scripts/Animation/`에 있고 asmdef는 `Game.Animation`, 네임스페이스도 같다. 참조는 `Core.Modules` 하나뿐이라 플레이어·컷신·보스 어디서든 순환 참조 없이 쓸 수 있다.

## 2026-09-06 아트 갱신

`python Tools/sync_drive.py --source 아트`로 **추가 4 / 갱신 0 / 이동 0 / 삭제 0 / 유지 17**. 새로 온 것과 간 곳.

| 파일 | 알파 박스(px) | 간 곳 | 상태 |
| --- | --- | --- | --- |
| `보스/낚시바늘.png` | 102 × 956 (바늘만 102 × 169) | 보스 1 `Hook/HookSprite` | **배선 완료**(보스 담당, 2026-09-06). 배 아트가 같이 와서 스케일 규칙이 바뀌었다 — `Docs/Boss.md` 「보스 1 낚싯줄」 |
| `보스/물결/물결1.png` | 1887 × 335 (2장 합집합) | 보스 3 `Rapid0..2` + `Boss_Rapid.asset` | 클립까지. 프리팹 배선 대기 |
| `보스/물결/물결 2.png` | 1887 × 332 | 같음 | 같음 |
| `오브젝트/긴돌.png` | 692 × 779 | **상류 단차 앞면** `LedgeSet.prefab` `Ledge1~3/Step` | **적용 완료**(`Docs/Ledge.md` 「단차 모양 (긴 돌)」) |

이번 갱신으로 **연어 12장은 하나도 바뀌지 않았다** — 합집합 알파 박스 `x=[755,1282] y=[266,545]`(528 × 280)와 pivot `(0.5307292, 0.37592593)`이 그대로다. 장애물 다섯 장도 그대로라 `Docs/Spawner.md` 「프리팹 비주얼」의 스케일·콜라이더 숫자가 전부 유지된다.

곰 보스 아트(`보스/-곰-/곰 발 양옆.png`, `곰 발 위아래.png`)는 다른 에이전트가 맡고 있어 손대지 않았다.

## 규칙

- **Unity `Animator`·`AnimationClip`·`Animation` 컴포넌트를 쓰지 않는다.** 사용자 결정이다. 상태 머신은 이미 `Game.StateMachine`이, 연출 트윈은 DOTween이 맡고 있어서 Animator를 더하면 같은 일을 하는 축이 셋이 된다. 프레임 교체만 필요한 2D 플립북에 Animator Controller·`.anim` 에셋·머신 상태를 얹으면 이진 에셋이 늘어 병합 충돌이 생기고, 재생 길이를 코드에서 `JumpDuration`에 맞추기도 어렵다. 프레임 배열과 fps만 있으면 되는 문제라서 `ScriptableObject` + `Module`로 끝낸다.
- 씬 오브젝트 코드이므로 `MonoBehaviour`가 아니라 `Module`이다(`Core/Docs/Modules.md`, `Docs/CoreLoop.md` 「팀 규칙」). 런타임에 `GameObject`를 만들지 않는다.
- 비동기는 `Awaitable`뿐이다. Coroutine·`Task`는 쓰지 않는다.
- 시간은 기본이 scaled다. `Time.deltaTime`을 쓰므로 `Time.timeScale = 0`이면 애니메이션도 멈춘다. 일시정지 화면 뒤에서 계속 돌아야 하면 모듈을 unscaled로 만든다.

## 파일

| 파일 | 타입 | 하는 일 |
| --- | --- | --- |
| `CustomAnimation.cs` | `ScriptableObject` | 클립 데이터. 프레임 배열, fps, 루프, 길이 재정의, 프레임 이벤트 |
| `CustomAnimationEvent.cs` | `struct` | 프레임 인덱스 + 문자열 id |
| `FlipbookClock.cs` | 순수 C# | 경과 시간 → 프레임 인덱스·완료·이벤트 범위. `UnityEngine`을 쓰지 않아 EditMode 테스트로 전부 검증한다 |
| `FlipbookStep.cs` | `readonly struct` | `FlipbookClock.Advance` 한 번의 결과 |
| `SpriteAnimatorModule.cs` | `Module` (`Ticks = Update`) | 클록을 돌리고 `SpriteRenderer.sprite`에 반영. 이벤트·완료 통지 |
| `Editor/AnimationAssetGenerator.cs` | 에디터 | 아트 PNG 임포트 설정 + `Design/Animations` 클립 생성 |
| `Editor/SalmonClipTable.cs` | 에디터 | `FlipbookClipDefinition`·`FlipbookClipDuration`(클립 한 줄의 정의)과 연어 클립 표(에셋 이름, 아트 루트·하위 폴더, 프레임 접두사, 프레임 수, fps, loop, duration 출처) |
| `Editor/ObstacleClipTable.cs` | 에디터 | 장애물 클립 표. 지금은 `Obstacle_Fish` 하나. 루트가 `Art/오브젝트`라 연어 표와 같은 정의 타입을 다른 루트로 쓴다 |
| `Editor/SalmonFrameMatcher.cs` | 에디터, 순수 C# | 파일명 → 프레임 번호. 인덱스 앞뒤 공백과 **`-` 없음**을 관대하게 읽는다. 1프레임 클립용으로 **번호가 아예 없는 이름**(`충돌.png`)을 고르는 `IsSingleFrame`·`FindSingleFrame`도 있다 |
| `Editor/SharedPivotFolder.cs` | 에디터 | 「폴더 하나가 pivot 하나를 쓴다」 묶음. 합집합 알파 박스·30초 캐시·경로 판정. `SharedPivotFolders`가 연어 폴더와 `이빨물고기` 폴더를 등록한다 |
| `Editor/SalmonArtPostprocessor.cs` | `AssetPostprocessor` | 연어 아트 폴더 PNG를 임포트할 때마다 임포트 설정을 다시 걸고, **합집합 pivot이 어긋난 형제 프레임을 스스로 재임포트 예약**한다 |
| `Editor/ObjectArtPostprocessor.cs` | `AssetPostprocessor` | 오브젝트 아트 폴더(`Art/오브젝트/**`, 하위 폴더 포함) PNG에 같은 설정 + 폴더 규칙에 맞는 pivot을 다시 건다 |
| `Editor/ObstacleClipTable.cs` 옆 `Editor/BossClipTable.cs` | 에디터 | 보스 클립 표. 지금은 `Boss_Rapid` 하나. 루트가 `Art/보스`다 |
| `Editor/BossArtPostprocessor.cs` | `AssetPostprocessor` | **보스 아트 세 장**(`물결/*.png`, `낚시바늘.png`)에 같은 설정 + 규칙 pivot을 다시 건다. 낚싯바늘만 고정 pivot(`HookPivot`) |
| `Tests/FlipbookClockTests.cs`, `Tests/CustomAnimationTests.cs`, `Tests/SalmonClipTableTests.cs`, `Tests/ObstacleClipTableTests.cs`, `Tests/SharedPivotFolderTests.cs`, `Tests/ObjectArtImportTests.cs`, `Tests/BossArtImportTests.cs` | EditMode | 클록·클립 데이터 + 파일명 매칭·클립 표·아트 에셋·폴더 pivot 묶음·오브젝트 아트 임포트 설정 검증 |

## CustomAnimation

`CreateAssetMenu`는 `Team1004/Custom Animation`이다. 정본은 `Assets/GameAssets/Design/Animations/`에 둔다(기획이 조정할 값이므로 코드 상수가 아니라 데이터다).

| 필드 | 기본값 | 뜻 |
| --- | --- | --- |
| `frames` (`Sprite[]`) | 빈 배열 | 재생 순서대로의 프레임. 0번이 첫 프레임 |
| `framesPerSecond` | `12` | 초당 프레임. `duration`이 0일 때만 길이 계산에 쓰인다 |
| `loop` | `false` | 끝에서 0번으로 돌아갈지 |
| `duration` | `0` | 0보다 크면 한 바퀴 길이(초)를 이 값으로 **재정의**한다. fps는 무시된다 |
| `events` (`CustomAnimationEvent[]`) | 빈 배열 | `Frame`(인덱스)에 들어갈 때 발화할 `Id` 문자열 |

읽기 전용 멤버.

| 멤버 | 설명 |
| --- | --- |
| `FrameCount` | 프레임 수 |
| `Length` | 한 바퀴 길이(초). `duration > 0`이면 `duration`, 아니면 `FrameCount / FramesPerSecond`. 프레임이 없거나 fps가 0 이하면 `0` |
| `GetFrame(float time)` | 시간 → **프레임 인덱스**. 루프면 한 바퀴를 빼고 나서 나누고(경계에서 정확히 0이 나온다), 원샷이면 마지막 프레임으로 클램프한다. 음수·0은 0 |
| `GetSprite(int index)` | 인덱스 → `Sprite`. 범위 밖은 클램프, 프레임이 없으면 `null` |
| `EventCount`, `GetEvent(int)` | 이벤트 목록. 범위 밖이면 `default`(빈 id) |
| `Validate(out string error)` | 프레임 없음, fps ≤ 0, 음수 duration, 빈 프레임 슬롯, 빈 이벤트 id, 범위 밖 이벤트 프레임을 잡는다. 생성기가 만들 때마다 돌린다 |

`Sprite`는 `[SerializeField] private` 배열이고 public 필드는 없다. 에디터 전용 `EditorInitialize`가 `#if UNITY_EDITOR`로 감싸져 있어 생성기만 값을 넣는다.

## SpriteAnimatorModule

```csharp
var animator = AddModule(new SpriteAnimatorModule(GetComponent<SpriteRenderer>()));
var unscaled = AddModule(new SpriteAnimatorModule(renderer, useUnscaledTime: true));
```

| 멤버 | 동작 |
| --- | --- |
| `Play(clip)` | 재생. **이미 그 클립을 같은 길이로 재생 중이면 아무것도 하지 않는다**(매 프레임 호출해도 안전). 다른 클립이면 0프레임부터. 멈춘 상태에서 같은 클립을 다시 넣으면 처음부터 다시 재생한다 |
| `Play(clip, duration)` | 한 바퀴를 `duration`초에 맞춘다. 클립의 `Length`를 무시한다. 점프 5프레임을 `GameConfig.JumpDuration`(0.8)에 맞출 때 쓴다. `duration ≤ 0`이면 클립 길이 |
| `PlayAsync(clip)` / `PlayAsync(clip, duration)` | `Awaitable` 반환. **원샷**은 마지막 프레임을 표시한 뒤 완료한다. **루프**는 한 바퀴가 끝나면 완료하되 재생은 계속한다. 이미 같은 클립·같은 길이로 재생 중이면 진행 중인 재생의 완료를 돌려준다 |
| `Stop()` | 틱만 멈춘다. **현재 프레임은 그대로 남는다.** 대기 중인 `PlayAsync`는 즉시 완료된다 |
| `SetFrame(clip, index)` | 정지 포즈. 재생을 멈추고 그 프레임을 표시한다. 범위 밖 인덱스는 클램프. 대기 중인 `PlayAsync`는 즉시 완료 |
| `Speed` | 재생 속도 배수. 음수는 0으로 잘린다(역재생 없음) |
| `IsPlaying`, `Current`, `CurrentFrame`, `NormalizedTime` | 상태 조회. `NormalizedTime`은 현재 바퀴 안에서 0~1 |
| `UseUnscaledTime`, `PlaybackLength` | 생성자 옵션과 지금 재생 중인 한 바퀴 길이 |
| `event Action<string> FrameEvent` | 프레임에 들어갈 때 그 프레임의 이벤트 id를 순서대로 발화 |
| `event Action<CustomAnimation> Completed` | 원샷 종료, 또는 루프 한 바퀴 완료 |

동작 세부.

- **틱 정지**: 호스트가 `animator.IsEnabled = false`로 두면 모듈이 틱을 받지 않아 프레임이 멈춘다. 떼지 않으므로 다시 켜면 그 자리에서 이어간다.
- **매 프레임 할당 없음**: `Advance`는 `readonly struct`를 돌려주고, 이벤트 목록은 인덱스로 훑고, `SpriteRenderer.sprite`는 **프레임이 실제로 바뀔 때만** 대입한다.
- **완료 가드**: `AwaitableCompletionSource`는 `TrySetResult`로만 완료하고, 완료시킨 뒤 필드를 즉시 `null`로 비운다. 두 번 `SetResult`가 되어 예외가 나는 경로는 없다. 모듈이 떼어질 때(`OnDetached`)도 대기 중인 것을 완료시켜 `await`가 영원히 매달리지 않는다.
- **이벤트 누락 없음**: 클록은 프레임 인덱스가 아니라 단조 증가하는 전역 인덱스로 "어디까지 들어갔는지"를 기억한다. 한 프레임에 dt가 커서 여러 프레임을 건너뛰어도 건너뛴 프레임의 이벤트가 전부, 각각 한 번씩 발화한다. 다만 한 번의 `Advance`에서 발화하는 범위는 **최대 한 바퀴**로 자른다(dt가 몇 초짜리로 튀어도 이벤트 수천 개가 쏟아지지 않게).
- **재진입**: `FrameEvent` 핸들러가 다른 클립을 `Play`하거나 `Stop`하면 그 자리에서 남은 이벤트 발화와 완료 처리를 멈춘다.

## 스킵과 일시정지

- **스킵**(컷신 Space, 재도전 시 컷신 건너뛰기): `Stop()`을 부르면 대기 중인 `PlayAsync`가 즉시 완료되고 화면에는 마지막으로 그린 프레임이 남는다. 끝 포즈로 맞추고 싶으면 `SetFrame(clip, clip.FrameCount - 1)`을 이어서 부른다. 컷신 헬퍼가 「스킵 시 목표값을 즉시 적용」하는 규약과 같은 모양이다.
- **일시정지**: 기본 모듈은 `Time.deltaTime`을 쓰므로 `Time.timeScale = 0`이면 자동으로 멈추고 프레임이 유지된다. 일시정지 UI 뒤에서도 돌아야 하는 연출(예: 타이틀 배경 물고기)은 `new SpriteAnimatorModule(renderer, true)`로 unscaled 모듈을 쓴다.
- 개별 오브젝트만 멈추려면 `IsEnabled = false`가 가장 싸다.

## 생성기

메뉴 두 개다.

- `Team1004/Generate Animation Assets` — 없는 것만 만든다(`Generate(false)`).
- `Team1004/Regenerate Animation Assets (Overwrite)` — 확인 대화 후 기본값으로 덮어쓴다(`Generate(true)`).

코드에서는 `AnimationAssetGenerator.Generate()`(= `Generate(false)`), `Generate(bool overwrite)`, 배치용 `RegenerateAllBatch()`를 쓴다.

한 번 돌 때 하는 일 순서: ① 폴더 pivot 캐시를 버리고 연어 합집합 pivot을 다시 계산한다 → ② 연어 12장에 임포트 설정을 다시 걸고 `SaveAndReimport`한다(**합집합이 바뀌면 12장 전부가 다시 임포트된다**) → ③ 장애물 클립이 쓰는 오브젝트 아트 프레임에 같은 일을 한다(폴더 묶음이면 합집합 pivot) → ④ 보스 클립이 쓰는 프레임(물결 2장)과 `낚시바늘.png`에 같은 일을 한다 → ⑤ 옛 `Player_Idle.asset`을 지운다 → ⑥ 연어·장애물·보스 표의 클립을 굽는다.

배치 실행:

```
Unity.exe -batchmode -nographics -projectPath "D:/Unity/Team1004/Team1004" -quit \
  -executeMethod Game.Animation.Editor.AnimationAssetGenerator.GenerateMissing \
  -logFile "D:/Unity/Team1004/Team1004/Logs/batch_anim_gen.log"
```

### (a) 아트 폴더

정본은 `Assets/GameAssets/Art/물고기 애니메이팅/`이고 클립마다 하위 폴더가 하나다. 전부 1920×1080 RGBA다.

| 하위 폴더 | 파일 | 클립 |
| --- | --- | --- |
| `기본/` | `물고기 기본-1.png`, `물고기 기본-2.png` | `Player_Swim` |
| `올라가기/` | `물고기 올라가기 -1.png`, `물고기 올라가기-2.png` | `Player_LaneUp` |
| `내려가기/` | `물고기 내려가기 -1.png`, `물고기 내려가기-2.png` | `Player_LaneDown` |
| `점프/` | `물고기 점프-1.png` ~ `-5.png` | `Player_Jump` |
| `충돌/` | `충돌.png` (2026-09-06 추가) | `Player_Hit` |

`충돌/`은 **번호가 없는 파일 한 장**이라 `SalmonFrameMatcher.TryGetFrameIndex`가 걸러낸다(접두사만 있고 숫자가 없으면 프레임이 아니다 — 그 판정은 그대로 둔다). 대신 `GetFramePath`가 프레임 수 1짜리 클립에 한해 `FindSingleFrame`으로 **접두사와 파일명이 완전히 같은 한 장**을 받아들인다. 그래서 `충돌/충돌.png`가 `Player_Hit`의 유일한 프레임이 된다.

**파일명이 일정하지 않다.** 인덱스 앞에 공백이 있는 것(`물고기 올라가기 -1.png`)과 없는 것(`물고기 올라가기-2.png`)이 섞여 있고, 장애물 물고기(이빨물고기)는 `-`가 아예 없고 1번 프레임에는 번호도 없다(`이빨.png`, `이빨 2.png`). 그래서 경로를 문자열로 조립하지 않고 폴더의 `*.png`를 훑어 `SalmonFrameMatcher`로 고른다. 규칙은 「접두사 → 공백 몇 개든 → `-`(**있어도 없어도 된다**) → 공백 몇 개든 → 1 이상의 십진수」이고 이름은 NFC로 정규화해 비교한다. `-`가 선택이라도 접두사 바로 뒤에 숫자가 아닌 글자가 붙은 이름(`물고기 기본자세-1`, `장애물 물고기알1`)은 걸리지 않는다. 못 찾으면 `접두사-번호.png`를 그대로 쓰고 경고를 남긴다. **1번 프레임만은 번호 없는 이름(`접두사.png`)도 받는다** — `GetFramePath`의 단일 프레임 대체 경로를 `FrameCount == 1`에서 `index == 0`으로 넓혔다(2026-09-06 이빨물고기). 번호가 붙은 1번 프레임이 있으면 그쪽이 먼저다.

옛 경로 `Assets/GameAssets/Art/점프/`는 Drive에서 `물고기 애니메이팅/점프/`로 옮겨졌다. 빈 폴더와 그 `.meta`는 지웠고, `Tools/sync_drive.py`는 이제 이동·삭제 뒤 빈 폴더와 `.meta`를 스스로 정리한다(`prune_empty_dirs`).

### (b) 아트 PNG 임포트 설정

이 경로는 아트 미러라 파일 자체는 건드리지 않고 `.meta`의 임포트 설정만 스크립트로 바꾼다. 손으로 고치지 않는다 — `SalmonArtPostprocessor`가 임포트마다 아래 표를 다시 건다.

| 항목 | 값 | 이유 |
| --- | --- | --- |
| `textureType` | Sprite | |
| `spriteMode` | Single | 원래 Multiple(시트 1장짜리)이었다. 프레임당 파일이 하나라 Single이 맞다 |
| `spriteAlignment` / `spritePivot` | Custom / 계산값 | 아래 |
| `spritePixelsPerUnit` | 100 | |
| `spriteMeshType` | Tight | 캔버스의 90%가 투명이라 FullRect면 오버드로가 크다 |
| `mipmapEnabled` | false | 2D 고정 화면이라 밉맵이 필요 없다 |
| `textureCompression` | Uncompressed | 압축 아티팩트 없이. 대신 메모리를 먹는다(아래) |
| `crunchedCompression` | false | |
| `maxTextureSize` | 2048 | 1920이 줄어들지 않게 |
| `alphaIsTransparency` | true, `filterMode` Bilinear, `wrapMode` Clamp | |

**pivot 계산 — 12장이 pivot 하나를 쓴다**: 12장의 PNG를 디스크에서 바이트로 읽어 임시 `Texture2D`에 `LoadImage`하고(임포트 설정의 `isReadable`을 켜지 않아도 된다) 알파 ≥ 8인 픽셀의 바운딩 박스를 **12장 합집합**으로 구한 뒤 그 중심을 정규화한다. 클립마다 pivot이 다르면 수영 → 올라가기 → 점프로 넘어갈 때 연어가 한 프레임씩 튄다. 그래서 클립별이 아니라 **폴더 전체 합집합 하나**를 모든 PNG에 똑같이 건다. `충돌/`도 하위 폴더라 같은 묶음에 들어간다(`SharedPivotFolders.Salmon`은 `Recursive = true`이고, 프레임 목록은 `SalmonClipTable`에서 받으므로 표에 `Player_Hit`을 넣은 순간 합집합에 포함된다 — 폴더 등록을 따로 손댈 필요가 없었다).

현재 결과(2026-09-06 아트 동기화 반영, 12장): 합집합 바운딩 박스 `x=[755,1282] y=[266,545]`, 크기 **528×280 px**(1920×1080) → pivot **`(0.5307292, 0.37592593)`**. 연어 몸통 중심이 오브젝트 원점이 되므로 `transform.position`을 레인 Y에 맞추면 연어 중심이 레인에 온다. 파일을 하나도 못 읽으면 이 값을 상수(`AnimationAssetGenerator.SalmonFallbackPivot`)로 쓴다.

중심은 `Rect.center`, 즉 `(최솟값 + 최댓값 + 1) / 2`다(박스는 양끝 픽셀을 포함한다). x는 `(755 + 1282 + 1) / 2 / 1920 = 1019 / 1920`, y는 `(266 + 545 + 1) / 2 / 1080 = 406 / 1080`. 반 픽셀을 빼먹고 `(min + max) / 2`로 계산하면 x가 `0.5304688`이 되어 후처리기가 쓰는 값과 어긋난다.

**2026-09-06 동기화로 `기본-1`·`기본-2`가 바뀌고 `충돌`이 새로 왔다.** 옛 값은 `x=[752,1282]`, 531×280, pivot `(0.5299479, 0.3759259)`였다. 새 `기본-1`의 x 최솟값이 752 → 756으로 올라가고 새 `기본-2`가 755를 찍어 합집합 x가 `[755,1282]`가 됐다. y 합집합은 `올라가기 -1`/`점프-2`(266)와 `내려가기 -1`/`점프-5`(545)가 그대로 잡아 **바뀌지 않았다** — 그래서 이번 갱신은 **x만 움직였다**(`0.5299479` → `0.5307292`, 차이 0.00078). 새 `충돌`(x 762~1232, y 277~525)은 합집합 안에 완전히 들어가 경계를 넓히지 않는다.

합집합은 **12장 전부의 pivot**이라 한 장만 바뀌어도 나머지 11장의 `.meta`를 다시 써야 한다. 이제는 메뉴를 돌리지 않아도 된다 — 아래 「형제 재임포트」를 보라. 메뉴(`Generate`)도 여전히 `EnumerateFramePaths()` 전체를 돌며 `ApplyImportSettings` + `SaveAndReimport`를 하므로 한 번이면 12장이 같이 맞는다(계산은 루프 전에 한 번 하고 캐시에 넣어 후처리기도 같은 값을 쓴다).

프레임별 알파 박스(y는 Unity 기준, 아래가 0. 양끝 포함):

| 프레임 | x | y |
| --- | --- | --- |
| 기본-1 (2026-09-06 갱신) | 756~1277 | 291~514 |
| 기본-2 (2026-09-06 갱신) | **755**~1277 | 293~516 |
| 내려가기-2 / 올라가기-2 / 점프-1 | 756~1277 | 293~515 |
| 내려가기 -1 / 점프-5 | 764~1265 | 285~**545** |
| 올라가기 -1 / 점프-2 | 759~**1282** | **266**~511 |
| 점프-3 | 770~1277 | 284~533 |
| 점프-4 | 766~1271 | 292~523 |
| 충돌 (2026-09-06 추가) | 762~1232 | 277~525 |

굵은 숫자가 합집합 경계를 만드는 값이다.

**형제 재임포트(`SalmonArtPostprocessor`).** `OnPreprocessTexture`가 자기 설정을 건 뒤, 방금 계산한 합집합 pivot을 **다른 11장의 현재 `spritePivot`과 비교**한다(`FindStaleSiblings`). `spriteAlignment`가 Custom이 아니거나 x·y 중 하나라도 `PivotEpsilon = 1e-5`보다 크게 다르면 그 경로를 정적 `HashSet`에 담고, `EditorApplication.delayCall`에 **한 번만**(`flushScheduled` 플래그) 플러시를 예약한다. 플러시는 집합을 비우고 `AssetDatabase.StartAssetEditing()` 안에서 `ImportAsset(..., ForceUpdate)`를 돌린다. 값이 **실제로 다를 때만** 담으므로 전부 맞은 상태에서는 아무것도 예약되지 않고, 재임포트된 형제가 자기 pivot을 새 값으로 쓰고 나면 다음 판정에서 걸리지 않아 임포트 루프가 멈춘다(최대 두 바퀴: 한 바퀴는 stale 목록을 만들고, 그 바퀴 안의 형제들이 아직 디스크에 옛 값을 가진 나머지를 한 번 더 예약할 수 있다). Drive에서 아트 한 장만 새로 와도 **메뉴를 돌리지 않고** 12장 pivot이 맞는다.

**`충돌.png.meta`는 딱 한 번 손으로 썼다.** `Player_Hit.asset`이 `충돌.png`의 스프라이트를 `{fileID: 21300000, guid: …}`로 가리켜야 하는데, 에셋 YAML을 손으로 쓰는 시점에 그 GUID가 존재하려면 `.meta`가 먼저 있어야 한다(에디터가 열려 있어 배치 임포트를 강제할 수 없었다). 그래서 `기본/물고기 기본-1.png.meta`를 본으로 GUID·스프라이트 이름(`충돌_0`)·`spritePivot`만 새 값으로 바꿔 썼다. **이것이 아트 미러에서 손으로 쓴 유일한 `.meta`다.** 임포트가 한 번 돌면 후처리기가 같은 값을 다시 계산해 덮으므로 결과가 달라지지 않는다(`rect`·`internalID`·`spriteID`는 Unity가 다시 쓴다). 나머지 11장은 **손대지 않았다** — 위의 형제 재임포트가 맞추게 두었다. 그전까지는 `.meta`에 옛 x(`0.52994794`)가 남아 있고, `SharedPivotFolderTests.EverySalmonMetaUsesTheRecomputedUnionPivot`과 `SalmonArtAssetTests.EveryFrameShareOnePivot`이 실패하는 것이 정상이다. `충돌/` **폴더** `.meta`는 쓰지 않았다 — 폴더 GUID를 가리키는 곳이 없어 Unity가 임포트할 때 스스로 만든다.

### (c) 생성되는 클립

`Assets/GameAssets/Design/Animations/`. 표의 정본은 연어가 `Editor/SalmonClipTable.cs`, 장애물이 `Editor/ObstacleClipTable.cs`이고 값은 **에셋에 굽는다**(런타임 상수가 아니다). 두 표는 같은 `FlipbookClipDefinition`을 쓰고 **아트 루트 폴더만 다르다**(연어 `Art/물고기 애니메이팅`, 장애물 `Art/오브젝트`). `Generate`는 두 표를 차례로 돈다.

| 에셋 | 아트 폴더 | 프레임 | fps | loop | duration | 비고 |
| --- | --- | --- | --- | --- | --- | --- |
| `Player_Swim.asset` | `물고기 애니메이팅/기본/` | 2 | 6 | **true** | 0 | 길이 = 2/6 ≈ 0.333초. 상시 도는 수영·대기 루프 |
| `Player_LaneUp.asset` | `물고기 애니메이팅/올라가기/` | 2 | 10 | false | **`LaneMoveDuration`(0.2)** | 레인 위로 이동 |
| `Player_LaneDown.asset` | `물고기 애니메이팅/내려가기/` | 2 | 10 | false | **`LaneMoveDuration`(0.2)** | 레인 아래로 이동 |
| `Player_Jump.asset` | `물고기 애니메이팅/점프/` | 5 | 12 | false | **`JumpDuration`(0.8)** | |
| `Player_Hit.asset` | `물고기 애니메이팅/충돌/` | **1** | 6 | false | 0 | 피격·사망 포즈(눈이 X). 한 장이라 사실상 정지 포즈다. 길이 = 1/6 ≈ 0.167초지만 `SetFrame(hitClip, 0)`으로 쓰면 길이가 무의미하다 |
| `Obstacle_Fish.asset` | `오브젝트/이빨물고기/` | 2 | 6 | **true** | 0 | 길이 ≈ 0.333초. 장애물 물고기가 헤엄치는 루프. `Fish.prefab`의 `ObstacleThing.swimClip`에 생성기가 연결한다(`Docs/Spawner.md` 「프리팹 비주얼」) |
| `Boss_Rapid.asset` | `보스/물결/` | 2 | 6 | **true** | 0 | 길이 ≈ 0.333초. 폭포 보스의 강한 물줄기(급류) 루프. `WaterfallBoss.rapidClip`에 연결하고 `rapidRenderers` 3개가 같은 클립을 돈다(`Docs/Boss.md` 「보스 3」) |

duration은 생성기가 `Assets/GameAssets/Design/GameConfig.asset`의 `GameConfigValues`에서 읽어 굽는다(`SalmonClipDuration.LaneMove`/`Jump`). 에셋이 없으면 `GameConfigValues` 기본값(0.2 / 0.8)을 쓴다. 플레이어는 재생할 때 `Play(clip, GameConfig.Current.XxxDuration)`으로 길이를 다시 넘기므로 런타임 config를 바꿔도 어긋나지 않고, 에셋 값은 컷신·미리보기용 기본값이다.

`Player_Idle`은 없앴다. 정지 포즈가 필요하면 `SetFrame(swimClip, 0)`이다. 생성기가 남아 있는 옛 `Player_Idle.asset`을 지운다.

`Player_Hit.asset`과 `Player_Hit.asset.meta`, 그리고 `충돌.png.meta`는 에디터가 열려 있어 생성기를 돌릴 수 없던 2026-09-06에 **YAML로 직접 썼다**(GUID `acbc43f2e6f54c9dac8757ca2094972e`). 구조는 `Player_Swim.asset`과 같고 `frames`가 스프라이트 하나뿐이다. `Generate(false)`는 이미 있는 에셋을 건드리지 않고, `NeedsRepair`도 프레임 수 1·슬롯 채움이라 통과하므로 나중에 메뉴를 돌려도 덮이지 않는다.

`Generate(false)`는 이미 있는 에셋의 fps·duration·loop를 건드리지 않는다(손으로 조정한 값이 남는다). 다만 **프레임 수가 표와 다르거나 프레임 슬롯이 비면 그 클립만 다시 굽는다**(`NeedsRepair`). 아트가 프레임을 늘려 와도 메뉴 한 번이면 맞고, 여러 번 눌러도 결과가 같다(멱등·배치 안전). 전부 기본값으로 되돌리는 건 별도 메뉴(`Generate(true)`, 배치는 `RegenerateAllBatch()`)다.

### 메모리 비용 — 아트에 요청함

1920×1080 RGBA32 무압축은 **한 장 7.9 MiB**다. 12장이면 **약 95 MiB**가 텍스처로 상주한다. 실제 연어는 캔버스 안에서 약 528×280 px, 즉 전체 픽셀의 **7%**뿐이고 나머지는 투명이다. 캔버스를 연어 크기(예: 560×300)로 잘라 오면 12장이 **약 7.2 MiB**로 줄고 아틀라스에도 들어간다. `Docs/Requests.md`에 「아트: 캐릭터 크기에 맞춘 캔버스로 요청」으로 남겼다(이제 12장 전부에 해당한다). 캔버스가 바뀌면 pivot 계산은 그대로 다시 돌리면 된다(생성기가 알파 바운딩 박스에서 구하므로 값만 바뀐다). **12장이 같은 캔버스·같은 연어 위치**여야 한다는 조건만 지켜지면 된다.

임시로 넘길 방법: 압축을 켜거나(`textureCompression`을 Normal로) `maxTextureSize`를 1024로 내리면 메모리는 1/4씩 줄지만 해상도·품질이 떨어진다. 지금은 무압축·2048로 두었다.

## 통합 절차

**완료(코어루프 통합).** 아래는 실제 붙은 형태다.

### 플레이어 (`Game.Player`)

- `Game.Player.asmdef` → `Game.Animation`, `Game.Water`.
- `LanePlayer`: `[SerializeField] CustomAnimation swimClip/laneUpClip/laneDownClip/jumpClip`(생성기가 `Design/Animations`의 네 에셋을 연결), `Awake` 끝에서 `animator = AddModule(new SpriteAnimatorModule(spriteRenderer))`. `Start`와 `SnapToLane`이 `PlaySwim()`.
- **레인 이동 클립**: `LaneMoveModule`이 `MoveStarted(int direction)`(이동 시작, `-1` 위 / `+1` 아래)와 `MoveFinished(int lane)`(이동 끝)를 쏘고 `MoveDirection` 프로퍼티로 진행 방향을 읽을 수 있다. `LanePlayer.OnModuleMoveStarted`가 방향에 맞는 클립을 `Play(clip, GameConfig.Current.LaneMoveDuration)`로 재생하고, `OnModuleMoveFinished`가 `PlaySwim()`으로 돌아온다. 기존 `LaneChanged`는 이동 시작 시점 그대로다(`PlayFlow`가 쓴다).
- **점프가 이긴다**: 이동 중에 점프가 시작되면(`jump.IsAirborne`) `OnModuleMoveStarted`·`OnModuleMoveFinished`가 둘 다 아무것도 하지 않아 점프 클립이 끊기지 않는다. `OnModuleJumped`가 `Play(jumpClip, GameConfig.Current.JumpDuration)`, `OnModuleLanded`가 `PlaySwim()`. 평소 입력 경로에서는 `CanAcceptInput`이 이동 중 점프를 막으므로 이 경합은 보스·컷신이 모듈을 직접 부를 때만 생긴다.
- `idleClip`은 없앴다. 정지 포즈는 `animator.SetFrame(swimClip, 0)`이다.
- **피격 포즈**: `Player_Hit.asset`(GUID `acbc43f2e6f54c9dac8757ca2094972e`)이 1프레임 정지 포즈다. `LanePlayer`의 `hitClip` 연결과 재생 시점은 코어루프 쪽이 정한다(`Game.Animation`은 에셋만 제공한다). 한 장뿐이라 `Play`보다 `SetFrame(hitClip, 0)`이 뜻이 분명하다.
- **애니메이터는 `InputEnabled`를 따라 멈춘다**(`IsEnabled`). 컷신·결과 화면·피격 중에는 플레이어 애니메이터가 틱을 받지 않으므로, 같은 `SpriteRenderer`를 쓰는 컷신 배우 애니메이터(`CutsceneActor.Animator`)와 싸우지 않는다. 컷신이 끝나면 `PlayFlow`가 `SnapToLane`을 불러 수영 포즈로 돌아온다.
- 스케일: 생성기가 `Player` 오브젝트에 `GameConfig.playerScale`(0.29)을 `localScale`로 준다. 528×280px 연어가 약 1.53×0.81 unit이 되어 레인 간격 1.1 안에 든다. 히트박스는 `BoxCollider2D.size = 알파 박스 × playerHitboxScale(0.87)`, `offset = (알파 박스 중심 − pivot)/PPU`(pivot이 박스 중심이라 0). 값은 `GameConfigValues`에 있다. 아래 숫자(로컬 4.58×2.44, 월드 1.33×0.71)는 531×280이던 시절 값이라 코어루프 생성기를 다시 돌리면 폭이 조금 줄어든다.
- `HitReactionModule`은 색만, 애니메이터는 `sprite`만 바꿔 겹치지 않는다.

### 컷신 베이스 (`Game.Cutscene`)

`Docs/Cutscene.md` 「헬퍼」에 `Animate`, `SetPose`, `Clip`이 있다. `CutsceneActor`는 `SpriteRenderer`가 있을 때만 `Animator`(지연 `AddModule`)를 만든다. 클립은 `CutscenePlayer.clips`(id → `CustomAnimation`)로 주입되며 생성기가 `player_jump`, `player_swim`, `player_lane_up`, `player_lane_down` 네 개를 연결한다. `CutsceneBase`에 `PlayerJumpClip`·`PlayerSwimClip`·`PlayerLaneUpClip`·`PlayerLaneDownClip` 접근자가 있다. `EndingCutscene`이 도착 직후 `Animate(Player, PlayerJumpClip)`을 한 번 쓴다.

### 장애물 (`Game.Spawner`)

`Game.Spawner.asmdef` → `Game.Animation`. `ObstacleThing`에 `[SerializeField] private CustomAnimation swimClip`이 있고, 클립이 있을 때만 `OnSpawned`에서 `AddModule(new SpriteAnimatorModule(visual))` 후 `Play(swimClip)`(루프), `OnReleased`에서 `Stop()` 뒤 `ClearModules()`를 한다. 풀에서 꺼낼 때마다 모듈을 새로 붙이므로 회수된 오브젝트에 프레임 상태가 남지 않는다. 돌·통나무는 클립이 없어 모듈도 만들지 않는다. 클립이 있는 장애물은 `variants`를 비워 둔다(`ApplyVariant`가 `sprite`를 덮으면 애니메이션과 싸운다). 자세한 것은 `Docs/Spawner.md` 「프리팹 비주얼」.

### 보스 (`Game.Boss`)

아직 붙이지 않았다. 보스 아트가 오면 「생성기」에 `Boss_*` 클립을 더하고 각 페이즈 `OnEnter`에서 `Play(clip, 상태 길이)`를 부른다.

### 아트 임포트 설정 유지 (`Editor/SalmonArtPostprocessor.cs`)

`Assets/GameAssets/Art/`는 Drive 미러라 `Tools/sync_drive.py`가 PNG 내용이 바뀌면 파일을 덮어쓴다(`.meta`는 파일 이동·삭제 때만 같이 옮기거나 지운다). `.meta`가 남아도 알파 박스가 바뀌면 pivot이 어긋나므로 `AssetPostprocessor.OnPreprocessTexture`가 **`Assets/GameAssets/Art/물고기 애니메이팅/` 아래 모든 `*.png`**를 임포트할 때마다 위 표의 설정(Sprite, Single, PPU 100, Tight, 밉맵 없음, 무압축, 2048, Custom pivot)을 다시 적용한다(`IsSalmonFramePath`). pivot은 `AnimationAssetGenerator.ResolveSharedPivotCached()`(= `SharedPivotFolders.Salmon.ResolvePivotCached()`, 12장 합집합 알파 박스, 30초 캐시, 파일이 없으면 고정값 `AnimationAssetGenerator.SalmonFallbackPivot`)다. 12장을 한꺼번에 임포트해도 첫 장만 계산하고 나머지는 캐시를 쓴다. 생성기의 `ConfigureSpriteImporter`도 같은 `ApplyImportSettings`를 부른 뒤 `SaveAndReimport`하므로 둘이 어긋나지 않는다. `Generate`는 시작할 때 `SharedPivotFolders.InvalidateAll()`로 캐시를 버리고 다시 계산하므로, 아트가 바뀐 직후에 돌려도 옛 값이 남지 않는다.

**한 장만 임포트돼도 나머지가 따라온다.** 예전에는 Drive가 PNG 한 장만 갈아치우면 그 한 장의 `.meta`만 새 합집합 pivot으로 바뀌고 나머지는 옛 값으로 남아, 메뉴를 손으로 돌릴 때까지 연어가 클립 전환에서 튀었다. 이제 후처리기가 「(b) 아트 PNG 임포트 설정」의 **형제 재임포트**를 하므로 임포트 한 번이면 12장이 스스로 맞는다. `PivotEpsilon = 1e-5`보다 작은 차이는 무시하고 `EditorApplication.delayCall` 예약은 한 번만 걸리므로 임포트가 무한히 도는 경로는 없다.

**미러 `.meta`는 손으로 고치지 않는다** — 값은 전부 이 후처리기가 만든다. 예외는 새로 온 `충돌.png.meta` 한 장뿐이고 이유는 「(b) 아트 PNG 임포트 설정」 끝에 적었다.

`AnimationAssetGenerator.TryGetSalmonBounds(out Rect, out Vector2Int)`가 **12장 합집합** 알파 박스(픽셀)와 텍스처 크기를 돌려주며 코어루프 생성기가 히트박스 계산에 쓴다. 클립이 바뀌어도 히트박스가 같아 판정이 흔들리지 않는다. 2026-09-06 갱신으로 이 박스가 531×280에서 **528×280**으로 줄었으니, 코어루프 생성기를 다시 돌리면 플레이어 히트박스와 스케일이 조금 움직인다.

### 후처리기 범위 — 연어와 오브젝트

알파 박스 계산은 `AnimationAssetGenerator.TryGetAlphaBounds`로 일반화했다. 오버로드가 둘이다: `IReadOnlyList<string>`(여러 장 합집합)와 `string`(한 장). `TryGetSalmonBounds`는 앞의 것을 부르는 얇은 껍데기이고, `AlphaBoundsPivot(Rect, Vector2Int)`가 박스 중심을 정규화한다. 임포트 설정 자체(`ApplyImportSettings`)는 두 후처리기가 그대로 공유한다.

**폴더 pivot 규칙(`SharedPivotFolder`).** 「pivot을 공유하는 폴더」를 하나의 값 객체로 만들었다. 생성자는 `(폴더 경로, 하위 폴더까지 볼지, 못 읽었을 때 쓸 값, 프레임 목록을 따로 줄지)`이고, `Contains(경로)`로 자기 소관인지 판정하며 `ResolvePivot()`이 폴더 안 PNG **전부의 합집합** 알파 박스 중심을 돌려준다. `ResolvePivotCached()`는 30초 캐시라 여러 장을 한꺼번에 임포트해도 계산이 한 번이다. 등록표는 `SharedPivotFolders`다.

| 묶음 | 폴더 | 하위 폴더 | 프레임 | 공유 pivot |
| --- | --- | --- | --- | --- |
| `SharedPivotFolders.Salmon` | `Art/물고기 애니메이팅` | 본다 | 12장(클립 표에서 받는다 — `충돌/` 포함) | `(0.5307292, 0.37592593)` |
| `SharedPivotFolders.ObstacleFish` | `Art/오브젝트/이빨물고기` | 안 본다 | 2장(폴더의 `*.png`) | `(0.4945312, 0.5518519)` |
| `SharedPivotFolders.BossWave` | `Art/보스/물결` | 안 본다 | 2장(폴더의 `*.png`) | `(0.5085937, 0.36712963)` |

이 표에 없는 PNG는 **파일별** pivot이다(돌 `(0.18125, 0.11667)`, 통나무 `(0.56693, 0.39352)`, 긴돌 `(0.6557292, 0.36064816)`). 새 애니메이션 오브젝트가 오면 하위 폴더를 하나 만들고 `SharedPivotFolders`에 한 줄 더하면 된다.

| | `SalmonArtPostprocessor` | `ObjectArtPostprocessor` | `BossArtPostprocessor` |
| --- | --- | --- | --- |
| 대상 | `Art/물고기 애니메이팅/**/*.png` | `Art/오브젝트/**/*.png`(하위 폴더 포함) | **`Art/보스/물결/*.png`와 `Art/보스/낚시바늘.png` 딱 이 셋뿐** |
| 판정 | `AnimationAssetGenerator.IsSalmonFramePath` | `ObjectArtPostprocessor.IsObjectArtPath` | `BossArtPostprocessor.IsBossArtPath` |
| pivot | 항상 `SharedPivotFolders.Salmon`(12장 합집합, 30초 캐시) | `SharedPivotFolders.Find(경로)`가 묶음을 찾으면 그 합집합, 없으면 그 파일의 알파 박스 중심 | 낚시바늘은 고정 `HookPivot`, 물결은 `SharedPivotFolders.BossWave` 합집합 |
| 이유 | 클립을 넘길 때 연어가 튀면 안 된다 | 돌·통나무는 서로 다른 오브젝트라 공유할 이유가 없고, 한 오브젝트의 애니메이션 프레임끼리는 공유해야 튀지 않는다 | 물결 2프레임은 튀면 안 되고, 낚싯바늘은 pivot이 **그림의 중심이 아니라 바늘**이어야 한다 |
| 형제 재임포트 | 한다(`FindStaleSiblings` + `delayCall` 1회) | 안 한다 | 안 한다 |
| 나머지 설정 | Sprite, Single, PPU 100, Tight, 밉맵 없음, 무압축, 2048, Custom pivot | 같음 | 같음 |

**`BossArtPostprocessor`의 대상이 좁은 이유.** `Art/보스/` 아래에는 다른 작업자(곰 보스)의 아트도 들어온다(`Art/보스/-곰-/`). 폴더 전체를 잡으면 남의 pivot 규칙을 덮어쓰므로, 판정을 **내가 쓰는 세 파일로 한정**했다. 곰 보스 아트가 자기 규칙을 갖게 되면 그쪽에서 묶음을 하나 더 등록하면 된다.

오브젝트 아트를 쓰는 쪽은 `Game.Spawner.Editor`다(`Game.Animation`·`Game.Animation.Editor`를 참조한다. 반대 방향 참조가 없어 순환이 아니다). 스포너 생성기는 `ObjectArtPostprocessor.TryGetAlphaSizePixels`로 알파 박스 크기를 받아 프리팹 스케일을 계산하고, 스프라이트가 아직 Sprite로 임포트되지 않았으면 `ObjectArtPostprocessor.Reimport`로 강제한다. **`TryGetAlphaSizePixels`도 폴더 묶음을 따른다** — 묶음에 속한 파일은 합집합 크기를 돌려주므로 pivot과 스케일이 같은 박스에서 나온다. 자세한 계산은 `Docs/Spawner.md`의 「프리팹 비주얼」에 있다.

`Tests/ObjectArtImportTests.cs`가 폴더(하위 폴더 포함) 안 모든 PNG에 대해 알파 박스가 잡히는지, 임포트 결과가 Sprite/Single/PPU 100/Tight/밉맵 없음/무압축인지, `spritePivot`이 폴더 규칙이 정한 값과 같은지, 물고기 pivot이 돌·통나무와 다른지를 본다. `Tests/SharedPivotFolderTests.cs`가 묶음 판정·합집합·연어 `.meta` 일치를 본다. 폴더가 비면 `Assert.Ignore`로 넘어간다. 2026-09-06에 넷을 더했다: `SalmonFolder_ListsTwelveFramesIncludingTheHitPose`(12장·`충돌/충돌.png` 포함·`Find`가 연어 묶음을 돌려주는지), `SalmonUnion_MatchesTheRecordedBoxAndPivot`(박스 `x=[755,1282] y=[266,545]`, 528×280, pivot `(0.5307292, 0.37592593)`, `SalmonFallbackPivot`도 같은 값인지), `SalmonUnion_ContainsEveryFrameBox`, `StaleSiblings_AreQueuedOnlyWhenThePivotActuallyDiffers`(맞은 상태에서는 빈 목록, 일부러 0.01 어긋내면 자기 자신을 뺀 11장). `Tests/SalmonClipTableTests.cs`에는 `TableHasFiveClips`(5클립·12프레임), `HitIsOneHeldFrame`, `HitFramePathIsTheDashlessSingleFile`, `FindSingleFrameTakesTheExactName`이 들어갔다.

## 보스 아트 (2026-09-06)

Drive에서 보스 아트 세 장이 왔다. **임포트 설정과 클립까지만 만들어 두었고 프리팹에는 붙이지 않았다** — `Assets/GameAssets/Boss/`와 `Assets/Scripts/Boss/`는 같은 시각에 곰 보스 작업이 돌고 있어 손대지 않기로 했다. 아래가 붙일 때 쓸 값이다.

> **2026-09-06 배선 완료.** 보스 담당이 두 프리팹에 붙였다. 보스 3(물결)은 아래 표 그대로다. 보스 1(낚시바늘)은 **같은 날 배 아트(`Art/오브젝트/배.png`)가 와서 아래 표의 「`HookSprite` `localScale` 0.55 / 콜라이더 0.561 × 0.9295」가 쓰이지 않았다** — 줄 앵커가 수면 위 가상의 점에서 배의 낚싯대 끝으로 바뀌면서 줄 길이가 레인마다 달라졌고, 그래서 스케일을 줄 길이에서 역산하게 됐다(상단 0.375 / 중단 0.474 / 하단 0.584, 콜라이더는 중단 기준 0.484 × 0.802). 실제 값은 `Docs/Boss.md` 「보스 1 낚싯줄」에 있다. 아래 표는 배가 없었을 때의 계산으로 남겨 둔다.

| 파일 | 알파 박스(px) | GUID | pivot | 쓸 곳 |
| --- | --- | --- | --- | --- |
| `Art/보스/낚시바늘.png` | 102 × 956 (전체) / 바늘만 102 × 169 | `4fca2523f07885141bbcaa1480150719` | **`(0.5390625, 0.19305556)`** = 바늘 부분의 중심 | 보스 1 `FishingLineBoss` / `Hook/HookSprite` |
| `Art/보스/물결/물결1.png` | 1887 × 335 (2장 합집합) | `709530025c24efe4680c74ff7ee127ab` | `(0.5085937, 0.36712963)` | 보스 3 `WaterfallBoss` / `Rapid0..2` |
| `Art/보스/물결/물결 2.png` | 1887 × 332 | `85dae4647b71228419663f0416763a8b` | 같음(합집합) | 위와 같음 |

**낚싯바늘.** 한 장에 **줄과 바늘이 같이** 그려져 있다(줄이 캔버스 위 끝부터 y 787까지 폭 2 px, 그 아래가 바늘). 그래서 pivot을 그림 중심이 아니라 **바늘의 중심**에 두어야 `Hook` 오브젝트를 레인 y에 놓았을 때 바늘이 레인에 온다. 알파 박스 중심으로 잡으면 바늘이 레인보다 4 unit 아래로 내려간다. 이 pivot은 계산으로 나오지 않으므로 `BossArtPostprocessor.HookPivot` 상수로 박아 두었다.

붙일 때 값(계산해 둔 것):

| 대상 | 값 |
| --- | --- |
| `HookSprite` `localScale` | **0.55** (균등). 바늘이 0.561 × 0.9295 unit이 되고, 그림 안의 줄이 pivot 위로 4.79 unit 올라가 어느 레인에서든 화면 위(3.6)를 넘어간다 |
| `Hook`의 `BoxCollider2D.m_Size` | **0.561 × 0.9295** (수면 반응용. `WaterInteractor`가 `shape`로 이 콜라이더를 쓴다) |
| `Line` 오브젝트 | **비활성**. 그림이 줄까지 그려 주므로 코드가 그리던 흰 막대(`LineWidth` 0.06)와 겹친다. `FishingLineBoss.UpdateLine`은 비활성 transform을 옮길 뿐이라 코드 변경이 필요 없다 |
| `Rapid0..2` `localScale` | **0.2686567** (균등). 합집합 335 px = 3.35 unit을 레인 띠 높이 0.9에 맞춘 값. 화면 크기 5.07 × 0.90 |
| `Rapid0..2` `SpriteRenderer.m_Color` | 흰색 (아트 원색). 지금은 흰 사각형에 `(0.5, 0.75, 1, 0.6)`을 곱하고 있다 |
| `Rapid0..2` `WaterInteractor.size` | **18.87 × 3.35** (로컬 단위. 균등 스케일이 곱해져 월드 5.07 × 0.90이 된다. 지금 값 0.64 × 0.64는 옛 사각형 스프라이트 기준이다) |
| 애니메이션 | `Boss_Rapid.asset`(아래)을 `WaterfallBoss`에 `[SerializeField] CustomAnimation rapidClip` + `SpriteRenderer[] rapidRenderers`로 받아 `OnBegin`에서 렌더러마다 `AddModule(new SpriteAnimatorModule(...)).Play(rapidClip)`. `Game.Boss.asmdef`에 `Game.Animation` 참조를 더해야 한다(`Game.Animation`은 `Core.Modules`만 참조하므로 순환 없음) |

`Boss_Rapid.asset`(GUID `1d3f8c47a05b4e42a71d6e9c58b3f0d7`)은 `Player_Hit.asset`과 같은 이유로 **YAML을 직접 썼다**(에디터가 열려 있어 생성기를 돌릴 수 없었다). 구조는 `Player_Swim.asset`과 같고 프레임이 물결 2장, fps 6, loop다. `BossClipTable`에 등록해 두었으므로 다음에 `Generate`를 돌려도 프레임 수·슬롯이 맞아 다시 구워지지 않는다.

**보스 아트 `.meta` 세 장은 손으로 썼다.** Unity 기본 임포터가 이 세 장을 `spriteMode: Multiple`로 잡고 자동 슬라이스까지 해 두어(물결1은 7조각) 스프라이트 참조가 `internalID`에 묶이는 상태였다. `통나무.png.meta`를 본으로 Single / PPU 100 / Tight / 밉맵 없음 / 무압축 / 2048 / Custom pivot으로 다시 쓰고 GUID는 Unity가 이미 준 값을 그대로 두었다. 이후로는 `BossArtPostprocessor`가 같은 값을 다시 계산해 덮으므로 결과가 달라지지 않는다. 연어 `충돌.png.meta`와 같은 예외 처리다.

**곰 보스 아트(`Art/보스/-곰-/*.png`)는 건드리지 않았다.** 후처리기 판정도 일부러 그 폴더를 잡지 않는다(위 「후처리기 범위」).

## 임시값

- 자리표시자는 없어졌다. 다섯 클립 모두 실제 아트다.
- `Player_Hit`의 fps 6은 `Player_Swim`을 따라간 값일 뿐이다. 1프레임 원샷이라 재생 길이가 눈에 띄지 않는다. 피격 연출을 몇 초 유지할지는 코어루프가 정하고, 여기서는 포즈만 준다.
- `Player_Swim`의 fps 6은 정한 값이다(2프레임 루프라 0.333초에 한 바퀴). 루프 클립은 한 바퀴마다 `Completed`를 쏘므로 초당 3번이다. 굼떠 보이면 8로 올린다.
- `Player_LaneUp`/`LaneDown`의 fps 10은 2프레임 ÷ 0.2초에서 나온 값이라 duration과 길이가 같다. duration이 이기므로 fps는 참고용이다.
- `Player_Jump`의 `duration` 0.8은 기획서 3번 문서의 「점프 지속 약 0.8초」에서 왔다. 플레이어가 `Play(jump, GameConfig.JumpDuration)`을 쓰면 에셋 값은 참고용이 된다.
- 프레임 이벤트는 아직 어느 클립에도 없다. 사운드 붙일 때 `jump_splash` 같은 id를 점프 클립 1~2프레임에 넣고 `FrameEvent`를 `AudioManager`로 잇는 것이 자연스럽다(`Docs/Requests.md`의 사운드 항목과 같은 결).

## 확신이 없는 지점

- **연어 크기.** 통합에서 `GameConfig.playerScale` 0.29(몸 높이 약 0.81 unit)로 정했다. PPU는 100 그대로. 스포너의 `playerHalfWidth` 0.35와 실제 히트박스 폭 1.33이 어긋나는 점은 `Docs/DesignQuestions.md`에 올렸다.
- **레인 이동 2프레임이 0.2초에 맞는지 모른다.** 프레임당 0.1초라 사실상 시작 포즈 한 장, 기울어진 포즈 한 장이고 도착 포즈는 수영 0프레임이 받는다. 아트가 「1번=기울기 시작, 2번=최대 기울기」로 그렸다고 보고 그대로 넘겼다. 착지 프레임이 따로 있어야 자연스러우면 3프레임을 요청해야 한다.
- **`올라가기`·`내려가기`의 2번 프레임이 `기본`의 2번 프레임과 파일 내용이 같다**(sha256 동일, `점프-1`도 같다). 아트가 「끝나면 기본 자세로 돌아온다」는 뜻으로 넣은 것으로 보고 그대로 썼다. 의도가 다르면 `Docs/DesignQuestions.md`로 올려야 한다.
- **점프 5프레임이 포물선 어디에 대응하는지 모른다.** `JumpModule`은 `4 * JumpHeight * t * (1 - t)` 포물선으로 Y를 직접 움직이고, 애니메이션은 0.8초를 5등분해 균등하게 넘긴다. 3번 프레임이 정점 그림인지 아닌지는 아트가 그렇게 그렸을 때만 맞는다. 어긋나면 프레임 수를 늘리거나 `Play(clip, duration)` 대신 `SetFrame(clip, index)`을 `JumpModule.Progress01`로 직접 몰아야 한다. 후자는 포물선과 완벽히 동기되지만 모듈 하나가 다른 모듈을 매 프레임 읽어야 해서 지금은 하지 않았다.
- **루프 클립의 `Completed` 의미.** 「한 바퀴 뒤 완료하되 계속 재생」이라는 지시대로 만들었는데, 그러면 이벤트가 바퀴마다 계속 발생한다. 수영처럼 상시 도는 클립에 `Completed`를 구독하면 초당 여러 번 불린다. 지금은 그게 맞는 동작이라고 보고 문서에만 적었다.
- **`Stop()`이 프레임을 유지한다**는 지시를 따랐다. 그래서 점프 도중 스킵하면 공중 포즈가 남는다. 컷신 스킵 규약(끝값 즉시 적용)과 다르므로 컷신 쪽에서는 `Stop()` 뒤에 `SetFrame`으로 끝 포즈를 찍어야 한다. 이걸 `Animate` 헬퍼 안에 넣을지는 컷신 소유자가 정한다.
- **아트 `.meta` 수정 범위.** `Tools/sync_drive.py`를 확인했다: sha256이 다르면 PNG만 덮어쓰고 `.meta`는 이동·삭제 때만 건드린다. 새 파일이 오면 `.meta`가 없어 기본 임포트가 되므로 `SalmonArtPostprocessor`가 임포트 시점에 설정을 다시 건다(「통합 절차」). 2026-09-06에 `충돌.png.meta` 한 장만 예외로 손으로 썼다(`Player_Hit.asset`이 GUID를 먼저 필요로 해서다). 앞으로도 이건 예외로 두고, 새 아트가 오면 임포트에 맡긴다.
- **손으로 쓴 `충돌.png.meta`의 `internalID`·`spriteID`·`rect`.** 본으로 삼은 `기본-1`의 값을 그대로 쓸 수 없어 새로 지어 넣었다. `spriteMode`가 Single이라 스프라이트 참조는 고정 `fileID: 21300000`으로 가고 이 값들을 가리키는 곳이 없어서 문제가 없다고 보았지만, Unity가 임포트하며 다시 쓰는지는 에디터를 열어봐야 확인된다.
- **`Play` 무시 조건**을 「같은 클립 + 같은 길이 + 재생 중」으로 했다. `Play(jump, 0.8)`을 매 프레임 불러도 안전하다는 뜻이다. 다만 멈춘 뒤 같은 클립을 다시 넣으면 처음부터 다시 돈다. 「무시」를 재생 여부와 무관하게 볼 수도 있어서 애매하다.
