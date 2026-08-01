using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class KTH_CardDragUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Image iconImage;
    private KTH_CardData cardData;
    private int databaseIndex = -1; // ★ 추가: cardDatabase 내 고유 인덱스

    private Transform originalParent;
    private CanvasGroup canvasGroup;
    private Canvas mainCanvas;

    public KTH_CardData CardData => cardData;
    public int DatabaseIndex => databaseIndex; // ★ 추가

    [Header("카드 앞면 UI 오브젝트")]
    public GameObject frontUI; // Inspector 미지정 시 "Image" 자동 탐색

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        mainCanvas = GetComponentInParent<Canvas>();

        // Inspector에 연결하지 않은 경우 이름이 "Image"인 자식을 자동 탐색
        if (frontUI == null)
        {
            Transform frontTransform = transform.Find("Image");
            if (frontTransform != null) frontUI = frontTransform.gameObject;
        }
    }

    private void Update()
    {
        if (frontUI == null) return;

        float yAngle = transform.localEulerAngles.y;

        // Y축 회전각이 90도 ~ 270도 사이일 때 (뒷면을 바라볼 때) -> 앞면을 끔 (뒤판이 보이게 됨)
        if (yAngle > 90f && yAngle < 270f)
        {
            if (frontUI.activeSelf) frontUI.SetActive(false);
        }
        else // 앞면을 바라볼 때 -> 앞면을 켬
        {
            if (!frontUI.activeSelf) frontUI.SetActive(true);
        }
    }

    /// <summary>기존 호출부 호환용 (인덱스 없이 세팅)</summary>
    public void Setup(KTH_CardData data)
    {
        Setup(data, -1);
    }

    /// <summary>고유 인덱스를 포함한 세팅 (★ 이 오버로드 추가)</summary>
    public void Setup(KTH_CardData data, int index)
    {
        cardData = data;
        databaseIndex = index;
        if (iconImage && data != null && data.icon != null) iconImage.sprite = data.icon;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        // 드래그 중에는 캔버스 맨 위로 올리고 마우스 투과 처리
        if (mainCanvas != null) transform.SetParent(mainCanvas.transform);
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
            // 드롭 대상이 인벤토리 구역(또는 그 자식)인지 확인
            KTH_InventoryDropArea area = dropTarget.GetComponentInParent<KTH_InventoryDropArea>();
            if (area != null)
            {
                // 인벤토리 구역으로 부모 변경
                transform.SetParent(area.transform);

                // 인벤토리에 넣었을 때도 리프레시 필요 시 호출
                if (KTH_DeckBuilderManager.Instance != null)
                {
                    KTH_DeckBuilderManager.Instance.RefreshPoolPage();
                }
                return;
            }
        }

        // 드롭 대상이 아니면 원래 위치로 복귀
        transform.SetParent(originalParent);

        // 카드 배치가 끝난 후 상단 풀 UI를 즉시 다시 계산하여 리프레시
        if (KTH_DeckBuilderManager.Instance != null)
        {
            KTH_DeckBuilderManager.Instance.RefreshPoolPage();
        }
    }
}