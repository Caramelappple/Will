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
            UnityEngine.Object.Destroy(effectObject, lifetime);
        }

        onComplete?.Invoke();
    }
}
