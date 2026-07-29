using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class KTH_CardDragUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Image iconImage;
    private KTH_CardData cardData;

    private Transform originalParent;
    private CanvasGroup canvasGroup;
    private Canvas mainCanvas;

    public KTH_CardData CardData => cardData;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        mainCanvas = GetComponentInParent<Canvas>();
    }

    public void Setup(KTH_CardData data)
    {
        cardData = data;
        if (iconImage && data.icon) iconImage.sprite = data.icon;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        // 드래그 중에는 캔버스 맨 위로 올리고 마우스 투과 처리
        transform.SetParent(mainCanvas.transform);
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        // 마우스 커서 위치 아래에 있는 UI 드롭 대상(인벤토리 구역 등) 감지
        GameObject dropTarget = eventData.pointerCurrentRaycast.gameObject;

        if (dropTarget != null)
        {
            // 드롭 대상이 인벤토리 구역(또나 그 자식)인지 확인
            KTH_InventoryDropArea area = dropTarget.GetComponentInParent<KTH_InventoryDropArea>();
            if (area != null)
            {
                // 인벤토리 구역으로 부모 변경
                transform.SetParent(area.transform);
                return;
            }
        }

        // 드롭 대상이 아니면 원래 위치로 복귀
        transform.SetParent(originalParent);
    }
}