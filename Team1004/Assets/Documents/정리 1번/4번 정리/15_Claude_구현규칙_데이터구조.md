# Claude 구현 규칙 / 데이터 구조

## 최우선 목표
> 시작부터 엔딩까지 한 번 완주 가능한 상태를 먼저 만든다.

## CONFIG 권장
```js
const CONFIG = {
  laneMoveDuration: 0.2,
  jumpDuration: 0.8,
  jumpCooldown: 1.2,

  bossDuration: 30,
  bossTelegraph: 0.8,
  bossFullLaneTelegraph: 1.0,
  bossAttackDuration: 0.6,
  bossRestDuration: 0.6,

  section1Duration: 25,
  section2Duration: 30,
  section3Duration: 35,
  section4Duration: 15
};
```

## BOSS_DATA 권장
```js
const BOSS_DATA = {
  fishingLine: {
    duration: 30,
    patterns: [[1], [2], [3]]
  },
  walrus: {
    duration: 30,
    patterns: [[1], [2], [3], [1,2], [2,3]]
  },
  waterfall: {
    duration: 30,
    patterns: [[1], [2], [3], [1,2], [2,3], [1,2,3]]
  }
};
```

## STORY_DATA
스토리 최종 확정 전이므로 코드와 분리.

```js
const STORY_DATA = {
  intro: [],
  cutscene1: [],
  cutscene2: [],
  cutscene3: [],
  ending: []
};
```

## 반드시 지킬 규칙
1. 3레인 유지
2. 가로 조작 추가 금지
3. ↑/↓ 기반
4. 1번 레인에서 ↑ = 점프
5. Space 점프 금지
6. 보스 HP 없음
7. 플레이어 공격 없음
8. 모든 보스 30초
9. 모든 보스 공격 전 히트박스
10. 낚싯줄 = 1 / 2 / 3
11. 바다코끼리 = 1 / 2 / 3 / 12 / 23
12. 폭포 = 1 / 2 / 3 / 12 / 23 / 123
13. 평소 물 흐름 동일
14. 일반 구간 물살 강화 금지
15. 3보스 공격 순간에만 강한 물줄기
16. 일반 장애물 = 돌 / 다른 물고기 / 통나무
17. 한 번 충돌 = 실패
18. 일반 사망 = 현재 일반 구간 처음
19. 보스 사망 = 해당 보스 처음
20. 보스 재도전 시 직전 컷신 생략
21. 3보스 이후 게임 진행 4 존재
22. 실제 목적지 도착 후 엔딩
23. 미확정 아이디어 임의 구현 금지

## 추가 금지 시스템
- 공격/슈팅
- HP
- 스킬
- 업그레이드
- 상점
- 코인
- 아이템
- 장비
- 콤보
- 미션
- 난이도 선택
- 복잡한 설정 메뉴
