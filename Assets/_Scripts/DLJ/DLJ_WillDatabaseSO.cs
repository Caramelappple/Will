using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LSO.Will
{
    [CreateAssetMenu(fileName = "DLJ_WillDatabase", menuName = "DLJ/Will/Database")]
    public class DLJ_WillDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<DLJ_WillData> wills = new();

        public DLJ_WillData Get(LSO_WillType willType)
        {
            foreach (DLJ_WillData will in wills)
            {
                if (will != null && will.willType == willType)
                    return will;
            }

            Debug.LogError($"Will data is missing: {willType}", this);
            return null;
        }
    }
}
