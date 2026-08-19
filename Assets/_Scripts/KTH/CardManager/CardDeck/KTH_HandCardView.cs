using System;
using _Scripts.LSO.Deck.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Canvas))]
public class KTH_HandCardView : MonoBehaviour, IPointerClickHandler
{
    [Header("참조")]
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject selectionOutline;

    [Header("카드 앞/뒷면 UI")]
    [SerializeField] private GameObject frontUI;
    [SerializeField] private GameObject backUI;

    [Header("선택 시 내려가는 연출")]
    [SerializeField] private float selectDropHeight = 100f;
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

        if (frontUI == null)
        {
            Transform foundFront = transform.Find("Front");

            if (foundFront != null)
                frontUI = foundFront.gameObject;
        }

        if (backUI == null)
        {
            Transform foundBack = transform.Find("Back");

            if (foundBack != null)
                backUI = foundBack.gameObject;
        }

        if (selectionOutline != null)
            selectionOutline.SetActive(false);
    }

    private void Update()
    {
        if (!_isOffsetSettled)
        {
            UpdateSelectionOffset();
        }

        UpdateCardFace();
    }

    private void UpdateSelectionOffset()
    {
        float targetOffset = _isSelected
            ? -selectDropHeight
            : 0f;

        _yOffset = Mathf.Lerp(
            _yOffset,
            targetOffset,
            Time.deltaTime * selectMoveSpeed
        );

        Quaternion targetRotation = _isSelected
            ? Quaternion.identity
            : _originRotation;

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRotation,
            Time.deltaTime * selectMoveSpeed
        );

        if (Mathf.Abs(_yOffset - targetOffset) < OffsetSnapThreshold &&
            Quaternion.Angle(
                transform.localRotation,
                targetRotation
            ) < 0.5f)
        {
            _yOffset = targetOffset;
            transform.localRotation = targetRotation;
            _isOffsetSettled = true;
        }

        ApplyPosition();
    }

    private void ApplyPosition()
    {
        if (_rectTransform == null)
        {
            _rectTransform = (RectTransform)transform;
        }

        _rectTransform.anchoredPosition =
            _basePosition +
            new Vector2(0f, _yOffset);
    }

    private void UpdateCardFace()
    {
        float yAngle = transform.localEulerAngles.y;

        bool isShowingBack =
            yAngle > 90f &&
            yAngle < 270f;

        if (frontUI != null)
            frontUI.SetActive(!isShowingBack);

        if (backUI != null)
            backUI.SetActive(isShowingBack);
    }

    public void SnapRotationToFront()
    {
        _originRotation = Quaternion.identity;

        transform.localRotation =
            Quaternion.identity;

        _isOffsetSettled = true;

        UpdateCardFace();
    }

    public void OnPointerClick(
    PointerEventData eventData)
    {
        if (_data == null)
        {
            Debug.LogWarning(
                $"[KTH_HandCardView] {name}: 데이터가 없어 클릭을 처리할 수 없습니다.",
                this
            );

            return;
        }

        if (_onClicked == null)
        {
            Debug.LogWarning(
                $"[KTH_HandCardView] {name}: 클릭 콜백이 등록되지 않았습니다.",
                this
            );

            return;
        }

        _onClicked.Invoke(this);
    }

    public void Setup(
        LSO_CardSO cardData,
        Action<KTH_HandCardView> onClicked)
    {
        _data = cardData;
        _onClicked = onClicked;

        if (_data == null)
        {
            Debug.LogWarning(
                $"[KTH_HandCardView] {name}: 카드 데이터가 비어 있습니다.",
                this
            );
        }
        else if (iconImage != null)
        {
            iconImage.sprite = _data.Image;
        }

        _isSelected = false;
        _yOffset = 0f;
        _isOffsetSettled = true;

        if (selectionOutline != null)
            selectionOutline.SetActive(false);

        if (_canvas != null)
        {
            _canvas.overrideSorting = false;
            _canvas.sortingOrder = 0;
        }

        UpdateCardFace();
    }

    public void SnapToBasePosition(
        Vector2 position,
        Quaternion rotation)
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
        SnapToBasePosition(
            position,
            Quaternion.identity
        );
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;

        if (selectionOutline != null)
            selectionOutline.SetActive(selected);

        if (_canvas != null)
        {
            _canvas.overrideSorting = selected;

            _canvas.sortingOrder =
                selected
                    ? selectedSortingOrder
                    : 0;
        }

        // ⭐ 선택된 카드를 UI 계층의 가장 앞으로 이동
        if (selected)
        {
            transform.SetAsLastSibling();
        }

        // 선택/해제 시 내려갔다 올라오는 연출 유지
        _isOffsetSettled = false;
    }

    public void ResetSelectionOffset()
    {
        _isSelected = false;

        if (selectionOutline != null)
            selectionOutline.SetActive(false);

        if (_canvas != null)
        {
            _canvas.overrideSorting = false;
            _canvas.sortingOrder = 0;
        }

        _yOffset = 0f;

        transform.localRotation =
            _originRotation;

        _isOffsetSettled = true;

        ApplyPosition();
        UpdateCardFace();
    }

    /// <summary>
    /// 배치 취소 등으로 카드의 시각적 상태를 완전히 초기화한다.
    /// </summary>
    public void ResetCardVisualState()
    {
        _isSelected = false;

        if (selectionOutline != null)
            selectionOutline.SetActive(false);

        if (_canvas != null)
        {
            _canvas.overrideSorting = false;
            _canvas.sortingOrder = 0;
        }

        _yOffset = 0f;

        _originRotation = Quaternion.identity;

        transform.localRotation =
            Quaternion.identity;

        _isOffsetSettled = true;

        ApplyPosition();
        UpdateCardFace();
    }

}