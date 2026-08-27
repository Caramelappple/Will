using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LSO.Will
{
    [CreateAssetMenu(fileName = "DLJ_WillDatabase", menuName = "DLJ/Will/Database")]
    public class DLJ_WillDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<DLJ_WillDataSO> wills = new();
        [SerializeField] private DLJ_StatIncreaseEffectSO statIncreaseEffect;

        public DLJ_StatIncreaseEffectSO StatIncreaseEffect => statIncreaseEffect;

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
