using System;
using System.Collections.Generic;
using _Scripts.LSO.Deck.Data;

namespace _Scripts.LSO.Will
{
    /// <summary>
    /// 기물을 소환할 때 유언을 고르게 하는 창구.
    ///
    /// 소환을 진행하는 쪽(LDY_CardPlacer)은 UI가 팝업인지 라디얼 메뉴인지 몰라야 하고,
    /// UI는 소환 절차를 몰라야 한다. 그 사이를 잇는 계약이 이것뿐이다.
    ///
    /// 구현하는 쪽이 지켜야 할 것:
    ///   - onSelected 또는 onCancelled 중 정확히 하나를 반드시 부른다.
    ///   - 둘 중 하나를 부르기 전까지 IsSelecting은 true를 유지한다.
    ///   - 이미 선택 중일 때 Request가 또 오면 이전 요청을 취소 처리한다.
    /// 하나도 안 부르면 소환 절차가 영원히 멈춘다.
    /// </summary>
    public interface LSO_IWillSelector
    {
        /// <summary>선택 창이 떠 있는 동안 true.</summary>
        bool IsSelecting { get; }

        /// <summary>
        /// 유언 선택을 요청한다.
        /// </summary>
        /// <param name="card">무엇을 소환하려는지. 이름과 스탯을 창에 띄우는 용도다.</param>
        /// <param name="options">고를 수 있는 유언. 해금된 것만 넘어온다.</param>
        /// <param name="onSelected">플레이어가 골랐을 때.</param>
        /// <param name="onCancelled">플레이어가 물렀을 때. 소환은 취소된다.</param>
        void Request(
            LSO_CardSO card,
            IReadOnlyList<LSO_WillType> options,
            Action<LSO_WillType> onSelected,
            Action onCancelled);

        /// <summary>
        /// 바깥 사정으로 선택을 중단한다(턴 종료, 씬 전환 등).
        /// onCancelled를 부르고 창을 닫아야 한다.
        /// </summary>
        void Abort();
    }
}
