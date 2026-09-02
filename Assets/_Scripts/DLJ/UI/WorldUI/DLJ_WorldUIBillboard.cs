using UnityEngine;

namespace _Scripts.DLJ.UI.WorldUI
{
    /// <summary>월드 렌더러 UI가 카메라 회전을 따라가게 한다.</summary>
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    public sealed class DLJ_WorldUIBillboard : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private bool lockVerticalTilt;
        [Tooltip("스프라이트나 3D 텍스트의 앞면이 반대로 보일 때 사용한다.")]
        [SerializeField] private bool flipForward;

        private Transform _cameraTransform;
        private bool _reportedMissingCamera;
        private int _nextCameraRefreshFrame;

        private void OnEnable()
        {
            ResolveCamera();
        }

        private void LateUpdate()
        {
            if (targetCamera == null && Time.frameCount >= _nextCameraRefreshFrame)
            {
                ResolveCamera();
                _nextCameraRefreshFrame = Time.frameCount + 30;
            }

            if (_cameraTransform == null)
            {
                ResolveCamera();
                if (_cameraTransform == null)
                {
                    if (!_reportedMissingCamera)
                    {
                        _reportedMissingCamera = true;
                        Debug.LogWarning($"{name}: World UI가 바라볼 카메라를 찾지 못했습니다.", this);
                    }

                    return;
                }
            }

            Vector3 forward = flipForward ? -_cameraTransform.forward : _cameraTransform.forward;
            Vector3 up = _cameraTransform.up;

            if (lockVerticalTilt)
            {
                forward.y = 0f;
                up = Vector3.up;
                if (forward.sqrMagnitude < 0.0001f) return;
            }

            Quaternion desiredRotation = Quaternion.LookRotation(forward, up);
            if (Quaternion.Angle(transform.rotation, desiredRotation) > 0.01f)
                transform.rotation = desiredRotation;
        }

        private void ResolveCamera()
        {
            Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;
            _cameraTransform = cameraToUse != null ? cameraToUse.transform : null;

            if (_cameraTransform != null)
                _reportedMissingCamera = false;
        }

    }
}
