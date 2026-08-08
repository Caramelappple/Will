using System;

namespace _Scripts.LDY.AI
{
    /// <summary>
    /// 사거리 안에 대상이 둘 이상일 때, 자기 진영 쪽으로 더 깊이 밀고 들어온 대상을 먼저 친다.
    /// z가 큰 절반이 Enemy 진영이므로(LDY_CardPlacer의 배치 구역 규칙),
    /// Enemy가 볼 때는 z가 클수록 위협이고 Player가 볼 때는 z가 작을수록 위협이다.
    /// 어느 팀이 쓰든 점수 범위는 0~7로 같다.
    /// </summary>
    [Serializable]
    public class LDY_FrontlineScorer : LDY_IActionScorer
    {
        public int Score(LDY_Animal self, in LDY_EnemyAction action, LDY_BoardManager board)
        {
            if (action.Kind != LDY_ActionKind.Attack) return 0;
            if (self == null || action.Target == null) return 0;

            int z = action.Target.pos.z;
            return self.team == LDY_Team.Enemy ? z : (LDY_BoardManager.Size - 1) - z;
        }
    }
}
