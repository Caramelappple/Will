using System;
using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LSO.Deck.Data;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using _Scripts.LSO.UI.Panel;

// 3D 전환 메모:
// 이 스크립트는 원래도 RectTransform이 아니라 transform.DOLocalMove / DOLocalRotate를
// 쓰고 있어서 좌표 계산 자체는 그대로 3D에서도 동작한다.
// 유일하게 UI 전용이던 부분은 렌더 순서를 정하던 transform.SetAsLastSibling()이라
// 그 부분만 KTH_HandCard.BringToFront() (내부적으로 SpriteRenderer.sortingOrder 조정)로 바꿨다.
//
// 아래 Spacing/Width/Distance 값들은 이제 "픽셀"이 아니라 월드 스페이스 유닛이라
// 원래 UI 픽셀 값(200, 800, 60...)을 그대로 두면 카드 크기 기준으로 터무니없이 커진다.
// 카드 폭이 대략 1유닛인 걸 기준으로 값들을 다시 잡아뒀으니, 실제 카드 프리팹 크기에 맞춰
// 인스펙터에서 다시 조정해서 쓰면 된다.
//
// handTiltAngle (신규):
// UI에서는 카드가 항상 화면을 정면으로 봐서 회전이 Z축(부채꼴 기울기) 하나면 충분했지만,
// 3D에서는 손패 카드가 살짝 눕는 각도(X축)도 표현할 수 있어야 자연스럽다.
// 그래서 KTH_HandCard의 회전 관련 API를 float(Z만) -> Vector3(X+Z)로 바꾸고,
// 부채꼴로 펼쳐질 때만 X축에 handTiltAngle을 적용한다.
// 선택/배치 중앙으로 모일 때는 항상 Vector3.zero로 세워진다 (원래 로직 그대로).
public class KTH_HandCardLayout : MonoBehaviour
{
    public static KTH_HandCardLayout Instance { get; private set; }

    [Header("References")]
    [SerializeField] private LSO_WillPanel willPanel;

    [Header("Piece Placement (LDY_CardPlacer 연동)")]
    [Tooltip("카드를 확정했을 때 실제 기물 배치를 시작할 대상. LDY_CardPlacer는 이 스크립트에서 건드리지 않고 공개 API만 호출한다.")]
    [SerializeField] private LDY_CardPlacer cardPlacer;

    [Tooltip("배치가 끝난 카드를 버릴 더미. 비워두면 그냥 카드 오브젝트만 반납/파괴한다.")]
    [SerializeField] private KTH_DiscardCardUI discardPile;

    [Header("Arc Layout Settings")]
    [SerializeField] private float maxCardSpacing = 1.2f;
    [SerializeField] private float minCardSpacing = 0.5f;
    [SerializeField] private float maxHandWidth = 6f;
    [SerializeField] private float arcHeight = 0.4f;
    [SerializeField] private float maxRotation = 12f;

    [Header("Hand Tilt (3D 전용)")]
    [Tooltip("손패에서 카드가 X축으로 얼마나 누워있을지. 0이면 완전히 세워짐, 값이 커질수록 뒤로 눕는다.")]
    [SerializeField] private float handTiltAngle = 20f;

    [Header("Organic Motion Settings")]
    [SerializeField] private float staggerDelay = 0.025f;
    [SerializeField] private Ease moveEase = Ease.OutCubic;

    [Header("Hand Settings")]
    [SerializeField] private int maxHandSize = 8;

    [Header("Draw Animation Settings")]
    [SerializeField] private float drawDuration = 0.4f;

    [Header("Selection Settings")]
    [Tooltip("카드 선택 해제/제거 시 손패가 원래대로 정리되는 애니메이션 시간")]
    [SerializeField] private float pushDuration = 0.28f;

    [Header("Placement Mode Settings")]
    [SerializeField] private bool enableMoveDown;
    [SerializeField] private float placementMoveDownDistance = 1f;
    [SerializeField] private float placementMoveDuration = 0.3f;
    [SerializeField] private float placementCenterGap = 1f;

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

    private void Update()
    {
        // 더블클릭으로 카드들이 내려가 있는 동안, 마우스 우클릭 한 번으로 그
        // 상태를 취소할 수 있게 한다. 활성화된 더블클릭이 없으면 CancelDoubleClick이
        // 알아서 아무 일도 하지 않으므로 매 프레임 조건 없이 불러도 안전하다.
        if (Mouse.current != null &&
            Mouse.current.rightButton.wasPressedThisFrame)
        {
            KTH_HandCard.CancelDoubleClick();
        }
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

        // 버려졌다가 풀에서 재사용된 카드는 더블클릭 대상 목록에서 빠진 채로
        // 돌아온다(ResetForPool에서 뺐음). 손패에 다시 들어오는 이 시점에 다시
        // 등록한다.
        card.RegisterForDoubleClick();

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

        // 확정 클릭(배치 시작/취소)을 여기서 받아서 LDY_CardPlacer로 연결한다.
        card.OnCardClicked -= HandleCardConfirmClicked;
        card.OnCardClicked += HandleCardConfirmClicked;

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
        if (card != null)
        {
            card.OnCardClicked -= HandleCardConfirmClicked;
        }

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
    // Piece Placement (LDY_CardPlacer 연동)
    // =========================================================

    /// <summary>
    /// 카드를 클릭했을 때 호출된다. OnCardClicked는 "확정 클릭"과 "취소 클릭" 둘 다에서 불리는데,
    /// 이 시점에는 KTH_HandCard 내부에서 이미 상태를 바꿔놓은 뒤라 card.IsConfirmed로 구분할 수 있다.
    ///   - 확정 클릭 (배치 시작): IsConfirmed == true
    ///   - 취소 클릭 (배치 모드에서 다시 눌러서 취소): IsConfirmed == false
    /// </summary>
    private void HandleCardConfirmClicked(KTH_HandCard card)
    {
        if (card == null)
        {
            return;
        }

        // 취소 클릭이면 여기서 할 일이 없다. 선택 해제는 KTH_HandCard 쪽에서 이미 처리했다.
        if (!card.IsConfirmed)
        {
            return;
        }

        if (cardPlacer == null)
        {
            Debug.LogWarning(
                "[KTH_HandCardLayout] LDY_CardPlacer가 연결되지 않아 기물 배치를 시작할 수 없습니다.",
                this
            );

            card.CancelSelectionState();

            return;
        }

        // KTH_HandCard.OnPointerClick은 같은 확정 클릭에서
        // KTH_InfoPanel.SelectInfoPanl() -> KTH_CardPlacementController.TryBeginPlacement()도
        // 먼저 호출한다. 그쪽이 이미 cardPlacer.BeginPlacement로 배치를 시작해놓은 상태에서
        // 여기서 또 BeginPlacement를 부르면, LDY_CardPlacer가 "이미 배치 중이면 취소하고
        // 새로 시작"하는 구조라 방금 시작된 세션이 조용히 취소되고 콜백이 이 경로 것으로
        // 바뀌어버린다. 배치 세션을 시작하는 주체가 매 클릭마다 둘로 갈리면서 카드가 보드에
        // 놓이는 흐름/위치가 꼬이는 원인이 되므로, 이미 배치가 시작돼 있으면 여기서는
        // 손대지 않는다.
        if (cardPlacer.IsPlacing)
        {
            return;
        }

        LSO_CardSO cardData =
            card.CardData;

        if (cardData == null)
        {
            card.CancelSelectionState();

            return;
        }

        bool started =
            cardPlacer.BeginPlacement(
                cardData,
                LDY_Team.Player,
                onPlaced: animal =>
                {
                    if (animal != null)
                    {
                        // 실제로 보드에 놓였을 때만 손패에서 빼고 버린다.
                        card.ConsumeAndRearrange(
                            discardPile
                        );
                    }
                    else
                    {
                        // 칸이 막혀있거나 실패한 경우 카드는 손패에 그대로 두고 선택만 푼다.
                        card.CancelSelectionState();
                    }
                },
                onCancelled: () =>
                {
                    // 우클릭 등으로 배치를 취소하면 카드는 손패에 남기고 선택만 푼다.
                    card.CancelSelectionState();
                }
            );

        if (!started)
        {
            // 내 턴이 아니거나 코스트가 부족해서 아예 시작을 못 한 경우.
            card.CancelSelectionState();
        }
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

        // 더블클릭으로 내려가 있는 카드가 있으면 부채꼴 재배치와 자리를 다투게
        // 되므로, 배치 모드로 들어가기 전에 먼저 정리한다.
        KTH_HandCard.CancelDoubleClick();

        // 확정된 카드가 아닌데도 호버로 선택된 채 남아있는 다른 카드가 있으면,
        // 부채꼴로 흩어지는 동안 UpdateHandLayout이 그 카드의 이동만 건너뛴다
        // (card.IsSelected면 MoveToHandPositionWithDelay가 자리를 옮기지 않음).
        // 그 상태로 두면 나중에 배치를 취소해도 그 카드만 계속 엉뚱한 자리에 남는다.
        // 배치 모드에 들어가기 전에 미리 정리해서 그런 카드가 없게 한다.
        for (int i = 0; i < handCards.Count; i++)
        {
            KTH_HandCard other = handCards[i];

            if (other == null || other == card)
            {
                continue;
            }

            if (other.IsSelected && !other.IsConfirmed)
            {
                other.CancelSelectionState();
            }
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
        selectedCard.BringToFront();

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

        // 위치는 정중앙이니 부채꼴 Z축 기울기는 0으로 되돌린다.
        // X축(handTiltAngle, 눕는 각도)은 그대로 유지한다.
        moveToCenterSequence.Join(
            selectedCard.transform
                .DOLocalRotate(
                    new Vector3(handTiltAngle, 0f, 0f),
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

    /// <summary>
    /// focalCard를 뺀 나머지 카드들이 focalCard를 기준으로 좌우 부채꼴로 벌어질 목표
    /// 위치/회전을 계산해서 애니메이션까지 실행한다.
    ///
    /// 배치 모드(포커스 카드가 중앙 Vector3.zero로 이동한 상태, anchorX = 0)와
    /// 호버(포커스 카드가 자기 자리에 그대로 있는 상태, anchorX = 그 자리의 X)
    /// 양쪽에서 같이 쓴다.
    ///
    /// 나머지 카드는 실제 손패상의 좌/우 순서가 아니라 "항상 절반씩 좌우로 균등 분배"한다.
    /// (가장자리 카드를 선택해도 나머지가 한쪽으로 쏠리지 않고 중앙 기준으로 고르게 펼쳐짐)
    /// </summary>
    private void ApplyFanAroundFocalCard(
        KTH_HandCard focalCard,
        float anchorX,
        float centerGap,
        float duration)
    {
        int count = handCards.Count;

        if (count <= 1 || focalCard == null)
        {
            return;
        }

        List<KTH_HandCard> otherCards =
            new List<KTH_HandCard>();

        for (int i = 0; i < count; i++)
        {
            KTH_HandCard card =
                handCards[i];

            if (card == null ||
                card == focalCard)
            {
                continue;
            }

            otherCards.Add(card);
        }

        int otherCount =
            otherCards.Count;

        if (otherCount == 0)
        {
            return;
        }

        int leftCount = otherCount / 2;
        int rightCount = otherCount - leftCount;

        // 나머지가 딱 1장일 때는 위 균등분배 공식이 항상 오른쪽으로 밀어버려서
        // 원래 왼쪽에 있던 카드를 선택해도 반대편으로 튀어 보인다.
        // 이 경우만 실제 손패 순서를 보고 원래 있던 쪽으로 보낸다.
        if (otherCount == 1)
        {
            int focalIndex =
                handCards.IndexOf(focalCard);

            int otherIndex =
                handCards.IndexOf(otherCards[0]);

            bool otherIsOnLeft =
                focalIndex >= 0 &&
                otherIndex >= 0 &&
                otherIndex < focalIndex;

            leftCount = otherIsOnLeft ? 1 : 0;
            rightCount = otherIsOnLeft ? 0 : 1;
        }

        float spacing =
            CalculatePlacementSpacing(otherCount + 1);

        for (int i = 0; i < otherCount; i++)
        {
            KTH_HandCard card =
                otherCards[i];

            int relativeIndex =
                i < leftCount
                    ? i - leftCount
                    : i - leftCount + 1;

            float targetX =
                relativeIndex * spacing;

            targetX +=
                relativeIndex < 0
                    ? -centerGap
                    : centerGap;

            targetX += anchorX;

            int sideCount =
                relativeIndex < 0
                    ? leftCount
                    : rightCount;

            float normalized =
                Mathf.Clamp01(
                    Mathf.Abs(relativeIndex) /
                    (float)Mathf.Max(1, sideCount)
                );

            float targetY =
                -normalized * normalized * arcHeight;

            float targetRotationZ =
                relativeIndex < 0
                    ? normalized * maxRotation
                    : -normalized * maxRotation;

            Vector3 targetPos =
                new Vector3(targetX, targetY, 0f);

            Vector3 targetRot =
                new Vector3(handTiltAngle, 0f, targetRotationZ);

            // 더블클릭으로 내려가 있는 카드는 이 부채꼴 재배치에서 건드리지 않는다.
            // 여기서 그대로 옮겨버리면 더블클릭의 "내려간" 위치를 덮어써서 카드가
            // 안 내려간 것처럼 보인다. 대신 "원래 자리"만 최신 값으로 갱신해두면,
            // 나중에 더블클릭이 취소돼 원위치로 돌아올 때 지금 계산한 자리로
            // 정확히 돌아온다.
            if (!card.IsMovedDown)
            {
                card.transform.DOKill();

                Sequence sequence =
                    DOTween.Sequence();

                sequence.SetTarget(card.transform);

                sequence.Join(
                    card.transform
                        .DOLocalMove(targetPos, duration)
                        .SetEase(moveEase)
                );

                sequence.Join(
                    card.transform
                        .DOLocalRotate(targetRot, duration)
                        .SetEase(moveEase)
                );

                sequence.Join(
                    card.transform
                        .DOScale(Vector3.one, duration)
                        .SetEase(moveEase)
                );
            }

            card.UpdateOriginalTransform(targetPos, targetRot);
        }
    }

    /// <summary>
    /// 배치 모드: 선택된 카드는 중앙(Vector3.zero)으로, 나머지는 그 주위로 부채꼴 벌어짐.
    /// </summary>
    private void SpreadCardsAroundCenter()
    {
        if (selectedCard == null)
        {
            return;
        }

        ApplyFanAroundFocalCard(
            selectedCard,
            0f,
            placementCenterGap,
            placementMoveDuration
        );

        selectedCard.transform.DOKill();

        Sequence selectedSequence =
            DOTween.Sequence();

        selectedSequence.SetTarget(selectedCard.transform);

        selectedSequence.Join(
            selectedCard.transform
                .DOLocalMove(Vector3.zero, placementMoveDuration)
                .SetEase(Ease.OutBack)
        );

        selectedSequence.Join(
            selectedCard.transform
                .DOLocalRotate(
                    new Vector3(handTiltAngle, 0f, 0f),
                    placementMoveDuration
                )
                .SetEase(Ease.OutBack)
        );

        selectedSequence.Join(
            selectedCard.transform
                .DOScale(
                    Vector3.one * selectedCard.SelectScale,
                    placementMoveDuration
                )
                .SetEase(Ease.OutBack)
        );

        selectedCard.BringToFront();
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
        // 더블클릭으로 "내려가 있는" 카드가 있는 채로 손패 재배치가 일어나면,
        // 그 카드는 더블클릭 쪽 좌표(isMovedDown)와 이 재배치 쪽 좌표를 각각
        // 다른 시점에 따로 계산해서 서로 자리를 다투게 된다(취소 시 엉뚱한
        // 자리로 튐). 재배치가 일어나는 순간 더블클릭 상태를 먼저 정리해서
        // 자리를 정하는 주체를 이 재배치 하나로 되돌린다.
        KTH_HandCard.CancelDoubleClick();

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

            Vector3 targetRotation =
                new Vector3(
                    handTiltAngle,
                    0f,
                    transformData.ZRotation
                );

            if (card == newlyDrawnCard)
            {
                card.PlayDrawAnimation(
                    targetPosition,
                    targetRotation,
                    drawDuration
                );

                card.UpdateOriginalTransform(
                    targetPosition,
                    targetRotation
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
                    targetRotation,
                    duration,
                    delay,
                    moveEase
                );
            }

            card.UpdateOriginalTransform(
                targetPosition,
                targetRotation
            );
        }

        if (selectedCard != null &&
            selectedCard.IsSelected)
        {
            selectedCard.BringToFront();
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
