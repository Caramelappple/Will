using UnityEngine;

namespace _Scripts.LDY.AI
{
    /// <summary>
    /// 기물 한 번의 행동 후보. 결정만 담고 실행은 하지 않는다.
    /// Kind에 따라 유효한 필드가 다르다 — Move는 MoveTo, Attack은 Target.
    /// </summary>
    public readonly struct LDY_EnemyAction
    {
        public readonly LDY_ActionKind Kind;
        public readonly Vector3Int MoveTo;
        public readonly LDY_Animal Target;

        private LDY_EnemyAction(LDY_ActionKind kind, Vector3Int moveTo, LDY_Animal target)
        {
            Kind = kind;
            MoveTo = moveTo;
            Target = target;
        }

        public static LDY_EnemyAction Wait() => new(LDY_ActionKind.Wait, default, null);

        public static LDY_EnemyAction Move(Vector3Int to) => new(LDY_ActionKind.Move, to, null);

        public static LDY_EnemyAction Attack(LDY_Animal target) => new(LDY_ActionKind.Attack, default, target);

        public override string ToString()
        {
            switch (Kind)
            {
                case LDY_ActionKind.Move: return $"Move({MoveTo.x},{MoveTo.z})";
                case LDY_ActionKind.Attack: return $"Attack({(Target != null ? Target.name : "null")})";
                default: return "Wait";
            }
        }
    }
}
