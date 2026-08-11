using _Scripts.LDY;
using _Scripts.LSO.HealthSystem.Data;
using UnityEngine;

namespace _Scripts.LSO.UI
{
    /// <summary>
    /// 기물 머리 위에 스탯 표시를 하나 만들어 붙이고, 값이 바뀔 때마다 갱신한다.
    ///
    /// 체력은 Health가 바뀌는 순간을 정확히 알려주지만,
    /// 공격력은 GetAtk()가 매번 계산하는 값이라 바뀌는 순간이 없다.
    /// 그래서 그 계산이 의존하는 보드 상태가 바뀔 때(OnBoardChanged) 다시 그린다.
    /// </summary>
    [RequireComponent(typeof(LDY_Animal))]
    public class LSO_StatBillBoardSpawner : MonoBehaviour
    {
        [SerializeField] public GameObject prefab;

        [Tooltip("기물 기준 표시 위치.")]
        [SerializeField] private Vector3 localPosition = new(0f, 0.225f, -0.4f);

        [Tooltip("붙인 뒤 강제할 크기. 기물 모델의 스케일이 1이 아니면 글자가 찌그러진다.")]
        [SerializeField] private Vector3 localScale = new(1.6f, 1.6f, 1.6f);

        private LDY_Animal _animal;
        private GameObject _textObject;
        private LSO_StatBillboard _billboard;
        private LDY_BoardManager _board;

        private void Awake()
        {
            _animal = GetComponent<LDY_Animal>();

            if (prefab == null)
            {
                Debug.LogError($"{name}: 스탯 표시 프리팹이 비어 있습니다.", this);
                return;
            }

            _textObject = Instantiate(prefab, transform);
            _textObject.transform.localPosition = localPosition;
            _textObject.transform.localScale = localScale;

            _billboard = _textObject.GetComponent<LSO_StatBillboard>();

            if (_billboard == null)
                Debug.LogError($"{name}: 프리팹에 LSO_StatBillboard가 없습니다.", this);
        }

        private void OnEnable()
        {
            if (_billboard == null) return;

            if (_animal.health != null)
            {
                _animal.health.OnDamage += HandleDamaged;
                _animal.health.OnRecover += HandleRecovered;
            }

            // 소환으로 나중에 만들어진 기물은 보드가 이미 있으므로 여기서 붙는다.
            TrySubscribeBoard();
            Refresh();
        }

        // 씬에 처음부터 있던 기물은 OnEnable이 보드의 Awake보다 먼저 돌 수 있다.
        // Start는 모든 Awake 이후라 그때는 반드시 찾을 수 있다.
        private void Start()
        {
            TrySubscribeBoard();
            Refresh();
        }

        private void OnDisable()
        {
            // 메서드 이름으로 빼는 게 중요하다.
            // 람다는 작성한 위치마다 별개로 컴파일돼서 -=로 떼어지지 않고,
            // 기물이 죽어도 보드에 구독이 남아 다음 신호에서 터진다.
            if (_animal != null && _animal.health != null)
            {
                _animal.health.OnDamage -= HandleDamaged;
                _animal.health.OnRecover -= HandleRecovered;
            }

            if (_board != null)
            {
                _board.OnBoardChanged -= Refresh;
                _board = null;
            }
        }

        /// <summary>아직 안 붙었으면 보드를 찾아 구독한다. 여러 번 불러도 한 번만 붙는다.</summary>
        private void TrySubscribeBoard()
        {
            if (_board != null) return;
            if (!GameManager.HasInstance) return;

            LDY_BoardManager board = GameManager.Instance.Board;
            if (board == null) return;

            _board = board;
            _board.OnBoardChanged += Refresh;
        }

        private void HandleDamaged(DamageResultData data) => Refresh();

        private void HandleRecovered(RecoverResultData data) => Refresh();

        /// <summary>지금 값을 다시 그린다.</summary>
        public void Refresh()
        {
            if (_billboard == null || _animal == null) return;

            _billboard.SetAtkText(_animal.GetAtk());

            if (_animal.health != null)
                _billboard.SetHpText(_animal.health.Value);
        }
    }
}
