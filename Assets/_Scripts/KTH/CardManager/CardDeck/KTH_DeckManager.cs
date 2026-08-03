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
    public List<KTH_CardData> cardDatabase = new List<KTH_CardData>();

    [Header("프리팹")]
    public KTH_HandCardView handCardPrefab; // ★ 프리팹은 이제 RectTransform을 가진 UI 오브젝트여야 함
    public KTH_PlacedUnitView placedUnitPrefab;

    [Header("배치 위치")]
    public RectTransform handContainer; // ★ Transform → RectTransform (Canvas 하위의 빈 UI 오브젝트)
    public Transform boardContainer;    // 보드 배치는 기존 월드 스페이스 그대로 유지
    public float handSpacing = 220f;    // ★ UI는 픽셀 단위이므로 기존 2.2f 같은 값 대신 픽셀값으로 조정
    public float boardSpacing = 2.2f;

    [Header("UI 카메라 (Canvas Render Mode가 Screen Space - Overlay면 비워두세요)")]
    public Camera uiCamera; // ★ Screen Space - Camera 또는 World Space Canvas라면 해당 카메라 연결

    [Header("카드 크기 설정")]
    public Vector3 targetCardScale = new Vector3(1f, 1f, 1f);  // ★ UI는 보통 RectTransform 크기로 카드 크기를 잡으므로 스케일은 1 근처를 권장

    [Header("DOTween 기본 이동 연출")]
    public float moveDuration = 0.5f;     // 손패로 이동하는 시간

    [Header("카드 회전 속도/시간 설정")]
    [Tooltip("카드가 뒤집히며 펼쳐지는 회전 지속 시간 (작을수록 빠르게 회전)")]
    public float flipAnimDuration = 0.25f;

    [Tooltip("카드 등장 시 차례대로 회전하는 시차 (작을수록 연달아 빠르게 나옴)")]
    public float cardAnimInterval = 0.08f;

    [Tooltip("회전 시작 시 Y축 각도 (-180, 180 추천)")]
    public float startYAngle = 180f;

    [Header("손패 누적 설정")]
    [Tooltip("드로우 시 한 번에 뽑는 카드 수")]
    public int drawCountPerTurn = 2;

    [Tooltip("손패에 쌓일 수 있는 최대 카드 수 (여기서 직접 설정)")]
    public int maxHandSize = 6;

    [Tooltip("기존에 손패에 있던 카드도 재정렬될 때 이동 연출 시간")]
    public float rearrangeDuration = 0.25f;

    [Header("UI")]
    public Button drawButton;
    public KTH_InfoPanelController infoPanel;

    private readonly List<KTH_HandCardView> currentHand = new List<KTH_HandCardView>();
    private int placedCount = 0;
    private bool isDrawing = false;

    private void Awake()
    {
        if (drawButton) drawButton.onClick.AddListener(DrawCards);
        if (infoPanel) infoPanel.Hide();

        if (KTH_DeckDataPersistent.Instance != null && KTH_DeckDataPersistent.Instance.savedInventory.Count > 0)
        {
            cardDatabase = new List<KTH_CardData>(KTH_DeckDataPersistent.Instance.savedInventory);
            Debug.Log($"[KTH_DeckManager] 1씬으로부터 총 {cardDatabase.Count}장의 카드를 성공적으로 불러왔습니다!");
        }
        else
        {
            Debug.LogWarning("[KTH_DeckManager] 불러올 저장 데이터가 없어 기본 cardDatabase를 사용합니다.");
        }
    }

    /// <summary>손패에 남은 자리 수 (maxHandSize - 현재 손패 수)</summary>
    public int GetRemainingHandSlots()
    {
        return Mathf.Max(0, maxHandSize - currentHand.Count);
    }

    public void DrawCards()
    {
        if (isDrawing) return; // ★ 연타 방지: 이미 드로우 중이면 무시

        if (infoPanel) infoPanel.Hide();

        if (cardDatabase == null || cardDatabase.Count == 0)
        {
            Debug.LogError("[KTH_DeckManager] cardDatabase가 비어있습니다! 1씬에서 카드를 넣었는지 확인하세요.");
            return;
        }

        // ★ 손패가 이미 꽉 찼으면 드로우 자체를 막음
        int remainingSlots = GetRemainingHandSlots();
        if (remainingSlots <= 0)
        {
            Debug.Log("[KTH_DeckManager] 손패가 이미 최대치(" + maxHandSize + "장)입니다. 더 이상 드로우할 수 없습니다.");
            return;
        }

        // ★ 남은 자리만큼만 뽑음 (예: 5/6인 상태에서 2장 요청 → 1장만 드로우)
        int actualDrawCount = Mathf.Min(drawCountPerTurn, remainingSlots);

        isDrawing = true;
        if (drawButton) drawButton.interactable = false; // ★ 버튼 비활성화

        List<KTH_CardData> drawn = new List<KTH_CardData>();
        for (int k = 0; k < actualDrawCount; k++)
        {
            int randomIndex = Random.Range(0, cardDatabase.Count);
            drawn.Add(cardDatabase[randomIndex]);
        }

        // ★ [UI 변환] 월드 좌표 InverseTransformPoint 대신, 스크린 좌표 기준으로 handContainer 로컬 좌표를 구함
        // (Canvas Render Mode가 Screen Space - Overlay면 uiCamera는 비워도 됨(null 전달), Camera/World면 연결 필요)
        Vector2 buttonStartPosition;
        Vector2 buttonScreenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, drawButton.transform.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(handContainer, buttonScreenPoint, uiCamera, out buttonStartPosition);

        // ★ 새로 뽑을 카드를 먼저 리스트에 추가한 뒤, 전체 손패 기준으로 목표 위치를 계산
        List<KTH_HandCardView> newlyDrawnViews = new List<KTH_HandCardView>();

        for (int i = 0; i < drawn.Count; i++)
        {
            var view = Instantiate(handCardPrefab, handContainer);
            view.Setup(drawn[i], this);

            RectTransform cardTransform = (RectTransform)view.transform; // ★ RectTransform으로 캐스팅
            cardTransform.anchoredPosition = buttonStartPosition; // ★ localPosition → anchoredPosition
            cardTransform.localScale = new Vector3(0f, targetCardScale.y, targetCardScale.z);
            cardTransform.localRotation = Quaternion.Euler(0f, startYAngle, 0f);

            currentHand.Add(view);
            newlyDrawnViews.Add(view);
        }

        int completedCount = 0;
        int totalAnimCount = currentHand.Count; // ★ 기존 카드 재정렬 + 신규 카드 등장 전부 포함

        // ★ 손패 전체를 기준으로 목표 위치 재계산 및 이동/등장 연출
        for (int i = 0; i < currentHand.Count; i++)
        {
            var view = currentHand[i];
            RectTransform cardTransform = (RectTransform)view.transform; // ★ RectTransform으로 캐스팅

            float targetX = (i - (currentHand.Count - 1) / 2f) * handSpacing;
            Vector2 targetPosition = new Vector2(targetX, 0f);

            bool isNewCard = newlyDrawnViews.Contains(view);

            cardTransform.DOKill();

            Sequence seq = DOTween.Sequence();
            seq.SetTarget(cardTransform);
            seq.SetLink(view.gameObject);

            if (isNewCard)
            {
                // ★ 새 카드: 드로우 버튼 위치에서 회전하며 등장
                int drawOrderIndex = newlyDrawnViews.IndexOf(view);
                seq.PrependInterval(drawOrderIndex * cardAnimInterval);
                seq.Join(cardTransform.DOAnchorPos(targetPosition, moveDuration).SetEase(Ease.OutCubic)); // ★ DOLocalMove → DOAnchorPos
                seq.Join(cardTransform.DOScale(targetCardScale, flipAnimDuration).SetEase(Ease.OutBack));
                seq.Join(cardTransform.DOLocalRotate(Vector3.zero, flipAnimDuration).SetEase(Ease.OutCubic));
            }
            else
            {
                // ★ 기존 카드: 새 자리로 슬라이드 이동만
                seq.Join(cardTransform.DOAnchorPos(targetPosition, rearrangeDuration).SetEase(Ease.OutCubic)); // ★ DOLocalMove → DOAnchorPos
            }

            // 콜백이 어느 쪽으로 불리든 "완료 처리"는 딱 한 번만 실행되도록 가드
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

    /// <summary>남아있는 손패 카드들을 현재 개수 기준으로 다시 가지런히 정렬</summary>
    private void RearrangeHand()
    {
        for (int i = 0; i < currentHand.Count; i++)
        {
            var view = currentHand[i];
            if (!view) continue;

            RectTransform cardTransform = (RectTransform)view.transform; // ★ RectTransform으로 캐스팅
            float targetX = (i - (currentHand.Count - 1) / 2f) * handSpacing;
            Vector2 targetPosition = new Vector2(targetX, 0f);

            cardTransform.DOKill();
            cardTransform.DOAnchorPos(targetPosition, rearrangeDuration).SetEase(Ease.OutCubic).SetLink(view.gameObject); // ★ DOLocalMove → DOAnchorPos
        }
    }

    private void ClearHand()
    {
        foreach (var c in currentHand)
        {
            if (c)
            {
                c.transform.DOKill(); // SetTarget/SetLink 덕분에 시퀀스까지 안전하게 정리됨
                Destroy(c.gameObject);
            }
        }
        currentHand.Clear();
    }

    public void SelectCard(KTH_HandCardView card)
    {
        foreach (var c in currentHand)
            c.SetSelected(c == card);

        infoPanel.Show(card.GetData(), true, () => PlaceCard(card));
    }

    public void ShowPlacedUnitInfo(KTH_CardData data)
    {
        infoPanel.Show(data, false, null);
    }

    private void PlaceCard(KTH_HandCardView card)
    {
        var data = card.GetData();

        currentHand.Remove(card);
        card.transform.DOKill();
        Destroy(card.gameObject);
        infoPanel.Hide();

        // ★ 카드 하나가 손패에서 빠졌으니 남은 카드들을 재정렬
        RearrangeHand();

        // ★ 손패에 자리가 생겼으니 드로우 중이 아니라면 버튼 다시 활성화
        if (!isDrawing && drawButton) drawButton.interactable = (GetRemainingHandSlots() > 0);

        // 보드에 배치되는 유닛은 기존처럼 월드 스페이스 그대로 유지
        var unit = Instantiate(placedUnitPrefab, boardContainer);
        unit.transform.localPosition = new Vector3(placedCount * boardSpacing, 0f, 0f);
        unit.Setup(data, this);

        unit.transform.localScale = targetCardScale * 0.5f;
        unit.transform.DOScale(targetCardScale, 0.25f).SetEase(Ease.OutBack);

        placedCount++;

        // 실제 전투에 참여하는 기물은 그리드 보드 쪽에 별도로 소환한다 (위 unit은 손패 카드의 연출용 배치).
        if (cardPlacer != null && data.animalCard != null)
        {
            var animal = cardPlacer.PlaceCardAtNextAvailable(data.animalCard, LDY_Team.Player);
            if (animal == null)
                Debug.LogWarning($"[KTH_DeckManager] {data.cardName}을(를) 그리드 보드에 소환하지 못했습니다.");
        }
    }
}
