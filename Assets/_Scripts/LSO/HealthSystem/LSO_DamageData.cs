namespace _Scripts.LSO.HealthSystem
{
    public struct LSO_DamageData
    {
        public readonly LSO_HealthSystem giver;
        public readonly int damage;
        
        private LSO_DamageData(LSO_HealthSystem giver, int damage)
        {
            this.giver = giver;
            this.damage = damage;
        }
        
        public static LSO_DamageData Create(LSO_HealthSystem giver, int damage)
        {
            LSO_DamageData result = new LSO_DamageData(giver, damage);
            return result;
        }
    }
}