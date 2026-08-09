using System.Collections.Generic;

namespace _Scripts.LDY
{
    /// <summary>
    /// 체력이 가장 높은 기물에게 계승한다. 동점이면 z가 큰 쪽(적 진영에 가까운 = 전선에서 먼 쪽),
    /// 그래도 동점이면 열거 순서상 먼저 나온 기물. 같은 보드 상태면 항상 같은 결과가 나온다.
    /// </summary>
    public class LDY_StrongestSuccessionPicker : LDY_ISuccessionTargetPicker
    {
        public LDY_Animal Pick(LDY_Animal dying, IReadOnlyList<LDY_Animal> candidates)
        {
            if (candidates == null) return null;

            LDY_Animal best = null;
            int bestHealth = 0;
            int bestZ = 0;

            foreach (LDY_Animal candidate in candidates)
            {
                if (candidate == null || candidate.health == null) continue;

                int health = candidate.health.GetValue();
                int z = candidate.pos.z;

                // 부등호가 전부 strict라 완전 동점이면 먼저 나온 후보가 남는다.
                bool better = best == null
                              || health > bestHealth
                              || (health == bestHealth && z > bestZ);

                if (!better) continue;

                best = candidate;
                bestHealth = health;
                bestZ = z;
            }

            return best;
        }
    }
}
