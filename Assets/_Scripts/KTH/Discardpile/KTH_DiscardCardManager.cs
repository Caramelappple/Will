using _Scripts.KTH.CardManager.CardDeck;
using UnityEngine;

/// <summary>
/// 버린 카드 더미의 상태를 관리합니다.
///
/// 중요:
/// 이 클래스에서는 덱을 다시 채우지 않습니다.
///
/// 덱 리필 타이밍은 KTH_DeckManager가 담당합니다.
///
/// 플레이어 턴 종료
///     ↓
/// 적 턴 시작
///     ↓
/// KTH_DeckManager.HandleTurnChanged()
///     ↓
/// 덱이 비어 있으면 버린 카드 더미를 덱으로 복귀
/// </summary>
public class KTH_DiscardCardManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KTH_DeckManager deckManager;
    [SerializeField] private KTH_HandCardLayout handLayout;
    [SerializeField] private KTH_DiscardCardUI discardPile;

    private void Awake()
    {
        if (deckManager == null)
        {
            deckManager =
                FindAnyObjectByType<KTH_DeckManager>();
        }

        if (handLayout == null)
        {
            handLayout =
                FindAnyObjectByType<KTH_HandCardLayout>();
        }

        if (discardPile == null)
        {
            discardPile =
                FindAnyObjectByType<KTH_DiscardCardUI>();
        }
    }

    private void OnEnable()
    {
        if (discardPile != null)
        {
            discardPile.OnCardAdded += HandleCardAdded;
        }
    }

    private void OnDisable()
    {
        if (discardPile != null)
        {
            discardPile.OnCardAdded -= HandleCardAdded;
        }
    }

    /// <summary>
    /// 카드가 버린 카드 더미에 추가되었을 때 호출됩니다.
    ///
    /// 여기서는 절대로 덱을 리필하지 않습니다.
    ///
    /// 마지막 카드를 버렸더라도
    /// 플레이어 턴이 끝날 때까지 덱은 0장인 상태로 유지됩니다.
    /// </summary>
    private void HandleCardAdded(int currentDiscardCount)
    {
        Debug.Log(
            $"[KTH_DiscardCardManager] 카드 추가됨 - " +
            $"버린 카드: {currentDiscardCount}장"
        );

        // 중요:
        // 여기서 ReshuffleFromDiscard()를 호출하지 않는다.
        //
        // 덱 리필은 KTH_DeckManager가
        // 적 턴 시작 시 처리한다.
    }

    /// <summary>
    /// 현재 덱 / 손패 / 버린 카드 상태를 확인합니다.
    ///
    /// 주의:
    /// 이 메서드는 상태 확인만 합니다.
    /// 덱을 리필하지 않습니다.
    ///
    /// 기존 코드와의 호환을 위해 public으로 유지합니다.
    /// </summary>
    public void CheckAndReshuffleIfNeeded()
    {
        if (deckManager == null ||
            handLayout == null ||
            discardPile == null)
        {
            Debug.LogWarning(
                $"[KTH_DiscardCardManager] 참조 누락! " +
                $"deckManager:{deckManager != null}, " +
                $"handLayout:{handLayout != null}, " +
                $"discardPile:{discardPile != null}"
            );

            return;
        }

        int deckCount =
            deckManager.RemainingCards;

        int handCount =
            handLayout.HandCount;

        int discardCount =
            discardPile.Count;

        Debug.Log(
            $"[KTH_DiscardCardManager] 상태 확인 - " +
            $"덱: {deckCount} | " +
            $"손패: {handCount} | " +
            $"버린 카드: {discardCount}"
        );

        // ==================================================
        // 여기서 리필하지 않는다.
        // ==================================================
        //
        // 덱 0
        // 손패 0
        //
        // 이 상태가 되어도 플레이어 턴이 끝날 때까지
        // 그대로 유지한다.
        //
        // 실제 리필은 KTH_DeckManager가
        // Enemy 턴 시작 시 처리한다.
    }
}