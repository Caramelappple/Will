using System;
using System.Collections.Generic;
using _Scripts.LSO.Deck.Data;
using DG.Tweening;
using UnityEngine;
using _Scripts.LSO.UI.Panel;

public class KTH_HandCardLayout : MonoBehaviour
{
    public static KTH_HandCardLayout Instance { get; private set; }

    [Header("References")]
    [SerializeField] private LSO_WillPanel willPanel;

    [Header("Arc Layout Settings")]
    [SerializeField] private float maxCardSpacing = 200f;
    [SerializeField] private float minCardSpacing = 60f;
    [SerializeField] private float maxHandWidth = 800f;
    [SerializeField] private float arcHeight = 40f;
    [SerializeField] private float maxRotation = 15f;

    [Header("Organic Motion Settings")]
    [SerializeField] private float staggerDelay = 0.025f;
    [SerializeField] private Ease moveEase = Ease.OutCubic;

    [Header("Hand Settings")]
    [SerializeField] private int maxHandSize = 8;

    [Header("Draw Animation Settings")]
    [SerializeField] private float drawDuration = 0.4f;

    [Header("Selection Push Settings")]
    [SerializeField] private float pushAmount = 60f;
    [SerializeField] private float pushDuration = 0.28f;
    [SerializeField] private float farCardPushMultiplier = 0.5f;

    [Header("Placement Mode Settings")]
    [SerializeField] private bool enableMoveDown;
    [SerializeField] private float placementMoveDownDistance = 150f;
    [SerializeField] private float placementMoveDuration = 0.3f;
    [SerializeField] private float placementCenterGap = 140f;

    [Header("Two Card Placement Settings")]
    [SerializeField] private float twoCardPlacementDistance = 400f;

    private readonly List<KTH_HandCard> handCards =
        new List<KTH_HandCard>();

    private Vector3 originalContainerLocalPos;
    private bool isCurrentlyDown;
    private KTH_HandCard selectedCard;

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

    private void Awake()
    {
        Instance = this;

        originalContainerLocalPos =
            transform.localPosition;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

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

    public bool CanAddCard()
    {
        return !IsFull;
    }

    public bool AddCard(KTH_HandCard card)
    {
        return AddCard(
            card,
            false
        );
    }

    public bool AddCard(
        KTH_HandCard card,
        Vector3 spawnerWorldPos)
    {
        bool insertAtFront =
            spawnerWorldPos.x >= 0f;

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
            return false;
        }

        KTH_HandCard.DeselectCurrent();

        if (insertAtFront)
        {
            handCards.Insert(
                0,
                card
            );
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

    public void RemoveCard(
        KTH_HandCard card)
    {
        if (selectedCard == card)
        {
            selectedCard = null;
        }

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

    public void OnCardSelectionChanged(
        KTH_HandCard card,
        bool selected)
    {
        if (selected)
        {
            if (selectedCard != null &&
                selectedCard != card)
            {
                selectedCard.SetSelected(false);
            }

            selectedCard = card;
            return;
        }

        if (selectedCard == card)
        {
            selectedCard = null;

            UpdateHandLayout(
                null,
                pushDuration,
                false
            );
        }
    }

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

        MoveSelectedCardToCenter();
    }

    public void ExitPlacementMode()
    {
        selectedCard = null;

        UpdateHandLayout(
            null,
            placementMoveDuration,
            false
        );
    }

    private void MoveSelectedCardToCenter()
    {
        if (selectedCard == null)
        {
            return;
        }

        selectedCard.transform.DOKill();
        selectedCard.transform.SetAsLastSibling();

        Sequence moveToCenterSequence =
            DOTween.Sequence();

        moveToCenterSequence.SetTarget(
            selectedCard.transform
        );

        moveToCenterSequence.Join(
            selectedCard.transform
                .DOLocalMove(
                    Vector3.zero,
                    placementMoveDuration
                )
                .SetEase(Ease.OutBack)
        );

        moveToCenterSequence.Join(
            selectedCard.transform
                .DOLocalRotate(
                    Vector3.zero,
                    placementMoveDuration
                )
                .SetEase(Ease.OutBack)
        );

        moveToCenterSequence.Join(
            selectedCard.transform
                .DOScale(
                    Vector3.one *
                    selectedCard.SelectScale,
                    placementMoveDuration
                )
                .SetEase(Ease.OutBack)
        );

        moveToCenterSequence.OnComplete(() =>
        {
            if (selectedCard == null)
            {
                return;
            }

            if (!selectedCard.IsPlacementMode)
            {
                return;
            }

            SpreadCardsAroundCenter();
        });
    }

    private void SpreadCardsAroundCenter()
{
    if (selectedCard == null)
    {
        return;
    }

    int count = handCards.Count;

    if (count <= 1)
    {
        return;
    }

    // =========================================================
    // 카드가 정확히 2장일 때
    // 선택 카드 = 중앙
    // 나머지 카드 = 바깥쪽으로 떨어지고 아래로 내려가며 기울어짐
    // =========================================================
    if (count == 2)
    {
        int selectedIndex =
            handCards.IndexOf(selectedCard);

        if (selectedIndex < 0)
        {
            return;
        }

        KTH_HandCard otherCard =
            handCards[
                selectedIndex == 0
                    ? 1
                    : 0
            ];

        if (otherCard == null)
        {
            return;
        }

        // 선택된 카드가 왼쪽이었다면
        // 다른 카드는 오른쪽에 배치
        bool otherIsRight =
            selectedIndex == 0;

        // 5장일 때 바깥쪽 카드처럼 충분히 떨어뜨림
        float otherCardX =
            otherIsRight
                ? maxCardSpacing * 1.25f
                : -maxCardSpacing * 1.25f;

        // 바깥쪽 카드처럼 아래로 내림
        float otherCardY =
            -arcHeight;

        // 부채꼴 레이아웃처럼 기울임
        float otherCardRotation =
            otherIsRight
                ? -maxRotation
                : maxRotation;

        otherCard.transform.DOKill();

        Sequence otherCardSequence =
            DOTween.Sequence();

        otherCardSequence.SetTarget(
            otherCard.transform
        );

        otherCardSequence.Join(
            otherCard.transform
                .DOLocalMove(
                    new Vector3(
                        otherCardX,
                        otherCardY,
                        0f
                    ),
                    placementMoveDuration
                )
                .SetEase(moveEase)
        );

        otherCardSequence.Join(
            otherCard.transform
                .DOLocalRotate(
                    new Vector3(
                        0f,
                        0f,
                        otherCardRotation
                    ),
                    placementMoveDuration
                )
                .SetEase(moveEase)
        );

        otherCardSequence.Join(
            otherCard.transform
                .DOScale(
                    Vector3.one,
                    placementMoveDuration
                )
                .SetEase(moveEase)
        );

        // =====================================================
        // 선택된 카드는 항상 중앙
        // =====================================================

        selectedCard.transform.DOKill();

        Sequence twoCardSelectedSequence =
            DOTween.Sequence();

        twoCardSelectedSequence.SetTarget(
            selectedCard.transform
        );

        twoCardSelectedSequence.Join(
            selectedCard.transform
                .DOLocalMove(
                    Vector3.zero,
                    placementMoveDuration
                )
                .SetEase(Ease.OutBack)
        );

        twoCardSelectedSequence.Join(
            selectedCard.transform
                .DOLocalRotate(
                    Vector3.zero,
                    placementMoveDuration
                )
                .SetEase(Ease.OutBack)
        );

        twoCardSelectedSequence.Join(
            selectedCard.transform
                .DOScale(
                    Vector3.one *
                    selectedCard.SelectScale,
                    placementMoveDuration
                )
                .SetEase(Ease.OutBack)
        );

        selectedCard.transform.SetAsLastSibling();

        return;
    }

    // =========================================================
    // 카드가 3장 이상일 때
    // 기존 부채꼴 레이아웃
    // =========================================================

    List<KTH_HandCard> otherCards =
        new List<KTH_HandCard>();

    for (int i = 0; i < count; i++)
    {
        KTH_HandCard card =
            handCards[i];

        if (card == null ||
            card == selectedCard)
        {
            continue;
        }

        otherCards.Add(card);
    }

    int otherCount =
        otherCards.Count;

    int leftCount =
        otherCount / 2;

    int rightCount =
        otherCount - leftCount;

    float spacing =
        CalculatePlacementSpacing(
            otherCount + 1
        );

    for (int i = 0; i < otherCount; i++)
    {
        KTH_HandCard card =
            otherCards[i];

        int relativeIndex;

        if (i < leftCount)
        {
            relativeIndex =
                i - leftCount;
        }
        else
        {
            relativeIndex =
                i - leftCount + 1;
        }

        float targetX =
            relativeIndex * spacing;

        if (relativeIndex < 0)
        {
            targetX -= placementCenterGap;
        }
        else
        {
            targetX += placementCenterGap;
        }

        float normalized;

        if (relativeIndex < 0)
        {
            normalized =
                Mathf.Clamp01(
                    Mathf.Abs(relativeIndex) /
                    (float)Mathf.Max(
                        1,
                        leftCount
                    )
                );
        }
        else
        {
            normalized =
                Mathf.Clamp01(
                    relativeIndex /
                    (float)Mathf.Max(
                        1,
                        rightCount
                    )
                );
        }

        float targetY =
            -normalized *
            normalized *
            arcHeight;

        float targetRotation;

        if (relativeIndex < 0)
        {
            targetRotation =
                normalized *
                maxRotation;
        }
        else
        {
            targetRotation =
                -normalized *
                maxRotation;
        }

        Vector3 targetPosition =
            new Vector3(
                targetX,
                targetY,
                0f
            );

        card.transform.DOKill();

        Sequence cardSequence =
            DOTween.Sequence();

        cardSequence.SetTarget(
            card.transform
        );

        cardSequence.Join(
            card.transform
                .DOLocalMove(
                    targetPosition,
                    placementMoveDuration
                )
                .SetEase(moveEase)
        );

        cardSequence.Join(
            card.transform
                .DOLocalRotate(
                    new Vector3(
                        0f,
                        0f,
                        targetRotation
                    ),
                    placementMoveDuration
                )
                .SetEase(moveEase)
        );

        cardSequence.Join(
            card.transform
                .DOScale(
                    Vector3.one,
                    placementMoveDuration
                )
                .SetEase(moveEase)
        );
    }

    // =========================================================
    // 3장 이상일 때 선택 카드 중앙 처리
    // =========================================================

    selectedCard.transform.DOKill();

    Sequence selectedSequence =
        DOTween.Sequence();

    selectedSequence.SetTarget(
        selectedCard.transform
    );

    selectedSequence.Join(
        selectedCard.transform
            .DOLocalMove(
                Vector3.zero,
                placementMoveDuration
            )
            .SetEase(Ease.OutBack)
    );

    selectedSequence.Join(
        selectedCard.transform
            .DOLocalRotate(
                Vector3.zero,
                placementMoveDuration
            )
            .SetEase(Ease.OutBack)
    );

    selectedSequence.Join(
        selectedCard.transform
            .DOScale(
                Vector3.one *
                selectedCard.SelectScale,
                placementMoveDuration
            )
            .SetEase(Ease.OutBack)
    );

    selectedCard.transform.SetAsLastSibling();
}

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

    public void UpdateHandLayout(
        KTH_HandCard newlyDrawnCard = null,
        float duration = 0.35f,
        bool useStagger = true)
    {
        int count =
            handCards.Count;

        if (count == 0)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            KTH_HandCard card =
                handCards[i];

            if (card == null)
            {
                continue;
            }

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

            Vector3 targetPosition =
                transformData.LocalPosition;

            if (card == newlyDrawnCard)
            {
                card.PlayDrawAnimation(
                    targetPosition,
                    transformData.ZRotation,
                    drawDuration
                );

                card.UpdateOriginalTransform(
                    targetPosition,
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
                    targetPosition,
                    transformData.ZRotation,
                    duration,
                    delay,
                    moveEase
                );
            }

            card.UpdateOriginalTransform(
                targetPosition,
                transformData.ZRotation
            );
        }

        if (selectedCard != null &&
            selectedCard.IsSelected)
        {
            selectedCard.transform.SetAsLastSibling();
        }
    }

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

    public void GatherCardsToCenter(
        float duration)
    {
        if (handCards.Count == 0)
        {
            return;
        }

        for (int i = 0; i < handCards.Count; i++)
        {
            KTH_HandCard card =
                handCards[i];

            if (card == null)
            {
                continue;
            }

            card.transform.DOKill();

            Sequence gatherSequence =
                DOTween.Sequence();

            gatherSequence.Join(
                card.transform
                    .DOLocalMove(
                        Vector3.zero,
                        duration
                    )
                    .SetEase(Ease.InBack)
            );

            gatherSequence.Join(
                card.transform
                    .DOLocalRotate(
                        Vector3.zero,
                        duration
                    )
                    .SetEase(Ease.InBack)
            );

            gatherSequence.Join(
                card.transform
                    .DOScale(
                        Vector3.one,
                        duration
                    )
                    .SetEase(Ease.InBack)
            );
        }
    }

    public void RestoreCardsFromCenter(
        float duration)
    {
        UpdateHandLayout(
            null,
            duration,
            false
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