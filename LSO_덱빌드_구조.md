# 덱 빌드 스크립트 구조

작성: 이시온 (LSO) · 2026-08-12
위치: `Assets/_Scripts/LSO/Deck/`

재설계안이 "무엇을 만들 것인가"였다면, 이 문서는 **"지금 무엇이 있는가"**다. 씬을 배선하거나 나중에 고칠 때 이것을 본다.

---

## 1. 한 문장 요약

**보유한 카드 한 장이 도감 칸 하나가 되고, 그 칸을 눌러 켜고 끄면 덱이 된다.**

---

## 2. 데이터가 흐르는 길

```
ItemLibraryManager.UnlockedPieces        보유 카드 (KTH 소유, 읽기만 함)
        │
        ▼
LSO_CardPalette                          도감 칸 목록
        │                                칸 하나 = 보유 카드 한 장
        ▼
LSO_DeckDraft  ◄──── LSO_DeckRulesSO     켜진 칸 번호 집합
        │                                (최대 8장 / 최소 N장)
        │ Commit
        ▼
LSO_RunDeck                              확정된 덱. 씬을 넘어감
        │
   ┌────┴──────────────────┐
   ▼                       ▼
KTH_DeckManager      LDY_DeckSaveGateway
 (전투 드로우)            (세이브 — 아직 주석 상태)
```

**정본이 하나뿐인 것이 이 구조의 목적이다.** 편집 중에는 `LSO_DeckDraft`, 확정한 뒤로는 `LSO_RunDeck`. 두 목록이 동시에 살아 있는 구간이 없어서 서로 어긋날 자리가 없다.

---

## 3. 데이터 계층 — 6개

| 파일 | 종류 | 하는 일 |
| --- | --- | --- |
| `LSO_CardPalette` | 순수 C# | 보유 목록에 번호를 매겨 도감 칸을 만든다 |
| `LSO_DeckDraft` | 순수 C# | 켜진 칸 번호를 들고 있는다. 토글과 검사 |
| `LSO_DeckRulesSO` | ScriptableObject | 최대·최소 장수 |
| `LSO_DeckValidation` | struct | 조작 결과와 화면에 띄울 문구 |
| `LSO_DeckRejectReason` | enum | 거절 이유 |
| `LSO_RunDeck` | MonoSingleton | 확정된 덱. 전투와 세이브가 읽는 정본 |

**앞의 둘은 MonoBehaviour가 아니다.** 씬 없이 만들 수 있어서 덱 규칙만 따로 시험해볼 수 있고, `FindAnyObjectByType`이 필요 없다.

### LSO_CardPalette

```csharp
public static LSO_CardPalette From(IEnumerable<LSO_CardSO> cards);

public int Count { get; }
public LSO_CardSO this[int slot] { get; }
public bool IsValidSlot(int slot);
```

**넘긴 순서가 곧 칸 번호다.** 그래서 덱을 짜는 동안 보유 목록이 바뀌면 이미 고른 칸이 엉뚱한 카드를 가리킨다.

종류별로 접지 않는 이유는 조작이 토글이기 때문이다. 칸마다 켜짐/꺼짐 두 상태뿐이라, 곰 셋을 한 칸으로 접으면 곰을 두 장 넣을 방법이 없어진다.

### LSO_DeckDraft

```csharp
public LSO_DeckDraft(LSO_CardPalette palette, LSO_DeckRulesSO rules);

public LSO_DeckValidation Toggle(int slot);
public bool IsSelected(int slot);
public IReadOnlyList<int> SelectedSlots { get; }   // 도감 순서로 정렬돼 있다
public List<LSO_CardSO> ToCards();                 // 확정할 때만 펼친다

public int Count { get; }
public int MaxCards { get; }
public bool IsFull { get; }

public LSO_DeckValidation ValidateForConfirm();
public void Clear();
public void SelectAll(IEnumerable<int> slots);     // 이어하기용

public event Action OnChanged;
```

카드 목록이 아니라 **칸 번호**를 들고 있다. 곰 셋 중 둘을 골랐을 때 목록이 `[곰, 곰]`이면 **어느 칸에 체크를 그려야 할지 알 수 없기 때문**이다.

`SelectedSlots`가 도감 순서로 정렬돼 있어서, 아래 덱 줄에서 하나를 취소해도 나머지가 자리를 옮기지 않는다.

### LSO_DeckValidation

```csharp
public bool IsValid { get; }
public LSO_DeckRejectReason Reason { get; }
public int Value { get; }
public string Message { get; }   // "덱은 8장까지 넣을 수 있습니다."
```

bool만 돌려주면 화면이 "왜 안 됐는지"를 다시 계산하게 된다. 숫자까지 실어 보내서 문구를 바로 만들 수 있게 했다.

### LSO_RunDeck

```csharp
public IReadOnlyList<LSO_CardSO> Cards { get; }
public bool HasDeck { get; }

public void Commit(IEnumerable<LSO_CardSO> cards);   // 덱 구성 화면이 확정할 때
public void Restore(IEnumerable<LSO_CardSO> cards);  // 세이브에서 되돌릴 때
public void Clear();
```

`MonoSingleton`이라 `Instance`로 접근하고 `DontDestroyOnLoad`로 씬을 넘어간다.

같은 카드가 여러 번 들어가는 평평한 목록이다. 수량으로 접지 않는 이유는 세이브(`LDY_RunSaveData.deckCardIds`)가 이미 같은 형태이고, 드로우는 어차피 한 장씩 꺼내기 때문이다.

---

## 4. 화면 계층 — 5개

| 파일 | 자리 | 하는 일 |
| --- | --- | --- |
| `LSO_PaletteScrollView` | 위 | 도감 목록을 스크롤 Content에 채운다 |
| `LSO_PaletteCardView` | 위 칸 | 칸 하나. 체크 표시를 켜고 끈다 |
| `LSO_DeckStripView` | 아래 | 덱 칸을 만들어 깔고 채운다 |
| `LSO_DeckSlotView` | 아래 칸 | 칸 하나. 비었거나 카드 하나 |
| `LSO_DeckBuildController` | 배선 | 눌림을 덱 조작으로 옮긴다 |

### 뷰가 스스로 켜지지 않는다

```csharp
// LSO_PaletteCardView
private void HandleClick()
{
    if (Slot < 0) return;

    OnClicked?.Invoke(Slot);   // 알리기만 한다
}
```

눌렸다고 알리기만 하고 체크 표시는 안 켠다. **8장이 차서 거절될 수 있기 때문**이다. 스스로 켜면 데이터는 안 들어갔는데 화면만 켜진 상태가 된다.

판정을 거친 뒤 `SetSelected`로 되돌아온다.

### 덱 칸은 만들어서 깐다

`LSO_DeckStripView`가 `HorizontalLayoutGroup` 아래에 `Slot Count`만큼 칸을 만든다. 씬에 8개를 손으로 배치할 필요가 없다.

빈 칸까지 미리 까는 이유는 둘이다. 몇 장 더 넣을 수 있는지가 숫자 없이도 읽히고, 줄 길이가 변하지 않아 취소할 때 나머지 카드가 자리를 옮기지 않는다.

---

## 5. 클릭 한 번이 도는 경로

```
PaletteCardView.OnClicked(slot)
        ▼
PaletteScrollView.OnSlotClicked
        ▼
DeckBuildController.HandleSlotClicked
        ▼
Draft.Toggle(slot) → LSO_DeckValidation
        ├ 실패 → messageText에 Message 표시
        └ Draft.OnChanged
              ▼
        Controller.Redraw()
              ├ PaletteScrollView.Refresh(draft)     체크 표시 · 누를 수 있는지
              ├ DeckStripView.Refresh(draft, palette) 8칸 다시 그림
              └ confirmButton.interactable            최소 장수 검사
```

**아래 덱 칸을 눌러도 같은 `HandleSlotClicked`로 들어간다.** 덱 칸이 자기가 어느 도감 번호에서 왔는지 들고 있어서, 취소가 한 경로로 모인다.

### 8장을 채웠을 때

```csharp
// LSO_PaletteScrollView.Refresh
view.SetInteractable(selected || !draft.IsFull);
```

아직 안 고른 칸만 꺼지고 **이미 고른 칸은 계속 눌린다.** 안 그러면 8장을 채운 뒤로 덱을 바꾸지 못한다.

---

## 6. 씬 배선

### 프리팹 둘 — `_Prefabs/LSO/`

**PaletteCard.prefab** (위 도감 칸)

```
PaletteCard           Image(배경) + Button + LSO_PaletteCardView
 ├ CardImage          Image          → cardImage
 ├ SelectedMark       체크·테두리     → selectedMark (기본 꺼짐)
 ├ NameText           TMP (선택)     → cardName
 └ CostText           TMP (선택)     → cost
```

**DeckSlot.prefab** (아래 덱 칸)

```
DeckSlot              Image(배경) + Button + LSO_DeckSlotView
 ├ CardImage          Image          → cardImage
 └ EmptyMark          빈 칸 표시      → emptyMark
```

> `cardImage`를 루트가 아니라 자식에 둘 것. 빈 칸일 때 `cardImage.enabled = false`로 끄는데, 루트 Image를 끄면 Button이 레이캐스트 대상을 잃는다.

### 씬 계층

```
DeckBuildCanvas               LSO_DeckBuildController
 ├ PaletteScroll              ScrollRect + LSO_PaletteScrollView
 │   └ Viewport               RectMask2D
 │       └ Content            GridLayoutGroup + ContentSizeFitter
 │                            (PaletteCard가 여기 생김)
 ├ DeckStrip                  LSO_DeckStripView
 │   ├ SlotContainer          HorizontalLayoutGroup
 │   │                        (DeckSlot이 여기 생김)
 │   └ CountText
 ├ ConfirmButton
 └ ResetButton
```

> `HorizontalLayoutGroup`을 `DeckStrip` 자신이 아니라 자식 컨테이너에 붙일 것. `DeckStrip`에 붙이면 `CountText`까지 가로로 줄 세워진다.

### 스크롤 설정

| 대상 | 설정 |
| --- | --- |
| ScrollRect | Horizontal 해제 · Vertical 체크 · Scroll Sensitivity 20~40 |
| Viewport | RectMask2D (Mask가 아니라) |
| Content RectTransform | Anchor `Top Stretch` · Pivot `(0.5, 1)` |
| Content Size Fitter | Horizontal `Unconstrained` · Vertical `Preferred Size` |
| Grid Layout Group | Constraint `Fixed Column Count` |

**Horizontal Fit을 Preferred로 두거나 Grid Constraint를 Flexible로 두면 세로로 안 쌓인다.** 세로 스크롤에서 가장 많이 걸리는 두 지점이다.

### 인스펙터 연결

| 컴포넌트 | 슬롯 | 넣을 것 |
| --- | --- | --- |
| `LSO_PaletteScrollView` | Card Prefab | PaletteCard.prefab |
| | Content | ScrollRect의 Content |
| `LSO_DeckStripView` | Layout Group | SlotContainer |
| | Slot Prefab | DeckSlot.prefab |
| | Slot Count | 8 |
| | Count Text | (선택) |
| `LSO_DeckBuildController` | Rules | 규칙 에셋 |
| | Palette View / Deck View | 위 둘 |
| | Confirm / Reset Button | 버튼 |
| | Message Text | (선택) |
| | Next Scene Name | 전투 씬 이름 |

---

## 7. 설계상 지켜야 할 것

**칸 번호로 들고 있는다.** 카드 목록으로 바꾸면 같은 카드가 여럿일 때 화면을 그릴 수 없다.

**뷰가 스스로 상태를 바꾸지 않는다.** 눌림만 알리고 결과를 받아 그린다. 옛 드래그 방식에서는 "카드 오브젝트가 어느 부모에 붙어 있나"가 곧 상태여서 데이터와 화면을 손으로 맞춰야 했고, 그것이 정본이 둘이던 원인이었다.

**정본은 하나다.** 편집 중엔 Draft, 확정 후엔 RunDeck.

**보유 목록을 덱 구성 중에 바꾸지 않는다.** 칸 번호가 순서에 묶여 있다.

---

## 8. 남은 일

| 항목 | 상태 |
| --- | --- |
| 데이터 계층 | 완료 |
| 화면 계층 | 완료 |
| `KTH_DeckManager` → `LSO_RunDeck` | 완료 |
| 옛 덱빌드 스크립트 삭제 | 완료 |
| 페이지 시스템 삭제 | 완료 |
| `KTH_BuildDeckScene` 새로 구성 | **남음** |
| `LDY_DeckSaveGateway` 주석 해제 | **남음** (LDY 담당) |

### Unity에서 뜰 경고

옛 스크립트를 지웠으므로 다음 두 곳에 missing script가 남아 있다. 새 화면을 만들면서 정리하면 된다.

- `KTH_BuildDeckScene` — `KTH_BuildUi` · `KTH_Inventory` · `KTH_FinalCardList`가 붙어 있던 오브젝트
- `_Prefabs/KTH/Card 1.prefab` — `KTH_SelectCardUi`가 붙어 있던 카드

카드 프리팹의 이미지 배치가 마음에 들면, 그것을 복사해 `LSO_PaletteCardView`를 붙이는 쪽이 새로 만드는 것보다 빠르다.
