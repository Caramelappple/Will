using System.Collections.Generic;
using _Scripts.LSO.Deck.Data;
using UnityEngine;

public class KTH_FinalCardList : MonoBehaviour
{
    public static KTH_FinalCardList Instance { get; private set; }

    [Header("Final Selected Card Data")]
    [SerializeField] private List<LSO_CardSO> finalSelectedCards = new List<LSO_CardSO>();

    public IReadOnlyList<LSO_CardSO> FinalSelectedCards => finalSelectedCards;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 카드 데이터 추가
    /// </summary>
    public void AddCard(LSO_CardSO cardData)
    {
        if (cardData == null) return;
        finalSelectedCards.Add(cardData);
    }

    /// <summary>
    /// 카드 데이터 제거
    /// </summary>
    public void RemoveCard(LSO_CardSO cardData)
    {
        if (cardData == null) return;
        finalSelectedCards.Remove(cardData);
    }

    /// <summary>
    /// 전체 카드 데이터 리셋
    /// </summary>
    public void ClearCards()
    {
        finalSelectedCards.Clear();
    }
}