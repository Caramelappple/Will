using System;
using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.Reward;
using UnityEngine;
using Random = UnityEngine.Random;

public class KTH_DeckManager : MonoBehaviour
{
    [Header("Deck Data")]
    [SerializeField] private List<LSO_CardSO> deck =
        new List<LSO_CardSO>();

    [SerializeField] private LDY_TurnManager turnManager;

    [Header("Reshuffle Settings")]
    [Tooltip("덱이 완전히 소진된 후 적 턴이 시작되면 버린 카드 더미를 덱으로 되돌립니다.")]
    [SerializeField] private KTH_DiscardCardUI discardPile;

    [Tooltip("덱이 완전히 소진된 후 적 턴 시작 시 자동 리셔플합니다.")]
    [SerializeField] private bool autoReshuffleFromDiscard = true;

    [Tooltip("손패 참조. 손패에 카드가 남아있으면 덱이 0장이어도 리셔플을 보류합니다.")]
    [SerializeField] private KTH_HandCardLayout handLayout;

    [Header("Draw Limit Settings")]
    [Tooltip("체크하면 턴/횟수 제한 없이 언제든 드로우 가능")]
    [SerializeField] private bool ignoreDrawLimit = false;

    [Tooltip("턴당 드로우 가능 횟수")]
    [SerializeField] private int maxDrawsPerTurn = 2;

    private int drawsUsedThisTurn = 0;

    // =========================================================
    // 중요
    // =========================================================
    //
    // 실제로 덱이 0장이 되었는지를 기록한다.
    //
    // false
    // = 아직 이번 덱을 전부 소진하지 않음
    //
    // true
    // = 덱의 마지막 카드까지 소진됨
    //
    // 이 값이 true인 상태에서 적 턴이 시작될 때만
    // 버린 카드 더미를 덱으로 되돌린다.
    //
    // 단, 손패에 카드가 남아있다면
    // (플레이어가 아직 다 쓰지 않았다면)
    // 리셔플을 보류한다.
    // =========================================================

    private bool deckWasExhausted = false;

    // =========================================================
    // Properties
    // =========================================================

    public IReadOnlyList<LSO_CardSO> Deck =>
        deck;

    public int RemainingCards =>
        deck.Count;

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

    public int DrawsUsedThisTurn =>
        drawsUsedThisTurn;

    public int DrawsRemainingThisTurn =>
        ignoreDrawLimit
            ? int.MaxValue
            : Mathf.Max(
                0,
                maxDrawsPerTurn -
                drawsUsedThisTurn
            );

    public event Action OnDrawLimitChanged;

    public event Action<int> OnDeckReshuffled;


    // =========================================================
    // Unity
    // =========================================================

    private void Start()
    {
        InitDeck();

        if (handLayout == null)
        {
            handLayout =
                KTH_HandCardLayout.Instance;

            if (handLayout == null)
            {
                handLayout =
                    FindAnyObjectByType<KTH_HandCardLayout>();
            }

            if (handLayout == null)
            {
                Debug.LogWarning(
                    "[KTH_DeckManager] HandLayout이 연결되지 않았습니다. " +
                    "손패 카드가 남아있어도 리셔플이 보류되지 않을 수 있습니다.",
                    this
                );
            }
        }

        if (turnManager != null)
        {
            turnManager.OnTurnChanged +=
                HandleTurnChanged;
        }
        else
        {
            Debug.LogWarning(
                "[KTH_DeckManager] TurnManager가 연결되지 않았습니다. " +
                "적 턴 시작 시 자동 리필이 작동하지 않습니다.",
                this
            );
        }
    }

    private void OnDestroy()
    {
        if (turnManager != null)
        {
            turnManager.OnTurnChanged -=
                HandleTurnChanged;
        }
    }


    // =========================================================
    // Deck 초기화
    // =========================================================

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
    // Turn
    // =========================================================

    private void HandleTurnChanged(
        LDY_Team newTurn)
    {
        // =====================================================
        // 플레이어 턴 시작
        // =====================================================

        if (newTurn == LDY_Team.Player)
        {
            drawsUsedThisTurn = 0;

            OnDrawLimitChanged?.Invoke();

            Debug.Log(
                "[KTH_DeckManager] 플레이어 턴 시작 - " +
                "드로우 횟수 리셋"
            );

            return;
        }


        // =====================================================
        // 적 턴 시작
        // =====================================================
        //
        // 여기서 중요한 것은
        //
        // deck.Count == 0
        //
        // 만 보는 것이 아니라
        //
        // deckWasExhausted == true
        //
        // 인지를 확인하는 것.
        //
        // 즉, 실제로 덱의 마지막 카드를 뽑아서
        // 덱이 소진된 경우에만 리셔플한다.
        //
        // 추가로, 손패에 카드가 남아있다면
        // (플레이어가 아직 손패를 다 쓰지 않았다면)
        // 리셔플을 보류한다.
        // =====================================================

        if (newTurn != LDY_Team.Enemy)
        {
            return;
        }

        if (!autoReshuffleFromDiscard)
        {
            return;
        }

        // 아직 덱을 완전히 소진하지 않았다.
        if (!deckWasExhausted)
        {
            Debug.Log(
                $"[KTH_DeckManager] 적 턴 시작 - " +
                $"덱이 아직 소진되지 않았습니다. " +
                $"현재 덱: {deck.Count}장"
            );

            return;
        }

        // 안전 체크
        if (deck.Count > 0)
        {
            Debug.Log(
                "[KTH_DeckManager] 소진 상태 플래그는 있지만 " +
                $"덱에 {deck.Count}장이 남아 있어 리셔플하지 않습니다."
            );

            return;
        }

        // =====================================================
        // ★ 손패 체크
        //
        // 덱은 0장이지만, 손패에 아직 카드가 남아있다면
        // 플레이어가 그 카드를 다 쓸 때까지
        // 버린 카드 더미를 덱으로 되돌리지 않는다.
        // =====================================================

        if (handLayout != null &&
            handLayout.HandCount > 0)
        {
            Debug.Log(
                $"[KTH_DeckManager] 적 턴 시작 - " +
                $"덱은 소진되었지만 손패에 " +
                $"{handLayout.HandCount}장이 남아있어 " +
                $"리셔플을 보류합니다."
            );

            return;
        }

        // =====================================================
        // 버린 카드 → 덱
        // =====================================================

        bool reshuffled =
            ReshuffleFromDiscard();

        if (reshuffled)
        {
            // =================================================
            // 중요
            //
            // 한 번 리셔플했으므로 소진 상태를 해제한다.
            //
            // 다음 적 턴에는 다시 리셔플하지 않는다.
            //
            // 다시 덱이 0장이 될 때까지 기다린다.
            // =================================================

            deckWasExhausted = false;

            Debug.Log(
                "[KTH_DeckManager] 적 턴 시작 - " +
                "덱이 완전히 소진된 상태였으므로 " +
                "버린 카드 더미를 덱으로 되돌렸습니다."
            );
        }
        else
        {
            Debug.Log(
                "[KTH_DeckManager] 적 턴 시작 - " +
                "덱은 소진되었지만 리셔플할 버린 카드가 없습니다."
            );
        }
    }


    // =========================================================
    // Draw Limit
    // =========================================================

    public bool CanDraw()
    {
        if (ignoreDrawLimit)
        {
            return true;
        }

        return drawsUsedThisTurn <
               maxDrawsPerTurn;
    }


    // =========================================================
    // Draw
    // =========================================================

    public LSO_CardSO DrawCard(
        bool bypassTurnLimit = false)
    {
        // =====================================================
        // 드로우 횟수 제한
        // =====================================================

        if (!bypassTurnLimit &&
            !CanDraw())
        {
            Debug.LogWarning(
                $"[KTH_DeckManager] 이번 턴 드로우 횟수를 " +
                $"모두 사용했습니다! " +
                $"({drawsUsedThisTurn}/{maxDrawsPerTurn})"
            );

            return null;
        }


        // =====================================================
        // 덱이 비어 있음
        // =====================================================

        if (deck.Count == 0)
        {
            // 이미 소진 상태임을 확실하게 기록
            deckWasExhausted = true;

            Debug.LogWarning(
                "[KTH_DeckManager] 현재 덱이 비어 있습니다. " +
                "적 턴 시작 시 버린 카드 더미를 확인합니다."
            );

            return null;
        }


        // =====================================================
        // 카드 뽑기
        // =====================================================

        LSO_CardSO drawnCard =
            deck[0];

        deck.RemoveAt(0);


        // =====================================================
        // 드로우 횟수 증가
        // =====================================================

        if (!bypassTurnLimit &&
            !ignoreDrawLimit)
        {
            drawsUsedThisTurn++;

            OnDrawLimitChanged?.Invoke();
        }


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
