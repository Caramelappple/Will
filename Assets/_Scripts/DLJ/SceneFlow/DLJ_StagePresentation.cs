using System;
using System.Collections;
using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LDY.Stage;
using _Scripts.LSO.Stage;
using _Scripts.LSO.Boss;
using TMPro;
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

        [Serializable]
        public sealed class StageDecoration
        {
            [Tooltip("이 장식을 표시할 스테이지 에셋.")]
            public LDY_StageSO stage;
            [Tooltip("해당 스테이지에서만 켤 씬 장식 또는 프리팹. 프리팹은 저장된 위치에 한 번 생성해 재사용한다.")]
            public GameObject[] objects = Array.Empty<GameObject>();
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

        [Header("스테이지별 장식")]
        [Tooltip("현재 스테이지에 등록된 장식만 켠다. 목록에 없는 오브젝트는 변경하지 않는다.")]
        [SerializeField] private StageDecoration[] stageDecorations = Array.Empty<StageDecoration>();

        [Header("연출 시간 (초)")]
        [SerializeField, Min(0.1f)] private float normalHold = 1.5f;
        [SerializeField, Min(0.1f)] private float glitchDuration = 0.65f;
        [SerializeField, Min(0.1f)] private float titleHold = 1.8f;
        [SerializeField, Min(0.1f)] private float fadeDuration = 0.45f;

        [Header("보스 등장")]
        [Tooltip("이름 전체가 부드럽게 나타난 뒤 불타듯 사라지는 효과를 사용한다.")]
        [SerializeField, InspectorName("보스 텍스트 효과 사용")] private bool useBossTextEffect = true;
        [SerializeField, Min(0.1f), InspectorName("보스 이름 등장 시간")] private float bossRevealDuration = 0.8f;
        [SerializeField, Min(0.1f), InspectorName("보스 이름 연소 시간")] private float bossBurnDuration = 1.1f;
        [SerializeField, InspectorName("보스 화면 울림 사용")] private bool useBossRoarRipple = true;
        [SerializeField, Min(0.1f), InspectorName("보스 화면 울림 시간")] private float bossRippleDuration = 1.6f;
        [SerializeField, Range(0f, 0.03f), InspectorName("보스 화면 울림 강도")] private float bossRippleStrength = 0.009f;
        [SerializeField, InspectorName("보스 화면 떨림 사용")] private bool useBossScreenShake = true;
        [SerializeField, Min(0.1f), InspectorName("보스 화면 떨림 시간")] private float bossScreenShakeDuration = 1.2f;
        [SerializeField, Range(0f, 0.02f), InspectorName("보스 화면 떨림 강도")] private float bossScreenShakeStrength = 0.006f;
        [SerializeField, Range(1f, 40f), InspectorName("보스 화면 떨림 속도")] private float bossScreenShakeFrequency = 24f;
        [SerializeField, InspectorName("보스 중심 암전 사용")] private bool useBossFocusDimming = true;
        [SerializeField, Min(0.01f), InspectorName("보스 완전 암전 유지 시간")] private float bossBlackHold = 0.2f;
        [SerializeField, Min(0.01f), InspectorName("보스 실루엣 등장 시간")] private float bossSilhouetteDuration = 0.35f;
        [SerializeField, Min(0f), InspectorName("보스 포효 전 대기 시간")] private float bossRoarDelay = 0.3f;
        [SerializeField, Min(0.1f), InspectorName("보스 빠르게 밝아지는 시간")] private float bossBrightenDuration = 0.3f;
        [SerializeField, Range(0f, 0.95f), InspectorName("보스 중심 암전 강도")] private float bossDimmingStrength = 0.7f;
        [SerializeField, Range(0.05f, 0.5f), InspectorName("보스 중심 밝은 영역")] private float bossFocusRadius = 0.22f;

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
        private DLJ_BossTitleEffect bossTitleEffect;
        private readonly DLJ_BossRoarRipple bossRoarRipple = new DLJ_BossRoarRipple();
        private readonly Dictionary<GameObject, GameObject> decorationInstances = new Dictionary<GameObject, GameObject>();
        private Transform decorationRoot;
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
            ApplyStageDecorations(null);
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
            bossRoarRipple.Dispose();
            bossTitleEffect?.Dispose();
            if (decorationRoot != null) Destroy(decorationRoot.gameObject);
            decorationInstances.Clear();
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
                    ApplyStageDecorations(null);
                    SetTitle("여정의 끝", "모든 스테이지 클리어", Color.white);
                    yield return FadeGroup(titleGroup, 0f, 1f);
                    // 마지막 판은 재시작하지 않는다. 씬을 나갈 때 잠금도 정리된다.
                    yield break;
                }

                if (progression.IsBoss)
                {
                    if (useBossFocusDimming) veil.color = Color.black;
                    ApplyLook(look);
                    ApplyStageDecorations(stage);
                    Transform bossFocus = ResolveBossFocus();
                    if (useBossFocusDimming)
                    {
                        bossRoarRipple.Prepare(bossFocus, bossDimmingStrength, bossFocusRadius);
                        yield return new WaitForSecondsRealtime(bossBlackHold);
                        yield return FadeVeil(Color.black, 1f, 0f, bossSilhouetteDuration);
                        yield return new WaitForSecondsRealtime(bossRoarDelay);
                    }

                    // 실루엣 대기가 끝난 같은 프레임에 밝아짐·포효·이름을 시작한다.
                    var cue = bossFocus != null && bossFocus.GetComponent<_Scripts.LDY.Boss.BullKing.LDY_BullKingBoss>() != null
                        ? _Scripts.LSO.Sound.LSO_SoundCue.BullCry
                        : _Scripts.LSO.Sound.LSO_SoundCue.StageIntro;
                    _Scripts.LSO.Sound.LSO_GameAudio.Play(cue);
                    if (useBossRoarRipple || useBossScreenShake || useBossFocusDimming)
                        bossRoarRipple.Play(bossRippleDuration, useBossRoarRipple ? bossRippleStrength : 0f,
                            bossScreenShakeDuration, useBossScreenShake ? bossScreenShakeStrength : 0f,
                            bossScreenShakeFrequency, bossFocus, bossBrightenDuration,
                            useBossFocusDimming ? bossDimmingStrength : 0f, bossFocusRadius);
                    yield return PlayBossTitle(
                        ResolveBossName(chapter, look),
                        look != null ? look.bossEpithet : string.Empty);
                    yield return new WaitForSecondsRealtime(titleHold);
                    if (useBossTextEffect)
                        yield return bossTitleEffect.Burn(bossBurnDuration);
                    else
                        yield return FadeGroup(titleGroup, 1f, 0f);
                }
                else if (chapterChanged)
                {
                    yield return GlitchToBlack();
                    // 배경과 보드 색은 화면이 완전히 덮인 이 시점에만 바꾼다.
                    ApplyLook(look);
                    ApplyStageDecorations(stage);
                    yield return new WaitForSecondsRealtime(0.25f);
                    SetTitle(chapter.regionName, $"CHAPTER {progression.ChapterNumber:00}", Color.white);
                    _Scripts.LSO.Sound.LSO_GameAudio.Play(_Scripts.LSO.Sound.LSO_SoundCue.StageIntro);
                    yield return FadeGroup(titleGroup, 0f, 1f);
                    yield return new WaitForSecondsRealtime(titleHold);
                    yield return FadeGroup(titleGroup, 1f, 0f);
                    yield return FadeVeil(Color.black, 1f, 0f);
                }
                else
                {
                    if (lastChapter == null) ApplyLook(look);
                    ApplyStageDecorations(stage);
                    _Scripts.LSO.Sound.LSO_GameAudio.Play(_Scripts.LSO.Sound.LSO_SoundCue.StageIntro);
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

        private void ApplyStageDecorations(LDY_StageSO stage)
        {
            if (stageDecorations == null) return;

            // 공유 장식은 현재 스테이지의 어느 항목에든 있으면 유지한다.
            // 먼저 최종 상태를 모아 같은 오브젝트를 껐다 켜지 않도록 한다.
            var visibility = new Dictionary<GameObject, bool>();
            foreach (StageDecoration entry in stageDecorations)
            {
                if (entry == null || entry.objects == null) continue;
                bool show = stage != null && entry.stage == stage;
                foreach (GameObject decoration in entry.objects)
                {
                    if (decoration == null) continue;
                    // 연출 자신이나 상위 루트를 끄면 진행 코루틴까지 중단된다.
                    if (transform.IsChildOf(decoration.transform)) continue;
                    visibility.TryGetValue(decoration, out bool alreadyVisible);
                    visibility[decoration] = alreadyVisible || show;
                }
            }

            foreach (KeyValuePair<GameObject, bool> item in visibility)
            {
                GameObject decoration = ResolveDecoration(item.Key, item.Value);
                if (decoration != null && decoration.activeSelf != item.Value)
                    decoration.SetActive(item.Value);
            }
        }

        private GameObject ResolveDecoration(GameObject source, bool show)
        {
            if (source == null) return null;
            if (source.scene.IsValid()) return source;

            // Project의 프리팹 에셋에 SetActive를 호출해도 씬에는 나타나지 않는다.
            // 같은 프리팹을 여러 스테이지에 등록해도 인스턴스는 하나만 만든다.
            if (decorationInstances.TryGetValue(source, out GameObject instance) && instance != null)
                return instance;
            if (!show) return null;

            if (decorationRoot == null)
            {
                decorationRoot = new GameObject("DLJ_StageDecorations (Runtime)").transform;
                // 프리팹에 저장된 배치를 유지하고 이 연출 객체와 함께 정리한다.
                decorationRoot.SetParent(transform, true);
            }

            instance = Instantiate(source, decorationRoot, false);
            instance.name = source.name + " (Runtime)";
            decorationInstances[source] = instance;
            return instance;
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

        private IEnumerator FadeVeil(Color color, float from, float to, float seconds = -1f)
        {
            float duration = seconds < 0f ? fadeDuration : Mathf.Max(0.01f, seconds);
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                color.a = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
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

        /// <summary>
        /// 화면에 띄울 보스 이름.
        ///
        /// ── 이름을 들고 있는 곳은 Chapter Looks 하나다 ────────────
        /// 예전에는 못 찾으면 stage.stageName 으로 떨어졌다. 그런데 스테이지 이름은
        /// "1-6 보스 · 황소왕" 처럼 자리와 이름이 섞인 값이라, 그대로 띄우면
        /// 화면에 "1-6 보스 · 황소왕" 이 통째로 나온다.
        ///
        /// 더 나쁜 것은 **그것이 그럴듯해 보인다**는 점이다. 배선이 끊긴 줄 모르고
        /// 넘어가게 된다. 실제로 챕터 에셋이 바뀌면서 Chapter Looks 가 옛 에셋을
        /// 가리키고 있었는데, 화면에는 이름이 떠서 한참 동안 드러나지 않았다.
        ///
        /// 그래서 대신 쓰지 않는다. 못 찾으면 못 찾았다고 남기고 눈에 띄게 둔다.
        /// ─────────────────────────────────────────────────────────
        /// </summary>
        private string ResolveBossName(LSO_ChapterSO chapter, ChapterLook look)
        {
            if (look == null)
            {
                Debug.LogWarning(
                    $"{name}: '{(chapter != null ? chapter.name : "챕터 없음")}' 의 Chapter Look 을 찾지 못했습니다. " +
                    "Chapter Looks 에 그 챕터 에셋이 등록돼 있는지 확인하세요 — " +
                    "LSO_StageProgression 이 쓰는 것과 **같은 에셋**이어야 합니다.", this);

                return "???";
            }

            if (string.IsNullOrWhiteSpace(look.bossName))
            {
                Debug.LogWarning(
                    $"{name}: '{chapter?.name}' 의 Boss Name 이 비어 있습니다. " +
                    "Chapter Looks 에서 이름을 적어 주세요.", this);

                return "???";
            }

            return look.bossName;
        }

        private Transform ResolveBossFocus()
        {
            LDY_BoardManager currentBoard = board != null ? board : FindAnyObjectByType<LDY_BoardManager>();
            if (currentBoard != null)
            {
                foreach (LDY_Animal enemy in currentBoard.GetAllByTeam(LDY_Team.Enemy))
                    if (enemy != null && enemy.gameObject.activeInHierarchy && !enemy.IsDeathProcessing &&
                        enemy.TryGetComponent<LSO_BossPhase>(out _))
                        return enemy.transform;
            }
            Debug.LogWarning($"{name}: 현재 보드의 보스를 찾지 못해 화면 중앙에서 등장 효과를 재생합니다.", this);
            return null;
        }

        private IEnumerator PlayBossTitle(string boss, string epithet)
        {
            SetTitle(boss, epithet, stageLabel.color);
            SetTitleLayout(true);
            if (useBossTextEffect)
                yield return bossTitleEffect.Reveal(bossRevealDuration);
            else
                titleGroup.alpha = 1f;
        }

        private void SetTitle(string main, string secondary, Color color)
        {
            SetTitleLayout(false);
            title.text = main;
            title.color = color;
            title.maxVisibleCharacters = int.MaxValue;
            subtitle.text = secondary;
            subtitle.color = color;
            subtitle.maxVisibleCharacters = int.MaxValue;
            if (titleRoot != null) titleRoot.anchoredPosition = Vector2.zero;
            if (title != null) title.rectTransform.localScale = Vector3.one;
        }

        private void SetTitleLayout(bool isBoss)
        {
            if (isBoss)
            {
                // 스테이지 번호와 같은 영역을 사용해 해상도가 바뀌어도 위치를 맞춘다.
                var badgeRect = (RectTransform)badge.transform;
                Stretch(titleRoot, badgeRect.anchorMin, badgeRect.anchorMax);
                Stretch(subtitle.rectTransform, new Vector2(0f, 0.65f), new Vector2(1f, 1.1f));
                Stretch(title.rectTransform, new Vector2(0f, -0.1f), new Vector2(1f, 0.7f));
            }
            else
            {
                Stretch(titleRoot, Vector2.zero, Vector2.one);
                Stretch(subtitle.rectTransform, new Vector2(0.12f, 0.54f), new Vector2(0.88f, 0.62f));
                Stretch(title.rectTransform, new Vector2(0.1f, 0.37f), new Vector2(0.9f, 0.54f));
            }

            float mainSize = isBoss ? 36f : 90f;
            float secondarySize = isBoss ? 18f : 32f;
            title.fontSize = title.fontSizeMax = mainSize;
            title.fontSizeMin = mainSize * 0.45f;
            subtitle.fontSize = subtitle.fontSizeMax = secondarySize;
            subtitle.fontSizeMin = secondarySize * 0.45f;
            title.characterSpacing = isBoss ? 4f : 12f;
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
            bossRoarRipple.Stop();
            bossTitleEffect?.Reset();
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
            bossTitleEffect = new DLJ_BossTitleEffect(titleRoot, titleGroup, title, subtitle);
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
