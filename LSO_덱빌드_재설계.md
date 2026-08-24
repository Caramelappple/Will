# 덱 빌드 시스템 재설계안

작성: 이시온 (LSO) · 2026-08-12
범위: 덱 구성 화면의 데이터 계층. UI 연출과 씬 배선은 포함하지 않는다.

## 전제

새 파일은 전부 `LSO_` 접두사로 만든다. 기존 KTH 파일은 남겨두고 나중에 전환한다.

받은 기획 규칙:

| 항목 | 값 |
| --- | --- |
| 덱 최대 장수 | **8** |
| 덱 최소 장수 | 미정 (노션에 없음) |
| 덱 내 중복 | **허용** |
| 카드 획득 | **무제한** |
| 해금 | **이번 작업에서 건드리지 않음** |
| 도감 표시 | **스크롤 목록** (페이지 넘김 없음) |
| 도감 칸 | **보유 장수만큼** (곰 3장 보유 → 곰 칸 3개) |
| 조작 | **누르면 선택, 다시 누르면 취소** (토글) |

---

## 1. 이 규칙이 설계에 미치는 영향

### 1-1. 도감 칸 하나 = 보유한 카드 한 장

조작이 토글(누르면 선택, 다시 누르면 취소)이므로 칸마다 상태가 **켜짐/꺼짐 둘뿐**이다. 종류별로 한 칸만 두면 같은 카드를 두 장 넣을 방법이 없다.

그래서 **보유한 장수만큼 칸을 만든다.** 곰 3장을 갖고 있으면 곰 칸이 3개 나오고, 셋 다 누르면 덱에 곰 3장이 들어간다.

`ItemLibraryManager`가 이미 그 형태다.

```csharp
// ⭐ 중복 체크 제거: 동일 카드라도 리스트에 계속 추가됨
unlockedPieces.Add(card);
```

**보유 목록을 그대로 쓰면 된다.** 종류별로 추리는 계층이 필요 없다.

### 1-2. 그래서 덱은 "고른 칸의 집합"이다

카드 목록이 아니라 **도감 칸 번호의 집합**으로 들고 있는 것이 토글과 맞는다.

```
도감  [0]곰 [1]곰 [2]곰 [3]늑대 [4]여우 ...
선택   ✓        ✓          ✓
덱    곰, 곰, 늑대
```

칸 번호로 들고 있으면 "이 칸이 켜져 있나"를 바로 답할 수 있다. 카드 목록으로 들고 있으면 곰 셋 중 어느 칸이 켜진 것인지 알 수 없어 화면을 그릴 수 없다.

밖으로 내보낼 때만 카드 목록으로 펼친다.

### 1-2. 유일한 제약은 8장이다

검사할 것이 하나뿐이라 규칙 객체가 아주 얇아진다. 카드당 매수 제한, 보유 수량 검사, 미보유 카드 검사가 전부 빠진다.

### 1-3. 페이지 개념을 통째로 없앤다

덱은 최대 8장이라 한 화면에 들어가고, 도감은 스크롤로 간다. **양쪽 다 페이지가 필요 없다.**

지금 페이지에 묶여 있는 것이 생각보다 많다.

| 대상 | 페이지에 묶인 부분 |
| --- | --- |
| `Pagination` | 클래스 전체 |
| `KTH_FixedPageConstraint` / `IPageConstraint` | 클래스 전체 |
| `KTH_Pagination` | 빈 껍데기 (원래 안 쓰임) |
| `KTH_BuildUi` | `nextButton` · `prevButton` · `pageText` · `OnClickNextPage` · `OnClickPrevPage` · `UpdatePageButtons` · `UpdatePageText` |
| `KTH_DeckBuildManager` | `CardPageBucket` · `pageBuckets` · `InitPageBuckets` · `GetCardsAtPage` · `TotalPages` · `ReturnCardToPage` · `RemoveCardFromPage` |
| `KTH_SelectCardUi` | `OriginalPageIndex` · `Setup`의 `pageIndex` 인자 |
| 드롭 핸들러 두 곳 | `buildUi.CurrentPage - 1` |

**덤으로 버그 하나가 사라진다.**

```csharp
// KTH_DeckBuildManager.ReturnCardToPage
// [핵심] 현재 페이지 자리가 꽉 찼으면 다른 페이지로 넘기지 않고 실패 처리 (인벤토리 원복용)
if (targetBucket.Count >= _itemsPerPage) return false;
```

덱에서 도감으로 카드를 되돌릴 때 **지금 보고 있는 페이지가 꽉 차 있으면 실패한다.** 카드가 손에서 튕겨 제자리로 돌아간다. 스크롤 목록이면 "지금 보는 페이지"라는 개념이 없어 이 실패 모드 자체가 없다.

---

## 2. 지금 구조에서 뒤집어야 하는 것

### 2-1. 덱 정본이 둘이다

```csharp
// KTH_Inventory.OnDrop
if (buildManager.RemoveCardFromPage(currentPageIndex, cardUi.CardData))
{
    finalCardList.AddCard(cardUi.CardData);   // ← 두 번째 목록
    cardUi.MoveToInventory(transform);
}
```

`KTH_DeckBuildManager.selectedCards`와 `KTH_FinalCardList.finalSelectedCards`를 손으로 맞춘다. 한쪽만 실패하는 경로가 생기면 조용히 어긋난다.

**`ConfirmDeck`은 `selectedCards`를 검사하는데 전투 씬은 `FinalSelectedCards`를 읽는다.** 검사한 목록과 쓰이는 목록이 다르다.

### 2-2. 이름이 뒤집혀 있다 — 읽을 때 주의할 것

| 코드 이름 | 실제 의미 |
| --- | --- |
| `KTH_Inventory` | 덱 패널 |
| `KTH_DeckBuildManager.pageBuckets` | 보유 카드(도감) |
| `deckLayoutArea` | 도감 영역 |
| `selectedCards` | 도감에서 빼낸 것 |

> **기존 파일의 이름은 바꾸지 않는다.** 담당이 갈린 상태에서 남의 파일을 개명하면 씬·프리팹 참조와 팀원 diff가 따라온다.
>
> 위 표는 기존 코드를 읽을 때 헷갈리지 말라는 안내다. **새로 만드는 LSO 파일에서만 도감 = Palette, 덱 = Deck으로 용어를 맞춘다.**

### 2-3. 세이브 경로가 끊겨 있다

`KTH_DeckDataPersistent` 클래스가 삭제됐는데 `LDY_DeckSaveGateway`가 그것을 쓰고 있어서, 본문이 통째로 `/* */` 주석 처리돼 있다. `LDY_TestScene`·`DLJ_WillUIScene`에는 missing script 항목이 남아 있다.

**세이브 자료형은 이미 맞는 모양이다.**

```csharp
// LDY_RunSaveData
// 덱은 같은 카드가 여러 장 들어가는 목록이므로 수량으로 접지 않고 그대로 나열한다.
public List<string> deckCardIds = new();
```

중복 허용 규칙과 정확히 맞는다. **세이브 스키마는 손대지 않는다.**

---

## 3. 새 구조

```
ItemLibraryManager (기존, 건드리지 않음)
        │ 보유 카드 목록
        ▼
LSO_CardPalette          고를 수 있는 카드 종류 (중복 제거)
        │
        ▼
LSO_DeckDraft            편집 중인 덱  ←── LSO_DeckRulesSO (최대 8장)
        │
        │ Confirm
        ▼
LSO_RunDeck              확정된 덱 (정본, 씬을 넘어감)
        │
   ┌────┴────────────────┬──────────────────┐
   ▼                     ▼                  ▼
KTH_DeckManager   LDY_DeckSaveGateway    덱 표시 UI
 (전투 드로우)         (세이브)
```

**정본은 `LSO_RunDeck` 하나다.** 편집 중인 목록은 덱빌드 화면에서만 살아 있고, 확정할 때 한 번에 넘긴다. 두 목록을 동시에 유지하는 구간이 없다.

---

## 4. 스크립트 목록

### 새로 만들 것 — 7개

| # | 파일 | 종류 | 줄 수(예상) | 책임 |
| --- | --- | --- | --- | --- |
| 1 | `LSO_DeckRejectReason.cs` | enum | ~10 | 거절 이유 |
| 2 | `LSO_DeckValidation.cs` | struct | ~25 | 검사 결과 + 이유 + 숫자 |
| 3 | `LSO_DeckRulesSO.cs` | ScriptableObject | ~20 | 최대·최소 장수 |
| 4 | `LSO_CardPalette.cs` | 순수 C# | ~35 | 도감 칸 목록. 보유 목록을 그대로 번호 매겨 들고 있는다 |
| 5 | `LSO_DeckDraft.cs` | 순수 C# | ~70 | 켜진 칸의 집합. 토글·조회·검사 |
| 6 | `LSO_RunDeck.cs` | MonoBehaviour (DDOL) | ~60 | 확정된 덱 보관. 세이브와 전투가 읽는 정본 |

폴더는 `_Scripts/LSO/Deck/` 아래에 둔다. 카드 데이터(`LSO_CardSO`)가 이미 `LSO/Deck/Data/`에 있다.

**5·6번을 MonoBehaviour로 만들지 않는 것이 핵심이다.** 순수 C#이면 씬 없이 테스트할 수 있고 `FindAnyObjectByType`이 필요 없어진다.

### UI 쪽 — 뒤집는 범위에 따라

| 파일 | 종류 | 책임 |
| --- | --- | --- |
| `LSO_DeckBuildController.cs` | MonoBehaviour | UI 입력을 Draft 조작으로 옮기는 다리. 확정 버튼 처리 |
| `LSO_PaletteScrollView.cs` | MonoBehaviour | **위** — 도감 목록을 스크롤 Content에 채운다 |
| `LSO_PaletteCardView.cs` | MonoBehaviour | 도감 칸 하나. 슬롯 번호를 들고 있고 체크 표시를 켜고 끈다 |
| `LSO_DeckStripView.cs` | MonoBehaviour | **아래** — 고른 카드 8칸을 그린다 |
| `LSO_DeckSlotView.cs` | MonoBehaviour | 덱 칸 하나. 비어 있거나 카드 하나 |

카드 뷰 둘은 `Button` 하나에 이미지를 채우고 **자기 슬롯 번호와 함께** 클릭을 컨트롤러로 넘기는 정도다. 드래그를 버렸으므로 `IBeginDragHandler` 계열을 구현하지 않는다.

### 화면 구성

```
┌─────────────────────────────────────┐
│  고를 수 있는 카드 (스크롤)          │
│  ┌────┐┌────┐┌────┐┌────┐          │
│  │곰 ✓││곰  ││곰 ✓││늑대│          │  ScrollRect
│  └────┘└────┘└────┘└────┘          │  + GridLayoutGroup
│  ┌────┐┌────┐┌────┐┌────┐          │  + ContentSizeFitter
│  │여우││여우││매  ││뱀 ✓│          │
│  └────┘└────┘└────┘└────┘          │
│              ⋮                       │
├─────────────────────────────────────┤
│  고른 카드          3 / 8            │
│  [곰][곰][뱀][ ][ ][ ][ ][ ]        │  고정 8칸
└─────────────────────────────────────┘
```

**아래는 8칸을 미리 깔아둔다.** 빈 칸이 보이면 몇 장 더 넣을 수 있는지가 숫자 없이도 읽힌다. 목록을 늘였다 줄였다 하지 않으므로 레이아웃이 튀지도 않는다.

**아래 칸의 순서는 도감 순서를 따른다.** 고른 순서로 쌓으면 하나를 취소했을 때 뒤의 것들이 앞으로 당겨지면서, 방금 누르려던 자리에 다른 카드가 와 있게 된다. 도감 순서로 고정하면 취소해도 나머지가 제자리에 남는다.

**스크롤 자체는 코드가 필요 없다.** Unity의 `ScrollRect` + Content에 `GridLayoutGroup` + `ContentSizeFitter`(Vertical: Preferred Size)면 배치와 스크롤이 끝난다. `LSO_PaletteScrollView`는 목록이 바뀔 때 카드 뷰를 다시 채우는 일만 한다.

### 안 만드는 것

| 후보 | 왜 뺐나 |
| --- | --- |
| `LSO_CardCollection` (보유 장수 조회) | 획득이 무제한이라 보유 장수가 제약이 아니다 |
| 카드당 매수 제한 | 중복이 자유롭다 |
| 페이지네이션 일체 | 덱은 8장, 도감은 스크롤 |
| 오브젝트 풀링 / 가상 스크롤 | 카드 종류가 수백 개가 되기 전에는 과하다 |
| 세이브 자료형 | `LDY_RunSaveData.deckCardIds`가 이미 맞는 모양 |

### 그대로 둘 것

**기존 KTH 파일은 지우지도 이름을 바꾸지도 않는다.** 새 경로가 자리를 잡으면 쓰이지 않게 될 뿐이고, 실제로 정리할지는 그때 KTH와 정한다.

| 파일 | 새 구조에서의 자리 |
| --- | --- |
| `KTH_FinalCardList` | `LSO_RunDeck`이 정본을 맡으면 쓰이지 않게 됨 |
| `KTH_DeckBuildManager` | 〃 (`LSO_DeckDraft` + `LSO_CardPalette`가 대신함) |
| `KTH_Inventory` | 〃 (`LSO_DeckBuildController`가 대신함) |
| `KTH_Pagination` / `Pagination` | 스크롤로 가면 안 쓰임. 그대로 둠 |
| `KTH_FixedPageConstraint` / `IPageConstraint` | 〃 |
| `KTH_SelectCardUi` · `KTH_BuildUi` · `CardPageBinder` · `IDrage` | 〃 (드래그 방식을 유지할지에 따라 갈림) |

전투·공용 쪽은 애초에 대상이 아니다. `KTH_DeckManager` · `KTH_SpawnCard` · `KTH_HandCard` · `KTH_HandCardLayout` · `KTH_DrawButton` · `KTH_InfoPanl` · `KTH_DiscardCardUI` · `KTH_DiscardCardManager` · `KTH_DeckUi` · `KTH_StartCardSet` · `ItemLibraryManager` 전부 KTH 담당으로 남는다.

### 손대야 하는 남의 파일 — 2곳

담당이 갈렸으므로 직접 고치지 않고 부탁한다.

| 파일 | 담당 | 바꿀 내용 | 시점 |
| --- | --- | --- | --- |
| `KTH_DeckManager.InitDeck()` | KTH | 덱을 읽는 곳을 `LSO_RunDeck.Cards`로 (한 줄) | **가능한 빨리** |
| `LDY_DeckSaveGateway` | LDY | 주석 해제 + 홀더를 `LSO_RunDeck`으로 | 덱빌드가 동작한 뒤 |

**`KTH_DeckManager` 한 줄을 먼저 처리해야 한다.** 이 파일은 지금 활발히 커지는 중이라(직전 pull에서 +140줄), 늦출수록 충돌 확률이 올라간다. 이 한 줄만 바뀌면 그 뒤로 KTH가 그 파일을 아무리 고쳐도 덱을 어디서 읽는지는 안 흔들린다.

---

## 5. 핵심 형태

### 정본은 평평한 목록

```csharp
// LSO_RunDeck
private readonly List<LSO_CardSO> _cards = new();   // 같은 카드가 여러 번 들어간다
```

수량으로 접지 않는 이유는 셋이다.

- 세이브(`deckCardIds`)가 이미 같은 형태다. 변환이 없다
- 전투 드로우는 한 장씩 꺼낸다. 수량 구조는 매번 펼쳐야 한다
- 카드마다 다른 상태(강화, 각인)가 나중에 붙으면 수량으로 접을 수 없다

### 편집 중에는 칸 번호로 들고 있는다

```csharp
public class LSO_DeckDraft
{
    private readonly HashSet<int> _selected = new();   // 켜진 도감 칸 번호

    public int Count => _selected.Count;
    public bool IsSelected(int slot) => _selected.Contains(slot);

    public LSO_DeckValidation Toggle(int slot);        // 켜기/끄기
    public void Clear();

    public IReadOnlyList<LSO_CardSO> ToCards();        // 확정할 때만 펼친다
    public event Action OnChanged;
}
```

**토글이 `Toggle(slot)` 하나로 끝난다.** 켜져 있으면 끄고, 꺼져 있으면 규칙을 확인한 뒤 켠다. 추가와 제거를 나눌 필요가 없다.

칸 번호로 들고 있어야 하는 이유는 화면 때문이다. 곰 3장을 갖고 있고 그중 둘을 골랐을 때, 카드 목록(`[곰, 곰]`)만으로는 **어느 칸에 체크 표시를 그려야 할지 알 수 없다.**

`ToCards()`는 확정하는 순간에만 부른다. 그때부터는 `LSO_RunDeck`이 평평한 카드 목록으로 들고 간다.

### 규칙

```csharp
[CreateAssetMenu(menuName = "LSO/Deck/Rules")]
public class LSO_DeckRulesSO : ScriptableObject
{
    [Tooltip("덱에 넣을 수 있는 최대 장수.")]
    public int maxCards = 8;

    [Tooltip("확정에 필요한 최소 장수. 기획 미정이라 1로 둔다.")]
    public int minCards = 1;
}
```

**8을 코드에 박지 않고 에셋으로 두는 이유**는 노션에 "나중에 바뀔 일 없음"이라고 적혀 있지 않기 때문이다. 밸런싱에서 바뀌면 에셋만 고치면 된다.

### 검사 결과

```csharp
public enum LSO_DeckRejectReason
{
    None,
    DeckFull,      // 8장을 이미 채웠다
    TooFewCards,   // 확정하기에 모자란다
    NullCard,
}

public readonly struct LSO_DeckValidation
{
    public readonly bool IsValid;
    public readonly LSO_DeckRejectReason Reason;
    public readonly int Value;   // "8장까지" "2장 더 필요" 를 띄우기 위한 숫자
}
```

**bool만 돌려주면 UI가 이유를 다시 계산하게 된다.** 지금 `ConfirmDeck`이 `Debug.LogWarning("덱에 선택된 카드가 없습니다!")` 하나로 끝나는 것도 같은 이유다.

검사 지점은 둘이다.

| 시점 | 검사 | 실패 시 |
| --- | --- | --- |
| 카드 추가 | 8장을 넘는가 | 추가하지 않고 이유 반환 |
| 확정 버튼 | 최소 장수를 채웠는가 | 확정하지 않고 이유 표시 |

---

## 6. 흐름

### 덱 구성 화면

```
진입
 ├ LSO_CardPalette 생성 (ItemLibraryManager를 읽어 종류별로 추림)
 └ LSO_DeckDraft 생성
     └ 이어하기라면 LSO_RunDeck의 현재 덱을 불러와 시작

위(도감) 칸 클릭
 └ Draft.Toggle(slot) → LSO_DeckValidation
     ├ 켜져 있었으면 → 끈다 (항상 성공)
     ├ 꺼져 있고 8장 미만 → 켠다
     └ 꺼져 있고 8장 참 → 실패(DeckFull), "8장까지 넣을 수 있습니다"

아래(고른 카드) 칸 클릭
 └ 같은 Draft.Toggle(slot) — 그 칸이 어느 도감 슬롯에서 왔는지 들고 있으므로

어느 쪽을 눌렀든
 └ Draft.OnChanged
     ├ LSO_PaletteScrollView → 체크 표시 갱신
     └ LSO_DeckStripView    → 8칸 다시 그림

확정 버튼
 └ Draft.Validate(rules)
     ├ 실패 → 이유 표시하고 멈춤
     └ 성공 → LSO_RunDeck.Commit(draft)
               └ 씬 전환은 LSO_SceneLoader로
```

**`SceneManager.LoadScene`을 직접 부르지 않는다.** 프로젝트에 `LSO_SceneLoader`가 있고 페이드 연출과 중복 요청 차단이 거기 붙어 있다.

### 전투 씬

```csharp
// KTH_DeckManager.InitDeck() — 한 줄만 바뀐다
deck.AddRange(LSO_RunDeck.Instance.Cards);
```

### 세이브

```
저장  LDY_DeckSaveGateway.Capture()
       └ LSO_RunDeck.Cards → card.Id 를 deckCardIds에 나열

복원  LDY_DeckSaveGateway.Restore()
       └ deckCardIds → LDY_CardCatalogSO.Find(id) → LSO_RunDeck.Restore(cards)
```

게이트웨이는 이미 이 모양으로 짜여 있다. **주석 처리된 본문에서 홀더 이름만 바꾸고 주석을 풀면 된다.**

---

## 7. 이관 순서

기존 화면이 도는 상태를 유지하면서 옮기려면 이 순서가 안전하다.

| 단계 | 할 일 | 담당 | 이 단계가 끝나면 |
| --- | --- | --- | --- |
| 1 | `LSO_RunDeck` 추가 | LSO | 정본이 생김. 아직 아무도 안 읽음 |
| 2 | `KTH_DeckBuildManager.ConfirmDeck` 시점에 `LSO_RunDeck`에도 넣어주는 한 줄 | KTH에 부탁 | 두 경로가 같은 값을 들고 있음 |
| 3 | `KTH_DeckManager.InitDeck`이 `LSO_RunDeck`을 읽게 변경 | KTH에 부탁 | **경계 확정. 이후 KTH가 뭘 고쳐도 안 흔들림** |
| 4 | `LSO_DeckEntry` · `LSO_DeckRejectReason` · `LSO_DeckValidation` · `LSO_DeckRulesSO` 추가 | LSO | 컴파일만 통과 |
| 5 | `LSO_CardPalette` · `LSO_DeckDraft` 추가 | LSO | 데이터 계층 완성. 씬 없이 테스트 가능 |
| 6 | `LSO_DeckBuildController` + 새 덱빌드 화면 | LSO | 새 덱빌드가 동작 |
| 7 | `LDY_DeckSaveGateway` 주석 해제 + 홀더 교체 | LDY에 부탁 | 덱이 저장됨 |

**1~3단계를 먼저 하는 것이 핵심이다.** 이 순서면 내 코드를 한 줄도 쓰기 전에 경계가 먼저 선다. `KTH_DeckManager`가 지금 활발히 커지는 중이라, 나중으로 미룰수록 부탁할 diff가 커진다.

2단계의 한 줄은 안전망이다. 두 경로가 같은 값을 들고 있는 동안에는 3단계에서 문제가 생겨도 되돌릴 자리가 있다.

4단계부터는 전부 내 땅 안이라 KTH 진행과 무관하게 굴러간다.

---

## 8. 스크롤로 갈 때 미리 걸리는 것

### 8-1. 드래그를 버리고 눌러서 추가한다 — 확정

원래 걸림돌은 이것이었다. 지금 도감 카드는 `IBeginDragHandler`를 구현한다.

```csharp
public class KTH_SelectCardUi : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
```

**`ScrollRect` 안에 드래그를 받는 자식이 있으면 스크롤이 안 된다.** Unity는 드래그를 처음 잡은 쪽에만 주기 때문에, 카드를 눌러 위아래로 끌면 스크롤 대신 카드가 딸려 나온다.

**도감 카드를 누르면 덱에 추가, 덱 칸을 누르면 제거**로 간다. 덱이 8칸뿐이라 드래그로 위치를 정할 이유가 없고, 스크롤과 충돌하지도 않는다.

#### 이 결정으로 사라지는 것

| 대상 | 없어지는 부분 |
| --- | --- |
| `KTH_SelectCardUi` | 인터페이스 4개 · `OnBeginDrag` · `OnDrag` · `OnEndDrag` · `OnDrop` · `MarkDroppedSuccess` · `MarkForDestruction` · `ReturnToOriginalPosition` · `MoveToInventory` · `droppedSuccessfully` · `isPendingDestroy` · `originalParent` · `IsInInventory` |
| `IDrage` | 인터페이스 전체 |
| `KTH_DeckBuildManager` | `IDropHandler` · `OnDrop` · `IsInsideDeckLayout` · `deckLayoutArea` |
| `KTH_Inventory` | `IDropHandler` · `OnDrop` |
| `KTH_BuildUi` | `IDropHandler` · `OnDrop` |

**드롭 핸들러가 세 곳에 흩어져 있던 것이 통째로 없어진다.** 지금 그 셋이 서로 `eventData.Use()`로 순서를 다투고 있는데, 그 조율 자체가 사라진다.

#### 더 중요한 것 — 상태가 UI에서 데이터로 옮겨간다

드래그 방식에서는 **"카드 오브젝트가 어느 부모에 붙어 있나"가 곧 상태**였다.

```csharp
// KTH_SelectCardUi
public bool IsInInventory { get; private set; } = false;

public void MoveToInventory(Transform inventoryParent)
{
    IsInInventory = true;
    transform.SetParent(inventoryParent, false);   // ← 부모를 옮기는 것이 "덱에 넣었다"는 뜻
}
```

그래서 데이터 목록(`selectedCards`·`finalSelectedCards`)과 화면 상태를 손으로 맞춰야 했다. **정본이 둘인 문제의 뿌리가 여기다.**

누르는 방식에서는 카드 오브젝트가 부모를 옮기지 않는다. 상태는 `LSO_DeckDraft`에만 있고 도감 뷰와 덱 뷰는 각자 자기 목록을 그리기만 한다.

```
누름 → LSO_DeckDraft.TryAdd(card) → OnChanged → 두 뷰가 다시 그림
```

**화면이 데이터를 따라가지, 데이터가 화면을 따라가지 않는다.**

### 8-2. 고른 카드를 도감에서 빼지 않는다

고른 카드도 도감 자리에 그대로 두고 **체크 표시만 켠다.**

목록에서 빼버리면 스크롤이 튀고, 방금 누른 자리에 다른 카드가 올라온다. 취소하려면 그 카드가 어디로 갔는지 다시 찾아야 한다. 토글 방식에서는 **누른 자리에서 바로 다시 누를 수 있어야** 한다.

`LSO_CardPalette`는 한 번 만들면 런 중에 거의 바뀌지 않는다. 보상으로 새 카드를 얻었을 때만 다시 만들면 된다.

### 8-3. 정렬 순서

스크롤이면 순서가 눈에 들어온다. 지금은 `ItemLibraryManager.unlockedPieces`에 들어온 순서 그대로다.

코스트순이나 이름순이 자연스러운데, `LSO_CardPalette`가 정렬해서 내주면 UI는 손댈 것이 없다. 나중에 정렬 버튼을 붙여도 같은 자리다.

---

## 9. 아직 정해지지 않은 것

**덱 최소 장수.** 노션에 없다. 1로 두고 시작하되, 8장을 다 채워야 시작할 수 있는지 확인이 필요하다. 8칸을 반드시 채우는 규칙이면 `minCards = maxCards = 8`이 되고 확정 버튼 로직이 더 단순해진다.

**새 런과 이어하기.** 새 런은 빈 덱, 이어하기는 저장된 덱이 자연스럽다. 덱빌드 화면이 "새 런인지"를 알아야 하는데, 이건 `LDY_RunEntryState.IsStartingNewRun`이 이미 하려던 일이다.

**리셋 버튼의 의미.** 지금은 도감을 처음 상태로 되돌린다. 새 구조에서는 Draft만 비우는 게 맞아 보인다.

**덱이 8칸을 채웠을 때 도감의 모습.** 아직 안 고른 칸을 회색으로 죽일지, 눌렀을 때만 "8장까지" 안내를 띄울지. `LSO_DeckValidation`이 이유를 돌려주므로 어느 쪽이든 붙는다. 회색으로 죽이면 **이미 고른 칸은 계속 누를 수 있어야 한다** — 취소까지 막히면 8장을 채운 뒤 아무것도 못 바꾸게 된다.

**보유 목록이 런 중에 바뀔 때.** 보상으로 카드를 얻으면 도감 칸이 늘어난다. 칸 번호가 뒤로 붙기만 하면 기존 선택이 그대로 유지되지만, 중간에 끼거나 순서가 바뀌면 **엉뚱한 칸이 켜진 것으로 보인다.** 덱빌드 화면에 들어온 뒤로는 보유 목록이 바뀌지 않는다고 봐도 되는지 확인이 필요하다.

---

## 10. 이 설계가 해결하는 것

| 지금 문제 | 어떻게 사라지나 |
| --- | --- |
| 덱 정본이 둘 | 편집 중에는 Draft 하나, 확정 후에는 RunDeck 하나 |
| 이름이 뒤집힘 | 새 파일만 Palette / Deck으로 고정. 기존 파일은 그대로 둠 |
| 장수 제한 없음 | `LSO_DeckRulesSO` + 두 지점 검사 |
| 실패 이유를 모름 | `LSO_DeckValidation` |
| 도감에 같은 카드가 여러 칸 | `LSO_CardPalette`가 종류별로 추림 |
| 페이지가 꽉 차면 카드 되돌리기 실패 | 페이지 개념이 없어져 실패 모드 자체가 사라짐 |
| 드롭 핸들러 3곳이 순서를 다툼 | 드래그를 버려 `OnDrop`이 전부 없어짐 |
| UI 부모가 곧 상태 | 상태는 Draft에만, 뷰는 그리기만 |
| 세이브 끊김 | RunDeck이 정본이라 게이트웨이가 볼 대상이 생김 |
| `FindAnyObjectByType` 남발 | Palette·Draft가 순수 C# |
| 씬 전환이 페이드를 건너뜀 | `LSO_SceneLoader` 경유 |
