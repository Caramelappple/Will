using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LSO.Ability;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>자신의 옹골참이 발동하면 꼬리만 월드 위쪽으로 띄우고 지운다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(LDY_Animal))]
public sealed class DLJ_GeckoSturdyEffect : MonoBehaviour
{
    [Header("대상")]
    [SerializeField] private Transform tail;

    [Header("상승")]
    [SerializeField, Min(0.01f)] private float riseDuration = 0.8f;
    [Tooltip("월드 Y축 기준 상승 거리")]
    [SerializeField, Min(0f)] private float riseHeight = 1.5f;
    [Tooltip("X: 시간 비율 0~1, Y: 상승 비율. 시작 (0,0), 끝 (1,1)을 권장")]
    [SerializeField] private AnimationCurve riseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool useUnscaledTime = true;

    [Header("사라짐")]
    [Tooltip("전체 시간 중 투명해지기 시작하는 비율")]
    [SerializeField, Range(0f, 0.99f)] private float fadeStart = 0.35f;

    private LDY_Animal _animal;
    private Transform _cachedTail;
    private Vector3 _restPosition;
    private Quaternion _restRotation;
    private bool _restActive;
    private Vector3 _startPosition;
    private Quaternion _startRotation;
    private float _elapsed;
    private bool _spent;
    private readonly List<RendererMaterials> _renderers = new();
    private readonly List<MaterialColor> _colors = new();

    public bool IsPlaying { get; private set; }

    private void OnEnable()
    {
        _animal = GetComponent<LDY_Animal>();
        CacheTail();
        LSO_AbilitySignal.Fired += HandleFired;
        _animal.AbilitiesChanged += HandleAbilitiesChanged;
        HandleAbilitiesChanged();
        SyncSpentTail();
    }

    private void OnDisable()
    {
        LSO_AbilitySignal.Fired -= HandleFired;
        if (_animal != null) _animal.AbilitiesChanged -= HandleAbilitiesChanged;
        if (IsPlaying) Finish();
        ReleaseMaterials();
    }

    private void HandleFired(LSO_AbilityFired fired)
    {
        if (fired.Type == LSO_AbilityType.Sturdy && fired.Animal == _animal)
            Play();
    }

    private void HandleAbilitiesChanged()
    {
        // Setup으로 새 특성 인스턴스를 받았을 때만 소모된 꼬리를 복구한다.
        foreach (LSO_IAbility ability in _animal.Abilities)
        {
            if (ability is LSO_Sturdy sturdy && !sturdy.HasTriggered)
            {
                RestoreTail();
                return;
            }
        }
    }

    private void SyncSpentTail()
    {
        foreach (LSO_IAbility ability in _animal.Abilities)
        {
            if (!(ability is LSO_Sturdy sturdy) || !sturdy.HasTriggered) continue;
            _spent = true;
            if (tail != null) tail.gameObject.SetActive(false);
            return;
        }
    }

    private bool CacheTail()
    {
        if (tail == null) tail = transform.Find("Will_GeckoTail");
        if (tail == null || tail == transform || !tail.IsChildOf(transform)) return false;
        if (_cachedTail == tail) return true;
        _cachedTail = tail;
        _restPosition = tail.localPosition;
        _restRotation = tail.localRotation;
        _restActive = tail.gameObject.activeSelf;
        return true;
    }

    public void Play()
    {
        if (!Application.isPlaying || !isActiveAndEnabled || _spent || IsPlaying) return;
        if (!CacheTail())
        {
            Debug.LogWarning("FullGecko의 자식 꼬리를 Tail에 연결해야 해.", this);
            return;
        }

        _spent = true;
        _startPosition = tail.position;
        _startRotation = tail.rotation;
        _elapsed = 0f;
        PrepareMaterials();
        IsPlaying = true;
    }

    private void LateUpdate()
    {
        if (!IsPlaying) return;
        if (tail == null)
        {
            Finish();
            return;
        }

        _elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, riseDuration));
        float rise = riseCurve != null && riseCurve.length > 0 ? riseCurve.Evaluate(t) : t;
        // 본체가 이동하거나 회전해도 발동한 자리에서 월드 위쪽으로 상승한다.
        tail.SetPositionAndRotation(_startPosition + Vector3.up * (riseHeight * rise), _startRotation);
        float fade = Mathf.InverseLerp(Mathf.Clamp(fadeStart, 0f, 0.99f), 1f, t);
        float alpha = 1f - Mathf.SmoothStep(0f, 1f, fade);
        foreach (MaterialColor binding in _colors)
        {
            Color color = binding.Color;
            color.a *= alpha;
            binding.Material.SetColor(binding.Property, color);
        }
        if (t >= 1f) Finish();
    }

    private void Finish()
    {
        IsPlaying = false;
        if (tail != null) tail.gameObject.SetActive(false);
        ReleaseMaterials();
    }

    /// <summary>연출만 되돌린다. 실제 옹골참의 소모 여부나 HP는 바꾸지 않는다.</summary>
    public void RestoreTail()
    {
        IsPlaying = false;
        ReleaseMaterials();
        if (_cachedTail != null)
        {
            _cachedTail.localPosition = _restPosition;
            _cachedTail.localRotation = _restRotation;
            _cachedTail.gameObject.SetActive(_restActive);
        }
        _spent = false;
    }

    [ContextMenu("Preview/Tail Rise (Play Mode)")]
    public void Preview()
    {
        if (!Application.isPlaying || !isActiveAndEnabled) return;
        RestoreTail();
        Play();
    }

    private void PrepareMaterials()
    {
        foreach (Renderer renderer in tail.GetComponentsInChildren<Renderer>(true))
        {
            if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
            Material[] originals = renderer.sharedMaterials;
            Material[] copies = new Material[originals.Length];
            for (int i = 0; i < originals.Length; i++)
            {
                if (originals[i] == null) continue;
                Material copy = new Material(originals[i]);
                copies[i] = copy;
                ConfigureTransparent(copy);
                string property = copy.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
                if (copy.HasProperty(property))
                    _colors.Add(new MaterialColor(copy, Shader.PropertyToID(property), copy.GetColor(property)));
            }
            _renderers.Add(new RendererMaterials(renderer, originals, copies));
            renderer.sharedMaterials = copies;
        }
    }

    private static void ConfigureTransparent(Material material)
    {
        // 현재 모델의 URP 재질과 Built-in Standard 재질을 지원한다.
        SetFloatIfPresent(material, "_Surface", 1f);
        SetFloatIfPresent(material, "_Mode", 2f);
        SetFloatIfPresent(material, "_Blend", 0f);
        SetFloatIfPresent(material, "_AlphaClip", 0f);
        SetFloatIfPresent(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
        SetFloatIfPresent(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        SetFloatIfPresent(material, "_SrcBlendAlpha", (float)BlendMode.One);
        SetFloatIfPresent(material, "_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        SetFloatIfPresent(material, "_ZWrite", 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.DisableKeyword("_ALPHAMODULATE_ON");
        if (material.HasProperty("_Mode")) material.EnableKeyword("_ALPHABLEND_ON");
        material.SetShaderPassEnabled("ShadowCaster", false);
        material.renderQueue = (int)RenderQueue.Transparent;
    }

    private static void SetFloatIfPresent(Material material, string property, float value)
    {
        if (material.HasProperty(property)) material.SetFloat(property, value);
    }

    private void ReleaseMaterials()
    {
        foreach (RendererMaterials binding in _renderers)
        {
            if (binding.Renderer != null) binding.Renderer.sharedMaterials = binding.Originals;
            foreach (Material copy in binding.Copies)
                if (copy != null) Destroy(copy);
        }
        _renderers.Clear();
        _colors.Clear();
    }

    private readonly struct RendererMaterials
    {
        public readonly Renderer Renderer;
        public readonly Material[] Originals;
        public readonly Material[] Copies;
        public RendererMaterials(Renderer renderer, Material[] originals, Material[] copies)
        {
            Renderer = renderer;
            Originals = originals;
            Copies = copies;
        }
    }

    private readonly struct MaterialColor
    {
        public readonly Material Material;
        public readonly int Property;
        public readonly Color Color;
        public MaterialColor(Material material, int property, Color color)
        {
            Material = material;
            Property = property;
            Color = color;
        }
    }
}
