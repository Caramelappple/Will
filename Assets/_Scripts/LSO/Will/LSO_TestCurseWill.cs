using UnityEngine;

namespace _Scripts.LSO.Will
{
    public class LSO_TestCurseWill : LSO_IWill
    {
        public void InvokeWill()
        {
            Debug.Log($"{typeof(LSO_TestCurseWill).FullName}.{nameof(InvokeWill)}");
        }
    }
}