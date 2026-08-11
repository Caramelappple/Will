using System;
using _Scripts.LSO.Deck.Data;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class KTH_HandCardView : MonoBehaviour, IPointerClickHandler
{
    [Header("참조")]
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject selectionOutline;

    [Header("카드 앞/뒷면 UI")]
    [SerializeField] private GameObject frontUI;
    [SerializeField] private GameObject backUI; // ★ 카드 뒷면 오브젝트 추가

    [Header("선택 시 떠오르는 연출")]
    [SerializeField] private float selectRiseHeight = 60f;
    [SerializeField] private float selectMoveSpeed = 10f;
    [SerializeField] private int selectedSortingOrder = 10;

    private const float OffsetSnapThreshold = 0.05f;

    private LSO_CardSO _data;
    private Action<KTH_HandCardView> _onClicked;
    private RectTransform _rectTransform;
    private Canvas _canvas;

    private Vector2 _originBasePosition;
    private Vector2 _basePosition;
    private Quaternion _originRotation = Quaternion.identity;
    private float _yOffset;
    private bool _isSelected;
    private bool _isOffsetSettled = true;

    public LSO_CardSO Data => _data;
    public bool IsSelected => _isSelected;

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
        if (_canvas != null)
        {
            _canvas.overrideSorting = false;
        }

        // 자동으로 Front/Back 오브젝트 찾기 (인스펙터 미할당 시)
        if (frontUI == null)
        {
            Transform foundFront = transform.Find("Front");
            if (foundFront != null) frontUI = foundFront.gameObject;
        }
        if (backUI == null)
        {
            Transform foundBack = transform.Find("Back");
            if (foundBack != null) backUI = foundBack.gameObject;
        }

        if (selectionOutline) selectionOutline.SetActive(false);
    }

    private void Update()
    {
        if (!_isOffsetSettled && enabled)
        {
            UpdateSelectionOffset();
        }

        // ★ 카드가 회전할 때 실시간으로 Y축 각도를 체크하여 앞/뒷면 전환
        UpdateCardFace();
    }

    private void UpdateSelectionOffset()
    {
        float targetOffset = _isSelected ? selectRiseHeight : 0f;
        _yOffset = Mathf.Lerp(_yOffset, targetOffset, Time.deltaTime * selectMoveSpeed);

        Quaternion targetRotation = _isSelected ? Quaternion.identity : _originRotation;
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * selectMoveSpeed);

        if (Mathf.Abs(_yOffset - targetOffset) < OffsetSnapThreshold &&
            Quaternion.Angle(transform.localRotation, targetRotation) < 0.5f)
        {
            _yOffset = targetOffset;
            transform.localRotation = targetRotation;
            _isOffsetSettled = true;
        }

        ApplyPosition();
    }

    private void ApplyPosition()
    {
        if (_rectTransform == null) _rectTransform = (RectTransform)transform;

        _rectTransform.anchoredPosition = _basePosition + new Vector2(0f, _yOffset);
        _rectTransform.localScale = Vector3.one;
    }

    /// <summary>
    /// Y축 회전각에 따라 앞면/뒷면 오브젝트를 켜고 끕니다.
    /// </summary>
    private void UpdateCardFace()
    {
        // 로컬 Y축 회전 각도를 0 ~ 360 도 범위로 계산
        float yAngle = transform.localEulerAngles.y;

        // 90도 ~ 270도 사이에 있을 때는 카드 뒷면이 보여야 함
        bool isShowingBack = yAngle > 90f && yAngle < 270f;

        if (frontUI != null) frontUI.SetActive(!isShowingBack);
        if (backUI != null) backUI.SetActive(isShowingBack);
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
        UpdateCardFace();
    }

    public void SnapToBasePosition(Vector2 position, Quaternion rotation)
    {
        _yOffset = 0f;
        _originRotation = rotation;
        transform.localRotation = rotation;
        _isOffsetSettled = true;
        OriginBasePosition = position;
        UpdateCardFace();
    }

    public void SnapToBasePosition(Vector2 position)
    {
        SnapToBasePosition(position, Quaternion.identity);
    }

    public void SetSelected(bool selected)
    {
        if (_isSelected == selected) return;

        _isSelected = selected;
        _isOffsetSettled = false;

        if (selectionOutline) selectionOutline.SetActive(selected);

        if (_canvas != null)
        {
            _canvas.overrideSorting = selected;
            if (selected) _canvas.sortingOrder = selectedSortingOrder;
        }
    }

    public void ResetSelectionOffset()
    {
        _yOffset = 0f;
        transform.localRotation = _originRotation;
        _isOffsetSettled = true;
        ApplyPosition();
        UpdateCardFace();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _onClicked?.Invoke(this);
    }
}