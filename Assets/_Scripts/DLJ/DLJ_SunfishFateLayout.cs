using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>저장된 UI 배치를 그대로 사용하고 문구/맥박 투명도만 갱신한다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas), typeof(CanvasGroup))]
public sealed class DLJ_SunfishFateLayout : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_Text shadow;
    [Tooltip("맥박에 맞춰 투명도가 변할 붉은 장식들")]
    [SerializeField] private Graphic[] pulseGraphics;

    public void SetMessage(string message, TMP_FontAsset fontOverride = null)
    {
        SetLabel(label, message, fontOverride);
        SetLabel(shadow, message, fontOverride);
    }

    private static void SetLabel(TMP_Text target, string message, TMP_FontAsset fontOverride)
    {
        if (target == null) return;
        target.text = message ?? string.Empty;
        if (fontOverride != null) target.font = fontOverride;
    }

    public void SetPulse(float alpha)
    {
        if (pulseGraphics == null) return;
        foreach (Graphic graphic in pulseGraphics)
            if (graphic != null) graphic.canvasRenderer.SetAlpha(Mathf.Clamp01(alpha));
    }
}
