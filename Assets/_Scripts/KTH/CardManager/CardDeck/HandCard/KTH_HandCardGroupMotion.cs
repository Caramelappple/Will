using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// KTH_HandCardLayout에서 "손패 전체를 하나의 덩어리로 다루는" 동작만 뽑았다:
// 배치 모드에서 컨테이너 전체를 내렸다 올리기, 턴이 넘어갈 때 카드를 가운데로 모으기/되돌리기.
// 카드 한 장 한 장의 자리를 다시 계산하는 건(UpdateHandLayout) 그대로 KTH_HandCardLayout에 있다 -
// 여긴 그 결과를 부르기만 한다.
public class KTH_HandCardGroupMotion
{
    private readonly KTH_HandCardLayout owner;
    private readonly Transform containerTransform;
    private readonly bool enableMoveDown;
    private readonly float placementMoveDownDistance;
    private readonly float placementMoveDuration;

    private readonly Vector3 originalContainerLocalPos;
    private bool isCurrentlyDown;

    public KTH_HandCardGroupMotion(
        KTH_HandCardLayout owner,
        Transform containerTransform,
        bool enableMoveDown,
        float placementMoveDownDistance,
        float placementMoveDuration)
    {
        this.owner = owner;
        this.containerTransform = containerTransform;
        this.enableMoveDown = enableMoveDown;
        this.placementMoveDownDistance = placementMoveDownDistance;
        this.placementMoveDuration = placementMoveDuration;

        originalContainerLocalPos = containerTransform.localPosition;
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
        containerTransform.DOKill();

        containerTransform
            .DOLocalMoveY(
                targetY,
                placementMoveDuration
            )
            .SetEase(Ease.OutCubic);
    }

    public void GatherCardsToCenter(
        float duration)
    {
        List<KTH_HandCard> handCards =
            owner.HandCards;

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
        owner.UpdateHandLayout(
            null,
            duration,
            false
        );
    }
}
