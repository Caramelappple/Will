using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using _Scripts.LSO.UI; // ★ LSO 인터페이스 사용 (LSO 스크립트 자체는 수정하지 않음)

[RequireComponent(typeof(CanvasGroup))]
public class KTH_CardDragUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    LSO_IClickEffect, LSO_IHoverEffect // ★ 추가: LSO_ButtonClickHandler / LSO_ButtonHoverHandler 가
                                       //         이 컴포넌트를 자동으로 찾아서 OnClick / OnHoverEnter / OnHoverExit 를 호출해줌
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

    [Header("LSO 클릭/호버 연동 (선택 사항)")]
    [Tooltip("카드에 LSO_ButtonHoverHandler를 붙였을 때, 호버 중 CanvasGroup 알파값")]
    [Range(0f, 1f)]
    public float hoverAlpha = 0.85f;

    [Tooltip("클릭 시 선택 상태를 토글할지 여부")]
    public bool toggleSelectedOnClick = true;

    public bool IsSelected { get; private set; }

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

    /// <summary>LSO_ButtonClickHandler가 호출 (좌클릭 시)</summary>
    public void OnClick()
    {
        if (toggleSelectedOnClick)
        {
            IsSelected = !IsSelected;
        }

        // TODO: 실제 프로젝트에 맞는 클릭 반응(정보창 표시 등)을 여기에 연결하세요.
    }

    /// <summary>LSO_ButtonHoverHandler가 호출 (포인터 진입 시)</summary>
    public void OnHoverEnter()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = hoverAlpha;
        }
    }

    /// <summary>LSO_ButtonHoverHandler가 호출 (포인터 이탈 시)</summary>
    public void OnHoverExit()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }
}