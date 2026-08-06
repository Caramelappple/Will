using System;
using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 덱 편성 화면의 카드 한 장. 드래그로 풀 ↔ 인벤토리를 오간다.
/// 드롭 결과는 콜백으로 알리기만 하고, 목록을 어떻게 갱신할지는 알지 못한다.
/// LSO_ButtonClickHandler / LSO_ButtonHoverHandler가 이 컴포넌트를 찾아 클릭·호버를 전달한다.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(RectTransform))]
public class KTH_CardDragUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    LSO_IClickEffect, LSO_IHoverEffect
{
    [Header("참조")]
    [SerializeField] private Image iconImage;

    [Header("카드 앞면 UI 오브젝트")]
    [Tooltip("비워두면 이름이 \"Image\"인 자식을 찾는다.")]
    [SerializeField] private GameObject frontUI;

    [Header("LSO 클릭/호버 연동 (선택 사항)")]
    [Tooltip("LSO_ButtonHoverHandler를 붙였을 때, 호버 중 CanvasGroup 알파값")]
    [SerializeField, Range(0f, 1f)] private float hoverAlpha = 0.85f;

    [Tooltip("클릭 시 선택 상태를 토글할지 여부")]
    [SerializeField] private bool toggleSelectedOnClick = true;

    private LSO_CardSO _cardData;
    private int _databaseIndex = -1;
    // 두 번째 인자는 "인벤토리 구역에 놓였는가". 어디에 놓였는지는 카드가 가장 정확히 안다.
    private Action<KTH_CardDragUI, bool> _onDropped;

    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Canvas _rootCanvas;
    private Camera _uiCamera;

    // 드래그를 취소했을 때 정확히 원래 자리로 돌리기 위해 함께 기억한다.
    private Transform _originalParent;
    private int _originalSiblingIndex;
    private Vector2 _originalAnchoredPosition;

    private float _lastCheckedYAngle = float.NaN;

    public LSO_CardSO CardData => _cardData;
    public int DatabaseIndex => _databaseIndex;
    public bool IsSelected { get; private set; }

    private void Awake()
    {
        _rectTransform = (RectTransform)transform;
        _canvasGroup = GetComponent<CanvasGroup>();
        _rootCanvas = GetComponentInParent<Canvas>();

        // Overlay 캔버스는 카메라를 넘기면 안 되고, 그 외에는 반드시 넘겨야 한다.
        if (_rootCanvas != null)
            _uiCamera = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _rootCanvas.worldCamera;

        if (frontUI == null)
        {
            Transform found = transform.Find("Image");
            if (found != null) frontUI = found.gameObject;
        }
    }

    /// <summary>
    /// 카드가 뒤집힌 각도면 앞면을 숨긴다.
    /// 회전은 등장 연출 때만 일어나므로 각도가 변했을 때만 검사한다.
    /// </summary>
    private void Update()
    {
        if (frontUI == null) return;

        float yAngle = _rectTransform.localEulerAngles.y;
        if (Mathf.Approximately(yAngle, _lastCheckedYAngle)) return;

        _lastCheckedYAngle = yAngle;

        bool showFront = !(yAngle > 90f && yAngle < 270f);
        if (frontUI.activeSelf != showFront) frontUI.SetActive(showFront);
    }

    /// <param name="onDropped">드롭이 끝났을 때 알릴 대상. 두 번째 인자는 인벤토리에 놓였는지 여부.</param>
    public void Setup(LSO_CardSO data, int index, Action<KTH_CardDragUI, bool> onDropped = null)
    {
        _cardData = data;
        _databaseIndex = index;
        _onDropped = onDropped;

        if (iconImage && data != null)
            iconImage.sprite = data.Image;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _originalParent = transform.parent;
        _originalSiblingIndex = transform.GetSiblingIndex();
        _originalAnchoredPosition = _rectTransform.anchoredPosition;

        // 드래그 중에는 캔버스 맨 위로 올리고, 자기 자신이 드롭 대상 판정을 가로막지 않게 한다.
        if (_rootCanvas != null) transform.SetParent(_rootCanvas.transform, true);
        _canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_rootCanvas == null) return;

        // 스크린 좌표를 그대로 transform.position에 대입하면 Overlay가 아닌 캔버스에서 어긋난다.
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                (RectTransform)_rootCanvas.transform,
                eventData.position,
                _uiCamera,
                out Vector3 worldPoint))
        {
            transform.position = worldPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _canvasGroup.blocksRaycasts = true;

        GameObject dropTarget = eventData.pointerCurrentRaycast.gameObject;
        KTH_InventoryDropArea area = dropTarget != null
            ? dropTarget.GetComponentInParent<KTH_InventoryDropArea>()
            : null;

        bool droppedInInventory = area != null;

        if (droppedInInventory)
            transform.SetParent(area.transform, false);
        else
            RestoreOriginalPlacement();

        _onDropped?.Invoke(this, droppedInInventory);
    }

    /// <summary>부모뿐 아니라 순서와 위치까지 되돌린다. 부모만 돌리면 카드가 엉뚱한 자리에 남는다.</summary>
    private void RestoreOriginalPlacement()
    {
        if (_originalParent == null) return;

        transform.SetParent(_originalParent, false);
        transform.SetSiblingIndex(_originalSiblingIndex);
        _rectTransform.anchoredPosition = _originalAnchoredPosition;
    }

    /// <summary>LSO_ButtonClickHandler가 호출 (좌클릭 시)</summary>
    public void OnClick()
    {
        if (!toggleSelectedOnClick) return;

        IsSelected = !IsSelected;
    }

    /// <summary>LSO_ButtonHoverHandler가 호출 (포인터 진입 시)</summary>
    public void OnHoverEnter()
    {
        if (_canvasGroup != null) _canvasGroup.alpha = hoverAlpha;
    }

    /// <summary>LSO_ButtonHoverHandler가 호출 (포인터 이탈 시)</summary>
    public void OnHoverExit()
    {
        if (_canvasGroup != null) _canvasGroup.alpha = 1f;
    }
}
