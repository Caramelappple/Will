using _Scripts.LDY;

namespace _Scripts.LSO.HealthSystem
{
    public readonly struct LSO_DamageData
    {
        public readonly LDY_Animal giver;
        public readonly int damage;

        private LSO_DamageData(LDY_Animal giver, int damage)
        {
            this.giver = giver;
            this.damage = damage;
        }

        public static LSO_DamageData Create(LDY_Animal giver, int damage)
            => new LSO_DamageData(giver, damage);
    }
}