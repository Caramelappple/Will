using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.LSO.UI
{
    /// <summary>
    /// 버튼을 누르면 자신의 창을 닫고 다른 창을 연다.
    ///
    /// closeSelf를 끄면 자신의 창을 열어둔 채로 대상 창만 띄운다.
    /// 설정창처럼 메인 화면 위에 겹쳐 띄우고 싶을 때 쓴다.
    ///
    /// 창에 LSO_Panel이 붙어 있으면 그쪽에 여닫기를 맡기고, 없으면 SetActive로 처리한다.
    /// 나중에 페이드 연출을 넣더라도 이 스크립트는 고칠 필요가 없다.
    /// </summary>
    public class LSO_PanelSwitchButton : MonoBehaviour
    {
        [Tooltip("이 버튼이 속한 창. closeSelf가 켜져 있을 때 닫는다.")]
        [SerializeField] private GameObject selfPanel;

        [Tooltip("열 창.")]
        [SerializeField] private GameObject targetPanel;

        [Tooltip("끄면 자신의 창을 닫지 않는다. 창을 겹쳐 띄울 때 사용.")]
        [SerializeField] private bool closeSelf = true;

        [Tooltip("비워두면 같은 오브젝트의 Button을 찾는다.")]
        [SerializeField] private Button button;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (button == null)
                Debug.LogWarning($"{name}: Button이 없습니다. Switch()를 직접 호출해야 합니다.", this);

            if (closeSelf && selfPanel == null)
                Debug.LogWarning($"{name}: closeSelf가 켜져 있는데 selfPanel이 비어 있습니다.", this);

            if (targetPanel != null && selfPanel == targetPanel)
                Debug.LogWarning($"{name}: selfPanel과 targetPanel이 같습니다.", this);
        }

        private void OnEnable()
        {
            if (button != null)
                button.onClick.AddListener(Switch);
        }

        private void OnDisable()
        {
            if (button != null)
                button.onClick.RemoveListener(Switch);
        }

        /// <summary>버튼 OnClick에 직접 연결해서 써도 된다.</summary>
        public void Switch()
        {
            if (targetPanel == null)
            {
                Debug.LogWarning($"{name}: targetPanel이 비어 있습니다.", this);
                return;
            }

            // 여는 쪽이 먼저다.
            // 자신을 먼저 닫으면 대상이 자신의 자식일 때 같이 꺼지고,
            // 아무 창도 없는 한 프레임이 생길 수 있다.
            LSO_PanelOps.Open(targetPanel);

            if (closeSelf)
                LSO_PanelOps.Close(selfPanel);
        }

        /// <summary>대상 창만 닫는다. 뒤로가기 버튼에 연결하면 된다.</summary>
        public void CloseTarget()
        {
            LSO_PanelOps.Close(targetPanel);
        }
    }
}
