using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Scripts.LSO.UI.Input
{
    /// <summary>
    /// uGUI 클릭만 감지해서 자신에게 붙은 LSO_IClickEffect들에게 전달한다.
    /// 어떤 연출인지는 알지 못하며, 붙은 연출이 여러 개면 모두 동시에 시작된다.
    /// </summary>
    [DisallowMultipleComponent]
    public class LSO_ButtonClickHandler : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("Selectable이 있을 때 interactable이 false면 연출을 재생하지 않는다.")]
        [SerializeField] private bool respectInteractable = true;

        [Tooltip("좌클릭에만 반응할지 여부. 끄면 우클릭/휠클릭에도 재생된다.")]
        [SerializeField] private bool leftButtonOnly = true;

        private LSO_IClickEffect[] _effects;
        private Selectable _selectable;

        private void Awake()
        {
            _effects = GetComponents<LSO_IClickEffect>();
            _selectable = GetComponent<Selectable>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (leftButtonOnly && eventData.button != PointerEventData.InputButton.Left) return;
            if (!CanPlay()) return;

            foreach (var effect in _effects)
                effect.OnClick();
        }

        private bool CanPlay()
        {
            if (_effects == null || _effects.Length == 0) return false;
            if (respectInteractable && _selectable != null && !_selectable.interactable) return false;

            return true;
        }
    }
}
