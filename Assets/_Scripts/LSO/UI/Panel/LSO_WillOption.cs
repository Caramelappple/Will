using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using _Scripts.LSO.Will;

namespace _Scripts.LSO.UI.Panel
{
    [RequireComponent(typeof(Button))]
    public class LSO_WillOption : MonoBehaviour
    {
        [Tooltip("유언 이름을 찍을 곳. 비워두면 자식에서 찾는다.")]
        [SerializeField] private TMP_Text nameText;

        [Tooltip("비워두면 같은 오브젝트의 Button을 쓴다.")]
        [SerializeField] private Button button;

        private LSO_WillType _willType;
        private Action<LSO_WillType> _onClicked;

        public LSO_WillType WillType => _willType;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (nameText == null)
                nameText = GetComponentInChildren<TMP_Text>(true);

            // 리스너는 여기서 한 번만 건다.
            // Init에서 걸면 다시 쓸 때마다 중복으로 쌓여 한 번 눌러도 여러 번 발동한다.
            button.onClick.AddListener(HandleClicked);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClicked);
        }

        /// <summary>
        /// 이 선택지가 무엇을 뜻하는지 정한다.
        /// </summary>
        /// <param name="willType">이 버튼이 고르는 유언.</param>
        /// <param name="onClicked">눌렸을 때 알릴 곳. 뷰는 누가 듣는지 알 필요가 없다.</param>
        public void Init(LSO_WillType willType, Action<LSO_WillType> onClicked)
        {
            _willType = willType;
            _onClicked = onClicked;

            if (nameText != null)
                nameText.text = willType.ToString();
        }

        /// <summary>고를 수 있는지. 해금되지 않은 유언을 회색으로 보여줄 때 쓴다.</summary>
        public void SetInteractable(bool value)
        {
            if (button != null)
                button.interactable = value;
        }

        private void HandleClicked()
        {
            _onClicked?.Invoke(_willType);
        }
    }
}