using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LDY.AI
{
    /// <summary>
    /// 이동 후 가장 가까운 상대 기물까지의 거리를 음수로 매긴다 — 가까울수록 높은 점수.
    /// 거리는 x/z 평면 맨해튼 거리다(pos.y는 모델 표시용 높이라 무시).
    /// </summary>
    [Serializable]
    public class LDY_ApproachScorer : LDY_IActionScorer
    {
        public int Score(LDY_Animal self, in LDY_EnemyAction action, LDY_BoardManager board)
        {
            if (action.Kind != LDY_ActionKind.Move) return 0;
            if (self == null || board == null) return 0;

            List<LDY_Animal> opponents = board.GetAllByTeam(Opponent(self.team));
            if (opponents.Count == 0) return 0;

            int nearest = int.MaxValue;
            foreach (LDY_Animal opponent in opponents)
            {
                if (opponent == null) continue;

                int distance = Manhattan(action.MoveTo, opponent.pos);
                if (distance < nearest)
                    nearest = distance;
            }

            return nearest == int.MaxValue ? 0 : -nearest;
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
