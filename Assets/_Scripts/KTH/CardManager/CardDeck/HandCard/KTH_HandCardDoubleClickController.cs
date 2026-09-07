using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 손패 카드 더블클릭 처리. "더블클릭으로 한 장을 활성화 <-> 취소" 하는 토글
/// 상태 기계다. 활성화되면 그 카드를 제외한 나머지 카드가 아래로 내려가고,
/// 취소되면(같은 카드를 다시 더블클릭 / 다른 카드로 넘어감) 전부 원래 자리로
/// 돌아온다. 더블클릭 시 "나머지 전부"에 접근해야 하므로, 씬에 존재하는 모든
/// KTH_HandCard를 추적하는 정적 레지스트리도 여기서 관리한다.
/// </summary>
public class KTH_HandCardDoubleClickController
{
    private static readonly List<KTH_HandCard> allHandCards = new List<KTH_HandCard>();

    // 지금 더블클릭으로 활성화돼 있는 카드. 없으면 null. 손패 전체에 하나만 존재한다.
    private static KTH_HandCard activeCard;

    /// <summary>
    /// 카드가 더블클릭으로 활성화됐을 때 발생하는 static 이벤트.
    /// 파라미터로 활성화된 카드(KTH_HandCard)가 전달됨.
    /// </summary>
    public static event Action<KTH_HandCard> OnCardDoubleClicked;

    /// <summary>
    /// 활성화돼 있던 더블클릭 상태가 취소됐을 때 발생하는 static 이벤트.
    /// (같은 카드를 다시 더블클릭했거나, 다른 카드를 더블클릭해서 전환된 경우 둘 다.)
    /// 파라미터로 "방금까지" 활성화돼 있던 카드가 전달됨.
    /// OnCardDoubleClicked를 구독해 무언가를 켰던 쪽은 이 이벤트에서 다시 꺼야 한다.
    /// </summary>
    public static event Action<KTH_HandCard> OnCardDoubleClickCancelled;

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

        RegisterInHand();
    }

    /// <summary>
    /// 이 카드를 "지금 손패에 있는 카드" 목록에 넣는다. 더블클릭이 "나머지 전부"를
    /// 찾을 때 이 목록을 쓴다.
    ///
    /// 생성자에서 한 번 불리지만, 그걸로 끝이 아니다. 카드는 버려질 때 Destroy되지
    /// 않고 풀에 반납만 되는 구조라(Awake는 다시 안 돈다), 나중에 같은 인스턴스가
    /// 다시 드로우돼 손패로 돌아오면 KTH_HandCardLayout.AddCard가 이걸 다시 불러서
    /// 등록해줘야 한다.
    /// </summary>
    public void RegisterInHand()
    {
        if (!allHandCards.Contains(owner))
        {
            allHandCards.Add(owner);
        }
    }

    /// <summary>
    /// 이 카드를 "지금 손패에 있는 카드" 목록에서 뺀다. 버림 더미로 가서 풀에
    /// 반납되는 시점(ResetForPool)에 부른다. 빼두지 않으면 버려진 카드가 계속
    /// "나머지 카드" 취급을 받아서, 다른 카드를 더블클릭할 때마다 버림 더미에 있는
    /// 카드까지 같이 내려가려 든다.
    /// </summary>
    public void UnregisterFromHand()
    {
        allHandCards.Remove(owner);

        if (activeCard == owner)
        {
            activeCard = null;
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

    /// <summary>
    /// 지금 활성화된 더블클릭 상태를 취소한다. 활성화된 게 없으면 아무 일도 하지
    /// 않고 false를 반환한다. 외부(취소 버튼 등)에서 직접 불러도 되고, 아래
    /// HandleDoubleClick도 이걸 쓴다.
    /// </summary>
    public static bool CancelActive()
    {
        if (activeCard == null)
        {
            return false;
        }

        KTH_HandCard cancelled = activeCard;
        activeCard = null;

        RestoreAllCards();

        OnCardDoubleClickCancelled?.Invoke(cancelled);

        return true;
    }

    public void HandleDoubleClick()
    {
        // KTH_HandCard.OnPointerClick이 더블클릭이든 아니든 항상 먼저 일반 확정
        // 클릭(HandleConfirmClick)을 실행한 뒤에 이걸 부른다. 즉 더블클릭은 "한 번
        // 클릭과 완전히 같은 확정 처리 + 이 아래 효과"를 더한 것이다. 확정 자체를
        // 취소하지 않는다.

        // 같은 카드를 다시 더블클릭해도 취소하지 않는다. 취소는 이제 마우스
        // 우클릭(KTH_HandCardLayout.Update -> CancelDoubleClick)으로만 한다.
        // 다른 카드가 활성화돼 있었다면 먼저 그쪽을 정리한다.
        // 취소 없이 바로 넘어가면, 이전 카드 때문에 켜졌던 것(예: 테스트 오브젝트)이
        // 계속 켜진 채로 남고, 카드들의 isMovedDown도 뒤섞여 "하나만 내려가는" 것처럼
        // 보이는 문제가 생긴다.
        CancelActive();

        activeCard = owner;

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

        ApplyMoveDownOffset();
    }

    /// <summary>
    /// 이미 내려가 있는 상태에서, "원래 자리"(OriginalLocalPosition)가 바뀌었을 때
    /// (예: 확정된 카드 주위로 부채꼴 재배치되면서 자리가 새로 계산됨) 그 새 자리를
    /// 기준으로 내려간 오프셋을 다시 적용한다.
    ///
    /// PlayMoveDownAnimation과 달리 isMovedDown 여부를 확인하지 않는다 - 이미
    /// 내려가 있는 카드를 "새로고침"하는 용도이기 때문이다.
    /// </summary>
    public void RefreshMoveDownOffset()
    {
        if (!isMovedDown)
        {
            return;
        }

        ApplyMoveDownOffset();
    }

    private void ApplyMoveDownOffset()
    {
        Vector3 targetPos = owner.OriginalLocalPosition;

        switch (moveDownAxis)
        {
            case KTH_Axis3D.X: targetPos.x -= moveDownAmount; break;
            case KTH_Axis3D.Y: targetPos.y -= moveDownAmount; break;
            case KTH_Axis3D.Z: targetPos.z -= moveDownAmount; break;
        }

        owner.transform.DOKill();

        // 위치만 트윈하면 안 된다. RefreshMoveDownOffset은 ApplyFanAroundFocalCard가
        // "이 카드는 새 부채꼴 자리 + 기울기로 가라"며 막 Join해둔 Sequence를 바로 위에서
        // DOKill로 죽이고 이 메서드를 부르는 경우가 있다(빠른 더블클릭으로 카드가 아직
        // 부채꼴 재배치 중일 때 내려가는 경우). 그때 회전을 안 건드리면 방금 죽은
        // Sequence가 걸어뒀던 기울기 회전이 통째로 날아가서 카드가 안 기울어진 채로
        // 멈춘다. OriginalLocalRotation은 이 시점에 이미 최신 목표 회전으로 갱신돼
        // 있으므로(UpdateOriginalTransform) 여기서도 같이 맞춰준다.
        Sequence sequence = DOTween.Sequence();
        sequence.SetTarget(owner.transform);

        sequence.Join(
            owner.transform.DOLocalMove(targetPos, moveDownDuration).SetEase(moveDownEase));

        sequence.Join(
            owner.transform.DOLocalRotate(owner.OriginalLocalRotation, moveDownDuration).SetEase(moveDownEase));
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

        // ApplyMoveDownOffset과 같은 이유로 회전도 같이 되돌린다 - 안 그러면
        // 내려가 있는 동안 부채꼴 재배치가 걸어둔 기울기가 복구 시 유지되지 않고
        // DOKill로 끊긴 채 남을 수 있다.
        Sequence sequence = DOTween.Sequence();
        sequence.SetTarget(owner.transform);

        sequence.Join(
            owner.transform.DOLocalMove(owner.OriginalLocalPosition, moveDownDuration).SetEase(moveDownEase));

        sequence.Join(
            owner.transform.DOLocalRotate(owner.OriginalLocalRotation, moveDownDuration).SetEase(moveDownEase));
    }

    /// <summary>풀에서 재사용하기 전 상태 초기화. 손패 목록에서도 뺀다(위 UnregisterFromHand 참고).</summary>
    public void ResetForPool()
    {
        isMovedDown = false;

        UnregisterFromHand();
    }

    public void OnOwnerDestroyed()
    {
        UnregisterFromHand();
    }
}
