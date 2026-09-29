# KTH CardManager 코드 리뷰

**대상**: `Assets/_Scripts/KTH/CardManager/` 9개 파일 (약 1,050줄)
**작성**: LSO 브랜치 작업 중 구조 점검 (2차)
**요약**: 지난 리뷰에서 가장 크게 지적했던 "카드가 전투와 연결되지 않는다"가 해결됐습니다. 배치 흐름의 완성도도 눈에 띄게 올라갔습니다. 다만 파일이 커지면서 God Class 문제는 심해졌고, 데이터 모델 이중화와 "UI가 곧 데이터"인 구조는 그대로입니다.

---

## 규모

| 파일 | 줄 수 | 역할 |
|---|---:|---|
| `KTH_DeckBuilderManager` | 315 | 덱 편성 화면 전체 |
| `KTH_DeckManager` | 286 | 전투 중 손패 |
| `KTH_CardDragUI` | 155 | 카드 드래그 |
| `KTH_HandCardView` | 121 | 손패 카드 뷰 |
| `KTH_InfoPanelController` | 100 | 상세 정보 패널 |
| `KTH_PlacedUnitView` | 38 | 배치된 유닛 뷰 |
| `KTH_DeckDataPersistent` | 28 | 씬 간 인벤토리 전달 |
| `KTH_CardData` | 22 | 카드 데이터 SO |
| `KTH_InventoryDropArea` | 3 | 드롭 영역 마커 |

지난 리뷰 대비 `KTH_DeckBuilderManager` 245 → **315줄**, `KTH_DeckManager` 151 → **286줄**.

---

## 개선된 부분

### 1. 카드 → 전투 그리드 연결 (가장 큰 진전)

지난 리뷰에서 "플레이어 덱이 전투로 들어가는 다리가 통째로 비어 있다"고 지적했던 부분이 메워졌습니다.

```csharp
// KTH_CardData
[Header("그리드 보드 연동")]
public LSO_CardSO animalCard;

// KTH_DeckManager.PlaceCard
bool started = cardPlacer.BeginPlacement(data.animalCard, LDY_Team.Player, ...);
```

`LDY_CardPlacer`를 통해 실제 보드 격자에 소환됩니다.

### 2. 배치 흐름의 순서가 정확합니다

```csharp
// 코스트 검사를 카드가 손패에서 빠지기 "전에" 한다
if (!cardPlacer.CanAfford(data.animalCard))
{
    Debug.Log("코스트가 부족해 배치할 수 없습니다.");
    return;   // 손패는 그대로
}
```

그리고 배치를 콜백 기반으로 처리해서, **취소하면 카드가 손패에 남습니다.**

```csharp
onPlaced: animal => { ... FinalizeCardPlacement(card, data); },
onCancelled: () => card.SetSelected(false)
```

카드를 먼저 소모하고 나중에 실패를 처리하는 방식이었다면 카드가 사라지는 버그가 났을 겁니다. 실제 손패 제거(`FinalizeCardPlacement`)가 소환 성공 콜백 안에서만 일어나는 게 좋습니다.

### 3. 손패 상한과 부분 드로우

```csharp
int actualDrawCount = Mathf.Min(drawCountPerTurn, remainingSlots);
```

5/6 상태에서 2장을 요청하면 1장만 뽑습니다. 경계 조건을 제대로 처리했습니다.

### 4. 연출 중 중복 입력 차단

`isDrawing`, `isAnimating` 플래그로 연타를 막고, 연출이 끝나면 버튼을 되살립니다. 완료 콜백이 여러 경로로 불릴 수 있는 것도 가드로 막아뒀습니다.

### 5. `DatabaseIndex` 도입

중복 카드가 있어도 어떤 인스턴스인지 구분할 수 있게 인덱스를 들고 다닙니다.

---

## 개선 제안

### 우선순위 상 — 데이터 모델 이중화가 더 나빠졌습니다

**현상**

지난 리뷰 시점에는 `KTH_CardData`와 `LSO_CardSO`가 서로 모르는 남남이었습니다. 지금은 `KTH_CardData`가 `LSO_CardSO`를 **품고 있습니다.**

```csharp
public class KTH_CardData : ScriptableObject
{
    public string cardId;
    public string cardName;      // ← LSO_CardSO.AnimalName 과 중복
    public string description;   // ← LSO_CardSO.Description 과 중복
    public Sprite icon;          // ← LSO_CardSO.Image 와 중복
    public int cost;             // ← LSO_CardSO.Cost 와 중복
    public GameObject unitModelPrefab;   // ← LSO_AnimalSO.unitPrefab 과 중복

    public LSO_CardSO animalCard;   // 그런데 이것도 들고 있음
}
```

**같은 카드의 이름·설명·아이콘·코스트가 두 에셋에 각각 존재합니다.** 둘을 다르게 적어두면 UI는 KTH 값을, 전투는 LSO 값을 씁니다. 이런 불일치는 반드시 발생하고, 발생하면 원인 추적이 매우 어렵습니다.

**제안**

`KTH_CardData`를 얇게 만들고 표시 정보는 `animalCard`에서 읽습니다.

```csharp
public class KTH_CardData : ScriptableObject
{
    public LSO_CardSO animalCard;

    public string CardName   => animalCard != null ? animalCard.AnimalName : "";
    public string Description=> animalCard != null ? animalCard.Description : "";
    public Sprite Icon       => animalCard != null ? animalCard.Image : null;
    public int    Cost       => animalCard != null ? animalCard.Cost : 0;
}
```

이러면 원본이 하나로 정리되고, 기획 수치를 한 곳에서만 고치면 됩니다. 중간 단계로는 기존 필드를 남기되 `[Obsolete]`를 붙이고 새 프로퍼티로 점진 이전하는 방법도 있습니다.

장기적으로는 `KTH_CardData`를 없애고 `LSO_CardSO`로 통일하는 것이 목표겠지만, 그건 팀 합의가 필요한 규모입니다.

### 우선순위 상 — UI 계층이 여전히 데이터 저장소입니다

**현상**

```csharp
public List<KTH_CardData> GetCurrentInventoryCardData()
{
    foreach (Transform child in inventoryContainer)   // ← 하이라키를 순회
    {
        var cardUI = child.GetComponent<KTH_CardDragUI>();
        if (cardUI != null && cardUI.CardData != null)
            inventoryList.Add(cardUI.CardData);
    }
}
```

"지금 덱에 무엇이 있는가"의 정답이 **오브젝트 부모 관계**에 들어 있습니다. 드래그는 `SetParent`만 하고 데이터는 건드리지 않습니다.

이 구조에서는 다음이 전부 불가능하거나 깨집니다.

- 스크롤뷰·필터·정렬 도입 (하이라키 순서가 논리 순서와 달라짐)
- 카드 종류별 최대 장수 제한
- UI 없이 덱 상태 검증하기 (테스트 불가)
- UI를 끄고 백그라운드에서 덱 다루기

**제안**

덱 상태는 순수 데이터가 갖고, UI는 그것을 그립니다.

```csharp
private readonly List<KTH_CardData> _inventory = new();

public void AddToInventory(KTH_CardData card) { _inventory.Add(card); RefreshInventoryUI(); }
public void RemoveFromInventory(KTH_CardData card) { _inventory.Remove(card); RefreshInventoryUI(); }
public IReadOnlyList<KTH_CardData> Inventory => _inventory;
```

드롭 처리에서 `SetParent`와 함께 이 목록을 갱신하면 됩니다.

참고로 LSO 쪽에 이미 `LSO_CardCollection`(보유 카드)과 `LSO_Deck`(뽑을 더미·손패·버린 더미)이 만들어져 있는데 **KTH에서 아무도 쓰지 않습니다.** 바퀴를 다시 만들기 전에 재사용을 검토할 만합니다.

### 우선순위 상 — God Class

**`KTH_DeckManager` (286줄)** 가 담당하는 것

1. 카드 데이터베이스 로드 2. 드로우 규칙 3. 손패 상한 관리 4. 손패 좌표 계산
5. DOTween 등장·재정렬 연출 6. 선택 상태 7. 배치 흐름 조율 8. InfoPanel 제어 9. 버튼 활성화

**`KTH_DeckBuilderManager` (315줄)** 가 담당하는 것

1. 카드 풀 생성 2. 페이지네이션 3. 키보드 입력 4. 인벤토리 조회 5. 리셋 연출
6. 레이아웃 강제 갱신 7. 씬 전환 8. 저장

**제안**

가장 효과가 큰 분리는 **연출**입니다. 두 클래스 모두 DOTween 코드가 절반 가까이 차지하는데, 이건 게임 규칙과 무관합니다.

```
KTH_DeckManager        →  손패 규칙 (드로우, 상한, 선택, 배치)
KTH_HandLayout         →  좌표 계산 + 정렬 연출
KTH_CardAnimator       →  등장·회전·이동 연출
```

DLJ 유언 시스템이 System(규칙)과 Effect(연출)를 분리해서 좋은 결과를 낸 선례가 있습니다.

### 우선순위 중 — 덱 개념이 여전히 없습니다

```csharp
int randomIndex = Random.Range(0, cardDatabase.Count);
drawn.Add(cardDatabase[randomIndex]);
```

덱에서 뽑는 것이 아니라 **카드 DB에서 무작위로 복제**합니다. 뽑을 더미·셔플·버린 더미가 없으므로 다음이 성립하지 않습니다.

- 덱에 넣은 카드가 반드시 나온다는 보장
- 같은 카드가 덱에 든 장수만큼만 나오는 제한
- "덱을 다 돌면 버린 더미를 섞는다"는 로그라이크 기본 규칙

`LSO_Deck`이 이미 이 기능을 갖추고 있습니다(Fisher-Yates 셔플, 소진 시 버린 더미 재활용, 부분 드로우). 연결만 하면 됩니다.

### 우선순위 중 — 레거시 경로가 남아 분기가 복잡합니다

```csharp
if (cardPlacer != null && data.animalCard != null)
{
    // 새 경로: 그리드 배치
    return;
}

// cardPlacer/animalCard 연결이 없는 예전 경로: 그냥 즉시 연출용 배치만 한다.
FinalizeCardPlacement(card, data);
```

두 경로가 공존하면 "왜 어떤 카드는 보드에 안 올라가지?"를 추적하기 어렵습니다. 게다가 예전 경로는 **코스트를 소모하지 않고** 카드만 사라집니다.

마이그레이션이 끝났다면 예전 경로를 제거하고, 아직이라면 최소한 경고 로그를 남기는 편이 좋습니다.

```csharp
Debug.LogWarning($"{data.cardName}: animalCard가 비어 있어 그리드에 소환되지 않습니다.", data);
```

### 우선순위 중 — 뷰가 매니저를 역참조합니다

```csharp
view.Setup(drawn[i], this);        // 뷰에 매니저를 넘김
manager.SelectCard(this);          // 뷰가 매니저를 호출
```

뷰가 매니저의 구체 타입을 알고 있어 양방향 의존입니다. 정작 같은 파일 안에 더 나은 패턴이 이미 있습니다.

```csharp
infoPanel.Show(card.GetData(), true, () => PlaceCard(card));   // 콜백 전달
cardPlacer.BeginPlacement(..., onPlaced: ..., onCancelled: ...);  // 콜백 전달
```

손패 뷰도 같은 방식으로 통일하면 됩니다.

### 우선순위 하 — `KTH_DeckBuilderManager` 싱글톤에 중복 방어가 없습니다

```csharp
private void Awake()
{
    Instance = this;   // 조건 없이 덮어씀
}
```

같은 폴더의 `KTH_DeckDataPersistent`는 중복을 막고 있어 패턴이 엇갈립니다. LSO의 `MonoSingleton<T>`를 상속하면 이 처리와 종료 시점 안전장치가 함께 들어옵니다.

### 우선순위 하 — 네임스페이스 부재

9개 파일 전부 전역 네임스페이스입니다. LSO와 LDY는 정리가 끝나 KTH와 DLJ만 남았습니다. `namespace _Scripts.KTH.Card` 추가를 권합니다.

---

## 체크리스트

- [ ] `KTH_CardData`의 중복 필드를 `animalCard` 위임 프로퍼티로 교체
- [ ] 인벤토리 상태를 UI 하이라키가 아닌 리스트로 보관
- [ ] `LSO_Deck` / `LSO_CardCollection` 재사용 검토
- [ ] `KTH_DeckManager`에서 연출 분리 (`KTH_HandLayout`, `KTH_CardAnimator`)
- [ ] `KTH_DeckBuilderManager`에서 페이지네이션·연출 분리
- [ ] 드로우를 덱 기반으로 전환 (셔플·버린 더미)
- [ ] 레거시 배치 경로 제거 또는 경고 추가
- [ ] 손패 뷰 → 매니저 역참조를 콜백으로 전환
- [ ] `KTH_DeckBuilderManager` 싱글톤 중복 방어
- [ ] `namespace _Scripts.KTH.Card` 적용
