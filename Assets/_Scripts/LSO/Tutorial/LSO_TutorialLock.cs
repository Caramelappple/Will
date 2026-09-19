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

        /// <summary>지금 허용된 것. 진단용으로 인스펙터에서 보이게 둔다.</summary>
        [Header("진단 (읽기 전용)")]
        [SerializeField] private LSO_TutorialAction current = LSO_TutorialAction.All;

        private void Awake()
        {
            if (selection == null) selection = FindAnyObjectByType<LDY_SelectionController>();
            if (cardPlacer == null) cardPlacer = FindAnyObjectByType<LDY_CardPlacer>();
            if (willCandle == null) willCandle = FindAnyObjectByType<LSO_WillCandle>();
        }

        /// <summary>이 걸음 동안 허용할 것을 정한다.</summary>
        public void Apply(LSO_TutorialAction allowed)
        {
            current = allowed;

            // 기물 고르기·이동·공격은 한 컴포넌트가 쥐고 있어 셋 중 하나라도
            // 허용되면 열어야 한다. 더 잘게 나누려면 그쪽을 먼저 쪼개야 한다.
            bool boardTouch =
                Has(allowed, LSO_TutorialAction.PieceSelect) ||
                Has(allowed, LSO_TutorialAction.Move) ||
                Has(allowed, LSO_TutorialAction.Attack);

            SetEnabled(selection, boardTouch);

            // 카드 선택과 배치도 같은 쪽이 쥐고 있다.
            bool cardTouch =
                Has(allowed, LSO_TutorialAction.CardSelect) ||
                Has(allowed, LSO_TutorialAction.Place);

            SetEnabled(cardPlacer, cardTouch);

            SetEnabled(endTurnButton, Has(allowed, LSO_TutorialAction.EndTurn));
            SetEnabled(willCandle, Has(allowed, LSO_TutorialAction.Will));
        }

        /// <summary>전부 연다. 튜토리얼이 끝나거나 건너뛸 때 부른다.</summary>
        public void Release()
        {
            Apply(LSO_TutorialAction.All);
        }

        private static bool Has(LSO_TutorialAction value, LSO_TutorialAction flag) =>
            (value & flag) != 0;

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
