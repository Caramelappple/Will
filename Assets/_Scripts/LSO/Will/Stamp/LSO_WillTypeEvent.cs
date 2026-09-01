using System;
using UnityEngine.Events;

namespace _Scripts.LSO.Will.Stamp
{
    /// <summary>
    /// 유언 하나를 인자로 넘기는 UnityEvent.
    ///
    /// UnityEvent&lt;T&gt;를 그대로 필드에 쓰면 인스펙터에 나오지 않는다.
    /// 유니티가 제네릭 타입을 직렬화하지 못하므로 닫힌 타입으로 한 번 감싸야 한다.
    /// </summary>
    [Serializable]
    public class LSO_WillTypeEvent : UnityEvent<LSO_WillType> { }
}
