using System;
using System.Collections.Generic;

namespace _Scripts.LDY
{
    public static class LDY_AttackRangeFactory
    {
        // 다른 팩토리와 규칙을 맞춰 "생성 방법"을 등록한다.
        // 사거리 전략에 상태(사용 횟수, 강화 여부 등)가 생겨도 개체끼리 섞이지 않는다.
        private static readonly Dictionary<LDY_RangeType, Func<LDY_IAttackRange>> Creators =
            new Dictionary<LDY_RangeType, Func<LDY_IAttackRange>>
            {
                { LDY_RangeType.Melee, () => new LDY_MeleeRange() },
                { LDY_RangeType.Ranged, () => new LDY_RangedRange() },
                { LDY_RangeType.Jump, () => new LDY_JumpRange() },
                { LDY_RangeType.None, () => new LDY_NoneRange() },
            };

        public static LDY_IAttackRange Create(LDY_RangeType type)
        {
            return Creators.TryGetValue(type, out Func<LDY_IAttackRange> creator)
                ? creator()
                : null;
        }
    }
}
