namespace _Scripts.LDY.AI
{
    /// <summary>
    /// 행동 후보 하나에 점수를 매긴다. 관심 없는 Kind에는 0을 돌려주고,
    /// 여러 scorer의 점수는 Brain이 단순 합산한다.
    /// </summary>
    public interface LDY_IActionScorer
    {
        int Score(LDY_Animal self, in LDY_EnemyAction action, LDY_BoardManager board);
    }
}
