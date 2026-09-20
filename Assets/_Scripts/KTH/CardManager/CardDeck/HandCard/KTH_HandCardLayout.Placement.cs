using System;
using _Scripts.LDY;
using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.Will.Candle;
using DG.Tweening;
using UnityEngine;

public partial class KTH_HandCardLayout
{
    // =========================================================
    // Piece Placement (LDY_CardPlacer 연동)
    // =========================================================

    /// <summary>
    /// 카드를 클릭했을 때 호출된다. OnCardClicked는 "확정 클릭"과 "취소 클릭" 둘 다에서 불리는데,
    /// 이 시점에는 KTH_HandCard 내부에서 이미 상태를 바꿔놓은 뒤라 card.IsConfirmed로 구분할 수 있다.
    ///   - 확정 클릭 (배치 시작): IsConfirmed == true
    ///   - 취소 클릭 (배치 모드에서 다시 눌러서 취소): IsConfirmed == false
    /// </summary>
    private void HandleCardConfirmClicked(KTH_HandCard card)
    {
        if (card == null)
        {
            return;
        }

        // 취소 클릭이면 여기서 할 일이 없다. 선택 해제는 KTH_HandCard 쪽에서 이미 처리했다.
        if (!card.IsConfirmed)
        {
            card.LogClick("취소 클릭이라 배치를 시작하지 않음");
            return;
        }

        if (cardPlacer == null)
        {
            Debug.LogWarning(
                "[KTH_HandCardLayout] LDY_CardPlacer가 연결되지 않아 기물 배치를 시작할 수 없습니다.",
                this
            );

            card.CancelSelectionState();

            return;
        }

        // ── 예전에 여기 있던 가드 ─────────────────────────────────
        // "이미 배치 중이면 아무것도 하지 않는다"가 있었다. 같은 확정 클릭에서
        // KTH_CardPlacementController.TryBeginPlacement 도 배치를 시작하던 때,
        // 두 곳이 서로를 덮는 것을 막으려고 둔 것이다.
        //
        // **그 컨트롤러는 이제 없다.** 배치를 시작하는 곳은 여기 하나뿐이다.
        // 그래서 그 가드는 지킬 것이 없어졌고, 대신 카드를 번갈아 누를 때
        // 앞 카드의 배치가 남아 있으면 그 뒤로 어떤 카드도 못 고르게 막았다.
        //
        // 지금은 아래 ExitPlacementMode 가 앞 세션을 확실히 물리고,
        // BeginPlacement 자체도 "이미 배치 중이면 취소하고 새로 시작"한다.
        // 막을 것이 아니라 이어받으면 되는 일이었다.
        // ─────────────────────────────────────────────────────────

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

        // 앞 카드의 세션은 여기서 끝난다. 아래 BeginPlacement 가 그것을 취소하며
        // 콜백을 돌리는데, 그 안에서 placingCard 를 보고 판단하는 곳이 있으므로
        // 미리 비워 둔다. 새 값은 시작이 확정된 뒤에 적는다.
        placingCard = null;

        bool started =
            cardPlacer.BeginPlacement(
                cardData,
                LDY_Team.Player,
                cardWill: cardWill,
                onPlaced: animal =>
                {
                    if (placingCard == card) placingCard = null;

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
                    if (placingCard == card) placingCard = null;

                    // 우클릭 등으로 배치를 취소하면 카드는 손패에 남기고 선택만 푼다.
                    card.CancelSelectionState();
                }
            );

        // 시작이 확정된 뒤에 적는다. BeginPlacement 안에서 앞 세션이 취소되며
        // 콜백이 도는데, 그 전에 적어두면 방금 적은 값을 그 콜백이 지운다.
        if (started) placingCard = card;

        card.LogClick(started
            ? "배치 시작됨 — 이제 칸을 누르면 놓인다"
            : "배치를 시작하지 못함 (내 턴이 아니거나 코스트 부족)");

        if (started)
        {
            CardConfirmed?.Invoke(card);
        }
        else
        {
            // 내 턴이 아니거나 코스트가 부족해서 아예 시작을 못 한 경우.
            card.CancelSelectionState();
        }
    }

    /// <summary>
    /// 손패 카드를 클릭해 배치 모드가 실제로 시작됐을 때.
    /// 튜토리얼이 "카드를 하나 골라보세요"를 기다리는 데 쓴다.
    ///
    /// 정적인 이유는 손패가 씬마다 새로 생기기 때문이다. 듣는 쪽이 인스턴스를
    /// 물고 있으면 판이 바뀔 때 끊긴다.
    ///
    /// 호버도 시각 연출을 위해 SetSelected(true)를 사용한다. 따라서 선택 상태가 아니라
    /// BeginPlacement가 성공한 이 자리에서만 쏴야 호버를 클릭으로 오인하지 않는다.
    /// </summary>
    public static event Action<KTH_HandCard> CardConfirmed;

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

    public void EnterPlacementMode(
        KTH_HandCard card)
    {
        if (card == null)
        {
            return;
        }

        // 손패 목록에 없는 카드가 확정됐다는 뜻이다.
        //
        // 카드는 "지금 손패에 있다"를 두 곳에 남긴다 — handCards 와 더블클릭
        // 레지스트리(allHandCards). 둘이 어긋나면 카드는 확정된 것처럼 보이는데
        // 부채꼴 재배치에서는 빠져, 눌러도 아무 반응이 없는 것처럼 보인다.
        //
        // 조용히 돌아서면 그 어긋남이 영영 안 드러난다.
        if (!handCards.Contains(card))
        {
            Debug.LogWarning(
                $"[KTH_HandCardLayout] '{card.name}' 가 손패 목록에 없는데 확정됐습니다. " +
                "버려지는 중인 카드를 눌렀거나, 손패에서 뺄 때 선택이 안 풀린 것입니다.",
                card);

            card.CancelSelectionState();

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

        selectedCard = card;

        MoveSelectedCardToCenter();
    }

    /// <summary>
    /// 고른 카드를 전부 내려놓는다. 턴이 넘어갈 때 부른다.
    ///
    /// ── 왜 따로 필요한가 ──────────────────────────────────────
    /// 턴이 끝나면 LDY_CardPlacer 가 배치를 물리면서 그 카드는 내려간다.
    /// 그런데 **배치까지 안 간 카드**는 아무도 안 내려놓는다 — 골라서 위로
    /// 올라와 있기만 한 카드, 코스트가 모자라 배치가 시작되지 않은 카드가
    /// 그렇다. 그대로 적 턴 내내 올라와 있는다.
    ///
    /// 손패 전체를 훑어 고른 상태를 푼다. 이미 내려온 카드는
    /// CancelSelectionState 가 첫 줄에서 돌아서므로 헛돌지 않는다.
    /// ─────────────────────────────────────────────────────────
    /// </summary>
    public void DeselectAll()
    {
        // 더블클릭으로 내려가 있는 카드들도 같이 올린다.
        KTH_HandCard.CancelDoubleClick();

        // ── 배치 중이던 카드도 여기서 놓는다 ──────────────────────
        // 아래 훑기는 IsSelected · IsConfirmed 만 본다. 칸을 고르는 중인 카드는
        // 그 둘로는 안 잡히고, placingCard 로만 남아 있다.
        //
        // 그대로 두면 턴이 넘어가도 배치 세션이 살아 있다. 다음 내 턴에
        // 카드를 누르면 "앞의 배치가 아직 안 끝났다"에 걸려 아무것도 안 된다.
        //
        // 턴이 끝나면 고른 것은 카드든 칸이든 전부 놓는다 — 예외를 두지 않는다.
        // ─────────────────────────────────────────────────────────
        if (placingCard != null) ExitPlacementMode();

        // 뒤에서부터 돈다. CancelSelectionState 안에서 재배치가 돌며
        // 목록을 건드릴 수 있다.
        for (int i = handCards.Count - 1; i >= 0; i--)
        {
            if (i >= handCards.Count) continue;

            KTH_HandCard card = handCards[i];

            if (card == null) continue;
            if (!card.IsSelected && !card.IsConfirmed) continue;

            card.CancelSelectionState();
        }

        selectedCard = null;
    }

    /// <summary>
    /// 배치 모드에서 빠져나온다. 카드의 선택이 풀릴 때(KTH_HandCardSelectionController)
    /// 불린다.
    ///
    /// ── 왜 여기서 배치까지 물리는가 ───────────────────────────
    /// "지금 고른 카드"를 두 곳이 따로 들고 있었다.
    ///   · 손패는 currentConfirmed 로
    ///   · LDY_CardPlacer 는 _pendingCard 로
    ///
    /// 카드를 번갈아 누르면 손패 쪽만 새 카드로 바뀌고 배치 쪽은 앞 카드를
    /// 그대로 쥐고 있었다. 보드를 안 누르고 카드를 눌렀으니 그 세션은 끝날
    /// 일이 없다. 그 뒤로는 어떤 카드를 눌러도 "앞의 배치가 안 끝났다"로
    /// 막혔다 — 손패가 고장 난 것처럼 보이지만 물린 곳은 배치 쪽이었다.
    ///
    /// 그래서 선택이 풀리는 이 자리에서 배치도 같이 물린다. 두 값이 어긋날
    /// 틈을 없애는 것이 요점이다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 이미 놓고 나온 경우에는 아무 일도 하지 않는다 — 그때는 LDY_CardPlacer 가
    /// 스스로 IsPlacing 을 내려놓은 뒤라 CancelPlacement 가 곧바로 돌아선다.
    /// </summary>
    /// <param name="card">
    /// 배치 모드에서 빠져나오는 카드.
    ///
    /// **이 카드가 시작한 배치일 때만 물린다.** 그냥 물리면 남이 시작한 세션까지
    /// 죽는다. 이를테면 카드를 한 장 뽑을 때 AddCard 가 DeselectCurrent 를 부르는데,
    /// 그때 방금 고른 카드의 배치가 함께 끊긴다 — 고른 적도 없는 카드 때문에
    /// 선택이 풀리는 것이라 원인을 짐작할 방법이 없다.
    ///
    /// null 이면 누가 부른지 모르는 경우라 그냥 물린다.
    /// </param>
    public void ExitPlacementMode(KTH_HandCard card = null)
    {
        selectedCard = null;

        bool mine = card == null || card == placingCard;

        if (mine)
        {
            // ── 비우는 것과 취소하는 것을 묶지 않는다 ──────────────
            // 예전에는 cardPlacer 가 있을 때만 placingCard 를 비웠다. 배선이
            // 빠지면 배치 중 표시가 영영 남아, 그 뒤로는 어떤 카드도 고를 수
            // 없었다. 무엇이 막고 있는지도 화면에 안 나온다.
            //
            // 표시는 이 클래스의 것이므로 언제나 비운다. 놓는 쪽을 물리는 것은
            // 그 다음이고, 부를 상대가 없으면 그 사실을 남긴다.
            // ─────────────────────────────────────────────────────
            placingCard = null;

            if (cardPlacer != null)
                cardPlacer.CancelPlacement();
            else
                Debug.LogWarning(
                    $"{name}: Card Placer 가 비어 있어 배치를 물리지 못했습니다. " +
                    "고른 표시만 풀었습니다.", this);
        }

        UpdateHandLayout(
            null,
            placementMoveDuration,
            false
        );
    }

    private void MoveSelectedCardToCenter()
    {
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

        // 정중앙으로 데려가되 **앞으로 뺀 만큼은 지킨다.**
        //
        // ── 왜 Vector3.zero 가 아닌가 ─────────────────────────────
        // 예전에는 그냥 원점으로 보냈다. 원점은 깊이가 0 이다. 그런데 부채꼴로
        // 흩어진 나머지 카드는 0.03 ~ 0.21 만큼 앞에 서 있다.
        //
        // 즉 BringToFront 로 앞으로 뺀 것을 이 트윈이 도로 끌어내려서,
        // 고른 카드가 손패에서 **제일 뒤**에 놓였다. 가운데 크게 뜬 카드 위로
        // 옆 카드의 그림과 글자가 덮이던 것이 그것이다.
        //
        // BringToFront 가 방금 더한 값을 그대로 목표에 싣는다. 얼마나 빼는지는
        // 손패가 정하므로(FrontDepthDistance) 여기서 다시 계산하지 않는다.
        // ─────────────────────────────────────────────────────────
        Vector3 centerTarget = selectedCard.FrontOffset;

        moveToCenterSequence.Join(
            selectedCard.transform
                .DOLocalMove(
                    centerTarget,
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
            if (selectedCard == null)
            {
                return;
            }

            if (!selectedCard.IsPlacementMode)
            {
                return;
            }

            SpreadCardsAroundCenter();
        });
    }

}
