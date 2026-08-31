using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Scripts.LSO.UI.Input
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

        /// <summary>지금 커서가 올라가 있는지. 밖에서 상태를 볼 때 쓴다.</summary>
        public bool IsHovered { get; private set; }

        private void Awake()
        {
            Rescan();

            _selectable = GetComponent<Selectable>();
        }

        /// <summary>
        /// 붙어 있는 연출을 다시 모은다.
        ///
        /// 런타임에 연출을 붙였다면 반드시 불러야 한다. RequireComponent 때문에
        /// 연출을 AddComponent하면 이 핸들러가 먼저 붙어 Awake를 돌리는데,
        /// 그 시점에는 연출이 아직 없어서 빈 배열을 잡고 영영 반응하지 않는다.
        ///
        /// 그 실패는 아무 소리도 내지 않는다 — 그냥 호버가 안 될 뿐이라
        /// 배선 문제인지 연출 문제인지 알 수가 없다. 그래서 다시 부를 길을 열어둔다.
        /// </summary>
        public void Rescan()
        {
            _effects = GetComponents<LSO_IHoverEffect>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!CanPlay()) return;

            IsHovered = true;

            foreach (var effect in _effects)
                effect.OnHoverEnter();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!CanPlay()) return;

            SendExit();
        }

        /// <summary>
        /// 커서가 올라간 채로 꺼지면 OnPointerExit이 오지 않는다.
        ///
        /// 게이트가 이 컴포넌트를 껐을 때가 그렇다. 그대로 두면 올라간 물건이 떠오른 채로,
        /// 커서 요청이 걸린 채로 굳는다. 꺼지기 전에 이탈을 한 번 보내 정리한다.
        /// </summary>
        private void OnDisable()
        {
            SendExit();
        }

        private void SendExit()
        {
            if (!IsHovered) return;

            IsHovered = false;

            if (_effects == null) return;

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
