using UnityEngine;

namespace _Scripts.LSO.Will
{
    public class LSO_TestCurseWill : LSO_IWill
    {
        public bool ShouldDeferDestruction => false;

        public void InvokeWill()
        {
            Debug.Log($"{typeof(LSO_TestCurseWill).FullName}.{nameof(InvokeWill)}");
        }
    }
}
