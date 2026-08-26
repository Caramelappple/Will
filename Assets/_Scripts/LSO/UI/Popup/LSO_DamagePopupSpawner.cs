using UnityEngine;
using UnityEngine.Pool;

namespace _Scripts.LSO.UI.Popup
{
    /// <summary>
    /// 팝업을 풀에서 꺼내 화면 좌표로 옮겨 재생시킨다.
    /// 씬의 UI 캔버스에 하나 두면 된다.
    ///
    /// 씬 배선: Container(팝업이 담길 RectTransform)와 Prefab을 연결할 것.
    ///          World Camera를 비우면 Camera.main을 쓴다.
    /// </summary>
    public class LSO_DamagePopupSpawner : MonoBehaviour, LSO_IDamagePopupSpawner
    {
        /// <summary>
        /// 런타임에 생성되는 기물은 인스펙터로 참조를 걸 수 없어서 이렇게 찾아 쓴다.
        /// 없으면 그냥 팝업이 안 뜰 뿐, 게임 로직에는 영향이 없다.
        /// </summary>
        public static LSO_IDamagePopupSpawner Current { get; private set; }

        [SerializeField] private LSO_DamagePopup prefab;

        [Tooltip("팝업이 붙을 부모. 비워두면 자기 자신을 쓴다.")]
        [SerializeField] private RectTransform container;

        [Tooltip("월드 좌표를 화면 좌표로 바꿀 카메라. 비우면 Camera.main.")]
        [SerializeField] private UnityEngine.Camera worldCamera;

        [Header("풀")]
        [SerializeField, Min(1)] private int defaultCapacity = 16;

        [Tooltip("이 수를 넘긴 팝업은 재사용하지 않고 버린다.")]
        [SerializeField, Min(1)] private int maxSize = 64;

        private ObjectPool<LSO_DamagePopup> _pool;
        private RectTransform _containerRect;
        private Canvas _canvas;

        private void Awake()
        {
            _containerRect = container != null ? container : (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>();

            if (prefab == null)
                Debug.LogWarning($"{name}: 팝업 프리팹이 비어 있습니다.", this);

            _pool = new ObjectPool<LSO_DamagePopup>(
                createFunc: CreatePopup,
                actionOnGet: p => p.gameObject.SetActive(true),
                actionOnRelease: p => p.gameObject.SetActive(false),
                actionOnDestroy: p => { if (p != null) Destroy(p.gameObject); },
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize);
        }

        private void OnEnable()
        {
            Current ??= this;
        }

        private void OnDisable()
        {
            if (ReferenceEquals(Current, this))
                Current = null;
        }

        private void OnDestroy()
        {
            _pool?.Dispose();
        }

        private LSO_DamagePopup CreatePopup()
        {
            LSO_DamagePopup popup = Instantiate(prefab, _containerRect);
            popup.gameObject.SetActive(false);
            return popup;
        }

        public void Spawn(Vector3 worldPosition, string text, Color color)
        {
            if (prefab == null || string.IsNullOrEmpty(text)) return;

            if (!TryGetAnchoredPosition(worldPosition, out Vector2 anchored))
                return;

            LSO_DamagePopup popup = _pool.Get();
            popup.transform.SetAsLastSibling();
            popup.Play(anchored, text, color, Release);
        }

        private void Release(LSO_DamagePopup popup)
        {
            // 씬 전환 등으로 이미 파괴된 뒤에 콜백이 오는 경우를 막는다.
            if (popup == null || _pool == null) return;

            _pool.Release(popup);
        }

        /// <summary>월드 좌표 → 컨테이너 기준 앵커 좌표. 카메라 뒤면 false.</summary>
        private bool TryGetAnchoredPosition(Vector3 worldPosition, out Vector2 anchored)
        {
            anchored = Vector2.zero;

            UnityEngine.Camera cam = worldCamera != null ? worldCamera : UnityEngine.Camera.main;
            if (cam == null) return false;

            Vector3 screenPoint = cam.WorldToScreenPoint(worldPosition);
            if (screenPoint.z < 0f) return false;

            // Overlay 캔버스는 UI 카메라가 없어야 좌표가 맞는다.
            UnityEngine.Camera uiCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _containerRect, screenPoint, uiCamera, out anchored);
        }
    }
}
