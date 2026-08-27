using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LSO.Manager;
using _Scripts.LSO.Will;
using UnityEngine;

namespace _Scripts.LSO.UI
{
    /// <summary>
    /// 정해둔 상황에서는 클릭을 받지 않는다. 여닫는 것 외의 책임은 갖지 않는다.
    ///
    /// 적 턴에 물건을 눌러 무언가가 일어나면 화면과 진행이 어긋난다.
    /// 기물을 고른 채로 다른 것을 누르는 것도 마찬가지다.
    ///
    /// 각 클릭 대상이 저마다 "지금 눌러도 되나"를 확인하게 두면 그 검사가 흩어지고
    /// 하나만 빠뜨려도 그것만 눌린다. 그래서 문 하나로 모아 막는다.
    ///
    /// 막는 대상은 LSO_ButtonClickHandler다. 콜라이더는 그대로 두므로
    /// 뒤에 있는 것이 대신 눌리는 일이 없다 — 눌러도 아무 일이 안 일어날 뿐이다.
    ///
    /// 씬 배선: Collider + LSO_ButtonClickHandler 가 있는 오브젝트에 같이 붙일 것.
    /// </summary>
    [RequireComponent(typeof(LSO_ButtonClickHandler))]
    [DisallowMultipleComponent]
    public class LSO_TurnClickGate : MonoBehaviour
    {
        [Header("막을 상황")]
        [Tooltip("여기 담긴 것 중 하나라도 해당하면 클릭을 받지 않는다.\n" +
                 "비워두면 언제나 눌린다.")]
        [SerializeField] private List<LSO_ClickBlockCondition> blockWhen = new List<LSO_ClickBlockCondition>
        {
            LSO_ClickBlockCondition.NotMyTurn,
            LSO_ClickBlockCondition.Animating
        };

        [Tooltip("Not My Turn 이 어느 턴을 뜻하는지. 이 턴이 아니면 막는다.")]
        [SerializeField] private LDY_Team allowedTurn = LDY_Team.Player;

        [Header("연결 (비우면 씬에서 찾는다)")]
        [Tooltip("Piece Selected 를 쓸 때만 필요하다.")]
        [SerializeField] private LDY_SelectionController selection;

        [Tooltip("Card Placing 을 쓸 때만 필요하다.")]
        [SerializeField] private LDY_CardPlacer cardPlacer;

        [Header("기타")]
        [Tooltip("켜면 콜라이더까지 끈다. 그러면 뒤에 있는 것이 대신 눌린다.\n" +
                 "보통은 꺼둔다 — 눌러도 아무 일이 없는 편이 예측 가능하다.")]
        [SerializeField] private bool alsoDisableCollider;

        private LSO_ButtonClickHandler _handler;
        private Collider[] _colliders;
        private LDY_TurnManager _turnManager;

        private void Awake()
        {
            _handler = GetComponent<LSO_ButtonClickHandler>();

            _colliders = alsoDisableCollider
                ? GetComponents<Collider>()
                : System.Array.Empty<Collider>();
        }

        private void Start()
        {
            // Awake가 아니라 Start다. 찾을 대상들이 자기 Awake를 마친 뒤여야 한다.
            // 쓰지 않는 조건까지 씬을 뒤지지는 않는다.
            if (selection == null && blockWhen.Contains(LSO_ClickBlockCondition.PieceSelected))
                selection = FindAnyObjectByType<LDY_SelectionController>();

            if (cardPlacer == null && blockWhen.Contains(LSO_ClickBlockCondition.CardPlacing))
                cardPlacer = FindAnyObjectByType<LDY_CardPlacer>();

            Refresh();
        }

        private void OnEnable()
        {
            // 전투 씬마다 턴 매니저가 새로 생긴다. 직접 참조로 물고 있으면 씬을 넘길 때 끊긴다.
            GameManager.Instance.TurnManagerChanged += Bind;

            Bind(GameManager.Instance.TurnManager);
        }

        private void OnDisable()
        {
            if (GameManager.HasInstance)
                GameManager.Instance.TurnManagerChanged -= Bind;

            Bind(null);

            // 꺼질 때는 열어둔 채로 남긴다. 닫힌 채로 굳으면
            // 다시 켰을 때 왜 안 눌리는지 알 수 없다.
            SetOpen(true);
        }

        private void Bind(LDY_TurnManager turnManager)
        {
            if (_turnManager == turnManager) return;

            _turnManager = turnManager;

            Refresh();
        }

        /// <summary>
        /// 매 프레임 확인한다.
        ///
        /// 턴은 이벤트로 알려주지만 나머지 넷은 그런 것이 없다.
        /// 연출 중인지, 기물을 골랐는지는 물어봐야만 알 수 있어서 여기서 본다.
        ///
        /// 막을 상황을 하나도 안 걸어뒀으면 아예 돌지 않는다.
        /// </summary>
        private void Update()
        {
            if (blockWhen.Count == 0) return;

            Refresh();
        }

        private void Refresh()
        {
            SetOpen(!IsBlocked());
        }

        private bool IsBlocked()
        {
            foreach (LSO_ClickBlockCondition condition in blockWhen)
            {
                if (Matches(condition)) return true;
            }

            return false;
        }

        /// <summary>
        /// 조건 하나가 지금 해당하는지.
        ///
        /// 볼 대상이 없으면 막지 않는다. 없다는 이유로 막아버리면
        /// 배선을 빠뜨렸을 때 영영 안 눌리는 상태가 되어 원인을 찾기 어렵다.
        /// </summary>
        private bool Matches(LSO_ClickBlockCondition condition)
        {
            switch (condition)
            {
                case LSO_ClickBlockCondition.NotMyTurn:
                    return _turnManager != null && _turnManager.CurrentTurn != allowedTurn;

                case LSO_ClickBlockCondition.Animating:
                    return _turnManager != null && _turnManager.IsAnimating();

                case LSO_ClickBlockCondition.PieceSelected:
                    return selection != null && selection.Selected != null;

                case LSO_ClickBlockCondition.CardPlacing:
                    return cardPlacer != null && cardPlacer.IsPlacing;

                case LSO_ClickBlockCondition.WillSelecting:
                    return LSO_WillSelection.IsSelecting;

                case LSO_ClickBlockCondition.SuccessionWaiting:
                    return DLJ_SuccessionSystem.IsWaitingForSuccessionTarget;

                default:
                    return false;
            }
        }

        /// <summary>
        /// 문을 여닫는다.
        ///
        /// 값이 그대로면 건드리지 않는다. 매 프레임 같은 값을 대입해도 동작은 같지만,
        /// 컴포넌트를 껐다 켜면 OnEnable/OnDisable이 돌아 다른 것들이 함께 반응한다.
        /// </summary>
        private void SetOpen(bool open)
        {
            if (_handler != null && _handler.enabled != open)
                _handler.enabled = open;

            foreach (Collider item in _colliders)
            {
                if (item == null) continue;

                if (item.enabled != open)
                    item.enabled = open;
            }
        }
    }
}
