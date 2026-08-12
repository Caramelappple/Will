namespace _Scripts.LSO.HealthSystem.Data
{
    public readonly struct DamageData
    {
        public readonly Health giver;
        public readonly int damage;

        /// <summary>피해 출처. 지정하지 않으면 Unknown이라 기존 호출부는 영향을 받지 않는다.</summary>
        public readonly LSO_DamageSource source;

        public DamageData(Health giver, int damage)
            : this(giver, damage, LSO_DamageSource.Unknown)
        {
        }

        public DamageData(Health giver, int damage, LSO_DamageSource source)
        {
            this.giver = giver;
            this.damage = damage;
            this.source = source;
        }

        public static DamageData Create(Health giver, int damage)
        {
            return new DamageData(giver, damage, LSO_DamageSource.Unknown);
        }

        public static DamageData Create(Health giver, int damage, LSO_DamageSource source)
        {
            return new DamageData(giver, damage, source);
        }
    }

    public readonly struct DamageResultData
    {
        public readonly Health giver;
        public readonly int damage;
        public readonly int currentHealth;

        public DamageResultData(Health giver, int damage, int currentHealth)
        {
            this.giver = giver;
            this.damage = damage;
            this.currentHealth = currentHealth;
        }

        public static DamageResultData Create(Health giver, int damage, int currentHealth)
        {
            return new DamageResultData(giver, damage, currentHealth);
        }
    }
}