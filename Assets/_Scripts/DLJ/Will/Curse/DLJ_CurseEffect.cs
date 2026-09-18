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
        effectTransform.position = context.origin + Vector3.up * data.effectHeightOffset;

        // The floor mesh defines the footprint; animated smoke bounds do not.
        MeshRenderer floor = effectObject.GetComponent<MeshRenderer>();
        float scaleRatio = 1f;
        bool fitFloor = floor != null &&
            floor.bounds.size.x > Mathf.Epsilon &&
            floor.bounds.size.z > Mathf.Epsilon;
        if (fitFloor)
        {
            Vector3 footprint = floor.bounds.size;
            scaleRatio = Mathf.Min(
                Mathf.Abs(context.areaSize.x) / footprint.x,
                Mathf.Abs(context.areaSize.z) / footprint.z);
            effectTransform.localScale *= scaleRatio;
        }
        else
        {
            // Preserve the original calibration for particle-only prefabs.
            float areaWorldSize =
                (Mathf.Abs(context.areaSize.x) + Mathf.Abs(context.areaSize.z)) * 0.5f;
            effectTransform.localScale =
                Vector3.one * (areaWorldSize * data.effectScalePerWorldUnit);
        }

        effectObject.SetActive(true);

        ParticleSystem[] particleSystems =
            effectObject.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem particleSystem in particleSystems)
        {
            particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = particleSystem.main;
            // Local mode ignores the root scale, unlike the floor and border meshes.
            if (fitFloor && main.scalingMode == ParticleSystemScalingMode.Local)
                particleSystem.transform.localScale *= scaleRatio;

            main.loop = true;
            particleSystem.gameObject.SetActive(true);
            particleSystem.Play(false);
        }

        DLJ_CurseFade fade = effectObject.GetComponent<DLJ_CurseFade>();
        if (fade == null) fade = effectObject.AddComponent<DLJ_CurseFade>();
        fade.FadeIn(data.effectFadeInTime, onComplete);
    }
}
