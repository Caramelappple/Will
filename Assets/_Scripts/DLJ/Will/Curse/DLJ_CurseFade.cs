using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>Fades the floor, additive border and live particles as one effect.</summary>
public sealed class DLJ_CurseFade : MonoBehaviour
{
    private readonly List<RendererFade> renderers = new();
    private readonly List<ParticleFade> particles = new();
    private Tween tween;
    private float opacity = 1f;

    private void Awake()
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null) continue;
                string property = material.HasProperty("_DLJ_Fade") ? "_DLJ_Fade"
                    : material.HasProperty("_Intensity") ? "_Intensity" : null;
                if (property != null)
                    renderers.Add(new RendererFade(renderer, i, material, property));
            }
        }

        foreach (ParticleSystem system in GetComponentsInChildren<ParticleSystem>(true))
            particles.Add(new ParticleFade(system));
    }

    public void FadeIn(float duration, Action onComplete = null)
    {
        tween?.Kill();
        SetOpacity(0f);
        FadeTo(1f, duration, onComplete);
    }

    public void FadeOut(float duration)
    {
        // An early expiry must continue from the current fade-in opacity.
        FadeTo(0f, duration, () => Destroy(gameObject));
    }

    private void FadeTo(float target, float duration, Action onComplete)
    {
        tween?.Kill();
        if (duration <= 0f)
        {
            SetOpacity(target);
            onComplete?.Invoke();
            return;
        }

        tween = DOTween.To(() => opacity, SetOpacity, target, duration)
            .SetEase(Ease.InOutSine)
            .SetLink(gameObject)
            .OnComplete(() => onComplete?.Invoke());
    }

    private void SetOpacity(float value)
    {
        opacity = Mathf.Clamp01(value);
        foreach (RendererFade renderer in renderers) renderer.Apply(opacity);
        foreach (ParticleFade particle in particles) particle.Apply(opacity);
    }

    private void OnDestroy() => tween?.Kill();

    private sealed class RendererFade
    {
        private readonly Renderer renderer;
        private readonly int index;
        private readonly int property;
        private readonly float original;
        private readonly MaterialPropertyBlock block = new();

        public RendererFade(Renderer renderer, int index, Material material, string property)
        {
            this.renderer = renderer;
            this.index = index;
            this.property = Shader.PropertyToID(property);
            original = material.GetFloat(this.property);
        }

        public void Apply(float opacity)
        {
            if (renderer == null) return;
            renderer.GetPropertyBlock(block, index);
            block.SetFloat(property, original * opacity);
            renderer.SetPropertyBlock(block, index);
        }
    }

    private sealed class ParticleFade
    {
        private readonly ParticleSystem system;
        private readonly ParticleSystem.MinMaxGradient original;
        private readonly GradientFade min;
        private readonly GradientFade max;

        public ParticleFade(ParticleSystem system)
        {
            this.system = system;
            var module = system.colorOverLifetime;
            original = module.enabled ? module.color : new ParticleSystem.MinMaxGradient(Color.white);
            if (original.gradientMin != null) min = new GradientFade(original.gradientMin);
            if (original.gradientMax != null) max = new GradientFade(original.gradientMax);
        }

        public void Apply(float opacity)
        {
            if (system == null) return;
            var value = original;
            Color low = original.colorMin;
            Color high = original.colorMax;
            low.a *= opacity;
            high.a *= opacity;
            value.colorMin = low;
            value.colorMax = high;
            if (min != null) value.gradientMin = min.Apply(opacity);
            if (max != null) value.gradientMax = max.Apply(opacity);
            value.mode = original.mode;
            var module = system.colorOverLifetime;
            module.enabled = true;
            module.color = value;
        }
    }

    private sealed class GradientFade
    {
        private readonly Gradient gradient = new();
        private readonly GradientColorKey[] colors;
        private readonly GradientAlphaKey[] original;
        private readonly GradientAlphaKey[] faded;

        public GradientFade(Gradient source)
        {
            colors = source.colorKeys;
            original = source.alphaKeys;
            faded = new GradientAlphaKey[original.Length];
            gradient.mode = source.mode;
            gradient.colorSpace = source.colorSpace;
        }

        public Gradient Apply(float opacity)
        {
            for (int i = 0; i < original.Length; i++)
                faded[i] = new GradientAlphaKey(original[i].alpha * opacity, original[i].time);
            gradient.SetKeys(colors, faded);
            return gradient;
        }
    }
}
