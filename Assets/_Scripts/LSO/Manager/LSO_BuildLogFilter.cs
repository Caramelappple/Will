using UnityEngine;
using _Scripts.LSO.Stage;

namespace _Scripts.LSO.Manager
{
    /// <summary>
    /// 배포 빌드에서 일반 로그를 막는다. 스테이지 진행 로그와 경고·에러는 통과시킨다.
    ///
    /// ── 왜 CoreLib 이 아니라 여기인가 ─────────────────────────
    /// 처음에는 LSO/CoreLib 에 뒀다가 컴파일이 안 됐다.
    /// 그 폴더에는 LSO.CoreLib.asmdef 가 있어서 <b>별도 어셈블리</b>이고,
    /// 스테이지 타입들은 asmdef 가 없어 Assembly-CSharp 에 있다.
    ///
    /// 참조는 한 방향뿐이다 — Assembly-CSharp 은 asmdef 어셈블리를 쓸 수 있지만
    /// 그 반대는 안 된다. CoreLib 에서 스테이지 타입을 이름으로 부를 방법이 없다.
    ///
    /// 그래서 asmdef 가 없는 폴더로 옮겼다. 새 파일이 다른 폴더의 타입을 참조할 때는
    /// 그 폴더에 asmdef 가 있는지 먼저 볼 것.
    /// ─────────────────────────────────────────────────────────
    ///
    /// ── 왜 필요한가 ───────────────────────────────────────────
    /// Debug.Log 가 134군데 있고 그중 125개는 토글 없이 무조건 찍힌다.
    /// 카드를 뽑을 때마다, 투자할 때마다, 계승할 때마다 한 줄씩 나간다.
    ///
    /// 개발 중에는 그게 단서지만 배포 빌드에서는 로그 파일만 불리고,
    /// 진짜 문제가 생겼을 때 경고가 그 사이에 묻힌다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// ── 무엇을 남기나 ─────────────────────────────────────────
    ///   경고 · 에러 · 예외      전부 남긴다. 배선 누락은 빌드에서도 보여야 한다
    ///   스테이지 진행 로그      남긴다. 테스터가 어디까지 갔는지 봐야 한다
    ///   나머지 Debug.Log        막는다
    /// ─────────────────────────────────────────────────────────
    ///
    /// 스테이지인지는 <b>메시지가 아니라 context 로</b> 가른다.
    /// 문구로 거르면 로그 문장을 고칠 때마다 조용히 필터가 어긋난다.
    /// 세 클래스 모두 Debug.Log 에 this 를 넘기고 있어서 이게 가능하다.
    ///
    /// 스테이지 로그 자체는 각 컴포넌트의 <b>Log Steps 체크박스</b>가 켜져 있어야 나온다.
    /// 여기서 하는 일은 "통과시킨다"까지고 "찍게 한다"가 아니다.
    /// </summary>
    public sealed class LSO_BuildLogFilter : ILogHandler
    {
        private readonly ILogHandler _inner;

        private LSO_BuildLogFilter(ILogHandler inner)
        {
            _inner = inner;
        }

        /// <summary>
        /// 배포 빌드에서만 끼워 넣는다.
        ///
        /// Debug.isDebugBuild 는 에디터와 Development Build 에서 참이다.
        /// 그래서 개발 중에는 아무것도 막지 않고, 배포용으로 뽑을 때만 조용해진다 —
        /// 빌드 설정을 바꿀 때마다 이 스크립트를 손댈 필요가 없다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (Debug.isDebugBuild) return;

            // 씬을 다시 불러도 한 번만 끼운다. 겹쳐 끼우면 로그가 여러 겹을 지난다.
            if (Debug.unityLogger.logHandler is LSO_BuildLogFilter) return;

            Debug.unityLogger.logHandler =
                new LSO_BuildLogFilter(Debug.unityLogger.logHandler);
        }

        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            // 막는 것은 일반 로그뿐이다. 경고·에러는 그대로 흘려보낸다.
            if (logType == LogType.Log && !IsStage(context)) return;

            _inner.LogFormat(logType, context, format, args);
        }

        /// <summary>예외는 무조건 남긴다. 이걸 막으면 터진 자리를 찾을 방법이 없다.</summary>
        public void LogException(System.Exception exception, Object context)
        {
            _inner.LogException(exception, context);
        }

        /// <summary>
        /// 스테이지 진행을 알리는 로그인지.
        ///
        /// 새 스테이지 컴포넌트를 만들어 로그를 남기려면 여기에 더해야 한다.
        /// 목록이 여기 하나라 어디를 고쳐야 하는지는 분명하다.
        /// </summary>
        private static bool IsStage(Object context)
        {
            return context is LSO_StageProgression
                or LSO_StageFlow
                or LSO_StageIntroDirector

                // 개발자 키도 여기 넣는다. 이것만 빌드에 들어가는 개발자 키인데,
                // 막아버리면 눌러도 아무 말이 없어서 먹혔는지 알 수 없다.
                or LSO_StageDevKey;
        }
    }
}
