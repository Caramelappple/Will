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

    private KTH_CardData data;
    private KTH_DeckManager manager;

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

        SetSelected(false);
    }

    public KTH_CardData GetData() => data;

    public void SetSelected(bool selected)
    {
        if (selectionOutline) selectionOutline.SetActive(selected);
    }

    private void OnMouseDown()
    {
        Debug.Log("카드 클릭됨: " + gameObject.name);
        if (manager != null) manager.SelectCard(this);
    }
}