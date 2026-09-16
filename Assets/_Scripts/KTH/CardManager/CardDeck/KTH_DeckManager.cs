using System;
using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.Reward;
using _Scripts.LSO.UI.Feedback;
using UnityEngine;
using Random = UnityEngine.Random;

public class KTH_DeckManager : MonoBehaviour
{
    [Header("Deck Data")]
    [SerializeField] private List<LSO_CardSO> deck =
        new List<LSO_CardSO>();

    [Header("Reshuffle Settings")]
    [Tooltip("되돌릴 버린 카드 더미.")]
    [SerializeField] private KTH_DiscardCardUI discardPile;

    /// <summary>
    /// 마지막 카드까지 실제로 뽑아서 덱이 빈 적이 있는지.
    ///
    /// 단순히 deck.Count == 0 을 보는 것과 다르다. 판을 새로 세우느라 잠깐
    /// 비어 있는 것과, 다 뽑아 써서 비운 것은 다른 상태다.
    ///
    /// **언제 되섞을지는 여기서 정하지 않는다.** 그건 LSO_DeckRefill 이 안다.
    /// </summary>
    private bool deckWasExhausted = false;

    // =========================================================
    // Properties
    // =========================================================

    public IReadOnlyList<LSO_CardSO> Deck =>
        deck;

    public int RemainingCards =>
        deck.Count;

    /// <summary>다 뽑아 써서 비었는지. 되섞을 때가 됐는지 보는 쪽이 쓴다.</summary>
    public bool IsExhausted =>
        deckWasExhausted;

    public event Action<int> OnDeckReshuffled;


    // =========================================================
    // Unity
    // =========================================================

    private void Start()
    {
        InitDeck();
    }


    // =========================================================
    // Deck 초기화
    // =========================================================

    /// <summary>
    /// 덱을 처음 상태로 되돌린다. 새 판을 세울 때 부른다.
    ///
    /// 보유 카드 전체로 덱을 다시 만들고 섞는다. 버린 카드 더미도 함께 비운다 —
    /// 어차피 덱을 통째로 다시 만들므로 더미에 남겨두면 그 카드가 두 번 존재하게 된다.
    ///
    /// 손패는 여기서 손대지 않는다. 그건 KTH_HandCardLayout 이 안다.
    /// </summary>
    public void ResetForNewStage()
    {
        if (discardPile != null)
            discardPile.ClearAndGetList();

        InitDeck();

        Debug.Log(
            "[KTH_DeckManager] 새 판 — 덱을 처음 상태로 되돌렸습니다."
        );
    }


    private void InitDeck()
    {
        LSO_ItemLibraryManager library =
            LSO_ItemLibraryManager.Instance;

        if (library == null)
        {
            Debug.LogWarning(
                "[KTH_DeckManager] ItemLibraryManager가 없어 " +
                "인스펙터 덱을 그대로 사용합니다.",
                this
            );

            ShuffleList(deck);

            deckWasExhausted =
                deck.Count == 0;

            return;
        }

        List<LSO_CardSO> owned =
            library.UnlockedPieces;

        if (owned == null ||
            owned.Count == 0)
        {
            Debug.LogWarning(
                "[KTH_DeckManager] 보유한 카드가 없습니다.",
                this
            );

            deck.Clear();

            deckWasExhausted = true;

            return;
        }

        deck.Clear();

        foreach (LSO_CardSO card in owned)
        {
            if (card == null)
            {
                continue;
            }

            deck.Add(card);
        }

        ShuffleList(deck);

        // 초기 덱이 비어있다면 소진 상태
        deckWasExhausted =
            deck.Count == 0;

        Debug.Log(
            $"[KTH_DeckManager] 보유 카드 {deck.Count}장을 " +
            $"덱에 로드하고 셔플했습니다."
        );
    }




    // =========================================================
    // Draw
    // =========================================================

    /// <summary>
    /// 덱에서 한 장을 꺼낸다. 못 꺼내면 null.
    ///
    /// 턴당 몇 장까지인지는 더 이상 여기서 세지 않는다. 판마다·턴마다 손패를
    /// 통째로 새로 받는 규칙이라 셀 것이 없고, 몇 장을 받는지는
    /// KTH_StartCardSet 이 정한다.
    /// </summary>
    public LSO_CardSO DrawCard()
    {

        // =====================================================
        // 덱이 비어 있음
        // =====================================================

        if (deck.Count == 0)
        {
            // 이미 소진 상태임을 확실하게 기록
            deckWasExhausted = true;

            // 덱이 비는 것도 정상이다. 적 턴이 시작되면 버린 더미가 돌아온다.
            LSO_RejectSignal.Raise(LSO_RejectReason.DeckEmpty);

            return null;
        }


        // =====================================================
        // 카드 뽑기
        // =====================================================

        LSO_CardSO drawnCard =
            deck[0];

        deck.RemoveAt(0);


        // =====================================================
        // ★ 마지막 카드인지 확인
        // =====================================================

        if (deck.Count == 0)
        {
            deckWasExhausted = true;

            Debug.Log(
                "[KTH_DeckManager] ★ 덱의 마지막 카드를 " +
                "소진했습니다. 적 턴 시작 시 리셔플합니다."
            );
        }


        Debug.Log(
            $"[KTH_DeckManager] 카드 드로우: " +
            $"{drawnCard.name} / " +
            $"남은 덱: {deck.Count}"
        );

        return drawnCard;
    }


    // =========================================================
    // Reshuffle
    // =========================================================

    public bool ReshuffleFromDiscard()
    {
        // =====================================================
        // 덱에 카드가 있다면 리셔플하지 않음
        // =====================================================

        if (deck.Count > 0)
        {
            return false;
        }


        // =====================================================
        // 버린 카드 더미 확인
        // =====================================================

        if (discardPile == null)
        {
            Debug.LogWarning(
                "[KTH_DeckManager] DiscardPile이 연결되지 않았습니다.",
                this
            );

            return false;
        }

        if (discardPile.Count == 0)
        {
            return false;
        }


        // =====================================================
        // 버린 카드 가져오기
        // =====================================================

        List<LSO_CardSO> reclaimed =
            discardPile.ClearAndGetList();

        if (reclaimed == null ||
            reclaimed.Count == 0)
        {
            return false;
        }


        // =====================================================
        // 셔플
        // =====================================================

        ShuffleList(reclaimed);


        // =====================================================
        // 덱에 추가
        // =====================================================

        deck.AddRange(reclaimed);

        // 다시 채워졌으니 소진 상태를 푼다.
        //
        // 이 값은 덱 자신의 것이라 여기서 되돌린다. 예전에는 되섞기를 부르는
        // 쪽이 풀었는데, 그러면 부르는 곳이 늘 때마다 그 줄을 같이 적어야 하고
        // 하나를 빠뜨리면 다음 적 턴에 또 되섞는다.
        deckWasExhausted = false;

        Debug.Log(
            $"[KTH_DeckManager] 버린 카드 더미 " +
            $"{reclaimed.Count}장을 셔플하여 " +
            $"덱으로 되돌렸습니다."
        );


        // =====================================================
        // 외부 알림
        // =====================================================

        OnDeckReshuffled?.Invoke(
            reclaimed.Count
        );

        return true;
    }


    // =========================================================
    // Shuffle
    // =========================================================

    private static void ShuffleList(
        List<LSO_CardSO> list)
    {
        if (list == null ||
            list.Count <= 1)
        {
            return;
        }

        for (
            int i = list.Count - 1;
            i > 0;
            i--
        )
        {
            int j =
                Random.Range(
                    0,
                    i + 1
                );

            (
                list[i],
                list[j]
            ) =
            (
                list[j],
                list[i]
            );
        }
    }
}
