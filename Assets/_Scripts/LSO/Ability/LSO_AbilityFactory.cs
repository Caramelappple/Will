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
            };

        public static LSO_IAbility Create(LSO_AbilityType type)
        {
            return Creators.TryGetValue(type, out Func<LSO_IAbility> creator)
                ? creator()
                : null;
        }
    }
}
