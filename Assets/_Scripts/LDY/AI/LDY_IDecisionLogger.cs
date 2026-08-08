using System.Collections.Generic;

namespace _Scripts.LDY.AI
{
    public readonly struct LDY_ScoreEntry
    {
        public readonly string ScorerName;
        public readonly int Score;

        public LDY_ScoreEntry(string scorerName, int score)
        {
            ScorerName = scorerName;
            Score = score;
        }
    }

    /// <summary>
    /// 밸런싱용 점수 추적 경로. LDY_EnemyBrain.Logger가 null이면 내역을 만들지도 않는다.
    /// breakdown 리스트는 후보마다 재사용되므로 구현체가 보관하지 말고 그 자리에서 소비할 것.
    /// </summary>
    public interface LDY_IDecisionLogger
    {
        void LogCandidate(LDY_Animal self, in LDY_EnemyAction action, IReadOnlyList<LDY_ScoreEntry> breakdown, int total);

        void LogDecision(LDY_Animal self, in LDY_EnemyAction action, int total);
    }
}
