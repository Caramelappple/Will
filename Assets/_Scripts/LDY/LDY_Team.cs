namespace _Scripts.LDY
{
    public enum LDY_Team
    {
        Player,
        Enemy
    }

    public static class LDY_TeamOps
    {
        public static LDY_Team Opposite(this LDY_Team team)
        {
            return team == LDY_Team.Enemy ? LDY_Team.Player : LDY_Team.Enemy;
        }

        public static bool IsEnemyOf(this LDY_Team team, LDY_Team other)
        {
            return team != other;
        }

        public static bool IsAllyOf(this LDY_Team team, LDY_Team other)
        {
            return team == other;
        }
    }
}
