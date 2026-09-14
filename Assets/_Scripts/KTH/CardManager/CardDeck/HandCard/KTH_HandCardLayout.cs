using System;
using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LSO.Deck.Data;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using _Scripts.LSO.UI.Panel;
using _Scripts.LSO.Will.Candle;

// 3D 전환 메모:
// 이 스크립트는 원래도 RectTransform이 아니라 transform.DOLocalMove / DOLocalRotate를
// 쓰고 있어서 좌표 계산 자체는 그대로 3D에서도 동작한다.
// 유일하게 UI 전용이던 부분은 렌더 순서를 정하던 transform.SetAsLastSibling()이라
// 그 부분만 KTH_HandCard.BringToFront() (내부적으로 SpriteRenderer.sortingOrder 조정)로 바꿨다.
//
// 아래 Spacing/Width/Distance 값들은 이제 "픽셀"이 아니라 월드 스페이스 유닛이라
// 원래 UI 픽셀 값(200, 800, 60...)을 그대로 두면 카드 크기 기준으로 터무니없이 커진다.
// 카드 폭이 대략 1유닛인 걸 기준으로 값들을 다시 잡아뒀으니, 실제 카드 프리팹 크기에 맞춰
// 인스펙터에서 다시 조정해서 쓰면 된다.
//
// handTiltAngle (신규):
// UI에서는 카드가 항상 화면을 정면으로 봐서 회전이 Z축(부채꼴 기울기) 하나면 충분했지만,
// 3D에서는 손패 카드가 살짝 눕는 각도(X축)도 표현할 수 있어야 자연스럽다.
// 그래서 KTH_HandCard의 회전 관련 API를 float(Z만) -> Vector3(X+Z)로 바꾸고,
// 부채꼴로 펼쳐질 때만 X축에 handTiltAngle을 적용한다.
// 선택/배치 중앙으로 모일 때는 항상 Vector3.zero로 세워진다 (원래 로직 그대로).
//
// 책임 분리 (God Class 방지):
// 이 클래스는 손패 "전체" 상태(handCards 리스트, selectedCard, 카드 수 이벤트)의
// 유일한 주인이다. 그 상태 위에서 벌어지는 동작 중 두 덩어리는 별도 클래스로 뺐다
// (KTH_HandCard가 자기 동작을 컨트롤러들에게 위임하는 것과 같은 방식 - MonoBehaviour가
// 아니라 이 클래스를 owner로 들고 있는 순수 C# 클래스).
//   - KTH_HandCardPlacementFlow : 카드 확정 클릭 -> 배치 모드 -> LDY_CardPlacer 연동 ->
//     포커스 카드 주위 부채꼴
//   - KTH_HandCardGroupMotion   : 손패 컨테이너 통째로 내렸다 올리기, 카드 전체 가운데로
//     모으기/되돌리기
// 카드 한 장의 "정위치"를 계산하는 UpdateHandLayout은 위 둘 다에서 불리는 공용 진입점이라
// 계속 여기 남아있다 - 자리를 정하는 주체를 하나로 유지하기 위해서다.
public class KTH_HandCardLayout : MonoBehaviour
{
    public static KTH_HandCardLayout Instance { get; private set; }

    [Header("References")]
    [SerializeField] private LSO_WillPanel willPanel;

    [Header("Piece Placement (LDY_CardPlacer 연동)")]
    [Tooltip("카드를 확정했을 때 실제 기물 배치를 시작할 대상. LDY_CardPlacer는 이 스크립트에서 건드리지 않고 공개 API만 호출한다.")]
    [SerializeField] private LDY_CardPlacer cardPlacer;

    [Tooltip("배치가 끝난 카드를 버릴 더미. 비워두면 그냥 카드 오브젝트만 반납/파괴한다.")]
    [SerializeField] private KTH_DiscardCardUI discardPile;

    [Header("Arc Layout Settings")]
    [SerializeField] private float maxCardSpacing = 1.2f;
    [SerializeField] private float minCardSpacing = 0.5f;
    [SerializeField] private float maxHandWidth = 6f;
    [SerializeField] private float arcHeight = 0.4f;
    [SerializeField] private float maxRotation = 12f;

    [Header("Hand Tilt (3D 전용)")]
    [Tooltip("손패에서 카드가 X축으로 얼마나 누워있을지. 0이면 완전히 세워짐, 값이 커질수록 뒤로 눕는다.")]
    [SerializeField] private float handTiltAngle = 20f;

    [Header("Organic Motion Settings")]
    [SerializeField] private float staggerDelay = 0.025f;
    [SerializeField] private Ease moveEase = Ease.OutCubic;

    [Header("Hand Settings")]
    [SerializeField] private int maxHandSize = 8;

    [Header("Draw Animation Settings")]
    [SerializeField] private float drawDuration = 0.4f;

    [Header("Selection Settings")]
    [Tooltip("카드 선택 해제/제거 시 손패가 원래대로 정리되는 애니메이션 시간")]
    [SerializeField] private float pushDuration = 0.28f;

    [Header("Placement Mode Settings")]
    [SerializeField] private bool enableMoveDown;
    [SerializeField] private float placementMoveDownDistance = 1f;
    [SerializeField] private float placementMoveDuration = 0.3f;
    [SerializeField] private float placementCenterGap = 1f;

    private readonly List<KTH_HandCard> handCards =
        new List<KTH_HandCard>();

    private KTH_HandCard selectedCard;

    // 카드 프리팹에 LSO_CardWill 이 없다는 경고를 이미 냈는지. SetupCard 참고.
    private bool warnedMissingCardWill;

    private KTH_HandCardPlacementFlow placementFlow;
    private KTH_HandCardGroupMotion groupMotion;

    public int HandCount => handCards.Count;

    public int MaxHandSize
    {
        get => maxHandSize;
        set => maxHandSize = value;
    }

    public bool IsFull =>
        maxHandSize > 0 &&
        handCards.Count >= maxHandSize;

    public event Action<int, int> OnHandCountChanged;

    // placementFlow/groupMotion이 handCards/selectedCard를 읽고 쓸 때 쓰는 통로.
    // 이 클래스가 여전히 "자리를 정하는 주체" 하나이므로 값을 여기서만 바꾼다 -
    // 두 헬퍼는 이 프로퍼티를 거칠 뿐 자기 필드로 따로 들고 있지 않는다.
    internal List<KTH_HandCard> HandCards => handCards;

    internal KTH_HandCard SelectedCard
    {
        get => selectedCard;
        set => selectedCard = value;
    }

    private void Awake()
    {
        Instance = this;

        placementFlow = new KTH_HandCardPlacementFlow(
            this,
            cardPlacer,
            discardPile,
            handTiltAngle,
            arcHeight,
            maxRotation,
            maxCardSpacing,
            minCardSpacing,
            maxHandWidth,
            placementMoveDuration,
            placementCenterGap,
            moveEase
        );

        groupMotion = new KTH_HandCardGroupMotion(
            this,
            transform,
            enableMoveDown,
            placementMoveDownDistance,
            placementMoveDuration
        );
    }

    private void Update()
    {
        // 더블클릭으로 카드들이 내려가 있는 동안, 마우스 우클릭 한 번으로 그
        // 상태를 취소할 수 있게 한다. 활성화된 더블클릭이 없으면 CancelDoubleClick이
        // 알아서 아무 일도 하지 않고 false를 반환하므로 매 프레임 조건 없이 불러도 안전하다.
        if (Mouse.current != null &&
            Mouse.current.rightButton.wasPressedThisFrame)
        {
            bool wasActive = KTH_HandCard.CancelDoubleClick();

            // CancelActive()가 쓰는 PlayMoveUpAnimation은 각 카드가 들고 있는
            // OriginalLocalPosition으로 돌아가는데, 이 값이 그 순간 최신이
            // 아닐 수 있다(예: 부채꼴 재배치 애니메이션이 아직 안 끝난 도중이라
            // 새 자리로 갱신되기 전). 취소가 실제로 일어났다면 곧바로 손패
            // 재배치를 한 번 더 돌려서, 최신 계산값으로 무조건 맞춘다.
            //
            // 더블클릭은 항상 그 카드를 "확정(배치 모드)"까지 같이 켠다
            // (KTH_HandCard.OnPointerClick 참고). 그런데 여기서는 더블클릭이
            // 켠 "나머지 카드 내리기"만 취소하고 그 확정 상태는 그대로 두면,
            // 포커스 카드는 계속 중앙에 남고 나머지는 그 카드를 위해 자리를
            // 비워둔 부채꼴로만 남는다 - 우클릭으로 "취소"했는데도 손패가
            // 촘촘하게 다시 모이지 않고 계속 벌어져 보이는 원인이다.
            // 그래서 확정된 카드가 있으면 그 선택 상태까지 같이 취소한다.
            if (wasActive)
            {
                if (selectedCard != null && selectedCard.IsPlacementMode)
                {
                    selectedCard.CancelSelectionState();
                }

                // UpdateHandLayout / MoveToHandPositionWithDelay는 IsSelected인
                // 카드는 "다른 쪽에서 알아서 자리를 잡고 있다"고 보고 건너뛴다.
                // 그런데 호버 등 다른 경로로 "선택됨(들려 있음)" 상태가 된 카드가,
                // 여러 이벤트가 겹치는 순간(예: 마우스가 다른 카드로 넘어가는
                // 도중에 더블클릭이 겹침) KTH_HandCardLayout.selectedCard 갱신을
                // 놓치면 - 그 카드만 IsSelected가 true인 채로 영영 남아서
                // 재정렬 때마다 계속 건너뛰어지고, 혼자 제자리로 못 돌아온 채
                // 계속 떨어져 있게 된다. 우클릭 취소는 "손패를 확실히 원래대로"
                // 되돌리는 조작이므로, 여기서 남아있는 선택 상태를 전부 강제로
                // 정리해서 재정렬이 모든 카드를 빠짐없이 이동시키게 한다.
                for (int i = 0; i < handCards.Count; i++)
                {
                    KTH_HandCard stray = handCards[i];

                    if (stray != null && stray.IsSelected)
                    {
                        stray.CancelSelectionState();
                    }
                }

                UpdateHandLayout(null, pushDuration, false);
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void SetupCard(
        KTH_HandCard card,
        LSO_CardSO cardData)
    {
        if (card == null)
        {
            return;
        }

        // 카드 오브젝트는 풀에서 돌려쓴다. 지난 판에 붙인 유언이 그대로 남은 채로
        // 돌아오면, 방금 뽑은 카드가 이미 저주가 붙어 있는 것처럼 보이고
        // 그대로 소환된다. 새 카드 데이터를 넣는 이 자리에서 지운다.
        //
        // KTH_HandCard.ResetForPool 이 아니라 여기인 이유:
        // 버림 연출(KTH_DiscardAnimation) 경로는 카드를 Pool.Release 하지 않고
        // 버림 더미의 자식으로 부모만 바꿔 눌러앉힌다 - 그 경로에서는
        // ResetForPool 이 아예 안 불린다(KTH_HandCardDiscardHandler 주석 참고).
        // 반면 SetupCard 는 풀에서 왔든 새로 만들었든 손패로 들어오는 모든 카드가
        // 반드시 한 번 거친다.
        LSO_CardWill cardWill =
            card.GetComponentInChildren<LSO_CardWill>(true);

        if (cardWill != null)
        {
            cardWill.Clear();
        }
        else if (!warnedMissingCardWill)
        {
            // 카드 프리팹에 LSO_CardWill 이 없으면 유언을 아예 붙일 수 없다.
            // LSO_WillPainter 도 경고를 내지만 그쪽은 양초를 눌러야 나온다 -
            // 한 번도 안 눌러보면 배선이 빠진 채로 넘어간다. 그래서 여기서도 알린다.
            //
            // 다만 드로우할 때마다 불리는 자리라 매번 내면 콘솔이 덮인다.
            // 처음 한 번만 내고 그 뒤로는 침묵한다 - 배선이 빠졌다는 사실은
            // 한 줄이면 충분하고, 고치면 어차피 다시 안 나온다.
            warnedMissingCardWill = true;

            Debug.LogWarning(
                $"[KTH_HandCardLayout] 카드 '{card.name}' 에 LSO_CardWill 이 없어 " +
                "유언을 붙일 수 없습니다. 카드 프리팹에 그 컴포넌트를 붙여 주세요. " +
                "(이 경고는 한 번만 나옵니다)",
                card
            );
        }

        card.Setup(
            cardData,
            willPanel
        );
    }

    public bool CanAddCard()
    {
        return !IsFull;
    }

    public bool AddCard(KTH_HandCard card)
    {
        return AddCard(
            card,
            false
        );
    }

    public bool AddCard(
        KTH_HandCard card,
        Vector3 spawnerWorldPos)
    {
        bool insertAtFront =
            spawnerWorldPos.x >= 0f;

        return AddCard(
            card,
            insertAtFront
        );
    }

    public bool AddCard(
        KTH_HandCard card,
        bool insertAtFront)
    {
        if (card == null)
        {
            return false;
        }

        if (handCards.Contains(card))
        {
            return false;
        }

        if (IsFull)
        {
            return false;
        }

        KTH_HandCard.DeselectCurrent();

        // 버려졌다가 풀에서 재사용된 카드는 더블클릭 대상 목록에서 빠진 채로
        // 돌아온다(ResetForPool에서 뺐음). 손패에 다시 들어오는 이 시점에 다시
        // 등록한다.
        card.RegisterForDoubleClick();

        if (insertAtFront)
        {
            handCards.Insert(
                0,
                card
            );
        }
        else
        {
            handCards.Add(card);
        }

        // 확정 클릭(배치 시작/취소)을 여기서 받아서 placementFlow로 연결한다.
        card.OnCardClicked -= placementFlow.HandleCardConfirmClicked;
        card.OnCardClicked += placementFlow.HandleCardConfirmClicked;

        UpdateHandLayout(card);

        OnHandCountChanged?.Invoke(
            handCards.Count,
            maxHandSize
        );

        return true;
    }

    public void RemoveCard(
        KTH_HandCard card)
    {
        if (card != null)
        {
            card.OnCardClicked -= placementFlow.HandleCardConfirmClicked;
        }

        if (selectedCard == card)
        {
            selectedCard = null;
        }

        if (!handCards.Remove(card))
        {
            return;
        }

        UpdateHandLayout(
            null,
            pushDuration,
            false
        );

        OnHandCountChanged?.Invoke(
            handCards.Count,
            maxHandSize
        );
    }

    // =========================================================
    // Piece Placement (LDY_CardPlacer 연동) - KTH_HandCardPlacementFlow에 위임
    // =========================================================

    public void OnCardSelectionChanged(
        KTH_HandCard card,
        bool selected)
    {
        if (selected)
        {
            if (selectedCard != null &&
                selectedCard != card)
            {
                selectedCard.SetSelected(false);
            }

            selectedCard = card;

            return;
        }

        if (selectedCard == card)
        {
            selectedCard = null;

            UpdateHandLayout(
                null,
                pushDuration,
                false
            );
        }
    }

    public void EnterPlacementMode(KTH_HandCard card)
    {
        placementFlow.EnterPlacementMode(card);
    }

    public void ExitPlacementMode()
    {
        placementFlow.ExitPlacementMode();
    }

    public void UpdateHandLayout(
        KTH_HandCard newlyDrawnCard = null,
        float duration = 0.35f,
        bool useStagger = true)
    {
        // 더블클릭으로 "내려가 있는" 카드가 있는 채로 손패 재배치가 일어나면,
        // 그 카드는 더블클릭 쪽 좌표(isMovedDown)와 이 재배치 쪽 좌표를 각각
        // 다른 시점에 따로 계산해서 서로 자리를 다투게 된다(취소 시 엉뚱한
        // 자리로 튐). 재배치가 일어나는 순간 더블클릭 상태를 먼저 정리해서
        // 자리를 정하는 주체를 이 재배치 하나로 되돌린다.
        KTH_HandCard.CancelDoubleClick();

        int count =
            handCards.Count;

        if (count == 0)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            KTH_HandCard card =
                handCards[i];

            if (card == null)
            {
                continue;
            }

            var transformData =
                KTH_CardLayoutCalculator
                    .CalculateCardTransform(
                        i,
                        count,
                        maxCardSpacing,
                        minCardSpacing,
                        maxHandWidth,
                        arcHeight,
                        maxRotation
                    );

            Vector3 targetPosition =
                transformData.LocalPosition;

            Vector3 targetRotation =
                new Vector3(
                    handTiltAngle,
                    0f,
                    transformData.ZRotation
                );

            if (card == newlyDrawnCard)
            {
                card.PlayDrawAnimation(
                    targetPosition,
                    targetRotation,
                    drawDuration
                );

                card.UpdateOriginalTransform(
                    targetPosition,
                    targetRotation
                );

                continue;
            }

            card.UpdateOriginalTransform(
                targetPosition,
                targetRotation
            );

            if (!card.IsSelected)
            {
                float delay =
                    useStagger
                        ? i * staggerDelay
                        : 0f;

                card.MoveToHandPositionWithDelay(
                    targetPosition,
                    targetRotation,
                    duration,
                    delay,
                    moveEase
                );
            }
            else if (!card.IsConfirmed)
            {
                // 호버로만 들려있는(확정 전) 카드는 재배치를 건너뛰는데, 그대로 두면
                // 카드 수가 바뀌어 간격(cardSpacing)이 다시 계산될 때 예전 자리에
                // 계속 떠 있게 된다. 옆 카드가 새 간격으로 옮겨오면서 그 자리와
                // 겹치는 게 "손패 카드가 가끔 겹친다"는 증상의 원인이었다.
                // 방금 갱신한 새 OriginalLocalPosition 기준으로 들림 오프셋만
                // 다시 적용해서 새 슬롯 위로 옮긴다.
                card.RefreshSelectedOffset();
            }
        }

        if (selectedCard != null &&
            selectedCard.IsSelected)
        {
            selectedCard.BringToFront();
        }
    }

    // =========================================================
    // 손패 전체 묶음 동작 - KTH_HandCardGroupMotion에 위임
    // =========================================================

    public void MoveDownForPlacement()
    {
        groupMotion.MoveDownForPlacement();
    }

    public void GatherCardsToCenter(float duration)
    {
        groupMotion.GatherCardsToCenter(duration);
    }

    public void RestoreCardsFromCenter(float duration)
    {
        groupMotion.RestoreCardsFromCenter(duration);
    }

    public void MoveUpFromPlacement()
    {
        groupMotion.MoveUpFromPlacement();
    }
}
