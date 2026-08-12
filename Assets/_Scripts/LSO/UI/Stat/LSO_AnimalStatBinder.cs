using System.Collections;
using _Scripts.LDY;
using _Scripts.LSO.HealthSystem.Data;
using UnityEngine;

namespace _Scripts.LSO.UI.Stat
{
    /// <summary>
    /// LDY_Animal의 스탯을 LSO_StatLabel로 옮겨주는 다리.
    ///
    /// 기물은 라벨이 있는지 모르고, 라벨은 기물이 있는지 모른다.
    /// 둘을 아는 건 이 컴포넌트뿐이라 어느 쪽을 갈아치워도 나머지는 그대로다.
    ///
    /// 갱신 시점이 스탯마다 다른 게 이 클래스의 존재 이유다.
    ///   체력  Health가 바뀌는 순간에 정확히 알려준다.
    ///   공격력 GetAtk()는 특성이 매번 계산하는 값이라 바뀌는 순간이 없다.
    ///          대신 그 특성들이 결국 보는 건 보드 상태이므로 OnBoardChanged를 신호로 쓴다.
    ///
    /// 그래도 새는 경로가 남는다(Health.Value 직접 대입, baseAtk 직접 증감 등).
    /// 그래서 저주기 폴링을 안전망으로 둔다. 값이 그대로면 라벨이 알아서 무시하므로 비용이 거의 없다.
    /// </summary>
    public class LSO_AnimalStatBinder : MonoBehaviour
    {
        [Tooltip("비우면 자신과 부모에서 찾는다.")]
        [SerializeField] private LDY_Animal animal;

        [Tooltip("비우면 자신과 자식에서 찾는다.")]
        [SerializeField] private LSO_StatLabel label;

        [Tooltip("놓친 변화를 뒤늦게라도 맞추는 안전망. 0이면 끈다.\n" +
                 "값이 같으면 라벨이 그리지 않으므로 짧게 잡아도 부담이 적다.")]
        [SerializeField, Min(0f)] private float pollInterval = 0.2f;

        private LDY_BoardManager _board;

        private void Awake()
        {
            if (animal == null)
                animal = GetComponentInParent<LDY_Animal>();

            if (label == null)
                label = GetComponentInChildren<LSO_StatLabel>(true);

            if (animal == null || label == null)
                Debug.LogWarning($"{name}: 기물 또는 라벨을 찾지 못해 스탯이 표시되지 않습니다.", this);
        }

        private void OnEnable()
        {
            if (animal == null || label == null) return;

            if (animal.health != null)
            {
                animal.health.OnDamage += HandleDamaged;
                animal.health.OnRecover += HandleRecovered;
            }

            // 소환으로 나중에 만들어진 기물은 보드가 이미 있으므로 여기서 붙는다.
            TrySubscribeBoard();
            Refresh();

            if (pollInterval > 0f)
                StartCoroutine(PollRoutine());
        }

        // 씬에 처음부터 있던 기물은 OnEnable이 보드의 Awake보다 먼저 돌 수 있다.
        // Start는 모든 Awake 이후라 그때는 반드시 찾을 수 있다.
        //
        // Start만 쓰면 껐다 켠 오브젝트가 다시 못 붙는다(Start는 한 번만 돈다).
        // 그래서 양쪽에서 시도하고, TrySubscribeBoard가 중복을 걸러낸다.
        private void Start()
        {
            TrySubscribeBoard();
            Refresh();
        }

        private void OnDisable()
        {
            if (animal != null && animal.health != null)
            {
                animal.health.OnDamage -= HandleDamaged;
                animal.health.OnRecover -= HandleRecovered;
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

        private IEnumerator PollRoutine()
        {
            var wait = new WaitForSeconds(pollInterval);

            while (true)
            {
                yield return wait;

                Refresh();
            }
        }

        private void HandleDamaged(DamageResultData data) => Refresh();

        private void HandleRecovered(RecoverResultData data) => Refresh();

        /// <summary>지금 값을 읽어 라벨에 넘긴다. 달라진 게 없으면 라벨이 무시한다.</summary>
        public void Refresh()
        {
            if (animal == null || label == null) return;

            int atk = animal.GetAtk();

            // 강화/약화 판단 기준은 동물 데이터의 원본 공격력이다.
            // baseAtk는 버프가 이미 누적된 값이라 기준으로 쓰면 변화가 드러나지 않는다.
            int originalAtk = animal.data != null ? animal.data.damage : atk;

            int hp = animal.health != null ? animal.health.Value : 0;
            int maxHp = animal.health != null ? animal.health.MaxValue : 0;

            label.SetStats(atk, originalAtk, hp, maxHp);
        }
    }
}
