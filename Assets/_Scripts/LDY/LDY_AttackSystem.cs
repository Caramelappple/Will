using System.Collections;
using System.Collections.Generic;
using _Scripts.LSO;
using _Scripts.LSO.DeathSystem;
using _Scripts.LSO.HealthSystem;
using UnityEngine;

namespace _Scripts.LDY
{
    // 씬 배선: BoardManager를 연결할 것.
    public class LDY_AttackSystem : MonoBehaviour
    {
        [SerializeField] private LDY_BoardManager board;
        [SerializeField] private LDY_ActionPointManager actionPoints;
        [SerializeField] private float attackDuration = 0.3f;
        [SerializeField] private float lungeRatio = 0.4f;

        // 공격 연출(코루틴)이 하나라도 재생 중이면 true. 턴 전환이 이 애니메이션 도중에 끼어들지 않도록 막는 용도.
        public bool IsBusy => _activeCount > 0;
        private int _activeCount;

        public List<Vector3Int> GetAttackableTiles(LDY_Animal attacker)
        {
            if (attacker == null) return new List<Vector3Int>();
            if (actionPoints != null && !actionPoints.HasActionPoints) return new List<Vector3Int>();

            var strategy = LDY_AttackRangeFactory.Create(attacker.RangeType);
            return strategy != null
                ? strategy.GetAttackableTiles(attacker.pos, board)
                : new List<Vector3Int>();
        }

        public List<LDY_Animal> GetAttackTargets(LDY_Animal attacker)
        {
            var targets = new List<LDY_Animal>();
            if (attacker == null) return targets;

            foreach (var tile in GetAttackableTiles(attacker))
            {
                var occupant = board.Get(tile);
                if (occupant != null && occupant.team != attacker.team)
                    targets.Add(occupant);
            }
            return targets;
        }

        public void Attack(LDY_Animal attacker, LDY_Animal target)
        {
            if (attacker == null || target == null) return;
            if (!GetAttackTargets(attacker).Contains(target)) return;
            if (actionPoints != null && !actionPoints.TryConsume()) return;

            StartCoroutine(AttackRoutine(attacker, target));
        }

        // 공격 대상 쪽으로 살짝 달려들었다가 원위치로 돌아오는 연출. 데미지는 달려든 시점(절반 지점)에 적용한다.
        private IEnumerator AttackRoutine(LDY_Animal attacker, LDY_Animal target)
        {
            _activeCount++;
            try
            {
                Transform t = attacker.modelTransform;
                Vector3 startPos = t.position;
                Vector3 lungePos = Vector3.Lerp(startPos, target.modelTransform.position, lungeRatio);
                float half = attackDuration * 0.5f;

                yield return LerpPosition(t, startPos, lungePos, half);

                // 연출이 재생되는 동안 다른 공격이 같은 대상을 먼저 처치했을 수 있으므로 다시 확인한다.
                if (target != null)
                {
                    if (target.health != null && attacker.health != null)
                    {

                        // 피해량은 때리는 쪽의 공격력이다. 출처를 함께 실어 보내면
                        // "근접 공격을 받으면 반격" 같은 특성이 판단할 수 있다.
                        DamageData data = DamageData.Create(
                            attacker.health,
                            attacker.GetAtk(),
                            ToDamageSource(attacker.RangeType));

                        target.health.GetDamage(data);
                        if (target.health.IsDestroyed)
                            HandleDeath(target, attacker);
                    }
                    else
                    {
                        Debug.Log("체력이 존재하지 않습니다");
                    }
                }

                if (attacker != null)
                    yield return LerpPosition(t, t.position, startPos, half);
            }
            finally
            {
                _activeCount--;
            }
        }

        private static IEnumerator LerpPosition(Transform t, Vector3 from, Vector3 to, float duration)
        {
            if (t == null)
                yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                // 유언으로 기물 사망시 예외처리
                if (t == null)
                    yield break;

                elapsed += Time.deltaTime;
                t.position = Vector3.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            if (t != null)
                t.position = to;
        }

        public void HandleDeath(LDY_Animal target)
        {
            HandleDeath(target, null);
        }

        /// <summary>
        /// 사망 처리는 LDY_DeathHandler에 위임한다.
        /// 씬에 핸들러가 없으면 예전 방식으로 직접 처리해서 기존 씬이 깨지지 않게 한다.
        /// </summary>
        public void HandleDeath(LDY_Animal target, LDY_Animal killer)
        {
            if (target == null) return;

            LSO_IDeathService deathService = GameManager.HasInstance
                ? GameManager.Instance.DeathService
                : null;

            if (deathService != null)
            {
                deathService.Kill(target, killer);
                return;
            }

            board.Remove(target);
            RaiseEnemyDead(target);

            var will = target.GetComponent<DLJ_IWillActivation>();
            will?.WillActivate();

            if (will == null || !will.ShouldDeferDestruction)
                Destroy(target.gameObject);
        }

        private static LSO_DamageSource ToDamageSource(LDY_RangeType rangeType)
        {
            switch (rangeType)
            {
                case LDY_RangeType.Melee: return LSO_DamageSource.Melee;
                case LDY_RangeType.Ranged: return LSO_DamageSource.Ranged;
                case LDY_RangeType.Jump: return LSO_DamageSource.Jump;
                default: return LSO_DamageSource.Unknown;
            }
        }

        // 적이 죽었을 때만 알린다. 구독자가 없어도 무해하며, 매니저가 없으면 조용히 넘어간다.
        private static void RaiseEnemyDead(LDY_Animal target)
        {
            if (target == null || target.team != LDY_Team.Enemy) return;
            if (!GameManager.HasInstance) return;

            GameEventDispatcher dispatcher = GameManager.Instance.EventDispatcher;
            if (dispatcher == null) return;

            dispatcher.RaiseEnemyDead(target);
        }
    }
}
