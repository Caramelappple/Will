using System;
using _Scripts.LSO.HealthSystem;
using _Scripts.LSO.HealthSystem.Data;
using UnityEngine;

namespace _Scripts.LSO.Ability
{
    /// <summary>
    /// 옹골참: 처음으로 즉사할 만한 데미지를 받으면 HP를 1 남기고 버틴다.
    /// 개체당 한 번만 발동하므로 반드시 개체마다 새 인스턴스를 만들어 써야 한다.
    /// </summary>
    public class LSO_Sturdy : LSO_IAbility, LSO_IDamageModifier
    {
        public int Priority => 1000;

        public bool HasTriggered { get; private set; }
        
        public event Action<DamageableResources> Triggered;

        public int ModifyIncomingDamage(DamageableResources target, DamageData data, int damage)
        {
            if (HasTriggered) return damage;
            if (target == null) return damage;
            
            int survivableDamage = target.Value - 1;
            if (survivableDamage <= 0) return damage;
            
            if (damage <= survivableDamage) return damage;

            HasTriggered = true;
            Debug.Log($"특성 발동 {this.ToString()} {survivableDamage}");
            Triggered?.Invoke(target);

            return survivableDamage;
        }
    }
}
