using System;
using UnityEngine;

namespace _Scripts.LSO.UI.Panel
{
    /// <summary>
    /// SetActive로 여닫는 기본 창.
    /// 창의 루트 오브젝트에 붙인다.
    ///
    /// 스스로를 끄더라도 다른 오브젝트가 이 컴포넌트를 참조하고 있으면 다시 열 수 있다.
    /// (비활성 오브젝트의 컴포넌트도 참조는 살아 있다)
    /// </summary>
    public class LSO_Panel : MonoBehaviour, LSO_IPanel
    {
        [Tooltip("시작할 때 이 상태로 맞춘다. 씬에 열린 채로 저장해두고도 게임 시작 시 닫히게 할 수 있다.")]
        [SerializeField] private bool openOnStart;

        [Tooltip("Awake에서 openOnStart를 적용할지. 끄면 씬에 저장된 상태를 그대로 쓴다.")]
        [SerializeField] private bool applyOnStart = true;

        /// <summary>열렸을 때. 열려 있는데 또 Open을 불러도 발생하지 않는다.</summary>
        public event Action<LSO_Panel> Opened;

        /// <summary>닫혔을 때.</summary>
        public event Action<LSO_Panel> Closed;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            if (applyOnStart)
                SetOpen(openOnStart);
        }

        public void Open()
        {
            if (IsOpen) return;

            gameObject.SetActive(true);
            Opened?.Invoke(this);
        }

        public void Close()
        {
            if (!IsOpen) return;

            gameObject.SetActive(false);
            Closed?.Invoke(this);
        }

        /// <summary>토글이 필요한 곳에서 쓴다. 버튼 OnClick에 직접 연결해도 된다.</summary>
        public void SetOpen(bool open)
        {
            if (open) Open();
            else Close();
        }

        public void Toggle()
        {
            SetOpen(!IsOpen);
        }
    }
}
