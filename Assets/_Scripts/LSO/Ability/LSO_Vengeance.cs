using _Scripts.LDY;
using _Scripts.LSO.CoreLib;
using _Scripts.LSO.DeathSystem;
using _Scripts.LSO.HealthSystem.Data;
using UnityEngine;

namespace _Scripts.LSO.Ability
{
    /// <summary>
    /// 복수: 죽을 때 자신을 처치한 상대에게 고정 피해를 준다.
    /// 처치자가 없는 죽음(자멸, 장판 피해 등)에는 발동하지 않는다.
    /// </summary>
    public sealed class LSO_Vengeance : LSO_IAbility, LSO_IOnDeath, LSO_IAbilityInitializable
    {
        private const int DefaultRevengeDamage = 1;

        public int RevengeDamage { get; private set; } = DefaultRevengeDamage;

        private LSO_AbilityContext _context;

        public LSO_Vengeance() { }

        public LSO_Vengeance(int revengeDamage)
        {
            RevengeDamage = Mathf.Max(0, revengeDamage);
        }

        public void Initialize(LSO_AbilityContext context)
        {
            _context = context;
        }

        public void OnDeath(LDY_Animal self, LDY_Animal killer)
        {
            if (RevengeDamage <= 0) return;
            if (killer == null || killer.health == null || killer.health.IsDestroyed) return;

            killer.health.GetDamage(
                DamageData.Create(self != null ? self.health : null, RevengeDamage, LSO_DamageSource.Ability));

            LSO_AbilityLog.Log($"<color=magenta>{(self != null ? self.name : "기물")}의 복수: {killer.name}에게 {RevengeDamage} 피해</color>", killer);

            if (killer.health.IsDestroyed)
                _context?.Deaths?.Kill(killer, self);
        }
    }
}
