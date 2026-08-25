using System;
using System.Collections.Generic;
using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.UI;
using DG.Tweening;
using UnityEngine;

public class KTH_HandCardLayout : MonoBehaviour
{
    public static KTH_HandCardLayout Instance { get; private set; }

    [Header("References")]
    [SerializeField] private LSO_WillPanel willPanel;

    [Header("Arc Layout Settings")]
    [SerializeField] private float maxCardSpacing = 180f;
    [SerializeField] private float minCardSpacing = 55f;
    [SerializeField] private float maxHandWidth = 850f;
    [SerializeField] private float arcHeight = 35f;
    [SerializeField] private float maxRotation = 14f;

    [Header("Organic Motion Settings")]
    [SerializeField] private float staggerDelay = 0.025f;
    [SerializeField] private Ease moveEase = Ease.OutCubic;

    [Header("Hand Settings")]
    [SerializeField] private int maxHandSize = 10;

    [Header("Draw Animation Settings")]
    [SerializeField] private float drawDuration = 0.45f;

    [Header("Selection Push Settings")]
    [Tooltip("선택된 카드 바로 옆 카드가 밀리는 거리")]
    [SerializeField] private float pushAmount = 60f;

    [Tooltip("밀림/복귀 애니메이션 시간")]
    [SerializeField] private float pushDuration = 0.28f;

    [Tooltip("선택 카드에서 멀어질수록 적용되는 밀림 비율")]
    [SerializeField] private float farCardPushMultiplier = 0.5f;

    [Header("Placement Mode Settings")]
    [SerializeField] private bool enableMoveDown = true;
    [SerializeField] private float placementMoveDownDistance = 150f;
    [SerializeField] private float placementMoveDuration = 0.3f;

    private readonly List<KTH_HandCard> handCards =
        new List<KTH_HandCard>();

    private Vector3 originalContainerLocalPos;
    private bool isCurrentlyDown;

    private KTH_HandCard selectedCard;

    // 배치모드 진입 시퀀스가 중복 실행되는 것을 막기 위한 토큰
    private int placementSequenceId;

    public int HandCount => handCards.Count;

    public int MaxHandSize
    {
        get => maxHandSize;
        set => maxHandSize = value;
    }

    public bool IsFull =>
        maxHandSize > 0 &&
        handCards.Count >= maxHandSize;

    public event Action<int, int> OnHandCountChanged;

    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        Instance = this;

        originalContainerLocalPos =
            transform.localPosition;

        isCurrentlyDown = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // =========================================================
    // Card Setup
    // =========================================================

    /// <summary>
    /// HandCard가 프리팹이어도 Find 없이
    /// WillPanel 참조를 주입해서 초기화한다.
    /// </summary>
    public void SetupCard(
        KTH_HandCard card,
        LSO_CardSO cardData)
    {
        if (card == null)
        {
            return;
        }

        card.Setup(
            cardData,
            willPanel
        );
    }

    // =========================================================
    // Hand Check
    // =========================================================

    public bool CanAddCard()
    {
        return !IsFull;
    }

    // =========================================================
    // Add Card
    // =========================================================

    public bool AddCard(KTH_HandCard card)
    {
        return AddCard(card, insertAtFront: false);
    }

    public bool AddCard(
        KTH_HandCard card,
        Vector3 spawnerWorldPos)
    {
        bool spawnerIsOnLeft =
            spawnerWorldPos.x < 0f;

        bool insertAtFront =
            !spawnerIsOnLeft;

        return AddCard(
            card,
            insertAtFront
        );
    }

    public bool AddCard(
        KTH_HandCard card,
        bool insertAtFront)
    {
        if (card == null)
        {
            return false;
        }

        if (handCards.Contains(card))
        {
            return false;
        }

        if (IsFull)
        {
            Debug.LogWarning(
                $"[KTH_HandCardLayout] 손패가 가득 찼습니다! " +
                $"({handCards.Count}/{maxHandSize})"
            );

            return false;
        }

        KTH_HandCard.DeselectCurrent();

        if (insertAtFront)
        {
            handCards.Insert(0, card);
        }
        else
        {
            handCards.Add(card);
        }

        UpdateHandLayout(card);

        OnHandCountChanged?.Invoke(
            handCards.Count,
            maxHandSize
        );

        return true;
    }

    // =========================================================
    // Remove Card
    // =========================================================

    public void RemoveCard(KTH_HandCard card)
    {
        if (selectedCard == card)
        {
            selectedCard = null;
        }

        // 배치모드 시퀀스가 진행 중이었다면 무효화
        placementSequenceId++;

        if (!handCards.Remove(card))
        {
            return;
        }

        UpdateHandLayout(
            null,
            pushDuration,
            false
        );

        OnHandCountChanged?.Invoke(
            handCards.Count,
            maxHandSize
        );
    }

    // =========================================================
    // Selection Changed
    // =========================================================

    public void OnCardSelectionChanged(
        KTH_HandCard card,
        bool selected)
    {
        if (selected)
        {
            if (selectedCard != null &&
                selectedCard != card)
            {
                KTH_HandCard previous =
                    selectedCard;

                previous.SetSelected(false);
            }

            selectedCard = card;
        }
        else if (selectedCard == card)
        {
            selectedCard = null;

            // 선택 해제 시 진행 중이던 배치 시퀀스 무효화
            placementSequenceId++;
        }

        UpdateHandLayout(
            null,
            pushDuration,
            false
        );
    }

    // =========================================================
    // Placement Mode
    // =========================================================

    public void EnterPlacementMode(
        KTH_HandCard card)
    {
        if (card == null)
        {
            return;
        }

        if (!handCards.Contains(card))
        {
            return;
        }

        selectedCard = card;

        UpdateHandLayoutForPlacement(
            placementMoveDuration
        );
    }

    public void ExitPlacementMode()
    {
        // 진행 중이던 배치 시퀀스 무효화
        placementSequenceId++;

        UpdateHandLayout(
            null,
            placementMoveDuration,
            false
        );
    }

    // =========================================================
    // Normal Layout
    // =========================================================

    public void UpdateHandLayout(
        KTH_HandCard newlyDrawnCard = null,
        float duration = 0.35f,
        bool useStagger = true)
    {
        int count = handCards.Count;

        if (count == 0)
        {
            return;
        }

        int selectedIndex =
            selectedCard != null
                ? handCards.IndexOf(selectedCard)
                : -1;

        for (int i = 0; i < count; i++)
        {
            KTH_HandCard card =
                handCards[i];

            var transformData =
                CardLayoutCalculator
                    .CalculateCardTransform(
                        i,
                        count,
                        maxCardSpacing,
                        minCardSpacing,
                        maxHandWidth,
                        arcHeight,
                        maxRotation
                    );

            Vector3 basePos =
                transformData.LocalPosition;

            Vector3 targetPos =
                basePos;

            if (selectedIndex >= 0 &&
                i != selectedIndex)
            {
                float push =
                    CalculatePushAmount(
                        i,
                        selectedIndex
                    );

                if (i < selectedIndex)
                {
                    targetPos.x += push;
                }
                else
                {
                    targetPos.x -= push;
                }
            }

            if (!card.IsSelected)
            {
                card.transform.SetSiblingIndex(i);
            }

            if (card == newlyDrawnCard)
            {
                card.PlayDrawAnimation(
                    targetPos,
                    transformData.ZRotation,
                    drawDuration
                );

                card.UpdateOriginalTransform(
                    basePos,
                    transformData.ZRotation
                );

                continue;
            }

            if (!card.IsSelected)
            {
                float delay =
                    useStagger
                        ? i * staggerDelay
                        : 0f;

                card.MoveToHandPositionWithDelay(
                    targetPos,
                    transformData.ZRotation,
                    duration,
                    delay,
                    moveEase
                );
            }

            card.UpdateOriginalTransform(
                basePos,
                transformData.ZRotation
            );
        }
    }

    // =========================================================
    // Placement Layout
    // 1단계: 선택된 카드만 먼저 중앙으로 이동
    // 2단계: 중앙 이동이 끝난 후 나머지 카드를 밀어냄
    // =========================================================

    private void UpdateHandLayoutForPlacement(float duration)
    {
        if (selectedCard == null)
        {
            return;
        }

        int count = handCards.Count;

        if (count == 0)
        {
            return;
        }

        int selectedIndex = handCards.IndexOf(selectedCard);

        if (selectedIndex < 0)
        {
            return;
        }

        // =====================================================
        // 선택 카드 중앙 이동
        // =====================================================

        selectedCard.transform.DOKill();
        selectedCard.transform.SetAsLastSibling();

        Sequence centerSequence = DOTween.Sequence();
        centerSequence.SetTarget(selectedCard.transform);

        centerSequence.Join(
            selectedCard.transform
                .DOLocalMove(
                    Vector3.zero,
                    duration
                )
                .SetEase(Ease.OutBack)
        );

        centerSequence.Join(
            selectedCard.transform
                .DOLocalRotate(
                    Vector3.zero,
                    duration
                )
                .SetEase(Ease.OutBack)
        );

        centerSequence.Join(
            selectedCard.transform
                .DOScale(
                    Vector3.one * selectedCard.SelectScale,
                    duration
                )
                .SetEase(Ease.OutBack)
        );

        // =====================================================
        // 선택 카드가 중앙으로 이동하는 동안
        // 주변 카드는 기존 위치에 그대로 둔다.
        // =====================================================

        // 중앙 이동이 끝난 후 주변 카드를 밀어낸다.
        centerSequence.OnComplete(() =>
        {
            if (selectedCard == null)
            {
                return;
            }

            if (!selectedCard.IsSelected)
            {
                return;
            }

            PushSideCardsForPlacement(
                selectedIndex,
                count
            );
        });
    }

    // =========================================================
    // Push Side Cards
    // (선택 카드의 중앙 이동이 끝난 뒤 호출됨)
    // =========================================================

    private void PushSideCardsForPlacement(
        int selectedIndex,
        int count)
    {
        float cardSpacing =
            CalculatePlacementSpacing(count);

        float centerIndex =
            (count - 1) * 0.5f;

        for (int i = 0; i < count; i++)
        {
            if (i == selectedIndex)
            {
                continue;
            }

            KTH_HandCard card =
                handCards[i];

            int relativeIndex =
                i - selectedIndex;

            float targetX =
                -relativeIndex *
                cardSpacing;

            float normalizedDistance =
                count > 1
                    ? Mathf.Clamp01(
                        Mathf.Abs(
                            (float)relativeIndex
                        ) /
                        Mathf.Max(
                            1f,
                            centerIndex
                        )
                    )
                    : 0f;

            float targetY =
                -normalizedDistance *
                normalizedDistance *
                arcHeight;

            float targetRotation =
                relativeIndex *
                (maxRotation /
                 Mathf.Max(
                     1f,
                     centerIndex
                 ));

            float push =
                CalculatePushAmount(
                    i,
                    selectedIndex
                );

            if (i < selectedIndex)
            {
                targetX += push;
            }
            else
            {
                targetX -= push;
            }

            Vector3 targetPos =
                new Vector3(
                    targetX,
                    targetY,
                    0f
                );

            card.transform.SetSiblingIndex(i);

            card.MoveToHandPositionWithDelay(
                targetPos,
                targetRotation,
                pushDuration,
                0f,
                moveEase
            );

            card.UpdateOriginalTransform(
                targetPos,
                targetRotation
            );
        }
    }

    // =========================================================
    // Placement Spacing
    // =========================================================

    private float CalculatePlacementSpacing(
        int count)
    {
        if (count <= 1)
        {
            return maxCardSpacing;
        }

        float spacing =
            Mathf.Min(
                maxCardSpacing,
                maxHandWidth /
                (count - 1)
            );

        return Mathf.Max(
            minCardSpacing,
            spacing
        );
    }

    // =========================================================
    // Calculate Push
    // =========================================================

    private float CalculatePushAmount(
        int cardIndex,
        int selectedIndex)
    {
        if (selectedIndex < 0 ||
            cardIndex == selectedIndex)
        {
            return 0f;
        }

        int distance =
            Mathf.Abs(
                cardIndex -
                selectedIndex
            );

        if (distance == 1)
        {
            return pushAmount;
        }

        float multiplier =
            Mathf.Pow(
                farCardPushMultiplier,
                distance - 1
            );

        return pushAmount *
               multiplier;
    }

    // =========================================================
    // Move Container Down
    // =========================================================

    public void MoveDownForPlacement()
    {
        if (!enableMoveDown ||
            isCurrentlyDown)
        {
            return;
        }

        isCurrentlyDown = true;

        AnimateContainerY(
            originalContainerLocalPos.y -
            placementMoveDownDistance
        );
    }

    public void MoveUpFromPlacement()
    {
        if (!isCurrentlyDown)
        {
            return;
        }

        isCurrentlyDown = false;

        AnimateContainerY(
            originalContainerLocalPos.y
        );
    }

    // =========================================================
    // Container Animation
    // =========================================================

    private void AnimateContainerY(
        float targetY)
    {
        transform.DOKill();

        transform
            .DOLocalMoveY(
                targetY,
                placementMoveDuration
            )
            .SetEase(Ease.OutCubic);
    }
}