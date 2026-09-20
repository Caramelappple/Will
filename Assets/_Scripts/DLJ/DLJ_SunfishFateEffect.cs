using System.Collections;
using _Scripts.LDY;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 개복치의 돌연사 알림처럼 어두운 패널과 붉은 심전도를 표시한다.
/// 문구와 전체 배치는 기물 인스펙터, 내부 UI 배치는 Layout Prefab에서 편집한다.
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

    [Header("Editable Layout")]
    [Tooltip("이 프리팹의 자식 RectTransform으로 내부 배치를 편집한다.")]
    [SerializeField] private DLJ_SunfishFateLayout layoutPrefab;

    [Header("Placement")]
    [SerializeField] private Vector3 worldOffset = new(0f, 1.75f, 0f);
    [Tooltip("기물에 붙였으면 기물 배율 × 이 값이 미리보기와 실제 재생에 함께 적용된다. 독립 프리팹으로 쓰면 특성 사전의 Effect Scale이 곱해진다.")]
    [SerializeField, Min(0.0001f)] private float worldScale = 0.0046f;
    [SerializeField] private int sortingOrder = 220;

    [Header("Playback")]
    [SerializeField] private bool autoPlayOnStart = true;
    [SerializeField] private bool previewInEditMode = true;
    [SerializeField, Min(0.2f)] private float duration = 2.2f;
    [SerializeField, Min(0f)] private float riseDistance = 0.16f;
    [SerializeField, Min(0f)] private float pixelJitter = 0.018f;

    private RectTransform _visualRoot;
    private CanvasGroup _canvasGroup;
    private DLJ_SunfishFateLayout _layout;
    private bool _refreshRequested;
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
    public float Duration => duration;
    public DLJ_SunfishFateLayout LayoutPrefab => layoutPrefab;

    // 에디터에서 프리팹 저장 후 다음 Update에 미리보기만 다시 만든다.
    public void RefreshLayoutPreview() => _refreshRequested = true;

    /// <summary>
    /// 기물 인스펙터에서 맞춘 설정과 월드 배율을 독립된 재생 오브젝트에 복사한다.
    /// 기물 자체를 복제하지 않으며, 기물이 파괴돼도 연출은 끝까지 살아남는다.
    /// </summary>
    public DLJ_SunfishFateEffect CreateDetachedPlayback(int variant)
    {
        var instance = new GameObject("DLJ_SunfishFateEffect_Playback");
        instance.SetActive(false);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance, gameObject.scene);
        instance.transform.SetPositionAndRotation(transform.position, transform.rotation);
        instance.transform.localScale = transform.lossyScale;

        var effect = instance.AddComponent<DLJ_SunfishFateEffect>();
        effect.deathText = deathText;
        effect.survivalText = survivalText;
        effect.fontAsset = fontAsset;
        effect.layoutPrefab = layoutPrefab;
        effect.worldOffset = worldOffset;
        effect.worldScale = worldScale;
        effect.sortingOrder = sortingOrder;
        effect.duration = duration;
        effect.riseDistance = riseDistance;
        effect.pixelJitter = pixelJitter;
        effect.autoPlayOnStart = true;
        effect.previewInEditMode = false;
        effect.ApplyAbilityEffectVariant(variant);
        instance.SetActive(true);
        return effect;
    }

    private void OnEnable()
    {
#if UNITY_EDITOR
        if (UnityEditor.EditorUtility.IsPersistent(this)) return;
#endif
        if (Application.isPlaying)
        {
            SetVisible(false);
            return;
        }

        if (!Application.isPlaying && previewInEditMode)
        {
            EnsureBuilt();
            SetVisible(true);
        }
    }

    private void Start()
    {
        if (!Application.isPlaying) return;

        // 기물에 붙은 컴포넌트는 설정/미리보기 원본이다. 판정 시 플레이어가
        // 독립 사본을 생성한다. 기물 위 원본까지 재생하면 두 연출이 겹친다.
        if (GetComponent<LDY_Animal>() != null)
        {
            SetVisible(false);
            return;
        }

        EnsureBuilt();
        SetRuntimeFlags();
        SetVisible(false);

        if (autoPlayOnStart)
            Play();
    }

    private void Update()
    {
        if (Application.isPlaying) return;
#if UNITY_EDITOR
        if (UnityEditor.EditorUtility.IsPersistent(this)) return;
#endif
        if (_refreshRequested)
        {
            ReleasePreview();
            _refreshRequested = false;
        }

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

        _refreshRequested = true;
    }

    private void OnDisable()
    {
        if (Application.isPlaying || _visualRoot == null) return;

        ReleasePreview();
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
        if (_layout == null) return;

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
                _animationScale = Mathf.SmoothStep(0.68f, 1f, pop);
            }
            else
            {
                _animationScale = 1f;
            }

            float steppedJitter = Mathf.Floor(elapsed * 24f) % 3f - 1f;
            _animationOffset = Vector3.up * (riseDistance * normalized) +
                               Vector3.right * (steppedJitter * pixelJitter * alphaOut);

            if (_layout != null)
                _layout.SetPulse(0.82f + Mathf.Sin(elapsed * 18f) * 0.18f);

            yield return null;
        }

        _animationScale = 1f;
        _animationOffset = Vector3.zero;
        _playRoutine = null;
        SetVisible(false);
    }

    private void EnsureBuilt()
    {
        if (_visualRoot != null || layoutPrefab == null) return;

        Transform staleRoot = transform.Find(GeneratedRootName);
        if (staleRoot != null)
        {
            if (Application.isPlaying) Destroy(staleRoot.gameObject);
            else DestroyImmediate(staleRoot.gameObject);
        }

        var rootObject = new GameObject(GeneratedRootName, typeof(RectTransform));
        _visualRoot = rootObject.GetComponent<RectTransform>();
        _visualRoot.SetParent(transform, false);
        _visualRoot.sizeDelta = new Vector2(720f, 260f);

        // 저장된 UI를 그대로 복제한다. 자식 위치/크기/회전/앵커/색상은 덮어쓰지 않는다.
        _layout = Instantiate(layoutPrefab, _visualRoot, false);
        _canvasGroup = _layout.GetComponent<CanvasGroup>();
        _layout.GetComponent<Canvas>().sortingOrder = sortingOrder;

        if (!Application.isPlaying)
        {
            foreach (Transform child in _visualRoot.GetComponentsInChildren<Transform>(true))
                child.gameObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            rootObject.hideFlags |= HideFlags.HideInHierarchy;
        }

        ApplyText();
        UpdatePose();
    }

    private void ApplyText()
    {
        if (_layout != null)
            _layout.SetMessage(DisplayText, fontAsset);
    }

    private void ReleasePreview()
    {
        if (_visualRoot != null) DestroyImmediate(_visualRoot.gameObject);
        _visualRoot = null;
        _canvasGroup = null;
        _layout = null;
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

    private void SetRuntimeFlags()
    {
        if (_visualRoot == null) return;

        Transform[] generatedObjects = _visualRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < generatedObjects.Length; i++)
            generatedObjects[i].gameObject.hideFlags = HideFlags.None;
    }


}
