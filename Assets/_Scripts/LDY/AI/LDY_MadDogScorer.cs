using System;
using UnityEngine;

namespace _Scripts.LDY.AI
{
    /// <summary>
    /// 광견 전용. 매 턴 체력이 깎이고 공격할 때만 회복하므로, 체력이 적을수록 공격을 더 강하게 밀어붙인다.
    /// 잃은 체력 비율에 비례해 attackBonus 위에 최대 lowHealthBonus까지 더한다.
    /// </summary>
    [Serializable]
    public class LDY_MadDogScorer : LDY_IActionScorer
    {
        [SerializeField] private int attackBonus = 40;
        [SerializeField] private int lowHealthBonus = 40;

        public int Score(LDY_Animal self, in LDY_EnemyAction action, LDY_BoardManager board)
        {
            if (action.Kind != LDY_ActionKind.Attack) return 0;
            if (self == null || self.health == null) return attackBonus;

            int max = self.health.MaxValue;
            if (max <= 0) return attackBonus;

            float missingRatio = Mathf.Clamp01((max - self.health.GetValue()) / (float)max);
            return attackBonus + Mathf.RoundToInt(lowHealthBonus * missingRatio);
        }
    }
}
