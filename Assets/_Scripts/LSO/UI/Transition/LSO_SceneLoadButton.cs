using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.LSO.UI.Transition
{
    /// <summary>
    /// 버튼을 누르면 씬을 바꾼다.
    /// 인스펙터에서 씬 이름만 적으면 되므로 메뉴 버튼에 바로 붙일 수 있다.
    /// </summary>
    public class LSO_SceneLoadButton : MonoBehaviour
    {
        [Tooltip("불러올 씬 이름. Build Settings에 등록돼 있어야 한다.")]
        [SerializeField] private string sceneName;

        [Tooltip("켜면 sceneName을 무시하고 현재 씬을 다시 불러온다.")]
        [SerializeField] private bool reloadCurrent;

        [Tooltip("비워두면 같은 오브젝트의 Button을 찾는다.")]
        [SerializeField] private Button button;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (button == null)
                Debug.LogWarning($"{name}: Button이 없습니다. Load()를 직접 호출해야 합니다.", this);

            if (!reloadCurrent && string.IsNullOrEmpty(sceneName))
                Debug.LogWarning($"{name}: 씬 이름이 비어 있습니다.", this);
        }

        private void OnEnable()
        {
            if (button != null)
                button.onClick.AddListener(Load);
        }

        private void OnDisable()
        {
            if (button != null)
                button.onClick.RemoveListener(Load);
        }

        /// <summary>버튼 OnClick에 직접 연결해도 된다.</summary>
        public void Load()
        {
            if (reloadCurrent)
                LSO_SceneLoader.Reload();
            else
                LSO_SceneLoader.Load(sceneName);
        }
    }
}
