using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LSO.Will
{
    [CreateAssetMenu(fileName = "DLJ_WillDatabase", menuName = "DLJ/Will/Database")]
    public class DLJ_WillDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<DLJ_WillDataSO> wills = new();

        public DLJ_WillDataSO Get(LSO_WillType willType)
        {
            foreach (DLJ_WillDataSO will in wills)
            {
                if (will != null && will.WillType == willType)
                    return will;
            }

            Debug.LogError($"Will data is missing: {willType}", this);
            return null;
        }
    }
}
