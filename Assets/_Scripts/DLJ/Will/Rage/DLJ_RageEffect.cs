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

        float height = data.effectHeight;
        Transform effectTransform = effectObject.transform;
        effectTransform.position = context.origin + Vector3.up * (height * 0.5f);
        Vector3 targetScale = new Vector3(
            context.areaSize.x,
            height,
            context.areaSize.z);
        effectTransform.localScale = Vector3.zero;
        effectObject.SetActive(true);

        DOTween.Sequence()
            .Append(effectTransform.DOScale(targetScale, data.expandTime).SetEase(Ease.Linear))
            .AppendInterval(data.holdTime)
            .Append(effectTransform.DOScale(Vector3.zero, data.expandTime).SetEase(Ease.Linear))
            .OnComplete(() =>
            {
                UnityEngine.Object.Destroy(effectObject);
                onComplete?.Invoke();
            });
    }
}
