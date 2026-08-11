using System;
using _Scripts.LDY;
using _Scripts.LSO;
using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.UI;
using _Scripts.LSO.Will;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 덱 편성 화면의 카드 한 장. 드래그로 풀 ↔ 인벤토리를 오간다.
/// ScriptableObject 다형성을 지원하여 LSO_AnimalSO, DLJ_WillDataSO 등 다양한 SO 데이터를 처리한다.
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

    private ScriptableObject _cardData;
    private int _databaseIndex = -1;
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

    public ScriptableObject CardData => _cardData;
    public int DatabaseIndex => _databaseIndex;
    public bool IsSelected { get; private set; }

    // [복사 방지용 추가] 해당 카드가 인벤토리에 속한 카드인지 여부
    public bool IsFromInventory { get; private set; }

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

    private void Update()
    {
        if (frontUI == null) return;

        float yAngle = _rectTransform.localEulerAngles.y;
        if (Mathf.Approximately(yAngle, _lastCheckedYAngle)) return;

        _lastCheckedYAngle = yAngle;

        bool showFront = !(yAngle > 90f && yAngle < 270f);
        if (frontUI.activeSelf != showFront) frontUI.SetActive(showFront);
    }

    /// <param name="data">LSO_AnimalSO, DLJ_WillDataSO 등 임의의 ScriptableObject</param>
    /// <param name="onDropped">드롭이 끝났을 때 알릴 대상. 두 번째 인자는 인벤토리에 놓였는지 여부.</param>
    /// <param name="isFromInventory">이 카드가 인벤토리 구역에 있던 카드로 생성되었는지 여부</param>
    public void Setup(ScriptableObject data, int index, Action<KTH_CardDragUI, bool> onDropped = null, bool isFromInventory = false)
    {
        _cardData = data;
        _databaseIndex = index;
        _onDropped = onDropped;
        IsFromInventory = isFromInventory;

        UpdateCardVisuals(data);
    }

    /// <summary>
    /// 타입별(AnimalSO, WillDataSO 등) 스프라이트 및 비주얼 바인딩
    /// </summary>
    private void UpdateCardVisuals(ScriptableObject data)
    {
        if (iconImage == null || data == null) return;

        // 1. 기존 LSO_CardSO 타입인 경우
        if (data is LSO_CardSO cardSO)
        {
            iconImage.sprite = cardSO.Image;
        }
        // 2. LSO_AnimalSO 타입인 경우 (필요 시 내부 Sprite 필드명에 맞게 조정 가능)
        else if (data is LSO_AnimalSO animalSO)
        {
            // 예: animalSO에 Sprite/Icon 변수가 있다면 지정
            // iconImage.sprite = animalSO.icon;
        }
        // 3. DLJ_WillDataSO 타입인 경우 (필요 시 내부 Sprite 필드명에 맞게 조정 가능)
        else if (data is DLJ_WillDataSO willSO)
        {
            // 예: willSO에 Sprite/Icon 변수가 있다면 지정
            // iconImage.sprite = willSO.icon;
        }
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

    private void RestoreOriginalPlacement()
    {
        if (_originalParent == null) return;

        transform.SetParent(_originalParent, false);
        transform.SetSiblingIndex(_originalSiblingIndex);
        _rectTransform.anchoredPosition = _originalAnchoredPosition;
    }

    public void OnClick()
    {
        if (!toggleSelectedOnClick) return;

        IsSelected = !IsSelected;
    }

    public void OnHoverEnter()
    {
        if (_canvasGroup != null) _canvasGroup.alpha = hoverAlpha;
    }

    public void OnHoverExit()
    {
        if (_canvasGroup != null) _canvasGroup.alpha = 1f;
    }
}