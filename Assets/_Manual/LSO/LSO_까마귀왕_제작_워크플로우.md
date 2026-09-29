# 까마귀왕 보스 제작 워크플로우

> 혼자 만들 때 순서대로 따라갈 문서.
> 순서의 기준은 **각 단계가 혼자서 검증 가능한가**다. 보스는 눈으로 확인하기 어려워서, 순서를 잘못 잡으면 뭐가 안 되는지 못 찾는다.

---

## 시작 전 — 이미 깔려 있는 것

이 셋은 까마귀왕을 위해 미리 뚫어둔 것이다. 새로 만들 필요 없다.

| 항목 | 위치 | 용도 |
| --- | --- | --- |
| `LSO_IOnKill` | `LSO/DeathSystem/` | 포식 — "내가 죽였을 때" |
| `LSO_IAbilityCountModifier` | `LSO/Ability/` | 기억 폭주 — 2회 공격 |
| `LDY_Animal.AddAbility(type)` | `LDY/` | 되먹임 — 런타임 특성 추가 |

그 외에 이미 되는 것들:

- **특성을 여러 개 달 수 있다** — `LSO_AnimalSO.abilities` 리스트
- **보드 변화 신호** — `LDY_BoardManager.OnBoardChanged`
- **선택 이벤트** — `LDY_SelectionController.OnSelectionChanged` / `OnEnemyInspectedChanged`

---

## 먼저 정해야 할 기획

여기가 안 정해지면 1·2단계에서 막힌다.

- [ ] **「최대 3회 계승」과 「최대 3개 특성 저장」이 같은 카운터인가?**
      곰·늑대·곰을 죽였을 때 계승 3회를 다 쓴 건지, 계승 2회 + 특성 1개인지.
- [ ] **기억 폭주의 「ATK −1」이 어디서 빠지나?**
      계승분인지 `baseAtk`인지. 「6 미만이 되지 않는다」가 계승 총합 기준인지 실제 ATK 기준인지.
- [ ] **2페이즈 「되먹임 효과 강화」의 구체적 수치**
- [ ] **사냥감이 없을 때** — 플레이어 기물이 하나도 없거나 이미 죽었으면?

---

## 특성 하나를 만들 때 — 항상 3종 세트

**클래스 · enum · 팩토리 등록은 반드시 같이 움직인다.** 하나라도 빠지면 조용히 안 먹는다.

### 1. 클래스

```csharp
namespace _Scripts.LSO.Boss
{
    public class LSO_Predation : LSO_IAbility, LSO_IOnKill, LSO_IAbilityInitializable
    {
        private LSO_CrowKingMemory _memory;

        public void Initialize(LSO_AbilityContext context)
        {
            LDY_Animal owner = context?.Owner;
            _memory = owner != null ? owner.GetComponent<LSO_CrowKingMemory>() : null;

            if (_memory == null)
                Debug.LogError($"{owner?.name}: LSO_CrowKingMemory가 없어 포식이 동작하지 않습니다.", owner);
        }

        public void OnKill(LDY_Animal self, LDY_Animal victim)
        {
            if (_memory == null) return;
            // ...
        }
    }
}
```

`LSO_IAbility`는 빈 마커 인터페이스다. **이걸 안 붙이면 `_abilities` 목록에 못 들어간다.**

### 2. enum — 반드시 목록 끝에

```csharp
public enum LSO_AbilityType
{
    ...
    WillEnhancement,   // 16
    Predation          // 17  ← 끝에 추가
}
```

enum은 에셋에 `int`로 저장된다. 중간에 끼우면 기존 에셋의 `ability: 10`이 갑자기 다른 특성을 가리킨다.

한 번에 하나씩 끝에 붙이면 값이 안정적이다. 미리 세 개를 몰아 넣으면 **클래스는 없는데 인스펙터에서 고를 수 있는 구간**이 생긴다.

### 3. 팩토리 등록

```csharp
// LSO_AbilityFactory.Creators
{ LSO_AbilityType.Predation, () => new LSO_Predation() },
```

빠뜨리면 `Create`가 `null`을 돌려주고 경고만 남는다. `Immune`과 `Double`이 지금 그 상태다.

### 4. 확인

`CrowKingSO`의 `abilities` 리스트에 추가하고, 재생 중 인스펙터의 **「실행 중인 특성 인스턴스」** 목록에 뜨는지 본다.

---

## 0단계 — 껍데기

- [ ] `_Scripts/LSO/Boss/` 폴더 생성 (네임스페이스 `_Scripts.LSO.Boss`)
- [ ] `CrowKingSO` 에셋 생성 — 스탯, 사거리, `defaultWill`
- [ ] 보스 프리팹 — `LDY_Animal`, `Health`, 스탯 라벨, 정보창 대상
- [ ] 씬에 배치해서 일반 적처럼 움직이는지 확인

> **왜 먼저** — 스탯 라벨과 정보창이 붙어 있어야 이후 단계를 눈으로 확인할 수 있다.

---

## 1단계 — Memory + 포식

**되먹임은 뺀다.** 순수 계승만.

### 만들 것

`LSO_CrowKingMemory` (MonoBehaviour, 보스 프리팹에 붙임)

```csharp
[SerializeField, Min(1)] private int maxDevourCount = 3;
[SerializeField, Min(1)] private int maxStolenAbilities = 3;

public int InheritedAtk { get; private set; }
public int InheritedHp  { get; private set; }

public bool CanDevour => _devoured.Count < maxDevourCount;
public bool HasDevoured(LSO_AnimalSO animal);

public bool TryRecord(LSO_AnimalSO animal, int atk, int hp);
public bool TryStoreAbility(LSO_AbilityType type);
```

`LSO_Predation` (특성, `LSO_IOnKill`)

### 설계 규칙

**Memory는 보관만 한다. 판단은 특성이 한다.**

```
Memory      "곰 4/6을 먹었다"를 기록          ← 사실
Predation   "2페이즈면 +1/+2 더해서" 판단     ← 규칙
```

그래서 `TryRecord`는 **이미 계산이 끝난 수치**를 받는다. Memory는 페이즈를 몰라도 된다.

**`_devoured`는 `LSO_AnimalSO` 참조로 기록한다.** 되먹임의 "같은 종류" 판정이 곧 SO 동일성이다. 이름 문자열로 비교하면 `animalName` 중복(Corvo / Corvo 1)에 걸린다.

**이벤트는 넣지 않는다.** 구독할 곳이 없다 — ATK는 `IStatModifier`로 자동 반영되고, HP는 `Health`가 자기 이벤트를 발행하고, UI는 보드 신호와 폴링으로 갱신된다.

### ATK와 HP를 반영하는 방법이 다르다

| | 방법 | 이유 |
| --- | --- | --- |
| ATK | `IStatModifier`로 **매번 더한다** | `baseAtk`를 직접 건드리면 되돌릴 수 없고 DLJ 유언과 충돌 |
| HP | `health` 최대치를 **직접 올린다** | 체력은 저장값이라 파생으로 만들 수 없다 |

ATK 계산은 `LSO_MemoryFrenzy` 한 곳에서만 한다(2단계). **포식은 기록만 하고 ATK를 안 건드린다.**

### 검증

> 보스가 곰(4/6)을 죽이면 정보창의 ATK/HP가 즉시 +4/+6.
> 네 번째 처치부터는 안 오름.

---

## 2단계 — 기억 폭주

`LSO_MemoryFrenzy` (특성, `IStatModifier` + `LSO_IAbilityCountModifier`)

Memory를 **읽기만** 한다.

```csharp
public int ModifyAttack(LDY_Animal self, int atk)
    => atk + _memory.InheritedAtk - 폭주감쇠;

public int ModifyAttackCount(LDY_Animal self, LDY_Animal target, int count)
    => 폭주중 ? count + 1 : count;
```

### 검증

> 계승 ATK 합이 6을 넘는 순간 다음 공격이 2연타.
> 행동력은 **한 번만** 소모.

행동력이 두 번 빠지면 `LDY_ActionExecutor`가 공격 성공을 오판한다. 반드시 확인할 것.

---

## 3단계 — 사냥감 3종 묶음

**한 덩어리로 만든다.** 지정만 있고 추적이 없으면 의미가 없다.

### `LSO_PreyTracker` (MonoBehaviour, 보스 프리팹)

```csharp
private LDY_Animal _prey;

/// 파괴된 기물은 null로 취급한다.
public LDY_Animal Prey => _prey != null ? _prey : null;

public event Action<LDY_Animal> PreyChanged;
public void SetPrey(LDY_Animal animal);
```

**여기는 이벤트가 정당하다** — 해골 UI가 이전 표시를 떼고 새로 붙여야 한다.

**사냥감이 죽었을 때 따로 처리할 게 없다.** 유니티의 `==` 오버로드가 파괴된 오브젝트를 `null`로 만들어주고, 해골을 사냥감의 자식으로 붙이면 함께 사라진다. 매 턴 새로 지정되므로 자동으로 갱신된다.

### `LSO_PreyMarking` (특성, `IOnTurnStart`)

```csharp
public void OnTurnStart(LDY_Team team)
{
    if (team != LDY_Team.Enemy) return;   // 보스가 움직이기 전에
    _tracker.SetPrey(고른 기물);
}
```

**정책(누구를 고를지)을 특성에 두는 이유** — 정보창에 능력으로 표시되어야 한다. Tracker가 직접 고르면 플레이어에게 안 보인다.

### `LSO_PreyScorer` (AI, `LDY_IActionScorer`)

```csharp
if (action.Kind != LDY_ActionKind.Attack) return 0;
return action.Target == tracker.Prey ? bonus : 0;
```

> **scorer 인스턴스는 모든 기물이 공유한다.** 절대 상태를 들지 말 것.
> `self.GetComponent<LSO_PreyTracker>()`로 매번 읽는다. Brain은 한 기물의 후보를 연속 평가하므로 **직전 `self` 하나만 캐시**하면 적중률이 사실상 100%다.

`LDY_ScorerRegistry` 에셋에 `CrowKingSO → [LSO_PreyScorer]` 등록.

### 검증

> **Enable Decision Log**를 켜고, 사냥감 대상 공격 후보에만 가산점이 붙는지 확인.

```
[LDY_EnemyBrain] CrowKing | Attack(Bear) = 157   LDY_AttackPriorityScorer:100   LSO_PreyScorer:50   ...
```

**해골 UI 없이 로그로 검증된다.** 이게 이 순서의 이점이다.

---

## 4단계 — 해골 표시

`LSO_PreyMarkView` — `tracker.PreyChanged`를 구독해서 표시를 옮긴다.

**로직이 맞는 걸 확인한 뒤에 붙인다.** 순서를 뒤집으면 "해골이 안 뜬다"가 지정 문제인지 표시 문제인지 구분이 안 된다.

---

## 5단계 — 페이즈 (HP 15 이하)

**수치 강화** — 각 특성이 `owner.health`를 보고 분기한다. 상태를 안 들어서 안전하다.

**AI 성향** — `LSO_BossPhaseScorer` (Composite scorer)

```csharp
[Serializable]
public class LSO_BossPhaseScorer : LDY_IActionScorer
{
    [SerializeField] private List<LSO_BossPhaseSO> phases = new();

    public int Score(LDY_Animal self, in LDY_EnemyAction action, LDY_BoardManager board)
    {
        LSO_BossPhaseSO phase = Resolve(self);   // 체력 비율로 고른다
        if (phase == null) return 0;

        int sum = 0;
        foreach (LDY_IActionScorer s in phase.Scorers)
            if (s != null) sum += s.Score(self, action, board);
        return sum;
    }
}
```

레지스트리에 **이거 하나만** 등록하면 내부에서 페이즈별로 갈라진다. `LDY_EnemyBrain`도 `LDY_EnemyAI`도 수정할 필요가 없다.

**페이즈를 SO로 빼는 이유** — `[SerializeReference]` 안에 또 `[SerializeReference]`를 중첩하면 인스펙터가 불안정하다.

**「2페이즈 진입!」 연출은 여기서 하면 안 된다.** `Score`는 후보 하나당 한 번, 한 턴에 수십 번 불린다. 체력 변화를 구독하는 별도 감시 컴포넌트가 처리해야 한다.

---

## 6단계 — 되먹임

**제일 마지막.** 같은 종류를 두 번 죽여야 발동해서 테스트가 가장 오래 걸린다.

`LSO_Predation`에 분기를 추가한다.

```csharp
if (_memory.HasDevoured(victim.data))
{
    foreach (LSO_AbilityType type in victim.AbilityTypes)
        if (_memory.TryStoreAbility(type))
            self.AddAbility(type);
}
```

**`AddAbility`가 개별로 `Bind`까지 한다.** 그냥 `_abilities`에 넣기만 하면 피해 계산과 디스패처에 안 걸려서, 붙었는데 아무 일도 안 일어난다.

### 남은 문제

`AbilityTypes`는 SO 데이터라서 **흡수한 특성이 정보창에 안 나온다.** 표시를 `Abilities`(인스턴스) 기준으로 바꾸거나, `LDY_Animal`이 추가된 타입을 따로 기억하게 해야 한다. 여기서 같이 정할 것.

---

## 훅 레퍼런스

특성이 구현할 수 있는 인터페이스 전부.

### 등록형 — `LSO_AbilityWiring.Bind`가 붙여준다

| 인터페이스 | 메서드 | 언제 |
| --- | --- | --- |
| `LSO_IDamageModifier` | `ModifyIncomingDamage(target, data, damage)` + `Priority` | 자기가 맞을 때, 피해량 변경 |
| `IOnTurnStart` | `OnTurnStart(LDY_Team team)` | 턴이 바뀔 때 |
| `IOnEnemyDead` | `OnEnemyDead(LDY_Animal animal)` | **Enemy 팀** 기물이 죽을 때 |

### 순회형 — 호출부가 `LSO_AbilityNotify`로 꺼내 쓴다

| 인터페이스 | 메서드 | 언제 |
| --- | --- | --- |
| `LSO_IOnHit` | `OnHit(self, data)` | 자기가 맞았을 때 |
| `LSO_IOnDeath` | `OnDeath(self, killer)` | 자기가 죽을 때 |
| `LSO_IOnKill` | `OnKill(self, victim)` | **자기가 죽였을 때** |
| `IOnAnimalAttack` | `OnAttack(LSO_AnimalSO animal)` | 자기가 공격할 때 (대상 정보 없음) |
| `IStatModifier` | `ModifyAttack(self, atk)` | `GetAtk()` 계산 중 |
| `LSO_IAbilityCountModifier` | `ModifyAttackCount(self, target, count)` | 타격 횟수 결정 |

### 부가

| 인터페이스 | 용도 |
| --- | --- |
| `LSO_IAbility` | **필수 마커.** 안 붙이면 목록에 안 들어감 |
| `LSO_IAbilityInitializable` | `Initialize(context)` — 소유자·보드·이벤트 접근 |

`LSO_AbilityContext`가 주는 것:

```csharp
context.Owner    // 이 특성을 든 기물
context.Board    // LDY_BoardManager (없으면 null)
context.Events   // GameEventDispatcher (없으면 null)
context.Deaths   // 기물을 죽일 때. 직접 Destroy 하지 말 것
```

---

## 함정 모음

**scorer는 인스턴스가 공유된다.** `LDY_ScorerRegistry`가 같은 리스트를 모든 기물에게 준다. 상태를 필드에 들면 같은 종류 기물끼리 섞인다. 특성은 반대로 개체마다 새로 만들어지니 상태를 들어도 된다.

**enum은 항상 끝에만 추가.** 중간에 끼우면 기존 에셋이 다른 특성을 가리킨다.

**`Score` 안에서 연출·상태 변경 금지.** 순수 계산만.

**`IStatModifier`에는 `Priority`가 없다.** 리스트 순서대로 누적된다. 지금은 전부 덧셈이라 무해하지만 곱연산이 생기면 인스펙터 배치가 밸런스를 좌우한다.

**같은 특성을 두 번 넣으면 뒤쪽이 무시된다.** 팩토리가 서로 다른 인스턴스를 만들어서 `DamageableResources`의 중복 검사를 통과하기 때문에, `LDY_Animal`이 미리 걸러낸다.

**`Awake`에서 `SetActive(false)`하면 `OnEnable`이 안 돈다.** UI를 만들 때 구독을 `OnEnable`에 두면 한 번 닫힌 뒤로는 신호를 못 받는다. `Awake`/`OnDestroy` 쌍을 쓸 것.

**`GetAtk()`는 계산값이라 바뀌는 순간이 없다.** UI가 갱신되려면 `LDY_BoardManager.OnBoardChanged`를 구독하거나 저주기 폴링을 써야 한다.

---

## 진행 체크리스트

- [ ] 기획 4항목 확정
- [ ] 0단계 — 폴더, SO, 프리팹
- [ ] 1단계 — `LSO_CrowKingMemory`, `LSO_Predation` (+ enum + 팩토리)
- [ ] 2단계 — `LSO_MemoryFrenzy` (+ enum + 팩토리)
- [ ] 3단계 — `LSO_PreyTracker`, `LSO_PreyMarking` (+ enum + 팩토리), `LSO_PreyScorer`
- [ ] 4단계 — `LSO_PreyMarkView`
- [ ] 5단계 — `LSO_BossPhaseSO`, `LSO_BossPhaseScorer`
- [ ] 6단계 — 되먹임 + 흡수 특성 표시
