using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LDY.AI
{
    /// <summary>
    /// 이동으로 가장 가까운 상대까지의 거리가 얼마나 줄었는지를 점수로 낸다.
    /// 절대 거리가 아니라 개선량이라 좋은 이동은 양수가 된다 — 가까워지면 +, 멀어지면 -.
    /// stepWeight를 곱해 LDY_FrontlineScorer(0~7)와 자릿수를 분리한다.
    /// 거리는 x/z 평면 맨해튼 거리다(pos.y는 모델 표시용 높이라 무시).
    /// </summary>
    [Serializable]
    public class LDY_ApproachScorer : LDY_IActionScorer
    {
        [SerializeField] private int stepWeight = 10;

        public int Score(LDY_Animal self, in LDY_EnemyAction action, LDY_BoardManager board)
        {
            if (action.Kind != LDY_ActionKind.Move) return 0;
            if (self == null || board == null) return 0;

            List<LDY_Animal> opponents = board.GetAllByTeam(Opponent(self.team));

            int before = NearestDistance(self.pos, opponents);
            int after = NearestDistance(action.MoveTo, opponents);
            if (before < 0 || after < 0) return 0;

            return (before - after) * stepWeight;
        }

        /// <summary>가장 가까운 상대까지의 거리. 상대가 하나도 없으면 -1.</summary>
        private static int NearestDistance(Vector3Int from, List<LDY_Animal> opponents)
        {
            int nearest = int.MaxValue;

            foreach (LDY_Animal opponent in opponents)
            {
                if (opponent == null) continue;

                int distance = Manhattan(from, opponent.pos);
                if (distance < nearest)
                    nearest = distance;
            }

            return nearest == int.MaxValue ? -1 : nearest;
        }

        private static LDY_Team Opponent(LDY_Team team)
        {
            return team == LDY_Team.Enemy ? LDY_Team.Player : LDY_Team.Enemy;
        }

        private static int Manhattan(Vector3Int a, Vector3Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.z - b.z);
        }
    }
}
