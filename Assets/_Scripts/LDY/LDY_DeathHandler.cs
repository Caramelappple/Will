using System.Collections.Generic;
using _Scripts.LSO;
using _Scripts.LSO.Ability;
using _Scripts.LSO.DeathSystem;
using _Scripts.LSO.Manager;
using _Scripts.LSO.Will;
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
            NotifyKillerAbilities(victim, killer);
            RaiseAnimalDead(victim);

            LSO_IWill will = DLJ_WillRuntime.Invoke(victim, targetBoard);

            if (!(will is DLJ_IDeferredDestruction deferred) ||
                !deferred.ShouldDeferDestruction)
            {
                // 즉시 파괴 대신 디졸브를 재생한다. 파괴는 destroyOnComplete가 연출 뒤에 한다.
                LDY_DissolveEffect.PlayOn(victim.gameObject);
                return;
            }

            // 파괴가 미뤄졌다. 계승처럼 뒤늦게 발동하는 유언은 주체를 알려주지 않으므로 여기서 적어둔다.
            LDY_DeferredDeaths.Record(victim);
        }

        /// <summary>죽는 본인의 특성에게 먼저 알린다. 파괴 전이라 아직 self를 쓸 수 있다.</summary>
        private static void NotifyOwnDeathAbilities(LDY_Animal victim, LDY_Animal killer)
        {
            LSO_AbilityNotify.Notify<LSO_IOnDeath>(
                victim.Abilities, a => a.OnDeath(victim, killer));
        }

        /// <summary>
        /// 죽인 쪽의 특성에게 알린다. 포식처럼 "내가 처치했을 때"가 조건인 특성이 여기서 발동한다.
        ///
        /// 유언보다 앞에 두는 이유는, 계승처럼 죽은 기물의 스탯을 다른 기물에게 옮기는 유언이 있어서다.
        /// 뒤에 부르면 이미 옮겨진 뒤의 값을 읽게 된다.
        /// </summary>
        private static void NotifyKillerAbilities(LDY_Animal victim, LDY_Animal killer)
        {
            // 독·유언 등으로 죽으면 죽인 주체가 없다. 자기 자신을 죽인 경우도 처치로 치지 않는다.
            if (killer == null || killer == victim) return;

            LSO_AbilityNotify.Notify<LSO_IOnKill>(
                killer.Abilities, a => a.OnKill(killer, victim));
        }

        // 팀을 가리지 않고 모든 죽음을 알린다. 누가 적인지는 받는 특성이 판단한다.
        // 여기서 걸러내면 "아군이 죽으면 발동" 같은 특성을 만들 방법이 없어진다.
        private static void RaiseAnimalDead(LDY_Animal victim)
        {
            if (!GameManager.HasInstance) return;

            GameEventDispatcher dispatcher = GameManager.Instance.EventDispatcher;
            dispatcher?.RaiseAnimalDead(victim);
        }
    }
}
