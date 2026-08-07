using System;

namespace _Scripts.LDY.AI
{
    /// <summary>
    /// 사거리 안에 대상이 둘 이상일 때, 적 진영 쪽으로 더 밀고 들어온 대상을 먼저 친다.
    /// 적 진영은 z가 큰 절반이므로(LDY_CardPlacer의 배치 구역 규칙) z를 그대로 점수로 쓴다. 범위는 0~7.
    /// </summary>
    [Serializable]
    public class LDY_FrontlineScorer : LDY_IActionScorer
    {
        public int Score(LDY_Animal self, in LDY_EnemyAction action, LDY_BoardManager board)
        {
            if (action.Kind != LDY_ActionKind.Attack || action.Target == null) return 0;

            return action.Target.pos.z;
        }
    }
}
