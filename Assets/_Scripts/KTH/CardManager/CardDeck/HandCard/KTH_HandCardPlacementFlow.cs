using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.Will.Candle;
using DG.Tweening;
using UnityEngine;

// KTH_HandCardLayout에서 "카드 확정 클릭 -> 배치 모드 -> LDY_CardPlacer 연동 -> 포커스 카드
// 주위로 나머지가 부채꼴로 벌어지기"만 뽑았다. 손패 리스트/선택된 카드 같은 전체 상태는
// 여전히 KTH_HandCardLayout이 들고 있고, 여기는 owner를 통해 그 상태를 읽고 쓴다
// (KTH_HandCard가 자기 컨트롤러들에게 하는 것과 같은 방식).
public class KTH_HandCardPlacementFlow
{
    private readonly KTH_HandCardLayout owner;
    private readonly LDY_CardPlacer cardPlacer;
    private readonly KTH_DiscardCardUI discardPile;

    private readonly float handTiltAngle;
    private readonly float arcHeight;
    private readonly float maxRotation;
    private readonly float maxCardSpacing;
    private readonly float minCardSpacing;
    private readonly float maxHandWidth;
    private readonly float placementMoveDuration;
    private readonly float placementCenterGap;
    private readonly Ease moveEase;

    public KTH_HandCardPlacementFlow(
        KTH_HandCardLayout owner,
        LDY_CardPlacer cardPlacer,
        KTH_DiscardCardUI discardPile,
        float handTiltAngle,
        float arcHeight,
        float maxRotation,
        float maxCardSpacing,
        float minCardSpacing,
        float maxHandWidth,
        float placementMoveDuration,
        float placementCenterGap,
        Ease moveEase)
    {
        this.owner = owner;
        this.cardPlacer = cardPlacer;
        this.discardPile = discardPile;
        this.handTiltAngle = handTiltAngle;
        this.arcHeight = arcHeight;
        this.maxRotation = maxRotation;
        this.maxCardSpacing = maxCardSpacing;
        this.minCardSpacing = minCardSpacing;
        this.maxHandWidth = maxHandWidth;
        this.placementMoveDuration = placementMoveDuration;
        this.placementCenterGap = placementCenterGap;
        this.moveEase = moveEase;
    }

    /// <summary>
    /// 카드를 클릭했을 때 호출된다. OnCardClicked는 "확정 클릭"과 "취소 클릭" 둘 다에서 불리는데,
    /// 이 시점에는 KTH_HandCard 내부에서 이미 상태를 바꿔놓은 뒤라 card.IsConfirmed로 구분할 수 있다.
    ///   - 확정 클릭 (배치 시작): IsConfirmed == true
    ///   - 취소 클릭 (배치 모드에서 다시 눌러서 취소): IsConfirmed == false
    /// </summary>
    public void HandleCardConfirmClicked(KTH_HandCard card)
    {
        if (card == null)
        {
            return;
        }

        // 취소 클릭이면 여기서 할 일이 없다. 선택 해제는 KTH_HandCard 쪽에서 이미 처리했다.
        if (!card.IsConfirmed)
        {
            return;
        }

        if (cardPlacer == null)
        {
            Debug.LogWarning(
                "[KTH_HandCardPlacementFlow] LDY_CardPlacer가 연결되지 않아 기물 배치를 시작할 수 없습니다.",
                owner
            );

            card.CancelSelectionState();

            return;
        }

        // KTH_HandCard.OnPointerClick은 같은 확정 클릭에서
        // KTH_InfoPanel.SelectInfoPanl() -> KTH_CardPlacementController.TryBeginPlacement()도
        // 먼저 호출한다. 그쪽이 이미 cardPlacer.BeginPlacement로 배치를 시작해놓은 상태에서
        // 여기서 또 BeginPlacement를 부르면, LDY_CardPlacer가 "이미 배치 중이면 취소하고
        // 새로 시작"하는 구조라 방금 시작된 세션이 조용히 취소되고 콜백이 이 경로 것으로
        // 바뀌어버린다. 배치 세션을 시작하는 주체가 매 클릭마다 둘로 갈리면서 카드가 보드에
        // 놓이는 흐름/위치가 꼬이는 원인이 되므로, 이미 배치가 시작돼 있으면 여기서는
        // 손대지 않는다.
        if (cardPlacer.IsPlacing)
        {
            return;
        }

        LSO_CardSO cardData =
            card.CardData;

        if (cardData == null)
        {
            card.CancelSelectionState();

            return;
        }

        // 유언 칸을 통째로 넘긴다. **값을 읽어서 넘기면 안 된다** —
        // 지금은 아직 양초로 안 붙였을 수 있고, 칸을 고르는 사이에 붙이기 때문이다.
        // 값은 실제로 놓는 순간 LDY_CardPlacer 가 읽는다.
        LSO_CardWill cardWill =
            card.GetComponentInChildren<LSO_CardWill>(true);

        bool started =
            cardPlacer.BeginPlacement(
                cardData,
                LDY_Team.Player,
                cardWill: cardWill,
                onPlaced: animal =>
                {
                    if (animal != null)
                    {
                        // 실제로 보드에 놓였을 때만 손패에서 빼고 버린다.
                        card.ConsumeAndRearrange(
                            discardPile
                        );
                    }
                    else
                    {
                        // 칸이 막혀있거나 실패한 경우 카드는 손패에 그대로 두고 선택만 푼다.
                        card.CancelSelectionState();
                    }
                },
                onCancelled: () =>
                {
                    // 우클릭 등으로 배치를 취소하면 카드는 손패에 남기고 선택만 푼다.
                    card.CancelSelectionState();
                }
            );

        if (!started)
        {
            // 내 턴이 아니거나 코스트가 부족해서 아예 시작을 못 한 경우.
            card.CancelSelectionState();
        }
    }

    public void EnterPlacementMode(KTH_HandCard card)
    {
        if (card == null)
        {
            return;
        }

        List<KTH_HandCard> handCards =
            owner.HandCards;

        if (!handCards.Contains(card))
        {
            return;
        }

        // 더블클릭으로 내려가 있는 카드가 있으면 부채꼴 재배치와 자리를 다투게
        // 되므로, 배치 모드로 들어가기 전에 먼저 정리한다.
        KTH_HandCard.CancelDoubleClick();

        // 확정된 카드가 아닌데도 호버로 선택된 채 남아있는 다른 카드가 있으면,
        // 부채꼴로 흩어지는 동안 UpdateHandLayout이 그 카드의 이동만 건너뛴다
        // (card.IsSelected면 MoveToHandPositionWithDelay가 자리를 옮기지 않음).
        // 그 상태로 두면 나중에 배치를 취소해도 그 카드만 계속 엉뚱한 자리에 남는다.
        // 배치 모드에 들어가기 전에 미리 정리해서 그런 카드가 없게 한다.
        for (int i = 0; i < handCards.Count; i++)
        {
            KTH_HandCard other = handCards[i];

            if (other == null || other == card)
            {
                continue;
            }

            if (other.IsSelected && !other.IsConfirmed)
            {
                other.CancelSelectionState();
            }
        }

        owner.SelectedCard = card;

        MoveSelectedCardToCenter();
    }

    public void ExitPlacementMode()
    {
        owner.SelectedCard = null;

        owner.UpdateHandLayout(
            null,
            placementMoveDuration,
            false
        );
    }

    private void MoveSelectedCardToCenter()
    {
        KTH_HandCard selectedCard =
            owner.SelectedCard;

        if (selectedCard == null)
        {
            return;
        }

        selectedCard.transform.DOKill();
        selectedCard.BringToFront();

        Sequence moveToCenterSequence =
            DOTween.Sequence();

        moveToCenterSequence.SetTarget(
            selectedCard.transform
        );

        moveToCenterSequence.Join(
            selectedCard.transform
                .DOLocalMove(
                    Vector3.zero,
                    placementMoveDuration
                )
                .SetEase(Ease.OutBack)
        );

        // 위치는 정중앙이니 부채꼴 Z축 기울기는 0으로 되돌린다.
        // X축(handTiltAngle, 눕는 각도)은 그대로 유지한다.
        moveToCenterSequence.Join(
            selectedCard.transform
                .DOLocalRotate(
                    new Vector3(handTiltAngle, 0f, 0f),
                    placementMoveDuration
                )
                .SetEase(Ease.OutBack)
        );

        moveToCenterSequence.Join(
            selectedCard.transform
                .DOScale(
                    selectedCard.BaseScale *
                    selectedCard.SelectScale,
                    placementMoveDuration
                )
                .SetEase(Ease.OutBack)
        );

        moveToCenterSequence.OnComplete(() =>
        {
            KTH_HandCard current =
                owner.SelectedCard;

            if (current == null)
            {
                return;
            }

            if (!current.IsPlacementMode)
            {
                return;
            }

            SpreadCardsAroundCenter();
        });
    }

    /// <summary>
    /// 배치 모드: 선택된 카드는 중앙(Vector3.zero)으로, 나머지는 그 주위로 부채꼴 벌어짐.
    /// </summary>
    private void SpreadCardsAroundCenter()
    {
        KTH_HandCard selectedCard =
            owner.SelectedCard;

        if (selectedCard == null)
        {
            return;
        }

        ApplyFanAroundFocalCard(
            selectedCard,
            0f,
            placementCenterGap,
            placementMoveDuration
        );

        selectedCard.transform.DOKill();

        Sequence selectedSequence =
            DOTween.Sequence();

        selectedSequence.SetTarget(selectedCard.transform);

        selectedSequence.Join(
            selectedCard.transform
                .DOLocalMove(Vector3.zero, placementMoveDuration)
                .SetEase(Ease.OutBack)
        );

        selectedSequence.Join(
            selectedCard.transform
                .DOLocalRotate(
                    new Vector3(handTiltAngle, 0f, 0f),
                    placementMoveDuration
                )
                .SetEase(Ease.OutBack)
        );

        selectedSequence.Join(
            selectedCard.transform
                .DOScale(
                    selectedCard.BaseScale * selectedCard.SelectScale,
                    placementMoveDuration
                )
                .SetEase(Ease.OutBack)
        );

        selectedCard.BringToFront();
    }

    /// <summary>
    /// focalCard를 뺀 나머지 카드들이 focalCard를 기준으로 좌우 부채꼴로 벌어질 목표
    /// 위치/회전을 계산해서 애니메이션까지 실행한다.
    ///
    /// 배치 모드(포커스 카드가 중앙 Vector3.zero로 이동한 상태, anchorX = 0)와
    /// 호버(포커스 카드가 자기 자리에 그대로 있는 상태, anchorX = 그 자리의 X)
    /// 양쪽에서 같이 쓴다.
    ///
    /// 나머지 카드는 실제 손패상의 좌/우 순서가 아니라 "항상 절반씩 좌우로 균등 분배"한다.
    /// (가장자리 카드를 선택해도 나머지가 한쪽으로 쏠리지 않고 중앙 기준으로 고르게 펼쳐짐)
    /// </summary>
    private void ApplyFanAroundFocalCard(
        KTH_HandCard focalCard,
        float anchorX,
        float centerGap,
        float duration)
    {
        List<KTH_HandCard> handCards =
            owner.HandCards;

        int count = handCards.Count;

        if (count <= 1 || focalCard == null)
        {
            return;
        }

        List<KTH_HandCard> otherCards =
            new List<KTH_HandCard>();

        for (int i = 0; i < count; i++)
        {
            KTH_HandCard card =
                handCards[i];

            if (card == null ||
                card == focalCard)
            {
                continue;
            }

            otherCards.Add(card);
        }

        int otherCount =
            otherCards.Count;

        if (otherCount == 0)
        {
            return;
        }

        // otherCount == 1일 때만 실제 손패 상 좌/우 위치가 필요하다
        // (그 외엔 CalculateSideCounts가 이 값을 안 쓴다).
        bool singleCardIsOnLeft = false;

        if (otherCount == 1)
        {
            int focalIndex =
                handCards.IndexOf(focalCard);

            int otherIndex0 =
                handCards.IndexOf(otherCards[0]);

            singleCardIsOnLeft =
                focalIndex >= 0 &&
                otherIndex0 >= 0 &&
                otherIndex0 < focalIndex;
        }

        KTH_CardFanLayoutCalculator.CalculateSideCounts(
            otherCount,
            singleCardIsOnLeft,
            out int leftCount,
            out int rightCount
        );

        float spacing =
            KTH_CardFanLayoutCalculator.CalculateSpacing(
                otherCount + 1,
                maxCardSpacing,
                minCardSpacing,
                maxHandWidth
            );

        for (int i = 0; i < otherCount; i++)
        {
            KTH_HandCard card =
                otherCards[i];

            KTH_CardFanLayoutCalculator.FanSlot slot =
                KTH_CardFanLayoutCalculator.CalculateSlot(
                    i,
                    leftCount,
                    rightCount,
                    spacing,
                    centerGap,
                    anchorX,
                    arcHeight,
                    maxRotation
                );

            Vector3 targetPos =
                slot.LocalPosition;

            Vector3 targetRot =
                new Vector3(handTiltAngle, 0f, slot.ZRotation);

            card.transform.DOKill();

            Sequence sequence =
                DOTween.Sequence();

            sequence.SetTarget(card.transform);

            sequence.Join(
                card.transform
                    .DOLocalMove(targetPos, duration)
                    .SetEase(moveEase)
            );

            sequence.Join(
                card.transform
                    .DOLocalRotate(targetRot, duration)
                    .SetEase(moveEase)
            );

            sequence.Join(
                card.transform
                    .DOScale(card.BaseScale, duration)
                    .SetEase(moveEase)
            );

            card.UpdateOriginalTransform(targetPos, targetRot);

            // 더블클릭으로 내려가 있는 카드는 "원래 자리"가 방금 새로 계산한
            // 부채꼴 자리로 갱신됐으니, 그 새 자리를 기준으로 내려간 오프셋을
            // 다시 적용한다. 그래야 부채꼴로도 벌어지고 내려간 채로도 있는
            // 두 효과가 같이 보인다.
            if (card.IsMovedDown)
            {
                card.RefreshMoveDownOffset();
            }
        }
    }
}
