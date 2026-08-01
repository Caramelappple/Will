using _Scripts.LDY;

namespace _Scripts.LSO.Ability
{
    public interface IStatModifier
    {
        public int ModifyAttack(LDY_Animal self, int atk);
    }
}