using System;
using _Scripts.LSO.Deck.Data;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 손패 카드 한 장의 표시를 담당한다.
///
/// 위치는 두 값의 합으로 정해진다.
///   BasePosition : 손패 어디에 놓일지. KTH_DeckManager가 트윈으로 움직인다.
///   yOffset      : 선택됐을 때 떠오르는 높이. 이 스크립트가 매 프레임 보간한다.
/// 둘을 분리해두지 않으면 매니저의 이동 연출과 서로 덮어써서 카드가 순간이동한다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class KTH_HandCardView : MonoBehaviour, IPointerClickHandler
{
    [Header("참조")]
    [SerializeField] private Image iconImage;          // Raycast Target이 켜져 있어야 클릭이 잡힌다
    [SerializeField] private GameObject selectionOutline;

    [Header("카드 앞/뒷면")]
    [Tooltip("앞면 요소를 묶은 자식. 비우면 iconImage를 직접 켜고 끈다.")]
    [SerializeField] private GameObject frontUI;
    [SerializeField] private GameObject backUI;

    [Header("선택 시 떠오르는 연출")]
    [Tooltip("선택됐을 때 위로 올라가는 높이(픽셀).")]
    [SerializeField] private float selectRiseHeight = 60f;

    [Tooltip("목표 높이까지 보간되는 속도. 클수록 빠르다.")]
    [SerializeField] private float selectMoveSpeed = 10f;

    private const float OffsetSnapThreshold = 0.05f;

    private LSO_CardSO _data;
    private Action<KTH_HandCardView> _onClicked;
    private RectTransform _rectTransform;

    private Vector2 _basePosition;
    private float _yOffset;
    private bool _isSelected;
    private bool _isOffsetSettled = true;
    private float _lastCheckedYAngle = float.NaN;

    public LSO_CardSO Data => _data;

    /// <summary>
    /// 손패에서의 기준 위치. 매니저가 이 값을 움직이면 선택 오프셋이 얹힌 채로 반영된다.
    /// </summary>
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

        if (frontUI == null)
        {
            Transform found = transform.Find("Front");
            if (found != null) frontUI = found.gameObject;
        }

        if (backUI == null)
        {
            Transform found = transform.Find("Back");
            if (found != null) backUI = found.gameObject;
        }
    }

    private void Update()
    {
        UpdateFacing();
        UpdateSelectionOffset();
    }

    /// <summary>
    /// Y축 회전이 뒤집힌 구간이면 뒷면을 보여준다.
    /// 회전은 드로우 연출 때만 일어나므로, 각도가 변했을 때만 검사해서 평소에는 아무 일도 하지 않는다.
    /// </summary>
    private void UpdateFacing()
    {
        float yAngle = _rectTransform.localEulerAngles.y;
        if (Mathf.Approximately(yAngle, _lastCheckedYAngle)) return;

        _lastCheckedYAngle = yAngle;

        bool showBack = yAngle > 90f && yAngle < 270f;
        SetFrontActive(!showBack);
        if (backUI && backUI.activeSelf != showBack) backUI.SetActive(showBack);
    }

    /// <summary>
    /// 선택 오프셋을 목표값으로 보간한다.
    /// 목표에 도달하면 스냅하고 멈춰서, 가만히 있는 카드가 매 프레임 위치를 다시 쓰지 않게 한다.
    /// </summary>
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

    /// <param name="onClicked">클릭됐을 때 알릴 대상. 뷰는 누가 듣는지 알 필요가 없다.</param>
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
    }

    /// <summary>연출 없이 즉시 기준 위치로 옮긴다. 카드를 처음 만들 때 쓴다.</summary>
    public void SnapToBasePosition(Vector2 position)
    {
        _yOffset = 0f;
        _isOffsetSettled = true;
        BasePosition = position;
    }

    public void SetSelected(bool selected)
    {
        if (_isSelected == selected) return;

        _isSelected = selected;
        _isOffsetSettled = false;   // 보간을 다시 시작한다
        if (selectionOutline) selectionOutline.SetActive(selected);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _onClicked?.Invoke(this);
    }
}
