using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Scripts.LDY;
using _Scripts.LSO.Deck.Data;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 플레이어의 손패(Hand)를 관리하는 매니저.
/// 카드 드로우, 손패 표시/재정렬, 카드 선택, 그리드 배치 연동을 담당한다.
/// </summary>
public class KTH_DeckManager : MonoBehaviour
{
    [Header("그리드 보드 연동")]
    [Tooltip("카드를 실제 그리드 보드에 배치할 때 사용하는 컴포넌트.")]
    [SerializeField] private LDY_CardPlacer cardPlacer;

    [Header("카드 데이터베이스")]
    [SerializeField]
    private List<LSO_CardSO> cardDatabase =
        new List<LSO_CardSO>();

    [Header("프리팹")]
    [SerializeField] private KTH_HandCardView handCardPrefab;

    [Header("배치 위치")]
    [SerializeField] private RectTransform handContainer;

    [Tooltip("카드 사이의 가로 간격")]
    [SerializeField] private float handSpacing = 220f;

    [Header("UI 카메라")]
    [SerializeField] private Camera uiCamera;

    [Header("DOTween 기본 이동 연출")]
    [SerializeField] private float moveDuration = 0.5f;

    [Header("카드 회전 속도/시간")]
    [SerializeField] private float flipAnimDuration = 0.25f;

    [SerializeField] private float cardAnimInterval = 0.08f;

    [SerializeField] private float startYAngle = 180f;

    [Header("손패 누적 설정")]
    [SerializeField] private int drawCountPerTurn = 2;

    [SerializeField] private int maxHandSize = 6;

    [SerializeField] private float rearrangeDuration = 0.25f;

    [Header("UI")]
    [SerializeField] private Button drawButton;

    [SerializeField] private KTH_InfoPanelController infoPanel;

    [Header("소환 시 덱 창 연출")]
    [SerializeField] private RectTransform deckPanelRoot;

    [SerializeField] private float deckHideYOffset = -300f;

    [SerializeField] private float deckAnimDuration = 0.3f;

    /// <summary>
    /// 덱 패널의 원래 위치
    /// </summary>
    private Vector2 _deckPanelOriginalPos;

    /// <summary>
    /// 현재 손패
    /// </summary>
    private readonly List<KTH_HandCardView> currentHand =
        new List<KTH_HandCardView>();

    /// <summary>
    /// 드로우 애니메이션 중인지 여부
    /// </summary>
    private bool isDrawing;


    // ============================================================
    // Unity
    // ============================================================

    private void Awake()
    {
        if (drawButton != null)
        {
            drawButton.onClick.RemoveListener(DrawCards);
            drawButton.onClick.AddListener(DrawCards);
        }

        if (deckPanelRoot != null)
        {
            _deckPanelOriginalPos =
                deckPanelRoot.anchoredPosition;
        }

        if (KTH_DeckDataPersistent.Instance != null &&
            KTH_DeckDataPersistent.Instance.SavedInventory != null &&
            KTH_DeckDataPersistent.Instance.SavedInventory.Count > 0)
        {
            cardDatabase =
                new List<LSO_CardSO>(
                    KTH_DeckDataPersistent.Instance.SavedInventory
                );

            Debug.Log(
                $"[KTH_DeckManager] 카드 {cardDatabase.Count}장을 불러왔습니다."
            );
        }
    }

    private void Start()
    {
        isDrawing = false;

        UpdateDrawButtonState();
    }


    // ============================================================
    // Draw Button
    // ============================================================

    public void UpdateDrawButtonState()
    {
        if (drawButton == null)
            return;

        bool hasDrawable =
            GetDrawableCards().Count > 0;

        bool hasSlot =
            GetRemainingHandSlots() > 0;

        drawButton.interactable =
            !isDrawing &&
            hasDrawable &&
            hasSlot;
    }


    // ============================================================
    // Card Database
    // ============================================================

    private List<LSO_CardSO> GetDrawableCards()
    {
        if (cardDatabase == null)
            return new List<LSO_CardSO>();

        return cardDatabase
            .Where(card =>
                card != null &&
                card.IsValid)
            .ToList();
    }

    public int GetRemainingHandSlots()
    {
        return Mathf.Max(
            0,
            maxHandSize - currentHand.Count
        );
    }


    // ============================================================
    // Selection
    // ============================================================

    /// <summary>
    /// 모든 카드의 선택 상태를 해제한다.
    /// 카드의 Raycast는 절대로 건드리지 않는다.
    /// </summary>
    public void DeselectAllCards()
    {
        foreach (var card in currentHand)
        {
            if (card == null)
                continue;

            card.SetSelected(false);
        }

        // 선택 해제 → 손패 전체가 다시 올라옴
        SetAllCardsVisible(true);
    }
    /// <summary>
    /// 카드 위치만 변경한다.
    /// Raycast / interactable은 건드리지 않는다.
    /// </summary>
    public void SetUnselectedCardsVisible(bool visible)
    {
        for (int i = 0; i < currentHand.Count; i++)
        {
            KTH_HandCardView card =
                currentHand[i];

            if (card == null)
                continue;

            RectTransform cardRect =
                (RectTransform)card.transform;

            cardRect.DOKill();

            Vector2 originPosition =
                card.OriginBasePosition;

            Vector2 targetPosition =
                visible
                    ? originPosition
                    : originPosition +
                      new Vector2(
                          0f,
                          deckHideYOffset
                      );

            DOTween.To(
                    () => card.BasePosition,
                    value => card.BasePosition = value,
                    targetPosition,
                    rearrangeDuration
                )
                .SetEase(
                    visible
                        ? Ease.OutCubic
                        : Ease.InCubic
                )
                .SetTarget(cardRect)
                .SetLink(card.gameObject);
        }
    }


    /// <summary>
    /// 카드를 선택한다.
    /// </summary>
    public void SelectCard(KTH_HandCardView card)
    {
        if (card == null || card.Data == null)
            return;

        if (isDrawing)
            return;

        if (Mouse.current != null &&
            Mouse.current.rightButton.wasPressedThisFrame)
        {
            return;
        }

        if (cardPlacer != null && cardPlacer.IsPlacing)
        {
            cardPlacer.CancelPlacement();
        }

        card.transform.DOKill(true);

        // 선택 상태 변경
        foreach (KTH_HandCardView currentCard in currentHand)
        {
            if (currentCard != null)
            {
                currentCard.SetSelected(currentCard == card);
            }
        }

        if (infoPanel != null)
        {
            infoPanel.Show(
                card,
                showPlaceButton: true,
                onPlace: () => PlaceCard(card),
                onCancel: DeselectAllCards
            );
        }

        SetAllCardsVisible(false);
    }


    // ============================================================
    // Draw Cards
    // ============================================================

    public void DrawCards()
    {
        if (isDrawing)
            return;


        // --------------------------------------------------------
        // 현재 배치 중이었다면 배치만 취소
        // 카드 삭제는 절대 하지 않는다.
        // --------------------------------------------------------

        if (cardPlacer != null &&
            cardPlacer.IsPlacing)
        {
            cardPlacer.CancelPlacement();

            SetDeckPanelVisible(true);

            DeselectAllCards();

            if (infoPanel != null)
                infoPanel.Hide();

            SetAllCardsVisible(true);
        }


        // --------------------------------------------------------
        // 손패 슬롯 확인
        // --------------------------------------------------------

        if (GetRemainingHandSlots() <= 0)
        {
            RearrangeHand();
            UpdateDrawButtonState();
            return;
        }


        // --------------------------------------------------------
        // 드로우 가능한 카드 확인
        // --------------------------------------------------------

        List<LSO_CardSO> drawableCards =
            GetDrawableCards();

        if (drawableCards.Count == 0)
        {
            Debug.LogWarning(
                "[KTH_DeckManager] 드로우 가능한 카드가 없습니다."
            );

            RearrangeHand();
            UpdateDrawButtonState();

            return;
        }


        if (infoPanel != null)
            infoPanel.Hide();


        // --------------------------------------------------------
        // 드로우 개수
        // --------------------------------------------------------

        int remainingSlots =
            GetRemainingHandSlots();

        int actualDrawCount =
            Mathf.Min(
                drawCountPerTurn,
                remainingSlots
            );

        isDrawing = true;


        if (drawButton != null)
            drawButton.interactable = false;


        // --------------------------------------------------------
        // 카드 뽑기
        // --------------------------------------------------------

        List<LSO_CardSO> drawn =
            new List<LSO_CardSO>();

        for (int i = 0;
             i < actualDrawCount;
             i++)
        {
            int randomIndex =
                Random.Range(
                    0,
                    drawableCards.Count
                );

            drawn.Add(
                drawableCards[randomIndex]
            );
        }


        // --------------------------------------------------------
        // 드로우 버튼 위치
        // --------------------------------------------------------

        Vector2 buttonStartPosition =
            Vector2.zero;

        if (drawButton != null &&
            handContainer != null)
        {
            Vector2 screenPoint =
                RectTransformUtility.WorldToScreenPoint(
                    uiCamera,
                    drawButton.transform.position
                );

            RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    handContainer,
                    screenPoint,
                    uiCamera,
                    out buttonStartPosition
                );
        }


        // --------------------------------------------------------
        // 새 카드 생성
        // --------------------------------------------------------

        List<KTH_HandCardView> newlyDrawnViews =
            new List<KTH_HandCardView>();

        foreach (LSO_CardSO cardData in drawn)
        {
            KTH_HandCardView view =
                Instantiate(
                    handCardPrefab,
                    handContainer
                );

            view.Setup(
                cardData,
                SelectCard
            );

            RectTransform cardRect =
                (RectTransform)view.transform;

            cardRect.localScale =
                new Vector3(
                    0f,
                    1f,
                    1f
                );

            view.SnapToBasePosition(
                buttonStartPosition
            );

            cardRect.localRotation =
                Quaternion.Euler(
                    0f,
                    startYAngle,
                    0f
                );

            currentHand.Add(view);
            newlyDrawnViews.Add(view);

            // 중요:
            // Raycast를 끄지 않는다.
            // SelectCard 내부에서 isDrawing을 검사한다.
        }


        // --------------------------------------------------------
        // 카드 배치 애니메이션
        // --------------------------------------------------------

        int completedCount = 0;

        int totalAnimCount =
            currentHand.Count;


        for (int i = 0;
             i < currentHand.Count;
             i++)
        {
            KTH_HandCardView view =
                currentHand[i];

            RectTransform cardTransform =
                (RectTransform)view.transform;

            Vector2 targetPosition =
                GetHandSlotPosition(
                    i,
                    currentHand.Count
                );

            bool isNewCard =
                newlyDrawnViews.Contains(view);

            cardTransform.DOKill();


            Sequence sequence =
                DOTween.Sequence();

            sequence.SetTarget(
                cardTransform
            );

            sequence.SetLink(
                view.gameObject
            );


            if (isNewCard)
            {
                int drawOrderIndex =
                    newlyDrawnViews.IndexOf(view);

                sequence.PrependInterval(
                    drawOrderIndex *
                    cardAnimInterval
                );

                sequence.Join(
                    TweenBasePosition(
                        view,
                        targetPosition,
                        moveDuration
                    )
                );

                sequence.Join(
                    cardTransform
                        .DOScale(
                            Vector3.one,
                            flipAnimDuration
                        )
                        .SetEase(
                            Ease.OutBack
                        )
                );

                sequence.Join(
                    cardTransform
                        .DOLocalRotate(
                            Vector3.zero,
                            flipAnimDuration
                        )
                        .SetEase(
                            Ease.OutCubic
                        )
                );
            }
            else
            {
                sequence.Join(
                    TweenBasePosition(
                        view,
                        targetPosition,
                        rearrangeDuration
                    )
                );
            }


            sequence.OnComplete(() =>
            {
                completedCount++;

                if (completedCount >=
                    totalAnimCount)
                {
                    FinishDraw();
                }
            });
        }


        // 혹시 Tween이 정상적으로 완료되지 않았을 때
        DOVirtual.DelayedCall(
            moveDuration +
            flipAnimDuration +
            cardAnimInterval *
            newlyDrawnViews.Count +
            0.5f,
            () =>
            {
                if (isDrawing)
                    FinishDraw();
            }
        );
    }


    private void FinishDraw()
    {
        if (!isDrawing)
            return;

        isDrawing = false;

        UpdateDrawButtonState();
    }


    // ============================================================
    // Hand Position
    // ============================================================

    private Vector2 GetHandSlotPosition(
        int index,
        int handCount)
    {
        float targetX =
            (
                index -
                (handCount - 1) / 2f
            ) * handSpacing;

        return new Vector2(
            targetX,
            0f
        );
    }


    private Tween TweenBasePosition(
        KTH_HandCardView view,
        Vector2 target,
        float duration)
    {
        view.OriginBasePosition =
            target;

        return DOTween.To(
                () => view.BasePosition,
                value => view.BasePosition = value,
                target,
                duration
            )
            .SetEase(
                Ease.OutCubic
            )
            .SetTarget(
                view.transform
            )
            .SetLink(
                view.gameObject
            );
    }


    // ============================================================
    // 카드 배치
    // ============================================================

    private void PlaceCard(
        KTH_HandCardView card)
    {
        if (card == null)
            return;

        LSO_CardSO data =
            card.Data;

        if (data == null ||
            !data.IsValid)
            return;


        // --------------------------------------------------------
        // 코스트 확인
        // --------------------------------------------------------

        if (cardPlacer != null)
        {
            if (!cardPlacer.CanAfford(data))
            {
                Debug.Log(
                    $"[KTH_DeckManager] 코스트가 부족합니다: {data.AnimalName}"
                );

                DeselectAllCards();

                return;
            }


            if (infoPanel != null)
                infoPanel.Hide();

            SetDeckPanelVisible(false);

            BeginCardPlacement(
                card,
                data
            );

            return;
        }


        // CardPlacer가 없는 경우
        // 바로 배치 완료 처리
        FinalizeCardPlacement(
            card,
            data
        );
    }


    private void BeginCardPlacement(
        KTH_HandCardView card,
        LSO_CardSO data)
    {
        if (cardPlacer == null)
            return;


        bool started =
            cardPlacer.BeginPlacement(
                data,
                LDY_Team.Player,

                // ==================================================
                // 실제 배치 결과
                // ==================================================

                onPlaced: animal =>
                {
                    SetDeckPanelVisible(true);


                    // 배치 실패 / 취소
                    // 카드 유지
                    if (animal == null)
                    {
                        DeselectAllCards();
                        UpdateDrawButtonState();

                        return;
                    }


                    // =================================================
                    // ⭐ 진짜 배치 성공
                    // ⭐ 이때만 카드 삭제
                    // =================================================

                    FinalizeCardPlacement(
                        card,
                        data
                    );
                },


                // ==================================================
                // 배치 취소
                // ==================================================

                onCancelled: () =>
                {
                    Debug.Log(
                        "[KTH_DeckManager] 카드 배치 취소 - 카드를 유지합니다."
                    );

                    SetDeckPanelVisible(true);

                    // 카드 삭제하지 않음
                    // Raycast도 건드리지 않음
                    DeselectAllCards();

                    UpdateDrawButtonState();
                }
            );


        if (!started)
        {
            SetDeckPanelVisible(true);

            DeselectAllCards();

            UpdateDrawButtonState();
        }
    }

    public void SetAllCardsVisible(bool visible)
    {
        for (int i = 0; i < currentHand.Count; i++)
        {
            KTH_HandCardView card = currentHand[i];

            if (card == null)
                continue;

            RectTransform cardRect =
                (RectTransform)card.transform;

            cardRect.DOKill();

            Vector2 originPosition =
                card.OriginBasePosition;

            Vector2 targetPosition;

            if (visible)
            {
                // 다시 원래 자리로 올라옴
                targetPosition = originPosition;

                // 선택 상태도 해제
                card.ResetSelectionOffset();

                SetCardRaycast(
                    card,
                    true
                );
            }
            else
            {
                // =========================================================
                // 화면 아래로 완전히 이동
                // =========================================================

                float screenHeight = 0f;

                if (handContainer != null)
                {
                    screenHeight =
                        handContainer.rect.height;
                }

                // 화면 높이보다 더 아래로 이동시켜
                // 카드가 화면에 전혀 보이지 않도록 한다.
                float hideDistance =
                    screenHeight +
                    cardRect.rect.height +
                    200f;

                targetPosition =
                    originPosition +
                    new Vector2(
                        0f,
                        -hideDistance
                    );

                // 화면 아래로 내려가는 동안에는
                // 클릭되지 않도록 한다.
                SetCardRaycast(
                    card,
                    false
                );
            }

            DOTween.To(
                    () => card.BasePosition,
                    value => card.BasePosition = value,
                    targetPosition,
                    rearrangeDuration
                )
                .SetEase(
                    visible
                        ? Ease.OutCubic
                        : Ease.InCubic
                )
                .SetTarget(cardRect)
                .SetLink(card.gameObject);
        }
    }
    private void SetCardRaycast(
        KTH_HandCardView card,
        bool enabled)
    {
        if (card == null)
            return;

        Graphic[] graphics =
            card.GetComponentsInChildren<Graphic>(true);

        foreach (Graphic graphic in graphics)
        {
            if (graphic == null)
                continue;

            graphic.raycastTarget = enabled;
        }
    }

    public void ShowPlacedUnitInfo(
        LSO_CardSO data)
    {
        if (infoPanel != null)
        {
            infoPanel.Show(
                data,
                false,
                null
            );
        }
    }


    // ============================================================
    // 덱 패널
    // ============================================================

    public void SetDeckPanelVisible(
        bool visible)
    {
        if (deckPanelRoot == null)
            return;


        deckPanelRoot.DOKill();


        Vector2 targetPosition =
            visible
                ? _deckPanelOriginalPos
                : _deckPanelOriginalPos +
                  new Vector2(
                      0f,
                      deckHideYOffset
                  );


        deckPanelRoot
            .DOAnchorPos(
                targetPosition,
                deckAnimDuration
            )
            .SetEase(
                visible
                    ? Ease.OutCubic
                    : Ease.InCubic
            );
    }
    private void FinalizeCardPlacement(
    KTH_HandCardView card,
    LSO_CardSO data)
    {
        if (card == null)
            return;

        // 손패 목록에서 먼저 제거
        currentHand.Remove(card);

        // ⭐ 실제 카드 사용/배치가 완료된 순간에만 삭제
        card.transform.DOKill();

        Destroy(card.gameObject);

        if (infoPanel != null)
            infoPanel.Hide();

        // 남은 카드 선택 해제
        DeselectAllCards();

        // 손패 재정렬
        RearrangeHand();

        // 드로우 버튼 상태 갱신
        UpdateDrawButtonState();
    }


    // ============================================================
    // 손패 재정렬
    // ============================================================

    private void RearrangeHand()
    {
        for (int i = 0;
             i < currentHand.Count;
             i++)
        {
            KTH_HandCardView card =
                currentHand[i];

            if (card == null)
                continue;


            card.transform.DOKill();


            TweenBasePosition(
                card,
                GetHandSlotPosition(
                    i,
                    currentHand.Count
                ),
                rearrangeDuration
            );
        }
    }
}