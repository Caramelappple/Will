using System;
using UnityEngine;

public sealed class DLJ_SacrificeEffect : DLJ_IWillEffect
{
    public void Play(
        GameObject effectObject,
        DLJ_WillEffectContext context,
        Action onComplete = null)
    {
        if (effectObject != null)
        {
            effectObject.SetActive(true);
            float lifetime = context?.data != null
                ? Mathf.Max(0f, context.data.holdTime)
                : 0f;
            UnityEngine.Object.Destroy(effectObject, lifetime);
        }

        onComplete?.Invoke();
    }
}
