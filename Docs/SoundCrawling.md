# 사운드 후보 고르기

게임에 넣을 소리를 무료·상업 이용 가능 음원에서 찾는 일이다. 두 사람이 나눠 맡는다.

- **팀원(선택 담당)**: 브라우저로 페이지 하나를 열고 듣고 고른다. **Python도 API 키도 설치할 것도 없다.**
- **소유자(크롤링 담당, roy0206)**: 자기 기계에서 후보를 긁고 페이지를 만들고, 팀원이 고른 것을 받아 프로젝트에 넣는다.

크롤러는 `D:/Coding/GameSoundCrawler`에 있는 개인 도구라 저장소에 없다. 저장소에 오는 것은 결과물뿐이다.

| 저장소 파일 | 내용 |
| --- | --- |
| `Docs/Sound/audition.html` | 듣고 고르는 페이지. **팀원이 열 파일** |
| `Docs/Sound/candidates.yaml` | 페이지에 박힌 후보 목록 원본(참고용) |
| `Docs/Sound/picks.yaml` | 팀원이 고른 결과. 팀원이 커밋하거나 소유자가 받아서 넣는다 |

---

## 팀원 (선택 담당)

### 1. 페이지 열기

```
git pull
```

`Docs/Sound/audition.html`을 더블클릭한다. 서버도 빌드도 필요 없고 브라우저가 바로 연다.

**인터넷은 필요하다.** 아직 아무 파일도 내려받지 않았기 때문에 후보 재생은 Freesound·Openverse·Incompetech의 원격 미리듣기를 스트리밍한다.

### 2. 듣기

소리 하나가 한 덩어리다. 맨 위에 id(예: `bear_paw_slam`)와 종류(`sfx`/`ui`/`ambience`/`bgm`), 루프 여부가 있고, 그 아래 회색 글씨가 **이 소리가 게임 어디서 나는지**에 대한 설명이다. 그 설명에 맞는 후보를 고른다.

후보 카드에는 재생기와 함께 **길이 · 라이선스(CC0/CC BY 등) · 저작자 · 크레딧 필요 여부**가 붙어 있다. 「자세히」를 펼치면 태그·설명·출처 링크·크레딧 문구가 나온다.

- 재생기는 한 번에 하나만 소리 난다(다른 것을 누르면 앞엣것이 멈춘다).
- 카드 색: 왼쪽 띠가 초록이면 크레딧 없이 쓸 수 있고(CC0), 노랑이면 CC BY라 크레딧을 남겨야 한다. **둘 다 써도 된다.** 상업 이용 불가(NC) 음원은 애초에 목록에 오르지 않는다.

### 3. 고르기

- 카드 왼쪽 「선택」 라디오를 누른다. **소리마다 하나만** 고를 수 있다.
- 마음에 드는 게 하나도 없으면 「선택 안 함」을 고르고 **메모 칸에 이유를 적는다**("전부 너무 길다", "물소리 말고 나무 부러지는 소리였으면" 등). 그 소리는 소유자가 검색어를 바꿔 다시 긁는다.
- 메모 칸은 비워도 된다. 적으면 `picks.yaml`의 `note`로 그대로 나간다.
- 선택과 메모는 브라우저에 자동 저장된다. 창을 닫았다 다시 열어도 남아 있다. 다만 **같은 브라우저·같은 기계에서만** 남는다(시크릿 창이나 다른 PC에서 열면 처음부터다).
- 하단 왼쪽에 `선택 n / 30`으로 진행 상황이 보인다.

### 4. 내보내서 보내기

1. 하단 **「선택 내보내기 (picks.yaml)」** 버튼을 누른다. 아래 상자에 `picks.yaml` 내용이 그대로 만들어진다.
2. **「복사」**를 눌러 통째로 복사하거나 **「다운로드」**로 `picks.yaml` 파일을 받는다.
3. 둘 중 하나로 소유자에게 전달한다.
   - 채팅으로 텍스트를 붙여넣기, 또는
   - `Docs/Sound/picks.yaml`에 저장하고 커밋한다(`사운드: 후보 선택` 같은 메시지면 된다).

「전부 지우기」는 선택과 메모를 **모두** 지운다. 확인 창이 한 번 뜬다.

### 팀원이 하지 않는 것

- 오디오 파일을 직접 받아서 저장소에 넣지 않는다. 내려받기·라이선스 장부·변환은 소유자 쪽 도구가 한꺼번에 한다.
- `Team1004/Assets/` 아래는 건드리지 않는다. 이 작업은 `Docs/`만 쓴다.

---

## 소유자 (크롤링 담당)

작업 폴더는 `D:/Coding/GameSoundCrawler`이고 이 게임의 워크스페이스는 그 아래 `team1004/`다.

```
D:/Coding/GameSoundCrawler
  soundcrawler/          도구
  team1004/
    brief.yaml           필요한 소리 목록 (여기만 손으로 고친다)
    candidates.yaml      search 결과
    picks.yaml           고른 것
    audition.html        듣기 페이지
    library/             받은 파일 + ledger.yaml. 저장소에 넣지 않는다
```

Freesound 키는 User 환경변수 `FREESOUND_API_KEY`다. 새 기계에서는 <https://freesound.org/apiv2/apply/>에서 받아서 한 번 넣는다.

```powershell
[Environment]::SetEnvironmentVariable('FREESOUND_API_KEY','<받은 키>','User')
```

### 1. 후보 긁기

```powershell
cd D:\Coding\GameSoundCrawler
.\.venv\Scripts\python.exe -m soundcrawler --root team1004 search --per 5
```

- **검색어는 두 단어까지만 쓴다.** Freesound 텍스트 검색은 단어를 사실상 AND로 묶어서, `fast whoosh attack swipe` 같은 네 단어는 결과가 0이 된다(2026-09-06 실측). 구체적인 조건은 `query`가 아니라 `tags`·`duration`에 적는다. 그러면 점수 계산이 알아서 걸러 준다.
- `bgm`은 Incompetech를 쓰므로 `query`를 비우고 `feel`(Calming, Driving, Intense … 22종)로 고른다.
- Openverse는 응답이 멈추는 날이 있다. 30초 타임아웃 2회 뒤 그 소리만 건너뛰고 넘어간다. 그대로 두면 된다.
- 일부만 다시 긁을 때는 `--only <id> <id>`.

### 2. 페이지 만들기

```powershell
.\.venv\Scripts\python.exe -m soundcrawler --root team1004 audition --title "초지일관 사운드 후보"
copy team1004\audition.html   D:\Unity\Team1004\Docs\Sound\audition.html
copy team1004\candidates.yaml D:\Unity\Team1004\Docs\Sound\candidates.yaml
```

`--title`을 빼면 제목이 `초지일관 사운드 오디션`이 된다. 두 파일을 커밋하면 팀원이 pull해서 연다.

**이미 `fetch`한 소리가 있으면 그 후보만 `library/…` 로컬 경로로 렌더된다.** 그 페이지를 남에게 보내면 그 재생기만 소리가 안 난다. 팀원에게 보낼 판을 만들 때는 `library/ledger.yaml`이 없는 상태(예전 것은 `library_prev/`로 옮겨 뒀다)에서 렌더한다. 지금 판은 150개 전부 원격 프리뷰다.

### 3. 팀원 선택 받기

팀원이 보낸 텍스트를 `team1004/picks.yaml`에 **통째로 덮어쓴다**. 페이지가 내보내는 형식이 크롤러가 읽는 형식과 같다.

```yaml
version: 1
updated_at: "2026-09-06T21:10:00+09:00"
sounds:
  bear_paw_slam:
    ref: "freesound:123456"
    by: human
    note: "묵직하고 물이 섞여 있다"
    at: "2026-09-06T21:10:00+09:00"
```

`by: human` 픽은 도구가 덮어쓰지 않는다. 덮어쓰기가 싫으면 한 줄씩 `pick`으로 넣어도 된다.

```powershell
.\.venv\Scripts\python.exe -m soundcrawler --root team1004 pick bear_paw_slam "freesound:123456" --by human --note "묵직하다"
```

`status`로 소리별 단계(`new` → `searched` → `picked` → `fetched`)를 본다.

### 4. 받기 · 라이선스 확인

```powershell
.\.venv\Scripts\python.exe -m soundcrawler --root team1004 fetch
.\.venv\Scripts\python.exe -m soundcrawler --root team1004 gate --credits
```

`fetch`는 파일과 함께 `library/ledger.yaml`(출처 URL·라이선스·크레딧 문구·sha256·취득 시각)과 출처 페이지 스냅샷을 남긴다. `gate`는 배포 가능 여부를 판정한다.

| 판정 | 뜻 |
| --- | --- |
| NC (상업 이용 불가) | **차단.** 검색 단계에서 이미 걸러지므로 나오면 안 된다 |
| ND (2차 저작 불가) | **차단.** 자르기·루프가 개작이라 못 쓴다 |
| 라이선스 미상 | **차단** |
| SA (동일 조건 변경 허락) | 경고. 쓸 수는 있으나 조건이 번진다 |
| CC BY | 크레딧 필요. `CREDITS.md`에 자동 수록된다 |

### 5. 프로젝트로 내보내기

```powershell
.\.venv\Scripts\python.exe -m soundcrawler --root team1004 export `
  --to D:\Unity\Team1004\Team1004\Assets\GameAssets\Audio\Resources\Audio --format ogg --trim
```

`<id>.ogg` 파일들과 `CREDITS.md`, `sounds_manifest.json`이 그 폴더에 생긴다. 게이트가 막으면 멈춘다(`--force`는 쓰지 않는다).

**`CREDITS.md`는 지우지 않는다.** CC BY 음원을 쓰는 한 이 파일이 라이선스 이행 증거다. 빌드에 포함되지 않아도 저장소에 남긴다.

### 6. id 등록

`Team1004/Assets/Scripts/Bootstrap/AudioManifest.json`의 `sounds` 배열에 한 줄 넣는다. `loadKey`는 `Audio/` + id를 PascalCase로 바꾼 것이고, `Resources/Audio/` 아래 실제 파일 이름과 맞아야 한다.

```json
{
  "id": "bear_paw_slam",
  "loadKey": "Audio/BearPawSlam"
}
```

크롤러가 내보내는 파일 이름은 id 그대로(`bear_paw_slam.ogg`)이므로, 매니페스트의 `loadKey`에 맞춰 파일 이름을 바꾸거나 `loadKey`를 `Audio/bear_paw_slam`으로 적는다. 기존 14개는 PascalCase 쪽으로 되어 있다.

재생하는 곳:

| 재생 지점 | id |
| --- | --- |
| `Play/PlayFlow.cs` | `bgm_play`, `lane_move`, `jump`, `land`, `hit`, `stage_clear`, `stage_fail`, `boss_clear`, `cutscene_transition`, `ending` |
| `Title/TitleScreen.cs` | `bgm_title` |
| `Settings/UiSound.cs` | `ui_click` |
| `Boss/PatternBoss.cs` | `boss_telegraph`, `boss_attack` |
| 아직 부르는 곳이 없음 | `bgm_boss1~3`, `bgm_ending`, `amb_underwater`, `wall_crash`, `hook_sweep`, `bear_paw_slam`, `bear_paw_sweep`, `bear_shadow_appear`, `rapids_rush`, `waterfall_final`, `qte_press`, `qte_success`, `qte_fail`, `ledge_approach` |

아래 신규 id를 부를 자리는 각각 `Docs/Qte.md`(수락·완료·실패), `Docs/Ledge.md`(`Approaching`·QTE 실패 벽 충돌), `Docs/Boss.md`(보스별 패턴)에 「사운드가 없다」로 적혀 있다. 소리가 실제로 들어온 뒤에 그쪽 소유자와 붙인다.

### 하지 말 것

- **`team1004/library/`를 저장소에 넣지 않는다.** 받은 음원 원본이고 용량이 크며, 프로젝트에 들어가는 것은 `export` 결과뿐이다.
- `--force`로 게이트를 뚫지 않는다.
- 라이선스를 짐작해서 적지 않는다. 판독 불가는 `NOASSERTION`으로 남고 게이트가 막는다.

---

## 소리 목록 (30개)

`brief.yaml` 기준이다. 「신규」는 `AudioManifest.json`에 아직 없는 id다.

| id | 종류 | 신규 | 쓰이는 곳 |
| --- | --- | --- | --- |
| `bgm_title` | bgm | | 타이틀 화면 |
| `bgm_play` | bgm | | 일반 진행 구간 1~4 |
| `bgm_boss1` | bgm | ○ | 보스 1 낚싯줄. 조여 오는 긴장 |
| `bgm_boss2` | bgm | ○ | 보스 2 곰. 묵직하고 위압적 |
| `bgm_boss3` | bgm | ○ | 보스 3 폭포. 몰아치는 급류 |
| `bgm_ending` | bgm | ○ | 엔딩. 감정적으로 |
| `amb_underwater` | ambience | ○ | 물속 흐름 루프. 내내 낮게 깔린다 |
| `ui_click` | ui | | 메뉴 버튼 |
| `lane_move` | sfx | | 위아래 레인 이동 |
| `jump` | sfx | | 수면 위로 점프 |
| `land` | sfx | | 착수. 묵직한 첨벙 |
| `hit` | sfx | | 피격 |
| `wall_crash` | sfx | ○ | 단차 QTE 실패. 긴 돌에 정면 충돌 |
| `boss_telegraph` | sfx | | 빨간 히트박스 예고 |
| `boss_attack` | sfx | | 보스 공격 공통 |
| `hook_sweep` | sfx | ○ | 보스 1 낚싯줄이 레인을 훑음 |
| `bear_paw_slam` | sfx | ○ | 보스 2 곰 팔 내리꽂기 |
| `bear_paw_sweep` | sfx | ○ | 보스 2 곰 팔 옆면 훑기 |
| `bear_shadow_appear` | sfx | ○ | 보스 2 진입. 곰 그림자. 낮은 울림 |
| `rapids_rush` | sfx | ○ | 보스 3 강한 물줄기 |
| `waterfall_final` | sfx | ○ | 마지막 큰 폭포 |
| `qte_press` | ui | ○ | QTE 입력 1회 수락. 6회 반복 |
| `qte_success` | ui | ○ | QTE 완료 |
| `qte_fail` | ui | ○ | QTE 시간 초과 |
| `ledge_approach` | sfx | ○ | 단차 접근 2초 |
| `boss_clear` | sfx | | 보스 30초 버티기 성공 |
| `stage_clear` | sfx | | 구간 클리어 |
| `stage_fail` | sfx | | 실패 |
| `cutscene_transition` | ui | | 구간 → 컷신 → 보스 전환 |
| `ending` | sfx | | 엔딩 진입 징글 |

기존 14개 id는 지금 `Assets/GameAssets/Audio/`의 게임잼·에셋 스토어 음원으로 임시로 채워져 있다(`Assets/GameAssets/Audio/README.md`). 여기서 고른 것으로 바꿀지는 들어 보고 정한다. 에셋 스토어 팩은 구매자 계정에 묶여 있어 라이선스가 깔끔하지 않으므로, 가능하면 CC0/CC BY 쪽으로 옮기는 편이 낫다.
