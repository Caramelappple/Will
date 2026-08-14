using System;
using System.Collections.Generic;
using _Scripts.LDY.Boss.BullKing;
using UnityEngine;

namespace _Scripts.LDY.AI.Boss
{
    /// <summary>
    /// 황소왕의 이동 판단. 돌진이 아닌 이동은 고르지 않게 만들고, 돌진끼리는 피해로 줄을 세운다.
    ///
    /// 이동 후보를 만드는 건 LDY_MoveSystem이고 그건 8방향 아무 칸이나 내놓는다.
    /// scorer는 후보를 지울 수 없으므로, 돌진이 아닌 후보에 큰 감점을 줘서 대기(0점)에도 지게 만든다.
    /// 그래서 황소왕은 "옆으로 한 칸 비켜서기" 같은 이동을 절대 하지 않는다.
    ///
    /// 무엇이 돌진인지는 LDY_ChargePath가 정한다. 여기에 다시 적으면 AI의 예상과 실제 결과가 어긋난다.
    /// </summary>
    [Serializable]
    public class LDY_BullChargeScorer : LDY_IActionScorer
    {
        [Tooltip("기물을 들이받는 돌진의 기본 점수.\n" +
                 "LDY_AttackPriorityScorer의 공격 가산점(100)보다 낮게 두면 " +
                 "붙어 있는 상대는 돌진 대신 그냥 때린다. 여러 기물을 밀 수 있을 때만 돌진이 이긴다.")]
        [SerializeField] private int collisionBonus = 60;

        [Tooltip("돌진으로 밀리는 상대 기물 1개당 가산점. 같은 값이 아군에게는 감점으로 적용된다.")]
        [SerializeField] private int perVictimBonus = 25;

        [Tooltip("이번 충돌로 확실히 죽는 상대 기물 1개당 가산점.")]
        [SerializeField] private int killBonus = 40;

        [Tooltip("줄이 막혀 벽 충돌이 나는 경우의 가산점.")]
        [SerializeField] private int wallSlamBonus = 20;

        [Tooltip("아무도 못 들이받는 돌진의 점수 기울기. 가장 가까운 상대에 한 칸 다가갈 때마다 이만큼.\n" +
                 "상하좌우에 아무도 없고 대각선에만 있을 때 그쪽으로 붙는 것이 이 점수다.")]
        [SerializeField] private int approachWeight = 10;

        [Tooltip("돌진이 아닌 이동에 매기는 감점. 대기(0점)를 확실히 이기지 못할 만큼 커야 한다.")]
        [Min(0)]
        [SerializeField] private int nonChargePenalty = 200;

        // 후보 하나를 재는 동안만 쓰는 작업 버퍼.
        private readonly List<LDY_Animal> _chain = new();

        public int Score(LDY_Animal self, in LDY_EnemyAction action, LDY_BoardManager board)
        {
            if (action.Kind != LDY_ActionKind.Move) return 0;
            if (self == null || board == null) return 0;

            // 황소왕이 아닌 기물에는 관여하지 않는다. 레지스트리 배선이 잘못돼도 남의 판단을 망치지 않는다.
            LDY_BullKingBoss boss = self.GetComponent<LDY_BullKingBoss>();
            if (boss == null) return 0;

            LDY_BullChargeRule rule = boss.Rule;

            if (!LDY_ChargePath.TryPlan(board, self.pos, action.MoveTo, rule.chargeRange, out LDY_ChargeLine line))
                return -nonChargePenalty;

            return line.Collides
                ? ScoreCollision(self, board, line, rule)
                : ScoreApproach(self, board, line);
        }

        /// <summary>부딪히는 돌진. 밀려날 줄을 미리 세워보고 예상 피해로 점수를 매긴다.</summary>
        private int ScoreCollision(
            LDY_Animal self, LDY_BoardManager board, in LDY_ChargeLine line, LDY_BullChargeRule rule)
        {
            LDY_ChargePath.CollectPushChain(board, line.Blocker, line.Direction, rule.maxChainPush, _chain);
            if (_chain.Count == 0) return 0;

            bool advanced = LDY_ChargePath.CanAdvance(board, _chain[_chain.Count - 1], line.Direction);

            int score = collisionBonus;
            if (!advanced) score += wallSlamBonus;

            for (int i = 0; i < _chain.Count; i++)
            {
                LDY_Animal victim = _chain[i];
                if (victim == null) continue;

                // 아군을 밀어 넣는 것은 이득이 아니다. 같은 무게로 부호만 뒤집어 되돌린다.
                if (victim.team == self.team)
                {
                    score -= perVictimBonus;
                    continue;
                }

                score += perVictimBonus;

                int damage = rule.collisionDamage;
                if (!advanced && i == _chain.Count - 1)
                    damage += rule.wallDamage;

                if (victim.health != null && victim.health.GetValue() <= damage)
                    score += killBonus;
            }

            _chain.Clear();
            return score;
        }

        /// <summary>
        /// 아무도 못 만나는 돌진. 가장 가까운 상대에 얼마나 가까워지는지로만 고른다.
        ///
        /// 기획서의 "상하좌우에 기물이 없고 대각선에만 있으면 그쪽으로 가까워지는 방향" 규칙이 이것이다.
        /// 방향을 따로 특수 처리하지 않아도, 대각선 상대에 붙는 쪽이 자연히 높은 점수를 받는다.
        /// </summary>
        private int ScoreApproach(LDY_Animal self, LDY_BoardManager board, in LDY_ChargeLine line)
        {
            int now = NearestOpponentDistance(self.pos, self.team, board);
            int there = NearestOpponentDistance(line.Destination, self.team, board);

            if (now < 0 || there < 0) return 0;

            return (now - there) * approachWeight; // 가까워지면 +, 멀어지면 −
        }

        /// <summary>
        /// 가장 가까운 상대까지의 거리. 상대가 하나도 없으면 -1.
        /// 거리 척도는 보드 전체와 같은 것을 써야 하므로 LDY_AttackRangeMetrics에 맡긴다.
        /// </summary>
        private static int NearestOpponentDistance(Vector3Int from, LDY_Team team, LDY_BoardManager board)
        {
            List<LDY_Animal> opponents = board.GetAllByTeam(Opponent(team));
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

        private static LDY_Team Opponent(LDY_Team team)
        {
            return team == LDY_Team.Enemy ? LDY_Team.Player : LDY_Team.Enemy;
        }
    }
}
