using System;
using DG.Tweening;
using UnityEngine;

public sealed class DLJ_SuccessionEffect : DLJ_IWillEffect
{
    public void Play(
        GameObject effectObject,
        DLJ_WillEffectContext context,
        Action onComplete = null)
    {
        if (effectObject == null || context == null || context.target == null)
        {
            onComplete?.Invoke();
            return;
        }

        float duration = context.data != null ? context.data.moveDuration : 1f;
        effectObject.transform
            .DOMove(context.targetPosition, duration)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                UnityEngine.Object.Destroy(effectObject);
                onComplete?.Invoke();
            });
    }
}
