using UnityEngine;

namespace _Scripts.LSO.CoreLib
{
    public static class LSO_ApplicationState
    {
        public static bool IsQuitting { get; private set; }

        public static void MarkQuitting() => IsQuitting = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => IsQuitting = false;
    }
}
