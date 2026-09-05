# Unity 자동 제작 구조

## 권장 계층
```text
BossPatternDefinition (Data)
        ↓
BossPatternScheduler
        ↓
BossPatternRunner
        ↓
IBossAttackExecutor
        ├─ FishingLineAttackExecutor
        ├─ BearAttackExecutor
        └─ WaterfallAttackExecutor
```

## BossPatternDefinition 권장 필드
```csharp
[System.Serializable]
public class BossPatternDefinition
{
    public string id;
    public PatternCategory category;
    public List<LaneStep> steps;
    public float firstTelegraphDuration;
    public float stepInterval;
    public float restAfterPattern;
    public bool requiresJumpReady;
}
```

## LaneStep
```csharp
[System.Serializable]
public class LaneStep
{
    public int[] dangerLanes;
}
```

## PatternCategory
```csharp
public enum PatternCategory
{
    Basic,
    Sequential,
    RoundTrip,
    Cross,
    Pressure,
    SafeLaneShift,
    FullLaneJump
}
```

## Scheduler 담당
- 현재 Boss Phase 판단
- 허용 패턴 계산
- GuaranteedPatternBag 관리
- 즉시 동일 패턴 반복 방지
- Boss3 Jump Ready 검사
- Boss3 20초 이후 Chain 선택
- 30초 종료 후 새 패턴 예약 중지

## Attack Executor
### FishingLineAttackExecutor
해당 레인에 낚싯줄 1~2개 활성.

### BearAttackExecutor
- Sequential/RoundTrip/Cross/Pressure = `BearArm_Back`
- Basic/SafeLaneShift = `BearArm_Side`
- Double은 같은 Side 팔 2개

### WaterfallAttackExecutor
- 빨간 경고
- 대상 레인 임시 강한 물줄기
- 돌 공격체
- 종료 후 baseline 복귀

## 자동 검증 항목
1. 세 보스 모두 공통 복합 5종 존재
2. Fake Pattern 없음
3. 일반 복합: Boss1 0.9 / Boss2 0.7 / Boss3 0.6
4. Pressure = 전부 0.5
5. SafeLane = 1.0 / 0.8 / 0.65
6. Rest = 0.8 / 0.6 / 0.45
7. Boss3 FullJump = [1,2,3]
8. Boss3 FullJump Telegraph = 1.5
9. Jump Ready false면 FullJump 선택 불가
10. LongRock은 Boss3 replacement가 아님
