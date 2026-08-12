using _Scripts.LSO.HealthSystem.Data;

namespace _Scripts.LSO.HealthSystem
{
    public interface LSO_IDamageModifier
    {
        public int Priority { get; }
        
        public int ModifyIncomingDamage(DamageableResources target, DamageData data, int damage);
    }
}
