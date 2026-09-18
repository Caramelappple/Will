using System.Collections.Generic;
using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.Will.Candle;
using DG.Tweening;
using UnityEngine;

public partial class KTH_HandCardLayout
{
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

        // 확정 클릭(배치 시작/취소)을 여기서 받아서 LDY_CardPlacer로 연결한다.
        card.OnCardClicked -= HandleCardConfirmClicked;
        card.OnCardClicked += HandleCardConfirmClicked;

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
            card.OnCardClicked -= HandleCardConfirmClicked;
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

    /// <summary>
    /// 손패를 통째로 버린다. 스테이지를 깼을 때 부른다.
    ///
    /// ClearHand 와 다르다. 이쪽은 **화면에 보이는 버림 연출을 태운다** —
    /// 카드가 버림 더미로 날아간다. 판이 뒤집히기 전이라 플레이어가 본다.
    ///
    /// 목록을 복사해서 도는 이유는 ConsumeAndRearrange 가 안에서 RemoveCard 로
    /// 원본을 건드리기 때문이다. 돌면서 지우면 중간부터 건너뛴다.
    /// </summary>
    public void DiscardHand(KTH_DiscardCardUI discardPile)
    {
        selectedCard = null;

        KTH_HandCard.CancelDoubleClick();

        List<KTH_HandCard> snapshot = new List<KTH_HandCard>(handCards);

        foreach (KTH_HandCard card in snapshot)
        {
            if (card == null) continue;

            KTH_HandCardDiscardHandler.ConsumeAndRearrange(card, discardPile, null);
        }
    }

    /// <summary>
    /// 손패를 통째로 비운다. 새 판을 세울 때 부른다.
    ///
    /// 한 장씩 RemoveCard 로 지우지 않는 이유는, 그때마다 재배치 애니메이션이
    /// 돌아 남은 카드가 우르르 움직이기 때문이다. 어차피 다 없앨 것이라
    /// 목록을 먼저 비우고 한 번만 알린다.
    ///
    /// 버린 카드 더미로 보내지 않는다. 판이 바뀌면 덱을 처음 상태로 되돌리므로
    /// 더미에 넣어봐야 곧바로 다시 걷힌다 — 넣었다 빼는 연출만 헛돈다.
    /// </summary>
    public void ClearHand()
    {
        // 배치 모드나 선택 상태가 남아 있으면 사라진 카드를 계속 가리킨다.
        selectedCard = null;

        KTH_HandCard.CancelDoubleClick();

        for (int i = 0; i < handCards.Count; i++)
        {
            KTH_HandCard card = handCards[i];

            if (card == null) continue;

            card.OnCardClicked -= HandleCardConfirmClicked;

            card.CancelSelectionState();
            card.transform.DOKill(true);

            KTH_HandCardDiscardHandler.ReleaseOrDestroy(card);
        }

        handCards.Clear();

        OnHandCountChanged?.Invoke(
            handCards.Count,
            maxHandSize
        );
    }

}
