using System.Collections.Generic;

namespace _Scripts.LSO.Will
{
    public class LSO_WillFactory
    {
        private static readonly Dictionary<LSO_WillType, LSO_IWill> Strategies =
            new ()
            {
                { LSO_WillType.Curse, new LSO_TestCurseWill() },
            };

        public static LSO_IWill Get(LSO_WillType type)
        {
            return Strategies.GetValueOrDefault(type);
        }
    }
}