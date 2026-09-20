using _Scripts.LDY;
using UnityEngine;

namespace _Scripts.LSO.Boss.CrowKing
{
    /// <summary>
    /// 사냥감 머리 위에 해골을 띄운다. 까마귀왕에 붙인다.
    ///
    /// 표식을 사냥감의 자식으로 만드는 게 핵심이다.
    ///   - 사냥감이 움직이면 따라간다
    ///   - 사냥감이 죽으면 같이 파괴된다 (정리 코드가 필요 없다)
    ///
    /// 표시만 하고 아무것도 판단하지 않는다. 누가 사냥감인지는 LSO_PreyMarking이 정한다.
    /// </summary>
    [RequireComponent(typeof(LSO_PreyTracker))]
    public class LSO_PreyMarkView : MonoBehaviour
    {
        [Tooltip("사냥감 위에 띄울 해골. 카메라를 보게 하려면 프리팹에 LSO_Billboard를 붙일 것.")]
        [SerializeField] private GameObject markPrefab;

        [Tooltip("사냥감 기준 위치. 기물 모델 높이에 맞춰 조절한다.")]
        [SerializeField] private Vector3 localOffset = new(0f, 0.6f, 0f);

        [Tooltip("붙인 뒤 강제할 크기. 사냥감 모델의 스케일이 1이 아니면 표식이 찌그러지므로 여기서 바로잡는다.")]
        [SerializeField] private Vector3 localScale = Vector3.one;

        private LSO_PreyTracker _tracker;
        private GameObject _mark;

        private void Awake()
        {
            _tracker = GetComponent<LSO_PreyTracker>();

            if (markPrefab == null)
                Debug.LogError($"{name}: 해골 프리팹이 비어 있어 사냥감 표시가 뜨지 않습니다.", this);
        }

        private void OnEnable()
        {
            _tracker.PreyChanged += HandlePreyChanged;

            // 이 컴포넌트가 켜지기 전에 이미 사냥감이 정해졌을 수 있다.
            HandlePreyChanged(_tracker.Prey);
        }

        private void OnDisable()
        {
            _tracker.PreyChanged -= HandlePreyChanged;

            // 까마귀왕이 죽어도 여기가 불린다.
            // 표식은 사냥감의 자식이라 그냥 두면 보스가 사라진 뒤에도 남는다.
            Clear();
        }

        private void HandlePreyChanged(LDY_Animal prey)
        {
            Clear();

            if (prey == null || markPrefab == null) return;

            // 모델 트랜스폼에 붙이면 공격 연출로 달려들 때도 표식이 따라간다.
            Transform anchor = prey.modelTransform != null ? prey.modelTransform : prey.transform;

            _mark = Instantiate(markPrefab, anchor);

            // ── 부모의 스케일을 되돌린다 ──────────────────────────────
            // 기물 모델은 스케일이 1이 아니다. 까마귀는 150이다.
            // 그 밑에 그냥 붙이면 Local Offset 0.6 이 90유닛이 되고 크기도 150배가 된다.
            // 화면 밖으로 날아가 "표식이 안 뜬다"로 보인다.
            //
            // 부모 스케일로 나눠주면 인스펙터에 적은 값이 **월드 기준**이 된다 —
            // 0.6 은 0.6 유닛 위, 1 은 프리팹 원래 크기.
            // ─────────────────────────────────────────────────────────
            Vector3 scale = anchor.lossyScale;

            _mark.transform.localPosition = Unscale(localOffset, scale);
            _mark.transform.localScale = Unscale(localScale, scale);
        }

        /// <summary>
        /// 부모 스케일을 나눠 없앤다.
        ///
        /// 0으로 나누지 않는다. 스케일이 0인 축은 어차피 보이지 않으므로 그대로 둔다 —
        /// 무한대를 넣으면 트랜스폼이 깨져 그 뒤로 아무것도 안 보인다.
        /// </summary>
        private static Vector3 Unscale(Vector3 value, Vector3 scale)
        {
            return new Vector3(
                Mathf.Approximately(scale.x, 0f) ? value.x : value.x / scale.x,
                Mathf.Approximately(scale.y, 0f) ? value.y : value.y / scale.y,
                Mathf.Approximately(scale.z, 0f) ? value.z : value.z / scale.z);
        }

        private void Clear()
        {
            // 사냥감이 죽었으면 자식인 표식도 이미 파괴돼 여기서 null이다.
            if (_mark == null) return;

            Destroy(_mark);
            _mark = null;
        }
    }
}
