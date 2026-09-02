using UnityEngine;

namespace _Scripts.DLJ.UI.WorldUI
{
    /// <summary>
    /// 기물의 크기와 무관하게 World UI의 위치와 월드 크기를 일정하게 유지한다.
    /// </summary>
    [DefaultExecutionOrder(900)]
    [DisallowMultipleComponent]
    public sealed class DLJ_WorldUIAnchor : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [Tooltip("렌더러가 들어 있는 자식. 대상이 비활성화되면 이 오브젝트만 숨긴다.")]
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private Vector3 worldOffset = new(0f, 1.5f, 0f);
        [SerializeField] private bool followTarget = true;
        [Tooltip("비균일 Scale의 왜곡을 피하려고 실행 시 기물 Transform에서 분리한다.")]
        [SerializeField] private bool detachFromTargetAtRuntime = true;
        [SerializeField] private bool destroyWhenTargetDestroyed = true;
        [SerializeField] private bool compensateParentScale = true;
        [SerializeField, Min(0.0001f)] private float targetWorldScale = 1f;

        private bool _hadTarget;

        private void OnEnable()
        {
            ResolveTarget();
            _hadTarget = target != null;

            ApplyTransform();
        }

        private void Start()
        {
            // 모든 Binder의 Awake/OnEnable 탐색이 끝난 뒤 분리해야 참조 자동 탐색이 안전하다.
            if (detachFromTargetAtRuntime && transform.parent != null)
                transform.SetParent(null, true);

            ApplyTransform();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                if (_hadTarget && destroyWhenTargetDestroyed)
                    Destroy(gameObject);
                return;
            }

            bool targetActive = target.gameObject.activeInHierarchy;
            if (visualRoot != null && visualRoot.activeSelf != targetActive)
                visualRoot.SetActive(targetActive);

            if (!targetActive) return;

            ApplyTransform();
        }

        public void Configure(
            Transform anchorTarget,
            GameObject visuals,
            Vector3 offset,
            float worldScale)
        {
            target = anchorTarget;
            visualRoot = visuals;
            _hadTarget = target != null;
            worldOffset = offset;
            targetWorldScale = Mathf.Max(0.0001f, worldScale);
            ApplyTransform();
        }

        private void ResolveTarget()
        {
            if (target == null)
                target = transform.parent;
        }

        private void ApplyTransform()
        {
            ResolveTarget();

            if (followTarget && target != null)
            {
                Vector3 desiredPosition = target.position + worldOffset;
                if ((transform.position - desiredPosition).sqrMagnitude > 0.00000001f)
                    transform.position = desiredPosition;
            }

            if (!compensateParentScale) return;

            Transform actualParent = transform.parent;
            if (actualParent == null)
            {
                SetLocalScaleIfChanged(Vector3.one * targetWorldScale);
                return;
            }

            Vector3 parentScale = actualParent.lossyScale;
            SetLocalScaleIfChanged(new Vector3(
                SafeDivide(targetWorldScale, parentScale.x),
                SafeDivide(targetWorldScale, parentScale.y),
                SafeDivide(targetWorldScale, parentScale.z)));
        }

        private static float SafeDivide(float value, float divisor)
        {
            return Mathf.Abs(divisor) > 0.0001f ? value / divisor : value;
        }

        private void SetLocalScaleIfChanged(Vector3 desiredScale)
        {
            if ((transform.localScale - desiredScale).sqrMagnitude > 0.00000001f)
                transform.localScale = desiredScale;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (targetWorldScale < 0.0001f)
                targetWorldScale = 0.0001f;
        }
#endif
    }
}
