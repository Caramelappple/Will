using System;
using _Scripts.LDY;
using _Scripts.LDY.AI;
using _Scripts.LSO.Boss.CrowKing;
using UnityEngine;

namespace _Scripts.LSO.AI.Boss
{
    [Serializable]
    public sealed class LSO_PreyScorer : LDY_IActionScorer
    {
        [Tooltip("사냥감을 공격하는 후보에 주는 가산점")]
        [SerializeField] private int attackBonus = 50;
        [Tooltip("사냥감이 되먹임 대상일 때 공격에 더 얹는 점수")]
        [SerializeField] private int reattackBonus = 40;

        [Tooltip("사냥감에 한 칸 가까워지는 이동에 주는 점수")]
        [SerializeField] private int approachWeight = 20;

        [Tooltip("사냥감이 되먹임 대상일 때 접근 점수에 더 얹는 가중치")]
        [SerializeField] private int reattackApproachWeight = 20;

        public int Score(LDY_Animal self, in LDY_EnemyAction action, LDY_BoardManager board)
        {
            if (self == null) return 0;
            
            LSO_PreyTracker tracker = self.GetComponent<LSO_PreyTracker>();
            
            LDY_Animal prey = tracker != null ? tracker.Prey : null;
            if (prey == null) return 0;
            
            LSO_CrowKingMemory memory = self.GetComponent<LSO_CrowKingMemory>();
            bool feedback = memory != null && memory.HasDevoured(prey.data);

            switch (action.Kind)
            {
                case LDY_ActionKind.Attack:
                    if (action.Target != prey) return 0;

                    return attackBonus + (feedback ? reattackBonus : 0);

                case LDY_ActionKind.Move:
                    return Approach(self.pos, action.MoveTo, prey.pos, feedback);

                default:
                    return 0;
            }
        }

     
        private int Approach(Vector3Int from, Vector3Int to, Vector3Int prey, bool feedback)
        {
            // 거리는 반드시 이 메서드로 잰다. 체비쇼프라 대각선 한 칸이 1이고,
            int now = LDY_AttackRangeMetrics.Distance(from, prey);
            int there = LDY_AttackRangeMetrics.Distance(to, prey);

            int weight = feedback ? approachWeight + reattackApproachWeight : approachWeight;

            return (now - there) * weight;   // 가까워지면 +, 멀어지면 −
        }
    }
}
