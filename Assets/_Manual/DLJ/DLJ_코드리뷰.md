# DLJ 유언 시스템 코드 리뷰

**대상**: `Assets/_Scripts/DLJ/` 10개 파일 (약 900줄)
**작성**: LSO 브랜치 작업 중 구조 점검
**요약**: 유언 시스템 이중화가 해소되고 로직/연출 분리도 잘 돼 있습니다. 다만 유언 종류가 늘어날 때 터질 구조적 부채가 두 군데 있습니다.

---

## 현재 구조

```
LDY_AttackSystem.HandleDeath
  └─ DLJ_IWillActivation.WillActivate()
       └─ DLJ_WillSystem            씬 배선 + 컨텍스트 조립
            └─ LSO_WillFactory.Create(animal.WillType, context)
                 └─ DLJ_CurseSystem / DLJ_RageSystem / DLJ_SuccessionSystem   : LSO_IWill
                      └─ DLJ_CurseEffect / DLJ_RageEffect / DLJ_SuccessionEffect   연출
```

| 파일 | 줄 수 | 역할 |
|---|---:|---|
| `DLJ_CurseSystem` | 208 | 저주 규칙 + 장판 인스턴스 |
| `DLJ_SuccessionSystem` | 177 | 계승 규칙 |
| `DLJ_RageSystem` | 133 | 분노 규칙 |
| `DLJ_SuccessionEffect` | 79 | 계승 연출 |
| `DLJ_WillSystem` | 76 | 유언 진입점 |
| `DLJ_CurseEffect` | 75 | 저주 연출 |
| `DLJ_RageEffect` | 70 | 분노 연출 |
| `DLJ_WillTest` | 59 | 테스트 |
| `DLJ_WillContext` | 22 | 생성 인자 묶음 |
| `DLJ_IWillActivation` | 5 | 발동 계약 |

---

## 잘 된 부분

**1. 유언 인터페이스 통일**

DLJ 계열과 LSO 계열이 따로 존재하던 문제가 해결됐습니다. 세 유언 모두 `LSO_IWill`을 구현하고, `LSO_WillFactory`가 `LSO_WillType` → DLJ 구현체를 만듭니다. LSO는 인터페이스만 알고 구현은 DLJ가 제공하는 형태라 의존 방향이 올바릅니다.

**2. 로직과 연출 분리**

```csharp
// System: 규칙만 계산하고 이벤트 발행
public event Action<Vector3, Vector3, Action<DLJ_CurseSystem>> OnCurseActivated;

// Effect: 구독해서 프리팹 생성 + DOTween
curseSystem.OnCurseActivated += Play;
```

`Unbind()`와 `OnDestroy()`로 구독 해제까지 챙겨져 있어 이벤트 누수가 없습니다.

**3. 파라미터 객체 도입**

`Configure(prefab, time, height, turnManager, board, attackSystem, team)` 같은 긴 인자 목록이 `DLJ_WillContext` 하나로 정리됐습니다.

**4. 팩토리가 생성 방법을 등록하는 방식**

`Dictionary<LSO_WillType, Func<DLJ_WillContext, LSO_IWill>>` 구조라 개체마다 새 인스턴스를 받습니다. 상태를 가진 유언(지속 턴, 발동 횟수)이 서로 섞이지 않습니다.

---

## 개선 제안

### 우선순위 상 — `DLJ_WillContext` 비대화

**현상**

```csharp
public class DLJ_WillContext
{
    public GameObject owner;  public LDY_Animal animal;
    public LDY_BoardManager board;  public LDY_TurnManager turnManager;
    public LDY_AttackSystem attackSystem;

    public GameObject rageObject;        public float rageExpandTime;
    public float rageHoldTime;           public float effectHeight;
    public GameObject curseObject;       public float curseExpandTime;
    public float curseEffectHeight;
    public GameObject successionObject;
}
```

저주 유언 하나를 만들 때도 분노·계승 설정이 전부 실려 옵니다. 유언이 6종이 되면 20개 넘는 필드가 쌓입니다. 같은 문제가 `DLJ_WillSystem` 인스펙터에도 그대로 나타나, 실제로는 하나만 쓰는데 세 벌의 헤더가 모두 노출됩니다.

**제안**

연출 설정을 유언별 ScriptableObject로 분리하고, 컨텍스트에는 공통 참조만 남깁니다.

```csharp
public class DLJ_WillContext
{
    public GameObject owner;
    public LDY_Animal animal;
    public LDY_BoardManager board;
    public LDY_TurnManager turnManager;
    public LDY_AttackSystem attackSystem;
    public DLJ_WillSettingSO setting;   // 유언별 설정은 여기 한 칸으로
}
```

`DLJ_CurseSettingSO`, `DLJ_RageSettingSO` 등을 만들면 기획 수치 조정도 에셋에서 끝나고, 새 유언 추가 시 `DLJ_WillContext`를 건드릴 필요가 없어집니다(OCP).

### 우선순위 중 — `DLJ_CurseSystem`이 두 역할 겸함

**현상**

같은 클래스가 **발동 주체**와 **생성된 장판 인스턴스** 양쪽으로 쓰입니다. 그래서 필드가 두 벌입니다.

```csharp
private LDY_TurnManager activationTurnManager;   // 발동시키는 쪽
private LDY_BoardManager activationBoard;
private LDY_AttackSystem activationAttackSystem;

private LDY_TurnManager effectTurnManager;       // 깔린 장판 쪽
private LDY_BoardManager effectBoard;
private LDY_AttackSystem effectAttackSystem;
```

DLJ에서 가장 긴 208줄이 된 주된 이유입니다.

**제안**

장판을 `DLJ_CurseArea`(가칭)로 분리합니다. `DLJ_CurseSystem`은 "어디에 얼마 크기로 깔지" 판정만, `DLJ_CurseArea`는 "매 턴 데미지, 지속 턴 감소, 만료" 처리만 맡습니다. 각각 100줄 아래로 떨어지고 `activation~`/`effect~` 접두사가 사라집니다.

### 우선순위 중 — `ShouldDeferDestruction`의 위치

**현상**

```csharp
public interface LSO_IWill
{
    void InvokeWill();
    bool ShouldDeferDestruction { get; }   // 계승만 true, 나머지는 항상 false
}
```

이건 유언의 본질(발동)이 아니라 **호출자 사정**(시체를 언제 파괴할지)입니다. 세 구현 중 하나만 의미 있는 값을 반환합니다.

**제안**

선택적 인터페이스로 분리하고 호출부에서 판별합니다 (ISP).

```csharp
public interface LSO_IDeferDestruction
{
    bool ShouldDeferDestruction { get; }
}

// LDY_AttackSystem.HandleDeath
bool defer = will is LSO_IDeferDestruction d && d.ShouldDeferDestruction;
if (!defer) Destroy(target.gameObject);
```

### 우선순위 하 — 네임스페이스 부재

DLJ 10개 파일 전부 네임스페이스가 없어 클래스가 전역에 노출됩니다. LSO와 LDY는 정리가 끝나 DLJ만 남은 상태입니다. `namespace _Scripts.DLJ` 추가를 권합니다. 참조하는 곳이 `LSO_WillFactory`와 `LDY_AttackSystem` 정도라 파급은 작습니다.

### 우선순위 하 — `FindFirstObjectByType` 폴백

```csharp
if (board == null) board = FindFirstObjectByType<LDY_BoardManager>();
if (turnManager == null) turnManager = FindFirstObjectByType<LDY_TurnManager>();
if (attackSystem == null) attackSystem = FindFirstObjectByType<LDY_AttackSystem>();
```

배선을 깜빡해도 동작해서 편하지만, 씬에 매니저가 둘 이상일 때 어느 것을 잡을지 알 수 없고 실수를 조용히 덮습니다. 프로젝트 전체에서 이 패턴을 쓰는 곳이 여기뿐이라 일관성도 떨어집니다. 최소한 폴백이 실제로 발동했을 때 경고 로그를 남기는 편이 좋습니다.

### 우선순위 하 — 런타임 `AddComponent`

세 `Create`가 모두 같은 패턴입니다.

```csharp
system = context.owner.AddComponent<DLJ_CurseSystem>();
effect = context.owner.AddComponent<DLJ_CurseEffect>();
```

동작에는 문제없지만, 규칙 계산만 하는 System이 MonoBehaviour여야 하는지 재검토할 만합니다. System을 순수 C# 클래스로 두고 MonoBehaviour는 Effect만 남기면 기물 GameObject에 컴포넌트가 쌓이지 않아 디버깅이 쉬워집니다.

---

## 체크리스트

- [ ] `DLJ_WillContext`에서 유언별 연출 설정을 SO로 분리
- [ ] `DLJ_WillSystem` 인스펙터에서 사용하지 않는 유언 설정 노출 제거
- [ ] `DLJ_CurseSystem`을 발동 주체와 장판 인스턴스로 분리
- [ ] `ShouldDeferDestruction`을 선택적 인터페이스로 분리
- [ ] DLJ 전체에 `namespace _Scripts.DLJ` 적용
- [ ] `FindFirstObjectByType` 폴백 시 경고 로그 추가
- [ ] System을 순수 C# 클래스로 둘 수 있는지 검토
