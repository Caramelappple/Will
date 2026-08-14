using System.Collections.Generic;
using System.Linq;
using _Scripts.LDY;
using _Scripts.LSO.Deck.Data;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class KTH_DeckManager : MonoBehaviour
{
    [Header("그리드 보드 연동")]
    public LDY_CardPlacer cardPlacer;

    [Header("카드 데이터베이스")]
    public List<LSO_CardSO> cardDatabase =
        new List<LSO_CardSO>();

    [Header("프리팹")]
    public KTH_HandCardView handCardPrefab;

    [Header("배치 위치")]
    public RectTransform handContainer;
    public float handSpacing = 220f;

    [Header("UI 카메라")]
    public Camera uiCamera;

    [Header("DOTween 기본 이동 연출")]
    public float moveDuration = 0.5f;

    [Header("카드 회전 속도/시간")]
    public float flipAnimDuration = 0.25f;
    public float cardAnimInterval = 0.08f;
    public float startYAngle = 180f;

    [Header("손패 누적 설정")]
    public int drawCountPerTurn = 2;
    public int maxHandSize = 6;
    public float rearrangeDuration = 0.25f;

    [Header("UI")]
    public Button drawButton;
    public KTH_InfoPanelController infoPanel;

    [Header("소환 시 덱 창 연출")]
    public RectTransform deckPanelRoot;
    public float deckHideYOffset = -300f;
    public float deckAnimDuration = 0.3f;

    private Vector2 _deckPanelOriginalPos;

    private readonly List<KTH_HandCardView> currentHand =
        new List<KTH_HandCardView>();

    private bool isDrawing;

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
            KTH_DeckDataPersistent.Instance.SavedInventory.Count > 0)
        {
            cardDatabase =
                new List<LSO_CardSO>(
                    KTH_DeckDataPersistent.Instance.SavedInventory
                );

            Debug.Log(
                $"[KTH_DeckManager] " +
                $"카드 {cardDatabase.Count}장을 불러왔습니다."
            );
        }
    }

    private void Start()
    {
        isDrawing = false;

        UpdateDrawButtonState();
    }

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

    public void DeselectAllCards()
    {
        foreach (var card in currentHand)
        {
            if (card != null)
                card.SetSelected(false);
        }

        SetUnselectedCardsVisible(true);
    }

    private void SetCardRaycast(
        KTH_HandCardView cardView,
        bool enabled)
    {
        if (cardView == null)
            return;

        CanvasGroup canvasGroup =
            cardView.GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = enabled;
            canvasGroup.interactable = enabled;

            return;
        }

        Graphic[] graphics =
            cardView.GetComponentsInChildren<Graphic>(
                true
            );

        foreach (Graphic graphic in graphics)
        {
            graphic.raycastTarget = enabled;
        }
    }

    public void SetUnselectedCardsVisible(
        bool visible)
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

            SetCardRaycast(
                card,
                visible
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

    public void DrawCards()
    {
        if (isDrawing)
            return;

        if (GetRemainingHandSlots() <= 0)
        {
            UpdateDrawButtonState();
            return;
        }

        List<LSO_CardSO> drawableCards =
            GetDrawableCards();

        if (drawableCards.Count == 0)
        {
            Debug.LogWarning(
                "[KTH_DeckManager] " +
                "드로우 가능한 카드가 없습니다."
            );

            UpdateDrawButtonState();

            return;
        }

        // 기존 선택 상태와 정보 패널을 먼저 정리한다.
        DeselectAllCards();

        if (infoPanel != null)
            infoPanel.Hide();

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

            // 생성 직후에는 클릭을 막는다.
            SetCardRaycast(
                view,
                false
            );
        }

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
                        .SetEase(Ease.OutBack)
                );

                sequence.Join(
                    cardTransform
                        .DOLocalRotate(
                            Vector3.zero,
                            flipAnimDuration
                        )
                        .SetEase(Ease.OutCubic)
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

        // 혹시 Tween OnComplete가 누락되는 상황 대비
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

        // 드로우가 완전히 끝난 뒤 카드 클릭을 허용한다.
        foreach (KTH_HandCardView card in currentHand)
        {
            if (card != null)
                SetCardRaycast(card, true);
        }

        UpdateDrawButtonState();
    }

    private Vector2 GetHandSlotPosition(
        int index,
        int handCount)
    {
        float targetX =
            (index -
             (handCount - 1) / 2f)
            * handSpacing;

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
        view.OriginBasePosition = target;

        return DOTween.To(
                () => view.BasePosition,
                value => view.BasePosition = value,
                target,
                duration
            )
            .SetEase(Ease.OutCubic)
            .SetTarget(view.transform)
            .SetLink(view.gameObject);
    }

    public void SelectCard(
        KTH_HandCardView card)
    {
        if (card == null ||
            card.Data == null)
            return;

        // 카드 이동 Tween만 정리
        card.transform.DOKill();

        foreach (KTH_HandCardView currentCard
                 in currentHand)
        {
            if (currentCard != null)
            {
                currentCard.SetSelected(
                    currentCard == card
                );
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

    public void SetAllCardsVisible(
        bool visible)
    {
        for (int i = 0;
             i < currentHand.Count;
             i++)
        {
            KTH_HandCardView card =
                currentHand[i];

            if (card == null)
                continue;

            if (visible)
                card.ResetSelectionOffset();

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

            SetCardRaycast(
                card,
                visible
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

    public void ShowPlacedUnitInfo(
        LSO_CardSO data)
    {
        if (infoPanel != null)
            infoPanel.Show(
                data,
                false,
                null
            );
    }

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

        if (cardPlacer != null)
        {
            if (!cardPlacer.CanAfford(data))
            {
                Debug.Log(
                    $"[KTH_DeckManager] " +
                    $"코스트가 부족합니다: {data.AnimalName}"
                );

                DeselectAllCards();

                return;
            }

            if (infoPanel != null)
                infoPanel.Hide();

            SetDeckPanelVisible(false);

            bool started =
                cardPlacer.BeginPlacement(
                    data,
                    LDY_Team.Player,

                    onPlaced: animal =>
                    {
                        SetDeckPanelVisible(true);

                        if (animal == null)
                        {
                            DeselectAllCards();
                            return;
                        }

                        FinalizeCardPlacement(
                            card,
                            data
                        );
                    },

                    onCancelled: () =>
                    {
                        SetDeckPanelVisible(true);
                        DeselectAllCards();
                    }
                );

            if (!started)
            {
                SetDeckPanelVisible(true);
                DeselectAllCards();
            }

            return;
        }

        FinalizeCardPlacement(
            card,
            data
        );
    }

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
        currentHand.Remove(card);

        if (card != null)
        {
            card.transform.DOKill();
            Destroy(card.gameObject);
        }

        if (infoPanel != null)
            infoPanel.Hide();

        DeselectAllCards();

        RearrangeHand();

        UpdateDrawButtonState();
    }

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