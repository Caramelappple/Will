using UnityEngine;

namespace _Scripts.JSJ
{
    [CreateAssetMenu(menuName = "JSJ/TestSO", fileName = "TestSO")]
    public class JSJ_TestSO : ScriptableObject
    {
        public float stickLength;
        public float appleRadius;
        public bool isGreen;
        [Range(0,100)]public float caramelDensity;
    }
}
