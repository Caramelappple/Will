using UnityEngine;

namespace _Scripts.LSO.CoreLib
{
    /// <summary>
    /// 게임 종료.
    ///
    /// Application.Quit()은 에디터에서 아무 일도 하지 않는다.
    /// 그대로 두면 테스트할 때마다 "왜 안 나가지"로 시간을 버리므로 여기서 갈라둔다.
    /// </summary>
    public static class LSO_Quit
    {
        public static void Request()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
