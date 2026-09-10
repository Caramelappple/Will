using System;
using System.Collections;
using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LDY.Stage;
using _Scripts.LSO.Camera;
using _Scripts.LSO.Stage;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.DLJ.SceneFlow
{
    /// <summary>기존 보상/배치 흐름의 Ready를 받아 화면 연출만 담당한다.</summary>
    [DisallowMultipleComponent]
    public sealed class DLJ_StagePresentation : MonoBehaviour
    {
        [Serializable]
        public sealed class ChapterLook
        {
            public LSO_ChapterSO chapter;
            public string bossName;
            public string bossEpithet;
            public Color lightTile = new Color(0.6f, 0.5f, 0.36f);
            public Color darkTile = new Color(0.16f, 0.12f, 0.1f);
            [Tooltip("별도 배경이 있다면 지정. 완전 암전 중 해당 챕터 배경만 켠다.")]
            public GameObject environment;
        }

        [Header("씬 연결")]
        [SerializeField] private LSO_StageIntroDirector intro;
        [SerializeField] private LDY_StageDirector stages;
        [SerializeField] private LSO_StageProgression progression;
        [SerializeField] private LSO_StageFlow flow;
        [SerializeField] private LDY_BoardManager board;
        [SerializeField] private TMP_FontAsset font;
        [Tooltip("동적 한글 글꼴 원본. 새 지역명과 공백도 런타임에 생성한다.")]
        [SerializeField] private Font sourceFont;
        [SerializeField] private ChapterLook[] chapterLooks = Array.Empty<ChapterLook>();

        [Header("연출 시간 (초)")]
        [SerializeField, Min(0.1f)] private float normalHold = 1.5f;
        [SerializeField, Min(0.1f)] private float glitchDuration = 0.65f;
        [SerializeField, Min(0.1f)] private float titleHold = 1.8f;
        [SerializeField, Min(0.1f)] private float fadeDuration = 0.45f;

        [Header("보스 등장")]
        [Tooltip("끄면 이명과 보스 이름을 바로 표시하고 타자, 타격, 흔들림 효과를 생략한다.")]
        [SerializeField, InspectorName("보스 텍스트 효과 사용")] private bool useBossTextEffect = true;
        [SerializeField, Min(0.01f)] private float bossTypeInterval = 0.045f;
        [Tooltip("이명 타자가 끝난 뒤 보스 이름 첫 타격까지 기다리는 시간.")]
        [SerializeField, Min(0f), InspectorName("이명-이름 대기 시간")] private float bossTitleDelay = 0.12f;
        [SerializeField, Min(0f)] private float bossBeatInterval = 0.16f;
        [SerializeField, Min(0.05f)] private float bossPunchDuration = 0.18f;
        [SerializeField, Min(0f)] private float bossShakeDuration = 0.2f;
        [SerializeField, Min(0f)] private float bossShakeStrength = 0.14f;
        [SerializeField, Min(0f)] private float bossUiShake = 10f;

        private Canvas canvas;
        private Image veil;
        private CanvasGroup badge;
        private CanvasGroup titleGroup;
        private TMP_Text stageLabel;
        private TMP_Text title;
        private TMP_Text subtitle;
        private RectTransform titleRoot;
        private readonly List<Image> noise = new List<Image>();
        private readonly List<Behaviour> disabledInputs = new List<Behaviour>();
        private readonly List<CinemachineImpulseListener> ownedImpulseListeners = new List<CinemachineImpulseListener>();
        private Coroutine routine;
        private LSO_ChapterSO lastChapter;
        private bool inputHeld;
        private TMP_FontAsset runtimeFont;
        public bool IsPlaying { get; private set; }

        private void Awake()
        {
            if (sourceFont != null)
            {
                runtimeFont = TMP_FontAsset.CreateFontAsset(sourceFont);
                runtimeFont.name = "DLJ_StageFont (Runtime)";
                font = runtimeFont;
            }
            BuildUI();
            EnsureImpulseListeners();
        }

        private void OnEnable()
        {
            if (intro != null) intro.Ready += OnReady;
            if (stages != null) stages.OnStageLoaded += OnLoaded;
        }

        private IEnumerator Start()
        {
            // 보드 등록과 보상 상자의 Awake/Start가 모두 끝난 후 첫 판을 세운다.
            yield return null;
            if (flow != null && stages != null && stages.CurrentStage == null)
                flow.StartRun();
        }

        private void OnDisable()
        {
            if (intro != null) intro.Ready -= OnReady;
            if (stages != null) stages.OnStageLoaded -= OnLoaded;
            if (routine != null) StopCoroutine(routine);
            routine = null;
            ResetUI();
            ReleaseInput();
            IsPlaying = false;
        }

        private void OnDestroy()
        {
            // 이 테스트 씬이 만든 진행 객체가 다른 씬의 실행을 가로막지 않게 정리한다.
            if (flow != null) Destroy(flow.gameObject);
            if (progression != null) Destroy(progression.gameObject);
            if (runtimeFont != null)
            {
                foreach (Texture2D atlas in runtimeFont.atlasTextures)
                    if (atlas != null) Destroy(atlas);
                Destroy(runtimeFont.material);
                Destroy(runtimeFont);
            }
            foreach (CinemachineImpulseListener listener in ownedImpulseListeners)
                if (listener != null) Destroy(listener);
        }

        private void OnLoaded(LDY_StageSO stage)
        {
            // 다음 판 배치 신호는 아직 뒤집힌 보드에서 온다. Ready까지 기다린다.
            if (intro != null && intro.IsPlaying) return;
            OnReady(stage);
        }

        private void OnReady(LDY_StageSO stage)
        {
            if (routine != null) StopCoroutine(routine);
            ResetUI();
            ReleaseInput();
            routine = StartCoroutine(Present(stage));
        }

        private IEnumerator Present(LDY_StageSO stage)
        {
            IsPlaying = true;
            HoldInput();
            bool finished = progression == null || progression.IsRunFinished;
            LSO_ChapterSO chapter = finished ? null : progression.Chapter;
            bool chapterChanged = lastChapter != null && lastChapter != chapter;
            ChapterLook look = FindLook(chapter);
            stageLabel.text = finished ? "여정의 끝" : $"{progression.ChapterNumber}-{progression.StageNumber}";

            try
            {
                if (finished)
                {
                    yield return FadeVeil(Color.black, 0f, 1f);
                    SetTitle("여정의 끝", "모든 스테이지 클리어", Color.white);
                    yield return FadeGroup(titleGroup, 0f, 1f);
                    // 마지막 판은 재시작하지 않는다. 씬을 나갈 때 잠금도 정리된다.
                    yield break;
                }

                if (progression.IsBoss)
                {
                    yield return FadeVeil(Color.white, 0f, 1f);
                    ApplyLook(look);
                    string boss = look != null && !string.IsNullOrWhiteSpace(look.bossName)
                        ? look.bossName : stage != null ? stage.stageName : "보스";
                    yield return PlayBossTitle(boss, look != null ? look.bossEpithet : string.Empty);
                    yield return new WaitForSecondsRealtime(titleHold);
                    yield return FadeGroup(titleGroup, 1f, 0f);
                    yield return FadeVeil(Color.white, 1f, 0f);
                }
                else if (chapterChanged)
                {
                    yield return GlitchToBlack();
                    // 배경과 보드 색은 화면이 완전히 덮인 이 시점에만 바꾼다.
                    ApplyLook(look);
                    yield return new WaitForSecondsRealtime(0.25f);
                    SetTitle(chapter.regionName, $"CHAPTER {progression.ChapterNumber:00}", Color.white);
                    yield return FadeGroup(titleGroup, 0f, 1f);
                    yield return new WaitForSecondsRealtime(titleHold);
                    yield return FadeGroup(titleGroup, 1f, 0f);
                    yield return FadeVeil(Color.black, 1f, 0f);
                }
                else
                {
                    if (lastChapter == null) ApplyLook(look);
                    yield return FadeGroup(badge, 0f, 1f);
                    yield return new WaitForSecondsRealtime(normalHold);
                    yield return FadeGroup(badge, 1f, 0f);
                }
            }
            finally
            {
                lastChapter = chapter;
                IsPlaying = false;
                routine = null;
                if (!finished)
                {
                    ResetUI();
                    ReleaseInput();
                }
            }
        }

        private ChapterLook FindLook(LSO_ChapterSO chapter)
        {
            foreach (ChapterLook look in chapterLooks)
                if (look != null && look.chapter == chapter) return look;
            return null;
        }

        private void ApplyLook(ChapterLook look)
        {
            if (look == null) return;
            foreach (ChapterLook item in chapterLooks)
                if (item != null && item.environment != null) item.environment.SetActive(item == look);
            if (board == null) return;
            var properties = new MaterialPropertyBlock();
            foreach (Renderer tile in board.BoardRoot.GetComponentsInChildren<Renderer>(true))
            {
                // 기물과 보상은 같은 루트에 잠시 붙는다. 타일만 변경한다.
                string[] coordinates = tile.name.Split('_');
                if (coordinates.Length != 3 || coordinates[0] != "Tile" ||
                    !int.TryParse(coordinates[1], out int x) || !int.TryParse(coordinates[2], out int z)) continue;
                tile.GetPropertyBlock(properties);
                Color color = (x + z) % 2 == 0 ? look.lightTile : look.darkTile;
                properties.SetColor("_BaseColor", color);
                properties.SetColor("_Color", color);
                tile.SetPropertyBlock(properties);
                properties.Clear();
            }
        }

        private IEnumerator GlitchToBlack()
        {
            float elapsed = 0f;
            int tick = -1;
            while (elapsed < glitchDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / glitchDuration);
                veil.color = new Color(0f, 0f, 0f, progress);
                int nextTick = Mathf.FloorToInt(elapsed * 12f);
                if (nextTick != tick)
                {
                    tick = nextTick;
                    for (int i = 0; i < noise.Count; i++)
                    {
                        Image strip = noise[i];
                        float y = Mathf.Repeat(i * 0.137f + tick * 0.173f, 1f);
                        strip.rectTransform.anchorMin = new Vector2(0f, y);
                        strip.rectTransform.anchorMax = new Vector2(1f, Mathf.Min(1f, y + 0.012f + i * 0.004f));
                        strip.color = new Color(0f, 0f, 0f, 0.45f + progress * 0.55f);
                    }
                }
                yield return null;
            }
            foreach (Image strip in noise) strip.color = Color.clear;
            veil.color = Color.black;
        }

        private IEnumerator FadeVeil(Color color, float from, float to)
        {
            for (float elapsed = 0f; elapsed < fadeDuration; elapsed += Time.unscaledDeltaTime)
            {
                color.a = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / fadeDuration));
                veil.color = color;
                yield return null;
            }
            color.a = to;
            veil.color = color;
        }

        private IEnumerator FadeGroup(CanvasGroup group, float from, float to)
        {
            for (float elapsed = 0f; elapsed < fadeDuration; elapsed += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / fadeDuration));
                yield return null;
            }
            group.alpha = to;
        }

        private IEnumerator PlayBossTitle(string boss, string epithet)
        {
            SetTitle(boss, epithet, Color.black);
            titleGroup.alpha = 1f;
            if (!useBossTextEffect) yield break;

            title.maxVisibleCharacters = 0;
            subtitle.maxVisibleCharacters = 0;

            subtitle.ForceMeshUpdate();
            int subtitleCharacters = subtitle.textInfo.characterCount;
            for (int i = 1; i <= subtitleCharacters; i++)
            {
                subtitle.maxVisibleCharacters = i;
                yield return new WaitForSecondsRealtime(bossTypeInterval);
            }

            if (bossTitleDelay > 0f)
                yield return new WaitForSecondsRealtime(bossTitleDelay);

            title.ForceMeshUpdate();
            int nameCharacters = Mathf.Max(1, title.textInfo.characterCount);
            int beats = Mathf.Min(3, nameCharacters);
            for (int beat = 1; beat <= beats; beat++)
            {
                title.maxVisibleCharacters = Mathf.CeilToInt(nameCharacters * beat / (float)beats);
                float weight = Mathf.Lerp(0.72f, 1f, beat / (float)beats);
                LSO_CameraImpulse.Shake(bossShakeDuration, bossShakeStrength * weight);
                yield return PunchBossTitle(weight);
                if (beat < beats && bossBeatInterval > 0f)
                    yield return new WaitForSecondsRealtime(bossBeatInterval);
            }

            title.maxVisibleCharacters = int.MaxValue;
            subtitle.maxVisibleCharacters = int.MaxValue;
            titleRoot.anchoredPosition = Vector2.zero;
            title.rectTransform.localScale = Vector3.one;
        }

        private IEnumerator PunchBossTitle(float weight)
        {
            float duration = Mathf.Max(0.05f, bossPunchDuration);
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float progress = Mathf.Clamp01(elapsed / duration);
                float envelope = 1f - progress;
                float punch = Mathf.Sin(progress * Mathf.PI) * 0.3f * weight;
                title.rectTransform.localScale = Vector3.one * (1f + punch);

                float wave = Mathf.Sin(progress * Mathf.PI * 8f);
                float vertical = Mathf.Cos(progress * Mathf.PI * 6f);
                titleRoot.anchoredPosition = new Vector2(wave, vertical) * (bossUiShake * envelope * weight);
                yield return null;
            }

            titleRoot.anchoredPosition = Vector2.zero;
            title.rectTransform.localScale = Vector3.one;
        }

        private void SetTitle(string main, string secondary, Color color)
        {
            title.text = main;
            title.color = color;
            title.maxVisibleCharacters = int.MaxValue;
            subtitle.text = secondary;
            subtitle.color = color;
            subtitle.maxVisibleCharacters = int.MaxValue;
            if (titleRoot != null) titleRoot.anchoredPosition = Vector2.zero;
            if (title != null) title.rectTransform.localScale = Vector3.one;
        }

        private void EnsureImpulseListeners()
        {
            foreach (CinemachineCamera camera in FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None))
            {
                CinemachineImpulseListener listener = camera.GetComponent<CinemachineImpulseListener>();
                if (listener != null) continue;
                listener = camera.gameObject.AddComponent<CinemachineImpulseListener>();
                listener.ChannelMask = 1;
                listener.Gain = 1f;
                ownedImpulseListeners.Add(listener);
            }
        }

        private void HoldInput()
        {
            inputHeld = true;
            veil.raycastTarget = true;
            foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (!(behaviour is LDY_SelectionController) && !(behaviour is LDY_CardPlacer) &&
                    behaviour.GetType().Name != "LSO_ButtonClickHandler") continue;
                if (!behaviour.enabled) continue;
                behaviour.enabled = false;
                disabledInputs.Add(behaviour);
            }
        }

        private void ReleaseInput()
        {
            if (!inputHeld) return;
            inputHeld = false;
            foreach (Behaviour behaviour in disabledInputs)
                if (behaviour != null) behaviour.enabled = true;
            disabledInputs.Clear();
            if (veil != null) veil.raycastTarget = false;
        }

        private void ResetUI()
        {
            if (veil == null) return;
            veil.color = Color.clear;
            badge.alpha = 0f;
            titleGroup.alpha = 0f;
            titleRoot.anchoredPosition = Vector2.zero;
            title.rectTransform.localScale = Vector3.one;
            title.maxVisibleCharacters = int.MaxValue;
            subtitle.maxVisibleCharacters = int.MaxValue;
            foreach (Image strip in noise) strip.color = Color.clear;
        }

        private void BuildUI()
        {
            var root = new GameObject("DLJ_StageOverlay", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            veil = MakeImage("Veil", root.transform, Vector2.zero, Vector2.one, Color.clear);
            for (int i = 0; i < 12; i++)
                noise.Add(MakeImage("Noise_" + i, root.transform, Vector2.zero, Vector2.one, Color.clear));
            Image panel = MakeImage("StageBadge", root.transform, new Vector2(0.42f, 0.89f),
                new Vector2(0.58f, 0.95f), new Color(0.035f, 0.03f, 0.025f, 0.94f));
            badge = panel.gameObject.AddComponent<CanvasGroup>();
            stageLabel = MakeText("Stage", panel.transform, Vector2.zero, Vector2.one, 32f);
            var center = new GameObject("Title", typeof(RectTransform), typeof(CanvasGroup));
            center.transform.SetParent(root.transform, false);
            titleRoot = center.GetComponent<RectTransform>();
            Stretch(titleRoot, Vector2.zero, Vector2.one);
            titleGroup = center.GetComponent<CanvasGroup>();
            subtitle = MakeText("Epithet", center.transform, new Vector2(0.12f, 0.54f), new Vector2(0.88f, 0.62f), 32f);
            title = MakeText("Name", center.transform, new Vector2(0.1f, 0.37f), new Vector2(0.9f, 0.54f), 90f);
            title.characterSpacing = 12f;
            ResetUI();
        }

        private TMP_Text MakeText(string objectName, Transform parent, Vector2 min, Vector2 max, float size)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>(), min, max);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = font != null ? font : TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = size * 0.45f;
            text.fontSizeMax = size;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.95f, 0.92f, 0.86f);
            text.raycastTarget = false;
            text.richText = false;
            return text;
        }

        private static Image MakeImage(string objectName, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>(), min, max);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
