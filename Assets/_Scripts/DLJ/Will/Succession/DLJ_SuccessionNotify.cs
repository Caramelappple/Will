using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform), typeof(RectMask2D))]
public sealed class DLJ_SuccessionNotify : MonoBehaviour
{
    [Header("Reveal")]
    [SerializeField, Min(1f)] private float visibleWidth = 800f;
    [SerializeField, Min(1f)] private float visibleHeight = 120f;
    [SerializeField, Min(0.05f)] private float revealDuration = 0.75f;
    [SerializeField] private bool playOnEnable = true;

    private RectTransform maskRect;
    private Coroutine revealCoroutine;
    private Coroutine UnrevealCoroutine;

    private void Awake()
    {
        maskRect = (RectTransform)transform;
        //SetMaskSize(visibleWidth, visibleHeight);

        if (!Mathf.Approximately(maskRect.pivot.x, 0.5f))
        {
            Debug.LogWarning(
                $"{name}: 중앙에서 펼치려면 RectTransform Pivot X를 0.5로 설정해야 합니다.",
                this);
        }
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            PlayReveal();
        }
    }

    private void OnDisable()
    {
        if (revealCoroutine != null)
        {
            StopCoroutine(revealCoroutine);
            revealCoroutine = null;
        }

        SetMaskSize(visibleWidth, visibleHeight);
    }

    public void PlayReveal()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (revealCoroutine != null)
        {
            StopCoroutine(revealCoroutine);
        }

        revealCoroutine = StartCoroutine(RevealRoutine());
    }
    
    public void PlayUnreveal()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (UnrevealCoroutine != null)
        {
            StopCoroutine(UnrevealCoroutine);
        }

        UnrevealCoroutine = StartCoroutine(UnrevealRoutine());
    }

    public void ShowAndPlay()
    {
        bool wasActive = gameObject.activeSelf;
        if (!wasActive)
        {
            gameObject.SetActive(true);
        }

        // 비활성 상태에서 켜졌다면 OnEnable이 대신 재생한다.
        if (wasActive || !playOnEnable)
        {
            PlayReveal();
        }
    }
    
    public void Unable()
    {
        // 비활성 상태에서 켜졌다면 OnEnable이 대신 재생한다.
        if (!playOnEnable)
        {
            PlayUnreveal();
        }
    }

    public void ShowImmediately()
    {
        if (revealCoroutine != null)
        {
            StopCoroutine(revealCoroutine);
            revealCoroutine = null;
        }

        SetMaskSize(visibleWidth, visibleHeight);
    }

    private IEnumerator RevealRoutine()
    {
        SetMaskSize(0f, visibleHeight);
        yield return null;

        float duration = Mathf.Max(0.05f, revealDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            SetMaskSize(visibleWidth * easedProgress, visibleHeight);

            yield return null;
        }

        SetMaskSize(visibleWidth, visibleHeight);
        revealCoroutine = null;
    }
    
    private IEnumerator UnrevealRoutine()
    {
        float duration = Mathf.Max(0.05f, revealDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = Mathf.SmoothStep(1f, 0f, progress);
            SetMaskSize(visibleWidth * easedProgress, visibleHeight);

            yield return null;
        }

        SetMaskSize(0, visibleHeight);
        gameObject.SetActive(false);
        revealCoroutine = null;
    }

    private void SetMaskSize(float width, float height)
    {
        maskRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        maskRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }
}
