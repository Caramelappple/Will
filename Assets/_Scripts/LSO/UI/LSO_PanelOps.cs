using UnityEngine;

namespace _Scripts.LSO.UI
{
    /// <summary>
    /// GameObject를 창으로 보고 여닫는다.
    ///
    /// LSO_IPanel이 붙어 있으면 그쪽에 맡기고(페이드 등 연출이 살아난다),
    /// 없으면 SetActive로 처리한다. 그래서 아무 GameObject나 넘겨도 동작한다.
    ///
    /// 인터페이스는 인스펙터에 직렬화할 수 없어서 GameObject로 받고 여기서 풀어낸다.
    /// 이 변환을 여러 곳에서 되풀이하지 않으려고 한 곳에 모아뒀다.
    /// </summary>
    public static class LSO_PanelOps
    {
        public static void Open(GameObject panel) => SetOpen(panel, true);

        public static void Close(GameObject panel) => SetOpen(panel, false);

        public static void SetOpen(GameObject panel, bool open)
        {
            if (panel == null) return;

            // 비활성 오브젝트여도 컴포넌트 참조는 살아 있다.
            LSO_IPanel handler = panel.GetComponent<LSO_IPanel>();

            if (handler != null)
            {
                if (open) handler.Open();
                else handler.Close();
                return;
            }

            if (panel.activeSelf != open)
                panel.SetActive(open);
        }

        public static void Toggle(GameObject panel)
        {
            if (panel == null) return;

            LSO_IPanel handler = panel.GetComponent<LSO_IPanel>();

            SetOpen(panel, handler != null ? !handler.IsOpen : !panel.activeSelf);
        }
    }
}
