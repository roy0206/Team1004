# 스테이지 진행 바

Play 화면 상단의 전체 진행 바다. 구간별 진행이 아니라 런 전체 진행을 하나의 바로 보여주고, 보스 위치를 아이콘으로 찍고, 핸들을 연어가 헤엄치는 모습으로 연출한다.

소유자는 moondodo다. 어셈블리는 `Game.StageProgress`, 네임스페이스도 같다.

## 구성

| 경로 | 내용 |
| --- | --- |
| `Assets/Scripts/StageProgress/` | 코드. asmdef는 `Core.Foundation`, `Game.Config`, `Game.Play`, `UnityEngine.UI`를 참조한다 |
| `Assets/GameAssets/StageProgress/StageProgressBar.prefab` | 프리팹 본체 |
| `Assets/GameAssets/StageProgress/*.png` | 스프라이트. `Placeholder` 접두사가 붙은 것은 임시다 |

`Game.Play`는 `Game.StageProgress`를 참조하지 않는다. 의존이 한 방향이라 순환이 없다.

## 붙이는 법

Play 씬 Canvas 아래에 프리팹을 그대로 넣으면 끝이다. 인스펙터 연결은 필요 없다.

`bindPlayFlow`가 켜져 있으면 `StageProgressBar`가 `PlayFlowProgressSource`를 스스로 만들어 매 프레임 `PlayFlow.TryGetCurrent`로 상태를 읽는다. `PlayFlow`가 없는 씬에서는 아무 것도 하지 않고 `manualProgress` 값만 그린다.

기존 `PlayHud.progressFill`은 구간별 진행을 그리는 별개 위젯이다. 둘 다 두면 화면에 바가 두 개 보인다. 정리는 `Docs/Requests.md` 항목을 참고한다.

## 진행값 계산

`StageProgressLayout`이 순수 함수로 담당한다. UI나 씬에 의존하지 않아 테스트할 수 있다.

전체 진행은 지나온 구간들의 길이 합에 현재 구간 경과 시간을 더해 총 길이로 나눈 값이다. 길이는 `GameConfigValues.SectionDurations`에서 온다. 코드에 상수를 두지 않으므로 기획이 구간 수나 길이를 바꾸면 바와 아이콘 위치가 같이 따라간다.

`sectionTime`은 `PlayState.Running`일 때만 흐른다. 컷신과 보스 동안에는 구간 길이에 멈춰 있으므로, 보스 중에는 연어가 정확히 그 보스 아이콘 위에 선다.

기본값 `{25, 30, 35, 15}` 기준 아이콘 위치다.

| 보스 | 구간 | 위치 |
| --- | --- | --- |
| 1 낚싯줄 | 1 끝 | 23.81% |
| 2 바다코끼리 | 2 끝 | 52.38% |
| 3 폭포 | 3 끝 | 85.71% |

마지막 구간 뒤에는 보스가 없으므로 아이콘은 구간 수보다 하나 적다.

## 아이콘 상태

| 상태 | 조건 | 모양 |
| --- | --- | --- |
| Upcoming | 아직 안 왔다 | 흰 원, 남색 아이콘 |
| Engaged | 그 보스와 싸우는 중이다 | 남색 원, 흰 아이콘, 맥박 |
| Cleared | 지나갔다 | 짙은 청록 원, 흰 아이콘 |

판정은 `PlayFlow.Section`과 `PlayFlow.State`로 한다. 구간 번호가 아이콘 번호보다 크면 Cleared, 같고 상태가 `Boss`면 Engaged다.

## 연출

핸들의 연어는 `SwimmingHandle`이 매 프레임 그린다. 2프레임 플립북에 상하 흔들림과 기울기를 겹치고, 바가 실제로 움직이는 속도에 맞춰 애니메이션 속도와 몸통 신축을 올린다. 멈춰 있을 때도 `restRate`만큼은 계속 헤엄친다.

`Time.deltaTime`을 쓰므로 일시정지에서 `Time.timeScale`이 0이 되면 같이 멈춘다.

채워지는 바는 `UIGradient`가 정점 색으로 가로 그라데이션을 넣는다. 패턴 텍스처를 쓰지 않는다. 단색으로 바꾸려면 두 색을 같게 두면 된다.

## 스프라이트

`Pill.png`와 `PillTrack.png`는 9슬라이스용 원이다. 슬라이스 경계가 스프라이트 높이의 절반이라 바를 가로로 늘려도 양 끝이 정확한 반원으로 남는다. 트랙 높이 56에는 56짜리를, 채움 높이 40에는 40짜리를 쓴다. 높이를 바꾸면 그에 맞는 크기의 원을 새로 만들어야 모서리가 찌그러지지 않는다.

`Placeholder`가 붙은 것은 임시 아트다. 교체 요청은 `Docs/Requests.md`에 있다.

## 다른 곳에서 쓰기

`bindPlayFlow`를 끄고 `SetProgress(0~1)`을 호출하면 어디서든 쓸 수 있다. 구간 데이터를 직접 넣으려면 `IStageProgressSource`를 구현해 `SetSource`로 넘긴다.
