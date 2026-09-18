using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public partial class KTH_HandCardLayout
{
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

        int leftCount = otherCount / 2;
        int rightCount = otherCount - leftCount;

        // 나머지가 딱 1장일 때는 위 균등분배 공식이 항상 오른쪽으로 밀어버려서
        // 원래 왼쪽에 있던 카드를 선택해도 반대편으로 튀어 보인다.
        // 이 경우만 실제 손패 순서를 보고 원래 있던 쪽으로 보낸다.
        if (otherCount == 1)
        {
            int focalIndex =
                handCards.IndexOf(focalCard);

            int otherIndex =
                handCards.IndexOf(otherCards[0]);

            bool otherIsOnLeft =
                focalIndex >= 0 &&
                otherIndex >= 0 &&
                otherIndex < focalIndex;

            leftCount = otherIsOnLeft ? 1 : 0;
            rightCount = otherIsOnLeft ? 0 : 1;
        }

        float spacing =
            CalculatePlacementSpacing(otherCount + 1);

        for (int i = 0; i < otherCount; i++)
        {
            KTH_HandCard card =
                otherCards[i];

            int relativeIndex =
                i < leftCount
                    ? i - leftCount
                    : i - leftCount + 1;

            float targetX =
                relativeIndex * spacing;

            targetX +=
                relativeIndex < 0
                    ? -centerGap
                    : centerGap;

            targetX += anchorX;

            int sideCount =
                relativeIndex < 0
                    ? leftCount
                    : rightCount;

            float normalized =
                Mathf.Clamp01(
                    Mathf.Abs(relativeIndex) /
                    (float)Mathf.Max(1, sideCount)
                );

            float targetY =
                -normalized * normalized * arcHeight;

            float targetRotationZ =
                relativeIndex < 0
                    ? normalized * maxRotation
                    : -normalized * maxRotation;

            // 여기도 부채꼴이라 겹친다. 손패와 같은 규칙으로 앞뒤를 준다 —
            // 배치 모드에서만 순서가 달라지면 눈에 걸린다.
            //
            // i 를 쓴다. relativeIndex 는 가운데를 비운 값이라 음수가 섞여
            // 깊이가 앞뒤로 튄다.
            Vector3 depthOffset =
                DepthAxis *
                (CardLayoutCalculator.DepthRank(i, otherCount, depthOrder) * depthStep);

            Vector3 targetPos =
                new Vector3(targetX, targetY, 0f) + depthOffset;

            Vector3 targetRot =
                new Vector3(handTiltAngle, 0f, targetRotationZ);

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

    /// <summary>
    /// 배치 모드: 선택된 카드는 중앙(Vector3.zero)으로, 나머지는 그 주위로 부채꼴 벌어짐.
    /// </summary>
    private void SpreadCardsAroundCenter()
    {
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

        // 앞으로 빼는 것이 **먼저다.** 아래 트윈이 그 값을 목표에 실어야 한다.
        // 예전에는 이 줄이 맨 끝에 있었고 목표도 Vector3.zero 였다 —
        // 그래서 고른 카드가 부채꼴로 흩어진 이웃들보다 뒤에 놓였다.
        selectedCard.BringToFront();

        Sequence selectedSequence =
            DOTween.Sequence();

        selectedSequence.SetTarget(selectedCard.transform);

        selectedSequence.Join(
            selectedCard.transform
                .DOLocalMove(selectedCard.FrontOffset, placementMoveDuration)
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
    }

    private float CalculatePlacementSpacing(
        int count)
    {
        if (count <= 1)
        {
            return maxCardSpacing;
        }

        float spacing =
            Mathf.Min(
                maxCardSpacing,
                maxHandWidth /
                (count - 1)
            );

        return Mathf.Max(
            minCardSpacing,
            spacing
        );
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
                CardLayoutCalculator
                    .CalculateCardTransform(
                        i,
                        count,
                        maxCardSpacing,
                        minCardSpacing,
                        maxHandWidth,
                        arcHeight,
                        maxRotation,
                        depthStep: depthStep,
                        depthOrder: depthOrder,
                        depthAxis: DepthAxis
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

    public void MoveDownForPlacement()
    {
        if (!enableMoveDown ||
            isCurrentlyDown)
        {
            return;
        }

        isCurrentlyDown = true;

        AnimateContainerY(
            originalContainerLocalPos.y -
            placementMoveDownDistance
        );
    }

    public void GatherCardsToCenter(
        float duration)
    {
        if (handCards.Count == 0)
        {
            return;
        }

        for (int i = 0; i < handCards.Count; i++)
        {
            KTH_HandCard card =
                handCards[i];

            if (card == null)
            {
                continue;
            }

            card.transform.DOKill();

            Sequence gatherSequence =
                DOTween.Sequence();

            gatherSequence.Join(
                card.transform
                    .DOLocalMove(
                        Vector3.zero,
                        duration
                    )
                    .SetEase(Ease.InBack)
            );

            gatherSequence.Join(
                card.transform
                    .DOLocalRotate(
                        Vector3.zero,
                        duration
                    )
                    .SetEase(Ease.InBack)
            );

            gatherSequence.Join(
                card.transform
                    .DOScale(
                        card.BaseScale,
                        duration
                    )
                    .SetEase(Ease.InBack)
            );
        }
    }

    public void RestoreCardsFromCenter(
        float duration)
    {
        UpdateHandLayout(
            null,
            duration,
            false
        );
    }

    public void MoveUpFromPlacement()
    {
        if (!isCurrentlyDown)
        {
            return;
        }

        isCurrentlyDown = false;

        AnimateContainerY(
            originalContainerLocalPos.y
        );
    }

    private void AnimateContainerY(
        float targetY)
    {
        transform.DOKill();

        transform
            .DOLocalMoveY(
                targetY,
                placementMoveDuration
            )
            .SetEase(Ease.OutCubic);
    }
}
