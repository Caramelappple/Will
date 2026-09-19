using System;
using UnityEngine;

namespace _Scripts.LSO.Tutorial.Gate
{
    /// <summary>
    /// 다음 걸음으로 넘어가는 조건 하나.
    ///
    /// 감독은 이 둘만 안다. 조건이 몇 가지로 늘어나도 감독은 안 고쳐진다.
    /// </summary>
    public interface LSO_ITutorialGate
    {
        /// <summary>
        /// 기다리기 시작한다.
        /// </summary>
        /// <param name="runner">
        /// 코루틴을 대신 돌려줄 몸. 관문은 SO(에셋)이라 스스로 코루틴을 못 돌린다.
        ///
        /// 관문마다 빈 오브젝트를 하나씩 만들게 하느니 이미 씬에 있는 감독을 빌려 쓴다.
        /// 쓰지 않는 관문(구독만 하는 쪽)은 그냥 무시하면 된다.
        /// </param>
        /// <param name="onPassed">조건이 맞았을 때 부를 것.</param>
        void Arm(MonoBehaviour runner, Action onPassed);

        /// <summary>그만 기다린다. 통과했든 시간이 지났든 반드시 불린다.</summary>
        void Disarm();
    }
}
