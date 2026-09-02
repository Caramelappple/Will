using UnityEngine;

namespace _Scripts.JSJ
{
    public class JSJ_Test : MonoBehaviour
    {
        [SerializeField] private JSJ_TestSO data;

        private void Start()
        {
            A();
        }
        [ContextMenu("A")]
        private void A()
        {
            Debug.Log(data.appleRadius);
            Debug.Log(data.stickLength);
            Debug.Log(data.isGreen);
            Debug.Log(data.caramelDensity);
        }

    }
}
