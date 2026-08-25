using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class KTH_HandCardLayout : MonoBehaviour
{
    public static KTH_HandCardLayout Instance { get; private set; }

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

    /// <summary>
    /// 스포너의 월드 X좌표 "부호"를 기준으로 카드를 삽입할 방향을 자동으로 결정한다.
    /// X가 음수(-)이면 왼쪽부터, 양수(+)이면 오른쪽부터 채워진다.
    /// (CardLayoutCalculator는 reversedIndex를 쓰므로 리스트 앞쪽=화면 오른쪽,
    ///  리스트 뒤쪽=화면 왼쪽에 대응한다)
    /// </summary>
    public bool AddCard(KTH_HandCard card, Vector3 spawnerWorldPos)
    {
        bool spawnerIsOnLeft =
            spawnerWorldPos.x < 0f;

        // 왼쪽부터 채우려면 새 카드가 화면 왼쪽(=리스트 뒤쪽)에 와야 하므로
        // insertAtFront = false (뒤에 추가).
        // 오른쪽부터 채우려면 새 카드가 화면 오른쪽(=리스트 앞쪽)에 와야 하므로
        // insertAtFront = true (앞에 추가).
        bool insertAtFront = !spawnerIsOnLeft;

        return AddCard(card, insertAtFront);
    }

    public bool AddCard(KTH_HandCard card, bool insertAtFront)
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
            // 동시에 하나만 선택되도록 보장한다.
            // 이전에 선택돼 있던 다른 카드가 있다면 강제로 해제시킨다.
            // (이 시점에서 selectedCard가 null이 아니고 card와 다르면
            //  아직 SetSelected(false)가 전파되지 않은 상태이므로 여기서 정리한다)
            if (selectedCard != null && selectedCard != card)
            {
                KTH_HandCard previous = selectedCard;

                // 재귀적으로 다시 OnCardSelectionChanged가 불려도
                // 아래에서 selectedCard를 곧바로 덮어쓸 것이므로 안전하다.
                previous.SetSelected(false);
            }

            selectedCard = card;
        }
        else if (selectedCard == card)
        {
            selectedCard = null;
        }

        UpdateHandLayout(
            null,
            pushDuration,
            false
        );
    }

    // =========================================================
    // Calculate Push
    // =========================================================

    private float CalculatePushAmount(
        int cardIndex,
        int selectedIndex)
    {
        if (selectedIndex < 0)
        {
            return 0f;
        }

        if (cardIndex == selectedIndex)
        {
            return 0f;
        }

        int distance =
            Mathf.Abs(
                cardIndex - selectedIndex
            );

        // 선택 카드 바로 옆
        if (distance == 1)
        {
            return pushAmount;
        }

        // 멀어질수록 점점 적게 밀림
        float multiplier =
            Mathf.Pow(
                farCardPushMultiplier,
                distance - 1
            );

        return pushAmount * multiplier;
    }

    // =========================================================
    // Update Layout
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
            var transformData =
                CardLayoutCalculator.CalculateCardTransform(
                    i,
                    count,
                    maxCardSpacing,
                    minCardSpacing,
                    maxHandWidth,
                    arcHeight,
                    maxRotation
                );

            KTH_HandCard card =
                handCards[i];

            // 카드 순서는 Layout에서만 관리
            card.transform.SetSiblingIndex(i);

            // 밀기 적용 "전"의 순수 부채꼴 기준 위치.
            // 이 값을 카드의 originalLocalPos로 저장해야
            // 선택 해제 시 정확한 자리로 복귀하고, 다음 레이아웃 갱신 때도
            // 흔들림 없이 같은 기준으로 재계산된다.
            Vector3 basePos = transformData.LocalPosition;

            // 실제로 이동할 위치 (밀기 적용 후)
            Vector3 targetPos = basePos;

            // =================================================
            // 선택 카드 주변 밀기
            // =================================================

            if (selectedIndex >= 0 &&
                i != selectedIndex)
            {
                float push =
                    CalculatePushAmount(
                        i,
                        selectedIndex
                    );

                // 주의: CardLayoutCalculator는 reversedIndex를 사용하므로
                // 리스트 인덱스가 작을수록 화면상으로는 "오른쪽"에 위치한다.
                // 따라서 인덱스 대소 비교와 화면 좌우 이동 방향이 반대가 되어야
                // 선택 카드 주변이 선택 카드로부터 "멀어지는" 방향으로 밀린다.
                if (i < selectedIndex)
                {
                    targetPos.x += push;
                }
                else
                {
                    targetPos.x -= push;
                }
            }

            // =================================================
            // 새 카드
            // =================================================

            if (card == newlyDrawnCard)
            {
                card.PlayDrawAnimation(
                    targetPos,
                    transformData.ZRotation,
                    drawDuration
                );

                // 기준 위치는 밀기 적용 전 값(basePos)으로 저장
                card.UpdateOriginalTransform(
                    basePos,
                    transformData.ZRotation
                );

                continue;
            }

            // =================================================
            // 일반 카드 이동
            // =================================================

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

            // =================================================
            // 기준 위치 저장 (밀기 적용 전 값)
            // =================================================

            card.UpdateOriginalTransform(
                basePos,
                transformData.ZRotation
            );
        }
    }

    // =========================================================
    // Placement
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

    private void AnimateContainerY(float targetY)
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