using _Scripts.LSO.Deck.Data;
using System.Collections.Generic;
using System.Linq;
using _Scripts.LDY;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class KTH_DeckManager : MonoBehaviour
{
    [Header("그리드 보드 연동 (없으면 기존 연출용 배치만 동작)")]
    public LDY_CardPlacer cardPlacer;

    [Header("카드 데이터베이스 (1씬의 인벤토리 카드가 자동으로 불러와집니다)")]
    public List<LSO_CardSO> cardDatabase = new List<LSO_CardSO>();

    [Header("프리팹")]
    public KTH_HandCardView handCardPrefab;

    [Header("배치 위치")]
    public RectTransform handContainer;
    public float handSpacing = 220f;

    [Header("UI 카메라 (Canvas Render Mode가 Screen Space - Overlay면 비워두세요)")]
    public Camera uiCamera;

    [Header("카드 크기 설정")]
    public Vector3 targetCardScale = new Vector3(1f, 1f, 1f);

    [Header("DOTween 기본 이동 연출")]
    public float moveDuration = 0.5f;

    [Header("카드 회전 속도/시간 설정")]
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

    [Header("소환 시 덱 창 연출 설정")]
    [Tooltip("소환 시작 시 화면 아래로 내려갈 덱/손패 부모 패널 (미지정 시 handContainer 사용)")]
    public RectTransform deckPanelRoot;
    public float deckHideYOffset = -300f; // 내려갈 깊이
    public float deckAnimDuration = 0.3f;

    private Vector2 _deckPanelOriginalPos;
    private readonly List<KTH_HandCardView> currentHand = new List<KTH_HandCardView>();
    private bool isDrawing = false;

    private void Awake()
    {
        if (drawButton) drawButton.onClick.AddListener(DrawCards);
        if (infoPanel) infoPanel.Hide();

        if (deckPanelRoot == null && handContainer != null)
            deckPanelRoot = handContainer;

        if (deckPanelRoot != null)
            _deckPanelOriginalPos = deckPanelRoot.anchoredPosition;

        if (KTH_DeckDataPersistent.Instance != null && KTH_DeckDataPersistent.Instance.savedInventory.Count > 0)
        {
            cardDatabase = new List<LSO_CardSO>(KTH_DeckDataPersistent.Instance.savedInventory);
            Debug.Log($"[KTH_DeckManager] 1씬으로부터 총 {cardDatabase.Count}장의 카드를 성공적으로 불러왔습니다!");
        }
        else
        {
            Debug.LogWarning("[KTH_DeckManager] 불러올 저장 데이터가 없어 기본 cardDatabase를 사용합니다.");
        }
    }

    private List<LSO_CardSO> GetDrawableCards()
    {
        List<LSO_CardSO> result = new List<LSO_CardSO>();
        if (cardDatabase == null) return result;

        for (int i = 0; i < cardDatabase.Count; i++)
        {
            LSO_CardSO card = cardDatabase[i];
            if (card == null || !card.IsValid) continue;
            result.Add(card);
        }

        return result;
    }

    public int GetRemainingHandSlots()
    {
        return Mathf.Max(0, maxHandSize - currentHand.Count);
    }

    /// <summary>
    /// 모든 카드의 선택을 해제하고 안 고른(내려갔던) 카드들을 다시 원위치로 올립니다.
    /// </summary>
    public void DeselectAllCards()
    {
        foreach (var card in currentHand)
        {
            if (card != null) card.SetSelected(false);
        }

        SetUnselectedCardsVisible(true);
    }

    /// <summary>
    /// 선택되지 않은 카드들만 아래로 내리거나 올리는 연출
    /// </summary>
    public void SetUnselectedCardsVisible(bool visible)
    {
        for (int i = 0; i < currentHand.Count; i++)
        {
            var cardView = currentHand[i];
            if (cardView == null) continue;

            if (!cardView.IsSelected)
            {
                RectTransform cardRect = (RectTransform)cardView.transform;
                cardRect.DOKill();

                // [핵심 수정] 기준 위치는 변하지 않는 OriginBasePosition을 사용합니다!
                Vector2 originPos = cardView.OriginBasePosition;
                Vector2 targetPos = visible ? originPos : originPos + new Vector2(0f, deckHideYOffset);

                DOTween.To(() => cardView.BasePosition, value => cardView.BasePosition = value, targetPos, rearrangeDuration)
                       .SetEase(visible ? Ease.OutCubic : Ease.InCubic)
                       .SetTarget(cardRect)
                       .SetLink(cardView.gameObject);
            }
        }
    }

    public void DrawCards()
    {
        if (isDrawing) return;

        DeselectAllCards();
        if (infoPanel) infoPanel.Hide();

        List<LSO_CardSO> drawableCards = GetDrawableCards();
        if (drawableCards.Count == 0) return;

        int remainingSlots = GetRemainingHandSlots();
        if (remainingSlots <= 0) return;

        int actualDrawCount = Mathf.Min(drawCountPerTurn, remainingSlots);

        isDrawing = true;
        if (drawButton) drawButton.interactable = false;

        List<LSO_CardSO> drawn = new List<LSO_CardSO>();
        for (int k = 0; k < actualDrawCount; k++)
        {
            int randomIndex = Random.Range(0, drawableCards.Count);
            drawn.Add(drawableCards[randomIndex]);
        }

        Vector2 buttonStartPosition;
        Vector2 buttonScreenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, drawButton.transform.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(handContainer, buttonScreenPoint, uiCamera, out buttonStartPosition);

        List<KTH_HandCardView> newlyDrawnViews = new List<KTH_HandCardView>();

        for (int i = 0; i < drawn.Count; i++)
        {
            var view = Instantiate(handCardPrefab, handContainer);
            view.Setup(drawn[i], SelectCard);

            RectTransform cardTransform = (RectTransform)view.transform;
            view.SnapToBasePosition(buttonStartPosition);
            cardTransform.localScale = new Vector3(0f, targetCardScale.y, targetCardScale.z);
            cardTransform.localRotation = Quaternion.Euler(0f, startYAngle, 0f);

            currentHand.Add(view);
            newlyDrawnViews.Add(view);
        }

        int completedCount = 0;
        int totalAnimCount = currentHand.Count;

        for (int i = 0; i < currentHand.Count; i++)
        {
            var view = currentHand[i];
            RectTransform cardTransform = (RectTransform)view.transform;

            Vector2 targetPosition = GetHandSlotPosition(i, currentHand.Count);
            bool isNewCard = newlyDrawnViews.Contains(view);

            cardTransform.DOKill();

            Sequence seq = DOTween.Sequence();
            seq.SetTarget(cardTransform);
            seq.SetLink(view.gameObject);

            if (isNewCard)
            {
                int drawOrderIndex = newlyDrawnViews.IndexOf(view);
                seq.PrependInterval(drawOrderIndex * cardAnimInterval);
                seq.Join(TweenBasePosition(view, targetPosition, moveDuration));
                seq.Join(cardTransform.DOScale(targetCardScale, flipAnimDuration).SetEase(Ease.OutBack));
                seq.Join(cardTransform.DOLocalRotate(Vector3.zero, flipAnimDuration).SetEase(Ease.OutCubic));
            }
            else
            {
                seq.Join(TweenBasePosition(view, targetPosition, rearrangeDuration));
            }

            bool finished = false;
            void HandleSequenceFinished()
            {
                if (finished) return;
                finished = true;

                completedCount++;
                if (completedCount >= totalAnimCount)
                {
                    isDrawing = false;
                    if (drawButton) drawButton.interactable = (GetRemainingHandSlots() > 0);
                }
            }

            seq.OnComplete(HandleSequenceFinished);
            seq.OnKill(HandleSequenceFinished);
        }
    }

    private void RearrangeHand()
    {
        for (int i = 0; i < currentHand.Count; i++)
        {
            var view = currentHand[i];
            if (!view) continue;

            view.transform.DOKill();
            TweenBasePosition(view, GetHandSlotPosition(i, currentHand.Count), rearrangeDuration);
        }
    }

    private Vector2 GetHandSlotPosition(int index, int handCount)
    {
        float targetX = (index - (handCount - 1) / 2f) * handSpacing;
        return new Vector2(targetX, 0f);
    }

    private Tween TweenBasePosition(KTH_HandCardView view, Vector2 target, float duration)
    {
        // [핵심 수정] 카드의 원본 위치 정보를 갱신합니다.
        view.OriginBasePosition = target;

        return DOTween.To(() => view.BasePosition, value => view.BasePosition = value, target, duration)
            .SetEase(Ease.OutCubic)
            .SetTarget(view.transform)
            .SetLink(view.gameObject);
    }

    public void SelectCard(KTH_HandCardView card)
    {
        if (card == null) return;

        foreach (var c in currentHand)
            c.SetSelected(c == card);

        // 안 고른 카드들만 내려 숨기기
        SetUnselectedCardsVisible(false);

        if (infoPanel != null)
        {
            infoPanel.Show(
                card,
                showPlaceButton: true,
                onPlace: () => PlaceCard(card),
                onCancel: () =>
                {
                    // [취소 시] 내려갔던 카드 복원
                    DeselectAllCards();
                }
            );
        }
    }

    public void ShowPlacedUnitInfo(LSO_CardSO data)
    {
        if (infoPanel) infoPanel.Show(data, false, null);
    }

    private void PlaceCard(KTH_HandCardView card)
    {
        var data = card.Data;
        if (data == null || !data.IsValid) return;

        if (cardPlacer != null)
        {
            if (!cardPlacer.CanAfford(data))
            {
                Debug.Log($"[KTH_DeckManager] 코스트가 부족해 {data.AnimalName}을(를) 배치할 수 없습니다.");
                return;
            }

            if (infoPanel) infoPanel.Hide();

            SetDeckPanelVisible(false);

            bool started = cardPlacer.BeginPlacement(data, LDY_Team.Player,
                onPlaced: animal =>
                {
                    SetDeckPanelVisible(true);

                    if (animal == null) return;
                    FinalizeCardPlacement(card, data);
                },
                onCancelled: () =>
                {
                    // [배치 취소 시] 패널 및 안 고른 카드 복원
                    SetDeckPanelVisible(true);
                    DeselectAllCards();
                });

            if (!started)
            {
                SetDeckPanelVisible(true);
                DeselectAllCards();
            }

            return;
        }

        FinalizeCardPlacement(card, data);
    }

    public void SetDeckPanelVisible(bool visible)
    {
        if (deckPanelRoot == null) return;

        deckPanelRoot.DOKill();
        Vector2 targetPos = visible ? _deckPanelOriginalPos : _deckPanelOriginalPos + new Vector2(0f, deckHideYOffset);
        deckPanelRoot.DOAnchorPos(targetPos, deckAnimDuration).SetEase(visible ? Ease.OutCubic : Ease.InCubic);
    }

    private void FinalizeCardPlacement(KTH_HandCardView card, LSO_CardSO data)
    {
        currentHand.Remove(card);
        card.transform.DOKill();
        Destroy(card.gameObject);
        if (infoPanel) infoPanel.Hide();

        RearrangeHand();

        if (!isDrawing && drawButton) drawButton.interactable = (GetRemainingHandSlots() > 0);
    }
}