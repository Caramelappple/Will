using System.Collections.Generic;
using _Scripts.LSO.Ability;

namespace _Scripts.LSO.Factories
{
    public class LSO_AbilityFactory
    {
        private static readonly Dictionary<LSO_AbilityType, LSO_IAbility> Strategies =
            new ()
            {
                { LSO_AbilityType.Test, new LSO_Test() },
            };

        public static LSO_IAbility Get(LSO_AbilityType type)
        {
            return Strategies.GetValueOrDefault(type);
        }
    }
}