using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ESC를 짧게 누르면 안내 문구만 보여주고,
/// 길게 누르면 두 단계로 화면을 어둡게 만든 뒤 메인 메뉴로 돌아간다.
/// </summary>
[DisallowMultipleComponent]
public sealed class DLJ_EscapeKey : MonoBehaviour
{
    [Header("안내 문구")]
    [Tooltip("ESC를 짧게 눌렀을 때 투명도가 바뀔 TMP 텍스트")]
    [SerializeField] private TMP_Text guideText;

    [Tooltip("평소 문구의 알파값")]
    [SerializeField, Range(0f, 1f)] private float hiddenTextAlpha;

    [Tooltip("ESC를 누르는 동안과 짧게 누른 직후의 알파값")]
    [SerializeField, Range(0f, 1f)] private float visibleTextAlpha = 1f;

    [Tooltip("문구가 선명해지는 데 걸리는 시간")]
    [SerializeField, Min(0f)] private float textFadeInDuration = 0.2f;

    [Tooltip("짧게 누른 뒤 문구가 선명하게 유지되는 시간")]
    [SerializeField, Min(0f)] private float textVisibleDuration = 0.8f;

    [Tooltip("문구가 다시 사라지는 데 걸리는 시간")]
    [SerializeField, Min(0f)] private float textFadeOutDuration = 0.35f;

    [Header("화면 암전")]
    [Tooltip("화면 전체를 덮는 검은색 UI Image. 안내 문구보다 뒤에 둘 것")]
    [SerializeField] private Graphic dimOverlay;

    [Tooltip("평소 오버레이의 알파값")]
    [SerializeField, Range(0f, 1f)] private float hiddenDimAlpha;

    [Tooltip("1차 지점에서 도달할 약한 암전 알파값")]
    [SerializeField, Range(0f, 1f)] private float softDimAlpha = 0.25f;

    [Header("알파 웨이포인트")]
    [Tooltip("ESC를 누른 뒤 약한 암전을 시작할 때까지의 시간")]
    [SerializeField, Min(0f)] private float softDimWaypoint = 2f;

    [Tooltip("약한 암전으로 변하는 데 걸리는 시간")]
    [SerializeField, Min(0f)] private float softDimFadeDuration = 0.25f;

    [Tooltip("약한 암전 지점부터 알파 1 전환까지 추가로 눌러야 하는 시간")]
    [SerializeField, Min(0f)] private float fullDimWaypoint = 1.5f;

    [Tooltip("최종 지점에서 알파 1로 빠르게 변하는 데 걸리는 시간")]
    [SerializeField, Min(0f)] private float fullDimFadeDuration = 0.12f;

    [Header("씬 전환")]
    [Tooltip("Build Settings에 등록된 메인 메뉴 씬 이름")]
    [SerializeField] private string mainMenuSceneName = "LSO_UI Scene";

    [Tooltip("완전히 어두운 화면을 유지한 뒤 씬을 불러올 때까지의 시간")]
    [SerializeField, Min(0f)] private float blackoutHoldDuration = 0.08f;

    private Coroutine textAnimationCoroutine;
    private Coroutine dimAnimationCoroutine;
    private Coroutine sceneTransitionCoroutine;
    private float pressedAt;
    private bool isPressed;
    private bool softDimStarted;
    private bool longPressHandled;

    private void Awake()
    {
        if (dimOverlay != null)
            dimOverlay.raycastTarget = false;

        SetTextAlpha(hiddenTextAlpha);
        SetDimAlpha(hiddenDimAlpha);
    }

    private void Update()
    {
        if (Keyboard.current == null || sceneTransitionCoroutine != null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            BeginPress();

        if (isPressed && !longPressHandled)
            UpdateHold();

        if (Keyboard.current.escapeKey.wasReleasedThisFrame)
            EndPress();
    }

    private void OnDisable()
    {
        isPressed = false;
        softDimStarted = false;
        longPressHandled = false;

        StopRunningCoroutine(ref textAnimationCoroutine);
        StopRunningCoroutine(ref dimAnimationCoroutine);
        StopRunningCoroutine(ref sceneTransitionCoroutine);

        SetTextAlpha(hiddenTextAlpha);
        SetDimAlpha(hiddenDimAlpha);
    }

    private void BeginPress()
    {
        isPressed = true;
        softDimStarted = false;
        longPressHandled = false;
        pressedAt = Time.unscaledTime;

        StopRunningCoroutine(ref textAnimationCoroutine);
        StopRunningCoroutine(ref dimAnimationCoroutine);

        // 짧게 누르는 동안에는 화면이 어두워지지 않는다.
        SetDimAlpha(hiddenDimAlpha);
        textAnimationCoroutine = StartCoroutine(ShowAndHideTextRoutine());
    }

    private void UpdateHold()
    {
        float heldDuration = Time.unscaledTime - pressedAt;

        if (!softDimStarted && heldDuration >= softDimWaypoint)
        {
            softDimStarted = true;
            float startAlpha = GetDimAlpha();
            dimAnimationCoroutine = StartCoroutine(
                FadeDimRoutine(startAlpha, softDimAlpha, softDimFadeDuration));
        }

        float fullDimTime = softDimWaypoint + fullDimWaypoint;
        if (heldDuration < fullDimTime)
            return;

        longPressHandled = true;
        BeginSceneTransition();
    }

    private void EndPress()
    {
        if (!isPressed)
            return;

        isPressed = false;

        if (longPressHandled && sceneTransitionCoroutine != null)
            return;

        StopRunningCoroutine(ref dimAnimationCoroutine);
        dimAnimationCoroutine = StartCoroutine(
            FadeDimAndClearRoutine(GetDimAlpha(), hiddenDimAlpha, textFadeOutDuration));
    }

    private IEnumerator ShowAndHideTextRoutine()
    {
        float startAlpha = GetTextAlpha();
        yield return FadeTextRoutine(startAlpha, visibleTextAlpha, textFadeInDuration);

        while (isPressed && !longPressHandled)
            yield return null;

        if (longPressHandled && sceneTransitionCoroutine != null)
        {
            textAnimationCoroutine = null;
            yield break;
        }

        if (textVisibleDuration > 0f)
            yield return new WaitForSecondsRealtime(textVisibleDuration);

        yield return FadeTextRoutine(GetTextAlpha(), hiddenTextAlpha, textFadeOutDuration);
        textAnimationCoroutine = null;
    }

    private IEnumerator FadeTextRoutine(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetTextAlpha(to);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetTextAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }

        SetTextAlpha(to);
    }

    private IEnumerator FadeDimRoutine(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetDimAlpha(to);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetDimAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }

        SetDimAlpha(to);
    }

    private IEnumerator FadeDimAndClearRoutine(float from, float to, float duration)
    {
        yield return FadeDimRoutine(from, to, duration);
        dimAnimationCoroutine = null;
    }

    private void BeginSceneTransition()
    {
        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            Debug.LogError($"{nameof(DLJ_EscapeKey)}: 메인 메뉴 씬 이름이 비어 있습니다.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            Debug.LogError(
                $"{nameof(DLJ_EscapeKey)}: Build Settings에서 '{mainMenuSceneName}' 씬을 찾을 수 없습니다.",
                this);
            return;
        }

        StopRunningCoroutine(ref dimAnimationCoroutine);
        sceneTransitionCoroutine = StartCoroutine(SceneTransitionRoutine());
    }

    private IEnumerator SceneTransitionRoutine()
    {
        // 최종 웨이포인트에서는 배경을 완전히 어둡게 만들면서 문구도 함께 지운다.
        float startDimAlpha = GetDimAlpha();
        float startTextAlpha = GetTextAlpha();

        if (fullDimFadeDuration <= 0f)
        {
            SetDimAlpha(1f);
            SetTextAlpha(hiddenTextAlpha);
        }
        else
        {
            float elapsed = 0f;

            while (elapsed < fullDimFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / fullDimFadeDuration;

                SetDimAlpha(Mathf.Lerp(startDimAlpha, 1f, progress));
                SetTextAlpha(Mathf.Lerp(startTextAlpha, hiddenTextAlpha, progress));
                yield return null;
            }
        }

        // 최소 한 프레임은 완전한 검은 화면이 실제로 렌더링되게 둔다.
        SetDimAlpha(1f);
        SetTextAlpha(hiddenTextAlpha);
        yield return null;

        if (blackoutHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(blackoutHoldDuration);

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private float GetTextAlpha()
    {
        return guideText != null ? guideText.color.a : hiddenTextAlpha;
    }

    private float GetDimAlpha()
    {
        return dimOverlay != null ? dimOverlay.color.a : hiddenDimAlpha;
    }

    private void SetTextAlpha(float alpha)
    {
        if (guideText == null)
            return;

        Color color = guideText.color;
        color.a = Mathf.Clamp01(alpha);
        guideText.color = color;
    }

    private void SetDimAlpha(float alpha)
    {
        if (dimOverlay == null)
            return;

        Color color = dimOverlay.color;
        color.a = Mathf.Clamp01(alpha);
        dimOverlay.color = color;
    }

    private void StopRunningCoroutine(ref Coroutine coroutine)
    {
        if (coroutine == null)
            return;

        StopCoroutine(coroutine);
        coroutine = null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        textFadeInDuration = Mathf.Max(0f, textFadeInDuration);
        textVisibleDuration = Mathf.Max(0f, textVisibleDuration);
        textFadeOutDuration = Mathf.Max(0f, textFadeOutDuration);
        softDimWaypoint = Mathf.Max(0f, softDimWaypoint);
        softDimFadeDuration = Mathf.Max(0f, softDimFadeDuration);
        fullDimWaypoint = Mathf.Max(0f, fullDimWaypoint);
        fullDimFadeDuration = Mathf.Max(0f, fullDimFadeDuration);
        blackoutHoldDuration = Mathf.Max(0f, blackoutHoldDuration);

        if (dimOverlay != null)
            dimOverlay.raycastTarget = false;
    }
#endif
}
