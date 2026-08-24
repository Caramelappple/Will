using System;
using UnityEngine;

public sealed class DLJ_CurseEffect : DLJ_IWillEffect
{
    public void Play(
        GameObject effectObject,
        DLJ_WillEffectContext context,
        Action onComplete = null)
    {
        DLJ_CurseWillDataSO data = context?.data as DLJ_CurseWillDataSO;
        if (effectObject == null || context == null || data == null)
        {
            onComplete?.Invoke();
            return;
        }

        Transform effectTransform = effectObject.transform;
        effectTransform.position = context.origin;

        float areaWorldSize =
            (Mathf.Abs(context.areaSize.x) + Mathf.Abs(context.areaSize.z)) * 0.5f;
        float effectScale = areaWorldSize * data.effectScalePerWorldUnit;
        effectTransform.localScale = Vector3.one * effectScale;

        effectObject.SetActive(true);

        ParticleSystem[] particleSystems =
            effectObject.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem particleSystem in particleSystems)
        {
            ParticleSystem.MainModule main = particleSystem.main;
            main.loop = true;
            particleSystem.Play(false);
        }

        onComplete?.Invoke();
    }
}
