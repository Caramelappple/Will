using System;
using _Scripts.LSO.CoreLib;
using _Scripts.LSO.HealthSystem;
using _Scripts.LSO.HealthSystem.Data;
using UnityEngine;

namespace _Scripts.LSO.Ability
{
    /// <summary>
    /// 날따름: 피해를 받을 때 일정 확률로 완전히 회피한다.
    /// 회피에 성공하면 피해가 0이 되므로, 뒤에 오는 옹골참 같은 수정자는 발동하지 않는다.
    /// </summary>
    public sealed class LSO_Dodge : LSO_IAbility, LSO_IDamageModifier
    {
        private const float DefaultDodgeChance = 0.67f;

        /// <summary>회피 판정은 다른 감쇄보다 먼저 끝나야 한다. 차례는 LSO_DamagePriority 참고.</summary>
        public int Priority => LSO_DamagePriority.Nullify;

        public float DodgeChance { get; private set; } = DefaultDodgeChance;

        /// <summary>회피에 성공했을 때 알린다. 연출·로그용.</summary>
        public event Action<DamageableResources> Dodged;

        public LSO_Dodge() { }

        public LSO_Dodge(float dodgeChance)
        {
            DodgeChance = Mathf.Clamp01(dodgeChance);
        }

        public int ModifyIncomingDamage(DamageableResources target, DamageData data, int damage)
        {
            if (damage <= 0) return damage;
            if (UnityEngine.Random.value >= DodgeChance) return damage;

            Dodged?.Invoke(target);
            LSO_AbilityLog.Log($"<color=cyan>{(target != null ? target.name : "대상")}: 회피 성공! 피해 {damage} 무효</color>", target);

            return 0;
        }
    }
}
