using _Scripts.LDY;
using _Scripts.LSO.CoreLib;
using _Scripts.LSO.HealthSystem;
using _Scripts.LSO.HealthSystem.Data;
using UnityEngine;

namespace _Scripts.LSO.Ability
{
    /// <summary>
    /// 가시: 근접 공격을 받으면 공격자에게 고정 피해를 되돌린다.
    /// 원거리·점프 공격이나 저주 같은 장판 피해에는 반응하지 않는다.
    /// </summary>
    public class LSO_Thorns : LSO_IAbility, LSO_IOnHit, LSO_IAbilityInitializable
    {
        private const int DefaultReflectDamage = 1;

        public int ReflectDamage { get; private set; } = DefaultReflectDamage;

        private LSO_AbilityContext _context;

        public LSO_Thorns() { }

        public LSO_Thorns(int reflectDamage)
        {
            ReflectDamage = Mathf.Max(0, reflectDamage);
        }

        public void Initialize(LSO_AbilityContext context)
        {
            _context = context;
        }

        public void OnHit(LDY_Animal self, DamageData data)
        {
            if (ReflectDamage <= 0) return;
            if (data.source != LSO_DamageSource.Melee) return;
            if (data.giver == null) return;
            if (self == null || self.health == null || self.health.IsDestroyed) return;

            LDY_Animal attacker = data.giver.GetComponent<LDY_Animal>();
            if (attacker == null || attacker == self) return;
            if (attacker.health == null || attacker.health.IsDestroyed) return;

            attacker.health.GetDamage(
                DamageData.Create(self.health, ReflectDamage, LSO_DamageSource.Ability));

            LSO_AbilityLog.Log($"<color=green>{self.name}의 가시: {attacker.name}에게 {ReflectDamage} 반사</color>", self);

            // 반사로 상대가 죽었다면 사망 처리까지 이어줘야 보드에서 사라진다.
            if (attacker.health.IsDestroyed)
                _context?.Deaths?.Kill(attacker, self);
        }
    }
}
