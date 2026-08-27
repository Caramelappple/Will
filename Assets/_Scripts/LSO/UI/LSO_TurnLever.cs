using _Scripts.LDY;
using _Scripts.LSO.Manager;
using DG.Tweening;
using UnityEngine;

namespace _Scripts.LSO.UI
{
    /// <summary>
    /// 지금이 누구 턴인지를 두 물건의 높이로 보여주고, 눌러서 턴을 넘긴다.
    ///
    /// 지금 턴인 쪽이 올라가 있고 반대쪽이 내려가 있다.
    /// 플레이어 턴이면 플레이어 레버가 위, 적 레버가 아래다.
    ///
    /// 자리를 정하는 곳이 여기 하나뿐이다.
    /// 클릭은 "턴을 넘겨달라"고 요청만 하고, 실제로 내려갈지 올라갈지는
    /// LDY_TurnManager가 턴을 바꿔 알려준 뒤에 정해진다.
    ///
    /// 그래서 화면과 진짜 턴이 어긋날 수 없다. 적 턴에 눌리거나 연출 도중에 눌려도
    /// 턴 매니저가 거절하면 자리는 그대로 있는다.
    ///
    /// 씬 배선:
    ///   이 컴포넌트를 두 물건의 공통 부모에 붙이고
    ///   Player Side / Enemy Side 에 각각을 연결한다.
    ///   누를 수 있게 하려면 각 물건에 Collider + LSO_ButtonClickHandler + LSO_TurnLeverSide 를 붙인다.
    /// </summary>
    public class LSO_TurnLever : MonoBehaviour
    {
        [Header("양쪽")]
        [Tooltip("플레이어 턴에 올라가 있을 것. 적 턴에는 내려간다.")]
        [SerializeField] private Transform playerSide;

        [Tooltip("적 턴에 올라가 있을 것. 플레이어 턴에는 내려간다.")]
        [SerializeField] private Transform enemySide;

        [Header("움직임")]
        [Tooltip("내려갈 방향. 각 물건의 로컬 기준이다.")]
        [SerializeField] private Vector3 direction = Vector3.down;

        [Tooltip("얼마나 내려갈지. UI(RectTransform)는 픽셀 단위라 10~20이 필요하다.")]
        [SerializeField, Min(0f)] private float depth = 0.15f;

        [Header("연출")]
        [SerializeField, Min(0.01f)] private float downDuration = 0.18f;

        [SerializeField, Min(0.01f)] private float upDuration = 0.24f;

        [SerializeField] private Ease downEase = Ease.OutQuad;

        [SerializeField] private Ease upEase = Ease.OutBack;

        [Tooltip("켜면 일시정지 중에도 연출이 진행된다.")]
        [SerializeField] private bool ignoreTimeScale = true;

        [Header("반응")]
        [Tooltip("턴이 바뀔 때마다. 인자는 새로 시작된 턴이다.\n" +
                 "적 턴이 끝나 돌아올 때도 발행된다. 시작할 때는 발행하지 않는다.")]
        [SerializeField] private LSO_TurnEvent onTurnChanged;

        [Tooltip("이 레버를 눌러서 턴이 넘어갔을 때. 인자는 넘어간 뒤의 턴이다.\n" +
                 "레버를 당기는 소리처럼 '눌렀을 때만' 나야 하는 것을 여기 건다.")]
        [SerializeField] private LSO_TurnEvent onAccepted;

        [Tooltip("지금은 넘길 수 없을 때(적 턴, 연출 중). 인자는 거절 당시의 턴이다.")]
        [SerializeField] private LSO_TurnEvent onRejected;

        private LDY_TurnManager _turnManager;

        private Vector3 _playerRest;
        private Vector3 _enemyRest;

        private Tween _playerTween;
        private Tween _enemyTween;

        private void Awake()
        {
            if (playerSide == null || enemySide == null)
            {
                Debug.LogError($"{name}: Player Side / Enemy Side 를 둘 다 연결해야 합니다.", this);
                enabled = false;
                return;
            }

            // 인스펙터에서 잡아둔 자리가 기준이다.
            // 여기서 기록하지 않으면 한 번 내려간 자리가 기준이 되어 점점 파묻힌다.
            _playerRest = playerSide.localPosition;
            _enemyRest = enemySide.localPosition;
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

            KillTweens();

            // 움직이는 도중에 꺼지면 어중간한 자리에 굳는다.
            if (playerSide != null) playerSide.localPosition = _playerRest;
            if (enemySide != null) enemySide.localPosition = _enemyRest;
        }

        private void Bind(LDY_TurnManager turnManager)
        {
            if (_turnManager == turnManager) return;

            if (_turnManager != null)
                _turnManager.OnTurnChanged -= HandleTurnChanged;

            _turnManager = turnManager;

            if (_turnManager == null) return;

            _turnManager.OnTurnChanged += HandleTurnChanged;

            // LDY_TurnManager는 Start에서 첫 턴을 한 번 알린다.
            // 이쪽이 늦게 붙으면 그 한 번을 놓치므로, 붙자마자 현재 턴을 직접 읽는다.
            // 시작 자리는 연출 없이 잡는다. 화면에 들어오자마자 움직이면 눌린 것처럼 보인다.
            Apply(_turnManager.CurrentTurn, animate: false);
        }

        private void HandleTurnChanged(LDY_Team team)
        {
            Apply(team, animate: true);

            // 여기서만 발행한다. Bind에서 자리를 처음 맞출 때는 부르지 않는다.
            // 그건 "바뀐 것"이 아니라 "원래 그랬던 것"이라, 시작하자마자
            // 레버 소리가 나거나 연출이 도는 것을 막는다.
            onTurnChanged?.Invoke(team);
        }

        /// <summary>
        /// 지금 턴에 맞게 양쪽 높이를 정한다. 이 메서드 말고 자리를 바꾸는 곳은 없다.
        ///
        /// 지금 턴인 쪽이 올라가고 반대쪽이 내려간다.
        /// 올라간 것이 "지금 움직일 차례"라는 뜻이다.
        /// </summary>
        private void Apply(LDY_Team turn, bool animate)
        {
            bool playerUp = turn == LDY_Team.Player;

            Move(playerSide, ref _playerTween, _playerRest, down: !playerUp, animate);
            Move(enemySide, ref _enemyTween, _enemyRest, down: playerUp, animate);
        }

        private void Move(Transform side, ref Tween tween, Vector3 rest, bool down, bool animate)
        {
            if (side == null) return;

            tween?.Kill();
            tween = null;

            Vector3 destination = down ? rest + Offset : rest;

            if (!animate)
            {
                side.localPosition = destination;
                return;
            }

            tween = side
                .DOLocalMove(destination, down ? downDuration : upDuration)
                .SetEase(down ? downEase : upEase)
                .SetUpdate(ignoreTimeScale)
                .SetLink(gameObject);
        }

        private Vector3 Offset
        {
            get
            {
                if (direction.sqrMagnitude <= 0f) return Vector3.zero;

                return direction.normalized * depth;
            }
        }

        /// <summary>
        /// 어느 쪽을 눌렀든 여기로 모인다. 턴을 넘겨달라고 요청만 한다.
        ///
        /// 여기서 자리를 직접 바꾸지 않는 것이 핵심이다.
        /// 바꿔버리면 턴 매니저가 거절했을 때 화면만 넘어간 것처럼 보인다.
        /// </summary>
        public void RequestEndTurn()
        {
            if (_turnManager == null)
            {
                Debug.LogWarning($"{name}: 턴 매니저가 없어 턴을 넘길 수 없습니다.", this);

                // 턴을 알 수 없으니 플레이어 턴으로 친다. 어차피 거절 연출을 고르는 데만 쓰인다.
                onRejected?.Invoke(LDY_Team.Player);
                return;
            }

            if (!_turnManager.CanEndPlayerTurn())
            {
                // 적 턴이거나 연출 중이다. 아무 반응이 없으면 고장으로 보이므로 알린다.
                onRejected?.Invoke(_turnManager.CurrentTurn);
                return;
            }

            // EndPlayerTurn 안에서 턴이 바뀌고 OnTurnChanged가 곧바로 발행된다.
            // 그래서 onTurnChanged가 onAccepted보다 먼저 불린다.
            _turnManager.EndPlayerTurn();

            onAccepted?.Invoke(_turnManager.CurrentTurn);
        }

        private void KillTweens()
        {
            _playerTween?.Kill();
            _playerTween = null;

            _enemyTween?.Kill();
            _enemyTween = null;
        }
    }
}
