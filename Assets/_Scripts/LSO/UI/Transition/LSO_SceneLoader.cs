using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.LSO.UI.Transition
{
    /// <summary>
    /// 화면을 가린 뒤 씬을 바꾸고 다시 걷는다.
    /// LSO_ScreenFader와 같은 오브젝트에 붙인다(DontDestroyOnLoad로 함께 남는다).
    ///
    /// 페이더가 없으면 연출 없이 그냥 씬을 바꾼다. 연출이 없다고 게임이 멈추면 안 된다.
    /// </summary>
    public class LSO_SceneLoader : MonoBehaviour
    {
        public static LSO_SceneLoader Current { get; private set; }

        [Tooltip("암전 상태를 최소 이만큼 유지한다. 로딩이 너무 빠르면 화면이 깜빡이기만 한다.")]
        [SerializeField, Min(0f)] private float minimumCoverTime = 0.2f;

        /// <summary>전환 중인지. 중복 요청을 막는 데 쓴다.</summary>
        public static bool IsLoading { get; private set; }

        private void Awake()
        {
            if (Current != null && Current != this)
            {
                Destroy(this);
                return;
            }

            Current = this;
        }

        private void OnDestroy()
        {
            if (Current == this)
                Current = null;
        }

        public static void Load(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("LSO_SceneLoader: 씬 이름이 비어 있습니다.");
                return;
            }

            Begin(() => SceneManager.LoadSceneAsync(sceneName), sceneName);
        }

        public static void Load(int buildIndex)
        {
            Begin(() => SceneManager.LoadSceneAsync(buildIndex), buildIndex.ToString());
        }

        /// <summary>현재 씬을 다시 불러온다. 재시작 버튼에 쓴다.</summary>
        public static void Reload()
        {
            Load(SceneManager.GetActiveScene().buildIndex);
        }

        private static void Begin(System.Func<AsyncOperation> loadOperation, string label)
        {
            // 전환 중에 또 누르면 씬이 두 번 로드되면서 상태가 꼬인다.
            if (IsLoading)
            {
                Debug.Log($"LSO_SceneLoader: 이미 전환 중이라 '{label}' 요청을 무시합니다.");
                return;
            }

            if (Current == null)
            {
                // 로더가 씬에 없으면 연출을 포기하고 바로 넘어간다.
                loadOperation();
                return;
            }

            IsLoading = true;
            Current.StartCoroutine(Current.Routine(loadOperation));
        }

        private IEnumerator Routine(System.Func<AsyncOperation> loadOperation)
        {
            LSO_IScreenFader fader = LSO_ScreenFader.Current;

            if (fader != null)
                yield return fader.Cover();

            float coverStart = Time.unscaledTime;

            AsyncOperation operation = loadOperation();
            while (operation != null && !operation.isDone)
                yield return null;

            // 로딩이 순식간이면 암전이 스쳐 지나가 오히려 거슬린다.
            while (Time.unscaledTime - coverStart < minimumCoverTime)
                yield return null;

            // 새 씬의 Awake/Start가 한 번 돌 틈을 준다.
            // 바로 걷으면 초기화 전 화면이 한 프레임 비친다.
            yield return null;

            if (fader != null)
                yield return fader.Reveal();

            IsLoading = false;
        }
    }
}
