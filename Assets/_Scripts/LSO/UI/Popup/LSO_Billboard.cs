using UnityEngine;

namespace _Scripts.LSO.UI.Popup
{
    /// <summary>
    /// 항상 카메라를 향하게 한다. 기물 머리 위에 띄우는 월드 스페이스 UI에 붙인다.
    ///
    /// LookAt이 아니라 카메라의 forward를 그대로 복사한다.
    /// LookAt은 화면 가장자리로 갈수록 대상이 기울어 보인다.
    /// </summary>
    public class LSO_Billboard : MonoBehaviour
    {
        [Tooltip("비우면 Camera.main을 쓴다.")]
        [SerializeField] private UnityEngine.Camera targetCamera;

        [Tooltip("켜면 좌우로만 돈다. 위아래로 눕는 걸 막고 싶을 때.")]
        [SerializeField] private bool lockVerticalTilt;

        private Transform _cachedCamera;

        private Transform CameraTransform
        {
            get
            {
                if (_cachedCamera != null) return _cachedCamera;

                UnityEngine.Camera cam = targetCamera != null ? targetCamera : UnityEngine.Camera.main;
                _cachedCamera = cam != null ? cam.transform : null;

                return _cachedCamera;
            }
        }

        // 카메라 이동이 끝난 뒤에 맞춰야 한 프레임 밀리지 않는다.
        private void LateUpdate()
        {
            Transform cam = CameraTransform;
            if (cam == null) return;

            Vector3 forward = cam.forward;

            if (lockVerticalTilt)
            {
                forward.y = 0f;

                // 카메라가 정확히 수직으로 내려다보면 방향이 사라진다.
                if (forward.sqrMagnitude < 0.0001f) return;
            }

            transform.rotation = Quaternion.LookRotation(forward, lockVerticalTilt ? Vector3.up : cam.up);
        }
    }
}
