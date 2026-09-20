using _Scripts.LDY;
using _Scripts.LSO.Manager;
using UnityEngine;
using UnityEngine.Events;

namespace _Scripts.LSO.Penalty
{
    /// <summary>
    /// 기물을 하나도 안 놓고 턴을 넘기면 양초를 깎는다.
    ///
    /// ── 왜 벌점이 필요한가 ───────────────────────────────────
    /// 아무것도 놓지 않는 것이 가장 안전한 수가 되면 안 된다. 판을 비워두고
    /// 턴만 넘기면 적은 때릴 것이 없어 아무 일도 일어나지 않는다.
    /// 그래서 비워둔 것에 값을 매긴다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// **세는 시점은 내 턴이 끝나는 순간 하나다.** 매 프레임 판을 보면 기물이
    /// 죽어 잠깐 비는 순간까지 벌점이 되고, 턴 시작에 보면 아직 놓을 기회가 없다.
    ///
    /// 깎는 곳은 DLJ_PlayerHealth 하나다. 여기서 양초를 따로 세지 않는다.
    ///
    /// 씬 배선: 아무 관리 오브젝트에나 하나만 붙일 것. 둘 붙이면 두 번 깎인다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LSO_EmptyBoardPenalty : MonoBehaviour
    {
        [Header("벌점")]
        [Tooltip("기물 없이 턴을 넘겼을 때 깎을 양.\n" +
                 "\n" +
                 "양초는 100씩 셋(합계 300)이고 기물 사망점수가 2~7 이므로,\n" +
                 "그 사이 어딘가로 잡아야 '차라리 한 기 잃는 편이 낫다'가 되지 않는다.")]
        [SerializeField, Min(0)] private int damage = 50;

        [Header("반응")]
        [Tooltip("벌점이 나갔을 때. 화면에 알리는 쪽이 듣는다.")]
        [SerializeField] private UnityEvent onPenalty;

        [Header("진단")]
        [Tooltip("켜면 턴이 끝날 때마다 판에 내 기물이 몇 기 있었는지 찍는다.")]
        [SerializeField] private bool logSteps;

        private LDY_TurnManager _turnManager;

        private void OnEnable()
        {
            // 전투 씬마다 턴 매니저가 새로 생긴다. 직접 참조로 물고 있으면 씬을 넘길 때 끊긴다.
            GameManager.Instance.TurnManagerChanged += Bind;

            Bind(GameManager.Instance.TurnManager);
        }

        private void OnDisable()
        {
            if (GameManager.HasInstance)
                GameManager.Instance.TurnManagerChanged -= Bind;

            Bind(null);
        }

        private void Bind(LDY_TurnManager turnManager)
        {
            if (_turnManager == turnManager) return;

            if (_turnManager != null)
                _turnManager.OnTurnChanged -= HandleTurnChanged;

            _turnManager = turnManager;

            if (_turnManager != null)
                _turnManager.OnTurnChanged += HandleTurnChanged;
        }

        /// <summary>
        /// 내 턴이 끝났다. 판을 보고 비어 있으면 깎는다.
        ///
        /// 내 턴이 **시작**될 때가 아니라 끝날 때 보는 이유는, 시작 시점에는
        /// 아직 놓을 기회가 없어 언제나 비어 있기 때문이다.
        /// </summary>
        private void HandleTurnChanged(LDY_Team team)
        {
            if (team == LDY_Team.Player) return;

            // 판이 이미 끝났으면 벌주지 않는다. 마지막 적을 잡고 클리어된 순간에도
            // 턴은 한 번 더 넘어가는데, 이긴 판에서 양초가 깎이면 이유를 알 수 없다.
            if (KTH_GameEndManager.IsBattleOver) return;

            if (!TryCountMyPieces(out int count)) return;

            if (logSteps)
                Debug.Log($"[{name}] 내 턴 종료 — 판에 내 기물 {count}기", this);

            if (count > 0) return;

            Penalize();
        }

        /// <summary>
        /// 판에 있는 내 기물 수. 판을 못 찾으면 거짓 — 그때는 벌주지 않는다.
        ///
        /// 못 찾았다고 0으로 치면 배선을 빠뜨렸을 때 매 턴 양초가 깎인다.
        /// 원인은 안 보이고 결과만 나오는, 가장 찾기 어려운 모양이다.
        /// </summary>
        private bool TryCountMyPieces(out int count)
        {
            count = 0;

            LDY_BoardManager board = GameManager.HasInstance ? GameManager.Instance.Board : null;

            if (board == null)
            {
                Debug.LogWarning(
                    $"{name}: LDY_BoardManager를 찾지 못해 판을 셀 수 없습니다. " +
                    "기물 없이 턴을 넘겨도 벌점이 나가지 않습니다.", this);

                return false;
            }

            count = board.GetAllByTeam(LDY_Team.Player).Count;

            return true;
        }

        private void Penalize()
        {
            DLJ_PlayerHealth health = DLJ_PlayerHealth.Instance;

            if (health == null)
            {
                Debug.LogWarning(
                    $"{name}: DLJ_PlayerHealth가 없어 벌점을 주지 못했습니다.", this);

                return;
            }

            if (damage <= 0) return;

            Debug.Log(
                $"[{name}] 기물 없이 턴을 넘겼습니다 → 양초 {damage} 깎습니다.", this);

            health.TakeDamage(damage);

            onPenalty?.Invoke();
        }
    }
}
