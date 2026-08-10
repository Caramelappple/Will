# 유언 선택 시스템 가이드

> 유언을 카드가 정하던 방식에서, 플레이어가 소환할 때 직접 고르는 방식으로 바꾼 작업 정리.

---

## 1. 왜 바꿨나

이전에는 `LSO_CardSO.willType`이 유언을 들고 있었다. 같은 까마귀라도 카드마다 유언이 달라서, 원하는 유언을 쓰려면 그 유언이 붙은 카드를 뽑아야 했다.

기획이 "배치 시 반드시 유언을 선택"으로 바뀌면서 **유언은 카드의 속성이 아니라 소환 행위의 결과**가 되었다.

---

## 2. 전체 흐름

```
카드 선택 → 칸 선택 → 기물 생성 → 보드 배치 → 코스트 차감 → 유언 선택
                          ↑                                    ↑
                  기본 유언이 이미 들어감              플레이어 선택이 덮어씀
```

핵심은 **유언 없는 기물이 존재하는 순간이 없다**는 것이다.

`LDY_Animal.Setup`이 소환 시점에 `AnimalSO.defaultWill`을 넣어두고, 플레이어 선택은 그 위에 덮어쓴다. 고르는 도중에 기물이 죽어도 유언이 정상 발동한다.

---

## 3. 파일 구성

### 새로 만든 것

| 파일 | 역할 |
| --- | --- |
| `LSO/Will/LSO_IWillSelector.cs` | UI가 구현할 계약 |
| `LSO/Will/LSO_WillSelection.cs` | UI를 찾아 쓰는 지점 + 예외 처리 |

### 고친 것

| 파일 | 변경 |
| --- | --- |
| `LSO/Animal/Data/LSO_AnimalSO.cs` | `defaultWill` 필드 추가 |
| `LSO/Deck/Data/LSO_CardSO.cs` | `DefaultWill` 접근자 추가, `willType` 제거 |
| `LDY/LDY_Animal.cs` | `SetWill`, `IsWillChosen` 추가. Setup이 기본값 적용 |
| `LDY/LDY_CardPlacer.cs` | 소환 후 `RequestWill` 호출 |
| `KTH/Reward/KTH_Reward.cs` | 해금된 유언 목록 공급 |

---

## 4. `LSO_IWillSelector` — UI가 구현할 것

```csharp
public interface LSO_IWillSelector
{
    bool IsSelecting { get; }

    void Request(
        LSO_CardSO card,
        IReadOnlyList<LSO_WillType> options,
        Action<LSO_WillType> onSelected,
        Action onCancelled);

    void Abort();
}
```

### 구현 규약

**`onSelected` 또는 `onCancelled` 중 정확히 하나를 반드시 부른다.**

하나도 안 부르면 그 기물은 기본 유언인 채로 남는다. 게임이 멈추지는 않지만 플레이어 선택이 사라진다.

- 둘 중 하나를 부르기 전까지 `IsSelecting`은 `true`를 유지한다
- 이미 선택 중일 때 `Request`가 또 오면 이전 요청을 취소 처리한다
- `Abort`는 `onCancelled`를 부르고 창을 닫는다

### 구현 예시

```csharp
public class LSO_WillSelectPanel : MonoBehaviour, LSO_IWillSelector
{
    private void OnEnable()  => LSO_WillSelection.Register(this);
    private void OnDisable() => LSO_WillSelection.Unregister(this);

    public bool IsSelecting { get; private set; }

    public void Request(card, options, onSelected, onCancelled) { ... }
    public void Abort() { ... }
}
```

---

## 5. `LSO_WillSelection` — 판단이 모인 곳

인터페이스만 있으면 호출부마다 null 검사를 반복하게 되고, 한 곳이라도 빠지면 소환이 멈춘다. 그래서 판단을 여기 모았다.

```
선택지가 1개     → 창 안 띄우고 바로 확정
UI 없음 / 선택지 0 → fallback으로 즉시 확정
그 외             → 창 띄움
```

### 선택지 1개일 때 창을 건너뛰는 이유

게임 초반에는 해금된 유언이 하나뿐이다. 버튼 하나짜리 팝업이 소환마다 뜨면 방해만 된다.

### fallback 경로가 필요한 이유

적 소환, 스테이지 초기 배치, 테스트 씬이 전부 UI 없이 같은 소환 코드를 지나간다.

### 정적 필드 초기화

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
private static void ResetOnLoad()
{
    Current = null;
    UnlockedWillsProvider = null;
}
```

정적 필드는 씬을 다시 로드해도 남는다. 파괴된 UI가 꽂혀 있으면 다음 씬에서 소환이 멈춘다.

---

## 6. 기본 유언 — `AnimalSO.defaultWill`

```csharp
// LDY_Animal.Setup
data = card.Animal;
WillType = card.DefaultWill;   // 태어날 때부터 유효한 값
```

플레이어가 고르지 않는 모든 경로가 이 값을 쓴다.

| 경로 | 유언 결정 |
| --- | --- |
| 플레이어 소환 | 선택 UI 결과 |
| 적 소환 | `defaultWill` (팀 검사로 걸러짐) |
| 스테이지 초기 배치 | `defaultWill` (`RequestWill`을 아예 안 부름) |
| 테스트 씬 (UI 없음) | `defaultWill` |

> **AnimalSO 에셋마다 `Default Will`을 채울 것.**
> 안 채우면 전부 `Curse`(enum 0번)가 된다.

---

## 7. `SetWill`과 `IsWillChosen`

```csharp
public bool IsWillChosen { get; private set; }

public void SetWill(LSO_WillType willType)
{
    if (IsWillChosen)
    {
        Debug.LogWarning($"{name}: 유언이 이미 {WillType}로 정해져 있습니다.", this);
        return;
    }

    WillType = willType;
    IsWillChosen = true;
}
```

`IsWillChosen`은 **"유언이 있는가"가 아니라 "플레이어가 골랐는가"** 를 뜻한다. 유언 자체는 소환 시점부터 항상 있다.

한 번만 먹는 이유는 UI가 콜백을 중복 호출하는 실수를 잡기 위해서다.

---

## 8. 해금 연동

LDY가 KTH를 직접 참조하지 않도록 **함수를 건네는 방식**으로 이었다.

```csharp
// KTH_Reward.Awake
LSO_WillSelection.UnlockedWillsProvider = GetUnlockedWillList;
```

```csharp
// LDY_CardPlacer
IReadOnlyList<LSO_WillType> unlocked = LSO_WillSelection.UnlockedWills;
if (unlocked is { Count: > 0 }) return unlocked;

return FallbackWills;   // 인스펙터 목록 → 비면 전체 5종
```

`KTH_Reward`가 없는 테스트 씬에서도 그냥 돌아간다.

### 캐시

`Unlocks.Wills`는 `HashSet`이라 `IReadOnlyList`로 바로 못 준다. 매번 복사하면 소환할 때마다 할당이 생기므로, 해금 개수가 바뀔 때만 다시 만든다.

### 해제

```csharp
if (LSO_WillSelection.UnlockedWillsProvider == GetUnlockedWillList)
    LSO_WillSelection.UnlockedWillsProvider = null;
```

자기가 등록한 것일 때만 뗀다. 씬 전환 중 새 인스턴스가 먼저 등록했을 수 있다.

---

## 9. 취소에 대하여

**소환 후에 고르는 구조라 취소가 없다.** 기물이 이미 보드에 올라가 있고 코스트도 빠졌기 때문이다.

`Abort`(턴 종료·씬 전환)가 들어와도 기물은 기본 유언인 채로 남을 뿐 문제가 생기지 않는다.

```csharp
// LDY_CardPlacer.HandleTurnChanged
CancelPlacement();
LSO_WillSelection.Abort();   // 적 턴에 창이 떠 있으면 화면이 막힌다
```

---

## 10. 남은 일

### UI 구현

`LSO_IWillSelector`를 구현한 패널이 아직 없다. **지금 실행하면 모든 기물이 `defaultWill`로 확정된다** — 동작은 이전과 같고, UI만 꽂으면 살아난다.

### 기획에서 정할 것

- [ ] 시작 시 해금돼 있는 유언 목록 (지금은 0개 → fallback으로 전체가 나옴)
- [ ] 적 기물별 기본 유언 (AnimalSO 에셋 채우기)
- [ ] 유언 선택 창의 취소 허용 여부 (현재 구조상 취소해도 기본값 확정)
