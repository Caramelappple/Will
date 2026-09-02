using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 손패 카드 더블클릭 처리. 더블클릭된 카드를 제외한 나머지 카드를 아래로 내리고
/// OnCardDoubleClicked를 알린다. 더블클릭 시 "나머지 전부"에 접근해야 하므로,
/// 씬에 존재하는 모든 KTH_HandCard를 추적하는 정적 레지스트리도 여기서 관리한다.
/// </summary>
public class KTH_HandCardDoubleClickController
{
    private static readonly List<KTH_HandCard> allHandCards = new List<KTH_HandCard>();

    /// <summary>
    /// 카드가 더블클릭됐을 때 발생하는 static 이벤트.
    /// 파라미터로 더블클릭된 카드(KTH_HandCard)가 전달됨.
    /// </summary>
    public static event Action<KTH_HandCard> OnCardDoubleClicked;

    private readonly KTH_HandCard owner;
    private readonly KTH_Axis3D moveDownAxis;
    private readonly float moveDownAmount;
    private readonly float moveDownDuration;
    private readonly Ease moveDownEase;

    private bool isMovedDown;

    public bool IsMovedDown => isMovedDown;

    public KTH_HandCardDoubleClickController(
        KTH_HandCard owner,
        KTH_Axis3D moveDownAxis,
        float moveDownAmount,
        float moveDownDuration,
        Ease moveDownEase)
    {
        this.owner = owner;
        this.moveDownAxis = moveDownAxis;
        this.moveDownAmount = moveDownAmount;
        this.moveDownDuration = moveDownDuration;
        this.moveDownEase = moveDownEase;

        if (!allHandCards.Contains(owner))
        {
            allHandCards.Add(owner);
        }
    }

    /// <summary>현재 내려가 있는 모든 카드를 원래 위치로 복구.</summary>
    public static void RestoreAllCards()
    {
        for (int i = 0; i < allHandCards.Count; i++)
        {
            allHandCards[i]?.PlayMoveUpAnimation();
        }
    }

    public void HandleDoubleClick()
    {
        for (int i = 0; i < allHandCards.Count; i++)
        {
            KTH_HandCard card = allHandCards[i];

            if (card == null || card == owner)
            {
                continue;
            }

            card.PlayMoveDownAnimation();
        }

        OnCardDoubleClicked?.Invoke(owner);
    }

    /// <summary>이 카드를 moveDownAxis 방향으로 moveDownAmount 만큼 내리는 애니메이션.</summary>
    public void PlayMoveDownAnimation()
    {
        if (isMovedDown)
        {
            return;
        }

        isMovedDown = true;

        Vector3 targetPos = owner.OriginalLocalPosition;

        switch (moveDownAxis)
        {
            case KTH_Axis3D.X: targetPos.x -= moveDownAmount; break;
            case KTH_Axis3D.Y: targetPos.y -= moveDownAmount; break;
            case KTH_Axis3D.Z: targetPos.z -= moveDownAmount; break;
        }

        owner.transform.DOKill();

        owner.transform
            .DOLocalMove(targetPos, moveDownDuration)
            .SetEase(moveDownEase)
            .SetTarget(owner.transform);
    }

    /// <summary>내려갔던 카드를 원래 위치로 복구.</summary>
    public void PlayMoveUpAnimation()
    {
        if (!isMovedDown)
        {
            return;
        }

        isMovedDown = false;

        owner.transform.DOKill();

        owner.transform
            .DOLocalMove(owner.OriginalLocalPosition, moveDownDuration)
            .SetEase(moveDownEase)
            .SetTarget(owner.transform);
    }

    /// <summary>풀에서 재사용하기 전 상태 초기화.</summary>
    public void ResetForPool()
    {
        isMovedDown = false;
    }

    public void OnOwnerDestroyed()
    {
        allHandCards.Remove(owner);
    }
}
