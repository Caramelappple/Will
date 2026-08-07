using System;
using UnityEngine;

namespace _Scripts.LDY.AI
{
    [Serializable]
    public class LDY_AttackPriorityScorer : LDY_IActionScorer
    {
        [SerializeField] private int bonus = 100;

        public int Score(LDY_Animal self, in LDY_EnemyAction action, LDY_BoardManager board)
        {
            return action.Kind == LDY_ActionKind.Attack ? bonus : 0;
        }
    }
}
