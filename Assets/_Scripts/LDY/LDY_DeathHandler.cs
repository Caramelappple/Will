using System.Collections.Generic;
using System.Linq;
using _Scripts.LSO;
using _Scripts.LSO.DeathSystem;
using UnityEngine;

namespace _Scripts.LDY
{
    /// <summary>
    /// 기물의 사망을 한 곳에서 처리한다.
    /// 보드 제거 → 자신의 사망 특성 → 적 사망 이벤트 → 유언 발동 → 오브젝트 파괴 순서로 진행한다.
    /// 씬 배선: BoardManager를 연결할 것. 비워두면 GameManager에 등록된 보드를 쓴다.
    /// </summary>
    public class LDY_DeathHandler : MonoBehaviour, LSO_IDeathService
    {
        [SerializeField] private LDY_BoardManager board;

        // 반격으로 서로를 죽이는 상황에서 같은 기물이 두 번 처리되는 것을 막는다.
        private readonly HashSet<LDY_Animal> _processed = new();

        private void Awake()
        {
            GameManager.Instance?.RegisterDeathService(this);
        }

        private void OnDestroy()
        {
            if (GameManager.HasInstance)
                GameManager.Instance.UnregisterDeathService(this);
        }

        public void Kill(LDY_Animal victim, LDY_Animal killer)
        {
            if (victim == null) return;
            if (!_processed.Add(victim)) return;

            LDY_BoardManager targetBoard = board != null
                ? board
                : (GameManager.HasInstance ? GameManager.Instance.Board : null);

            if (targetBoard != null)
                targetBoard.Remove(victim);
            else
                Debug.LogWarning($"{name}: BoardManager를 찾을 수 없어 격자에서 제거하지 못했습니다.", this);

            NotifyOwnDeathAbilities(victim, killer);
            RaiseEnemyDead(victim);

            var will = victim.GetComponent<DLJ_IWillActivation>();
            will?.WillActivate();

            if (will == null || !will.ShouldDeferDestruction)
                Destroy(victim.gameObject);
        }

        /// <summary>죽는 본인의 특성에게 먼저 알린다. 파괴 전이라 아직 self를 쓸 수 있다.</summary>
        private static void NotifyOwnDeathAbilities(LDY_Animal victim, LDY_Animal killer)
        {
            if (victim.Abilities == null) return;

            // 처리 중 목록이 바뀔 수 있으므로 복사본으로 순회한다.
            foreach (LSO_IOnDeath ability in victim.Abilities.OfType<LSO_IOnDeath>().ToArray())
                ability.OnDeath(victim, killer);
        }

        private static void RaiseEnemyDead(LDY_Animal victim)
        {
            if (victim.team != LDY_Team.Enemy) return;
            if (!GameManager.HasInstance) return;

            GameEventDispatcher dispatcher = GameManager.Instance.EventDispatcher;
            dispatcher?.RaiseEnemyDead(victim);
        }
    }
}
