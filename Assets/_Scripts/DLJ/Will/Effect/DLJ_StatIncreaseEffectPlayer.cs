using UnityEngine;

/// <summary>능력치 증가 대상에게 공통 파티클 연출을 재생한다.</summary>
internal static class DLJ_StatIncreaseEffectPlayer
{
    private const float FallbackLifetime = 1f;

    public static void Play(
        GameObject target,
        DLJ_StatIncreaseEffectSO settings)
    {
        if (target == null || settings == null || settings.effectPrefab == null)
            return;

        GameObject effectObject = Object.Instantiate(
            settings.effectPrefab,
            target.transform.position,
            settings.effectPrefab.transform.rotation,
            target.transform);
        effectObject.SetActive(true);

        ParticleSystem[] particleSystems =
            effectObject.GetComponentsInChildren<ParticleSystem>(true);
        float lifetime = 0f;

        foreach (ParticleSystem particleSystem in particleSystems)
        {
            particleSystem.gameObject.SetActive(true);
            ParticleSystem.MainModule main = particleSystem.main;
            main.loop = false;
            lifetime = Mathf.Max(
                lifetime,
                main.startDelay.constantMax +
                main.duration +
                main.startLifetime.constantMax);
            particleSystem.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Play(false);
        }

        Object.Destroy(
            effectObject,
            lifetime > 0f ? lifetime : FallbackLifetime);
    }
}
