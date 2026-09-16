using System;
using _Scripts.LDY;
using UnityEngine;
using UnityEngine.Events;

namespace _Scripts.LSO.UI.Turn
{
    /// <summary>
    /// 지금 턴을 넘길 수 있는지를 **계속** 지켜보고, 값이 바뀔 때만 알린다.
    ///
    /// ── 왜 필요했나 ───────────────────────────────────────────
    /// LSO_TurnLever 는 누른 순간에만 CanEndPlayerTurn 을 묻는다. 안 되면
    /// 거부 신호를 낼 뿐이라, 플레이어는 **눌러보기 전까지 알 수 없다.**
    /// "턴 넘기기가 켜진 걸 알아차리기 힘들다"는 말이 그 이야기다.
    ///
    /// 누를 수 있는지를 아는 곳을 하나 두고, 발광·표식·소리가 전부 여기에
    /// 붙게 한다. 각자 CanEndPlayerTurn 을 묻게 하면 같은 판정이 여러 벌
    /// 생기고, 한 곳이 조건을 빠뜨렸을 때 그 표시만 어긋난다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 매 프레임 묻지 않는다. CanEndPlayerTurn 은 이동·공격 연출까지 훑으므로
    /// 사이를 두고 묻는다 — 사람 눈에는 차이가 없다.
    ///
    /// 씬 배선: 레버와 같은 오브젝트에 붙이면 된다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LSO_EndTurnReady : MonoBehaviour, LSO_IReadyCondition
    {
        [Tooltip("지켜볼 턴 매니저. 비워두면 씬에서 찾는다.")]
        [SerializeField] private LDY_TurnManager turnManager;

        [Tooltip("몇 초마다 물어볼지. 0이면 매 프레임.\n" +
                 "\n" +
                 "CanEndPlayerTurn 은 이동·공격 연출을 전부 훑는다. 매 프레임\n" +
                 "물을 이유가 없고, 0.1초면 눈에는 곧바로 바뀌는 것처럼 보인다.")]
        [SerializeField, Min(0f)] private float checkInterval = 0.1f;

        [Header("반응")]
        [Tooltip("누를 수 있게 됐을 때. 표식을 띄우거나 불을 켜는 것을 여기 건다.")]
        [SerializeField] private UnityEvent onBecameReady;

        [Tooltip("못 누르게 됐을 때. 위에서 켠 것을 여기서 되돌린다.")]
        [SerializeField] private UnityEvent onBecameBlocked;

        [Tooltip("바뀔 때마다. 인자는 지금 누를 수 있는지다.\n" +
                 "GameObject.SetActive 를 그대로 걸 수 있다.")]
        [SerializeField] private UnityEvent<bool> onChanged;

        /// <summary>지금 누를 수 있는지. 처음 판정이 돌기 전에는 false다.</summary>
        public bool IsReady { get; private set; }

        /// <summary>여러 조건을 묶는 쪽(LSO_ReadyConditions)이 보는 이름.</summary>
        public bool IsMet => IsReady;

        /// <summary>값이 바뀔 때만 발행된다. 인자는 지금 누를 수 있는지.</summary>
        public event Action<bool> Changed;

        private float _nextCheckAt;

        private void Awake()
        {
            if (turnManager == null) turnManager = FindAnyObjectByType<LDY_TurnManager>();

            if (turnManager == null)
            {
                Debug.LogWarning(
                    $"{name}: LDY_TurnManager 를 찾지 못해 턴 넘기기 표시가 켜지지 않습니다.",
                    this);

                enabled = false;
            }
        }

        /// <summary>
        /// 켜질 때 한 번 맞춰둔다.
        ///
        /// 안 그러면 켜진 직후부터 값이 바뀌기 전까지 표식이 지난 상태로 남는다.
        /// 처음 상태는 "못 누른다"로 보고 알리므로, 듣는 쪽이 꺼진 모습으로 시작한다.
        /// </summary>
        private void OnEnable()
        {
            IsReady = false;
            _nextCheckAt = 0f;

            Raise(false);
        }

        private void Update()
        {
            if (turnManager == null) return;

            if (checkInterval > 0f)
            {
                if (Time.unscaledTime < _nextCheckAt) return;

                _nextCheckAt = Time.unscaledTime + checkInterval;
            }

            bool ready = turnManager.CanEndPlayerTurn();

            if (ready == IsReady) return;

            IsReady = ready;

            Raise(ready);
        }

        private void Raise(bool ready)
        {
            onChanged?.Invoke(ready);

            if (ready) onBecameReady?.Invoke();
            else onBecameBlocked?.Invoke();

            Changed?.Invoke(ready);
        }
    }
}
