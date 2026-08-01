using TMPro;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class KTH_HandCardView : MonoBehaviour
{
    [Header("참조")]
    public SpriteRenderer iconRenderer;
    public GameObject selectionOutline; // 선택됐을 때만 활성화

    [Header("카드 앞/뒷면 설정")]
    public GameObject frontUI; // 앞면 요소를 하나로 묶은 자식 오브젝트 (없으면 iconRenderer 자동 제어)
    public GameObject backUI;  // 카드 뒷면 스프라이트/오브젝트

    [Header("선택 시 위로 올라오는 연출")]
    [Tooltip("선택됐을 때 카드가 위로 떠오르는 높이")]
    public float selectRiseHeight = 0.6f;

    [Tooltip("목표 높이까지 보간되는 속도 (클수록 빠르게 움직임)")]
    public float selectMoveSpeed = 10f;

    private KTH_CardData data;
    private KTH_DeckManager manager;

    private bool isSelected = false;
    private float currentYOffset = 0f; // ★ 매니저가 관리하는 X와는 별개로, 이 값만 독립적으로 Y를 제어

    private void Awake()
    {
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
        // Y축 회전각 체크 (0 ~ 360도)
        float yAngle = transform.localEulerAngles.y;

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
        // KTH_DeckManager가 이 transform의 X 위치(및 등장/재정렬 애니메이션)를 DOTween으로 직접 제어하므로,
        // 여기서 또 DOTween으로 같은 transform을 움직이면 매니저 쪽 DOKill()에 의해 트윈이 끊겨
        // 카드가 중간에 멈추는 문제가 재발할 수 있습니다.
        // 그래서 DOTween을 쓰지 않고 매 프레임 Y값만 별도로 보간해서 매니저의 트윈과 절대 충돌하지 않게 합니다.
        // (매니저는 손패 카드의 목표 Y를 항상 0으로 두므로, 여기서 Y 채널을 독점해도 안전합니다.)
        float targetYOffset = isSelected ? selectRiseHeight : 0f;
        currentYOffset = Mathf.Lerp(currentYOffset, targetYOffset, Time.deltaTime * selectMoveSpeed);

        Vector3 pos = transform.localPosition;
        transform.localPosition = new Vector3(pos.x, currentYOffset, pos.z);
    }

    /// <summary>앞면 요소 활성화/비활성화 제어</summary>
    private void SetFrontActive(bool active)
    {
        if (frontUI != null)
        {
            if (frontUI.activeSelf != active) frontUI.SetActive(active);
        }
        else if (iconRenderer != null)
        {
            // frontUI 묶음이 따로 없다면 iconRenderer를 직접 제어
            if (iconRenderer.enabled != active) iconRenderer.enabled = active;
        }
    }

    public void Setup(KTH_CardData cardData, KTH_DeckManager deckManager)
    {
        data = cardData;
        manager = deckManager;

        if (iconRenderer) iconRenderer.sprite = cardData.icon;

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

    private void OnMouseDown()
    {
        Debug.Log("카드 클릭됨: " + gameObject.name);
        if (manager != null) manager.SelectCard(this);
    }
}