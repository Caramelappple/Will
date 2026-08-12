using _Scripts.LDY;
using _Scripts.LSO.HealthSystem.Data;
using _Scripts.LSO.Manager;
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

            SubscribeManager();
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

            if (GameManager.HasInstance)
                GameManager.Instance.BoardChanged -= BindBoard;

            BindBoard(null);
        }

        // 보드는 씬마다 새로 생기고, 이 기물이 켜지는 시점에 아직 없을 수도 있다.
        // 그래서 "지금 있는 보드"와 "앞으로 바뀔 보드"를 한 경로로 받는다.
        private void SubscribeManager()
        {
            GameManager manager = GameManager.Instance;
            if (manager == null) return;

            manager.BoardChanged += BindBoard;
            BindBoard(manager.Board);
        }

        private void BindBoard(LDY_BoardManager board)
        {
            if (_board == board) return;

            if (_board != null)
                _board.OnBoardChanged -= Refresh;

            _board = board;

            if (_board == null) return;

            _board.OnBoardChanged += Refresh;
            Refresh();
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
