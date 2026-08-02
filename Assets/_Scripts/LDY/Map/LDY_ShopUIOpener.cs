using System.Collections;
using UnityEngine;

// 초록별(Shop 노드) 클릭 -> openDelay(기본 0.1초) 뒤에 shopUI.SetActive(true), 나가기 버튼 ->
// shopUI.SetActive(false). 상점 진열 등 다른 시스템은 shopUI 오브젝트 쪽에서 SetActive(true)를
// 감지해 알아서 처리하면 되고, 이 스크립트는 on/off 하나만 항상 확실하게 동작하는 걸 목적으로 한다.
public class LDY_ShopUIOpener : MonoBehaviour
{
    [SerializeField] private GameObject shopUI;
    [SerializeField] private float openDelay = 0.1f;

    private void Start()
    {
        if (shopUI != null) shopUI.SetActive(false);

        if (LDY_MapManager.Instance != null)
            LDY_MapManager.Instance.onShopNodeSelected.AddListener(HandleShopNodeSelected);
    }

    private void OnDestroy()
    {
        if (LDY_MapManager.Instance != null)
            LDY_MapManager.Instance.onShopNodeSelected.RemoveListener(HandleShopNodeSelected);
    }

    private void HandleShopNodeSelected(LDY_MapNode node)
    {
        StartCoroutine(OpenAfterDelay());
    }

    private IEnumerator OpenAfterDelay()
    {
        if (openDelay > 0f) yield return new WaitForSeconds(openDelay);

        if (shopUI != null) shopUI.SetActive(true);
    }

    // 나가기 버튼의 OnClick()에 이 메서드를 직접 연결해서 쓰면 됨.
    public void Close()
    {
        if (shopUI != null) shopUI.SetActive(false);
    }
}
