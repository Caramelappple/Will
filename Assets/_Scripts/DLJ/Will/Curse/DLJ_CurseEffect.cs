using System;
using DG.Tweening;
using UnityEngine;

public sealed class DLJ_CurseEffect : DLJ_IWillEffect
{
    public void Play(
        GameObject effectObject,
        DLJ_WillEffectContext context,
        Action onComplete = null)
    {
        if (effectObject == null || context == null)
        {
            onComplete?.Invoke();
            return;
        }

        float height = context.data != null ? context.data.effectHeight : 0.12f;
        float expandTime = context.data != null ? context.data.expandTime : 0.25f;
        Transform effectTransform = effectObject.transform;
        effectTransform.position = context.origin + Vector3.up * (height * 0.5f);
        Vector3 targetScale = new Vector3(
            context.areaSize.x,
            height,
            context.areaSize.z);
        effectTransform.localScale = Vector3.zero;
        effectObject.SetActive(true);
        effectTransform.DOScale(targetScale, expandTime).SetEase(Ease.Linear);
        onComplete?.Invoke();
    }
}
