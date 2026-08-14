using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LDY.AI
{
    /// <summary>
    /// 이동을 사거리 기준으로 평가한다. 거리만 보던 방식으로는 원거리·점프 기물이 사거리를 맞추려고
    /// 물러설 수 없었다 — 후퇴는 항상 음수라 대기(0점)를 이기지 못했기 때문이다.
    ///
    /// 사거리에 들어가고 나가는 것은 ±로 확실히 가르고,
    /// 어느 쪽도 아닌 이동만 사거리 오차의 개선량으로 미세 조정한다.
    /// </summary>
    [Serializable]
    public class LDY_PositioningScorer : LDY_IActionScorer
    {
        // 공격 후보는 LDY_AttackPriorityScorer에서 DefaultBonus를 받는다.
        // 그보다 낮아야 "지금 공격할 수 있으면 이동보다 공격"이 유지되므로 상한을 걸어 둔다.
        [Tooltip("사거리 밖에서 안으로 들어가는 이동에 주는 보너스.")]
        [Range(0, LDY_AttackPriorityScorer.DefaultBonus - 1)]
        [SerializeField] private int enterRangeBonus = 60;

        [Tooltip("사거리 안에서 밖으로 나가는 이동에 매기는 감점.")]
        [Min(0)]
        [SerializeField] private int exitRangePenalty = 60;

        [Tooltip("사거리 안팎이 바뀌지 않는 이동의 기울기. 사거리 오차 1칸당 점수.")]
        [Min(0)]
        [SerializeField] private int stepWeight = 10;

        public int Score(LDY_Animal self, in LDY_EnemyAction action, LDY_BoardManager board)
        {
            if (action.Kind != LDY_ActionKind.Move) return 0;
            if (self == null || board == null) return 0;

            bool canNow = LDY_AttackSystem.HasTargetFrom(self, self.pos, board);
            bool canThere = LDY_AttackSystem.HasTargetFrom(self, action.MoveTo, board);

            if (canThere && !canNow) return enterRangeBonus;
            if (!canThere && canNow) return -exitRangePenalty;

            return (RangeError(self, self.pos, board) - RangeError(self, action.MoveTo, board)) * stepWeight;
        }

        /// <summary>가장 가까운 상대까지의 거리가 자기 사거리에서 얼마나 어긋나 있는지.</summary>
        private static int RangeError(LDY_Animal self, Vector3Int from, LDY_BoardManager board)
        {
            LDY_RangeSpan span = LDY_AttackRangeMetrics.Get(self.RangeType, board);
            if (!span.IsValid) return 0;

            int nearest = NearestOpponentDistance(from, self.team, board);
            return nearest < 0 ? 0 : span.ErrorFrom(nearest);
        }

        /// <summary>
        /// 가장 가까운 상대까지의 거리. 상대가 하나도 없으면 -1.
        /// 사거리 실측과 같은 척도를 써야 하므로 거리 계산은 LDY_AttackRangeMetrics에 맡긴다.
        /// </summary>
        private static int NearestOpponentDistance(Vector3Int from, LDY_Team team, LDY_BoardManager board)
        {
            List<LDY_Animal> opponents = board.GetAllByTeam(team.Opposite());
            int nearest = int.MaxValue;

            foreach (LDY_Animal opponent in opponents)
            {
                if (opponent == null) continue;

                int distance = LDY_AttackRangeMetrics.Distance(from, opponent.pos);
                if (distance < nearest)
                    nearest = distance;
            }

            return nearest == int.MaxValue ? -1 : nearest;
        }
    }
}
