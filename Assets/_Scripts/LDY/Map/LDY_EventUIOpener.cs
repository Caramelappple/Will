using UnityEngine;

// 맵에서 노란별(Event 노드)에 들어가면 지정한 이벤트 UI 오브젝트를 SetActive(true)로 켠다.
// LDY_ShopUIOpener와 완전히 동일한 패턴 - eventUI에는 이벤트 처리 패널을 연결하면 됨.
public class LDY_EventUIOpener : MonoBehaviour
{
    [SerializeField] private GameObject eventUI;

    private void Start()
    {
        if (eventUI != null) eventUI.SetActive(false);

        if (LDY_MapManager.Instance != null)
            LDY_MapManager.Instance.onEventNodeSelected.AddListener(HandleEventNodeSelected);
    }

    private void OnDestroy()
    {
        if (LDY_MapManager.Instance != null)
            LDY_MapManager.Instance.onEventNodeSelected.RemoveListener(HandleEventNodeSelected);
    }

    private void HandleEventNodeSelected(LDY_MapNode node)
    {
        if (eventUI != null) eventUI.SetActive(true);
    }

    // 닫기 버튼의 OnClick()에 직접 연결해서 쓸 수도 있음.
    public void Close()
    {
        if (eventUI != null) eventUI.SetActive(false);
    }
}
