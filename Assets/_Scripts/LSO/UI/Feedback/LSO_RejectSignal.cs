using System;
using UnityEngine;

namespace _Scripts.LSO.UI.Feedback
{
    /// <summary>
    /// "지금 이 행동은 안 된다"를 알리는 창구.
    ///
    /// 거부를 판단하는 쪽(팩토리, 배치기, 턴 매니저)은 UI가 있는지 몰라야 하고,
    /// 반응하는 쪽(흔들기, 사운드, 토스트)은 누가 거부했는지 몰라야 한다.
    /// 그래서 정적 이벤트 하나로만 이어둔다.
    ///
    /// 구독자가 없어도 아무 일도 일어나지 않으므로 게임 로직은 영향받지 않는다.
    /// </summary>
    public static class LSO_RejectSignal
    {
        public static event Action<LSO_RejectReason> Rejected;

        public static void Raise(LSO_RejectReason reason)
        {
            Rejected?.Invoke(reason);
        }

        /// <summary>
        /// 씬을 다시 로드해도 정적 이벤트는 살아남는다.
        /// 파괴된 오브젝트의 핸들러가 남아 있으면 다음 씬에서 예외가 난다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            Rejected = null;
        }
    }
}
