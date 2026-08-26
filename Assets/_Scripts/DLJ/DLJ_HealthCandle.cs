using UnityEngine;

public class DLJ_HealthCandle : MonoBehaviour
{
    // URP Particles/Unlit 셰이더의 Inspector에 표시되는 HDR Emission 값이다.
    private static readonly int EmissionProperty = Shader.PropertyToID("_Color");

    [Header("References")]
    [SerializeField, Min(0)] private int candleIndex = 1;
    [SerializeField] private ParticleSystemRenderer flameRenderer;

    private MaterialPropertyBlock propertyBlock;
    private Material flameMaterial;
    private Color originalEmission = Color.white;
    private int previousLightLevel = -1;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        
        if (flameRenderer == null)
            flameRenderer = GetComponentInChildren<ParticleSystemRenderer>();

        flameMaterial = flameRenderer != null ? flameRenderer.sharedMaterial : null;

        if (flameMaterial != null)
        {
            if (flameMaterial.HasProperty(EmissionProperty))
                originalEmission = flameMaterial.GetColor(EmissionProperty);
        }
    }

    private void Update()
    {
        if (DLJ_PlayerHealth.Instance == null)
            return;

        int lightLevel = DLJ_PlayerHealth.Instance.GetBrightnessLevel(candleIndex);
        if (lightLevel == previousLightLevel)
            return;

        previousLightLevel = lightLevel;

        if (flameRenderer == null)
            return;

        int maximumLightLevel =
            DLJ_PlayerHealth.MaxHealthPerCandle / DLJ_PlayerHealth.BrightnessStep;
        float emissionRatio = lightLevel / (float)maximumLightLevel;

        Color emission = originalEmission * emissionRatio;
        emission.a = originalEmission.a;

        flameRenderer.GetPropertyBlock(propertyBlock);

        if (flameMaterial != null && flameMaterial.HasProperty(EmissionProperty))
            propertyBlock.SetColor(EmissionProperty, emission);

        flameRenderer.SetPropertyBlock(propertyBlock);
    }
}
