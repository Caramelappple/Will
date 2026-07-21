using _Scripts.LSO.Animal;

namespace _Scripts.LSO.HealthSystem
{
    public readonly struct LSO_DamageData
    {
        public readonly LSO_Animal giver;
        public readonly int damage;

        private LSO_DamageData(LSO_Animal giver, int damage)
        {
            this.giver = giver;
            this.damage = damage;
        }

        public static LSO_DamageData Create(LSO_Animal giver, int damage)
            => new LSO_DamageData(giver, damage);
    }
}