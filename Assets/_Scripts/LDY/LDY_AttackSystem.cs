using System.Collections;
using System.Collections.Generic;
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
        [SerializeField] private float willCleanupDelay = 4f;

        // 공격 연출(코루틴)이 하나라도 재생 중이면 true. 턴 전환이 이 애니메이션 도중에 끼어들지 않도록 막는 용도.
        public bool IsBusy => _activeCount > 0;
        private int _activeCount;

        public List<Vector3Int> GetAttackableTiles(LDY_Animal attacker)
        {
            if (attacker == null) return new List<Vector3Int>();
            if (actionPoints != null && !actionPoints.HasActionPoints) return new List<Vector3Int>();

            var strategy = LDY_AttackRangeFactory.Get(attacker.rangeType);
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
                    target.hp -= attacker.GetAtk();
                    if (target.hp <= 0)
                        HandleDeath(target);
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
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                t.position = Vector3.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            t.position = to;
        }

        private void HandleDeath(LDY_Animal target)
        {
            // TODO: 여기서 유언(Will) 발동
            board.Remove(target);
            var will = target.GetComponent<DLJ_IWillActivation>();
            will?.WillActivate();
            Destroy(target.gameObject, will == null ? 0f : willCleanupDelay);
        }
    }
}
