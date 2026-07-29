using _Scripts.LSO.HealthSystem;

public readonly struct DamageData
{
    public readonly Health giver;
    public readonly int damage;

    public DamageData(Health giver, int damage)
    {
        this.giver = giver;
        this.damage = damage;
    }

    public static DamageData Create(Health giver, int damage)
    {
        return new DamageData(giver, damage);
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