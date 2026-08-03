# LSO 특성(Ability) 시스템 가이드

**대상**: 동물 특성을 새로 만들거나 기존 특성을 수정하는 사람
**핵심 규칙**: 특성은 순수 C# 클래스다. MonoBehaviour가 아니고, 기물마다 새 인스턴스를 받는다.

---

## 1. 전체 그림

```
LSO_AnimalSO.ability (enum)
        │
LSO_AbilityFactory.Create(type)        생성 방법을 등록해둔 팩토리
        │
LDY_Animal.Init()                      개체별 인스턴스 생성 + 컨텍스트 주입
        │
LDY_Animal.RegisterAbilities()         구현한 인터페이스에 따라 연결처가 갈림
        ├─ LSO_IDamageModifier ──> 자신의 Health (피해 계산 파이프라인)
        ├─ LSO_IOnHit          ──> 자신의 Health.OnHit (피격 반응)
        └─ IOnTurnStart 등     ──> GameEventDispatcher (전역 이벤트)

LDY_DeathHandler.Kill()                사망은 여기 한 곳으로만
        └─ LSO_IOnDeath        ──> 죽는 본인의 특성에게 직접 통보
```

---

## 2. 훅 인터페이스 목록

특성은 필요한 인터페이스만 골라 구현한다. 여러 개를 동시에 구현해도 된다.

| 인터페이스 | 네임스페이스 | 발동 시점 | 용도 |
|---|---|---|---|
| `LSO_IDamageModifier` | `_Scripts.LSO.HealthSystem` | 피해가 차감되기 **직전** | 피해량 증감·무효 |
| `LSO_IOnHit` | `_Scripts.LSO.HealthSystem` | 피해를 **맞은 뒤** | 반격 등 후속 반응 |
| `LSO_IOnDeath` | `_Scripts.LSO.DeathSystem` | 자신이 **죽는 순간** (파괴 전) | 사망 시 효과 |
| `IStatModifier` | `_Scripts.LSO.Ability` | 공격력 조회 시 | 공격력 증감 |
| `IOnTurnStart` | `_Scripts.LSO` | 턴 시작 | 지속 효과, 확률 판정 |
| `IOnEnemyDead` | `_Scripts.LSO` | 적이 죽었을 때 | 처치 누적 |
| `LSO_IAbilityInitializable` | `_Scripts.LSO.Ability` | 생성 직후 1회 | 보드·사망창구 접근이 필요할 때 |

> 하위 네임스페이스는 자동 탐색되지 않는다. `using`을 반드시 명시할 것.
> 예: `_Scripts.LSO`에 있는 클래스에서 `LSO_IDeathService`를 쓰려면 `using _Scripts.LSO.DeathSystem;` 필요.

---

## 3. 인터페이스별 사용법

### LSO_IDamageModifier — 피해량 바꾸기

```csharp
int Priority { get; }
int ModifyIncomingDamage(DamageableResources target, DamageData data, int damage);
```

등록된 수정자들이 **Priority 오름차순**으로 차례차례 damage를 가공한다. 관여하지 않을 때는 받은 값을 그대로 반환한다.

**Priority 설계가 중요하다.**

| 값 | 성격 | 예시 |
|---:|---|---|
| -1000 | 회피처럼 **가장 먼저** 판정 | `LSO_Dodge` |
| 0 | 일반 증감 (방어력 등) | |
| 1000 | 최종 하한선을 정하는 효과 | `LSO_Sturdy` |

회피가 옹골참보다 먼저 처리되어야, 회피에 성공했을 때 옹골참의 1회 발동권이 낭비되지 않는다. 시뮬레이션 200회로 검증된 부분이다.

### LSO_IOnHit — 맞은 뒤 반응하기

```csharp
void OnHit(LDY_Animal self, DamageData data);
```

`data.source`로 공격 종류를, `data.giver`로 공격자의 Health를 알 수 있다. 공격자 기물이 필요하면 `data.giver.GetComponent<LDY_Animal>()`로 얻는다.

피해량을 바꾸는 것이 목적이면 이게 아니라 `LSO_IDamageModifier`를 쓸 것.

### LSO_IOnDeath — 죽을 때

```csharp
void OnDeath(LDY_Animal self, LDY_Animal killer);
```

오브젝트가 파괴되기 **전에** 불리므로 이 시점에는 `self`를 쓸 수 있다. `killer`는 자멸·장판 피해 등에서 `null`이 될 수 있으니 항상 확인할 것.

전역 등록이 아니라 **죽는 본인의 특성만** 호출된다.

### LSO_IAbilityInitializable — 바깥 정보가 필요할 때

```csharp
void Initialize(LSO_AbilityContext context);
```

`LSO_AbilityContext`가 제공하는 것:

| 멤버 | 내용 |
|---|---|
| `Owner` | 이 특성을 들고 있는 기물 |
| `Board` | 격자 조회 (`Get`, `IsInside`) |
| `Events` | 전역 이벤트 디스패처 |
| `Deaths` | 기물을 죽이는 창구 |

`Board`·`Events`·`Deaths`는 **호출 시점에 조회**하므로, 특성이 매니저보다 먼저 만들어져도 안전하다. 다만 매니저가 없으면 `null`이 오므로 확인 후 사용할 것.

---

## 4. 피해 출처 (LSO_DamageSource)

```csharp
public enum LSO_DamageSource { Unknown, Melee, Ranged, Jump, Curse, Rage, Ability }
```

`DamageData`에 실려 다니며, "근접 공격에만 반격", "저주 피해 무효" 같은 판단에 쓴다.

**현재 채워지는 곳**

| 출처 | 채우는 위치 | 상태 |
|---|---|---|
| Melee / Ranged / Jump | `LDY_AttackSystem` (공격자의 RangeType 기준) | 완료 |
| Ability | 특성이 주는 피해 (가시, 복수) | 완료 |
| Curse / Rage | DLJ 유언 시스템 | **미적용** |

`Curse`가 채워지지 않아 **"저주 영향 무효" 특성은 아직 만들 수 없다.** DLJ 쪽에서 `DamageData.Create(giver, damage, LSO_DamageSource.Curse)`로 한 줄만 바꾸면 열린다.

출처를 지정하지 않은 `DamageData`는 `Unknown`이 되므로 기존 호출부는 영향받지 않는다.

---

## 5. 사망 처리 규칙

**기물을 죽일 때 `Destroy`를 직접 부르지 말 것.** 반드시 사망 창구를 거친다.

```csharp
context.Deaths?.Kill(victim, killer);
```

`LDY_DeathHandler.Kill()`이 순서대로 처리한다.

1. 보드 격자에서 제거
2. 죽는 본인의 `LSO_IOnDeath` 특성 호출
3. 적 사망 이벤트 발행 (`IOnEnemyDead`)
4. 유언 발동
5. 오브젝트 파괴 (유언이 보류를 요청하면 생략)

같은 기물이 두 번 처리되지 않도록 내부에서 막고 있다. 가시와 복수가 서로를 죽이는 상황에서도 각각 한 번씩만 처리된다.

> **씬 배선**: 빈 GameObject에 `LDY_DeathHandler`를 붙여 씬에 배치해야 한다.
> 없으면 `LDY_AttackSystem`이 예전 방식으로 폴백하지만, 특성으로 인한 사망(허약·복수·가시 연쇄)은 처리되지 않는다.

---

## 6. 새 특성 만드는 순서

### 1) 특성 클래스 작성

`Assets/_Scripts/LSO/Ability/LSO_이름.cs`

```csharp
using _Scripts.LDY;
using _Scripts.LSO.HealthSystem;

namespace _Scripts.LSO.Ability
{
    /// <summary>방어: 받는 피해를 1 줄인다.</summary>
    public class LSO_Guard : LSO_IAbility, LSO_IDamageModifier
    {
        public int Priority => 0;

        public int ModifyIncomingDamage(DamageableResources target, DamageData data, int damage)
        {
            return damage - 1;
        }
    }
}
```

`LSO_IAbility`는 반드시 함께 구현한다. 이게 있어야 특성으로 인식된다.

### 2) enum에 추가

`LSO_AbilityType.cs`

```csharp
public enum LSO_AbilityType
{
    None, Immune, Double, Test, Sturdy,
    Dodge, Bloodlust, PackTactics, Thorns, Vengeance, Frail,
    Guard,      // 추가
}
```

기존 항목 사이에 끼워넣지 말 것. 이미 저장된 SO의 값이 밀린다.

### 3) 팩토리에 등록

`LSO_AbilityFactory.cs`

```csharp
{ LSO_AbilityType.Guard, () => new LSO_Guard() },
```

**완성된 인스턴스가 아니라 생성 방법(`Func`)을 등록한다.** 인스턴스를 공유하면 발동 여부나 누적치 같은 상태가 모든 기물에 섞인다.

### 4) 에셋 지정

AnimalSO의 Ability 항목을 새 값으로 지정한다. `LDY_Animal` 인스펙터에서 연결 상태를 확인할 수 있고, 재생 중에는 실제 생성된 특성 인스턴스 목록도 보인다.

---

## 7. 구현된 특성

| enum | 클래스 | 효과 | 구현 방식 |
|---|---|---|---|
| `Sturdy` | `LSO_Sturdy` | 첫 즉사 피해에서 HP 1로 생존 | `LSO_IDamageModifier` (Priority 1000) |
| `Dodge` | `LSO_Dodge` | 67% 확률로 피해 완전 회피 | `LSO_IDamageModifier` (Priority -1000) |
| `Thorns` | `LSO_Thorns` | 근접 피격 시 공격자에게 1 피해 | `LSO_IOnHit` + 컨텍스트 |
| `Vengeance` | `LSO_Vengeance` | 죽을 때 처치자에게 1 피해 | `LSO_IOnDeath` + 컨텍스트 |
| `Frail` | `LSO_Frail` | 자기 팀 턴마다 60% 확률로 사망 | `IOnTurnStart` + 컨텍스트 |
| `Bloodlust` | `LSO_Bloodlust` | 적 처치마다 ATK +1 (최대 +3) | `IOnEnemyDead` + `IStatModifier` |
| `PackTactics` | `LSO_PackTactics` | 인접한 같은 종 아군 1기당 ATK +1 | `IStatModifier` + 컨텍스트 |

`LSO_PackTactics`는 "늑대"를 직접 지목하지 않고 **같은 AnimalSO를 가진 인접 아군**을 센다. 늑대 카드에 붙이면 결과적으로 "인접 늑대 수"가 된다.

`LSO_Frail`은 자기 팀 턴에만 판정한다. 양 팀 턴마다 굴리면 실효 사망률이 60%가 아니라 84%가 된다.

---

## 8. 아직 못 만드는 특성

| 특성 | 막는 요인 |
|---|---|
| 저주 영향 무효 | DLJ가 `LSO_DamageSource.Curse`를 채워야 함 (한 줄) |
| 유언 효과 2배 | 유언에 "위력" 개념 자체가 없음 |
| 코스트 축적 / 반환 | 런타임 코스트 자원 시스템 없음 |
| 조개 획득 → 아군 회복 | 아이템·자원 시스템 없음 |
| 스택 부화 → 드래곤 | 기물 변신·교체 시스템 없음 |
| 조종 불가 | `LDY_SelectionController`에 선택 판정 훅 필요 |

---

## 9. 주의사항 모음

- **특성은 개체마다 새 인스턴스여야 한다.** 팩토리에 `new`한 객체를 직접 등록하면 모든 기물이 상태를 공유한다.
- **`Destroy`를 직접 부르지 말 것.** 사망 창구를 거치지 않으면 보드에 유령 기물이 남고 유언이 발동하지 않는다.
- **`Priority` 값을 정할 때 다른 수정자와의 순서를 먼저 생각할 것.** 특히 무효화 계열은 낮게, 하한선 계열은 높게.
- **`AnimalSO`가 비어 있으면 특성이 아예 생성되지 않는다.** `LDY_Animal.Init()`이 조기 반환하기 때문이다.
- **enum 값은 뒤에만 추가할 것.** 중간 삽입은 기존 에셋의 값을 밀어버린다.
