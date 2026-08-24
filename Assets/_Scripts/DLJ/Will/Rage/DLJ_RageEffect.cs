using System;
using DG.Tweening;
using UnityEngine;

public sealed class DLJ_RageEffect : DLJ_IWillEffect
{
    public void Play(
        GameObject effectObject,
        DLJ_WillEffectContext context,
        Action onComplete = null)
    {
        DLJ_RageWillDataSO data = context?.data as DLJ_RageWillDataSO;
        if (effectObject == null || context == null || data == null)
        {
            onComplete?.Invoke();
            return;
        }

        Transform effectTransform = effectObject.transform;
        effectTransform.position =
            context.origin + Vector3.up * (data.effectHeight * 0.5f);
        effectObject.SetActive(true);

        ParticleSystem[] particleSystems =
            effectObject.GetComponentsInChildren<ParticleSystem>(true);
        float playbackDuration =
            Mathf.Max(0f, data.expandTime * 2f + data.holdTime);

        foreach (ParticleSystem particleSystem in particleSystems)
        {
            particleSystem.gameObject.SetActive(true);
            ParticleSystem.MainModule main = particleSystem.main;
            playbackDuration = Mathf.Max(
                playbackDuration,
                main.startDelay.constantMax + main.startLifetime.constantMax);
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Play(false);
        }

        DOVirtual.DelayedCall(playbackDuration, () =>
            {
                UnityEngine.Object.Destroy(effectObject);
                onComplete?.Invoke();
            })
            .SetLink(effectObject)
            .SetUpdate(UpdateType.Normal);
    }
}
