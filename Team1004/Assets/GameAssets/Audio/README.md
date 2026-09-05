# Audio

`D:/Unity/Tavern_Gamejam_CAU_SSU`에서 가져온 사운드. 짧은 효과음은 원본 wav와 `.meta`(GUID 유지)를 그대로, 긴 음악·루프는 ogg로 변환했다. 2026-09-05.

## 폴더

| 폴더 | 출처 | 내용 |
| --- | --- | --- |
| `Tavern/` | `Assets/Resources/Sounds/` | 게임잼 때 직접 넣은 11개. 물놀이 게임이라 물 소리가 많다 |
| `CasualMobilePack/SFX/` | Casual & Mobile Music and Sounds Pack Vol 1 | UI 클릭 12종, 코인 3종, 부정 4종, 벨·금고·상자·힌트·열쇠·특수 |
| `CasualMobilePack/Jingles/` | 같은 팩 | Positive 26종, Negative 19종 (ogg) |
| `CasualMobilePack/MusicLoops/` | 같은 팩 | 캐주얼 루프 BGM 27종 (ogg, 원본 357MB라 변환) |
| `8BitMusic/` | 8Bit Music - 062022 (GWriterStudio) | 8비트 BGM 10곡 (ogg, 원본 222MB라 변환) |

## 연어 러너 배정안

| 용도 | 1순위 | 대안 | 비고 |
| --- | --- | --- | --- |
| 물속 헤엄 루프 | `Tavern/Swiming.ogg` (70초) | | Tavern에서 물속 상태 동안 `PlaySound(..., loop)`로 계속 틀던 소리 |
| 대시 | `Tavern/Dashing.wav` (2.2초) | | 앞부분만 쓰려면 AudioSource로 잘라서 |
| 수면 진입·착수 | `Tavern/Diving.wav` (1.7초) | | Tavern은 낙하 속도에 비례해 볼륨을 줬다 (`linearVelocityY / 3`) |
| 곰·새에게 잡힘 | `Tavern/Bite.mp3` (0.7초) | `Tavern/Stab.wav` | |
| 바위·가시 충돌 | `Tavern/Stab.wav` (0.8초) | `CasualMobilePack/SFX/Negative 2-1.wav` | |
| 사망 | `Tavern/Dead.wav` (2.8초) | `Jingles/Negative Retro 01.ogg` | |
| 보트·프로펠러 장애물 | `Tavern/Motor.ogg` (26초 루프) | | 기획에 모터 장애물이 없으면 안 쓴다 |
| 아이템 획득 | `SFX/Coin 9-1.wav` (0.6초) | `Coin 3-1`, `Coin 5-1`, `Bells 1-1` | |
| 스테이지 클리어 | `Jingles/Positive 05.ogg` | Positive 시리즈 아무거나 | 들어보고 정한다 |
| 게임 오버 | `Jingles/Negative 05.ogg` | Negative 시리즈 | |
| UI 클릭 | `SFX/Interface Click 1-1.wav` | Interface Click 시리즈 | |
| 타이틀 BGM | `Tavern/StartBGM.ogg` (67초) | `MusicLoops/Casual Music Loop 01.ogg` | |
| 인게임 BGM | `Tavern/BGM.mp3` (140초) | `Tavern/bgm queue1.mp3`, `8BitMusic/` | 속도감이 부족하면 8Bit 트랙 |

강물 앰비언스는 Tavern에 없다. `Swiming.ogg`를 낮은 볼륨으로 깔거나 크롤러(`D:/Coding/GameSoundCrawler/team1004`)에서 다시 찾는다.

## 라이선스

- `Tavern/`: 팀원이 게임잼에서 만든 것(git 최초 커밋자 roy0206, YUJUN SHIN). 출처는 Tavern 저장소 이력을 따른다.
- `CasualMobilePack/`, `8BitMusic/`: 에셋 스토어 팩. 구매자 계정의 프로젝트에서만 쓸 수 있으니 구매자가 팀원인지 확인한다. 팩 안에 별도 라이선스 문서는 없었고 8Bit는 `8BitMusic/Notes.txt`에 작가 연락처만 있다.

## 변환

ogg는 ffmpeg libvorbis로 만들었다. 루프(`Swiming`, `Motor`, `MusicLoops`)는 wav→ogg라 이음새가 유지된다. 원본 wav가 필요하면 Tavern 저장소에서 가져온다.
