using System.Collections.Generic;
using _Scripts.LSO.Will;
using UnityEngine;

namespace _Scripts.LDY.Effect
{
    /// <summary>
    /// 연출이 도는 동안 보드 조작을 막는다.
    ///
    /// 승리 판정이 떨어져도 LDY_SelectionController는 계속 클릭을 받는다.
    /// "게임이 끝났다"를 보는 가드가 없고 턴 가드(CurrentTurn != Player)만 있는데,
    /// 승리 시점의 턴은 대개 Player라 그대로 통과한다.
    /// 보드를 돌리는 동안 클릭이 들어오면 WorldToGrid가 돌아간 좌표를 그대로 환산해
    /// 엉뚱한 칸을 집는다(격자 계산은 보드 루트의 rotation을 보지 않는다).
    ///
    /// 잠그는 방법이 두 가지인 이유:
    ///
    ///   · 컴포넌트를 끄는 쪽(gatedBehaviours)이 기본이다. 부작용이 없다.
    ///   · LSO_WillSelection의 전역 잠금은 기본으로 쓰지 않는다.
    ///     LSO_WillPanel이 BoardInteractionLockChanged를 구독해서 전체 화면 검은 디머를 페이드인하는데
    ///     (LSO_WillPanel.HandleBoardInteractionLockChanged), 그 패널이 LSO_Test 씬에 들어 있다.
    ///     보드가 뒤집히는 장면 위에 검은 막이 덮이면 연출 자체가 안 보인다.
    ///     디머를 오히려 원한다면 디렉터의 useGlobalInteractionLock을 켜면 된다.
    /// </summary>
    public sealed class LDY_BoardInputGate
    {
        private readonly LDY_CardPlacer _cardPlacer;
        private readonly IReadOnlyList<Behaviour> _gated;
        private readonly bool _useGlobalLock;

        private readonly List<Behaviour> _disabled = new();
        private bool _isClosed;
        private bool _globalLockReleased;

        public LDY_BoardInputGate(
            LDY_CardPlacer cardPlacer,
            IReadOnlyList<Behaviour> gatedBehaviours,
            bool useGlobalLock)
        {
            _cardPlacer = cardPlacer;
            _gated = gatedBehaviours;
            _useGlobalLock = useGlobalLock;
        }

        public bool IsClosed => _isClosed;

        /// <summary>보드 입력을 막는다. 여러 번 불러도 안전하다.</summary>
        public void Close()
        {
            if (_isClosed) return;
            _isClosed = true;
            _globalLockReleased = false;

            if (_cardPlacer != null)
            {
                // 배치 위치를 고르던 중이면 먼저 물린다. 카드는 손패에 그대로 남는다.
                if (_cardPlacer.IsPlacing)
                    _cardPlacer.CancelPlacement();

                // 배치 입력은 전역 잠금의 대상이 아니라 CardPlacer가 따로 처리한다.
                _cardPlacer.SetBoardActive(false);
            }

            if (_gated != null)
            {
                foreach (Behaviour behaviour in _gated)
                {
                    if (behaviour == null || !behaviour.enabled) continue;

                    behaviour.enabled = false;
                    _disabled.Add(behaviour);
                }
            }

            if (_useGlobalLock)
                LSO_WillSelection.BeginBoardInteractionLock();
        }

        /// <summary>
        /// 연출이 정상적으로 끝났을 때 부른다. 컴포넌트는 끈 채로 두고 전역 잠금만 푼다.
        ///
        /// 다시 열지 않는 이유: 연출이 끝난 보드는 뒤집혀 있고 기물은 숨겨져 있는데,
        /// 격자에는 그 기물들이 그대로 등록돼 있다. 여기서 클릭을 다시 받으면
        /// 보이지도 않는 기물이 선택되고 하이라이트가 뜬다. 이 뒤로는 보상 선택과 씬 전환만 남았으므로
        /// 보드 입력은 씬이 끝날 때까지 닫아두는 편이 맞다.
        ///
        /// 전역 잠금은 반대로 반드시 푼다. static이라 씬을 넘어가도 남아서,
        /// 그대로 두면 다음 전투 씬이 잠긴 채로 시작한다.
        /// </summary>
        public void Seal()
        {
            if (!_isClosed) return;

            ReleaseGlobalLock();

            // _disabled는 비우지 않는다. 디버그로 연출을 되돌릴 때 Open()이 그대로 살려낼 수 있어야 한다.
        }

        /// <summary>막기 전 상태로 되돌린다. 여러 번 불러도 안전하다.</summary>
        public void Open()
        {
            if (!_isClosed) return;
            _isClosed = false;

            ReleaseGlobalLock();

            foreach (Behaviour behaviour in _disabled)
            {
                if (behaviour != null)
                    behaviour.enabled = true;
            }
            _disabled.Clear();

            if (_cardPlacer != null)
                _cardPlacer.SetBoardActive(true);
        }

        /// <summary>
        /// 전역(static) 잠금을 푼다. 두 번 풀지 않는다.
        ///
        /// 이 잠금은 static이라 씬을 넘어가도 남는다.
        /// 그대로 두면 다음 전투 씬이 잠긴 채로 시작한다.
        /// </summary>
        private void ReleaseGlobalLock()
        {
            if (!_useGlobalLock || _globalLockReleased) return;

            _globalLockReleased = true;
            LSO_WillSelection.EndBoardInteractionLock(0f);
        }
    }
}
