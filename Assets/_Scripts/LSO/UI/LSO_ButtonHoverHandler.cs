using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Scripts.LSO.UI
{
    /// <summary>
    /// uGUI 포인터 진입/이탈만 감지해서 자신에게 붙은 LSO_IHoverEffect들에게 전달한다.
    /// 어떤 연출인지는 알지 못한다.
    /// </summary>
    [DisallowMultipleComponent]
    public class LSO_ButtonHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("Selectable이 있을 때 interactable이 false면 연출을 재생하지 않는다.")]
        [SerializeField] private bool respectInteractable = true;

        private LSO_IHoverEffect[] _effects;
        private Selectable _selectable;

        private void Awake()
        {
            _effects = GetComponents<LSO_IHoverEffect>();
            _selectable = GetComponent<Selectable>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!CanPlay()) return;

            foreach (var effect in _effects)
                effect.OnHoverEnter();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!CanPlay()) return;

            foreach (var effect in _effects)
                effect.OnHoverExit();
        }

        private bool CanPlay()
        {
            if (_effects == null || _effects.Length == 0) return false;
            if (respectInteractable && _selectable != null && !_selectable.interactable) return false;

            return true;
        }
    }
}
