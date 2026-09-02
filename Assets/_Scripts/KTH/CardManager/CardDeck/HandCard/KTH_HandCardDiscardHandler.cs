using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 손패 카드를 "다 썼을 때" 손패에서 빼고, 버림 더미가 있으면 그쪽 연출로 보내거나
/// 없으면 그냥 반납/파괴하는 흐름을 담당한다. 디스카드 연출 유무에 따라 갈라지는
/// 세 가지 경로가 전부 여기 모여 있다.
/// </summary>
public static class KTH_HandCardDiscardHandler
{
    public static void ConsumeAndRearrange(
        KTH_HandCard card,
        KTH_DiscardCardUI discardPile,
        Action onComplete)
    {
        // 선택 상태 해제
        card.CancelSelectionState();

        // 현재 카드의 기존 애니메이션 제거
        card.transform.DOKill(true);

        // ==================================================
        // 손패에서 제거
        // ==================================================

        if (KTH_HandCardLayout.Instance != null)
        {
            KTH_HandCardLayout.Instance.RemoveCard(card);
        }

        // ==================================================
        // 디스카드 더미가 없는 경우
        // ==================================================

        if (discardPile == null || discardPile.DiscardCardTransform == null)
        {
            ReleaseOrDestroy(card);

            onComplete?.Invoke();

            return;
        }

        // ==================================================
        // 디스카드 애니메이션 찾기
        // ==================================================

        KTH_DiscardAnimation discardAnimation = discardPile.GetComponent<KTH_DiscardAnimation>();

        if (discardAnimation == null)
        {
            discardAnimation = UnityEngine.Object.FindAnyObjectByType<KTH_DiscardAnimation>();
        }

        // ==================================================
        // 디스카드 애니메이션이 없는 경우
        // ==================================================

        if (discardAnimation == null)
        {
            discardPile.AddToDiscardPile(card.CardData);

            ReleaseOrDestroy(card);

            onComplete?.Invoke();

            return;
        }

        // ==================================================
        // 디스카드 애니메이션
        // ==================================================

        discardAnimation.Play(card, discardPile, card.CardData, onComplete);
    }

    public static void ReleaseOrDestroy(KTH_HandCard card)
    {
        if (KTH_HandCardPool.Instance != null)
        {
            KTH_HandCardPool.Instance.Release(card);
        }
        else
        {
            UnityEngine.Object.Destroy(card.gameObject);
        }
    }
}
