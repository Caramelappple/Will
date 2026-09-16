using System;
using _Scripts.LDY;
using UnityEngine;
using UnityEngine.Events;

namespace _Scripts.LSO.UI.Turn
{
    /// <summary>
    /// 코스트를 다 써서 **더 낼 수 있는 카드가 없는지**를 지켜본다.
    ///
    /// ── "다 썼다"가 두 가지다 ─────────────────────────────────
    /// 1. 코스트가 0이다.
    /// 2. 코스트는 남았지만 손패에서 가장 싼 카드보다 적다.
    ///
    /// 둘째가 중요하다. 3코스트 카드만 손에 들고 2코스트가 남았으면 숫자는
    /// 남아 있어도 낼 수 있는 것은 없다. 플레이어는 그걸 한눈에 못 보고
    /// 카드를 하나씩 눌러보며 확인하게 된다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 손패가 비어 있을 때도 맞는 것으로 본다. 낼 것이 없는 것은 같다.
    ///
    /// **소환만의 이야기다.** 코스트가 1이라도 남아 있으면 이동과 공격은
    /// 아직 할 수 있다. 이 조건만으로 "할 일이 다 끝났다"고 하면 안 된다.
    ///
    /// 씬 배선: 레버와 같은 오브젝트에 붙이면 된다. 참조는 비워두면 찾는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LSO_OutOfCost : MonoBehaviour, LSO_IReadyCondition
    {
        [Tooltip("남은 코스트를 물어볼 곳. 비워두면 LDY_ActionPointManager.instance 를 쓴다.")]
        [SerializeField] private LDY_ActionPointManager actionPoints;

        [Tooltip("손패. 비워두면 씬에서 찾는다.")]
        [SerializeField] private KTH_HandCardLayout handLayout;

        [Tooltip("몇 초마다 물어볼지. 0이면 매 프레임.\n" +
                 "\n" +
                 "코스트와 손패 양쪽이 바뀔 때마다 듣는 대신 사이를 두고 묻는다.\n" +
                 "두 신호를 엮으면 어느 하나를 빠뜨렸을 때 표시만 조용히 어긋난다.\n" +
                 "손패는 여덟 장이 상한이라 그때그때 세도 값이 싸다.")]
        [SerializeField, Min(0f)] private float checkInterval = 0.1f;

        [Header("반응")]
        [Tooltip("더 낼 수 없게 됐을 때.")]
        [SerializeField] private UnityEvent onBecameOut;

        [Tooltip("다시 낼 수 있게 됐을 때. 위에서 켠 것을 여기서 되돌린다.")]
        [SerializeField] private UnityEvent onBecameAvailable;

        [Tooltip("바뀔 때마다. 인자는 '더 낼 수 없는지'다.\n" +
                 "GameObject.SetActive 를 그대로 걸 수 있다.")]
        [SerializeField] private UnityEvent<bool> onChanged;

        [Header("진단")]
        [Tooltip("켜면 바뀔 때마다 남은 코스트와 손패 최소 코스트를 찍는다.")]
        [SerializeField] private bool logSteps;

        /// <summary>더 낼 수 있는 카드가 없는지.</summary>
        public bool IsOut { get; private set; }

        public bool IsMet => IsOut;

        public event Action<bool> Changed;

        private float _nextCheckAt;

        private LDY_ActionPointManager Points =>
            actionPoints != null ? actionPoints : LDY_ActionPointManager.instance;

        private KTH_HandCardLayout Hand =>
            handLayout != null ? handLayout : KTH_HandCardLayout.Instance;

        private void Awake()
        {
            if (handLayout == null) handLayout = KTH_HandCardLayout.Instance;
            if (handLayout == null) handLayout = FindAnyObjectByType<KTH_HandCardLayout>();
        }

        private void OnEnable()
        {
            // 켜질 때 한 번 맞춰둔다. 안 그러면 값이 바뀌기 전까지 듣는 쪽이
            // 지난 상태로 남는다. 처음은 "아직 낼 수 있다"로 보고 알린다.
            IsOut = false;
            _nextCheckAt = 0f;

            Raise(false);
        }

        private void Update()
        {
            if (checkInterval > 0f)
            {
                if (Time.unscaledTime < _nextCheckAt) return;

                _nextCheckAt = Time.unscaledTime + checkInterval;
            }

            bool out_ = Evaluate();

            if (out_ == IsOut) return;

            IsOut = out_;

            Raise(out_);
        }

        /// <summary>
        /// 지금 더 낼 수 있는 것이 없는지.
        ///
        /// 물어볼 곳이 없으면 false 를 돌려준다 — 모르는 것을 "다 썼다"로
        /// 단정하면 표식이 엉뚱한 때에 뜬다.
        /// </summary>
        private bool Evaluate()
        {
            LDY_ActionPointManager points = Points;

            if (points == null) return false;

            // 1. 코스트가 아예 없다.
            if (points.Current <= 0) return true;

            KTH_HandCardLayout hand = Hand;

            if (hand == null) return false;

            int min = hand.MinCardCost;

            // 손패가 비었거나 쓸 수 있는 카드가 하나도 없다.
            if (min < 0) return true;

            // 2. 남은 코스트로는 가장 싼 카드도 못 낸다.
            return min > points.Current;
        }

        private void Raise(bool out_)
        {
            if (logSteps)
            {
                LDY_ActionPointManager points = Points;
                KTH_HandCardLayout hand = Hand;

                Debug.Log(
                    $"[{name}] {(out_ ? "더 낼 수 없음" : "아직 낼 수 있음")} — " +
                    $"남은 코스트 {(points != null ? points.Current.ToString() : "?")} · " +
                    $"손패 최소 코스트 {(hand != null ? hand.MinCardCost.ToString() : "?")}",
                    this);
            }

            onChanged?.Invoke(out_);

            if (out_) onBecameOut?.Invoke();
            else onBecameAvailable?.Invoke();

            Changed?.Invoke(out_);
        }
    }
}
