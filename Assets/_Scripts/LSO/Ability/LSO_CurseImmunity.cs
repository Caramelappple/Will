using System;
using _Scripts.LSO.CoreLib;
using _Scripts.LSO.HealthSystem;
using _Scripts.LSO.HealthSystem.Data;
using UnityEngine;

namespace _Scripts.LSO.Ability
{
    /// <summary>
    /// 키위 - 저주 면역: 저주에서 온 피해를 전부 무효화한다. 횟수 제한은 없다.
    /// 출처가 Curse인 피해만 막으므로 일반 공격이나 다른 유언 피해는 그대로 받는다.
    /// </summary>
    public sealed class LSO_CurseImmunity : LSO_IAbility, LSO_IDamageModifier
    {
        /// <summary>
        /// 무효화 계열은 다른 감쇄보다 먼저 처리한다.
        /// 여기서 0으로 만들어두면 옹골참 같은 1회성 특성이 헛되이 소진되지 않는다.
        /// </summary>
        public int Priority => LSO_DamagePriority.Nullify;

        /// <summary>무효화가 일어났을 때 알린다. 연출·로그용.</summary>
        public event Action<DamageableResources> Immuned;

        public int ModifyIncomingDamage(DamageableResources target, DamageData data, int damage)
        {
            if (damage <= 0) return damage;
            if (data.source != LSO_DamageSource.Curse) return damage;

            Immuned?.Invoke(target);

            // 누가 얼마를 주려 했는지까지 적는다.
            //
            // "무효" 한 줄만 있으면 막은 것이 맞는지 확인하려고 체력을 따로 봐야 한다.
            // 준 쪽까지 있으면 저주 장판이 여러 겹일 때 몇 번 막았는지도 그대로 읽힌다.
            string who = target != null ? target.name : "대상";
            string giver = data.giver != null ? data.giver.name : "알 수 없는 저주";

            LSO_AbilitySignal.Raise(
                LSO_AbilityType.CurseImmunity, target,
                $"<color=violet>[비타민] {who}: {giver} 의 저주 피해 {damage} 무효 " +
                $"(남은 체력 {(target != null ? target.Value.ToString() : "?")})</color>");

            return 0;
        }
    }
}
