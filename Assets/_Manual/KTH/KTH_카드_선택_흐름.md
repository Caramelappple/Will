# KTH 카드 선택 · 소환 흐름

작성: 이시온 (LSO) · 2026-08-12
대상: `Assets/_Scripts/KTH/CardManager/CardDeck/**`, `KTH/Discardpile/**`

`KTH_HandCard`를 시작점으로 잡고, 카드 한 장이 손패에 들어와 보드 기물이 되기까지 지나가는 스크립트를 전부 훑었다.

---

## 1. 한눈에 보는 흐름

```
[게임 시작]
KTH_StartCardSet
  └ 1프레임 대기 후 startingHandCount(5)장 연속 드로우
      │
      ▼
KTH_SpawnCard.SpawnOneCard(bypassDrawLimit: true)
  ├ 손패 가득참 검사        → KTH_HandCardLayout.IsFull
  ├ 턴당 드로우 횟수 검사   → KTH_DeckManager.CanDraw()   (시작 핸드는 건너뜀)
  ├ 카드 뽑기               → KTH_DeckManager.DrawCard()
  ├ 프리팹 생성             → Instantiate(KTH_HandCard)
  ├ 데이터 주입             → KTH_HandCard.Setup(LSO_CardSO)
  ├ 시작 위치               → SetSpawnPosition(드로우 버튼 위치)
  └ 손패 등록               → KTH_HandCardLayout.AddCard()
                                └ UpdateHandLayout() → 부채꼴 배치 + 등장 연출

[플레이 중 드로우]
KTH_DrawButton (클릭)
  └ OnDrawRequested 이벤트
      └ KTH_SpawnCard.SpawnNextCard()  → 위와 동일 (횟수 제한 적용)

[카드 선택]
KTH_HandCard.OnPointerClick
  ├ 이전 선택 카드 해제      → currentSelectedCard.SetSelected(false)
  ├ 선택/해제 토글           → SetSelected(true/false)
  │   └ DOTween: 중앙으로 이동 + 확대 + 회전 정렬 + 최상단 렌더
  └ 정보창 열기/닫기         → KTH_InfoPanl.Instance.StartInfoPanl() / CancleInfoPanl()

[정보창]
KTH_InfoPanl
  ├ SetPanl()      → 아이콘·이름·ATK·HP·코스트·이동·사거리·설명 채우기
  ├ 취소 버튼      → KTH_InfoPanlCancelButton → CancleInfoPanl()
  └ 선택 버튼      → KTH_InfoPanlSelectButton → SelectInfoPanl()

[보드 배치]
KTH_InfoPanl.SelectInfoPanl()
  ├ 정보창 닫기 (보드를 가리면 안 되므로)
  ├ 카드 선택 해제 (아직 소환 확정 아님)
  └ LDY_CardPlacer.BeginPlacement(card, team, onPlaced, onCancelled)
       │
       ├ [성공] 보드 칸 클릭 → LDY_CardPlacer.PlaceCard()
       │     ├ 코스트 차감    → LDY_ActionPointManager
       │     ├ 기물 생성      → LSO_AnimalFactory.Create()
       │     ├ 보드 배치      → LDY_BoardManager.Place()
       │     ├ 유언 선택      → LSO_WillSelection.Request()
       │     └ onPlaced 콜백  → KTH_HandCard.ConsumeAndRearrange(discardPile)
       │           ├ 손패에서 제거 + 재정렬
       │           └ 버린 더미로 날아가는 연출 → KTH_DiscardCardUI.AddToDiscardPile()
       │
       └ [취소] 우클릭 → onCancelled (카드는 이미 손패로 복귀해 있음)

[덱 소진]
KTH_DeckManager.DrawCard()
  └ deck.Count == 0 && autoReshuffleFromDiscard
       └ ReshuffleFromDiscard()
            ├ KTH_DiscardCardUI.ClearAndGetList()
            ├ Fisher-Yates 셔플
            └ 덱에 되돌리기 + OnDeckReshuffled 이벤트
```

---

## 2. 파일별 역할

### 카드 한 장

| 파일 | 역할 |
| --- | --- |
| `KTH_HandCard` | 손패 카드 한 장. 클릭 감지(`IPointerClickHandler`), 선택 연출, 소모·폐기 연출 |
| `KTH_HandCardLayout` | 손패 전체 배치. 부채꼴 좌표 계산, 최대 손패 수 관리 |

`KTH_HandCard`가 들고 있는 데이터는 `LSO_CardSO` 하나다. 표시용 값은 전부 `cardData.Animal`(= `LSO_AnimalSO`)에서 꺼낸다.

**선택 상태는 `static` 필드 하나로 관리된다.**

```csharp
private static KTH_HandCard currentSelectedCard;
```

한 번에 한 장만 선택되는 구조라 클릭할 때 이전 카드를 직접 풀어준다.

### 덱과 드로우

| 파일 | 역할 |
| --- | --- |
| `KTH_DeckManager` | 덱 목록 보관, 드로우, 턴당 횟수 제한, 버린 더미 리셔플 |
| `KTH_DrawButton` | 버튼 클릭을 `OnDrawRequested` 이벤트로 바꿔 전달 |
| `KTH_SpawnCard` | 드로우 요청을 받아 실제 카드 오브젝트를 만들고 손패에 넣음 |
| `KTH_StartCardSet` | 게임 시작 시 시작 핸드 지급 |
| `KTH_DiscardCardUI` | 버린 카드 더미 보관 + 개수 표시 |

덱의 원본은 `KTH_FinalCardList.Instance.FinalSelectedCards`다. 덱빌드 씬에서 확정한 목록을 전투 씬에서 그대로 읽는다.

### 정보창과 소환

| 파일 | 역할 |
| --- | --- |
| `KTH_InfoPanl` | 카드 상세 정보 표시. 소환 시작 지점 |
| `KTH_InfoPanlSelectButton` | 선택 버튼 → `SelectInfoPanl()` |
| `KTH_InfoPanlCancelButton` | 취소 버튼 → `CancleInfoPanl()` |

**`KTH_InfoPanl`이 KTH와 LDY의 경계다.** 여기서 `LDY_CardPlacer.BeginPlacement`를 부르는 순간부터는 LDY 쪽 흐름이다.

---

## 3. 이벤트 연결표

| 발행 | 구독 | 하는 일 |
| --- | --- | --- |
| `KTH_DrawButton.OnDrawRequested` | `KTH_SpawnCard` | 카드 뽑기 |
| `KTH_HandCardLayout.OnHandCountChanged` | `KTH_SpawnCard` | 드로우 버튼 활성/비활성 |
| `KTH_DeckManager.OnDrawLimitChanged` | `KTH_SpawnCard` | 드로우 버튼 활성/비활성 |
| `KTH_DeckManager.OnDeckReshuffled` | (없음) | 리셔플 알림 — 구독자 없음 |
| `KTH_DiscardCardUI.OnCardAdded` | (없음) | 버린 카드 추가 알림 — 구독자 없음 |
| `KTH_HandCard.OnCardClicked` | (없음) | 카드 클릭 알림 — 구독자 없음 |
| `LDY_TurnManager.OnTurnChanged` | `KTH_DeckManager` | 플레이어 턴에 드로우 횟수 리셋 |

---

## 4. 눈에 띈 것

### 4-1. 싱글톤이 `Awake`에서 무조건 자기를 덮어쓴다

```csharp
// KTH_InfoPanl, KTH_HandCardLayout 둘 다
private void Awake()
{
    Instance = this;
}
```

중복 검사도 없고 `OnDestroy`에서 비우지도 않는다. 씬에 둘이 있으면 나중에 `Awake`가 도는 쪽이 이기고, 씬을 나갈 때 파괴된 인스턴스가 `Instance`에 그대로 남는다.

`KTH_HandCard.OnPointerClick`은 `KTH_InfoPanl.Instance`를 null 검사 없이 부른다.

```csharp
KTH_InfoPanl.Instance.StartInfoPanl(cardData, this);
```

정보창이 없는 씬에서 카드를 클릭하면 여기서 NRE가 난다.

### 4-2. `KTH_HandCard.SettingUi`가 null 검사를 하지 않는다

```csharp
public void SettingUi()
{
    cardImage.sprite = cardData.Image;
    title.text = cardData.Animal.animalName;
    ...
}
```

`LSO_CardSO.IsValid`(= `animal != null`)를 확인하지 않아서 동물 데이터가 빠진 카드가 덱에 들어가면 `cardData.Animal.animalName`에서 터진다. `KTH_InfoPanl.SetPanl`은 `IsValid`를 확인하는데 이쪽은 안 한다.

### 4-3. 이름과 하는 일이 어긋나는 곳

`willExplanationText`에 들어가는 값이 유언이 아니라 **특성**이다.

```csharp
var abilityTypes = data.AbilityTypes;
willExplanationText.text = abilityTypes.Count > 0
    ? GetAbilityExplanation(abilityTypes[0])
    : string.Empty;
```

그리고 특성이 여러 개여도 **첫 번째만** 보여준다. 까마귀왕처럼 특성이 셋인 기물은 나머지가 안 보인다.

`TraitExplanationText`에는 특성이 아니라 `data.Description`(동물 설명)이 들어간다. 두 변수의 이름이 서로 바뀐 것으로 보인다.

### 4-4. `GetAbilityExplanation`이 빈 껍데기

```csharp
private string GetAbilityExplanation(LSO_AbilityType type)
{
    return type switch
    {
        _ => type.ToString()
    };
}
```

`switch`에 분기가 하나도 없어 `type.ToString()`과 같다. `WillEnhancement` 같은 영문이 그대로 나온다.

`attackTypeStateText`도 마찬가지로 `data.Range`를 그대로 찍어 `MeleeOrthogonal`이 나온다.

> **LSO 쪽에 `LSO_DisplayNames`를 만들어뒀다.** 기물 정보창이 쓰고 있고 카드창도 같은 표를 쓰면 표기가 갈리지 않는다.
>
> ```csharp
> using _Scripts.LSO.UI;
>
> attackTypeStateText.text = LSO_DisplayNames.Of(data.Range);
> willExplanationText.text = LSO_DisplayNames.Of(abilityTypes[0]);
> ```

### 4-5. 오타가 굳어 있다

`KTH_InfoPanl`(→ Panel), `CancleInfoPanl`(→ Cancel), `KTH_LoadManger`(→ Manager).

클래스 이름이라 고치려면 씬·프리팹 참조가 따라와야 한다. `.meta` guid는 유지되므로 참조는 안 깨지지만 전 팀원 diff가 생긴다. 급하지 않으니 다음 리팩토링 때 같이 다루면 된다.

### 4-6. 사용되지 않는 이벤트 셋

`OnDeckReshuffled` · `OnCardAdded` · `OnCardClicked` 모두 구독자가 없다. 나중에 쓸 자리를 미리 뚫어둔 것으로 보이는데, 지금은 죽은 코드라 정리하거나 용도를 주석으로 남기는 편이 낫다.

### 4-7. 폐기 연출 중 카드가 부모를 옮긴다

```csharp
Transform discardParent = discardPile.DiscardCardTransform.parent;
transform.SetParent(discardParent, worldPositionStays: true);
```

연출 중에 씬이 바뀌거나 부모가 파괴되면 DOTween 시퀀스가 파괴된 트랜스폼을 만진다. `transform.DOKill()`이 `OnDestroy`에 있어 대부분 막히지만, 부모 쪽이 먼저 사라지는 경우는 보장되지 않는다.

### 4-8. `FindAnyObjectByType` 의존

`KTH_SpawnCard` · `KTH_DeckManager` · `KTH_StartCardSet`이 `Awake`에서 참조를 못 찾으면 씬을 훑는다. 시작 시 한 번이라 비용은 문제없지만, **인스펙터 연결이 빠져도 조용히 동작해서** 배선 실수를 못 잡는다.

---

## 5. 카드 선택 관련해서 손댈 때 알아둘 것

- **선택 상태는 `KTH_HandCard.currentSelectedCard`(static) 하나가 정본이다.** 밖에서 선택을 풀려면 `SetSelected(false)`를 부르고 이 필드도 같이 정리해야 한다. `ConsumeAndRearrange`와 `OnDestroy`가 그렇게 하고 있다.
- **선택 해제 시 위치 복원은 `originalLocalPos`·`originalZRotation`에 의존한다.** 이 값은 `MoveToHandPosition`과 `PlayDrawAnimation`에서만 채워진다. 손패에 등록되지 않은 카드를 선택하면 (0,0,0)으로 돌아간다.
- **`MoveToHandPosition`은 선택 중인 카드를 건너뛴다.** 좌표는 갱신하되 이동은 하지 않아서, 다른 카드가 뽑혀도 선택된 카드가 중앙에 머문다.
- **소환 확정 시점은 `LDY_CardPlacer`의 `onPlaced` 콜백이다.** 선택 버튼을 누른 시점이 아니다. 보드 칸을 클릭해야 카드가 손패에서 빠진다.
