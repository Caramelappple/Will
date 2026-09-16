using System;
using UnityEngine;

public sealed class DLJ_ContractEffect : DLJ_IWillEffect
{
    public void Play(
        GameObject effectObject,
        DLJ_WillEffectContext context,
        Action onComplete = null)
    {
        if (effectObject != null)
        {
            effectObject.SetActive(true);
            DLJ_ContractWillDataSO data = context?.data as DLJ_ContractWillDataSO;
            float lifetime = data != null
                ? Mathf.Max(0f, data.holdTime)
                : 0f;

            ParticleSystem[] particleSystems =
                effectObject.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                particleSystem.gameObject.SetActive(true);
                ParticleSystem.MainModule main = particleSystem.main;
                main.loop = false;
                // Keep the last emitted particles alive after emission ends.
                lifetime = Mathf.Max(
                    lifetime,
                    main.startDelay.constantMax +
                    main.duration +
                    main.startLifetime.constantMax);
                particleSystem.Stop(
                    false,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
                particleSystem.Play(false);
            }

            UnityEngine.Object.Destroy(effectObject, lifetime);
        }

        onComplete?.Invoke();
    }
}
