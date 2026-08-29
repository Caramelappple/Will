# 턴 레버 (LSO_TurnLever)

**지금 턴인 쪽이 올라가 있다.**
플레이어 턴이면 플레이어 레버가 위, 적 레버가 아래다.
누르면 턴이 넘어가고 두 레버가 서로 자리를 바꾼다.

---

## 1. 왜 이렇게 짰나

앞선 시도(`LSO_ExclusiveGroup` + `LSO_ExclusiveMember` + `LSO_ClickSinkEffect`)는
**자리를 정하는 주체가 셋**이었다.

| 주체 | 하던 일 |
|---|---|
| `LSO_ClickSinkEffect` | 클릭하면 스스로 토글 |
| `LSO_ExclusiveGroup` | 선택 상태를 들고 있음 |
| UnityEvent 배선 | `Down()` / `Up()` 호출 |

셋이 같은 `localPosition`을 두고 서로 덮어썼다.
`IsDown`이 실제 자리와 어긋나면 다음 호출이 *"이미 그 상태다"* 검사에 걸려
아무 일도 일어나지 않았고, 어느 하나만 봐서는 원인을 알 수 없었다.

**이번 구조의 규칙은 하나다 — 자리를 바꾸는 코드는 `LSO_TurnLever.Move()` 뿐이다.**

---

## 2. 파일

| 파일 | 책임 |
|---|---|
| `_Scripts/LSO/UI/LSO_TurnLever.cs` | 턴 상태 구독 · 양쪽 높이 결정 · 턴 넘기기 요청 |
| `_Scripts/LSO/UI/LSO_TurnLeverSide.cs` | 클릭을 레버에 넘기기만 함. **상태 없음** |
| `_Scripts/LSO/UI/LSO_TurnEvent.cs` | `UnityEvent<LDY_Team>` 직렬화용 껍데기 |

기존 것 두 개를 그대로 쓴다.

| 파일 | 역할 |
|---|---|
| `LSO_ButtonClickHandler.cs` | 클릭 감지. "좌클릭만", "상호작용 불가면 무시" |
| `LSO_IClickEffect.cs` | 클릭을 받을 것들의 공통 인터페이스 |

---

## 3. 흐름

```
클릭
 ↓  LSO_ButtonClickHandler (IPointerClickHandler)
 ↓  LSO_TurnLeverSide.OnClick()
 ↓  LSO_TurnLever.RequestEndTurn()
 ↓
 ├─ CanEndPlayerTurn() == false  →  onRejected 발행. 자리는 그대로.
 │
 └─ LDY_TurnManager.EndPlayerTurn()
        ↓
    OnTurnChanged(LDY_Team)
        ↓
    LSO_TurnLever.Apply()   ←  자리가 바뀌는 유일한 지점
```

**클릭은 자리를 바꾸지 않는다.** "턴을 넘겨달라"고 요청할 뿐이다.

턴 매니저가 거절하면(적 턴, 공격 연출 중) 화면은 그대로 있는다.
그래서 화면과 진짜 턴이 어긋날 수 없다.

### 턴 매니저를 직접 물지 않는 이유

```csharp
GameManager.Instance.TurnManagerChanged += Bind;
Bind(GameManager.Instance.TurnManager);
```

전투 씬마다 `LDY_TurnManager`가 새로 생긴다.
인스펙터 직접 참조로 두면 씬을 넘길 때 끊긴다.

구독한 **직후에 현재 값을 한 번 읽는** 것도 중요하다.
`LDY_TurnManager`는 자기 `Start`에서 첫 턴을 한 번 알리는데,
레버가 늦게 붙으면 그 한 번을 놓쳐 자리가 영영 안 잡힌다.

---

## 4. 씬 배선

```
TurnLever                    ← LSO_TurnLever
 ├ PlayerSide                ← 누를 수 있어야 함
 │    Collider
 │    LSO_ButtonClickHandler
 │    LSO_TurnLeverSide
 └ EnemySide                 ← 비주얼일 뿐. 아무것도 안 붙여도 된다
```

`LSO_TurnLever`의 `Player Side` / `Enemy Side`에 두 자식을 연결한다.
`LSO_TurnLeverSide`는 부모에서 레버를 알아서 찾으므로 비워둬도 된다.

### 적 쪽은 누르는 것이 아니다

적 턴은 `LDY_TurnManager.RunEnemyTurnRoutine`이 끝나면 스스로 넘어간다.
그래서 `EnemySide`에는 Collider도, 클릭 핸들러도, `LSO_TurnLeverSide`도 필요 없다.

레버가 양쪽 높이를 다 정해주므로 **비주얼만 있으면 된다.**

적 쪽에도 클릭을 붙이면 플레이어가 적 턴을 대신 넘길 수 있게 되어,
`CanEndPlayerTurn()`이 막아주긴 하지만 누를 수 있는 것처럼 보여 혼란만 준다.

### 3D 물건일 때 필요한 것

| | |
|---|---|
| 씬에 `EventSystem` | GameObject > UI > Event System |
| 카메라에 `Physics Raycaster` | 2D 콜라이더면 `Physics 2D Raycaster` |

`IPointerClickHandler`는 uGUI 전용이 아니다.
`Physics Raycaster`가 EventSystem의 레이를 3D 콜라이더까지 연장해준다.

---

## 5. 인스펙터

```
양쪽
  Player Side       플레이어 턴에 올라가 있을 것
  Enemy Side        적 턴에 올라가 있을 것

움직임
  Direction         (0, -1, 0)   각 물건의 로컬 기준
  Depth             0.15

연출
  Down Duration     0.18   / Out Quad
  Up Duration       0.24   / Out Back
  Ignore Time Scale ✓

반응  (셋 다 LDY_Team 하나를 넘긴다)
  On Turn Changed   턴이 바뀔 때마다. 인자는 새 턴
  On Accepted       이 레버를 눌러서 넘어갔을 때. 인자는 넘어간 뒤의 턴
  On Rejected       못 넘겼을 때. 인자는 거절 당시의 턴
```

### 셋을 나눈 이유

| | 언제 |
|---|---|
| `On Turn Changed` | 양방향 전부. **적 턴이 끝나 돌아올 때도** |
| `On Accepted` | 레버를 눌러서 넘어갔을 때만 |
| `On Rejected` | 눌렀는데 거절됐을 때 |

레버 당기는 소리처럼 **"눌렀을 때만" 나야 하는 것은 `On Accepted`**에 건다.
`On Turn Changed`에 걸면 적 턴이 끝나고 돌아올 때도 소리가 난다.

`On Turn Changed`는 **시작할 때는 발행되지 않는다.**
첫 자리를 맞추는 것은 "바뀐 것"이 아니라 "원래 그랬던 것"이라,
씬에 들어가자마자 연출이 도는 것을 막는다.

발행 순서는 `On Turn Changed` → `On Accepted` 다.
`EndPlayerTurn()` 안에서 턴이 바뀌며 `OnTurnChanged`가 먼저 날아오기 때문이다.

### UnityEvent에 인자를 넘기려면

```csharp
[Serializable]
public class LSO_TurnEvent : UnityEvent<LDY_Team> { }
```

`UnityEvent<T>`를 그대로 필드에 쓰면 **인스펙터에 나오지 않는다.**
유니티가 제네릭 타입을 직렬화하지 못하므로 닫힌 타입으로 한 번 감싸야 한다.
`_Scripts/LSO/UI/LSO_TurnEvent.cs` 가 그 역할이다.

인스펙터에서 **인자를 받는 메서드**를 고르면 값이 그대로 전달되고,
**인자 없는 메서드**를 고르면 그냥 호출된다. 둘 다 목록에 나온다.

### Depth 값 감각

| 대상 | 값 |
|---|---|
| 3D 월드 | `0.05` ~ `0.2` |
| UI (RectTransform) | **픽셀 단위** — `10` ~ `20` |

UI인데 `0.15`를 넣으면 움직이지 않는 것처럼 보인다.

---

## 6. 주의점

### 자리를 밖에서 바꾸지 말 것

`Player Side` / `Enemy Side`의 `localPosition`을 다른 스크립트나 애니메이터가
건드리면 즉시 어긋난다. 레버는 `Awake`에서 잡아둔 자리를 기준으로 계산하기 때문이다.

**연출을 더 붙이고 싶으면 `On Accepted` / `On Rejected`를 쓰거나,
움직임과 상관없는 것(색, 소리, 파티클)으로 한정할 것.**

### 콜라이더가 같이 내려간다

`Player Side`에 콜라이더가 붙어 있으면 내려갈 때 함께 움직인다.
`Depth`가 크면 커서에서 벗어나거나 탁자에 파묻혀 다른 콜라이더가 먼저 맞을 수 있다.

크게 움직여야 하면 **콜라이더는 안 움직이는 부모에 두고,
`Player Side`에는 자식 비주얼을 연결**한다.

```
PlayerSide            ← Collider + 핸들러 + LSO_TurnLeverSide (고정)
 └ Visual             ← 레버의 Player Side 에 이것을 연결
```

### UI가 앞을 가리면 클릭이 안 간다

Screen Space - Overlay 캔버스에 **`Raycast Target`이 켜진 투명 이미지**가
화면을 덮고 있으면 3D 콜라이더까지 레이가 내려가지 않는다.

배경용 `Image` / `Panel`은 `Raycast Target`을 끌 것.
UI 쪽에서 원인을 찾기 가장 어려운 문제다.

### Is Trigger

`Physics Raycaster`는 **Project Settings > Physics > Queries Hit Triggers**를 따른다.
꺼져 있는데 콜라이더가 트리거면 클릭이 잡히지 않는다.

### 거절 반응을 반드시 걸 것

`CanEndPlayerTurn()`이 false면 아무것도 움직이지 않는다.
`On Rejected`를 비워두면 **"버튼이 안 먹는다"로 보인다.**

거절되는 경우는 셋이다.

| 상황 | 이유 |
|---|---|
| 적 턴 | `CurrentTurn != Player` |
| 적 턴 처리 중 | `_isProcessingTurn` |
| 이동·공격 연출 중 | `IsAnimating()` |

### 자동 턴 종료는 없다

`LDY_TurnManager`의 `Update`를 걷어냈다.
**행동력이 0이 돼도 턴이 자동으로 넘어가지 않는다.** 이 레버가 유일한 경로다.

> 기획서: "코스트를 모두 사용한 경우에도 직접 버튼을 눌러 턴을 종료한다."

---

## 7. 확장할 때

### 개수를 늘리려면

지금은 플레이어/적 두 개 고정이다.
셋 이상이 필요해지면 `Apply()`에서 `LDY_Team`을 보고 나누는 부분만 바꾸면 된다.

**다만 "누가 자리를 정하는가"는 하나로 유지할 것.**
그 규칙이 깨지는 순간 이 문서 1절의 문제가 그대로 돌아온다.

### 다른 반응을 붙이려면

`On Turn Changed` / `On Accepted` / `On Rejected` 를 쓴다.
자리를 바꾸는 것만 아니면 무엇을 걸어도 안전하다.

인자로 `LDY_Team`이 오므로, 받는 쪽 메서드를 `void Foo(LDY_Team team)` 으로 두면
누구 턴이 됐는지에 따라 다르게 반응할 수 있다.
