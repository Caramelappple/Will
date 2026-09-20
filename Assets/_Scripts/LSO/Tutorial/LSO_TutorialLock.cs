using _Scripts.LDY;
using _Scripts.LSO.UI.Input;
using _Scripts.LSO.Will.Candle;
using UnityEngine;

namespace _Scripts.LSO.Tutorial
{
    /// <summary>
    /// 지금 걸음에서 허용된 조작만 연다.
    ///
    /// ── 왜 필요한가 ───────────────────────────────────────────
    /// "카드 한 장을 선택해보세요" 할 때 플레이어가 턴을 넘겨버리면 대본이 무너진다.
    /// 관문은 기다릴 뿐 막지는 못하므로, 막는 쪽이 따로 있어야 한다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// **여기서 조작을 새로 구현하지 않는다.** 이미 있는 컴포넌트를 껐다 켜는 것이
    /// 전부다. 튜토리얼이 판단을 하나라도 들고 있으면 평소 게임과 규칙이 갈린다.
    ///
    /// 씬 배선: 아무 곳에나 하나. 참조는 비워두면 찾는다.
    /// Will Candle · End Turn Button 은 씬마다 달라 손으로 꽂는 편이 확실하다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LSO_TutorialLock : MonoBehaviour
    {
        [Header("보드 조작 (비우면 찾는다)")]
        [Tooltip("기물 고르기·이동·공격을 한꺼번에 쥐고 있다.")]
        [SerializeField] private LDY_SelectionController selection;

        [Tooltip("카드로 기물을 놓는 쪽.")]
        [SerializeField] private LDY_CardPlacer cardPlacer;

        [Header("손으로 꽂을 것")]
        [Tooltip("턴 넘기기 버튼의 클릭 처리. 끄면 눌러도 아무 일이 없다.")]
        [SerializeField] private LSO_ButtonClickHandler endTurnButton;

        [Tooltip("유언 촛불. 끄면 숫자키·휠이 안 먹는다.")]
        [SerializeField] private LSO_WillCandle willCandle;

        [Tooltip("손패를 다시 훑는 간격(초). 새로 뽑힌 카드를 막는 데 쓴다.")]
        [SerializeField, Min(0.05f)] private float sweepInterval = 0.25f;

        /// <summary>지금 허용된 것. 진단용으로 인스펙터에서 보이게 둔다.</summary>
        [Header("진단 (읽기 전용)")]
        [SerializeField] private LSO_TutorialAction current = LSO_TutorialAction.All;

        private static LSO_TutorialLock _active;
        public static bool Allows(LSO_TutorialAction action) =>
            _active == null || !_active.isActiveAndEnabled || Has(_active.current, action);

        private void Awake()
        {
            if (selection == null) selection = FindAnyObjectByType<LDY_SelectionController>();
            if (cardPlacer == null) cardPlacer = FindAnyObjectByType<LDY_CardPlacer>();
            if (willCandle == null) willCandle = FindAnyObjectByType<LSO_WillCandle>();
        }

        /// <summary>이 걸음 동안 허용할 것을 정한다.</summary>
        public void Apply(LSO_TutorialAction allowed)
        {
            _active = this;
            current = allowed;

            // 전부 허용이면 다시 훑을 이유가 없다.
            _locking = allowed != LSO_TutorialAction.All;

            // 기물 고르기·이동·공격은 한 컴포넌트가 쥐고 있어 셋 중 하나라도
            // 허용되면 열어야 한다. 더 잘게 나누려면 그쪽을 먼저 쪼개야 한다.
            bool boardTouch =
                Has(allowed, LSO_TutorialAction.PieceSelect) ||
                Has(allowed, LSO_TutorialAction.Move) ||
                Has(allowed, LSO_TutorialAction.Attack);

            SetEnabled(selection, boardTouch);

            // ── 카드는 두 군데를 따로 막아야 한다 ─────────────────────
            // LDY_CardPlacer 는 "칸에 놓는" 쪽이다. 그것만 끄면 카드를 고르는 것은
            // 그대로 된다 — 카드가 올라오고 배치 모드까지 들어간다.
            //
            // 카드 클릭을 실제로 받는 것은 KTH_HandCard 자신이다.
            // 고르는 것을 막으려면 그쪽을 꺼야 한다.
            // ─────────────────────────────────────────────────────────
            SetEnabled(cardPlacer, Has(allowed, LSO_TutorialAction.Place));

            SetHandCardsEnabled(Has(allowed, LSO_TutorialAction.CardSelect));

            SetEnabled(endTurnButton, Has(allowed, LSO_TutorialAction.EndTurn));
            SetEnabled(willCandle, Has(allowed, LSO_TutorialAction.Will));
        }

        /// <summary>전부 연다. 튜토리얼이 끝나거나 건너뛸 때 부른다.</summary>
        public void Release()
        {
            Apply(LSO_TutorialAction.All);

            _locking = false;
        }

        /// <summary>
        /// 지금 무언가를 막고 있는지. 참일 때만 손패를 다시 훑는다.
        /// </summary>
        private bool _locking;

        private float _nextSweep;

        /// <summary>
        /// ── 손패는 계속 다시 훑는다 ───────────────────────────────
        /// Apply 는 걸음이 시작될 때 한 번 돈다. 그 뒤에 뽑힌 카드는 켜진 채로 나온다.
        /// 내 턴이 시작되면 손패를 통째로 새로 받으므로, 한 번만 끄는 것으로는 못 막는다.
        ///
        /// 매 프레임은 아니고 Sweep Interval 마다 본다. 손패가 여덟 장 상한이라
        /// 훑는 값이 싸고, 카드가 생기고 반 박자 뒤에 막혀도 눌릴 틈은 거의 없다.
        ///
        /// 막는 것이 없으면(Release 뒤) 아예 안 돈다.
        /// ─────────────────────────────────────────────────────────
        /// </summary>
        private void Update()
        {
            if (!_locking) return;
            if (Time.unscaledTime < _nextSweep) return;

            _nextSweep = Time.unscaledTime + sweepInterval;

            SetHandCardsEnabled(Has(current, LSO_TutorialAction.CardSelect));
        }

        private static bool Has(LSO_TutorialAction value, LSO_TutorialAction flag) =>
            (value & flag) != 0;

        /// <summary>
        /// 손패 카드의 클릭을 여닫는다.
        ///
        /// 목록을 들고 있지 않고 부를 때마다 다시 훑는다. 카드는 매 턴 뽑히고 버려져서
        /// 들고 있으면 그 사이에 생긴 카드만 막히거나, 사라진 카드를 붙들고 있게 된다.
        ///
        /// 꺼진 카드까지 찾는 이유는 손패가 카드를 풀에 넣어두기 때문이다.
        /// 지금 안 보이는 카드도 다음에 뽑히면 그대로 나온다.
        /// </summary>
        private static void SetHandCardsEnabled(bool on)
        {
            KTH_HandCard[] cards = FindObjectsByType<KTH_HandCard>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < cards.Length; i++)
                SetEnabled(cards[i], on);
        }

        /// <summary>
        /// 컴포넌트를 껐다 켠다. 값이 그대로면 건드리지 않는다 —
        /// 껐다 켜면 OnEnable/OnDisable 이 돌아 다른 것들이 함께 반응한다.
        /// </summary>
        private static void SetEnabled(Behaviour target, bool on)
        {
            if (target == null) return;
            if (target.enabled == on) return;

            target.enabled = on;
        }
    }
}
