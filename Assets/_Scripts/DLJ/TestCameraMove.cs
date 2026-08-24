using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestCameraMove : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera cameraPoint1;
    [SerializeField] private Camera cameraPoint2;

    [Header("Timing")]
    [Min(0f)] [SerializeField] private float moveToPoint2Duration = 1f;
    [Min(0f)] [SerializeField] private float point2HoldDuration = 0.25f;
    [Min(0f)] [SerializeField] private float returnDuration = 1f;
    [SerializeField] private bool playOnSpace = true;

    private Vector3 homePosition;
    private Quaternion homeRotation;
    private float homeFieldOfView;
    private float homeOrthographicSize;
    private bool homeOrthographic;
    private Behaviour cameraController;
    private bool wasCameraControllerEnabled;
    private Sequence moveSequence;

    private void Awake()
    {
        ResolveCameras();

        if (mainCamera == null)
        {
            Debug.LogError("TestCameraMove: Main Camera를 찾을 수 없어.", this);
            enabled = false;
            return;
        }

        SaveHomeView();
        DisableCameraPoint(cameraPoint1);
        DisableCameraPoint(cameraPoint2);
    }

    private void Update()
    {
        if (playOnSpace &&
            Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
            PlayCameraMove();
    }

    public void PlayCameraMove()
    {
        if (mainCamera == null || cameraPoint1 == null || cameraPoint2 == null)
        {
            Debug.LogError(
                "TestCameraMove: Main Camera, Main Camera (1), Main Camera (2)가 모두 필요해.",
                this);
            return;
        }

        if (moveSequence != null && moveSequence.IsActive())
            return;

        SuspendMainCameraController();
        ApplyView(cameraPoint1);

        moveSequence = DOTween.Sequence()
            .SetUpdate(true)
            .Append(mainCamera.transform
                .DOMove(cameraPoint2.transform.position, moveToPoint2Duration)
                .SetEase(Ease.Linear))
            .Join(mainCamera.transform
                .DORotateQuaternion(cameraPoint2.transform.rotation, moveToPoint2Duration)
                .SetEase(Ease.Linear))
            .Join(CreateLensTween(
                GetCurrentLensValue(),
                GetLensValue(cameraPoint2),
                moveToPoint2Duration))
            .AppendInterval(point2HoldDuration)
            .Append(mainCamera.transform
                .DOMove(homePosition, returnDuration)
                .SetEase(Ease.Linear))
            .Join(mainCamera.transform
                .DORotateQuaternion(homeRotation, returnDuration)
                .SetEase(Ease.Linear))
            .Join(CreateLensTween(
                GetLensValue(cameraPoint2),
                GetHomeLensValue(),
                returnDuration))
            .OnComplete(FinishCameraMove);
    }

    private void ResolveCameras()
    {
        if (mainCamera == null)
            mainCamera = FindCamera("Main Camera");
        if (cameraPoint1 == null)
            cameraPoint1 = FindCamera("Main Camera (1)");
        if (cameraPoint2 == null)
            cameraPoint2 = FindCamera("Main Camera (2)");
    }

    private static Camera FindCamera(string objectName)
    {
        GameObject cameraObject = GameObject.Find(objectName);
        return cameraObject != null ? cameraObject.GetComponent<Camera>() : null;
    }

    private void SaveHomeView()
    {
        homePosition = mainCamera.transform.position;
        homeRotation = mainCamera.transform.rotation;
        homeFieldOfView = mainCamera.fieldOfView;
        homeOrthographicSize = mainCamera.orthographicSize;
        homeOrthographic = mainCamera.orthographic;
    }

    private static void DisableCameraPoint(Camera cameraPoint)
    {
        if (cameraPoint == null)
            return;

        cameraPoint.enabled = false;
        if (cameraPoint.TryGetComponent(out AudioListener listener))
            listener.enabled = false;
    }

    private void ApplyView(Camera source)
    {
        mainCamera.transform.SetPositionAndRotation(
            source.transform.position,
            source.transform.rotation);
        mainCamera.orthographic = source.orthographic;
        mainCamera.fieldOfView = source.fieldOfView;
        mainCamera.orthographicSize = source.orthographicSize;
    }

    private Tween CreateLensTween(float from, float to, float duration)
    {
        return DOVirtual.Float(from, to, duration, SetCurrentLensValue)
            .SetEase(Ease.InOutSine);
    }

    private float GetCurrentLensValue()
    {
        return mainCamera.orthographic
            ? mainCamera.orthographicSize
            : mainCamera.fieldOfView;
    }

    private static float GetLensValue(Camera source)
    {
        return source.orthographic
            ? source.orthographicSize
            : source.fieldOfView;
    }

    private float GetHomeLensValue()
    {
        return homeOrthographic ? homeOrthographicSize : homeFieldOfView;
    }

    private void SetCurrentLensValue(float value)
    {
        if (mainCamera.orthographic)
            mainCamera.orthographicSize = value;
        else
            mainCamera.fieldOfView = value;
    }

    private void SuspendMainCameraController()
    {
        // Cinemachine Brain 같은 외부 카메라 제어기가 위치를 덮어쓰지 못하게 잠시 중지한다.
        cameraController = mainCamera.GetComponent("CinemachineBrain") as Behaviour;
        wasCameraControllerEnabled = cameraController != null && cameraController.enabled;
        if (wasCameraControllerEnabled)
            cameraController.enabled = false;
    }

    private void FinishCameraMove()
    {
        mainCamera.orthographic = homeOrthographic;
        mainCamera.fieldOfView = homeFieldOfView;
        mainCamera.orthographicSize = homeOrthographicSize;

        if (cameraController != null && wasCameraControllerEnabled)
            cameraController.enabled = true;

        moveSequence = null;
    }

    private void OnDisable()
    {
        if (moveSequence == null || !moveSequence.IsActive())
            return;

        moveSequence.Kill(false);
        mainCamera.transform.SetPositionAndRotation(homePosition, homeRotation);
        FinishCameraMove();
    }
}
