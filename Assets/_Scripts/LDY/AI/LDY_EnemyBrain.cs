using System;
using System.Collections.Generic;

namespace _Scripts.LDY.AI
{
    /// <summary>
    /// 한 기물이 이번에 할 행동을 고른다. 결정만 반환하고 이동/공격을 실제로 실행하지는 않는다.
    /// 후보 열거는 기존 LDY_MoveSystem / LDY_AttackSystem을 그대로 재사용하므로
    /// 점유 칸 차단·사거리 판정·행동력 소진 여부가 자동으로 반영된다.
    /// </summary>
    public class LDY_EnemyBrain
    {
        private readonly LDY_MoveSystem _moveSystem;
        private readonly LDY_AttackSystem _attackSystem;
        private readonly IReadOnlyList<LDY_IActionScorer> _sharedScorers;
        private readonly LDY_ScorerRegistry _registry;

        private readonly List<LDY_EnemyAction> _candidates = new();
        private readonly List<LDY_ScoreEntry> _breakdown = new();

        /// <summary>밸런싱용 점수 추적. null이면 내역 자체를 만들지 않는다.</summary>
        public LDY_IDecisionLogger Logger { get; set; }

        public LDY_EnemyBrain(
            LDY_MoveSystem moveSystem,
            LDY_AttackSystem attackSystem,
            IReadOnlyList<LDY_IActionScorer> sharedScorers,
            LDY_ScorerRegistry registry = null)
        {
            _moveSystem = moveSystem != null ? moveSystem : throw new ArgumentNullException(nameof(moveSystem));
            _attackSystem = attackSystem != null ? attackSystem : throw new ArgumentNullException(nameof(attackSystem));
            _sharedScorers = sharedScorers ?? throw new ArgumentNullException(nameof(sharedScorers));
            _registry = registry;
        }

        public LDY_EnemyAction Decide(LDY_Animal self, LDY_BoardManager board)
        {
            if (self == null || board == null) return LDY_EnemyAction.Wait();

            CollectCandidates(self);

            // Wait는 점수 경쟁에 넣지 않는다. Move 점수가 음수라 0점짜리 Wait이 항상 이겨버리고,
            // "공격 불가면 가장 가까운 상대에게 이동" 규칙이 무력화되기 때문이다.
            if (_candidates.Count == 0) return LDY_EnemyAction.Wait();

            IReadOnlyList<LDY_IActionScorer> ownScorers = _registry != null ? _registry.GetScorers(self) : null;

            LDY_EnemyAction best = _candidates[0];
            int bestScore = ScoreCandidate(self, best, board, ownScorers);

            for (int i = 1; i < _candidates.Count; i++)
            {
                LDY_EnemyAction candidate = _candidates[i];
                int score = ScoreCandidate(self, candidate, board, ownScorers);

                // 동점이면 먼저 열거된 후보를 유지한다.
                if (score <= bestScore) continue;

                best = candidate;
                bestScore = score;
            }

            Logger?.LogDecision(self, best, bestScore);
            return best;
        }

        // 열거 순서: 공격 후보 → 이동 후보.
        // 두 목록 모두 고정된 방향 배열/좌표 순회에서 나오므로 같은 보드 상태면 항상 같은 순서다.
        private void CollectCandidates(LDY_Animal self)
        {
            _candidates.Clear();

            foreach (LDY_Animal target in _attackSystem.GetAttackTargets(self))
            {
                if (target != null)
                    _candidates.Add(LDY_EnemyAction.Attack(target));
            }

            foreach (var tile in _moveSystem.GetMovableTiles(self))
                _candidates.Add(LDY_EnemyAction.Move(tile));
        }

        private int ScoreCandidate(
            LDY_Animal self,
            in LDY_EnemyAction action,
            LDY_BoardManager board,
            IReadOnlyList<LDY_IActionScorer> ownScorers)
        {
            bool trace = Logger != null;
            if (trace) _breakdown.Clear();

            int total = Accumulate(self, action, board, _sharedScorers, trace);
            total += Accumulate(self, action, board, ownScorers, trace);

            if (trace) Logger.LogCandidate(self, action, _breakdown, total);
            return total;
        }

        private int Accumulate(
            LDY_Animal self,
            in LDY_EnemyAction action,
            LDY_BoardManager board,
            IReadOnlyList<LDY_IActionScorer> scorers,
            bool trace)
        {
            if (scorers == null) return 0;

            int sum = 0;
            for (int i = 0; i < scorers.Count; i++)
            {
                LDY_IActionScorer scorer = scorers[i];
                if (scorer == null) continue;

                int score = scorer.Score(self, action, board);
                sum += score;

                if (trace)
                    _breakdown.Add(new LDY_ScoreEntry(scorer.GetType().Name, score));
            }

            return sum;
        }
    }
}
