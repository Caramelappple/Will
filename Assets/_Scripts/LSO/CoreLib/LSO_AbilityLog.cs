using UnityEngine;

namespace _Scripts.LSO.CoreLib
{
    /// <summary>
    /// 특성이 발동을 알리는 창구.
    ///
    /// 특성은 MonoBehaviour가 아니라 인스펙터 토글을 달 수 없다.
    /// 그래서 스위치를 한곳에 모아두고 전부 여기를 거치게 한다.
    ///
    /// 끄고 싶으면 LSO_AbilityLog.Enabled = false 한 줄이면 되고,
    /// 씬에서 만지고 싶으면 이 값을 대신 써주는 컴포넌트를 하나 붙이면 된다.
    /// </summary>
    public static class LSO_AbilityLog
    {
        public static bool Enabled { get; set; } = true;

        public static void Log(string message, Object context = null)
        {
            if (!Enabled) return;

            Debug.Log(message, context);
        }

        // 정적 값은 Reload Domain을 끄면 플레이 사이에 살아남는다.
        // 지난 판에서 꺼둔 상태가 다음 판까지 따라오지 않게 되돌린다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Enabled = true;
    }
}
