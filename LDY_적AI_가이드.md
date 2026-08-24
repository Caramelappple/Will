# 적 AI 구조 가이드

> `Assets/_Scripts/LDY/AI/` 15개 파일 정리.
> 규칙 기반 FSM이 아니라 **점수제(스코어링)** 방식이다.

---

## 1. 한 줄 요약

**할 수 있는 모든 행동을 늘어놓고, 각각 점수를 매겨, 제일 높은 걸 고른다.**

```
후보 열거 → 점수 합산 → 최고점 선택 → 실행
```

if문으로 "공격 가능하면 공격, 아니면 이동"을 적는 대신, 각 행동에 점수를 붙여 자연스럽게 우선순위가 나오게 했다. 기물마다 다른 성격을 주기 쉽고, 규칙이 늘어도 `if`가 중첩되지 않는다.

---

## 2. 전체 흐름

```
LDY_EnemyAI              턴을 굴린다 (코루틴)
  │
  ├─ LDY_EnemyBrain      무엇을 할지 정한다 (결정만)
  │    ├─ 후보 열거      MoveSystem / AttackSystem 재사용
  │    └─ 점수 합산      LDY_IActionScorer 여러 개
  │
  └─ LDY_ActionExecutor  결정을 실제로 실행하고 결과를 확인
```

**결정과 실행이 분리돼 있다.** Brain은 `LDY_EnemyAction`(구조체)만 돌려주고 보드를 건드리지 않는다. 그래서 Brain만 따로 테스트할 수 있다.

---

## 3. 행동 후보 — `LDY_EnemyAction`

```csharp
public readonly struct LDY_EnemyAction
{
    public readonly LDY_ActionKind Kind;   // Wait / Move / Attack
    public readonly Vector3Int MoveTo;     // Move일 때만
    public readonly LDY_Animal Target;     // Attack일 때만
}
```

`struct`라 후보를 잔뜩 만들어도 힙 할당이 없다. 생성자가 private이고 `Wait()` / `Move()` / `Attack()` 팩토리로만 만들어서, Kind와 필드가 어긋나지 않는다.

### 후보 열거 순서

```
1. 대기
2. 공격 가능한 대상들
3. 이동 가능한 칸들
```

**대기가 맨 앞인 게 의도적이다.** 모두 동점이면 제자리에 남는다. 부화를 기다리는 드래곤 알처럼 대기가 정답인 기물은 전용 scorer가 대기에 점수를 얹으면 된다.

후보는 기존 `LDY_MoveSystem.GetMovableTiles` / `LDY_AttackSystem.GetAttackTargets`에서 그대로 가져온다. **판정 규칙을 두 번 적지 않기 위해서다.** 점유 칸 차단, 사거리, 행동력 소진이 자동으로 반영된다.

---

## 4. 점수 매기기 — `LDY_IActionScorer`

```csharp
public interface LDY_IActionScorer
{
    int Score(LDY_Animal self, in LDY_EnemyAction action, LDY_BoardManager board);
}
```

관심 없는 Kind에는 `0`을 돌려주고, 여러 scorer의 점수는 Brain이 **단순 합산**한다.

### 기본 4종 (공용)

| Scorer | 언제 | 점수 |
| --- | --- | --- |
| `LDY_AttackPriorityScorer` | 공격 후보 | **+100** |
| `LDY_KillBonusScorer` | 이번 공격으로 처치 가능 | +50 |
| `LDY_FrontlineScorer` | 공격 대상이 아군 진영 깊이 들어옴 | 0~7 |
| `LDY_PositioningScorer` | 이동 | ±60 또는 오차 기울기 |

### 점수 설계의 핵심

**공격 100 > 이동 최대 60.**

이게 "공격할 수 있으면 공격한다"를 만든다. 코드에도 못을 박아뒀다.

```csharp
[Range(0, LDY_AttackPriorityScorer.DefaultBonus - 1)]
[SerializeField] private int enterRangeBonus = 60;
```

인스펙터에서 이동 보너스를 100 이상으로 못 올린다. **규칙을 숫자로 강제한 것이다.**

---

## 5. `LDY_PositioningScorer` — 가장 까다로운 부분

이동 평가만 따로 설명이 필요하다.

### 왜 거리만으로는 안 되나

처음엔 "적에게 가까워지면 +점"이었다. 그런데 **원거리·점프 기물이 사거리를 맞추려고 물러설 수 없었다.** 후퇴는 항상 음수라 대기(0점)를 절대 못 이기기 때문이다.

점프 기물은 3칸 거리에서만 공격 가능한데, 2칸까지 붙으면 영영 공격을 못 한다.

### 지금 방식

```csharp
if (canThere && !canNow) return +enterRangeBonus;   // 사거리 안으로 들어감
if (!canThere && canNow) return -exitRangePenalty;  // 사거리 밖으로 나감

// 어느 쪽도 아니면 사거리 오차의 개선량으로 미세 조정
return (지금_오차 - 거기_오차) * stepWeight;
```

**사거리 진입/이탈은 ±로 확실히 가르고, 그 외에만 기울기를 준다.** 점프 기물은 너무 가까우면 "오차"가 생기므로 물러서는 이동이 양수가 된다.

공격 가능 여부의 진짜 판정은 항상 `LDY_AttackSystem.HasTargetFrom`이 한다.

---

## 6. `LDY_AttackRangeMetrics` — 사거리를 재는 곳

사거리 숫자를 AI에 옮겨 적지 않기 위해, **실제 사거리 전략이 짚는 타일에서 역산**한다.

```csharp
var center = new Vector3Int(Size / 2, 0, Size / 2);
List<Vector3Int> tiles = strategy.GetAttackableTiles(center, board);
// 여기서 최소·최대 거리를 뽑는다
```

보드 한가운데서 재는 이유는 가장자리에서 재면 `IsInside` 필터에 타일이 잘려 실제보다 좁게 나오기 때문이다.

### 거리 척도

**체비쇼프(King 거리)** 를 쓴다.

```csharp
Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.z - b.z))
```

이동이 대각선 포함 8방향이라서다. 맨해튼으로 재면 대각선 한 칸이 2로 계산돼 **같은 1칸 이동인데 점수가 두 배**가 된다.

> 주의: 이 값은 "붙어라 / 떨어져라" 기울기용 휴리스틱일 뿐이다.
> 근접은 대각선 때문에 원거리와 같은 범위가 나오는데, 진짜 판정은 `HasTargetFrom`이 한다.

`RangeType`별로 고정이라 한 번 재고 캐시한다. `[RuntimeInitializeOnLoadMethod]`로 씬 로드 때 비운다.

---

## 7. 기물별 성격 — `LDY_ScorerRegistry`

```
공용 scorer   모든 적이 공통으로 씀
    +
전용 scorer   특정 기물만 추가로 씀
```

Brain이 둘을 합산한다.

```csharp
int total = Accumulate(공용 scorers);
total += Accumulate(전용 scorers);
```

### 예: `LDY_MadDogScorer` (광견)

매 턴 체력이 깎이고 공격할 때만 회복하는 기물이다. **체력이 적을수록 공격을 더 강하게 밀어붙인다.**

```csharp
float missingRatio = (max - current) / (float)max;
return attackBonus + Mathf.RoundToInt(lowHealthBonus * missingRatio);
```

체력이 가득이면 +40, 빈사면 +80. 공격 우선 100과 합쳐져 다른 적보다 훨씬 공격적으로 움직인다.

### 매핑 방식

`LSO_AnimalSO` 에셋 참조를 키로 쓴다.

```csharp
[SerializeReference] private List<LDY_IActionScorer> scorers;
```

`[SerializeReference]`라 **인스펙터의 타입 선택기에서 scorer 구현을 직접 골라 붙일 수 있다.** 일반 `[SerializeField]`는 인터페이스를 직렬화하지 못한다.

기물 식별용 문자열 id가 생기면 키만 바꾸면 되고 Brain은 손대지 않는다.

---

## 8. 실행과 검증 — `LDY_ActionExecutor`

`MoveSystem.MoveTo`와 `AttackSystem.Attack`은 **`void`라서 자체 검증에 걸리면 조용히 아무것도 안 한다.**

"결정은 맞는데 실행이 안 된" 상황을 구분하려고 결과를 관측한다.

| 결과 | 뜻 |
| --- | --- |
| `Waited` | 대기를 골라 실행할 게 없었다 |
| `Executed` | 실행이 실제로 일어난 것을 확인했다 |
| `Rejected` | 호출했지만 시스템이 조용히 무시했다 |
| `Unverified` | 확인할 수단이 없었다 |

### 어떻게 확인하나

**이동** — `board.Move`가 `animal.pos`를 그 자리에서 갱신하므로 좌표 변화만 보면 된다.

**공격** — 피해가 연출 코루틴 중간에 들어가서 호출 직후에는 보드에 변화가 없다. 동기적으로 볼 수 있는 신호는 **행동력 소모뿐**이다.

```csharp
int before = _actionPoints.Current;
_attackSystem.Attack(self, target);

return _actionPoints.Current < before ? Executed : Rejected;
```

---

## 9. 턴 루프 — `LDY_EnemyAI`

```
while (행동력 있음)
    적 목록 순회
        각 적: 판단 → 실행
    아무도 행동 못 했으면 break   ← 무한 루프 방지
```

행동력이 남아 있는 한 **같은 적이 여러 번 행동할 수 있다.** 그래서 적 목록을 반복해서 훑는다.

### 무한 루프 방지

```csharp
if (outcome == Executed)
    actedThisPass = true;
```

**확인된 실행만 "행동했다"로 친다.** 대기나 거부를 true로 치면 보드가 그대로인데 패스가 영원히 이어진다.

### 대기 중 재확인

```csharp
yield return new WaitForSeconds(actionDelay);

if (enemy == null) continue;              // 그 사이 죽었을 수 있다
if (!actionPoints.HasActionPoints) break;
if (!CanAct(enemy)) continue;
```

연출 대기 동안 상황이 바뀔 수 있어서 판단 직전에 다시 본다.

### 필수 배선

`Board` / `MoveSystem` / `AttackSystem` / **`ActionPointManager`** 넷 다 필요하다.

`ActionPointManager`가 없으면 행동력이 안 줄어 **턴 루프의 종료 조건이 사라지고**, 공격 실행 여부도 관측할 수 없다. 그래서 없으면 아예 진행하지 않는다.

---

## 10. 밸런싱 — `LDY_IDecisionLogger`

인스펙터의 **Enable Decision Log**를 켜면 후보별 점수 내역이 콘솔에 찍힌다.

```
Crow: Attack(Wolf) = 157
  LDY_AttackPriorityScorer  +100
  LDY_KillBonusScorer        +50
  LDY_FrontlineScorer         +7
```

`Logger`가 `null`이면 **내역 리스트를 만들지도 않는다.** 평소에는 비용이 0이다.

```csharp
bool trace = Logger != null;
if (trace) _breakdown.Clear();
```

재생 중에 체크박스를 껐다 켜도 다음 판단부터 반영된다.

> `Rejected`(조용히 무시된 행동)는 **토글과 무관하게 항상 경고**로 남는다. 놓치면 "결정이 틀린 것"과 구분할 수 없다.

---

## 11. 새 성격을 추가하려면

`LDY_MadDogScorer`를 본떠 만들면 된다.

```csharp
[Serializable]                                   // ← 인스펙터에 뜨려면 필수
public class LDY_MyScorer : LDY_IActionScorer
{
    [SerializeField] private int bonus = 30;

    public int Score(LDY_Animal self, in LDY_EnemyAction action, LDY_BoardManager board)
    {
        if (action.Kind != LDY_ActionKind.Attack) return 0;   // 관심 없으면 0
        // ...
        return bonus;
    }
}
```

1. `[Serializable]` 붙이기
2. `LDY_ScorerRegistry` 에셋에 기물과 함께 등록
3. `[SerializeReference]` 목록의 타입 선택기에서 고르기

**Brain도 EnemyAI도 고칠 필요가 없다.**

---

## 12. 알아둘 것

**점수는 상대적이다.** 새 scorer의 값을 크게 잡으면 기존 우선순위가 통째로 뒤집힌다. 공격 100이 기준선이라고 보면 된다.

**동점이면 먼저 열거된 후보가 이긴다.** 열거 순서가 대기 → 공격 → 이동이라, 같은 점수면 대기가 뽑힌다. 공격 후보끼리 동점이면 `GetAttackTargets`가 돌려주는 순서를 따른다.

**같은 보드 상태면 항상 같은 결정이 나온다.** 공격·이동 목록이 고정된 방향 배열과 좌표 순회에서 나오기 때문이다. 재현이 가능해서 디버깅이 쉽다.
