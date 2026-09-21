using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform), typeof(RectMask2D))]
public sealed class DLJ_SuccessionNotify : MonoBehaviour
{
    private const string DefaultMessage = "계승받을 아군 기물을 선택하세요";

    [Header("Message")]
    [SerializeField] private TMP_Text label;
    [SerializeField] private string message = DefaultMessage;

    [Header("Reveal")]
    [SerializeField, Min(1f)] private float visibleWidth = 800f;
    [SerializeField, Min(1f)] private float visibleHeight = 120f;
    [SerializeField, Min(0.05f)] private float revealDuration = 0.75f;
    [SerializeField] private bool playOnEnable = true;

    private RectTransform maskRect;
    private Coroutine revealCoroutine;
    private Coroutine UnrevealCoroutine;

    private void Awake()
    {
        maskRect = (RectTransform)transform;
        if (label == null) label = GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = message;
        //SetMaskSize(visibleWidth, visibleHeight);

        if (!Mathf.Approximately(maskRect.pivot.x, 0.5f))
        {
            Debug.LogWarning(
                $"{name}: 중앙에서 펼치려면 RectTransform Pivot X를 0.5로 설정해야 합니다.",
                this);
        }
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            PlayReveal();
        }
    }

    private void OnDisable()
    {
        if (revealCoroutine != null)
        {
            StopCoroutine(revealCoroutine);
            revealCoroutine = null;
        }

        if (UnrevealCoroutine != null)
        {
            StopCoroutine(UnrevealCoroutine);
            UnrevealCoroutine = null;
        }

        SetMaskSize(visibleWidth, visibleHeight);
    }

    public void PlayReveal()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (revealCoroutine != null)
        {
            StopCoroutine(revealCoroutine);
        }

        if (UnrevealCoroutine != null)
        {
            StopCoroutine(UnrevealCoroutine);
            UnrevealCoroutine = null;
        }

        revealCoroutine = StartCoroutine(RevealRoutine());
    }
    
    public void PlayUnreveal()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (UnrevealCoroutine != null)
        {
            StopCoroutine(UnrevealCoroutine);
        }

        if (revealCoroutine != null)
        {
            StopCoroutine(revealCoroutine);
            revealCoroutine = null;
        }

        UnrevealCoroutine = StartCoroutine(UnrevealRoutine());
    }

    public void ShowAndPlay()
    {
        if (label != null) label.text = message;

        bool wasActive = gameObject.activeSelf;
        if (!wasActive)
        {
            gameObject.SetActive(true);
        }

        // 비활성 상태에서 켜졌다면 OnEnable이 대신 재생한다.
        if (wasActive || !playOnEnable)
        {
            PlayReveal();
        }
    }
    
    public void Unable()
    {
        PlayUnreveal();
    }

    /// <summary>씬에 배치된 알림을 쓰고, 없으면 튜토리얼 안내처럼 상단 패널을 만든다.</summary>
    public static void ShowPrompt()
    {
        DLJ_SuccessionNotify notify = FindOrCreate();
        notify?.ShowAndPlay();
    }

    /// <summary>계승 대기 안내를 닫는다.</summary>
    public static void HidePrompt()
    {
        DLJ_SuccessionNotify notify = Object.FindFirstObjectByType<DLJ_SuccessionNotify>(
            FindObjectsInactive.Include);
        notify?.Unable();
    }

    private static DLJ_SuccessionNotify FindOrCreate()
    {
        DLJ_SuccessionNotify existing = Object.FindFirstObjectByType<DLJ_SuccessionNotify>(
            FindObjectsInactive.Include);
        if (existing != null) return existing;

        var canvasObject = new GameObject("Succession Prompt Canvas");
        canvasObject.SetActive(false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        var panelObject = new GameObject("Succession Prompt", typeof(RectTransform));
        panelObject.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect = (RectTransform)panelObject.transform;
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -72f);
        panelRect.sizeDelta = new Vector2(800f, 120f);

        Image background = panelObject.AddComponent<Image>();
        background.color = new Color(0.055f, 0.045f, 0.035f, 0.9f);
        background.raycastTarget = false;
        panelObject.AddComponent<RectMask2D>();

        var textObject = new GameObject("Message", typeof(RectTransform));
        textObject.transform.SetParent(panelObject.transform, false);

        RectTransform textRect = (RectTransform)textObject.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(36f, 12f);
        textRect.offsetMax = new Vector2(-36f, -12f);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = DefaultMessage;
        text.fontSize = 34f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.94f, 0.78f, 1f);
        text.raycastTarget = false;

        // 튜토리얼 안내에 연결된 한글 폰트가 있으면 같은 글꼴을 이어받는다.
        _Scripts.LSO.Tutorial.LSO_TutorialBanner tutorialBanner =
            Object.FindFirstObjectByType<_Scripts.LSO.Tutorial.LSO_TutorialBanner>(
                FindObjectsInactive.Include);
        TMP_Text tutorialLabel = tutorialBanner != null
            ? tutorialBanner.GetComponentInChildren<TMP_Text>(true)
            : null;
        if (tutorialLabel != null && tutorialLabel.font != null)
            text.font = tutorialLabel.font;

        DLJ_SuccessionNotify created = panelObject.AddComponent<DLJ_SuccessionNotify>();
        created.label = text;
        created.message = DefaultMessage;
        created.visibleWidth = 800f;
        created.visibleHeight = 120f;
        created.playOnEnable = true;

        canvasObject.SetActive(true);
        return created;
    }

    public void ShowImmediately()
    {
        if (revealCoroutine != null)
        {
            StopCoroutine(revealCoroutine);
            revealCoroutine = null;
        }

        SetMaskSize(visibleWidth, visibleHeight);
    }

    private IEnumerator RevealRoutine()
    {
        SetMaskSize(0f, visibleHeight);
        yield return null;

        float duration = Mathf.Max(0.05f, revealDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            SetMaskSize(visibleWidth * easedProgress, visibleHeight);

            yield return null;
        }

        SetMaskSize(visibleWidth, visibleHeight);
        revealCoroutine = null;
    }
    
    private IEnumerator UnrevealRoutine()
    {
        float duration = Mathf.Max(0.05f, revealDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = Mathf.SmoothStep(1f, 0f, progress);
            SetMaskSize(visibleWidth * easedProgress, visibleHeight);

            yield return null;
        }

        SetMaskSize(0, visibleHeight);
        gameObject.SetActive(false);
        UnrevealCoroutine = null;
    }

    private void SetMaskSize(float width, float height)
    {
        maskRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        maskRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }
}
