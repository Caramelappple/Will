using System;
using UnityEngine.Events;

namespace _Scripts.LSO.Reward
{
    /// <summary>
    /// 보상 하나를 인자로 넘기는 UnityEvent.
    ///
    /// UnityEvent&lt;T&gt;를 그대로 필드에 쓰면 인스펙터에 나오지 않는다.
    /// 유니티가 제네릭 타입을 직렬화하지 못하므로 닫힌 타입으로 한 번 감싸야 한다.
    ///
    /// 인자가 필요 없는 자리(카드가 다 나왔을 때 등)에서는 null을 넘긴다.
    /// 받는 쪽이 인자 없는 메서드를 골랐으면 그냥 호출되고, 받는 메서드를 골랐으면 null이 간다.
    /// </summary>
    [Serializable]
    public class LSO_RewardEvent : UnityEvent<LSO_RewardOption> { }
}
