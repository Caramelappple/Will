using TMPro;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class KTH_HandCardView : MonoBehaviour
{
    [Header("참조")]
    public SpriteRenderer iconRenderer;
    public GameObject selectionOutline; // 선택됐을 때만 활성화

    private KTH_CardData data;
    private KTH_DeckManager manager;



    public void Setup(KTH_CardData cardData, KTH_DeckManager deckManager)
    {
        data = cardData;
        manager = deckManager;

        if (iconRenderer) iconRenderer.sprite = cardData.icon;

        SetSelected(false);
    }

    public KTH_CardData GetData() => data;

    public void SetSelected(bool selected)
    {
        if (selectionOutline) selectionOutline.SetActive(selected);
    }

    private void OnMouseDown()
    {
        Debug.Log("카드 클릭됨: " + gameObject.name); // 이 줄 추가
        if (manager != null) manager.SelectCard(this);
    }   
}
