#if UNITY_EDITOR
using System;
using System.Linq;
using _Scripts.LSO.Sound;
using DevLib.ServiceLocator;
using DevLib.SoundSystem.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.LSO.Editor
{
    [InitializeOnLoad]
    public static class LSO_AudioValidation
    {
        private const string Pending = "WillAudio.BatchValidation";

        static LSO_AudioValidation()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        [MenuItem("Tools/Will/Validate Audio")]
        public static void Validate()
        {
            foreach (LSO_SoundCue cue in Enum.GetValues(typeof(LSO_SoundCue)))
            {
                SoundClipSO data = Resources.Load<SoundClipSO>("WillAudio/Clips/" + cue);
                Require(data != null && data.clip != null, cue + " clip reference");
                Require(data.clip.samples > 0 && data.clip.length > 0, cue + " imported audio");
                Require(data.volume > 0 && data.pitch > 0, cue + " playback settings");
                bool music = cue.ToString().EndsWith("Music", StringComparison.Ordinal);
                Require(data.isLoop == music, cue + " loop setting");
                if (music) Require(data.clip.loadType == AudioClipLoadType.Streaming, cue + " streaming");
            }
            GameObject prefab = Resources.Load<GameObject>("WillAudio/AudioService");
            Require(prefab != null, "service prefab");
            AudioService service = prefab.GetComponent<AudioService>();
            Require(service != null, "service component");
            var serialized = new SerializedObject(service);
            var player = serialized.FindProperty("soundPlayerPrefab").objectReferenceValue as GameObject;
            Require(player != null && player.GetComponent<SoundPlayer>() != null, "pooled SoundPlayer");
            var speaker = new SerializedObject(player.GetComponent<SoundPlayer>());
            Require(speaker.FindProperty("sfxGroup").objectReferenceValue != null, "SFX mixer");
            Require(speaker.FindProperty("musicGroup").objectReferenceValue != null, "music mixer");
            Debug.Log("WILL_AUDIO_ASSETS_PASS: 34 clips, service prefab, SFX/BGM mixer references.");
        }

        // Batch-only isolated empty scene. Never changes the user's saved scenes.
        public static void RunBatch()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use Validate Audio in the editor.");
            try
            {
                Validate();
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                SessionState.SetBool(Pending, true);
                EditorApplication.EnterPlaymode();
            }
            catch (Exception error) { Fail(error); }
        }

        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (!Application.isBatchMode || !SessionState.GetBool(Pending, false) ||
                state != PlayModeStateChange.EnteredPlayMode) return;
            SessionState.SetBool(Pending, false);
            try
            {
                new GameObject("Audio validation listener").AddComponent<AudioListener>();
                Require(LSO_AudioRuntime.Ensure(), "runtime bootstrap");
                IAudioService service = ServiceLocator.Get<IAudioService>();
                Require(service is AudioService, "registered audio service");
                var owner = (AudioService)service;
                Require(AudioService.Active == owner, "single active audio service");
                var duplicate = new GameObject("Duplicate audio service check").AddComponent<AudioService>();
                Require(AudioService.Active == owner && ReferenceEquals(service, ServiceLocator.Get<IAudioService>()),
                    "duplicate service never replaces active service");
                UnityEngine.Object.DestroyImmediate(duplicate.gameObject);
                foreach (LSO_SoundCue cue in Enum.GetValues(typeof(LSO_SoundCue)))
                {
                    SoundClipSO clip = LSO_GameAudio.Clip(cue);
                    if (clip.isLoop)
                    {
                        LSO_GameAudio.Music(cue);
                        LSO_GameAudio.Music(cue);
                        Require(owner.GetComponentsInChildren<AudioSource>().Count(s => s.clip == clip.clip && s.loop) == 1,
                            cue + " single BGM source");
                    }
                    else
                    {
                        service.PlaySfx(clip, 901);
                        Require(owner.GetComponentsInChildren<AudioSource>().Any(s => s.clip == clip.clip), cue + " assigned playback");
                        service.StopSfx(901);
                        Require(!owner.GetComponentsInChildren<AudioSource>().Any(s => s.clip == clip.clip), cue + " pool return");
                    }
                }
                ValidateInteractionAudio(service);
                LSO_GameAudio.Music(LSO_SoundCue.MenuMusic);
                LSO_GameAudio.Music(LSO_SoundCue.BattleMusic);
                LSO_GameAudio.Music(LSO_SoundCue.MenuMusic);
                Require(owner.GetComponentsInChildren<AudioSource>().Count(s => s.clip != null && s.loop) == 1,
                    "menu/battle/menu uses one BGM source");
                Scene previous = SceneManager.GetActiveScene();
                Scene next = SceneManager.CreateScene("Audio validation next scene");
                SceneManager.SetActiveScene(next);
                SceneManager.UnloadSceneAsync(previous);
                Require(LSO_AudioRuntime.Ensure() && ReferenceEquals(service, ServiceLocator.Get<IAudioService>()), "service survives scene change");
                Debug.Log("WILL_AUDIO_RUNTIME_PASS: bootstrap, 31 SFX channel play/stop, 3 music loops, service persistence.");
                EditorApplication.Exit(0);
            }
            catch (Exception error) { Fail(error); }
        }

        private sealed class RecordingAudio : IAudioService
        {
            public readonly System.Collections.Generic.List<SoundClipSO> Played = new();
            public void PlaySfx(SoundClipSO clip, int channel = 0) => Played.Add(clip);
            public void StopSfx(int channel) { }
            public void PlayBgm(SoundClipSO clip) { }
            public void StopBgm() { }
            public void Clear()
            {
                Played.Clear();
                // Test each action independently of the anti-overlap cooldown.
                var field = typeof(LSO_GameAudio).GetField("LastPlayed",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                ((System.Collections.IDictionary)field.GetValue(null)).Clear();
            }
            public bool Only(LSO_SoundCue cue) =>
                Played.Count == 1 && Played[0] == LSO_GameAudio.Clip(cue);
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Require(field != null, target.GetType().Name + "." + name);
            field.SetValue(target, value);
        }

        private static void ValidateInteractionAudio(IAudioService service)
        {
            var recorder = new RecordingAudio();
            ServiceLocator.Register<IAudioService>(recorder);
            var root = new GameObject("Audio interaction validation");
            try
            {
                var cardObject = new GameObject("Hand card", typeof(BoxCollider));
                cardObject.transform.SetParent(root.transform);
                var card = cardObject.AddComponent<KTH_HandCard>();
                recorder.Clear();
                card.SetSelected(true);
                card.SetSelected(false);
                card.SetSelected(true);
                Require(recorder.Played.Count == 0, "hover enter/exit stays silent");
                var selection = new KTH_HandCardSelectionController(card, KTH_Axis3D.Y, 0.2f, 0f);
                selection.HandleConfirmClick();
                Require(recorder.Only(LSO_SoundCue.CardDraw), "confirmed card uses card entrance sound");
                recorder.Clear();
                selection.CancelSelectionState();
                Require(recorder.Played.Count == 0, "card cancellation stays silent");

                var willObject = new GameObject("Card will");
                willObject.transform.SetParent(root.transform);
                var cardWill = willObject.AddComponent<_Scripts.LSO.Will.Candle.LSO_CardWill>();
                recorder.Clear();
                cardWill.Apply(_Scripts.LSO.Will.LSO_WillType.Curse, revealNow: false);
                Require(recorder.Only(LSO_SoundCue.WillApply), "applying will plays provided whoosh once");
                recorder.Clear();
                cardWill.Clear();
                Require(recorder.Played.Count == 0, "clearing will stays silent");

                var infoObject = new GameObject("Info window");
                infoObject.transform.SetParent(root.transform);
                var info = infoObject.AddComponent<DLJ_InfoPanelAnimation>();
                SetField(info, "showDuration", 0f);
                SetField(info, "hideDuration", 0f);
                info.HideImmediate();
                recorder.Clear();
                info.Show();
                Require(recorder.Only(LSO_SoundCue.InfoOpen), "actual info window opens with InfoOpen");
                recorder.Clear();
                info.Show();
                Require(recorder.Played.Count == 0, "already open info stays silent");
                info.Replay();
                Require(recorder.Only(LSO_SoundCue.InfoOpen), "info replacement plays on reveal");
                recorder.Clear();
                info.Hide();
                Require(recorder.Only(LSO_SoundCue.InfoOpen), "info closing plays info sound");
                recorder.Clear();
                info.Hide();
                Require(recorder.Played.Count == 0, "already hidden info stays silent");

                var piece = new GameObject("Shared animation");
                piece.transform.SetParent(root.transform);
                recorder.Clear();
                DG.Tweening.TweenExtensions.Complete(
                    new KTH_PlacementAnimation().Play(piece.transform, Vector3.zero, piece), true);
                Require(recorder.Played.Count == 0, "initial placement animation stays silent");

                var boardObject = new GameObject("Board");
                boardObject.transform.SetParent(root.transform);
                var board = boardObject.AddComponent<_Scripts.LDY.LDY_BoardManager>();
                var placer = boardObject.AddComponent<_Scripts.LDY.LDY_CardPlacer>();
                SetField(placer, "board", board);
                var source = AssetDatabase.FindAssets("t:LSO_CardSO")
                    .Select(g => AssetDatabase.LoadAssetAtPath<_Scripts.LSO.Deck.Data.LSO_CardSO>(AssetDatabase.GUIDToAssetPath(g)))
                    .First(c => c.IsValid && c.Animal.unitPrefab != null &&
                        c.Animal.unitPrefab.GetComponent<_Scripts.LDY.LDY_Animal>() != null);
                var data = UnityEngine.Object.Instantiate(source.Animal);
                data.cost = 0;
                data.abilities.Clear();
                var testCard = UnityEngine.Object.Instantiate(source);
                SetField(testCard, "animal", data);
                recorder.Clear();
                var placed = placer.PlaceCard(testCard, _Scripts.LDY.LDY_Team.Player,
                    Vector3Int.zero, _Scripts.LSO.Will.LSO_WillType.None);
                Require(placed != null && recorder.Only(LSO_SoundCue.PieceMove), "successful placement uses PieceMove only");
                recorder.Clear();
                Require(placer.PlaceCard(testCard, _Scripts.LDY.LDY_Team.Player, Vector3Int.zero) == null,
                    "occupied tile rejects placement");
                Require(recorder.Played.Count == 0, "rejected placement stays silent");
                UnityEngine.Object.DestroyImmediate(testCard);
                UnityEngine.Object.DestroyImmediate(data);

                var menu = new GameObject("Menu").AddComponent<_Scripts.LSO.UI.Panel.LSO_MenuActions>();
                menu.transform.SetParent(root.transform);
                var panel = new GameObject("Menu panel");
                panel.transform.SetParent(root.transform);
                SetField(menu, "mainPanel", panel);
                var button = new GameObject("Menu button", typeof(RectTransform), typeof(UnityEngine.UI.Button))
                    .GetComponent<UnityEngine.UI.Button>();
                button.transform.SetParent(panel.transform);
                button.onClick.AddListener(() => panel.SetActive(false));
                var battleButton = new GameObject("Battle button", typeof(RectTransform), typeof(UnityEngine.UI.Button));
                battleButton.transform.SetParent(root.transform);
                var runtime = UnityEngine.Object.FindFirstObjectByType<LSO_AudioRuntime>();
                typeof(LSO_AudioRuntime).GetMethod("ConnectScene",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .Invoke(runtime, new object[] { SceneManager.GetActiveScene() });
                Require(button.GetComponent<LSO_MenuButtonAudio>() != null, "menu button gets menu audio");
                Require(battleButton.GetComponent<LSO_MenuButtonAudio>() == null, "battle button never gets menu audio");
                recorder.Clear();
                button.onClick.Invoke();
                Require(recorder.Only(LSO_SoundCue.MenuClick), "menu click uses menu file");
                Debug.Log("WILL_AUDIO_INTERACTION_PASS: silent hover/cancel, info open/reopen/close, initial placement, successful/rejected placement, menu-only binding.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                ServiceLocator.Register<IAudioService>(service);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("[Will Audio validation] " + message);
        }

        private static void Fail(Exception error)
        {
            SessionState.SetBool(Pending, false);
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }
}
#endif
