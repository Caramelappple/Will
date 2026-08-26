using System;
using UnityEngine;

public sealed class DLJ_SacrificeEffect : DLJ_IWillEffect
{
    public void Play(
        GameObject effectObject,
        DLJ_WillEffectContext context,
        Action onComplete = null)
    {
        if (effectObject == null)
        {
            onComplete?.Invoke();
            return;
        }

        DLJ_SacrificeWillDataSO data =
            context?.data as DLJ_SacrificeWillDataSO;
        float lifetime = data != null
            ? Mathf.Max(0f, data.holdTime)
            : 0f;

        Transform effectTransform = effectObject.transform;
        effectTransform.position = context.origin;
        if (context.areaSize.x > 0f && context.areaSize.z > 0f)
        {
            float uniformScale =
                Mathf.Max(context.areaSize.x, context.areaSize.z);
            effectTransform.localScale = Vector3.Scale(
                effectTransform.localScale,
                Vector3.one * uniformScale);
        }

        effectObject.SetActive(true);
        ParticleSystem[] particleSystems =
            effectObject.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem particleSystem in particleSystems)
        {
            particleSystem.gameObject.SetActive(true);
            ParticleSystem.MainModule main = particleSystem.main;
            lifetime = Mathf.Max(
                lifetime,
                main.startDelay.constantMax + main.startLifetime.constantMax);
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Play(false);
        }

        UnityEngine.Object.Destroy(effectObject, lifetime);
        onComplete?.Invoke();
    }
}
