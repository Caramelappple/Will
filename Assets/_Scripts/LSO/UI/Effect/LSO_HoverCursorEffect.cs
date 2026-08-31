using _Scripts.LSO.UI.Input;
using UnityEngine;

namespace _Scripts.LSO.UI.Effect
{
    /// <summary>
    /// 커서가 올라가 있는 동안 커서 모양을 바꾼다.
    ///
    /// 모양을 직접 바꾸지 않고 LSO_CursorManager에 요청만 한다.
    /// 겹친 물건들이 각자 Cursor.SetCursor를 부르면 벗어나는 순서에 따라
    /// 아직 다른 것 위인데도 기본 커서로 돌아가버린다.
    ///
    /// 지금 누를 수 있는지는 LSO_ButtonClickHandler의 enabled로 판단한다.
    /// LSO_TurnClickGate가 막을 때 끄는 것이 바로 그 값이라, 게이트를 따로 볼 필요가 없다.
    /// 게이트를 직접 참조하면 "막는 방법"이 늘 때마다 여기도 고쳐야 한다.
    ///
    /// 씬 배선: LSO_ButtonHoverHandler 와 함께 붙일 것.
    /// </summary>
    [RequireComponent(typeof(LSO_ButtonHoverHandler))]
    public class LSO_HoverCursorEffect : MonoBehaviour, LSO_IHoverEffect
    {
        [Header("모양")]
        [Tooltip("누를 수 있을 때 쓸 모양.")]
        [SerializeField] private LSO_CursorState interactableState = LSO_CursorState.Interactable;

        [Tooltip("막혀 있을 때 쓸 모양.\n" +
                 "Default로 두면 막힌 동안 아무 표시도 하지 않는다.")]
        [SerializeField] private LSO_CursorState blockedState = LSO_CursorState.Blocked;

        [Header("막힘 판정")]
        [Tooltip("켜면 클릭 핸들러가 꺼져 있을 때 Blocked 모양으로 바꾼다.\n" +
                 "LSO_TurnClickGate가 그 핸들러를 껐다 켜므로 게이트를 따라가게 된다.\n" +
                 "\n" +
                 "끄면 언제나 Interactable 모양이다. 애초에 막힐 일이 없는 물건에 쓴다.")]
        [SerializeField] private bool followClickGate = true;

        [Tooltip("막힘 판정에 쓸 핸들러. 비워두면 같은 오브젝트에서 찾는다.")]
        [SerializeField] private LSO_ButtonClickHandler clickHandler;

        // 요청해둔 상태. 도중에 모양이 바뀌어도 짝이 맞게 이 값으로 무른다.
        private LSO_CursorState _requested;
        private bool _isRequesting;
        private bool _isHovering;

        private void Awake()
        {
            if (clickHandler == null) clickHandler = GetComponent<LSO_ButtonClickHandler>();
        }

        /// <summary>
        /// 막힘 판정을 따라갈지 코드로 정한다. 런타임에 붙일 때 쓴다.
        ///
        /// 클릭 판정이 아예 없는 물건에 붙일 때 끄면 된다.
        /// </summary>
        public void Configure(bool follow)
        {
            followClickGate = follow;
        }

        /// <summary>
        /// 경고는 Awake가 아니라 Start에서 낸다.
        ///
        /// AddComponent는 그 자리에서 Awake를 돌리므로, 붙인 쪽이 Configure로
        /// 판정을 끄기도 전에 경고가 나가버린다. Start면 배선이 끝난 뒤다.
        /// </summary>
        private void Start()
        {
            if (followClickGate && clickHandler == null)
            {
                Debug.LogWarning(
                    $"{name}: 클릭 핸들러가 없어 막힘을 판단할 수 없습니다. 항상 눌리는 것으로 봅니다.", this);
            }
        }

        public void OnHoverEnter()
        {
            _isHovering = true;

            ApplyCurrent();
        }

        public void OnHoverExit()
        {
            _isHovering = false;

            ReleaseIfNeeded();
        }

        /// <summary>
        /// 올라가 있는 동안만 돈다.
        ///
        /// 게이트는 매 프레임 판단을 다시 하므로, 커서를 올려둔 채로 적 턴이 되거나
        /// 연출이 시작될 수 있다. 그때 모양이 그대로면 못 누르는데 누를 수 있어 보인다.
        /// </summary>
        private void Update()
        {
            if (!_isHovering) return;
            if (!followClickGate) return;

            ApplyCurrent();
        }

        /// <summary>
        /// 커서가 올라간 채로 오브젝트가 꺼지면 OnHoverExit이 오지 않는다.
        /// 그대로 두면 요청이 남아 커서가 영영 안 돌아온다.
        /// </summary>
        private void OnDisable()
        {
            _isHovering = false;

            ReleaseIfNeeded();
        }

        private void ApplyCurrent()
        {
            LSO_CursorState next = CanClick ? interactableState : blockedState;

            if (_isRequesting && _requested == next) return;

            ReleaseIfNeeded();

            _requested = next;
            _isRequesting = true;

            LSO_CursorManager.Request(_requested);
        }

        /// <summary>
        /// 지금 눌리는지.
        ///
        /// 핸들러가 없으면 눌리는 것으로 본다. 없다는 이유로 막아버리면
        /// 배선을 빠뜨렸을 때 영영 막힌 커서가 되어 원인을 찾기 어렵다.
        /// </summary>
        private bool CanClick =>
            !followClickGate || clickHandler == null || clickHandler.enabled;

        private void ReleaseIfNeeded()
        {
            if (!_isRequesting) return;

            _isRequesting = false;

            LSO_CursorManager.Release(_requested);
        }
    }
}
