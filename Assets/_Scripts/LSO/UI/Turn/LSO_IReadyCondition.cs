using System;

namespace _Scripts.LSO.UI.Turn
{
    /// <summary>
    /// "지금 이 조건이 맞는가"를 알려주는 것.
    ///
    /// 턴 넘기기 표식을 띄울지 정하는 조건이 하나가 아니다.
    ///   · 턴을 넘길 수 있는 상태인가 (LSO_EndTurnReady)
    ///   · 코스트를 다 써서 더 낼 것이 없는가 (LSO_OutOfCost)
    ///
    /// 조건이 늘어날 때마다 표식 쪽을 고치지 않으려고 모양을 맞춰 둔다.
    /// 합치는 일은 LSO_ReadyConditions 가 한다.
    /// </summary>
    public interface LSO_IReadyCondition
    {
        /// <summary>지금 이 조건이 맞는지.</summary>
        bool IsMet { get; }

        /// <summary>값이 바뀔 때만 발행된다. 인자는 바뀐 뒤의 값.</summary>
        event Action<bool> Changed;
    }
}
