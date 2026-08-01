using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class KTH_DeckManager : MonoBehaviour
{
    [Header("카드 데이터베이스 (1씬의 인벤토리 카드가 자동으로 불러와집니다)")]
    public List<KTH_CardData> cardDatabase = new List<KTH_CardData>();

    [Header("프리팹")]
    public KTH_HandCardView handCardPrefab;
    public KTH_PlacedUnitView placedUnitPrefab;

    [Header("배치 위치 (빈 오브젝트로 씬에 배치)")]
    public Transform handContainer;
    public Transform boardContainer;
    public float handSpacing = 2.2f;
    public float boardSpacing = 2.2f;

    [Header("카드 크기 설정")]
    public Vector3 targetCardScale = new Vector3(4f, 6f, 1f);  // 카드 목표 크기

    [Header("DOTween 기본 이동 연출")]
    public float moveDuration = 0.5f;     // 손패로 이동하는 시간

    // ★ [핵심] 회전 속도 및 시간을 인스펙터에서 개인적으로 관리하기 쉽게 분리!
    [Header("카드 회전 속도/시간 설정")]
    [Tooltip("카드가 뒤집히며 펼쳐지는 회전 지속 시간 (작을수록 빠르게 회전)")]
    public float flipAnimDuration = 0.25f; // 기본값 0.25초 (빠른 회전)

    [Tooltip("카드 등장 시 차례대로 회전하는 시차 (작을수록 연달아 빠르게 나옴)")]
    public float cardAnimInterval = 0.08f;

    [Tooltip("회전 시작 시 Y축 각도 (-180, 180 추천)")]
    public float startYAngle = 180f;

    [Header("UI")]
    public Button drawButton;
    public KTH_InfoPanelController infoPanel;

    private readonly List<KTH_HandCardView> currentHand = new List<KTH_HandCardView>();
    private int placedCount = 0;

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

    /// <summary>독립된 회전 수치(flipAnimDuration)를 반영하여 드로우</summary>
    public void DrawCards()
    {
        ClearHand();
        if (infoPanel) infoPanel.Hide();

        if (cardDatabase == null || cardDatabase.Count == 0)
        {
            Debug.LogError("[KTH_DeckManager] cardDatabase가 비어있습니다! 1씬에서 카드를 넣었는지 확인하세요.");
            return;
        }

        List<KTH_CardData> drawn = new List<KTH_CardData>();
        int drawCount = 2; // 뽑을 카드 수

        for (int k = 0; k < drawCount; k++)
        {
            int randomIndex = Random.Range(0, cardDatabase.Count);
            drawn.Add(cardDatabase[randomIndex]);
        }

        // 1. 드로우 버튼 출발 위치 계산
        Vector3 buttonStartPosition = handContainer.InverseTransformPoint(drawButton.transform.position);

        for (int i = 0; i < drawn.Count; i++)
        {
            var view = Instantiate(handCardPrefab, handContainer);
            view.Setup(drawn[i], this);
            currentHand.Add(view);

            // 2. 최종 손패 목표 위치 계산
            float targetX = (i - (drawn.Count - 1) / 2f) * handSpacing;
            Vector3 targetPosition = new Vector3(targetX, 0f, 0f);

            Transform cardTransform = view.transform;

            // 3. 초기 상태 설정 (X축 Scale 0, Y축 startYAngle 회전)
            cardTransform.localPosition = buttonStartPosition;
            cardTransform.localScale = new Vector3(0f, targetCardScale.y, targetCardScale.z);
            cardTransform.localRotation = Quaternion.Euler(0f, startYAngle, 0f);

            // 4. 연출 실행
            Sequence drawSequence = DOTween.Sequence();

            drawSequence.PrependInterval(i * cardAnimInterval);
            drawSequence.Join(cardTransform.DOLocalMove(targetPosition, moveDuration).SetEase(Ease.OutCubic));

            // ★ 설정한 flipAnimDuration 속도로 회전 및 커짐!
            drawSequence.Join(cardTransform.DOScale(targetCardScale, flipAnimDuration).SetEase(Ease.OutBack));
            drawSequence.Join(cardTransform.DOLocalRotate(Vector3.zero, flipAnimDuration).SetEase(Ease.OutCubic));
        }
    }

    private void ClearHand()
    {
        foreach (var c in currentHand)
        {
            if (c)
            {
                c.transform.DOKill();
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

        var unit = Instantiate(placedUnitPrefab, boardContainer);
        unit.transform.localPosition = new Vector3(placedCount * boardSpacing, 0f, 0f);
        unit.Setup(data, this);

        unit.transform.localScale = targetCardScale * 0.5f;
        unit.transform.DOScale(targetCardScale, 0.25f).SetEase(Ease.OutBack);

        placedCount++;
    }
}