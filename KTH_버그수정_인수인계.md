# KTH 버그 수정 인수인계

KTH 브랜치. 이 세션에서 고친 버그 3개 + `KTH_HandCardLayout` 분리 1개 + 세션 시작 전부터
커밋 안 된 채 남아있던 남의 미완성 수정 1개(4번, 내가 만든 거 아님). 전부
`Assets/_Scripts/KTH/`, `Assets/_Prefabs/KTH/` 안쪽만 건드렸다 — 남의 폴더는 안 건드림.

**지금 상태: 전부 미커밋.** 유니티 콘솔 에러 0 확인하고 커밋할 것.

```
M  Assets/_Prefabs/KTH/Cylinder.prefab
M  Assets/_Scripts/KTH/CardManager/CardDeck/Discardpile/KTH_DiscardCardUI.cs
D  Assets/_Scripts/KTH/CardManager/CardDeck/CardLayoutCalculator.cs       (+ .meta)
+  Assets/_Scripts/KTH/CardManager/CardDeck/KTH_CardFanLayoutCalculator.cs
+  Assets/_Scripts/KTH/CardManager/CardDeck/HandCard/KTH_HandCardPlacementFlow.cs
+  Assets/_Scripts/KTH/CardManager/CardDeck/HandCard/KTH_HandCardGroupMotion.cs
M  Assets/_Scripts/KTH/CardManager/CardDeck/HandCard/KTH_HandCardLayout.cs   (975→544줄, 아래 5번 참고)
M  Assets/_Scripts/KTH/CardManager/CardDeck/KTH_DeckUi.cs            (내가 안 만짐, 아래 4번 참고)
M  Assets/_Scripts/KTH/Pieces/KTH_PlacementAnimation.cs
```

---

## 1. 기물 배치 후 "내려갔다 다시 뜨는" 이중 동작 — ✅ 코드 수정 완료

**증상**: 기물을 보드에 놓으면 솟았다 내려앉는 연출이 끝나자마자, 커서가 이미 그 위에 있어서
호버 연출이 곧바로 다시 떠서 "한 번 더 뜨는" 것처럼 보임.

**원인**: 배치 연출(rise→settle)이 끝나는 즉시 호버를 재개시키는데, 배치 직후엔 커서가 대부분
그 자리에 있어서 재개와 동시에 또 뜨는 트윈이 걸림.

**고친 것** — `Assets/_Scripts/KTH/Pieces/KTH_PlacementAnimation.cs`
- `settleEase`: `OutBounce` → `OutQuad` (오버슈트로 여러 번 튀어 보이던 것 제거)
- `hoverResumeDelay`(기본 1.5초) 필드 추가 — 착지 후 이만큼 기다렸다가 호버를 재개.
  시퀀스에 `AppendInterval`만 추가한 거라 다른 로직(LDY_BoardManager.Place는 반환값을
  안 기다림)엔 영향 없음.

**⚠️ 유니티에서 확인할 것**: 이 클래스는 `LDY_BoardManager`가 `[SerializeField]`로 들고 있고,
이미 저장된 씬(`Assets/_Scenes/KTH/LSO_TestScene 1.unity`)에 **옛 기본값이 구워져 있다**
(`settleEase: 30`=OutBounce). 코드 기본값만 바꿔서는 이미 저장된 씬/프리팹 인스턴스는 안 바뀐다.
인스펙터에서 `Placement Animation` 아래 `Settle Ease`가 `OutQuad`인지, `Hover Resume Delay`가
채워져 있는지 확인할 것.

---

## 2. 보드-기물 경계에서 호버가 위아래로 떨리는 버그 — ✅ 고쳐짐 (직접 확인함)

**증상**: 마우스를 기물과 그 뒤 보드 칸의 경계에 두면 계속 위아래로 떨림(무한 반복).

**원인**: `LSO_HoverMoveEffect.target`이 콜라이더(`CapsuleCollider`)가 달린 오브젝트 자신을
가리키고 있어서, 호버로 뜰 때 콜라이더까지 같이 움직임 → 카메라가 비스듬해서 살짝만 떠도
그 화면 위치엔 더 이상 콜라이더가 없음 → 이탈 → 복귀 → 재진입 → 다시 이탈, 무한 반복.

`LSO_ButtonHoverHandler`의 `exitGraceSeconds`(이탈 유예, 0.08초)로는 못 막는다 — 이건 커서가
"순간적으로" 떨리는 걸 걸러내는 용도라, 콜라이더 자체가 뜰 때마다 확실히 커서를 벗어나는
이번 경우는 아무리 기다려도 결국 이탈 처리됨.

**고친 것** — `Assets/_Prefabs/KTH/Cylinder.prefab`
- 루트(`Cylinder`, 콜라이더+로직 전부)와 분리된 `Visual` 자식을 만들어 실제 보이는 메시
  (캡슐)를 거기로 옮김. 루트는 완전히 고정, `Visual`만 호버로 오르내림.
- `LSO_HoverMoveEffect.target`을 그 `Visual` 자식으로 연결.

**시행착오 기록** (다른 기물 프리팹 고칠 때 똑같이 걸릴 수 있어서 남김):
- 처음에 `target`을 `Visual` 자식으로 옮겼는데, 그 자식엔 실제로 안 쓰이는 `Will_Bull.fbx`
  모델이 물려 있었고 화면에 실제로 보이던 건 루트 자신의 기본 Capsule 메시였다. 그래서
  호버링이 아예 안 보이는 것처럼 됨 — **target을 옮기기 전에 어느 메시가 진짜 화면에
  보이는지(루트 자신의 MeshRenderer 여부) 먼저 확인할 것.**
- 이 프리팹은 `Assets/_Assets/LSO/Animals/Will_Bull.fbx`(guid `5c328aee...`)를 쓰던 흔적이
  남아있었다. 지금은 안 쓰지만 다른 기물엔 진짜 모델을 붙일 차례가 올 수 있음 — 그때도
  "콜라이더/로직은 루트, 보이는 메시는 target 자식" 구조를 유지할 것.

---

## 3. 카드를 빠르게 연달아 배치하면 버림 더미에서 겹침 — ⏳ 코드 수정 완료, 재현 테스트 아직 안 함

**증상**: 카드를 배치하고 그 카드가 버림 더미로 날아가는 연출 중에, 또 다른 카드를 빠르게
배치하면 두 카드가 버림 더미의 정확히 같은 자리에 겹쳐서 떨어짐.

**원인**: `KTH_DiscardCardUI.GetNextStackTarget()`이 "몇 번째 층에 놓을지"를
`_discardCardList.Count`(실제 쌓인 카드 수)로 계산하는데, 이 카운트는 카드가 날아가서
착지한 뒤(`AddExistingCardToDiscardPile`, 비행+착지 애니메이션 다 끝난 뒤, 약 0.3초 뒤)에야
올라간다. 그래서 첫 카드가 아직 날아가는 중에 두 번째 카드가 같은 함수를 부르면 둘 다
"몇 번째 층"으로 같은 값을 받아 같은 자리로 날아감.

**고친 것** — `Assets/_Scripts/KTH/CardManager/CardDeck/Discardpile/KTH_DiscardCardUI.cs`
- `_reservedStackCount` 필드 추가. `GetNextStackTarget()`을 부르는 즉시(착지를 기다리지
  않고) 그 자리를 예약해서 카운트를 올림 — 실제 리스트 카운트와 예약 카운트 중 큰 값을
  기준으로 삼아서 어느 경로로 카드가 추가되든 안전.
- 리셔플(`ClearAndGetList`)에서 `_reservedStackCount`도 같이 0으로 초기화.

**미확인**: 이 수정 이후 실제로 빠르게 연달아 배치해서 안 겹치는지 유니티에서 아직 확인
안 됨. 확인 부탁.

### 재현 조건 / 테스트 방법

카드 A가 버림 더미로 날아가는 연출이 **완전히 끝나기 전**(도착 + 착지 + 그림자 복구까지,
기본값 기준 약 0.3~0.4초 안)에 카드 B도 배치해야 재현된다.

1. 손패에 놓을 수 있는 카드 두 장 준비
2. 카드1 클릭(확정) → 보드 칸 클릭(배치)
3. 반 초 이내에 곧바로 카드2 클릭(확정) → 보드 칸 클릭(배치)
4. 버림 더미 확인 — 겹쳐서 한 장처럼 보이면 재현, 층이 나뉘어 쌓이면 정상

사람 손으로 이 타이밍을 맞추기 빡빡하면, `KTH_DiscardAnimation` 컴포넌트(버림 더미
오브젝트에 붙어 있음)의 인스펙터에서 **잠깐** 시간을 늘려서 테스트하면 확실하다:

- `Discard Duration`: 0.18 → 2 정도
- `Landing Duration`: 0.08 → 1 정도

이렇게 늘려두면 카드1을 놓고 2~3초 여유 있게 카드2를 놓아도 재현/미재현이 눈으로 확실히
구분된다. **확인 끝나면 원래 값(0.18 / 0.08)으로 꼭 되돌릴 것.**

---

## 4. ⚠️ 나 말고 다른 누군가가 만들다 만 수정 — 테스트해보고 문제없으면 커밋

결론부터: **네, 커밋하란 말 맞습니다.** 다만 "무조건 커밋"이 아니라 "**한 번 테스트해보고**
문제없으면 커밋"이라는 뜻이라 따로 항목을 나눴습니다. 나머지(1~3번)는 제가 이번 세션에서
고친 거라 원인/수정 내용을 제가 설명할 수 있는데, 이건 **제가 손댄 게 아니라 세션 시작
시점에 이미 그렇게 되어 있던 걸 발견만 한 것**이라 — 태호님이 하던 작업이면 왜 따로
적었나 싶으실 수 있어서, 그게 아니라면 커밋 전에 누가 언제 한 건지도 모른 채 넘어가지
말라고 표시해둔 겁니다.

**파일**: `Assets/_Scripts/KTH/CardManager/CardDeck/KTH_DeckUi.cs`

**무슨 내용이냐면**: 턴이 아주 빠르게 넘어가면(예: 내 턴 → 바로 적 턴 → 바로 다시 내 턴)
손패 카드들이 화면 가운데(0,0,0)에 뭉친 채로 안 풀리고 멈춰버리는 버그가 있었다고
주석에 적혀 있고, 그걸 고친 코드가 이미 들어가 있음(미커밋).

**해야 할 일**:
1. `git log -p -- Assets/_Scripts/KTH/CardManager/CardDeck/KTH_DeckUi.cs` 나 `git blame`으로
   본인이 한 게 맞는지 확인 (본인 거면 그냥 이어서 하면 됨)
2. 플레이 모드에서 턴을 빠르게 여러 번 반복 전환해서 카드가 안 뭉치는지 확인
3. 문제없으면 커밋 — 단, **1~3번(제가 고친 것)이랑 같은 커밋에 섞지 말 것.** 원인이
   완전히 다른 수정이라 나중에 문제 생기면 뭐가 뭘 고친 건지 구분이 안 됨
   (`GIT_협업규칙.md`의 "포맷/로직 커밋 안 섞기"랑 같은 이유)

---

## 5. KTH_HandCardLayout 쪼갬 (God Class 방지) — ✅ 완료, 실행은 아직 안 켜봄

975줄까지 커진 `KTH_HandCardLayout.cs`에서 두 덩어리를 KTH_HandCard가 자기 동작을
컨트롤러들(Hover/Selection/DoubleClick/MotionAnimator)에게 위임하는 것과 같은 방식으로
뽑았다 — MonoBehaviour가 아니라 `KTH_HandCardLayout`을 owner로 들고 있는 순수 C# 클래스다.

**만든 것**
- `Assets/_Scripts/KTH/CardManager/CardDeck/HandCard/KTH_HandCardPlacementFlow.cs`
  카드 확정 클릭 → 배치 모드 → `LDY_CardPlacer` 연동 → 포커스 카드 주위 부채꼴.
  (`HandleCardConfirmClicked`, `EnterPlacementMode`, `ExitPlacementMode`,
  `MoveSelectedCardToCenter`, `ApplyFanAroundFocalCard`, `SpreadCardsAroundCenter`)
- `Assets/_Scripts/KTH/CardManager/CardDeck/HandCard/KTH_HandCardGroupMotion.cs`
  손패 컨테이너 통째로 내렸다 올리기, 카드 전체 가운데로 모으기/되돌리기.
  (`MoveDownForPlacement`, `MoveUpFromPlacement`, `GatherCardsToCenter`, `RestoreCardsFromCenter`)

**고친 것** — `KTH_HandCardLayout.cs` (975줄 → 544줄)
- 위 두 클래스를 `Awake()`에서 생성해서 들고 있고, 해당 public 메서드들은 그쪽으로
  한 줄 위임(delegate)하도록 바꿨다. 외부에서 부르는 시그니처(`KTH_HandCardLayout.Instance.
  EnterPlacementMode(...)` 등)는 전혀 안 바뀌었다 — 안에서만 바뀜.
- `handCards` 리스트와 `selectedCard`는 여전히 이 클래스가 유일하게 들고 있다. 두 헬퍼는
  `internal List<KTH_HandCard> HandCards`, `internal KTH_HandCard SelectedCard { get; set; }`
  프로퍼티로 읽고 쓴다 — "자리를 정하는 주체는 하나" 원칙 유지.
- 카드 한 장의 정위치를 계산하는 `UpdateHandLayout`은 두 헬퍼 양쪽에서 다 불리는 공용
  진입점이라 그대로 `KTH_HandCardLayout`에 남겨뒀다.

**미확인**: 로직을 그대로 옮기기만 했고(동작 변경 없음) 컴파일 에러 없는 것과 교차 참조는
확인했지만, 유니티에서 실제로 켜서 카드 뽑기 → 확정 클릭 → 배치 → 우클릭 취소 →
더블클릭까지 한 바퀴 다 돌려보진 않았다. Play 모드에서 한 번 훑어봐 주면 좋겠다.

---

## 6. 다음에 볼 사람이 알아야 할 것

- 2번(경계 떨림)은 실제로 켜서 확인함 — 고쳐짐.
- 1번(배치 이중 동작)은 코드는 적용했지만 이 값(`hoverResumeDelay` 등)이 원하는 느낌인지
  직접 켜서 보진 않았다. 씬에 옛 기본값이 구워져 있을 수 있다는 점도 위 1번 항목 참고.
- 3번(카드 겹침)은 코드만 고쳤고 실제로 두 장을 빠르게 연달아 배치해서 안 겹치는지는
  아직 확인 안 됐다.
- 4번(KTH_DeckUi.cs)은 남의 미완성 수정이라 테스트하고 커밋할지 말지도 본인 판단.
- 5번(KTH_HandCardLayout 쪼갬)도 로직은 그대로 옮긴 거라 이론상 동작이 같아야 하지만,
  손패 전체 흐름(뽑기/확정/배치/취소/더블클릭)을 한 바퀴 직접 돌려보진 않았다.
- 아직 유니티 콘솔 에러 0인지 최종 확인 후 커밋할 것 (`GIT_협업규칙.md` 규칙).
- 씬(`LSO_TestScene 1.unity`)에 옛 `settleEase` 값이 구워져 있던 것처럼, 다른 씬/프리팹에도
  `KTH_PlacementAnimation`을 쓰는 인스턴스가 있으면 같은 문제가 있을 수 있다 — 인스펙터로
  한 번씩 확인할 것.
