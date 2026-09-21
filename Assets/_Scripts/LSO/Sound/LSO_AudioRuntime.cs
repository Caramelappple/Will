using _Scripts.LDY;
using _Scripts.LDY.Stage;
using _Scripts.LSO.Ability;
using _Scripts.LSO.Boss;
using _Scripts.LSO.UI.Panel;
using DevLib.ServiceLocator;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _Scripts.LSO.Sound
{
    /// <summary>씬 파일을 수정하지 않고 오디오 서비스를 한 번 준비하고 씬·특성 알림을 연결한다.</summary>
    public sealed class LSO_AudioRuntime : MonoBehaviour
    {
        private static LSO_AudioRuntime _instance;
        private static bool _quitting;
        private bool _ready;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { _instance = null; _quitting = false; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Ensure()) _instance.ConnectScene(SceneManager.GetActiveScene());
        }

        public static bool Ensure()
        {
            if (_quitting || !Application.isPlaying) return false;
            if (_instance == null)
                new GameObject("Will Audio").AddComponent<LSO_AudioRuntime>();
            return _instance != null && _instance._ready;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            AudioService existing = AudioService.Active;
            if (existing != null)
            {
                existing.transform.SetParent(transform, true);
                _ready = ReferenceEquals(ServiceLocator.Get<IAudioService>(), existing);
                if (!_ready) Debug.LogError("[Will Audio] 기존 오디오 서비스 등록 상태가 올바르지 않습니다.", this);
                return;
            }

            GameObject prefab = Resources.Load<GameObject>("WillAudio/AudioService");
            if (prefab == null)
            {
                Debug.LogError("[Will Audio] AudioService 프리팹이 없습니다.", this);
                return;
            }
            Instantiate(prefab, transform);
            _ready = ReferenceEquals(ServiceLocator.Get<IAudioService>(), AudioService.Active);
            if (!_ready) Debug.LogError("[Will Audio] 오디오 서비스 초기화 실패.", this);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            LSO_AbilitySignal.Fired += OnAbility;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            LSO_AbilitySignal.Fired -= OnAbility;
        }

        private void OnApplicationQuit() { _quitting = true; }
        private void OnDestroy() { if (_instance == this) _instance = null; }
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single) AudioService.Active?.StopAllSfx();
            ConnectScene(scene);
        }

        private void ConnectScene(Scene scene)
        {
            if (!_ready) return;
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (LSO_MenuActions menu in root.GetComponentsInChildren<LSO_MenuActions>(true))
            foreach (GameObject panel in menu.Panels)
            {
                if (panel == null) continue;
                foreach (Button button in panel.GetComponentsInChildren<Button>(true))
                    if (button.GetComponent<LSO_MenuButtonAudio>() == null)
                        button.gameObject.AddComponent<LSO_MenuButtonAudio>();
            }

            // 스테이지 데이터가 선택한 음악을 추가 UI 씬 로딩으로 덮어쓰지 않는다.
            LDY_StageDirector stages = FindFirstObjectByType<LDY_StageDirector>();
            if (stages != null && stages.CurrentStage != null)
                LSO_CombatAudio.Stage(stages.CurrentStage);
            else if (FindFirstObjectByType<LDY_BoardManager>() != null)
                LSO_GameAudio.Music(LSO_SoundCue.BattleMusic);
            else if (FindFirstObjectByType<LSO_MenuActions>() != null)
                LSO_GameAudio.Music(LSO_SoundCue.MenuMusic);
            else LSO_GameAudio.StopMusic();
        }

        private void OnAbility(LSO_AbilityFired fired)
        {
            switch (fired.Type)
            {
                case LSO_AbilityType.Sturdy: LSO_GameAudio.Play(LSO_SoundCue.Gecko); break;
                case LSO_AbilityType.CurseImmunity: LSO_GameAudio.Play(LSO_SoundCue.Kiwi); break;
                case LSO_AbilityType.Thorns: LSO_GameAudio.Play(LSO_SoundCue.Thorns); break;
                case LSO_AbilityType.PackTactics: LSO_GameAudio.Play(LSO_SoundCue.Wolf, 104, 0.5f); break;
                case LSO_AbilityType.PreyMarking:
                case LSO_AbilityType.Predation:
                    LSO_GameAudio.Play(LSO_SoundCue.Crow, 105, 0.5f); break;
            }
        }
    }
}
