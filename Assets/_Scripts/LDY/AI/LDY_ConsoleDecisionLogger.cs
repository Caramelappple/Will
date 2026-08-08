using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Debug = UnityEngine.Debug;

namespace _Scripts.LDY.AI
{
    /// <summary>
    /// 점수 내역을 Unity 콘솔로 흘리는 기본 구현. Brain에 붙였을 때만 로그가 나간다.
    /// 인터페이스 구현 메서드에는 Conditional을 붙일 수 없으므로, 문자열 조립까지 통째로
    /// 조건부 컴파일 대상인 private 메서드 안에 두어 릴리즈 빌드에서 호출 자체가 사라지게 한다.
    /// </summary>
    public class LDY_ConsoleDecisionLogger : LDY_IDecisionLogger
    {
        private readonly StringBuilder _builder = new StringBuilder();

        public void LogCandidate(LDY_Animal self, in LDY_EnemyAction action, IReadOnlyList<LDY_ScoreEntry> breakdown, int total)
        {
            WriteCandidate(self, action, breakdown, total);
        }

        public void LogDecision(LDY_Animal self, in LDY_EnemyAction action, int total)
        {
            WriteDecision(self, action, total);
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        private void WriteCandidate(LDY_Animal self, LDY_EnemyAction action, IReadOnlyList<LDY_ScoreEntry> breakdown, int total)
        {
            _builder.Clear();
            _builder.Append("[LDY_EnemyBrain] ").Append(Name(self))
                    .Append(" | ").Append(action.ToString())
                    .Append(" = ").Append(total);

            for (int i = 0; i < breakdown.Count; i++)
            {
                if (breakdown[i].Score == 0) continue;
                _builder.Append("   ").Append(breakdown[i].ScorerName).Append(':').Append(breakdown[i].Score);
            }

            Debug.Log(_builder.ToString());
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        private static void WriteDecision(LDY_Animal self, LDY_EnemyAction action, int total)
        {
            Debug.Log($"[LDY_EnemyBrain] {Name(self)} → {action} (총점 {total})");
        }

        private static string Name(LDY_Animal animal)
        {
            return animal != null ? animal.name : "null";
        }
    }
}
