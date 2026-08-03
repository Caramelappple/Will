using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// ★ [UI 변환] BoxCollider2D 필요 없음 (UI 클릭은 EventSystem + Graphic Raycaster로 처리됨)
// ★ [UI 변환] IPointerClickHandler를 구현해서 OnMouseDown() 대신 UI 클릭 이벤트를 받음
[RequireComponent(typeof(RectTransform))]
public class KTH_HandCardView : MonoBehaviour, IPointerClickHandler
{
    [Header("참조")]
    public Image iconImage; // ★ SpriteRenderer → Image로 변경 (Raycast Target 켜져있어야 클릭 감지됨)
    public GameObject selectionOutline; // 선택됐을 때만 활성화

    [Header("카드 앞/뒷면 설정")]
    public GameObject frontUI; // 앞면 요소를 하나로 묶은 자식 오브젝트 (없으면 iconImage 자동 제어)
    public GameObject backUI;  // 카드 뒷면 이미지/오브젝트

    [Header("선택 시 위로 올라오는 연출")]
    [Tooltip("선택됐을 때 카드가 위로 떠오르는 높이 (UI는 픽셀 단위입니다. 기존 0.6f 같은 월드 단위값은 그대로 쓰면 거의 안 보이니 40~80 정도로 조정하세요)")]
    public float selectRiseHeight = 60f;

    [Tooltip("목표 높이까지 보간되는 속도 (클수록 빠르게 움직임)")]
    public float selectMoveSpeed = 10f;

    private KTH_CardData data;
    private KTH_DeckManager manager;
    private RectTransform rectTransform; // ★ Transform 대신 RectTransform을 캐싱해서 사용

    private bool isSelected = false;
    private float currentYOffset = 0f; // ★ 매니저가 관리하는 X와는 별개로, 이 값만 독립적으로 Y를 제어

    private void Awake()
    {
        rectTransform = (RectTransform)transform; // ★ UI 오브젝트는 항상 RectTransform을 가짐

        // Inspector 연결을 안 했을 경우 자식 오브젝트 이름으로 자동 탐색
        if (frontUI == null)
        {
            Transform frontTransform = transform.Find("Front");
            if (frontTransform != null) frontUI = frontTransform.gameObject;
        }

        if (backUI == null)
        {
            Transform backTransform = transform.Find("Back");
            if (backTransform != null) backUI = backTransform.gameObject;
        }
    }

    private void Update()
    {
        // Y축 회전각 체크 (0 ~ 360도) - RectTransform도 동일하게 localEulerAngles 사용 가능
        float yAngle = rectTransform.localEulerAngles.y;

        // 90도 ~ 270도 사이일 때 (뒷면이 카메라를 향할 때)
        if (yAngle > 90f && yAngle < 270f)
        {
            SetFrontActive(false);
            if (backUI && !backUI.activeSelf) backUI.SetActive(true);
        }
        else // 앞면이 카메라를 향할 때
        {
            SetFrontActive(true);
            if (backUI && backUI.activeSelf) backUI.SetActive(false);
        }

        // ★ 선택 시 위로 떠오르는 연출.
        // KTH_DeckManager가 이 RectTransform의 X 위치(및 등장/재정렬 애니메이션)를 DOTween(DOAnchorPos)으로
        // 직접 제어하므로, 여기서 또 DOTween으로 같은 RectTransform을 움직이면 매니저 쪽 DOKill()에 의해
        // 트윈이 끊겨 카드가 중간에 멈추는 문제가 재발할 수 있습니다.
        // 그래서 DOTween을 쓰지 않고 매 프레임 Y값만 별도로 보간해서 매니저의 트윈과 절대 충돌하지 않게 합니다.
        float targetYOffset = isSelected ? selectRiseHeight : 0f;
        currentYOffset = Mathf.Lerp(currentYOffset, targetYOffset, Time.deltaTime * selectMoveSpeed);

        Vector2 pos = rectTransform.anchoredPosition; // ★ localPosition → anchoredPosition
        rectTransform.anchoredPosition = new Vector2(pos.x, currentYOffset);
    }

    /// <summary>앞면 요소 활성화/비활성화 제어</summary>
    private void SetFrontActive(bool active)
    {
        if (frontUI != null)
        {
            if (frontUI.activeSelf != active) frontUI.SetActive(active);
        }
        else if (iconImage != null)
        {
            // frontUI 묶음이 따로 없다면 iconImage를 직접 제어
            if (iconImage.enabled != active) iconImage.enabled = active;
        }
    }

    public void Setup(KTH_CardData cardData, KTH_DeckManager deckManager)
    {
        data = cardData;
        manager = deckManager;

        if (iconImage) iconImage.sprite = cardData.icon;

        currentYOffset = 0f; // ★ 새 카드는 항상 들뜬 상태 없이 시작
        SetSelected(false);
    }

    public KTH_CardData GetData() => data;

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (selectionOutline) selectionOutline.SetActive(selected);
        // 실제 위로 떠오르는 이동은 Update()에서 currentYOffset 보간으로 처리됨
    }

    // ★ OnMouseDown() → OnPointerClick()으로 변경
    // 이 컴포넌트가 붙은 오브젝트(또는 자식)에 Raycast Target이 켜진 Graphic(Image 등)이 있어야
    // 클릭이 감지됩니다. 씬에 EventSystem 오브젝트와 Canvas에 Graphic Raycaster가 있는지도 확인하세요.
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("카드 클릭됨: " + gameObject.name);
        if (manager != null) manager.SelectCard(this);
    }
}