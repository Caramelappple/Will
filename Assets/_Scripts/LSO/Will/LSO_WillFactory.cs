using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LSO.Will
{
    public static class LSO_WillFactory
    {
        private static readonly Dictionary<
            LSO_WillType,
            Func<DLJ_WillContext, DLJ_WillDataSO, LSO_IWill>> Creators =
            new()
            {
                { LSO_WillType.Curse, DLJ_CurseSystem.Create },
                { LSO_WillType.Rage, DLJ_RageSystem.Create },
                { LSO_WillType.Succession, DLJ_SuccessionSystem.Create },
                { LSO_WillType.Contract, DLJ_ContractSystem.Create },
                { LSO_WillType.Sacrifice, DLJ_SacrificeSystem.Create }
            };

        public static LSO_IWill Create(
            LSO_WillType type,
            DLJ_WillContext context,
            DLJ_WillDataSO data)
        {
            if (type == LSO_WillType.None)
                return null;

            if (!Creators.TryGetValue(
                    type,
                    out Func<DLJ_WillContext, DLJ_WillDataSO, LSO_IWill> creator))
            {
                Debug.LogError($"Unknown will type: {type}");
                return null;
            }

            if (data == null)
            {
                Debug.LogError($"Will data is missing: {type}");
                return null;
            }

            return creator(context, data);
        }
    }
}
