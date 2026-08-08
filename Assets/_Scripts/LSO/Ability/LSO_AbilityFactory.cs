using System;
using System.Collections.Generic;

namespace _Scripts.LSO.Ability
{
    public class LSO_AbilityFactory
    {
        // 특성은 개체마다 상태(발동 여부, 누적 수치 등)를 가질 수 있으므로
        // 완성된 인스턴스가 아니라 "생성 방법"을 등록한다. 인스턴스를 공유하면 상태가 섞인다.
        private static readonly Dictionary<LSO_AbilityType, Func<LSO_IAbility>> Creators =
            new ()
            {
                { LSO_AbilityType.Test, () => new LSO_Test() },
                { LSO_AbilityType.Sturdy, () => new LSO_Sturdy() },
                { LSO_AbilityType.Dodge, () => new LSO_Dodge() },
                { LSO_AbilityType.Bloodlust, () => new LSO_Bloodlust() },
                { LSO_AbilityType.PackTactics, () => new LSO_PackTactics() },
                { LSO_AbilityType.Thorns, () => new LSO_Thorns() },
                { LSO_AbilityType.Vengeance, () => new LSO_Vengeance() },
                { LSO_AbilityType.Frail, () => new LSO_Frail() },
                { LSO_AbilityType.CurseImmunity, () => new LSO_CurseImmunity() },
                { LSO_AbilityType.Evolve, () => new DLJ_Evolve() },
                { LSO_AbilityType.AllHeal, () => new DLJ_AllHeal() },
                { LSO_AbilityType.LifeSteal, () => new DLJ_LifeSteal() },
                { LSO_AbilityType.CostRefund, () => new DLJ_CostRefund() },
                { LSO_AbilityType.WillEnhancement, () => new DLJ_WillEnhancement() },
            };

        public static LSO_IAbility Create(LSO_AbilityType type)
        {
            return Creators.TryGetValue(type, out Func<LSO_IAbility> creator)
                ? creator()
                : null;
        }
    }
}
