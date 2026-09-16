using System;
using _Scripts.LSO.CoreLib;
using _Scripts.LSO.HealthSystem;
using _Scripts.LSO.HealthSystem.Data;
using UnityEngine;

namespace _Scripts.LSO.Ability
{
    /// <summary>
    /// 옹골참: 처음으로 즉사할 만한 데미지를 받으면 HP를 1 남기고 버틴다.
    /// 개체당 한 번만 발동하므로 반드시 개체마다 새 인스턴스를 만들어 써야 한다.
    /// </summary>
    public sealed class LSO_Sturdy : LSO_IAbility, LSO_IDamageModifier
    {
        /// <summary>맨 뒤. "이 피해로 죽는가"를 보려면 최종 값이어야 한다.</summary>
        public int Priority => LSO_DamagePriority.LastStand;

        public bool HasTriggered { get; private set; }
        
        public event Action<DamageableResources> Triggered;

        public int ModifyIncomingDamage(DamageableResources target, DamageData data, int damage)
        {
            if (HasTriggered) return damage;
            if (target == null) return damage;

            // 막을 수 없는 피해는 비켜선다. 한 번뿐인 발동을 여기서 써버리지 않도록
            // HasTriggered 도 건드리지 않는다 — 버티지 못했는데 기회만 날리면 억울하다.
            if (LSO_DamageRules.IgnoresDefenses(data.source)) return damage;
            
            int survivableDamage = target.Value - 1;
            if (survivableDamage <= 0) return damage;
            
            if (damage <= survivableDamage) return damage;

            HasTriggered = true;
            LSO_AbilitySignal.Raise(LSO_AbilityType.Sturdy, target,
                $"<color=yellow>{target.name}의 옹골참: 치명타를 버티고 HP 1이 남았습니다</color>");
            Triggered?.Invoke(target);

            return survivableDamage;
        }
    }
}
