using System.Collections.Generic;

namespace _Scripts.LDY
{
    public static class LDY_AttackRangeFactory
    {
        private static readonly Dictionary<LDY_RangeType, LDY_IAttackRange> Strategies =
            new Dictionary<LDY_RangeType, LDY_IAttackRange>
            {
                { LDY_RangeType.Melee, new LDY_MeleeRange() },
                { LDY_RangeType.Ranged, new LDY_RangedRange() },
                { LDY_RangeType.Jump, new LDY_JumpRange() },
            };

        public static LDY_IAttackRange Get(LDY_RangeType type)
        {
            return Strategies.TryGetValue(type, out var strategy) ? strategy : null;
        }
    }
}
