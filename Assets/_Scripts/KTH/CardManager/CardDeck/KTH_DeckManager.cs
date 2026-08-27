using System;
using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.Reward;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Scripts.KTH.CardManager.CardDeck
{
public class KTH_DeckManager : MonoBehaviour
    {
        [Header("Deck Data")]
        [SerializeField] private List<LSO_CardSO> deck =
            new List<LSO_CardSO>();

        [SerializeField] private LDY_TurnManager turnManager;

        [Header("Reshuffle Settings")]
        [Tooltip("덱이 비었을 때 다시 채워올 버린 카드 더미")]
        [SerializeField] private KTH_DiscardCardUI discardPile;

[SerializeField] private bool autoReshuffleFromDiscard = true;

[Header("Draw Limit Settings")]
[Tooltip("체크하면 턴/횟수 제한 없이 언제든 드로우 가능")]
[SerializeField] private bool ignoreDrawLimit = false;

[Tooltip("턴당 드로우 가능 횟수")]
[SerializeField] private int maxDrawsPerTurn = 2;

private int drawsUsedThisTurn = 0;

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

        // 적 턴 전환 처리를 예약했는지
        private bool _reshuffleCheckScheduled;

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
        }

        public int DrawsUsedThisTurn =>
            _drawsUsedThisTurn;

        public int DrawsRemainingThisTurn =>
            ignoreDrawLimit
                ? int.MaxValue
                : Mathf.Max(
                    0,
                    maxDrawsPerTurn - _drawsUsedThisTurn
                );

        /// <summary>
        /// 드로우 횟수 제한 변경
        /// </summary>
        public event Action OnDrawLimitChanged;

        /// <summary>
        /// 덱이 버린 카드 더미에서 리필되었을 때
        /// int = 리필된 카드 수
        /// </summary>
        public event Action<int> OnDeckReshuffled;

        // ============================================================
        // Unity
        // ============================================================

        private void Start()
        {
            InitDeck();

            if (turnManager != null)
            {
                turnManager.OnTurnChanged += HandleTurnChanged;
            }
            else
            {
                Debug.LogWarning(
                    "[KTH_DeckManager] TurnManager가 연결되지 않았습니다.",
                    this
                );
            }
        }

        private void OnDestroy()
        {
            if (turnManager != null)
            {
                turnManager.OnTurnChanged -= HandleTurnChanged;
            }
        }

        // ============================================================
        // Deck 초기화
        // ============================================================

        private void InitDeck()
        {
            LSO_ItemLibraryManager library =
                LSO_ItemLibraryManager.Instance;

            if (library == null)
            {
                Debug.LogWarning(
                    "[KTH_DeckManager] ItemLibraryManager가 없어 " +
                    "인스펙터의 덱을 그대로 사용합니다."
                );

                ShuffleList(deck);
                return;
            }

            List<LSO_CardSO> owned =
                library.UnlockedPieces;

            if (owned == null || owned.Count == 0)
            {
                Debug.LogWarning(
                    "[KTH_DeckManager] 보유한 카드가 없습니다."
                );

deck.Clear();

foreach (LSO_CardSO card in owned)
{
    if (card == null)
    {
        continue;
    }

    deck.Add(card);
}

                deck.Add(card);
            }

            ShuffleList(deck);

            Debug.Log(
                $"[KTH_DeckManager] 보유 카드 {deck.Count}장을 " +
                $"덱에 로드하고 셔플했습니다."
            );
        }

private void HandleTurnChanged(LDY_Team newTurn)
{
    // 플레이어 턴 시작
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

    // 적 턴 시작: 예약 처리
    if (newTurn == LDY_Team.Enemy)
    {
        if (!autoReshuffleFromDiscard)
        {
            return;
        }

        ScheduleReshuffle();
    }
}

private void ScheduleReshuffle()
{
    if (_reshuffleCheckScheduled)
    {
        return;
    }

    _reshuffleCheckScheduled = true;

    StartCoroutine(
        ReshuffleAtEnemyTurnStart()
    );
}

private IEnumerator ReshuffleAtEnemyTurnStart()
        {
            // --------------------------------------------------------
            // TurnManager가 턴 변경을 처리한 뒤까지 기다림
            // --------------------------------------------------------

            yield return null;

            // 한 프레임 더 기다림.
            // UI / 턴 관련 스크립트가 먼저 처리될 수 있도록 함.
            yield return null;

            _reshuffleCheckScheduled = false;

            // --------------------------------------------------------
            // 여기서 덱 상태 확인
            // --------------------------------------------------------

            if (deck.Count > 0)
            {
                Debug.Log(
                    $"[KTH_DeckManager] 적 턴 시작 - " +
                    $"덱에 {deck.Count}장이 남아 있어 리필하지 않습니다."
                );

                yield break;
            }

            // --------------------------------------------------------
            // 디스카드가 없으면 종료
            // --------------------------------------------------------

            if (discardPile == null)
            {
                Debug.LogWarning(
                    "[KTH_DeckManager] 적 턴 시작 - " +
                    "DiscardPile이 연결되지 않았습니다."
                );

                yield break;
            }

            if (discardPile.Count <= 0)
            {
                Debug.Log(
                    "[KTH_DeckManager] 적 턴 시작 - " +
                    "덱과 버린 카드 더미가 모두 비어 있습니다."
                );

                yield break;
            }

            // --------------------------------------------------------
            // 리필
            // --------------------------------------------------------

            bool reshuffled =
                ReshuffleFromDiscard();

            if (reshuffled)
            {
                Debug.Log(
                    "[KTH_DeckManager] 적 턴 시작 - " +
                    "덱이 비어 있어 버린 카드 더미를 " +
                    "셔플하여 덱으로 되돌렸습니다."
                );
            }
        }

        // ============================================================
        // Draw Limit
        // ============================================================

        public bool CanDraw()
        {
            if (ignoreDrawLimit)
            {
                return true;
            }

            return _drawsUsedThisTurn < maxDrawsPerTurn;
        }

        // ============================================================
        // Draw
        // ============================================================

        public LSO_CardSO DrawCard(
            bool bypassTurnLimit = false)
        {
            // --------------------------------------------------------
            // 드로우 제한
            // --------------------------------------------------------

            if (!bypassTurnLimit && !CanDraw())
            {
                Debug.LogWarning(
                    $"[KTH_DeckManager] 이번 턴 드로우 횟수를 " +
                    $"모두 사용했습니다! " +
                    $"({_drawsUsedThisTurn}/{maxDrawsPerTurn})"
                );

                return null;
            }

            // --------------------------------------------------------
            // 덱이 비어 있음
            //
            // 여기서는 절대 리필하지 않는다.
            // --------------------------------------------------------

            if (deck.Count == 0)
            {
                Debug.LogWarning(
                    "[KTH_DeckManager] 현재 덱이 비어 있습니다. " +
                    "리필은 적 턴 시작 시 처리됩니다."
                );

                return null;
            }

            // --------------------------------------------------------
            // 카드 드로우
            // --------------------------------------------------------

            LSO_CardSO drawnCard =
                deck[0];

            deck.RemoveAt(0);

            // --------------------------------------------------------
            // 드로우 횟수 증가
            // --------------------------------------------------------

            if (!bypassTurnLimit &&
                !ignoreDrawLimit)
            {
                _drawsUsedThisTurn++;

                OnDrawLimitChanged?.Invoke();
            }

            Debug.Log(
{
    // TurnManager가 턴 변경을 처리한 뒤까지 기다림
    yield return null;

    _reshuffleCheckScheduled = false;

    // 아직 덱을 완전히 소진하지 않았다면 리셔플 대상 아님
    if (!deckWasExhausted)
    {
        Debug.Log(
            $"[KTH_DeckManager] 적 턴 시작 - 덱이 아직 소진되지 않았습니다. " +
            $"현재 덱: {deck.Count}장"
        );

        yield break;
    }

    // 안전 체크: 덱에 카드가 있으면 리셔플하지 않음
    if (deck.Count > 0)
    {
        Debug.Log(
            "[KTH_DeckManager] 소진 상태 플래그는 있지만 " +
            $"덱에 {deck.Count}장이 남아 있어 리셔플하지 않습니다."
        );

        yield break;
    }

    // 손패가 남아있다면 리셔플 보류
    if (handLayout != null &&
        handLayout.HandCount > 0)
    {
        Debug.Log(
            $"[KTH_DeckManager] 적 턴 시작 - " +
            $"덱은 소진되었지만 손패에 " +
            $"{handLayout.HandCount}장이 남아있어 " +
            $"리셔플을 보류합니다."
        );

        yield break;
    }

    bool reshuffled = ReshuffleFromDiscard();

    if (reshuffled)
    {
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
            );

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
    // 드로우 횟수 제한
    if (!bypassTurnLimit &&
        !CanDraw())
    {
        return null;
    }

    // 덱이 비어 있음
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

    // 카드 뽑기
    LSO_CardSO drawnCard =
        deck[0];

    deck.RemoveAt(0);

    // 드로우 횟수 증가
    if (!bypassTurnLimit &&
        !ignoreDrawLimit)
    {
        drawsUsedThisTurn++;

        OnDrawLimitChanged?.Invoke();
    }

    // 마지막 카드인지 확인
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
        {
            if (list == null ||
                list.Count <= 1)
            {
                return;
            }

            for (int i = list.Count - 1;
                 i > 0;
                 i--)
            {
                // System.Random과 충돌 방지
                int j =
                    UnityEngine.Random.Range(
                        0,
                        i + 1
                    );

public bool ReshuffleFromDiscard()
{
    // 덱에 카드가 있다면 리필하지 않음
    if (deck.Count > 0)
    {
        return false;
    }

    // Discard 확인
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

    // 버린 카드 가져오기
    List<LSO_CardSO> reclaimed =
        discardPile.ClearAndGetList();

    if (reclaimed == null ||
        reclaimed.Count == 0)
    {
        return false;
    }

    // 셔플
    ShuffleList(reclaimed);

    // 덱에 추가
    deck.AddRange(reclaimed);

    Debug.Log(
        $"[KTH_DeckManager] 버린 카드 더미 " +
        $"{reclaimed.Count}장을 셔플하여 " +
        $"덱으로 되돌렸습니다."
    );

    // 외부 알림
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
        i--)
    {
        int j =
            Random.Range(
                0,
                i + 1
            );

        (list[i], list[j]) = (list[j], list[i]);
    }
}
        }
    }
}