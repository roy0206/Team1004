# 대사

`Game.Dialogue`는 게임의 모든 대사 문장을 CSV 한 장으로 모아 두고 id로 꺼내 쓰는 모듈이다. 기획서 3판 20절이 "정확한 문장은 다음 기획 단계에서 확정한다"고 했으므로, 문장이 바뀔 때 코드를 건드리지 않도록 대사를 데이터로 분리했다. 씬 오브젝트가 없어 `MonoThing`·`Module`·`Singleton`을 쓰지 않고 순수 클래스 3개와 정적 서비스 1개로 되어 있다.

- 경로: `Assets/Scripts/Dialogue/`, 에디터: `Assets/Scripts/Dialogue/Editor/`, 테스트: `Assets/Scripts/Dialogue/Tests/`
- asmdef: `Game.Dialogue`(참조 없음), `Game.Dialogue.Editor`(참조 `Game.Dialogue`, `Game.Cutscene`, Editor 전용), `Game.Dialogue.Tests`(EditMode)
- 네임스페이스: `Game.Dialogue`, `Game.Dialogue.Editor`, `Game.Dialogue.Tests`
- CSV 정본: `Assets/GameAssets/Design/Dialogue/dialogue.csv`
- `Game.Cutscene`이 `Game.Dialogue`를 참조한다. 반대 방향은 없다.

## 파일

| 파일 | 타입 | 역할 |
| --- | --- | --- |
| `CsvParser.cs` | static, 순수 C# | RFC 4180 CSV 파서. 문자열 → `List<string[]>` |
| `DialogueLine.cs` | 불변 클래스 | 대사 한 줄. `Id`, `Speaker`, `Text`, `AutoAdvanceDelay`, `CharsPerSecond` |
| `DialogueTable.cs` | 순수 C# | CSV 텍스트 → id 사전. 헤더 이름으로 열을 찾고 경고를 모은다 |
| `DialogueService.cs` | static (UnityEngine 의존) | 런타임 전역 접근점. `TextAsset` 로드, `Get`/`TryGet`, 대체 줄 |
| `Editor/DialogueCsvTools.cs` | 에디터 | 메뉴 `Team1004/Import Dialogue CSV`, `Team1004/Validate Dialogue CSV` |
| `Tests/CsvParserTests.cs` 외 2개 | EditMode 테스트 | 파서·테이블·서비스 |

UnityEngine 의존은 `DialogueService.cs` 한 파일에만 있다. 파서와 테이블은 순수 C#이라 에디터 도구와 테스트에서 그대로 쓴다.

## CSV 형식 (기획용)

`Assets/GameAssets/Design/Dialogue/dialogue.csv`. 첫 줄은 반드시 헤더다.

```
id,speaker,text,auto_advance,cps
intro.01,연이 (어린 시절),여기는 기억해 둬야겠다.,1,
intro.03,연이,…여기다.,,
ending.04,,"처음 품은 뜻을, 끝까지.
初志一貫",,
```

| 열 | 필수 | 뜻 | 비우면 |
| --- | --- | --- | --- |
| `id` | 필수 | 대사를 가리키는 이름. 컷신이 이 값을 참조한다 | 그 행을 건너뛰고 경고 |
| `speaker` | 선택 | 화자 이름. 패널 위쪽에 그대로 나온다 | 화자 없음(내레이션·자막) |
| `text` | 필수 | 본문. 줄바꿈은 큰따옴표로 감싼 실제 줄바꿈 | 빈 대사(경고 없음) |
| `auto_advance` | 선택 | 타자가 끝난 뒤 몇 초 있다가 자동으로 넘어갈지(초). `0`이면 즉시 | 비면 `-1`, 즉 Space를 누를 때까지 기다린다 |
| `cps` | 선택 | 초당 글자 수(타자 속도) | 비면 `0`, `CutsceneBase.DefaultCharsPerSecond`(30)를 쓴다 |

작성 규칙:

- 열 순서는 자유다. 헤더 이름으로 찾으며 대소문자·공백·`_`·`-`를 무시한다(`Auto Advance`, `AUTOADVANCE`, `auto_advance` 모두 같다).
- 셀에 쉼표·큰따옴표·줄바꿈이 들어가면 그 셀 전체를 큰따옴표로 감싼다. 셀 안의 큰따옴표는 `""`로 두 번 쓴다.
- 빈 줄은 무시한다. 구간마다 빈 줄을 넣어 나눠도 된다.
- 저장은 UTF-8. BOM이 있어도 없어도 읽힌다(엑셀이 붙이는 BOM은 자동으로 건너뛴다). 정본 파일은 BOM 없이 저장한다.
- 같은 `id`가 두 번 나오면 **마지막 행**을 쓰고 경고를 남긴다. `Validate Dialogue CSV`로 확인한다.
- 숫자 열은 `1`, `1.2`처럼 마침표 소수점만 쓴다. 읽지 못하면 기본값으로 두고 경고한다.

엑셀·Google Sheets로 편집한 뒤 "CSV(쉼표로 분리)"로 내보내면 위 규칙을 자동으로 지킨다.

## id 규칙

`<컷신 id>.<두 자리 순번>`. 컷신 id는 `CutsceneBase.Id`(= `CutsceneCatalog`의 상수)와 같다.

| 컷신 클래스 | 컷신 id | 대사 id |
| --- | --- | --- |
| `IntroCutscene` | `intro` | `intro.01` ~ `intro.06` |
| `FishingLineCutscene` | `cutscene1` | `cutscene1.01` ~ `cutscene1.04` |
| `WalrusCutscene` | `cutscene2` | `cutscene2.01` ~ `cutscene2.04` |
| `WaterfallCutscene` | `cutscene3` | `cutscene3.01` ~ `cutscene3.03` |
| `EndingCutscene` | `ending` | `ending.01` ~ `ending.04` |

중간에 대사를 끼워 넣을 때는 뒤 번호를 밀지 말고 `intro.045`처럼 늘리거나, 컷신 클래스의 `Run()` 순서만 바꾸고 id는 그대로 둔다. id는 컷신 클래스에 문자열 상수로 박혀 있으므로 **이미 쓰이는 id를 바꾸면 그 대사가 화면에서 `[intro.01]`처럼 보인다.** 바꿔야 하면 `Validate Dialogue CSV`를 돌려 끊어진 참조를 먼저 확인한다.

컷신 밖(튜토리얼·힌트 등)에서 쓸 대사는 `tutorial.01`, `hint.jump`처럼 앞자리를 다르게 둔다. 이 경우 `Validate`의 「안 쓰는 id」 목록에 뜨는 것은 정상이다.

## Drive → 프로젝트 절차

1. 기획이 Drive의 `Design/` 폴더에 `dialogue.csv`(또는 이름에 `dialogue`나 `대사`가 든 csv)를 올린다.
2. 세션 시작 훅 또는 `python Tools/sync_drive.py`가 그것을 `Team1004/Assets/Documents/` 아래로 미러링한다(`Tools/drive_sync.json`의 `기획서` 항목이 `.csv`를 이미 포함한다). `Assets/Documents/`는 읽기 전용 미러라 여기 파일을 직접 고치지 않는다.
3. Unity 메뉴 `Team1004/Import Dialogue CSV`를 누른다. `Assets/Documents/` 아래를 재귀로 뒤져 후보 csv를 찾고, 여러 개면 **가장 최근에 수정된 것**을 골라 `Assets/GameAssets/Design/Dialogue/dialogue.csv`로 복사한다. 고른 경로와 후보 목록을 콘솔에 남기고 이어서 검증까지 돌린다.
4. 컷신 재생에 반영하려면 그 CSV가 Bootstrap의 `dialogueCsv`에 연결되어 있어야 한다(아래 「Bootstrap 초기화」).

정본은 어디까지나 Drive이고, `Assets/GameAssets/Design/Dialogue/dialogue.csv`는 게임이 읽는 사본이다. 급할 때 사본을 직접 고쳐도 되지만 다음 Import에서 덮어써진다.

## 런타임 API

```csharp
public static class DialogueService
{
    public static bool IsLoaded { get; }
    public static int Count { get; }
    public static IReadOnlyList<string> Warnings { get; }
    public static IReadOnlyCollection<string> Ids { get; }

    public static void Load(TextAsset asset);
    public static void Load(string csvText);
    public static void Reload(TextAsset asset);
    public static void Reload(string csvText);
    public static void Unload();
    public static bool TryGet(string id, out DialogueLine line);
    public static DialogueLine Get(string id);
}
```

| 멤버 | 동작 |
| --- | --- |
| `Load` | CSV를 파싱해 테이블을 만든다. 파싱 경고는 전부 `Debug.LogWarning`으로 나온다. `TextAsset`이 `null`이면 경고만 남기고 아무것도 하지 않는다 |
| `Reload` | `Unload` 후 `Load`. "없는 id" 경고 기록도 지워지므로 CSV를 다시 넣은 뒤 경고를 다시 보고 싶을 때 쓴다 |
| `TryGet` | 있으면 `true`. 로드 전이면 항상 `false` |
| `Get` | 없으면 **id당 한 번** 경고하고 `Text`가 `"[id]"`인 대체 줄을 돌려준다. 빠진 대사가 화면에 눈에 띄게 남는다 |
| `Warnings` | 마지막 로드의 파싱 경고 목록. 로드 전이면 빈 목록 |

`[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`으로 정적 상태를 비운다. 에디터에서 Domain Reload를 꺼도 이전 플레이의 테이블이 남지 않는다.

`DialogueLine`은 불변이다. `WaitForAdvance`(= `-1f`) 상수와 `WaitsForAdvance` 프로퍼티가 있다.

```csharp
public sealed class DialogueLine
{
    public const float WaitForAdvance = -1f;
    public string Id { get; }
    public string Speaker { get; }
    public string Text { get; }
    public float AutoAdvanceDelay { get; }
    public float CharsPerSecond { get; }
    public bool WaitsForAdvance { get; }
    public static DialogueLine Missing(string id);
}
```

`DialogueTable`과 `CsvParser`는 서비스 없이도 쓸 수 있다. `DialogueTable.Parse(csvText)` → `Count`, `Warnings`, `Ids`, `TryGet`. `CsvParser.Parse(text)`는 헤더 해석 없이 셀 배열의 행 목록만 돌려준다.

## Bootstrap 초기화

`Assets/Scripts/Bootstrap/GameBootstrap.cs`는 코어루프 담당 소유라 직접 고치지 않았다. `Docs/Requests.md`에 요청을 남겼다. 아래 두 줄을 넣으면 된다.

```csharp
[SerializeField] private TextAsset dialogueCsv;
```

```csharp
GameConfig.Load(configAsset);
DialogueService.Load(dialogueCsv);
await SettingsService.LoadAsync();
```

- `Game.Bootstrap.asmdef` 참조에 `Game.Dialogue`를 추가하고 파일 위에 `using Game.Dialogue;`를 넣는다.
- Bootstrap 씬의 `GameBootstrap` 오브젝트 `dialogueCsv`에 `Assets/GameAssets/Design/Dialogue/dialogue.csv`를 끌어다 넣는다.
- 위치는 `GameConfig.Load` 근처면 어디든 된다. 비동기가 아니고 파일 I/O도 없다(TextAsset은 이미 메모리에 있다). 대사 21줄짜리 현재 CSV 기준 파싱은 즉시 끝난다.
- 연결을 잊으면 컷신 대사가 전부 `[intro.01]`처럼 보이고 콘솔에 `[DialogueService] Dialogue table is not loaded.` 경고가 뜬다. 컷신 재생 자체는 막히지 않는다.

## 컷신 연결

컷신은 컷신별 C# 클래스다(`Docs/Cutscene.md`). 대사는 베이스 헬퍼 `Say`로 낸다.

```csharp
await Say("intro.01");
await Together(CameraTo(offset, 0f, 1.2f, Ease.InOutSine, true), Say("intro.04"));
```

각 컷신 클래스는 자기가 쓰는 id를 `LineIds`로 노출하고 `Validate Dialogue CSV`가 이것을 읽는다.

컷신은 `CutsceneBase.Say(lineId)`로만 대사를 낸다. `Say`가 `DialogueService.Get(lineId)`로 줄을 가져와 아래대로 쓴다.

| 값 | 우선순위 |
| --- | --- |
| `speaker` | **항상 테이블**. 테이블이 비어 있으면 화자 없음으로 표시한다 |
| `text` | **항상 테이블** |
| `auto_advance` | 테이블 값이 `0` 이상이면 그 초 뒤 자동 진행, 비어 있으면(`-1`) Space 대기 |
| `cps` | 테이블 값이 `0`보다 크면 테이블, 아니면 `CutsceneBase.DefaultCharsPerSecond`(30) |

화자·본문·자동 진행·속도는 전부 기획 소관이므로 CSV가 정본이다. 코드에는 대사 문자열 리터럴이 없다. 대사를 고치면 CSV만 고치면 되고 컴파일도 생성기 실행도 필요 없다(연출 순서를 바꿀 때만 컷신 클래스를 고친다).

## 검증 도구

메뉴 `Team1004/Validate Dialogue CSV`는 콘솔에 아래 표를 한 덩어리로 찍는다.

1. **파싱 경고** — 중복 id, 빈 id, 숫자로 못 읽은 값, 없는 열.
2. **컷신이 참조하지만 CSV에 없는 id** — 어떤 컷신이 쓰는지 함께 표시. 하나라도 있으면 `LogError`(콘솔에 빨간 줄).
3. **CSV에 있지만 아무도 안 쓰는 id** — 오타이거나, 아직 안 붙였거나, 컷신 밖에서 쓰는 id다.

`CutsceneCatalog.CreateAll`로 등록된 컷신을 전부 만들어 각 인스턴스의 `LineIds`를 모은다(예전에는 `AssetDatabase.FindAssets`로 `CutsceneAsset`을 훑었다). 그래서 **`Say`를 새로 넣으면 그 컷신 클래스의 `LineIds`에도 넣어야** 검증에 잡힌다. `Import Dialogue CSV`도 끝에 이 검증을 한 번 돌린다.

## 테스트

`Assets/Scripts/Dialogue/Tests/`, EditMode. Test Runner에서 `Game.Dialogue.Tests`로 보인다.

| 파일 | 다루는 것 |
| --- | --- |
| `CsvParserTests.cs` | 따옴표 감싸기, 셀 안 쉼표·줄바꿈, `""` 이스케이프, CRLF, BOM, 빈 줄 무시, 마지막 줄 개행 없음, 빈 셀 보존 |
| `DialogueTableTests.cs` | 열 순서 무관, 헤더 대소문자·공백·`_` 무시, 없는 열 기본값, 빈 `auto_advance`/`cps` 기본값, 중복 id 경고, 빈 id 건너뛰기, 숫자 파싱 실패 경고 |
| `DialogueServiceTests.cs` | 로드 전 상태, 로드, 없는 id의 대체 줄, 로드 전 `Get`, `Reload`가 테이블을 교체하는지 |

## 컴파일 검증

컷신 교체 작업에서 배치로 다시 확인했다: `-batchmode -quit` 컴파일 `error CS` 0, `-runTests -testPlatform EditMode` 119/119 통과(`Game.Dialogue.Tests` 포함), PlayMode 스모크 1/1 통과.

## 확신이 없는 지점

- `auto_advance`가 비면 Space 대기다. 컷신 쪽에 기본값을 두는 선택지는 없앴다(스텝이 사라져서 담을 곳이 없다). 자동 진행을 원하면 CSV에 초를 적는다.
- CSV를 어느 시점에 로드할지는 Bootstrap 담당의 판단에 맡겼다. 컷신 재생 직전에 로드해도 되지만, 대사는 타이틀·설정 화면에서도 쓰일 수 있어 Bootstrap 1회 로드를 권한다.
- `Import Dialogue CSV`는 파일 이름만 보고 후보를 고른다. Drive에 대사가 아닌 `dialogue_notes.csv` 같은 파일이 올라오면 잘못 고를 수 있다. 콘솔에 후보를 전부 찍으므로 확인할 수 있다.
- 대사 문장 자체는 기획서 2판의 것 그대로다. 3판 20절이 "다음 기획 단계에서 확정"이라 했으므로 CSV의 문장은 전부 임시값이다.
