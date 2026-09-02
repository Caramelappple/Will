using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
public struct KTH_CameraZoomSettings
{
    public float zoomedOrthoSize;
    public float zoomedFieldOfView;
    public float duration;
    public Ease ease;

    public static KTH_CameraZoomSettings Default => new KTH_CameraZoomSettings
    {
        zoomedOrthoSize = 3f,
        zoomedFieldOfView = 40f,
        duration = 0.4f,
        ease = Ease.OutQuad
    };
}

// =========================================================
// SRP: 카메라 확대/복귀 연출만 담당한다.
//
// 연산량 최적화: Camera.main은 내부적으로 태그로 오브젝트를 검색하기 때문에
// 호출할 때마다 비용이 든다. 생성 시점에 한 번만 찾아서 캐싱해 재검색을 없앤다.
// =========================================================
public interface ICameraZoomService : IDisposable
{
    void ZoomIn();
    void ZoomOut();
}

public sealed class KTH_CameraZoomService : ICameraZoomService
{
    private readonly Camera targetCamera;
    private readonly KTH_CameraZoomSettings settings;
    private Tween zoomTween;
    private float originalOrthoSize;
    private float originalFieldOfView;
    private bool hasCachedOriginal;

    public KTH_CameraZoomService(Camera camera, KTH_CameraZoomSettings settings)
    {
        // 기본 세팅: 카메라를 지정하지 않으면 Camera.main으로 자동 대체한다.
        targetCamera = camera != null ? camera : Camera.main;
        this.settings = settings;
    }

    public void ZoomIn()
    {
        if (targetCamera == null)
        {
            return;
        }
        CacheOriginalIfNeeded();
        zoomTween?.Kill();
        zoomTween = targetCamera.orthographic
            ? targetCamera.DOOrthoSize(settings.zoomedOrthoSize, settings.duration).SetEase(settings.ease)
            : targetCamera.DOFieldOfView(settings.zoomedFieldOfView, settings.duration).SetEase(settings.ease);
    }

    public void ZoomOut()
    {
        if (targetCamera == null || !hasCachedOriginal)
        {
            return;
        }
        zoomTween?.Kill();
        zoomTween = targetCamera.orthographic
            ? targetCamera.DOOrthoSize(originalOrthoSize, settings.duration).SetEase(settings.ease)
            : targetCamera.DOFieldOfView(originalFieldOfView, settings.duration).SetEase(settings.ease);
    }

    private void CacheOriginalIfNeeded()
    {
        if (hasCachedOriginal)
        {
            return;
        }
        originalOrthoSize = targetCamera.orthographicSize;
        originalFieldOfView = targetCamera.fieldOfView;
        hasCachedOriginal = true;
    }

    public void Dispose()
    {
        zoomTween?.Kill();
    }
}
