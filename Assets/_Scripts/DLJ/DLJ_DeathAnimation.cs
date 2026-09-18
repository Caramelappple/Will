using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 보드에서 제거된 기물을 제자리에서 흔든 뒤 넘어뜨리고 투명하게 지운다.
/// 실제 GameObject 파괴 시점은 사망 시스템이 전달한 콜백이 결정한다.
/// </summary>
public sealed class DLJ_DeathAnimation : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField, Min(0f)] private float shakeDuration = 0.22f;
    [SerializeField, Min(0.01f)] private float fallDuration = 1f;
    [SerializeField, Min(0f)] private float endHoldDuration = 0.05f;

    [Header("Motion")]
    [SerializeField, Range(0f, 30f)] private float shakeAngle = 7f;
    [SerializeField, Min(0f)] private float shakeFrequency = 32f;
    [SerializeField, Range(30f, 120f)] private float fallAngle = 82f;
    [SerializeField, Range(0f, 1f)] private float fallDropByHeight = 0.35f;

    [Header("Fade")]
    [SerializeField, Range(0f, 1f)] private float fadeStart = 0.2f;

    private readonly List<MaterialColor> _materialColors = new();
    private bool _isPlaying;

    public bool IsPlaying => _isPlaying;

    /// <summary>
    /// 애니메이션을 한 번만 재생한다. visualRoot가 없으면 이 오브젝트의 Transform을 사용한다.
    /// </summary>
    public void Play(Transform visualRoot, Action onComplete)
    {
        if (_isPlaying) return;

        _isPlaying = true;
        Transform target = visualRoot != null ? visualRoot : transform;
        StartCoroutine(PlayRoutine(target, onComplete));
    }

    /// <summary>
    /// 꺼지면 돌던 코루틴이 죽는다. **그때 _isPlaying 은 되돌아가지 않는다.**
    ///
    /// ── 왜 여기가 필요한가 ────────────────────────────────────
    /// 유니티는 코루틴을 중단할 때 반복자를 정리하지 않는다. PlayRoutine 안에서만
    /// _isPlaying 을 false 로 되돌리므로, 연출 도중에 오브젝트가 꺼지면 그 값이
    /// true 로 굳는다.
    ///
    /// KTH_GameEndManager 는 HasPlayingDeathAnimation 이 거짓이 될 때까지
    /// 클리어를 미룬다. 하나라도 굳어 있으면 **적을 다 잡아도 보상으로
    /// 넘어가지 않는다.** 판정이 막히는 것이지 적이 살아 있는 것이 아니라
    /// 화면만 보고는 원인을 알 수 없다.
    ///
    /// 같은 종류로 LDY_DissolveEffect.ActiveCount 가 새던 적이 있다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 콜백은 부르지 않는다. 그쪽은 대개 오브젝트를 파괴하는데, 꺼지는 중에
    /// 부르면 파괴 순서가 꼬인다. 여기서는 "더 기다리지 말라"만 알리면 된다.
    /// </summary>
    private void OnDisable()
    {
        if (!_isPlaying) return;

        _isPlaying = false;

        Debug.LogWarning(
            $"{name}: 사망 연출이 끝나기 전에 꺼졌습니다. " +
            "클리어 판정이 막히지 않도록 재생 표시를 되돌립니다.", this);
    }

    private IEnumerator PlayRoutine(Transform target, Action onComplete)
    {
        DisableColliders();
        PrepareMaterials(target);

        Quaternion startRotation = target.localRotation;
        Vector3 startPosition = target.localPosition;
        float direction = (GetInstanceID() & 1) == 0 ? 1f : -1f;

        float elapsed = 0f;
        while (elapsed < shakeDuration && target != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = shakeDuration > 0f
                ? Mathf.Clamp01(elapsed / shakeDuration)
                : 1f;
            float damping = 1f - normalized;
            float angle = Mathf.Sin(elapsed * shakeFrequency) * shakeAngle * damping;

            target.localRotation = startRotation * Quaternion.Euler(0f, 0f, 0f);
            yield return null;
        }

        if (target == null)
        {
            _isPlaying = false;
            onComplete?.Invoke();
            yield break;
        }

        target.localRotation = startRotation;

        float dropDistance = CalculateDropDistance(target);
        Quaternion fallenRotation = startRotation *
                                     Quaternion.Euler(0f, 0f, dropDistance * fallAngle);
        Vector3 fallenPosition = startPosition + Vector3.down * dropDistance;

        elapsed = 0f;
        while (elapsed < fallDuration && target != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / fallDuration);
            float eased = 1f - Mathf.Pow(1f - normalized, 3f);

            target.localRotation = Quaternion.Slerp(startRotation, fallenRotation, eased);
            target.localPosition = Vector3.Lerp(startPosition, fallenPosition, eased);

            float fade = Mathf.InverseLerp(fadeStart, 1f, normalized);
            SetAlpha(1f - fade);
            yield return null;
        }

        if (target != null)
        {
            target.localRotation = fallenRotation;
            target.localPosition = fallenPosition;
        }

        SetAlpha(0f);

        if (endHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(endHoldDuration);

        _isPlaying = false;
        onComplete?.Invoke();
    }

    private void DisableColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        Collider2D[] colliders2D = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders2D.Length; i++)
            colliders2D[i].enabled = false;
    }

    private void PrepareMaterials(Transform target)
    {
        _materialColors.Clear();

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer renderer = renderers[rendererIndex];
            if (renderer is ParticleSystemRenderer || renderer is TrailRenderer ||
                renderer is LineRenderer)
                continue;

            Material[] materials = renderer.materials;
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];
                if (material == null) continue;

                ConfigureTransparent(material);
                RegisterColor(material, "_BaseColor");

                // 일부 셰이더는 _BaseColor 대신 _Color만 사용한다.
                if (!material.HasProperty("_BaseColor"))
                    RegisterColor(material, "_Color");
            }
        }
    }

    private void RegisterColor(Material material, string propertyName)
    {
        if (!material.HasProperty(propertyName)) return;

        _materialColors.Add(new MaterialColor(
            material,
            Shader.PropertyToID(propertyName),
            material.GetColor(propertyName)));
    }

    private static void ConfigureTransparent(Material material)
    {
        // URP Lit / Simple Lit / Shader Graph 계열.
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_AlphaClip")) material.SetFloat("_AlphaClip", 0f);
            if (material.HasProperty("_SrcBlend"))
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend"))
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
            return;
        }

        // Built-in Standard 셰이더를 쓰는 기물이 생겨도 같은 연출이 동작하게 한다.
        if (material.HasProperty("_Mode"))
        {
            material.SetFloat("_Mode", 2f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
        }
    }

    private void SetAlpha(float alpha)
    {
        alpha = Mathf.Clamp01(alpha);

        for (int i = 0; i < _materialColors.Count; i++)
        {
            MaterialColor binding = _materialColors[i];
            if (binding.Material == null) continue;

            Color color = binding.OriginalColor;
            color.a *= alpha;
            binding.Material.SetColor(binding.PropertyId, color);
        }
    }

    private float CalculateDropDistance(Transform target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        bool hasBounds = false;
        Bounds bounds = default;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] is ParticleSystemRenderer || renderers[i] is TrailRenderer ||
                renderers[i] is LineRenderer)
                continue;

            if (!hasBounds)
            {
                bounds = renderers[i].bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        if (!hasBounds) return 0.25f;

        float parentScaleY = target.parent != null
            ? Mathf.Max(Mathf.Abs(target.parent.lossyScale.y), 0.0001f)
            : 1f;
        return bounds.size.y * fallDropByHeight / parentScaleY;
    }

    private readonly struct MaterialColor
    {
        public Material Material { get; }
        public int PropertyId { get; }
        public Color OriginalColor { get; }

        public MaterialColor(Material material, int propertyId, Color originalColor)
        {
            Material = material;
            PropertyId = propertyId;
            OriginalColor = originalColor;
        }
    }
}
