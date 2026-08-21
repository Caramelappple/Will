using System.Collections.Generic;
using _Scripts.LSO.Deck.Data;
using UnityEngine;

public class KTH_DeckManager : MonoBehaviour
{
    [Header("Deck Data")]
    [SerializeField] private List<LSO_CardSO> deck = new List<LSO_CardSO>();

    public IReadOnlyList<LSO_CardSO> Deck => deck;
    public int RemainingCards => deck.Count;

    private void Start()
    {
        InitDeck();
    }

    private void InitDeck()
    {
        var finalCardList = KTH_FinalCardList.Instance != null
            ? KTH_FinalCardList.Instance
            : FindAnyObjectByType<KTH_FinalCardList>();

        if (finalCardList != null && finalCardList.FinalSelectedCards != null)
        {
            deck.Clear();
            deck.AddRange(finalCardList.FinalSelectedCards);
            Debug.Log($"[KTH_DeckManager] 총 {deck.Count}장의 카드를 덱에 로드했습니다.");
        }
    }

    /// <summary>
    /// 덱 최상단 카드를 뽑아 반환합니다.
    /// </summary>
    public LSO_CardSO DrawCard()
    {
        if (deck.Count == 0)
        {
            Debug.LogWarning("[KTH_DeckManager] 덱에 남은 카드가 없습니다.");
            return null;
        }

        LSO_CardSO drawnCard = deck[0];
        deck.RemoveAt(0);
        return drawnCard;
    }
}