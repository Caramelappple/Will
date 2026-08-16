using UnityEngine;

[CreateAssetMenu(fileName = "DLJ_SuccessionEffectSO", menuName = "DLJ/Will/DLJ_SuccessionEffectSO")]
public class DLJ_SuccessionEffectSO : ScriptableObject
{
    [Header("Succession Link")]
    public Color successionLineColor = new Color(0.72f, 0.92f, 1f, 0.9f);
    [Min(0.001f)] public float successionLineWidth = 0.025f;
    [Min(0f)] public float successionLineHeight = 0.35f;
    [Min(0f)] public float successionAbsorbDuration = 0.22f;
    [Min(0f)] public float successionFlashDuration = 0.18f;
    [Min(0f)] public float successionBloomIntensity = 0.8f;
    [Min(0f)] public float successionLightIntensity = 1.8f;
    [Min(0f)] public float successionLightRange = 1.6f;
}
