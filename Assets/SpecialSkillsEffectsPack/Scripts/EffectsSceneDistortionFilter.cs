using UnityEngine;

/// <summary>
/// Disables only legacy GrabPass distortion renderers in the asset preview scene.
/// Source shaders, materials, and prefabs remain unchanged.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(32000)]
public sealed class EffectsSceneDistortionFilter : MonoBehaviour
{
    private const string UnsupportedShaderName = "GAPH Custom Shader/Distortion Effect";

    private void Awake()
    {
        DisableUnsupportedRenderers();
    }

    private void LateUpdate()
    {
        // VariousEffectsScene can instantiate a different effect during Update.
        // Run before rendering so newly spawned distortion renderers never draw.
        DisableUnsupportedRenderers();
    }

    private static void DisableUnsupportedRenderers()
    {
        Renderer[] renderers = Object.FindObjectsByType<Renderer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Renderer targetRenderer in renderers)
        {
            if (!targetRenderer.enabled)
                continue;

            foreach (Material material in targetRenderer.sharedMaterials)
            {
                if (material == null || material.shader == null)
                    continue;

                if (material.shader.name != UnsupportedShaderName)
                    continue;

                targetRenderer.enabled = false;
                break;
            }
        }
    }
}
