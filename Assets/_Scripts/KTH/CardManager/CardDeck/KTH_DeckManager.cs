using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LSO.Deck.Data;
using UnityEngine;

public class KTH_DeckManager : MonoBehaviour
{
    [Header("Deck Data")]
    [SerializeField] private List<LSO_CardSO> deck = new List<LSO_CardSO>();
    [SerializeField] private LDY_TurnManager turnManager;

    [Header("Reshuffle Settings")]
    [Tooltip("덱이 비었을 때 다시 채워올 버린 카드 더미")]
    [SerializeField] private KTH_DiscardCardUI discardPile;

    [Tooltip("체크하면 드로우 시도 시(덱이 비었을 때) 자동으로 버린 카드 더미를 셔플하여 덱을 채운다")]
    [SerializeField] private bool autoReshuffleFromDiscard = true;

    [Header("Draw Limit Settings")]
    [Tooltip("체크하면 턴/횟수 제한 없이 언제든 드로우 가능")]
    [SerializeField] private bool ignoreDrawLimit = false;

    [Tooltip("턴당 드로우 가능 횟수 (ignoreDrawLimit이 false일 때만 적용)")]
    [SerializeField] private int maxDrawsPerTurn = 2;

    private int drawsUsedThisTurn = 0;

    public IReadOnlyList<LSO_CardSO> Deck => deck;
    public int RemainingCards => deck.Count;

    public bool IgnoreDrawLimit
    {
        get => ignoreDrawLimit;
        set => ignoreDrawLimit = value;
    }

    public int MaxDrawsPerTurn
    {
        get => maxDrawsPerTurn;
        set => maxDrawsPerTurn = value;
    }

    public int DrawsUsedThisTurn => drawsUsedThisTurn;
    public int DrawsRemainingThisTurn => ignoreDrawLimit
        ? int.MaxValue
        : Mathf.Max(0, maxDrawsPerTurn - drawsUsedThisTurn);

    /// <summary>
    /// 드로우 횟수 제한이 바뀔 때(리셋 등) 외부(UI, KTH_SpawnCard 등)에 알립니다.
    /// </summary>
    public event System.Action OnDrawLimitChanged;

    /// <summary>
    /// 버린 카드 더미를 셔플해 덱을 리필했을 때 외부(UI 등)에 알립니다. (리필된 카드 수)
    /// </summary>
    public event System.Action<int> OnDeckReshuffled;

    private void Awake()
    {
        if (turnManager == null) turnManager = FindAnyObjectByType<LDY_TurnManager>();
        if (discardPile == null) discardPile = FindAnyObjectByType<KTH_DiscardCardUI>();
    }

    private void Start()
    {
        InitDeck();

        if (turnManager != null)
            turnManager.OnTurnChanged += HandleTurnChanged;
    }

    private void OnDestroy()
    {
        if (turnManager != null)
            turnManager.OnTurnChanged -= HandleTurnChanged;
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
    /// 턴이 바뀔 때마다 호출됨. 플레이어 턴이 시작되면 드로우 횟수를 리셋한다.
    /// </summary>
    private void HandleTurnChanged(LDY_Team newTurn)
    {
        if (newTurn == LDY_Team.Player)
        {
            drawsUsedThisTurn = 0;
            OnDrawLimitChanged?.Invoke();
            Debug.Log("[KTH_DeckManager] 플레이어 턴 시작 - 드로우 횟수 리셋");
        }
    }

    /// <summary>
    /// 지금 드로우가 가능한 상태인지 (덱 유무는 확인하지 않고, 순수 횟수 제한만 확인)
    /// </summary>
    public bool CanDraw()
    {
        if (ignoreDrawLimit) return true;
        return drawsUsedThisTurn < maxDrawsPerTurn;
    }

    /// <summary>
    /// 덱이 비어있고 버린 카드 더미에 카드가 있으면, 그것들을 셔플해서 덱으로 되돌린다.
    /// 외부(KTH_DiscardCardManager 등)에서 직접 호출할 수 있도록 public으로 열어둔다.
    /// </summary>
    /// <returns>실제로 리셔플이 일어났으면 true</returns>
    public bool ReshuffleFromDiscard()
    {
        if (deck.Count > 0) return false;
        if (discardPile == null || discardPile.Count == 0) return false;

        List<LSO_CardSO> reclaimed = discardPile.ClearAndGetList();
        if (reclaimed.Count == 0) return false;

        ShuffleList(reclaimed);
        deck.AddRange(reclaimed);

        Debug.Log($"[KTH_DeckManager] 버린 카드 더미 {reclaimed.Count}장을 셔플하여 덱으로 되돌렸습니다.");
        OnDeckReshuffled?.Invoke(reclaimed.Count);
        return true;
    }

    /// <summary>
    /// Fisher-Yates 셔플
    /// </summary>
    private static void ShuffleList(List<LSO_CardSO> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>
    /// 덱 최상단 카드를 뽑아 반환합니다. 턴당 드로우 횟수 제한을 확인합니다.
    /// 덱이 비어있으면 버린 카드 더미에서 자동으로 리필을 시도합니다.
    /// </summary>
    public LSO_CardSO DrawCard(bool bypassTurnLimit = false)
    {
        if (!bypassTurnLimit && !CanDraw())
        {
            Debug.LogWarning($"[KTH_DeckManager] 이번 턴 드로우 횟수를 모두 사용했습니다! ({drawsUsedThisTurn}/{maxDrawsPerTurn})");
            return null;
        }

        if (deck.Count == 0 && autoReshuffleFromDiscard)
        {
            ReshuffleFromDiscard();
        }

        if (deck.Count == 0)
        {
            Debug.LogWarning("[KTH_DeckManager] 덱과 버린 카드 더미 모두 비어있습니다. 더 이상 드로우할 수 없습니다.");
            return null;
        }

        LSO_CardSO drawnCard = deck[0];
        deck.RemoveAt(0);

        if (!bypassTurnLimit && !ignoreDrawLimit)
        {
            drawsUsedThisTurn++;
            OnDrawLimitChanged?.Invoke();
        }

        return drawnCard;
    }
}