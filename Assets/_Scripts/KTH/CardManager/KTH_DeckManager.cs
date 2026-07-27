using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class KTH_DeckManager : MonoBehaviour
{
    [Header("카드 데이터베이스 (에셋 8장 등록)")]
    public List<KTH_CardData> cardDatabase;

    [Header("프리팹")]
    public KTH_HandCardView handCardPrefab;
    public KTH_PlacedUnitView placedUnitPrefab;

    [Header("배치 위치 (빈 오브젝트로 씬에 배치)")]
    public Transform handContainer;
    public Transform boardContainer;
    public float handSpacing = 2.2f;
    public float boardSpacing = 2.2f;

    [Header("UI")]
    public Button drawButton;
    public KTH_InfoPanelController infoPanel;

    private readonly List<KTH_HandCardView> currentHand = new List<KTH_HandCardView>();
    private int placedCount = 0;

    private void Awake()
    {
        if (drawButton) drawButton.onClick.AddListener(DrawCards);
        if (infoPanel) infoPanel.Hide();
    }



    /// <summary>드로우 버튼 -> 카드 2장 랜덤 뽑아서 손패에 배치</summary>
    public void DrawCards()
    {
        ClearHand();
        if (infoPanel) infoPanel.Hide();

        var drawn = cardDatabase.OrderBy(_ => Random.value).Take(2).ToList();

        for (int i = 0; i < drawn.Count; i++)
        {
            var view = Instantiate(handCardPrefab, handContainer);
            float offset = (i - (drawn.Count - 1) / 2f) * handSpacing;
            view.transform.localPosition = new Vector3(offset, 0f, 0f);
            view.Setup(drawn[i], this);
            currentHand.Add(view);
        }
    }

    private void ClearHand()
    {
        foreach (var c in currentHand)
            if (c) Destroy(c.gameObject);
        currentHand.Clear();
    }

    /// <summary>손패 카드를 클릭했을 때 -> 정보 패널 + 배치 버튼 노출</summary>
    public void SelectCard(KTH_HandCardView card)
    {
        foreach (var c in currentHand)
            c.SetSelected(c == card);

        infoPanel.Show(card.GetData(), true, () => PlaceCard(card));
    }

    /// <summary>보드에 이미 배치된 기물을 클릭했을 때 -> 정보만 표시 (배치 버튼 없음)</summary>
    public void ShowPlacedUnitInfo(KTH_CardData data)
    {
        infoPanel.Show(data, false, null);
    }

    private void PlaceCard(KTH_HandCardView card)
    {
        var data = card.GetData();

        currentHand.Remove(card);
        Destroy(card.gameObject);
        infoPanel.Hide();

        var unit = Instantiate(placedUnitPrefab, boardContainer);
        unit.transform.localPosition = new Vector3(placedCount * boardSpacing, 0f, 0f);
        unit.Setup(data, this);
        placedCount++;
    }
}
