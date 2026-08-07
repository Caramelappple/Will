using System;
using UnityEngine;

namespace _Scripts.LDY.AI
{
    /// <summary>이번 공격으로 대상을 확실히 처치할 수 있으면 가산한다.</summary>
    [Serializable]
    public class LDY_KillBonusScorer : LDY_IActionScorer
    {
        [SerializeField] private int bonus = 50;

        public int Score(LDY_Animal self, in LDY_EnemyAction action, LDY_BoardManager board)
        {
            if (action.Kind != LDY_ActionKind.Attack) return 0;
            if (self == null || action.Target == null || action.Target.health == null) return 0;

            return action.Target.health.GetValue() <= self.GetAtk() ? bonus : 0;
        }
    }
}
