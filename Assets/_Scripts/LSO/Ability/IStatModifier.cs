using _Scripts.LDY;

namespace _Scripts.LSO.DeathSystem
{
    public interface IStatModifier
    {
        public int ModifyAttack(LDY_Animal self, int atk);
    }
}