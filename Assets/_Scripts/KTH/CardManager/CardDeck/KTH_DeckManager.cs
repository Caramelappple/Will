using System;
using System.Collections;
using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.Reward;
using UnityEngine;

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

        [Tooltip("적 턴이 시작될 때 덱이 비어 있으면 버린 카드 더미를 덱으로 되돌립니다.")]
        [SerializeField] private bool autoReshuffleFromDiscard = true;

        [Header("Draw Limit Settings")]
        [Tooltip("체크하면 턴/횟수 제한 없이 언제든 드로우 가능")]
        [SerializeField] private bool ignoreDrawLimit;

        [Tooltip("턴당 드로우 가능 횟수")]
        [SerializeField] private int maxDrawsPerTurn = 2;

        private int _drawsUsedThisTurn;

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

            Debug.Log(
                $"[KTH_DeckManager] 보유 카드 {deck.Count}장을 " +
                $"덱에 로드하고 셔플했습니다."
            );
        }

        // ============================================================
        // Turn
        // ============================================================

        private void HandleTurnChanged(LDY_Team newTurn)
        {
            // ========================================================
            // 플레이어 턴 시작
            // ========================================================

            if (newTurn == LDY_Team.Player)
            {
                _drawsUsedThisTurn = 0;

                OnDrawLimitChanged?.Invoke();

                Debug.Log(
                    "[KTH_DeckManager] 플레이어 턴 시작 - " +
                    "드로우 횟수 리셋"
                );

                return;
            }

            // ========================================================
            // 적 턴 시작
            //
            // 중요:
            // TurnManager의 이벤트가 실제 턴 전환보다 먼저 발생할
            // 가능성이 있으므로 즉시 리필하지 않는다.
            //
            // 코루틴으로 한 프레임 뒤에 처리한다.
            // ========================================================

            if (newTurn == LDY_Team.Enemy)
            {
                if (!autoReshuffleFromDiscard)
                {
                    return;
                }

                ScheduleReshuffle();
            }
        }

        // ============================================================
        // 적 턴 시작 후 리필 예약
        // ============================================================

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
                $"[KTH_DeckManager] 카드 드로우: " +
                $"{drawnCard.name} / " +
                $"남은 덱: {deck.Count}"
            );

            return drawnCard;
        }

        // ============================================================
        // Reshuffle
        // ============================================================

        public bool ReshuffleFromDiscard()
        {
            // --------------------------------------------------------
            // 덱에 카드가 남아 있으면 리필하지 않음
            // --------------------------------------------------------

            if (deck.Count > 0)
            {
                return false;
            }

            // --------------------------------------------------------
            // Discard 확인
            // --------------------------------------------------------

            if (discardPile == null)
            {
                Debug.LogWarning(
                    "[KTH_DeckManager] DiscardPile이 연결되지 않았습니다."
                );

                return false;
            }

            if (discardPile.Count <= 0)
            {
                return false;
            }

            // --------------------------------------------------------
            // 버린 카드 가져오기
            // --------------------------------------------------------

            List<LSO_CardSO> reclaimed =
                discardPile.ClearAndGetList();

            if (reclaimed == null ||
                reclaimed.Count == 0)
            {
                return false;
            }

            // --------------------------------------------------------
            // 셔플
            // --------------------------------------------------------

            ShuffleList(reclaimed);

            // --------------------------------------------------------
            // 덱으로 이동
            // --------------------------------------------------------

            deck.AddRange(reclaimed);

            Debug.Log(
                $"[KTH_DeckManager] 버린 카드 더미 " +
                $"{reclaimed.Count}장을 셔플하여 " +
                $"덱으로 되돌렸습니다."
            );

            // --------------------------------------------------------
            // 외부 알림
            // --------------------------------------------------------

            OnDeckReshuffled?.Invoke(
                reclaimed.Count
            );

            return true;
        }

        // ============================================================
        // Shuffle
        // ============================================================

        private static void ShuffleList(
            List<LSO_CardSO> list)
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

                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}