# 보스 페이즈 설계

> 보스 4종이 공유할 페이즈 시스템.
> 전환 조건은 **체력 기준**, 한 번 넘어가면 **되돌아오지 않는다.**

---

## 확정된 규칙

```
전환 조건   특정 HP 이하
방향        단방향 — 회복해도 이전 페이즈로 안 돌아감
```

이 둘이 설계를 크게 단순화합니다.

- 조건이 체력 하나뿐이라 **조건 인터페이스가 필요 없습니다.** 경계값 목록이면 충분합니다.
- 단방향이라 **되돌아갈 때의 처리를 고민할 필요가 없습니다.** 해금한 특성을 다시 떼거나, 강화 수치를 되돌리는 코드가 통째로 사라집니다.

나중에 「5턴 경과」 같은 조건이 필요해지면 그때 인터페이스로 뽑으면 됩니다. 지금 미리 만들면 쓰지도 않는 추상화가 남습니다.

---

## 페이즈는 네 가지 다른 일이다

```
언제 바뀌나      조건       체력 경계
특성 수치 강화   효과       특성이 스스로 조정
특성 해금        효과       AddAbility
AI 성향          효과       scorer 교체
연출             효과       대사 · 이펙트
```

**가운데에 「지금 몇 페이즈인가」 하나를 두고 나머지가 그걸 읽습니다.** 이 형태라야 보스 넷이 같은 부품을 씁니다.

```
                 LSO_BossPhase
                  (상태 하나)
                       │
      ┌────────┬───────┼────────┬─────────┐
      ▼        ▼       ▼        ▼         ▼
   특성 강화  해금    AI 성향   연출    (앞으로 늘어날 것)
```

---

## 1. LSO_BossPhase — 상태

```csharp
[RequireComponent(typeof(LDY_Animal))]
public class LSO_BossPhase : MonoBehaviour
{
    [Tooltip("페이즈가 올라가는 체력 경계. 큰 값부터 내림차순으로 넣는다.\n" +
             "예: [15] 이면 HP 15 이하에서 2페이즈.")]
    [SerializeField] private List<int> healthThresholds = new() { 15 };

    /// <summary>1부터 시작한다. 기획서의 "1페이즈"와 숫자를 맞춘 것이다.</summary>
    public int Current { get; private set; } = 1;

    public event Action<int> PhaseChanged;
}
```

경계값을 **절대 HP**로 두는 이유가 있습니다. 까마귀왕은 포식으로 최대 체력이 계속 늘어나는데, 비율 기준이면 최대치가 오를 때마다 전환 지점이 밀립니다. 기획도 「HP 15 이하」라고 절대값으로 적혀 있습니다.

### 단방향은 한 줄로 보장됩니다

```csharp
private void Refresh()
{
    int reached = Resolve(_animal.health.Value);

    // 되돌아가지 않는다. 회복해서 경계 위로 올라가도 그대로다.
    if (reached <= Current) return;

    Current = reached;
    PhaseChanged?.Invoke(Current);
}

private int Resolve(int hp)
{
    int phase = 1;
    foreach (int t in healthThresholds)
        if (hp <= t) phase++;

    return phase;
}
```

한 번에 두 단계를 뛰어넘어도(즉사급 피해) `Current`가 곧바로 최종 페이즈가 되고, 이벤트는 **한 번만** 발행됩니다.

### 언제 확인하나

```csharp
OnEnable:   _animal.health.OnDamage += HandleDamaged;
OnDisable:  _animal.health.OnDamage -= HandleDamaged;
```

보스 체력은 피해로만 줄어듭니다. 회복은 단방향 가드가 막아주므로 구독할 필요가 없습니다.

> **`Health.Value`는 밖에서 직접 대입할 수도 있습니다.** 그런 경로가 생기면 `Refresh()`를 public으로 열어두고 그쪽에서 부르면 됩니다.

---

## 2. 특성 수치 강화 — LSO_IPhaseAware

특성이 매번 `GetComponent<LSO_BossPhase>()`로 물어보는 대신, **바뀔 때 알려주는 쪽**을 씁니다.

```csharp
public interface LSO_IPhaseAware
{
    void OnPhaseChanged(int phase);
}
```

```csharp
// LSO_BossPhase가 전환될 때
LSO_AbilityNotify.Notify<LSO_IPhaseAware>(_animal.Abilities, a => a.OnPhaseChanged(Current));
```

`LSO_IOnKill`·`IStatModifier`와 **완전히 같은 패턴**입니다. 새로 배울 게 없고, 페이즈를 신경 쓰는 특성만 이 인터페이스를 구현합니다.

### 까마귀왕 포식에 적용하면

```csharp
public class LSO_Predation : LSO_IAbility, LSO_IOnKill, ..., LSO_IPhaseAware
{
    [SerializeField] private int phase2BonusAtk = 1;
    [SerializeField] private int phase2BonusHp = 2;

    private int _bonusAtk;
    private int _bonusHp;

    public void OnPhaseChanged(int phase)
    {
        if (phase < 2) return;

        _bonusAtk = phase2BonusAtk;
        _bonusHp = phase2BonusHp;
    }

    private void Devour(LDY_Animal self, LDY_Animal victim)
    {
        int atk = Inherit(victim.GetAtk()) + _bonusAtk;
        int hp = Inherit(victim.health.MaxValue) + _bonusHp;
        // ...
    }
}
```

`LSO_CrowKingMemory`는 **계산이 끝난 값만 받으므로 손댈 필요가 없습니다.** 처음부터 "보관만 한다"로 나눠둔 게 여기서 값을 합니다.

<br>

특성은 팩토리가 개체마다 새로 만들기 때문에 `_bonusAtk` 같은 상태를 들고 있어도 안전합니다. **scorer와 정반대**라는 점만 기억하면 됩니다.

---

## 3. 특성 해금 — 이미 부품이 있습니다

되먹임 때문에 만들어둔 `LDY_Animal.AddAbility(type)`가 그대로 쓰입니다. 개별 `Bind`까지 해주므로 런타임에 붙여도 정상 동작합니다.

```csharp
[RequireComponent(typeof(LSO_BossPhase))]
public class LSO_BossPhaseUnlocker : MonoBehaviour
{
    [Serializable]
    private class Unlock
    {
        [Min(2)] public int phase = 2;
        public List<LSO_AbilityType> abilities = new();
    }

    [SerializeField] private List<Unlock> unlocks = new();
}
```

`PhaseChanged`를 구독해서 해당 페이즈 이하의 항목을 전부 붙입니다.

**보스별 코드가 전혀 필요 없습니다.** 인스펙터에서 「몇 페이즈에 무엇을」만 채우면 끝입니다.

> 한 번에 두 단계를 뛰어넘는 경우가 있으니 `phase == Current`가 아니라 **`phase <= Current`** 로 훑어야 합니다. 안 그러면 건너뛴 페이즈의 특성을 영영 못 받습니다.

---

## 4. AI 성향 — LSO_BossPhaseScorer

```csharp
[Serializable]
public class LSO_BossPhaseScorer : LDY_IActionScorer
{
    [Serializable]
    private class PhaseSet
    {
        [Min(1)] public int phase = 1;
        [SerializeReference, LSO_SubclassPicker]
        public List<LDY_IActionScorer> scorers = new();
    }

    [SerializeField] private List<PhaseSet> sets = new();

    public int Score(LDY_Animal self, in LDY_EnemyAction action, LDY_BoardManager board)
    {
        LSO_BossPhase phase = self != null ? self.GetComponent<LSO_BossPhase>() : null;
        if (phase == null) return 0;

        int sum = 0;
        foreach (PhaseSet set in sets)
        {
            if (set.phase != phase.Current) continue;

            foreach (LDY_IActionScorer s in set.scorers)
                if (s != null) sum += s.Score(self, action, board);
        }

        return sum;
    }
}
```

레지스트리에 **이거 하나만** 등록하면 내부에서 페이즈별로 갈라집니다. `LDY_EnemyBrain`도 `LDY_EnemyAI`도 수정할 필요가 없습니다.

> **scorer 인스턴스는 모든 기물이 공유합니다.** 페이즈를 필드에 들고 있으면 안 되고, 매번 `self`에서 읽어야 합니다.

<br>

`[SerializeReference]` 중첩이 걱정되면 페이즈 세트를 `LSO_BossPhaseSO`로 빼는 방법도 있습니다. 다만 `LSO_SubclassPicker`가 생겼으니 우선 중첩으로 시도해보고, 인스펙터가 이상하면 그때 SO로 옮기는 순서가 낫습니다.

---

## 5. 연출 — LSO_BossPhaseView

`PhaseChanged`를 구독해서 대사·이펙트·색을 처리합니다.

<br>

**`Score` 안에서 연출을 재생하면 안 됩니다.** 후보 하나당 한 번, 한 턴에 수십 번 불립니다. 반드시 이벤트 쪽에서 하세요.

---

## 제작 순서

| 단계 | 만들 것 | 검증 수단 |
| --- | --- | --- |
| 1 | `LSO_BossPhase` | 콘솔 로그로 전환 확인 |
| 2 | `LSO_IPhaseAware` + 포식 강화 | 정보창 ATK/HP 증가폭 |
| 3 | `LSO_BossPhaseUnlocker` | 인스펙터 「실행 중인 특성 인스턴스」 |
| 4 | `LSO_BossPhaseScorer` | Enable Decision Log |
| 5 | `LSO_BossPhaseView` | 화면 |

1이 없으면 나머지가 전부 못 돕니다.

2를 먼저 하는 건 **정보창 숫자로 즉시 확인**되기 때문이고, 3은 `AddAbility`가 이미 있어서 가장 짧습니다.

### 각 단계 검증 기준

**1단계** — 까마귀왕 HP를 15 이하로 만들면 로그 한 줄. 회복시켜 15를 넘겨도 다시 안 뜸.

**2단계** — 2페이즈에서 곰(4/6) 처치 시 계승이 `+2/+2` 에서 `+3/+4` 로.

**3단계** — 전환 순간 「실행 중인 특성」 목록에 항목이 추가됨.

**4단계** — Decision Log의 점수 구성이 전환 전후로 달라짐.

---

## 4보스 재활용

```
공통 (넷이 공유)
  LSO_BossPhase          체력 경계 목록만 다르게
  LSO_IPhaseAware        훅 계약
  LSO_BossPhaseUnlocker  페이즈별 해금 목록만 다르게
  LSO_BossPhaseScorer    페이즈별 scorer 조합만 다르게
  LSO_BossPhaseView      연출 에셋만 다르게

보스별
  각자의 특성 클래스만
```

**두 번째 보스부터는 코드를 안 씁니다.** 프리팹에 컴포넌트 셋을 붙이고 인스펙터를 채우면 페이즈가 붙습니다.

---

## 함정

**scorer는 인스턴스가 공유됩니다.** 페이즈든 뭐든 상태를 필드에 들면 같은 종류 기물끼리 섞입니다. 특성은 반대로 개체마다 새로 만들어지니 들어도 됩니다.

**한 번에 여러 단계를 뛰어넘을 수 있습니다.** 해금과 강화 모두 `phase <= Current` 기준으로 처리해야 건너뛴 페이즈의 효과를 놓치지 않습니다.

**`Score` 안에서 연출·상태 변경 금지.** 순수 계산만.

**`Awake`에서 `SetActive(false)`하면 `OnEnable`이 안 돕니다.** 연출용 오브젝트를 숨겨둘 때 조심하세요.

**해금한 특성은 정보창에 안 나옵니다.** `AbilityTypes`가 SO 데이터라서 런타임 추가분이 빠집니다. 되먹임 때 남겨둔 문제와 같은 것이라 한 번에 같이 해결하면 됩니다.

---

## 체크리스트

- [ ] 보스 4종의 체력 경계값 확정
- [ ] 2페이즈에 해금할 특성 목록 (보스별)
- [ ] 2페이즈 강화 수치 (까마귀왕 포식 +1/+2 외)
- [ ] 1단계 — `LSO_BossPhase`
- [ ] 2단계 — `LSO_IPhaseAware` + 포식 강화
- [ ] 3단계 — `LSO_BossPhaseUnlocker`
- [ ] 4단계 — `LSO_BossPhaseScorer`
- [ ] 5단계 — `LSO_BossPhaseView`
