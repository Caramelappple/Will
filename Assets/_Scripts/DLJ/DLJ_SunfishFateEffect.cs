using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 개복치의 돌연사 알림처럼 어두운 패널과 붉은 심전도를 표시한다.
/// 외부 텍스처 없이 UI 메시를 만들기 때문에 문구와 색을 인스펙터에서 바로 바꿀 수 있다.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("DLJ/Effects/Sunfish Fate Effect")]
public sealed class DLJ_SunfishFateEffect : MonoBehaviour
{
    private const string GeneratedRootName = "DLJ_SunfishFateEffect_Runtime";

    [Header("Content")]
    [SerializeField, FormerlySerializedAs("displayText")] private string deathText = "돌연사";
    [SerializeField] private string survivalText = "살았다!";
    [SerializeField] private TMP_FontAsset fontAsset;

    [Header("Placement")]
    [SerializeField] private Vector3 worldOffset = new(0f, 1.75f, 0f);
    [Tooltip("패널의 기본 크기. 특성 사전의 Effect Scale이 이 값에 곱해진다.")]
    [SerializeField, Min(0.0001f)] private float worldScale = 0.0046f;
    [SerializeField] private int sortingOrder = 220;

    [Header("Playback")]
    [SerializeField] private bool autoPlayOnStart = true;
    [SerializeField] private bool previewInEditMode = true;
    [SerializeField, Min(0.2f)] private float duration = 2.2f;
    [SerializeField, Min(0f)] private float riseDistance = 0.16f;
    [SerializeField, Min(0f)] private float pixelJitter = 0.018f;

    [Header("Palette")]
    [SerializeField] private Color panelColor = new(0.025f, 0.14f, 0.16f, 0.98f);
    [SerializeField] private Color borderColor = new(0.015f, 0.018f, 0.02f, 1f);
    [SerializeField] private Color pulseColor = new(0.92f, 0.035f, 0.055f, 1f);
    [SerializeField] private Color textColor = new(0.96f, 0.95f, 0.90f, 1f);

    private RectTransform _visualRoot;
    private CanvasGroup _canvasGroup;
    private DLJ_SunfishPanelGraphic _panel;
    private TextMeshProUGUI _label;
    private TextMeshProUGUI _shadow;
    private Coroutine _playRoutine;
    private float _animationScale = 1f;
    private Vector3 _animationOffset;
    private string _runtimeText;

    public string DisplayText
    {
        get => string.IsNullOrWhiteSpace(_runtimeText) ? deathText : _runtimeText;
        set
        {
            _runtimeText = string.IsNullOrWhiteSpace(value) ? " " : value;
            ApplyText();
        }
    }

    public string DeathText => deathText;
    public string SurvivalText => survivalText;

    private void OnEnable()
    {
        if (!Application.isPlaying && previewInEditMode)
        {
            EnsureBuilt();
            SetVisible(true);
        }
    }

    private void Start()
    {
        if (!Application.isPlaying) return;

        EnsureBuilt();
        SetRuntimeFlags();
        SetVisible(false);

        if (autoPlayOnStart)
            Play();
    }

    private void Update()
    {
        if (Application.isPlaying) return;

        if (previewInEditMode)
        {
            EnsureBuilt();
            SetVisible(true);
            UpdatePose();
        }
        else
        {
            SetVisible(false);
        }
    }

    private void LateUpdate()
    {
        if (Application.isPlaying && _visualRoot != null && _visualRoot.gameObject.activeSelf)
            UpdatePose();
    }

    private void OnValidate()
    {
        duration = Mathf.Max(0.2f, duration);
        worldScale = Mathf.Max(0.0001f, worldScale);

        ApplyText();
        ApplyStyle();
    }

    private void OnDisable()
    {
        if (Application.isPlaying || _visualRoot == null) return;

        DestroyImmediate(_visualRoot.gameObject);
        ClearRuntimeReferences();
    }

    /// <summary>현재 인스펙터 문구로 이펙트를 재생한다.</summary>
    [ContextMenu("Play Effect")]
    public void Play()
    {
        if (!Application.isPlaying)
        {
            EnsureBuilt();
            SetVisible(true);
            return;
        }

        EnsureBuilt();

        if (_playRoutine != null)
            StopCoroutine(_playRoutine);

        _playRoutine = StartCoroutine(PlayRoutine());
    }

    /// <summary>문구를 바꾸고 즉시 재생한다.</summary>
    public void Play(string message)
    {
        DisplayText = message;
        Play();
    }

    /// <summary>
    /// 특성 이펙트 플레이어가 전달한 판정 결과를 문구로 바꾼다.
    /// 0은 돌연사, 1은 생존이다.
    /// </summary>
    public void ApplyAbilityEffectVariant(int variant)
    {
        _runtimeText = variant == 1 ? survivalText : deathText;
        ApplyText();
    }

    /// <summary>재생 중인 이펙트를 즉시 감춘다.</summary>
    public void Hide()
    {
        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        _animationScale = 1f;
        _animationOffset = Vector3.zero;
        SetVisible(false);
    }

    private IEnumerator PlayRoutine()
    {
        SetVisible(true);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);

            float alphaIn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.09f, normalized));
            float alphaOut = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.78f, 1f, normalized));
            _canvasGroup.alpha = alphaIn * alphaOut;

            if (normalized < 0.14f)
            {
                float pop = Mathf.InverseLerp(0f, 0.14f, normalized);
                _animationScale = Mathf.LerpUnclamped(0.68f, 1.06f, EaseOutBack(pop));
            }
            else
            {
                _animationScale = Mathf.Lerp(1.06f, 1f, Mathf.InverseLerp(0.14f, 0.27f, normalized));
            }

            float steppedJitter = Mathf.Floor(elapsed * 24f) % 3f - 1f;
            _animationOffset = Vector3.up * (riseDistance * normalized) +
                               Vector3.right * (steppedJitter * pixelJitter * alphaOut);

            if (_panel != null)
                _panel.Pulse = 0.82f + Mathf.Sin(elapsed * 18f) * 0.18f;

            yield return null;
        }

        _animationScale = 1f;
        _animationOffset = Vector3.zero;
        _playRoutine = null;
        SetVisible(false);
    }

    private void EnsureBuilt()
    {
        if (_visualRoot != null) return;

        Transform staleRoot = transform.Find(GeneratedRootName);
        if (staleRoot != null)
        {
            if (Application.isPlaying)
                Destroy(staleRoot.gameObject);
            else
                DestroyImmediate(staleRoot.gameObject);
        }

        var rootObject = new GameObject(
            GeneratedRootName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasGroup));

        if (!Application.isPlaying)
            rootObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

        _visualRoot = rootObject.GetComponent<RectTransform>();
        _visualRoot.SetParent(transform, false);
        _visualRoot.sizeDelta = new Vector2(720f, 260f);

        Canvas canvas = rootObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        _canvasGroup = rootObject.GetComponent<CanvasGroup>();
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        var panelObject = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer));
        SetGeneratedFlags(panelObject);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.SetParent(_visualRoot, false);
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        _panel = panelObject.AddComponent<DLJ_SunfishPanelGraphic>();
        _panel.raycastTarget = false;

        _shadow = CreateLabel("Text Shadow", new Vector2(7f, -7f), borderColor);
        _label = CreateLabel("Text", Vector2.zero, textColor);

        ApplyText();
        ApplyStyle();
        UpdatePose();
    }

    private TextMeshProUGUI CreateLabel(string objectName, Vector2 offset, Color color)
    {
        var labelObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer));
        SetGeneratedFlags(labelObject);
        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.SetParent(_visualRoot, false);
        rect.anchorMin = new Vector2(0.10f, 0.18f);
        rect.anchorMax = new Vector2(0.90f, 0.82f);
        rect.offsetMin = offset;
        rect.offsetMax = offset;

        TextMeshProUGUI text = labelObject.AddComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 42f;
        text.fontSizeMax = 112f;
        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 18f;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.color = color;
        text.font = ResolveFont();
        return text;
    }

    private TMP_FontAsset ResolveFont()
    {
        if (fontAsset != null) return fontAsset;

        return Resources.Load<TMP_FontAsset>("Fonts & Materials/MaruBuri-Bold SDF") ??
               TMP_Settings.defaultFontAsset;
    }

    private void ApplyText()
    {
        string value = DisplayText;
        if (_label != null) _label.text = value;
        if (_shadow != null) _shadow.text = value;
    }

    private void ApplyStyle()
    {
        if (_panel != null)
        {
            _panel.PanelColor = panelColor;
            _panel.BorderColor = borderColor;
            _panel.PulseColor = pulseColor;
        }

        if (_label != null) _label.color = textColor;
        if (_shadow != null) _shadow.color = borderColor;

        TMP_FontAsset resolvedFont = ResolveFont();
        if (_label != null) _label.font = resolvedFont;
        if (_shadow != null) _shadow.font = resolvedFont;

        Canvas canvas = _visualRoot != null ? _visualRoot.GetComponent<Canvas>() : null;
        if (canvas != null) canvas.sortingOrder = sortingOrder;
    }

    private void UpdatePose()
    {
        if (_visualRoot == null) return;

        Camera targetCamera = Camera.main;
        Vector3 cameraRight = targetCamera != null ? targetCamera.transform.right : Vector3.right;
        _visualRoot.position = transform.position + worldOffset +
                               Vector3.up * _animationOffset.y +
                               cameraRight * _animationOffset.x;

        if (targetCamera != null)
        {
            Vector3 direction = _visualRoot.position - targetCamera.transform.position;
            if (direction.sqrMagnitude > 0.0001f)
                _visualRoot.rotation = Quaternion.LookRotation(direction, targetCamera.transform.up);
        }

        // 특성 사전의 Effect Scale은 이 컴포넌트가 붙은 루트에 적용된다.
        // 여기서 부모 lossyScale로 다시 나누면 미리보기에서 맞춘 배율이 런타임에 상쇄된다.
        // UI에는 기본 배율만 주고, 최종 월드 크기는 부모 배율을 그대로 상속시킨다.
        _visualRoot.localScale = Vector3.one * (worldScale * _animationScale);
    }

    private void SetVisible(bool visible)
    {
        if (_visualRoot == null) return;

        if (_visualRoot.gameObject.activeSelf != visible)
            _visualRoot.gameObject.SetActive(visible);

        if (visible && _canvasGroup != null && !Application.isPlaying)
            _canvasGroup.alpha = 1f;
    }

    private void SetGeneratedFlags(GameObject target)
    {
        if (!Application.isPlaying)
            target.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
    }

    private void SetRuntimeFlags()
    {
        if (_visualRoot == null) return;

        Transform[] generatedObjects = _visualRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < generatedObjects.Length; i++)
            generatedObjects[i].gameObject.hideFlags = HideFlags.None;
    }

    private void ClearRuntimeReferences()
    {
        _visualRoot = null;
        _canvasGroup = null;
        _panel = null;
        _label = null;
        _shadow = null;
    }

    private static float EaseOutBack(float value)
    {
        const float overshoot = 1.70158f;
        float shifted = value - 1f;
        return 1f + (overshoot + 1f) * shifted * shifted * shifted +
               overshoot * shifted * shifted;
    }
}

/// <summary>개복치 알림 패널의 픽셀풍 도형을 한 번에 그리는 런타임 UI 그래픽.</summary>
internal sealed class DLJ_SunfishPanelGraphic : MaskableGraphic
{
    private Color _panelColor;
    private Color _borderColor;
    private Color _pulseColor;
    private float _pulse = 1f;

    public Color PanelColor
    {
        get => _panelColor;
        set { _panelColor = value; SetVerticesDirty(); }
    }

    public Color BorderColor
    {
        get => _borderColor;
        set { _borderColor = value; SetVerticesDirty(); }
    }

    public Color PulseColor
    {
        get => _pulseColor;
        set { _pulseColor = value; SetVerticesDirty(); }
    }

    public float Pulse
    {
        get => _pulse;
        set
        {
            float next = Mathf.Clamp01(value);
            if (Mathf.Approximately(_pulse, next)) return;
            _pulse = next;
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        float cx = r.center.x;
        float cy = r.center.y;

        Color red = _pulseColor;
        red.a *= _pulse;

        AddRect(vh, new Rect(cx - 342f, cy - 112f, 684f, 224f), _borderColor);
        AddRect(vh, new Rect(cx - 330f, cy - 100f, 660f, 200f), _panelColor);

        Color innerShade = Color.Lerp(_panelColor, Color.black, 0.28f);
        AddRect(vh, new Rect(cx - 330f, cy + 62f, 660f, 38f), innerShade);
        AddRect(vh, new Rect(cx - 330f, cy - 100f, 660f, 36f), innerShade);

        AddRect(vh, new Rect(cx - 342f, cy + 105f, 684f, 7f), red);
        AddRect(vh, new Rect(cx - 342f, cy - 112f, 684f, 7f), red);

        Vector2[] topWave =
        {
            P(cx, cy, -296f, 78f), P(cx, cy, -220f, 78f), P(cx, cy, -188f, 102f),
            P(cx, cy, -180f, 151f), P(cx, cy, -169f, 105f), P(cx, cy, -142f, 78f),
            P(cx, cy, -102f, 78f), P(cx, cy, -82f, 98f), P(cx, cy, -72f, 139f),
            P(cx, cy, -60f, 96f), P(cx, cy, -34f, 78f), P(cx, cy, 8f, 78f),
            P(cx, cy, 31f, 98f), P(cx, cy, 48f, 127f), P(cx, cy, 59f, 87f),
            P(cx, cy, 88f, 78f), P(cx, cy, 125f, 78f), P(cx, cy, 144f, 98f),
            P(cx, cy, 158f, 148f), P(cx, cy, 172f, 98f), P(cx, cy, 207f, 78f),
            P(cx, cy, 296f, 78f)
        };
        AddPolyline(vh, topWave, 8f, red);

        Vector2[] bottomWave =
        {
            P(cx, cy, -296f, -78f), P(cx, cy, -217f, -78f), P(cx, cy, -191f, -99f),
            P(cx, cy, -180f, -148f), P(cx, cy, -167f, -102f), P(cx, cy, -143f, -78f),
            P(cx, cy, -110f, -78f), P(cx, cy, -89f, -101f), P(cx, cy, -77f, -132f),
            P(cx, cy, -66f, -95f), P(cx, cy, -40f, -78f), P(cx, cy, -12f, -78f),
            P(cx, cy, 7f, -100f), P(cx, cy, 20f, -137f), P(cx, cy, 33f, -96f),
            P(cx, cy, 58f, -78f), P(cx, cy, 88f, -78f), P(cx, cy, 106f, -100f),
            P(cx, cy, 119f, -139f), P(cx, cy, 132f, -98f), P(cx, cy, 158f, -78f),
            P(cx, cy, 184f, -78f), P(cx, cy, 199f, -98f), P(cx, cy, 212f, -124f),
            P(cx, cy, 225f, -94f), P(cx, cy, 248f, -78f), P(cx, cy, 296f, -78f)
        };
        AddPolyline(vh, bottomWave, 8f, red);

        Vector2[] leftChevron =
        {
            P(cx, cy, -342f, 20f), P(cx, cy, -371f, 0f), P(cx, cy, -342f, -20f)
        };
        Vector2[] leftInner =
        {
            P(cx, cy, -326f, 18f), P(cx, cy, -353f, 0f), P(cx, cy, -326f, -18f)
        };
        Vector2[] rightChevron =
        {
            P(cx, cy, 342f, 20f), P(cx, cy, 371f, 0f), P(cx, cy, 342f, -20f)
        };
        Vector2[] rightInner =
        {
            P(cx, cy, 326f, 18f), P(cx, cy, 353f, 0f), P(cx, cy, 326f, -18f)
        };
        AddPolyline(vh, leftChevron, 8f, red);
        AddPolyline(vh, leftInner, 8f, red);
        AddPolyline(vh, rightChevron, 8f, red);
        AddPolyline(vh, rightInner, 8f, red);

        Color scratch = Color.Lerp(_panelColor, Color.white, 0.18f);
        scratch.a = 0.28f;
        AddRect(vh, new Rect(cx - 272f, cy + 31f, 42f, 5f), scratch);
        AddRect(vh, new Rect(cx + 224f, cy - 40f, 36f, 5f), scratch);
        AddRect(vh, new Rect(cx + 258f, cy + 34f, 20f, 5f), scratch);
    }

    private static Vector2 P(float cx, float cy, float x, float y) => new(cx + x, cy + y);

    private static void AddRect(VertexHelper vh, Rect rect, Color color)
    {
        int start = vh.currentVertCount;
        vh.AddVert(new Vector3(rect.xMin, rect.yMin), color, Vector2.zero);
        vh.AddVert(new Vector3(rect.xMin, rect.yMax), color, Vector2.zero);
        vh.AddVert(new Vector3(rect.xMax, rect.yMax), color, Vector2.zero);
        vh.AddVert(new Vector3(rect.xMax, rect.yMin), color, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }

    private static void AddPolyline(VertexHelper vh, Vector2[] points, float thickness, Color color)
    {
        if (points == null || points.Length < 2) return;

        float half = thickness * 0.5f;
        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector2 from = points[i];
            Vector2 to = points[i + 1];
            Vector2 direction = (to - from).normalized;
            Vector2 normal = new(-direction.y * half, direction.x * half);

            int start = vh.currentVertCount;
            vh.AddVert(from - normal, color, Vector2.zero);
            vh.AddVert(from + normal, color, Vector2.zero);
            vh.AddVert(to + normal, color, Vector2.zero);
            vh.AddVert(to - normal, color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
