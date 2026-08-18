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

        [Tooltip("돌진한 자리에서 다음에 들이받을 수 있게 되는 상대 1기물당 가산점.\n" +
                 "'각을 만드는' 재배치가 이 점수로 일어난다.")]
        [SerializeField] private int lineupBonus = 30;

        [Tooltip("아무도 못 들이받는 돌진의 점수 기울기. 가장 가까운 상대에 한 칸 다가갈 때마다 이만큼.")]
        [SerializeField] private int approachWeight = 10;

        [Tooltip("제자리에 서 있는 것에 매기는 감점.\n" +
                 "황소왕은 멈춰 서지 않는다 — 갈 곳이 마땅찮아도 일단 달린다.\n" +
                 "돌진 감점(nonChargePenalty)보다는 작아야 '대각선으로 비켜서기'까지 하지는 않는다.")]
        [Min(0)]
        [SerializeField] private int waitPenalty = 100;

        [Tooltip("돌진이 아닌 이동에 매기는 감점. 대기 감점보다 커야 한다.")]
        [Min(0)]
        [SerializeField] private int nonChargePenalty = 200;

        // 후보 하나를 재는 동안만 쓰는 작업 버퍼.
        private readonly List<LDY_Animal> _chain = new();

        public int Score(LDY_Animal self, in LDY_EnemyAction action, LDY_BoardManager board)
        {
            if (self == null || board == null) return 0;

            // 황소왕이 아닌 기물에는 관여하지 않는다. 레지스트리 배선이 잘못돼도 남의 판단을 망치지 않는다.
            LDY_BullKingBoss boss = self.GetComponent<LDY_BullKingBoss>();
            if (boss == null) return 0;

            // 멈춰 서는 것 자체에 감점을 준다.
            //
            // 돌진은 항상 끝까지 달리므로 목적지를 고를 수 없고, 그래서 상대를 지나쳐 버리는 일이 잦다.
            // 그런 돌진은 거리가 줄지 않아 0점 근처가 되는데, 대기도 0점이라 동점이면 대기가 이긴다
            // (LDY_EnemyBrain이 먼저 열거된 후보를 남긴다). 그 결과 각이 안 맞으면 아예 굳어버렸다.
            //
            // 황소왕은 멈춰 서는 기물이 아니다. 갈 곳이 마땅찮아도 일단 달리게 한다.
            if (action.Kind == LDY_ActionKind.Wait) return -waitPenalty;

            if (action.Kind != LDY_ActionKind.Move) return 0;

            LDY_BullChargeRule rule = boss.Rule;

            if (!LDY_ChargePath.TryPlan(board, self.pos, action.MoveTo, rule.chargeRange, out LDY_ChargeLine line))
                return -nonChargePenalty;

            return line.Collides
                ? ScoreCollision(self, board, line, rule)
                : ScoreApproach(self, board, line, rule);
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
        /// 아무도 못 만나는 돌진. "다음 턴에 들이받을 각이 서는가"를 먼저 보고, 그다음 거리를 본다.
        ///
        /// 거리만으로는 부족하다. 돌진은 항상 끝까지 달려서 상대를 지나쳐 버리기 때문에,
        /// 어느 방향으로 가도 거리가 줄지 않는 자리가 흔하다. 그런 자리에서 거리만 보면
        /// 모든 후보가 0점이 되어 아무 데도 못 간다.
        ///
        /// 각 점수는 그 자리를 "지금 얼마나 가까운가"가 아니라 "다음에 뭘 할 수 있는가"로 평가한다.
        /// 기획서의 "대각선에만 기물이 있으면 가까워지는 방향으로" 규칙도 결국 이걸 노린 것이다 —
        /// 대각선 상대와 같은 줄에 서야 들이받을 수 있으니, 줄을 맞추는 자리가 높은 점수를 받는다.
        /// </summary>
        private int ScoreApproach(
            LDY_Animal self, LDY_BoardManager board, in LDY_ChargeLine line, LDY_BullChargeRule rule)
        {
            int score = CountLineups(board, self, line.Destination, rule.chargeRange) * lineupBonus;

            int now = NearestOpponentDistance(self.pos, self.team, board);
            int there = NearestOpponentDistance(line.Destination, self.team, board);

            if (now >= 0 && there >= 0)
                score += (now - there) * approachWeight; // 가까워지면 +, 멀어지면 −

            return score;
        }

        /// <summary>
        /// 이 자리에 섰을 때 상하좌우로 들이받을 수 있는 상대의 수.
        ///
        /// 아직 옮기기 전이라 황소왕은 출발 칸에 서 있다. 그대로 재면 자기 자신이 길을 막은 것으로
        /// 보이므로 계산에서 빼달라고 넘긴다.
        /// </summary>
        private static int CountLineups(
            LDY_BoardManager board, LDY_Animal self, Vector3Int from, int maxSteps)
        {
            int count = 0;

            IReadOnlyList<Vector3Int> directions = LDY_ChargePath.Directions;
            for (int i = 0; i < directions.Count; i++)
            {
                LDY_ChargeLine probe =
                    LDY_ChargePath.Resolve(board, from, directions[i], maxSteps, self);

                if (probe.Collides && probe.Blocker.team != self.team)
                    count++;
            }

            return count;
        }

        /// <summary>
        /// 가장 가까운 상대까지의 거리. 상대가 하나도 없으면 -1.
        /// </summary>
        private static int NearestOpponentDistance(Vector3Int from, LDY_Team team, LDY_BoardManager board)
        {
            List<LDY_Animal> opponents = board.GetAllByTeam(Opponent(team));
            int nearest = int.MaxValue;

            foreach (LDY_Animal opponent in opponents)
            {
                if (opponent == null) continue;

                int distance = ManhattanDistance(from, opponent.pos);
                if (distance < nearest)
                    nearest = distance;
            }

            return nearest == int.MaxValue ? -1 : nearest;
        }

        /// <summary>
        /// 황소왕만 맨해튼으로 잰다. 보드의 기본 척도(LDY_AttackRangeMetrics.Distance)는
        /// 8방향 이동을 전제한 체비쇼프라, 상하좌우로만 가는 기물에게는 답이 어긋난다.
        ///
        /// (0,0)에 서서 (3,3)의 상대를 볼 때가 그렇다. 체비쇼프로는 어느 방향으로 몇 칸을 가도
        /// 거리가 3에서 줄지 않아 모든 돌진이 0점이 되고, 결국 대기(0점)가 이겨서 제자리에 선다.
        /// 기획서의 "대각선에만 기물이 있으면 가까워지는 방향으로" 규칙이 통째로 죽는 자리다.
        ///
        /// 맨해튼은 한 축만 좁혀도 거리가 줄어드므로, 대각선 상대에게도 기울기가 생긴다.
        /// </summary>
        private static int ManhattanDistance(Vector3Int a, Vector3Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.z - b.z);
        }

        private static LDY_Team Opponent(LDY_Team team)
        {
            return team == LDY_Team.Enemy ? LDY_Team.Player : LDY_Team.Enemy;
        }
    }
}
