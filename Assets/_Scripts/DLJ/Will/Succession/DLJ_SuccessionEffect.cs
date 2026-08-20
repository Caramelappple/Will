using System;
using System.Collections.Generic;
using _Scripts.LSO.Will;
using DG.Tweening;
using UnityEngine;

public sealed class DLJ_SuccessionEffect : DLJ_IWillEffect
{
    public void Play(
        GameObject effectObject,
        DLJ_WillEffectContext context,
        Action onComplete = null)
    {
        if (context == null || context.target == null)
        {
            if (effectObject != null)
                UnityEngine.Object.Destroy(effectObject);
            onComplete?.Invoke();
            return;
        }

        if (effectObject == null)
        {
            onComplete?.Invoke();
            return;
        }

        DLJ_SuccessionWillDataSO data = context.data as DLJ_SuccessionWillDataSO;
        DLJ_SuccessionEffectSO visual = data != null ? data.successionEffect : null;
        float lineHeight = visual != null ? visual.successionLineHeight : 0.35f;
        Vector3 rotation = visual != null
            ? visual.successionRotation
            : new Vector3(0f, 360f, 0f);
        float travelDuration = data != null ? data.moveDuration : 1f;
        float absorbDuration = visual != null ? visual.successionAbsorbDuration : 0.22f;

        Vector3 start = context.origin + Vector3.up * lineHeight;
        Vector3 end = context.targetPosition + Vector3.up * lineHeight;
        Transform effectTransform = effectObject.transform;
        effectTransform.position = start;
        effectObject.SetActive(true);

        TrailRenderer[] trails = effectObject.GetComponentsInChildren<TrailRenderer>(true);
        AnimationCurve[] trailWidthCurves = new AnimationCurve[trails.Length];
        for (int i = 0; i < trails.Length; i++)
            trailWidthCurves[i] = trails[i].widthCurve;

        Sequence sequence = DOTween.Sequence()
            .AppendInterval(2f)
            .SetUpdate(true)
            .Append(effectTransform
                .DOMove(end, Mathf.Max(0f, travelDuration))
                .SetEase(Ease.Linear))
            .Join(effectTransform
                .DORotate(rotation, Mathf.Max(0f, travelDuration), RotateMode.LocalAxisAdd)
                .SetEase(Ease.Linear))
            .AppendCallback(() =>
            {
                foreach (TrailRenderer trail in trails)
                {
                    if (trail != null)
                        trail.emitting = false;
                }
            })
            .Append(effectTransform
                .DOScale(Vector3.zero, Mathf.Max(0f, absorbDuration))
                .SetEase(Ease.InCubic))
            .Join(DOVirtual.Float(0f, 1f, Mathf.Max(0f, absorbDuration), value =>
            {
                float eased = DOVirtual.EasedValue(0f, 1f, value, Ease.Linear);
                for (int i = 0; i < trails.Length; i++)
                {
                    TrailRenderer trail = trails[i];
                    if (trail == null)
                        continue;

                    ApplyTrailAbsorption(trail, trailWidthCurves[i], eased);
                }
            }).SetEase(Ease.Linear))
            .AppendCallback(() => UnityEngine.Object.Destroy(effectObject))
            .OnComplete(() => onComplete?.Invoke())
            .OnKill(() =>
            {
                if (effectObject != null)
                    UnityEngine.Object.Destroy(effectObject);
            });

        // 0초 설정에서도 콜백과 정리 순서를 DOTween이 끝까지 실행하게 시퀀스를 유지한다.
        sequence.Play();
    }

    /// <summary>Trail의 오래된 끝(시작점)부터 현재 위치(도착점) 방향으로 폭을 지운다.</summary>
    private static void ApplyTrailAbsorption(
        TrailRenderer trail,
        AnimationCurve sourceCurve,
        float progress)
    {
        float clamped = Mathf.Clamp01(progress);
        if (clamped <= 0f)
        {
            trail.widthCurve = sourceCurve;
            return;
        }

        if (clamped >= 1f)
        {
            trail.widthCurve = AnimationCurve.Constant(0f, 1f, 0f);
            return;
        }

        // Trail의 0은 현재 위치, 1은 가장 오래된 끝이므로 경계를 1에서 0으로 옮긴다.
        float cutoff = 1f - clamped;
        float fadeStart = Mathf.Max(0f, cutoff - 0.08f);
        var keys = new List<Keyframe>();

        foreach (Keyframe key in sourceCurve.keys)
        {
            if (key.time < fadeStart)
                keys.Add(key);
        }

        if (keys.Count == 0 || keys[0].time > 0f)
            keys.Insert(0, new Keyframe(0f, sourceCurve.Evaluate(0f)));

        if (fadeStart > 0f && (keys.Count == 0 || keys[keys.Count - 1].time < fadeStart))
            keys.Add(new Keyframe(fadeStart, sourceCurve.Evaluate(fadeStart)));

        keys.Add(new Keyframe(cutoff, 0f));
        keys.Add(new Keyframe(1f, 0f));

        var absorbedCurve = new AnimationCurve(keys.ToArray())
        {
            preWrapMode = sourceCurve.preWrapMode,
            postWrapMode = sourceCurve.postWrapMode
        };
        trail.widthCurve = absorbedCurve;
    }

}
