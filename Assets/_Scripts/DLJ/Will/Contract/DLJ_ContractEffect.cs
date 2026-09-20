using System;
using _Scripts.LDY;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DLJ_ContractEffect : DLJ_IWillEffect
{
    public DLJ_PigCoinPayout PlayCoins(DLJ_WillEffectContext context, int amount,
        LDY_TurnManager turns, LDY_ActionPointManager points)
    {
        if (amount <= 0 || turns == null || points == null ||
            context?.data is not DLJ_ContractWillDataSO data)
            return null;

        Vector3 center = context.origin + Vector3.up * 0.35f;
        float ground = context.origin.y;
        if (context.owner != null)
        {
            LDY_Animal animal = context.owner.GetComponent<LDY_Animal>();
            Transform model = animal != null && animal.modelTransform != null
                ? animal.modelTransform : context.owner.transform;
            bool found = false;
            Bounds bounds = default;
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is not MeshRenderer && renderer is not SkinnedMeshRenderer) continue;
                if (renderer.GetComponent<TMPro.TMP_Text>() != null) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            if (found)
            {
                center = bounds.center;
                ground = bounds.min.y;
            }
        }

        GameObject root = new GameObject("DLJ_ContractCoins");
        if (context.owner != null && context.owner.scene.IsValid())
            SceneManager.MoveGameObjectToScene(root, context.owner.scene);
        root.transform.position = center;
        DLJ_PigCoinPayout payout = root.AddComponent<DLJ_PigCoinPayout>();
        payout.Initialize(amount, turns, points, data.coinMesh, data.goldMaterial,
            data.coinDiameter, data.scatterRadius, 0.45f,
            data.collectionDuration, data.collectionInterval, ground,
            externalPayout: true);
        return payout;
    }

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
