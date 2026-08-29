using System;
using _Scripts.LDY;
using _Scripts.LSO.Manager;
using UnityEngine;

namespace _Scripts.LSO.UI.Turn
{
    /// <summary>
    /// 시계 두 개를 각자의 턴에만 돌린다. 체스 시계와 같은 방식이다.
    ///
    /// 플레이어 턴에는 플레이어 시계만 돌고 적 시계는 멈춰 선다.
    /// 멈춘 시계는 되감기지 않고 그 자리에 그대로 있다가, 다시 자기 턴이 오면 이어서 돈다.
    /// 그래서 두 바늘의 벌어진 정도가 "누가 더 오래 생각했나"로 읽힌다.
    ///
    /// 바늘은 분침과 초침이다. 초침이 한 바퀴 돌면 분침이 한 칸 간다.
    ///
    /// 자리를 정하는 곳은 Tick 하나뿐이다.
    /// 바늘 각도는 흘러간 시간에서 매번 다시 계산하므로, 프레임을 건너뛰거나
    /// 도중에 창을 내려놓았다 돌아와도 어긋나지 않는다.
    ///
    /// 트윈을 쓰지 않는 이유가 이것이다. 트윈은 "지금부터 저기까지"를 재생하는 것이라
    /// 중간에 멈췄다 이어붙이면 오차가 쌓인다.
    ///
    /// 씬 배선: 씬 아무 곳에나 하나 붙이고 두 시계의 분침·초침을 연결할 것.
    /// </summary>
    public class LSO_TurnClock : MonoBehaviour
    {
        /// <summary>한 바퀴.</summary>
        private const float FullTurn = 360f;

        /// <summary>한 바퀴에 들어가는 눈금 수. 분침이 초침보다 이만큼 느리다.</summary>
        private const float NotchesPerTurn = 60f;

        /// <summary>
        /// 바늘 하나가 도는 방향.
        ///
        /// 분침과 초침을 따로 두는 이유는 모델링에 따라 두 바늘의 기준 각도가
        /// 다르게 잡히는 일이 흔하기 때문이다. 한쪽만 반대로 도는 것을
        /// 공통 설정으로는 고칠 수가 없다.
        /// </summary>
        [Serializable]
        public class HandAxis
        {
            [Tooltip("이 바늘이 도는 축. 바늘 자신의 로컬 축이다.\n" +
                     "씬 뷰를 Local 모드로 두고, 시계 판을 뚫고 나오는 화살표가 어느 축인지 보면 된다.\n" +
                     "파랑=Z, 초록=Y, 빨강=X")]
            public Vector3 axis = new Vector3(0f, 0f, 1f);

            [Tooltip("끄면 반대 방향으로 돈다.")]
            public bool clockwise = true;

            /// <summary>
            /// 각도만큼 돌리는 회전.
            ///
            /// 오일러 각을 더하지 않는다. localEulerAngles는 Y→X→Z 순으로 합쳐지므로
            /// X나 Y에 값을 더하면 바늘 자신의 축이 아니라 중간 축을 도는 셈이 되고,
            /// 처음 각도가 무엇이냐에 따라 결과가 달라진다.
            ///
            /// 처음 회전에 이 회전을 곱하면 처음 각도와 무관하게 언제나
            /// "바늘 자신의 축을 도는 것"이 된다.
            /// </summary>
            public Quaternion Rotation(float degrees)
            {
                Vector3 safeAxis = axis.sqrMagnitude > 0f ? axis : Vector3.forward;

                return Quaternion.AngleAxis(degrees * (clockwise ? -1f : 1f), safeAxis);
            }
        }

        /// <summary>
        /// 시계 하나. 분침과 초침을 들고, 자기가 흘려보낸 시간을 기억한다.
        /// </summary>
        [Serializable]
        public class Face
        {
            [Tooltip("분침. 초침의 1/60 속도로 돈다.")]
            public Transform minuteHand;

            [Tooltip("초침.")]
            public Transform secondHand;

            // 인스펙터에서 맞춰둔 각도가 0초 자리다.
            // 여기서 잡아두지 않으면 한 바퀴 돈 자리가 기준이 되어 계속 밀린다.
            //
            // 오일러가 아니라 쿼터니언으로 들고 있는다. 오일러는 (90,90,90)처럼
            // 같은 회전을 여러 값으로 적을 수 있어서, 되읽을 때 다른 숫자가 나온다.
            private Quaternion _minuteRest;
            private Quaternion _secondRest;

            private bool _cached;

            /// <summary>이 시계가 지금까지 돈 시간(초).</summary>
            public float Elapsed { get; private set; }

            public void CacheRest()
            {
                if (_cached) return;

                if (minuteHand != null) _minuteRest = minuteHand.localRotation;
                if (secondHand != null) _secondRest = secondHand.localRotation;

                _cached = true;
            }

            public void Add(float seconds)
            {
                Elapsed += seconds;
            }

            public void ResetElapsed()
            {
                Elapsed = 0f;
            }

            /// <summary>
            /// 흘러간 시간으로 바늘 각도를 다시 계산한다.
            ///
            /// 지난 각도에 더하지 않고 매번 처음부터 구한다.
            /// 더해 나가면 프레임마다 생기는 오차가 쌓여 두 바늘이 서로 안 맞게 된다.
            /// </summary>
            public void Apply(
                HandAxis minuteAxis,
                HandAxis secondAxis,
                float secondDegreesPerSecond,
                bool steppedSecond,
                bool steppedMinute)
            {
                CacheRest();

                // 초침이 한 바퀴 도는 데 걸리는 시간. 속도를 바꿔도 눈금 수는 그대로여야 하므로
                // "1초에 한 칸"이 아니라 "한 바퀴를 60칸으로" 나눈다.
                float turnSeconds = secondDegreesPerSecond > 0f
                    ? FullTurn / secondDegreesPerSecond
                    : 0f;

                float secondTime = Elapsed;
                float minuteTime = Elapsed;

                if (turnSeconds > 0f)
                {
                    if (steppedSecond)
                    {
                        float notchSeconds = turnSeconds / NotchesPerTurn;

                        secondTime = Mathf.Floor(Elapsed / notchSeconds) * notchSeconds;
                    }

                    // 분침은 초침이 한 바퀴를 다 돌았을 때만 한 칸 간다.
                    // 매끄럽게 흐르게 두면 한 바퀴에 6도뿐이라 움직이는지 아닌지 알 수가 없다.
                    if (steppedMinute)
                        minuteTime = Mathf.Floor(Elapsed / turnSeconds) * turnSeconds;
                }

                float secondAngle = secondTime * secondDegreesPerSecond;
                float minuteAngle = minuteTime * secondDegreesPerSecond / NotchesPerTurn;

                // 처음 회전에 곱한다. 순서가 뒤바뀌면(회전 * 처음) 바늘 자신의 축이 아니라
                // 부모 기준 축을 돌게 되어 처음 각도에 따라 결과가 달라진다.
                if (secondHand != null)
                    secondHand.localRotation = _secondRest * secondAxis.Rotation(secondAngle);

                if (minuteHand != null)
                    minuteHand.localRotation = _minuteRest * minuteAxis.Rotation(minuteAngle);
            }
        }

        [Header("시계")]
        [SerializeField] private Face playerClock = new Face();

        [SerializeField] private Face enemyClock = new Face();

        [Header("회전 방향")]
        [Tooltip("분침이 도는 축과 방향.")]
        [SerializeField] private HandAxis minuteAxis = new HandAxis();

        [Tooltip("초침이 도는 축과 방향.")]
        [SerializeField] private HandAxis secondAxis = new HandAxis();

        [Header("속도")]
        [Tooltip("초침이 1초에 도는 각도. 6이면 60초에 한 바퀴다.\n" +
                 "분침은 이것의 1/60로 돈다.")]
        [SerializeField, Min(0f)] private float secondDegreesPerSecond = 6f;

        [Tooltip("켜면 초침이 한 눈금(6도)씩 끊어 돈다. 끄면 매끄럽게 흐른다.")]
        [SerializeField] private bool steppedSecondHand = true;

        [Tooltip("켜면 분침이 초침 한 바퀴마다 한 칸씩 간다.\n" +
                 "끄면 매끄럽게 흐르는데, 한 바퀴에 6도뿐이라 거의 멈춰 보인다.")]
        [SerializeField] private bool steppedMinuteHand = true;

        [Header("기타")]
        [Tooltip("켜면 일시정지 중에도 시계가 돈다. 보통은 꺼둔다.")]
        [SerializeField] private bool ignoreTimeScale;

        private LDY_TurnManager _turnManager;

        /// <summary>플레이어가 지금까지 쓴 시간(초).</summary>
        public float PlayerElapsed => playerClock.Elapsed;

        /// <summary>적이 지금까지 쓴 시간(초).</summary>
        public float EnemyElapsed => enemyClock.Elapsed;

        private void Awake()
        {
            playerClock.CacheRest();
            enemyClock.CacheRest();

            Warn(playerClock, "Player Clock");
            Warn(enemyClock, "Enemy Clock");
        }

        private void Warn(Face face, string label)
        {
            // 바늘이 안 꽂혀 있으면 그냥 안 도는 것으로만 보여 원인을 알 수 없다.
            if (face.minuteHand == null && face.secondHand == null)
                Debug.LogWarning($"{name}: {label}에 분침·초침이 하나도 없습니다.", this);
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

            _turnManager = null;
        }

        private void Bind(LDY_TurnManager turnManager)
        {
            _turnManager = turnManager;
        }

        private void Update()
        {
            // 턴을 모르면 어느 시계를 돌릴지도 모른다. 둘 다 멈춰 둔다.
            if (_turnManager == null) return;

            float delta = ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;

            Tick(delta, _turnManager.CurrentTurn);
        }

        /// <summary>
        /// 지금 턴인 쪽만 시간을 흘려보내고, 양쪽 바늘을 다시 그린다.
        ///
        /// 멈춘 쪽도 Apply를 부른다. 아무것도 안 하는 것 같지만,
        /// 누가 바늘을 건드렸을 때 원래 자리로 되돌려 놓는 역할을 한다.
        /// </summary>
        private void Tick(float delta, LDY_Team turn)
        {
            if (turn == LDY_Team.Player) playerClock.Add(delta);
            else enemyClock.Add(delta);

            ApplyBoth();
        }

        private void ApplyBoth()
        {
            playerClock.Apply(minuteAxis, secondAxis, secondDegreesPerSecond, steppedSecondHand, steppedMinuteHand);
            enemyClock.Apply(minuteAxis, secondAxis, secondDegreesPerSecond, steppedSecondHand, steppedMinuteHand);
        }

        /// <summary>
        /// 두 시계를 처음으로 되돌린다. 전투를 다시 시작할 때 부른다.
        /// </summary>
        public void ResetClocks()
        {
            playerClock.ResetElapsed();
            enemyClock.ResetElapsed();

            ApplyBoth();
        }
    }
}
