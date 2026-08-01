using _Scripts.LDY;

namespace _Scripts.LSO.Ability
{
    public class LSO_Test : IOnTurnStart, LSO_IAbility, IStatModifier
    {
        private int _baseAtk;
        
        public void OnTurnStart(LDY_Team team)//턴 지날때마다 공격력 증가략 1씩 증가
        {
            _baseAtk += 1;
        }

        public int ModifyAttack(LDY_Animal self, int atk)
        {
            DamageData data = new DamageData(self.health, 1);
            self.health.GetDamage(data);
            return atk + _baseAtk;
        }
    }
}