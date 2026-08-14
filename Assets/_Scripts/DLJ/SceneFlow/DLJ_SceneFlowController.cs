using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.DLJ.SceneFlow
{
    [DefaultExecutionOrder(-1000)]
    public sealed class DLJ_SceneFlowController : MonoBehaviour
    {
        private static DLJ_SceneFlowController instance;
        private static bool isQuitting;

        private Coroutine loadCoroutine;

        public static DLJ_SceneFlowController Instance
        {
            get
            {
                if (isQuitting)
                    return null;

                if (instance == null)
                    CreateInstance();

                return instance;
            }
        }

        public DLJ_SceneStateMachine StateMachine { get; private set; }
        public bool IsLoading { get; private set; }
        public float LoadProgress { get; private set; }

        public event Action<float> LoadProgressChanged;
        public event Action<string> SceneLoadCompleted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            isQuitting = false;
            CreateInstance();
        }

        private static void CreateInstance()
        {
            if (instance != null)
                return;

            GameObject controllerObject = new GameObject(nameof(DLJ_SceneFlowController));
            instance = controllerObject.AddComponent<DLJ_SceneFlowController>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            StateMachine = new DLJ_SceneStateMachine(BeginSceneLoad, () => IsLoading);
        }

        private void Update()
        {
            StateMachine?.Tick();
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        public bool ChangeState(DLJ_ISceneState nextState)
        {
            return StateMachine.ChangeState(nextState);
        }

        private void BeginSceneLoad(string sceneName)
        {
            if (loadCoroutine != null)
                throw new InvalidOperationException("이미 다른 씬을 불러오는 중입니다.");

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
                throw new ArgumentException($"Build Settings에서 씬을 찾을 수 없습니다: {sceneName}", nameof(sceneName));

            loadCoroutine = StartCoroutine(LoadSceneRoutine(sceneName));
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            IsLoading = true;
            SetProgress(0f);

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation == null)
            {
                IsLoading = false;
                loadCoroutine = null;
                Debug.LogError($"[DLJ SceneFlow] 씬 로드를 시작하지 못했습니다: {sceneName}", this);
                yield break;
            }

            while (!operation.isDone)
            {
                SetProgress(Mathf.Clamp01(operation.progress / 0.9f));
                yield return null;
            }

            SetProgress(1f);
            IsLoading = false;
            loadCoroutine = null;
            SceneLoadCompleted?.Invoke(sceneName);
        }

        private void SetProgress(float progress)
        {
            LoadProgress = progress;
            LoadProgressChanged?.Invoke(LoadProgress);
        }
    }
}
