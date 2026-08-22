using UnityEngine;

/// <summary>
/// 버린 카드 더미(KTH_DiscardCardUI)에 카드가 추가될 때마다
/// 덱과 손패가 모두 비었는지 확인하고, 둘 다 비었으면
/// 버린 카드 더미를 셔플해서 덱으로 되돌린다.
/// </summary>
public class KTH_DiscardCardManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KTH_DeckManager deckManager;
    [SerializeField] private KTH_HandCardLayout handLayout;
    [SerializeField] private KTH_DiscardCardUI discardPile;

    private void Awake()
    {
        if (deckManager == null) deckManager = FindAnyObjectByType<KTH_DeckManager>();
        if (handLayout == null) handLayout = FindAnyObjectByType<KTH_HandCardLayout>();
        if (discardPile == null) discardPile = FindAnyObjectByType<KTH_DiscardCardUI>();
    }

    private void OnEnable()
    {
        if (discardPile != null)
            discardPile.OnCardAdded += HandleCardAdded;
    }

    private void OnDisable()
    {
        if (discardPile != null)
            discardPile.OnCardAdded -= HandleCardAdded;
    }

    private void HandleCardAdded(int currentDiscardCount)
    {
        CheckAndReshuffleIfNeeded();
    }

    /// <summary>
    /// 덱과 손패가 모두 비어있는지 확인하고, 비어있다면 버린 카드 더미를 셔플해서 덱으로 되돌린다.
    /// 외부에서도 필요 시 직접 호출할 수 있도록 public으로 열어둔다.
    /// </summary>
    public void CheckAndReshuffleIfNeeded()
    {
        if (deckManager == null || handLayout == null || discardPile == null)
        {
            Debug.LogWarning($"[KTH_DiscardCardManager] 참조 누락! deckManager:{deckManager != null}, handLayout:{handLayout != null}, discardPile:{discardPile != null}");
            return;
        }

        int deckCount = deckManager.RemainingCards;
        int handCount = handLayout.HandCount;

        Debug.Log($"[KTH_DiscardCardManager] 상태 확인 - 덱: {deckCount} | 손패: {handCount} | 버린 카드: {discardPile.Count}");

        if (deckCount == 0 && handCount == 0)
        {
            bool reshuffled = deckManager.ReshuffleFromDiscard();
            if (reshuffled)
            {
                Debug.Log("[KTH_DiscardCardManager] 덱과 손패가 모두 비어 버린 카드 더미를 셔플하여 덱을 채웠습니다. 다시 드로우할 수 있습니다.");
            }
        }
    }
}