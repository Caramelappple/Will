using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LSO.Will
{
    public class LSO_WillFactory
    {
        private static readonly Dictionary<LSO_WillType, Func<DLJ_WillContext, LSO_IWill>> Creators =
            new()
            {
                { LSO_WillType.Curse, DLJ_CurseSystem.Create },
                { LSO_WillType.Rage, DLJ_RageSystem.Create },
                { LSO_WillType.Succession, DLJ_SuccessionSystem.Create }
            };

        public static LSO_IWill Create(
            LSO_WillType type,
            DLJ_WillContext context)
        {
            if (!Creators.TryGetValue(type, out Func<DLJ_WillContext, LSO_IWill> creator))
            {
                Debug.LogError($"Unknown will type: {type}");
                return null;
            }

            return creator(context);
        }
    }
}
