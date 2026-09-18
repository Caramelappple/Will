using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>까마귀왕 페이즈 연출 동안 렌즈를 좁혔다가 원래 배율로 복구.</summary>
public sealed class DLJ_CorvoKingCameraZoom : MonoBehaviour
{
    private readonly List<DLJ_CorvoKingZoomExtension> extensions = new List<DLJ_CorvoKingZoomExtension>();
    private Camera fallbackCamera;
    private float originalFov, originalSize;
    private double startedAt;
    private float zoomRatio, inDuration, outStart, outDuration;
    private AnimationCurve inCurve, outCurve;
    private bool playing;

    public float LensRatio
    {
        get
        {
            if (!playing) return 1f;
            float time = (float)(Time.unscaledTimeAsDouble - startedAt);
            if (time < inDuration)
                return Mathf.Lerp(1f, zoomRatio, Evaluate(inCurve, time / inDuration));
            if (time < outStart) return zoomRatio;
            return Mathf.Lerp(zoomRatio, 1f, Evaluate(outCurve, (time - outStart) / outDuration));
        }
    }

    public void Play(float ratio, float zoomInSeconds, float returnAtSeconds, float zoomOutSeconds,
        AnimationCurve enterCurve, AnimationCurve exitCurve)
    {
        Restore();
        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("까마귀왕 줌 연출: MainCamera를 찾지 못했어.", this);
            return;
        }
        zoomRatio = Mathf.Clamp(ratio, 0.5f, 1f);
        inDuration = Mathf.Max(0.01f, zoomInSeconds);
        outStart = Mathf.Max(inDuration, returnAtSeconds);
        outDuration = Mathf.Max(0.01f, zoomOutSeconds);
        inCurve = enterCurve;
        outCurve = exitCurve;
        startedAt = Time.unscaledTimeAsDouble;
        playing = true;

        var brain = mainCamera.GetComponent<CinemachineBrain>();
        if (brain != null && brain.isActiveAndEnabled)
        {
            // 개별 CinemachineCamera의 계산 결과만 수정. 블렌드 중에도 같은 배율을 적용.
            foreach (var camera in FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if ((camera.OutputChannel & brain.ChannelMask) == 0) continue;
                var extension = camera.gameObject.AddComponent<DLJ_CorvoKingZoomExtension>();
                extension.hideFlags = HideFlags.DontSave;
                extension.Owner = this;
                extensions.Add(extension);
            }
            if (extensions.Count == 0)
            {
                Debug.LogWarning("까마귀왕 줌 연출: 연결할 CinemachineCamera가 없어.", this);
                Restore();
            }
        }
        else
        {
            fallbackCamera = mainCamera;
            originalFov = mainCamera.fieldOfView;
            originalSize = mainCamera.orthographicSize;
        }
    }

    private static float Evaluate(AnimationCurve curve, float t)
    {
        t = Mathf.Clamp01(t);
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;
        return Mathf.Clamp01(curve != null && curve.length > 0 ? curve.Evaluate(t) : Mathf.SmoothStep(0f, 1f, t));
    }

    private void LateUpdate()
    {
        if (!playing) return;
        if (fallbackCamera != null)
        {
            fallbackCamera.fieldOfView = Mathf.Clamp(originalFov * LensRatio, 1f, 179f);
            fallbackCamera.orthographicSize = Mathf.Max(0.001f, originalSize * LensRatio);
        }
        if (Time.unscaledTimeAsDouble - startedAt >= outStart + outDuration) Restore();
    }

    public void Restore()
    {
        playing = false;
        foreach (var extension in extensions)
        {
            if (extension == null) continue;
            extension.Owner = null;
            Destroy(extension);
        }
        extensions.Clear();
        if (fallbackCamera != null)
        {
            fallbackCamera.fieldOfView = originalFov;
            fallbackCamera.orthographicSize = originalSize;
        }
        fallbackCamera = null;
    }

    private void OnDisable() => Restore();
    private void OnDestroy() => Restore();
}
