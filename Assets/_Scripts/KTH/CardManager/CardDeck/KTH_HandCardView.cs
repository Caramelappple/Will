using System;
using _Scripts.LSO.Deck.Data;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(GraphicRaycaster))]
public class KTH_HandCardView : MonoBehaviour, IPointerClickHandler
{
    [Header("참조")]
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject selectionOutline;

    [Header("카드 앞면")]
    [SerializeField] private GameObject frontUI;

    [Header("선택 시 떠오르는 연출")]
    [SerializeField] private float selectRiseHeight = 60f;
    [SerializeField] private float selectMoveSpeed = 10f;
    [SerializeField] private int selectedSortingOrder = 10;

    private const float OffsetSnapThreshold = 0.05f;

    private LSO_CardSO _data;
    private Action<KTH_HandCardView> _onClicked;
    private RectTransform _rectTransform;
    private Canvas _canvas;

    private Vector2 _originBasePosition; // [추가] 덱 정렬로 부여된 원래의 진짜 기준 위치
    private Vector2 _basePosition;
    private float _yOffset;
    private bool _isSelected;
    private bool _isOffsetSettled = true;

    public LSO_CardSO Data => _data;
    public bool IsSelected => _isSelected;

    // [추가] 외부에서 카드의 절대 기준 좌표를 설정/조회할 수 있는 프로퍼티
    public Vector2 OriginBasePosition
    {
        get => _originBasePosition;
        set
        {
            _originBasePosition = value;
            BasePosition = value;
        }
    }

    public Vector2 BasePosition
    {
        get => _basePosition;
        set
        {
            _basePosition = value;
            ApplyPosition();
        }
    }

    private void Awake()
    {
        _rectTransform = (RectTransform)transform;
        _canvas = GetComponent<Canvas>();
        _canvas.overrideSorting = false;

        if (frontUI == null)
        {
            Transform found = transform.Find("Front");
            if (found != null) frontUI = found.gameObject;
        }

        if (selectionOutline) selectionOutline.SetActive(false);
        SetFrontActive(true);
    }

    private void Update()
    {
        UpdateSelectionOffset();
    }

    private void UpdateSelectionOffset()
    {
        if (_isOffsetSettled) return;

        float target = _isSelected ? selectRiseHeight : 0f;
        _yOffset = Mathf.Lerp(_yOffset, target, Time.deltaTime * selectMoveSpeed);

        if (Mathf.Abs(_yOffset - target) < OffsetSnapThreshold)
        {
            _yOffset = target;
            _isOffsetSettled = true;
        }

        ApplyPosition();
    }

    private void ApplyPosition()
    {
        if (_rectTransform == null) _rectTransform = (RectTransform)transform;

        _rectTransform.anchoredPosition = _basePosition + new Vector2(0f, _yOffset);
    }

    private void SetFrontActive(bool active)
    {
        if (frontUI != null)
        {
            if (frontUI.activeSelf != active) frontUI.SetActive(active);
        }
        else if (iconImage != null)
        {
            if (iconImage.enabled != active) iconImage.enabled = active;
        }
    }

    public void Setup(LSO_CardSO cardData, Action<KTH_HandCardView> onClicked)
    {
        _data = cardData;
        _onClicked = onClicked;

        if (cardData == null)
            Debug.LogWarning($"[KTH_HandCardView] {name}: 카드 데이터가 비어 있습니다.", this);
        else if (iconImage)
            iconImage.sprite = cardData.Image;

        _yOffset = 0f;
        _isOffsetSettled = true;
        SetSelected(false);
        SetFrontActive(true);
    }

    public void SnapToBasePosition(Vector2 position)
    {
        _yOffset = 0f;
        _isOffsetSettled = true;
        OriginBasePosition = position;
    }

    public void SetSelected(bool selected)
    {
        if (_isSelected == selected) return;

        _isSelected = selected;
        _isOffsetSettled = false;

        if (selectionOutline) selectionOutline.SetActive(selected);

        _canvas.overrideSorting = selected;
        if (selected) _canvas.sortingOrder = selectedSortingOrder;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _onClicked?.Invoke(this);
    }
}