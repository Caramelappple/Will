using System;
using System.Collections.Generic;
using _Scripts.LSO.Boss.CrowKing;
using UnityEngine;

namespace _Scripts.LSO.Ability
{
    public static class LSO_AbilityFactory
    {
        // 특성은 개체마다 상태(발동 여부, 누적 수치 등)를 가질 수 있으므로
        // 완성된 인스턴스가 아니라 "생성 방법"을 등록한다. 인스턴스를 공유하면 상태가 섞인다.
        private static readonly Dictionary<LSO_AbilityType, Func<LSO_IAbility>> Creators =
            new ()
            {
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
                { LSO_AbilityType.Predation, () => new LSO_Predation() },
                { LSO_AbilityType.MemoryFrenzy, () => new LSO_MemoryFrenzy() },
                { LSO_AbilityType.PreyMarking, () => new LSO_PreyMarking() },
            };

        /// <summary>
        /// 특성 하나를 만든다. None이면 조용히 null을 돌려준다.
        ///
        /// 등록되지 않은 값을 고르면 경고를 낸다.
        /// enum에는 있는데 위 표에 없는 항목이 있어서, 예전에는 인스펙터에서 고르고도
        /// 아무 일이 일어나지 않는 이유를 알 방법이 없었다.
        /// </summary>
        public static LSO_IAbility Create(LSO_AbilityType type)
        {
            if (type == LSO_AbilityType.None) return null;

            if (Creators.TryGetValue(type, out Func<LSO_IAbility> creator))
                return creator();

            Debug.LogWarning(
                $"LSO_AbilityFactory: '{type}'은(는) 구현체가 등록되지 않아 붙지 않습니다. " +
                $"에셋에서 다른 특성으로 바꾸거나 LSO_AbilityFactory에 등록하세요.");

            return null;
        }
    }
}
