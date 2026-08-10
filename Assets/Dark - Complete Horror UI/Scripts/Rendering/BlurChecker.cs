using UnityEngine;
using UnityEngine.Rendering;

namespace Michsky.UI.Dark
{
    [ExecuteInEditMode]
    public class BlurChecker : MonoBehaviour
    {
        void OnEnable()
        {
            if (GraphicsSettings.defaultRenderPipeline != null && gameObject.activeSelf == true)
                gameObject.SetActive(false);
        }
    }
}