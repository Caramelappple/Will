using System;
using UnityEngine;

namespace _Scripts.LDY.AI
{
    /// <summary>
    /// LDY_EnemyBrain의 결정을 기존 이동/공격 시스템으로 실행하고, 실제로 실행됐는지 확인해서 돌려준다.
    /// 실행 자체는 전부 기존 API에 위임한다 — 여기서 판정을 다시 하지 않는다.
    /// </summary>
    public class LDY_ActionExecutor
    {
        private readonly LDY_MoveSystem _moveSystem;
        private readonly LDY_AttackSystem _attackSystem;
        private readonly LDY_ActionPointManager _actionPoints;

        public LDY_ActionExecutor(
            LDY_MoveSystem moveSystem,
            LDY_AttackSystem attackSystem,
            LDY_ActionPointManager actionPoints)
        {
            _moveSystem = moveSystem != null ? moveSystem : throw new ArgumentNullException(nameof(moveSystem));
            _attackSystem = attackSystem != null ? attackSystem : throw new ArgumentNullException(nameof(attackSystem));
            _actionPoints = actionPoints;
        }

        public LDY_ActionOutcome Execute(LDY_Animal self, in LDY_EnemyAction action)
        {
            if (self == null) return LDY_ActionOutcome.Rejected;

            switch (action.Kind)
            {
                case LDY_ActionKind.Attack:
                    return ExecuteAttack(self, action.Target);

                case LDY_ActionKind.Move:
                    return ExecuteMove(self, action.MoveTo);

                default:
                    return LDY_ActionOutcome.Waited;
            }
        }

        // 이동은 board.Move가 animal.pos를 그 자리에서 갱신하므로 좌표 변화만 보면 확실하다.
        private LDY_ActionOutcome ExecuteMove(LDY_Animal self, Vector3Int target)
        {
            Vector3Int before = self.pos;
            _moveSystem.MoveTo(self, target);

            bool moved = self.pos.x != before.x || self.pos.z != before.z;
            return moved ? LDY_ActionOutcome.Executed : LDY_ActionOutcome.Rejected;
        }

        // 공격은 피해가 연출 코루틴 중간에 들어가서 호출 직후에는 보드에 아무 변화가 없다.
        // 동기적으로 확인할 수 있는 신호는 행동력 소모뿐이다 — Attack은 검증을 통과해야 TryConsume에 도달한다.
        private LDY_ActionOutcome ExecuteAttack(LDY_Animal self, LDY_Animal target)
        {
            if (target == null) return LDY_ActionOutcome.Rejected;

            if (_actionPoints == null)
            {
                _attackSystem.Attack(self, target);
                return LDY_ActionOutcome.Unverified;
            }

            int before = _actionPoints.Current;
            _attackSystem.Attack(self, target);

            return _actionPoints.Current < before
                ? LDY_ActionOutcome.Executed
                : LDY_ActionOutcome.Rejected;
        }
    }
}
