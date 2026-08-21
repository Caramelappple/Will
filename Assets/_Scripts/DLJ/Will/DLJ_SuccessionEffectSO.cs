using UnityEngine;

[CreateAssetMenu(fileName = "DLJ_SuccessionEffectSO", menuName = "DLJ/Will/DLJ_SuccessionEffectSO")]
public class DLJ_SuccessionEffectSO : ScriptableObject
{
    [Header("Succession Link")]
    public Color successionLineColor = new Color(0.72f, 0.92f, 1f, 0.9f);
    [Min(0.001f)] public float successionLineWidth = 0.025f;
    [Min(0f)] public float successionLineHeight = 0.35f;
    [Tooltip("프리팹이 타겟까지 이동하는 동안 회전할 각도")]
    public Vector3 successionRotation = new Vector3(0f, 360f, 0f);
    [Min(0f)] public float successionAbsorbDuration = 0.22f;
    [Min(0f)] public float successionFlashDuration = 0.18f;
    [Min(0f)] public float successionBloomIntensity = 0.8f;
    [Min(0f)] public float successionLightIntensity = 1.8f;
    [Min(0f)] public float successionLightRange = 1.6f;

    [Header("Succession Camera")]
    public bool successionCameraEnabled = true;
    [Min(0f)] public float successionCameraZoomDuration = 0.35f;
    [Range(0.1f, 1f)] public float successionCameraZoomRatio = 0.55f;
    [Min(0f)] public float successionCameraFocusHeight = 0.45f;
    public float successionCameraOrbitAngle = 24f;
    [Min(0f)] public float successionCameraRestoreDuration = 0.45f;
    [Min(0f)] public float successionCameraStartDelay = 0.4f;
    [Min(0f)] public float successionCameraMoveDuration = 2.3f;
    [Range(0f, 1f)] public float successionCameraTrackingSwitchPoint = 0.5f;
}
