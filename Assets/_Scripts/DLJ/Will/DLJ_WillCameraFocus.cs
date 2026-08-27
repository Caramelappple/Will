using DG.Tweening;
using UnityEngine;

/// <summary>모든 유언 발동 위치를 짧게 강조하는 공통 카메라 연출.</summary>
internal static class DLJ_WillCameraFocus
{
    private const float FocusStrength = 0.35f;
    private const float ZoomRatio = 0.88f;
    private const float ZoomInDuration = 0.18f;
    private const float RestoreDuration = 0.28f;

    private static Camera activeCamera;
    private static Behaviour cameraController;
    private static Sequence sequence;
    private static Vector3 homePosition;
    private static float homeLens;
    private static bool controllerWasEnabled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        sequence?.Kill(false);
        sequence = null;
        activeCamera = null;
        cameraController = null;
        controllerWasEnabled = false;
    }

    public static void Play(Vector3 focusPosition, float holdDuration)
    {
        RestoreImmediately();

        activeCamera = Camera.main != null
            ? Camera.main
            : Object.FindFirstObjectByType<Camera>();
        if (activeCamera == null)
            return;

        Transform cameraTransform = activeCamera.transform;
        homePosition = cameraTransform.position;
        homeLens = GetLens();

        cameraController =
            activeCamera.GetComponent("CinemachineBrain") as Behaviour;
        controllerWasEnabled =
            cameraController != null && cameraController.enabled;
        if (controllerWasEnabled)
            cameraController.enabled = false;

        Vector3 targetPosition = CalculateFocusPosition(
            cameraTransform,
            focusPosition);
        float targetLens = Mathf.Max(0.01f, homeLens * ZoomRatio);

        sequence = DOTween.Sequence()
            .SetUpdate(true)
            .Append(cameraTransform
                .DOMove(targetPosition, ZoomInDuration)
                .SetEase(Ease.OutSine))
            .Join(DOVirtual.Float(
                    homeLens,
                    targetLens,
                    ZoomInDuration,
                    SetLens)
                .SetEase(Ease.OutSine))
            .AppendInterval(Mathf.Max(0f, holdDuration))
            .Append(cameraTransform
                .DOMove(homePosition, RestoreDuration)
                .SetEase(Ease.InOutSine))
            .Join(DOVirtual.Float(
                    targetLens,
                    homeLens,
                    RestoreDuration,
                    SetLens)
                .SetEase(Ease.InOutSine))
            .OnComplete(Finish);
    }

    private static Vector3 CalculateFocusPosition(
        Transform cameraTransform,
        Vector3 focusPosition)
    {
        Vector3 forward = cameraTransform.forward;
        if (Mathf.Abs(forward.y) < 0.001f)
            return cameraTransform.position;

        float distanceToPlane =
            (focusPosition.y - cameraTransform.position.y) / forward.y;
        if (distanceToPlane <= 0f)
            return cameraTransform.position;

        Vector3 currentFocus =
            cameraTransform.position + forward * distanceToPlane;
        Vector3 offset = focusPosition - currentFocus;
        offset.y = 0f;
        return cameraTransform.position + offset * FocusStrength;
    }

    private static float GetLens()
    {
        return activeCamera.orthographic
            ? activeCamera.orthographicSize
            : activeCamera.fieldOfView;
    }

    private static void SetLens(float value)
    {
        if (activeCamera == null)
            return;

        if (activeCamera.orthographic)
            activeCamera.orthographicSize = value;
        else
            activeCamera.fieldOfView = value;
    }

    private static void Finish()
    {
        if (activeCamera != null)
        {
            activeCamera.transform.position = homePosition;
            SetLens(homeLens);
        }

        if (cameraController != null && controllerWasEnabled)
            cameraController.enabled = true;

        sequence = null;
        activeCamera = null;
        cameraController = null;
        controllerWasEnabled = false;
    }

    private static void RestoreImmediately()
    {
        if (sequence == null || !sequence.IsActive())
            return;

        sequence.Kill(false);
        Finish();
    }
}
