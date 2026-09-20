# 튜토리얼 설계

이시온(LSO) 작업. 이 문서만 보고 코드를 짤 수 있게 적는다.

---

## 0. 전제

튜토리얼 전용 씬을 만들지 않는다. **실제 게임 위에서 대본을 읽어주고 조작을 좁힌다.**
소환·이동·공격·적 반격·유언 발동·보상·스테이지 전환이 전부 진짜로 일어나야 하기 때문이다.

기존 시스템은 튜토리얼을 모른다. 튜토리얼을 지워도 게임은 그대로 돈다.

```
LSO_TutorialDirector   대본을 한 걸음씩 재생
     ↓ 시킨다                    ↓ 듣는다
카메라 · 안내문 · 칸 표시     "플레이어가 그걸 했다" 신호
```

---

## 1. 만들 파일

```
Assets/_Scripts/LSO/Tutorial/
  LSO_TutorialDirector.cs
  LSO_TutorialBanner.cs
  LSO_TutorialGuide.cs
  LSO_TutorialLock.cs
  LSO_TutorialAction.cs            (enum)
  Data/
    LSO_TutorialChapterSO.cs
    LSO_TutorialStepSO.cs
  LSO_TutorialEnemyPlan.cs         (적을 대본대로 — 8-2)
  Gate/
    LSO_ITutorialGate.cs
    LSO_TutorialGateSO.cs          (추상 베이스)
    LSO_GateDelay.cs
    LSO_GateAnyClick.cs
    LSO_GatePieceInspected.cs
    LSO_GateInfoClosed.cs
    LSO_GateCardSelected.cs
    LSO_GatePiecePlaced.cs
    LSO_GatePieceMoved.cs
    LSO_GatePieceAttacked.cs
    LSO_GateWillPicked.cs
    LSO_GateWillPainted.cs
    LSO_GateTurnEnded.cs
    LSO_GateRewardTaken.cs
    LSO_GateStageAdvanced.cs
```

---

## 2. 데이터

### LSO_TutorialStepSO

```csharp
[CreateAssetMenu(menuName = "SO/Tutorial/Step")]
public sealed class LSO_TutorialStepSO : ScriptableObject
{
    [Header("카메라")]
    public string shotId;                 // 비우면 카메라를 안 움직인다
    public bool waitForCamera = true;     // 블렌드가 끝나야 안내문을 띄운다

    [Header("안내문")]
    [TextArea(2, 5)] public string[] lines;
    [Min(0f)] public float lineHold = 2.5f;   // 마지막 줄은 관문이 열릴 때까지 남는다

    [Header("가이드")]
    public LSO_TutorialGuideKind guide;   // None / Place / Move
    public Vector3Int[] guideTiles;

    [Header("조작 허용")]
    public LSO_TutorialAction allowed;    // [Flags]

    [Header("관문")]
    public LSO_TutorialGateSO gate;       // null 이면 마지막 줄을 읽고 바로 다음
    [Min(0f)] public float gateTimeout = 30f;  // 0 이면 무한
}
```

### LSO_TutorialChapterSO

```csharp
[CreateAssetMenu(menuName = "SO/Tutorial/Chapter")]
public sealed class LSO_TutorialChapterSO : ScriptableObject
{
    public string title;                  // "1. 게임 설명"
    public List<LSO_TutorialStepSO> steps = new();
}
```

### LSO_TutorialAction (Flags)

```csharp
[Flags]
public enum LSO_TutorialAction
{
    None        = 0,
    CardSelect  = 1 << 0,
    Place       = 1 << 1,
    PieceSelect = 1 << 2,
    Move        = 1 << 3,
    Attack      = 1 << 4,
    EndTurn     = 1 << 5,
    Will        = 1 << 6,   // 촛불 숫자키/휠 + 유언 붙이기
    InfoPanel   = 1 << 7,
    Reward      = 1 << 8,
    All         = ~0
}
```

### LSO_TutorialGuideKind

```csharp
public enum LSO_TutorialGuideKind { None, Place, Move }
```

---

## 3. 관문

### 인터페이스

```csharp
public interface LSO_ITutorialGate
{
    void Arm(Action onPassed);   // 기다리기 시작
    void Disarm();               // 그만 기다린다 (통과했든 시간이 지났든 반드시 부른다)
}
```

### 베이스

```csharp
public abstract class LSO_TutorialGateSO : ScriptableObject, LSO_ITutorialGate
{
    private Action _onPassed;

    public void Arm(Action onPassed)
    {
        _onPassed = onPassed;
        OnArm();
    }

    public void Disarm()
    {
        OnDisarm();
        _onPassed = null;
    }

    protected void Pass()
    {
        Action cb = _onPassed;
        _onPassed = null;
        cb?.Invoke();
    }

    protected abstract void OnArm();
    protected abstract void OnDisarm();
}
```

> SO 는 에셋이라 상태를 들고 있으면 런타임 값이 에셋에 남는다.
> `_onPassed` 는 직렬화 대상이 아니므로 남지 않는다. **직렬화되는 필드에 런타임 상태를 쓰지 말 것.**
> 코루틴이 필요한 관문(Delay)은 `LSO_TutorialDirector` 가 대신 돌려준다 — 아래 참고.

### 관문별 구현

| 관문 | 붙을 신호 | 상태 |
|---|---|---|
| `Delay` | 감독이 돌리는 코루틴 | 새로 |
| `AnyClick` | `Mouse.current.leftButton.wasPressedThisFrame` (감독의 Update) | 새로 |
| `PieceInspected` | `DLJ_InfoPanelEvents.PieceDoubleClicked` (static event) | **있음** |
| `InfoClosed` | `DLJ_InfoPanel` 에 `Closed` 이벤트 추가 필요 | 없음 |
| `CardSelected` | `KTH_HandCardLayout` 에 `CardSelected` 이벤트 추가 필요 | 없음 |
| `PiecePlaced` | `LDY_CardPlacer` 에 `Placed` 이벤트 추가 필요 | **없음** |
| `PieceMoved` | `LDY_MoveSystem` 에 `Moved` 이벤트 추가 필요 | **없음** |
| `PieceAttacked` | `LDY_AttackSystem` 에 `Attacked` 이벤트 추가 필요 | **없음** |
| `WillPicked` | `LSO_WillCandle.Changed` | **있음** |
| `WillPainted` | `LSO_WillPainter.onPainted` (UnityEvent) | **있음** |
| `TurnEnded` | `LDY_TurnManager.OnTurnChanged` | **있음** |
| `RewardTaken` | `LSO_RewardBox.OnFinished` | **있음** |
| `StageAdvanced` | `LSO_StageProgression.Advanced` | **있음** |

`Delay` · `AnyClick` 은 감독이 대신 봐준다. 나머지는 자기가 구독하고 `Disarm` 에서 해제한다.

---

## 4. 기존 파일에 달 이벤트 (선행 작업)

전부 LDY·KTH·DLJ 라 **보고할 때 DLJ 는 반드시 짚는다.**

### 4-1. `Assets/_Scripts/LDY/LDY_MoveSystem.cs`

`MoveVisual` 코루틴 끝, `OnMoved` 알림 옆(210줄 근처)에서 쏜다.

```csharp
/// <summary>기물이 실제로 옮겨 앉았을 때. 연출까지 끝난 뒤다.</summary>
public event Action<LDY_Animal, Vector3Int, Vector3Int> Moved;   // 기물, 출발, 도착
```

### 4-2. `Assets/_Scripts/LDY/LDY_AttackSystem.cs`

`Attack` 의 `finally` 안, `onComplete?.Invoke()` 바로 앞.

```csharp
/// <summary>공격 연출이 전부 끝났을 때. 대상이 죽었어도 쏜다.</summary>
public event Action<LDY_Animal, LDY_Animal> Attacked;   // 공격자, 대상
```

### 4-3. `Assets/_Scripts/LDY/LDY_CardPlacer.cs`

실제로 보드에 놓인 직후.

```csharp
/// <summary>카드로 기물을 놓았을 때. 취소·실패는 쏘지 않는다.</summary>
public event Action<LDY_Animal> Placed;
```

### 4-4. `Assets/_Scripts/KTH/CardManager/CardDeck/HandCard/KTH_HandCardLayout.Placement.cs`

`OnCardSelectionChanged` 의 `selected == true` 갈래.

```csharp
/// <summary>손패에서 카드를 골랐을 때.</summary>
public static event Action<KTH_HandCard> CardSelected;
```

> 정적인 이유는 손패가 씬마다 새로 생기기 때문이다. `KTH_GameEndManager.IsBattleOver` 와 같은 이유·같은 모양.

### 4-5. `Assets/_Scripts/DLJ/UI/InfoPanel/DLJ_InfoPanel.cs` ← **주다림 파일**

`Hide()` 안, `SetVisible(false)` 뒤.

```csharp
/// <summary>인포창이 닫혔을 때.</summary>
public static event Action Closed;
```

### 4-6. `Assets/_Scripts/LDY/LDY_CardPlacer.cs` · `LDY_MoveSystem.cs` — 칸 제한

8-1 참고.

```csharp
public void RestrictTo(IEnumerable<Vector3Int> tiles);
public void ClearRestriction();
public bool IsRestricted { get; }
```

### 4-7. `Assets/_Scripts/LDY/AI/LDY_EnemyAI.cs` — 대본 주입구

8-2 참고. `LDY_IEnemyPlan` 인터페이스 신설 + `LDY_EnemyAI.Plan` 프로퍼티.

---

## 5. 컴포넌트

### LSO_TutorialBanner

화면 위 안내문. **뜨고 지는 것만 안다.**

```csharp
public void Show(string line);   // 이미 떠 있으면 글자만 바꾼다
public void Clear();
public bool IsBusy { get; }      // 페이드 중
```

- `TMP_Text` 하나 + CanvasGroup 페이드
- `WaitForSecondsRealtime` 을 쓸 것 — 유언 연출이 timeScale 을 쥐는 구간이 있다

### LSO_TutorialGuide

칸 물들이기. `LDY_TileHighlighter` 를 빌려 쓴다.

```csharp
public void Show(LSO_TutorialGuideKind kind, IEnumerable<Vector3Int> tiles);
public void Clear();
```

```csharp
// Place → ShowAttackHighlights(this, tiles)   붉은색
// Move  → ShowMoveHighlights(this, tiles)     노란색
// Clear → ClearHighlights(this)
```

> `owner` 를 `this` 로 넘기는 것이 중요하다. `LDY_SelectionController` 가 자기 하이라이트를 지울 때 튜토리얼 것까지 지우지 않는다.

### LSO_TutorialLock

지금 허용된 조작만 연다.

```csharp
public void Apply(LSO_TutorialAction allowed);
public void Release();   // 튜토리얼이 끝나면 전부 연다
```

막는 방법은 이미 있는 게이트들을 그대로 쓴다.

| 조작 | 막는 법 |
|---|---|
| `PieceSelect` · `Move` · `Attack` | `LDY_SelectionController.enabled` |
| `CardSelect` · `Place` | `LDY_CardPlacer` / 손패 게이트 |
| `EndTurn` | 턴 넘기기 오브젝트의 `LSO_ButtonClickHandler.enabled` |
| `Will` | `LSO_WillCandle.enabled` + `LSO_WillPainter` |
| `InfoPanel` | `DLJ_InfoPanel` 구독을 끊지 말고 `LSO_TutorialLock` 이 자체 플래그로 |
| `Reward` | `LSO_RewardClickGate` |

> **컴포넌트를 껐다 켜는 방식의 함정**: `LSO_TeamHoverGate` 주석에 적혀 있듯, 껐다 켜면 유니티가 호버 진입을 다시 안 보낸다. 클릭만 막는 것은 괜찮지만, 호버가 걸린 것은 `SetGateOpen` 같은 방식을 따를 것.

### LSO_TutorialDirector

대본을 한 걸음씩 재생. **조건은 하나도 모른다.**

```csharp
[SerializeField] private List<LSO_TutorialChapterSO> chapters = new();
[SerializeField] private LSO_TutorialBanner banner;
[SerializeField] private LSO_TutorialGuide guide;
[SerializeField] private LSO_TutorialLock  lockControl;
[SerializeField] private LSO_CameraDirector camera;
[SerializeField] private bool playOnStart;
[SerializeField] private bool logSteps = true;

public bool IsPlaying { get; private set; }
public event Action Finished;

public void Play();
public void Stop();   // 정상 종료와 건너뛰기가 함께 쓴다 (8-3)
```

한 걸음의 흐름:

```
1. 카메라        shotId 있으면 camera.Play(shotId)
                 waitForCamera 면 !camera.IsBlending 까지 대기
2. 가이드        guide.Show(step.guide, step.guideTiles)
3. 잠금          lockControl.Apply(step.allowed)
4. 안내문        lines 를 차례로. 마지막 줄 전까지는 lineHold 만큼 머문다
5. 관문          gate == null 이면 lineHold 만큼 더 기다리고 통과
                 아니면 gate.Arm(...) 하고 통과 신호를 기다린다
                 gateTimeout 을 넘으면 경고를 남기고 통과   ← 멈춰 서는 쪽이 더 나쁘다
6. 정리          gate.Disarm() · guide.Clear()
7. 다음 걸음
```

마지막에 `lockControl.Release()` · `camera.ReturnToDefault()` · `banner.Clear()` · `Finished?.Invoke()`.

에디터 도구 (없으면 매번 처음부터 돌려야 한다):

```csharp
#if UNITY_EDITOR
[ContextMenu("테스트: 다음 걸음")]      private void TestNext();
[ContextMenu("테스트: 이 챕터 건너뛰기")] private void TestSkipChapter();
#endif
```

---

## 6. 만들 에셋

- **튜토리얼 스테이지 SO** — 적 1~2기 고정 배치. "적 기물 하나를 확대한다"를 하려면 그 기물이 어디 있는지 정해져 있어야 한다
- **튜토리얼 시작 덱** — 뽑히는 카드가 매번 다르면 "카드 한 장을 선택해보세요" 뒤가 매번 달라진다
- **카메라 샷** — `LSO_CameraDirector` 에 등록할 이름들

```
tut_table    탁자 전체
tut_board    체스판
tut_enemy    적 기물
tut_deck     덱
tut_hand     손패
tut_cost     코스트 케이스
tut_clock    체스 시계
tut_candle   유언 촛불
tut_health   목숨 양초
tut_reward   보상 상자
```

- **챕터 SO 일곱 개** — 기획서 본문 기준

```
1. 게임 설명   2. 전장과 적   3. 덱   4. 유언   5. 체력   6. 보상   7. 스테이지
```

---

## 7. 작업 순서

0. **칸 제한 + 적 대본** (4-6 · 4-7) — 구조가 가장 크게 걸리는 곳이라 먼저 확인한다
1. **이벤트 다섯 개 달기** (4-1~4-5) — 이게 없으면 나머지가 전부 폴링이 된다
2. `LSO_TutorialAction` · `LSO_TutorialGuideKind` · `LSO_TutorialStepSO` · `LSO_TutorialChapterSO`
3. `LSO_ITutorialGate` · `LSO_TutorialGateSO` · `LSO_GateDelay` · `LSO_GateAnyClick`
4. `LSO_TutorialBanner` · `LSO_TutorialGuide` · `LSO_TutorialLock`
5. `LSO_TutorialDirector`
6. **챕터 1만 만들어서 끝까지 돌려본다** — 여기서 구조가 맞는지 드러난다
7. 나머지 관문들
8. 챕터 2~7

---

## 8. 정해진 것 (확정)

### 8-1. 대본에 표시한 칸에만 놓을 수 있다 (확정)

**표시하는 칸과 놓을 수 있는 칸은 같은 값이어야 한다.** 둘을 따로 적으면 언젠가 어긋나고,
그때 플레이어는 노란 칸을 눌렀는데 아무 일도 안 일어나는 화면을 본다.

그래서 `LSO_TutorialStepSO.guideTiles` **하나**가 표시와 허용을 동시에 정한다.

`Assets/_Scripts/LDY/LDY_CardPlacer.cs` 에 제한을 단다.

```csharp
/// <summary>
/// 놓을 수 있는 칸을 좁힌다. 비우면(ClearRestriction) 다시 전부 허용한다.
///
/// 튜토리얼이 쓴다. 평소에는 아무도 안 부르므로 게임 동작은 그대로다.
/// </summary>
public void RestrictTo(IEnumerable<Vector3Int> tiles);
public void ClearRestriction();
public bool IsRestricted { get; }
```

- 놓을 수 있는지 보는 기존 검사 **뒤에** 한 줄 더 얹는다 — 원래 규칙을 느슨하게 만들지 않는다
- 제한에 걸려 거부되면 `Debug.Log` 한 줄. 조용히 거부하면 왜 안 놓이는지 알 수 없다
- 감독은 걸음을 시작할 때 `RestrictTo(step.guideTiles)`, 끝낼 때 `ClearRestriction()`

이동도 같다. `LSO_TutorialGuideKind.Move` 인 걸음에서는 `LDY_MoveSystem` 에 같은 모양의
`RestrictTo` / `ClearRestriction` 을 단다.

> **제한을 푸는 책임은 감독 하나다.** 걸음이 어떻게 끝나든(통과·시간초과·Stop) 반드시 푼다.
> 안 풀면 튜토리얼이 끝난 뒤에도 그 칸에만 놓을 수 있게 된다 — 원인을 짐작할 수 없는 종류의 버그다.

### 8-2. 튜토리얼 동안 적은 대본대로 움직인다 (확정)

지금 AI 는 점수로 고른다. 판이 고정이라도 **보장은 없고**, 특성이 하나 바뀌면 조용히 달라진다.
4번 챕터의 "적이 유언 기물을 때린다 → 분노로 전멸"은 그게 일어나야만 성립한다.

`Assets/_Scripts/LDY/AI/` 에 주입구를 만든다.

```csharp
/// <summary>
/// 이번 수를 대신 정해주는 대본. 없으면 평소처럼 LDY_EnemyBrain 이 고른다.
/// </summary>
public interface LDY_IEnemyPlan
{
    /// <returns>대본이 정해준 수가 있으면 참. 거짓이면 AI 에게 맡긴다.</returns>
    bool TryGetAction(LDY_Animal self, LDY_BoardManager board, out LDY_EnemyAction action);
}
```

`Assets/_Scripts/LDY/LDY_EnemyAI.cs` 의 `ActEnemy` 에서:

```csharp
LDY_EnemyAction action =
    Plan != null && Plan.TryGetAction(enemy, board, out LDY_EnemyAction scripted)
        ? scripted
        : _brain.Decide(enemy, board);
```

```csharp
/// <summary>대본. 튜토리얼이 켜고 끈다. 평소에는 null 이라 아무 영향이 없다.</summary>
public LDY_IEnemyPlan Plan { get; set; }
```

튜토리얼 쪽 구현은 `Assets/_Scripts/LSO/Tutorial/LSO_TutorialEnemyPlan.cs`.

```
턴 1  (e2 의 적) → (d3) 공격
턴 2  …
```

- 대본에 없는 기물·턴은 `false` 를 돌려 AI 에게 맡긴다 — 전부 적을 필요가 없다
- 대본이 시킨 수가 지금 불가능하면(대상이 이미 죽었다 등) **경고를 남기고** `false`.
  조용히 AI 로 넘기면 "대본이 왜 안 먹지"를 알 수 없다
- 감독이 `Play()` 에서 꽂고 `Stop()`·`Finished` 에서 **반드시 `Plan = null`**

### 8-3. 건너뛰기 (확정)

```
튜토리얼은 새 게임 첫 판에서 자동으로 돈다
ESC 로 언제든 건너뛴다
메뉴에서 다시 보기는 만들지 않는다
```

**다시 보기를 만들지 않는 이유**는, 튜토리얼이 전용 스테이지·전용 덱 위에서 돌기 때문이다.
진행 중인 런에서 그것을 부르려면 지금 판을 저장했다 되돌리는 길이 필요하고, 그건 튜토리얼보다 큰 일이다.

건너뛰기는 `LSO_TutorialDirector.Stop()` 이 받는다. **Stop 이 하는 일은 정상 종료와 똑같다.**

```
1. 지금 걸음의 관문을 Disarm
2. 코루틴 정지
3. guide.Clear()
4. cardPlacer.ClearRestriction() · moveSystem.ClearRestriction()
5. enemyAI.Plan = null
6. lockControl.Release()
7. banner.Clear()
8. camera.ReturnToDefault()
9. IsPlaying = false · Finished?.Invoke()
```

**끝나는 길이 둘(정상 종료·건너뛰기)이지만 정리하는 곳은 하나다.**
정상 종료도 마지막 걸음을 마치고 `Stop()` 을 부른다. 둘을 따로 적으면
한쪽만 고쳤을 때 "건너뛰면 칸 제한이 안 풀리는" 식으로 갈린다.

ESC 를 받는 곳은 `LSO_TutorialDirector.Update` 하나다. 별도 입력 스크립트를 두지 않는다 —
튜토리얼이 도는 동안에만 유효한 키라 감독이 직접 보는 편이 범위가 분명하다.

> ESC 가 이미 다른 데 걸려 있으면(`DLJ_ESC` 프리팹이 있다) 튜토리얼 중에는 그쪽을 막는다.
> `LSO_TutorialLock` 의 허용 목록에 넣지 말고, 감독이 먼저 받아 소비할 것.

---

## 9. 지키는 규칙

- **관문이 늘어도 감독은 안 고친다.** 감독은 `Arm` / `Disarm` 만 안다
- **모든 관문에 상한 시간.** 넘으면 경고를 남기고 통과한다 — 멈춰 서는 쪽이 연출이 잘리는 쪽보다 나쁘다
- **걸음마다 로그 한 줄.** 어디서 멈췄는지 콘솔만 보고 알 수 있어야 한다
- **배선이 빠지면 경고를 낸다.** 조용히 넘기면 "튜토리얼이 원래 저런가"로 읽힌다
