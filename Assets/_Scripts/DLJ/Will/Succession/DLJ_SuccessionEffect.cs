using System;
using System.Collections.Generic;
using _Scripts.LSO.Will;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class DLJ_SuccessionEffect : DLJ_IWillEffect
{
    private const string ColorProperty = "_Color";
    private const string BaseColorProperty = "_BaseColor";
    private const string EmissionColorProperty = "_EmissionColor";

    public void Play(
        GameObject effectObject,
        DLJ_WillEffectContext context,
        Action onComplete = null)
    {
        if (context == null || context.target == null)
        {
            if (effectObject != null)
                UnityEngine.Object.Destroy(effectObject);
            onComplete?.Invoke();
            return;
        }

        if (effectObject != null)
            UnityEngine.Object.Destroy(effectObject);

        DLJ_SuccessionWillDataSO data = context.data as DLJ_SuccessionWillDataSO;
        DLJ_SuccessionEffectSO visual = data != null ? data.successionEffect : null;
        Color lineColor = visual != null
            ? visual.successionLineColor
            : new Color(0.72f, 0.92f, 1f, 0.9f);
        float lineWidth = visual != null ? visual.successionLineWidth : 0.025f;
        float lineHeight = visual != null ? visual.successionLineHeight : 0.35f;
        float travelDuration = data != null ? data.moveDuration : 1f;
        float absorbDuration = visual != null ? visual.successionAbsorbDuration : 0.22f;
        float flashDuration = visual != null ? visual.successionFlashDuration : 0.18f;

        Vector3 start = context.origin + Vector3.up * lineHeight;
        Vector3 end = context.targetPosition + Vector3.up * lineHeight;
        Vector3 control = Vector3.Lerp(start, end, 0.5f) + Vector3.up * (lineHeight * 0.45f);

        LineRenderer line = CreateLine(lineColor, lineWidth);
        Material lineMaterial = line.material;
        SetLineSegment(line, start, control, end, 0f, 0f);

        var glow = new SuccessionGlowState(context.target, lineColor, data);
        Sequence sequence = DOTween.Sequence()
            .SetUpdate(true)
            .Append(DOVirtual.Float(0f, 1f, Mathf.Max(0f, travelDuration), value =>
            {
                if (line == null) return;
                float eased = DOVirtual.EasedValue(0f, 1f, value, Ease.OutCubic);
                SetLineSegment(line, start, control, end, 0f, eased);
            }))
            .Append(DOVirtual.Float(0f, 1f, Mathf.Max(0f, absorbDuration), value =>
            {
                if (line == null) return;
                float eased = DOVirtual.EasedValue(0f, 1f, value, Ease.InCubic);
                SetLineSegment(line, start, control, end, eased, 1f);
                line.widthMultiplier = Mathf.Lerp(lineWidth, 0f, eased);
                Color fading = lineColor;
                fading.a *= 1f - eased;
                line.startColor = fading;
                line.endColor = fading;
            }))
            .AppendCallback(() => DestroyLine(line, lineMaterial))
            .Append(DOVirtual.Float(0f, 1f, Mathf.Max(0f, flashDuration) * 0.5f,
                glow.Apply))
            .Append(DOVirtual.Float(1f, 0f, Mathf.Max(0f, flashDuration) * 0.5f,
                glow.Apply))
            .OnComplete(() =>
            {
                glow.Cleanup();
                onComplete?.Invoke();
            })
            .OnKill(glow.Cleanup);

        // 0초 설정에서도 콜백과 정리 순서를 DOTween이 끝까지 실행하게 시퀀스를 유지한다.
        sequence.Play();
    }

    private static LineRenderer CreateLine(Color color, float width)
    {
        GameObject lineObject = new GameObject("Succession Link");
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Hidden/Internal-Colored");

        line.material = new Material(shader);
        line.useWorldSpace = true;
        line.positionCount = 3;
        line.textureMode = LineTextureMode.Stretch;
        line.alignment = LineAlignment.View;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        line.widthMultiplier = Mathf.Max(0.001f, width);
        line.startColor = color;
        line.endColor = color;
        return line;
    }

    private static void SetLineSegment(
        LineRenderer line,
        Vector3 start,
        Vector3 control,
        Vector3 end,
        float from,
        float to)
    {
        float middle = (from + to) * 0.5f;
        line.SetPosition(0, EvaluateCurve(start, control, end, from));
        line.SetPosition(1, EvaluateCurve(start, control, end, middle));
        line.SetPosition(2, EvaluateCurve(start, control, end, to));
    }

    private static Vector3 EvaluateCurve(
        Vector3 start,
        Vector3 control,
        Vector3 end,
        float t)
    {
        float inverse = 1f - t;
        return inverse * inverse * start +
               2f * inverse * t * control +
               t * t * end;
    }

    private static void DestroyLine(LineRenderer line, Material material)
    {
        if (line != null)
            UnityEngine.Object.Destroy(line.gameObject);
        if (material != null)
            UnityEngine.Object.Destroy(material);
    }

    private static List<RendererFlashState> CaptureRenderers(GameObject target)
    {
        var states = new List<RendererFlashState>();
        if (target == null)
            return states;

        foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null || renderer is LineRenderer)
                continue;

            Material shared = renderer.sharedMaterial;
            if (shared == null)
                continue;

            bool hasColor = shared.HasProperty(ColorProperty);
            bool hasBaseColor = shared.HasProperty(BaseColorProperty);
            bool hasEmission = shared.HasProperty(EmissionColorProperty);
            if (!hasColor && !hasBaseColor && !hasEmission)
                continue;

            var originalBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(originalBlock);
            states.Add(new RendererFlashState(
                renderer,
                originalBlock,
                hasColor,
                hasBaseColor,
                hasEmission,
                hasColor ? shared.GetColor(ColorProperty) : Color.white,
                hasBaseColor ? shared.GetColor(BaseColorProperty) : Color.white));
        }

        return states;
    }

    private static void RestoreRenderers(List<RendererFlashState> states)
    {
        foreach (RendererFlashState state in states)
        {
            if (state.Renderer != null)
                state.Renderer.SetPropertyBlock(state.OriginalBlock);
        }
    }

    /// <summary>대상 HDR 발광, Bloom Volume, 주변 Point Light의 생명주기를 한데 묶는다.</summary>
    private sealed class SuccessionGlowState
    {
        private readonly List<RendererFlashState> renderers;
        private readonly Color glowColor;
        private readonly float lightIntensity;
        private readonly GameObject lightObject;
        private readonly Light pointLight;
        private readonly GameObject volumeObject;
        private readonly VolumeProfile volumeProfile;
        private readonly Volume volume;
        private bool cleanedUp;

        public SuccessionGlowState(
            GameObject target,
            Color sourceColor,
            DLJ_SuccessionWillDataSO data)
        {
            renderers = CaptureRenderers(target);
            glowColor = Color.Lerp(sourceColor, Color.white, 0.7f);
            glowColor.a = 1f;
            DLJ_SuccessionEffectSO visual = data != null ? data.successionEffect : null;
            lightIntensity = visual != null ? visual.successionLightIntensity : 1.8f;

            lightObject = new GameObject("Succession Target Light");
            lightObject.transform.position = target.transform.position + Vector3.up * 0.45f;
            pointLight = lightObject.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = glowColor;
            pointLight.range = visual != null ? visual.successionLightRange : 1.6f;
            pointLight.intensity = 0f;
            pointLight.shadows = LightShadows.None;

            volumeObject = new GameObject("Succession Bloom Volume");
            volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1000f;
            volume.weight = 0f;

            volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            volumeProfile.name = "Succession Runtime Bloom";
            Bloom bloom = volumeProfile.Add<Bloom>();
            bloom.active = true;
            bloom.threshold.Override(0.75f);
            bloom.intensity.Override(visual != null ? visual.successionBloomIntensity : 0.8f);
            bloom.scatter.Override(0.65f);
            bloom.tint.Override(glowColor);
            volume.sharedProfile = volumeProfile;
        }

        public void Apply(float strength)
        {
            if (cleanedUp)
                return;

            float clamped = Mathf.Clamp01(strength);
            // 1보다 큰 HDR 색을 넣어 Bloom threshold를 확실히 넘긴다.
            Color hdrGlow = glowColor * Mathf.Lerp(1f, 3.5f, clamped);
            hdrGlow.a = 1f;

            foreach (RendererFlashState state in renderers)
            {
                if (state.Renderer == null)
                    continue;

                var block = new MaterialPropertyBlock();
                state.Renderer.GetPropertyBlock(block);
                if (state.HasColor)
                    block.SetColor(ColorProperty, Color.Lerp(state.Color, hdrGlow, clamped));
                if (state.HasBaseColor)
                    block.SetColor(BaseColorProperty, Color.Lerp(state.BaseColor, hdrGlow, clamped));
                if (state.HasEmission)
                    block.SetColor(EmissionColorProperty, hdrGlow * clamped);
                state.Renderer.SetPropertyBlock(block);
            }

            if (pointLight != null)
                pointLight.intensity = lightIntensity * clamped;
            if (volume != null)
                volume.weight = clamped;
        }

        public void Cleanup()
        {
            if (cleanedUp)
                return;

            cleanedUp = true;
            RestoreRenderers(renderers);
            if (lightObject != null)
                UnityEngine.Object.Destroy(lightObject);
            if (volumeObject != null)
                UnityEngine.Object.Destroy(volumeObject);
            if (volumeProfile != null)
                UnityEngine.Object.Destroy(volumeProfile);
        }
    }

    private sealed class RendererFlashState
    {
        public readonly Renderer Renderer;
        public readonly MaterialPropertyBlock OriginalBlock;
        public readonly bool HasColor;
        public readonly bool HasBaseColor;
        public readonly bool HasEmission;
        public readonly Color Color;
        public readonly Color BaseColor;

        public RendererFlashState(
            Renderer renderer,
            MaterialPropertyBlock originalBlock,
            bool hasColor,
            bool hasBaseColor,
            bool hasEmission,
            Color color,
            Color baseColor)
        {
            Renderer = renderer;
            OriginalBlock = originalBlock;
            HasColor = hasColor;
            HasBaseColor = hasBaseColor;
            HasEmission = hasEmission;
            Color = color;
            BaseColor = baseColor;
        }
    }
}
