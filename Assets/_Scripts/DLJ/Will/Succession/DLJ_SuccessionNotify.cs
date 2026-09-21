using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>계승 대상을 고르는 동안 안내 문구를 잉크 번짐으로 표시한다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class DLJ_SuccessionNotify : MonoBehaviour
{
    [Header("Ink Reveal")]
    [SerializeField] private TMP_Text label;
    [Tooltip("다른 툴팁과 같은 DLJ_LedgerInk 머티리얼. 런타임 복사본에 글꼴 아틀라스를 연결한다.")]
    [SerializeField] private Material inkMaterial;
    [SerializeField, Min(0.05f)] private float revealDuration = 0.75f;
    [SerializeField, Min(0.05f)] private float fadeOutDuration = 0.2f;
    [SerializeField] private bool playOnEnable = true;

    private static readonly int ProgressId = Shader.PropertyToID("_InkProgress");

    private Material originalMaterial;
    private Material labelMaterial;
    private Color originalColor;
    private Coroutine animationCoroutine;

    private void Awake()
    {
        if (label == null) label = GetComponentInChildren<TMP_Text>(true);
        if (label == null || label.font == null || inkMaterial == null)
        {
            Debug.LogError(
                $"[{nameof(DLJ_SuccessionNotify)}] Text와 Ink Material 연결이 필요해.",
                this);
            enabled = false;
            return;
        }

        originalMaterial = label.fontSharedMaterial;
        originalColor = label.color;
        labelMaterial = new Material(inkMaterial) { name = $"{label.name}_SuccessionInk" };
        labelMaterial.mainTexture = label.font.atlasTexture;

        Material fontMaterial = originalMaterial != null ? originalMaterial : label.font.material;
        if (fontMaterial != null && fontMaterial.HasProperty("_GradientScale"))
            labelMaterial.SetFloat("_GradientScale", fontMaterial.GetFloat("_GradientScale"));
        labelMaterial.SetFloat("_TextureWidth", label.font.atlasWidth);
        labelMaterial.SetFloat("_TextureHeight", label.font.atlasHeight);
        label.fontSharedMaterial = labelMaterial;
        label.raycastTarget = false;

        SetVisible(0f, 0f);
    }

    private void OnEnable()
    {
        if (playOnEnable && labelMaterial != null) PlayReveal();
    }

    private void OnDisable()
    {
        StopAnimation();
        SetVisible(0f, 0f);
    }

    public void PlayReveal()
    {
        if (!isActiveAndEnabled || labelMaterial == null) return;
        StartAnimation(RevealRoutine());
    }

    public void PlayUnreveal()
    {
        if (!isActiveAndEnabled || labelMaterial == null) return;
        StartAnimation(FadeOutRoutine());
    }

    public void ShowAndPlay()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            if (playOnEnable) return;
        }

        PlayReveal();
    }

    public void Unable()
    {
        PlayUnreveal();
    }

    public void ShowImmediately()
    {
        StopAnimation();
        SetVisible(1f, 1f);
    }

    private IEnumerator RevealRoutine()
    {
        SetVisible(0f, 1f);
        float duration = Mathf.Max(0.05f, revealDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            SetVisible(Mathf.SmoothStep(0f, 1f, progress), 1f);
            yield return null;
        }

        SetVisible(1f, 1f);
        animationCoroutine = null;
    }

    private IEnumerator FadeOutRoutine()
    {
        float duration = Mathf.Max(0.05f, fadeOutDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float opacity = 1f - Mathf.Clamp01(elapsed / duration);
            SetVisible(1f, opacity);
            yield return null;
        }

        SetVisible(0f, 0f);
        animationCoroutine = null;
        gameObject.SetActive(false);
    }

    private void StartAnimation(IEnumerator routine)
    {
        StopAnimation();
        animationCoroutine = StartCoroutine(routine);
    }

    private void StopAnimation()
    {
        if (animationCoroutine == null) return;
        StopCoroutine(animationCoroutine);
        animationCoroutine = null;
    }

    private void SetVisible(float inkProgress, float opacity)
    {
        if (labelMaterial != null)
            labelMaterial.SetFloat(ProgressId, Mathf.Clamp01(inkProgress));
        if (label != null)
        {
            Color color = originalColor;
            color.a *= Mathf.Clamp01(opacity);
            label.color = color;
        }
    }

    private void OnDestroy()
    {
        if (label != null && originalMaterial != null)
            label.fontSharedMaterial = originalMaterial;
        if (labelMaterial != null) Destroy(labelMaterial);
    }
}
