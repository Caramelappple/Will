using System.Collections;
using UnityEngine;

/// <summary>
/// 인포창을 아래에서 올리고, 닫을 때 다시 아래로 내린다.
/// 이동 시간과 보간 그래프는 Inspector에서 조절한다.
/// </summary>
public sealed class DLJ_InfoPanelAnimation : MonoBehaviour
{
    [Header("이동 대상")]
    [Tooltip("실제로 움직이고 활성화/비활성화할 인포창 루트. 비워두면 이 오브젝트를 사용한다.")]
    [SerializeField] private Transform animatedTarget;
    [Tooltip("열린 위치를 기준으로 닫혔을 때 더해질 로컬 좌표.")]
    [SerializeField] private Vector3 hiddenOffset = new Vector3(0f, -600f, 0f);

    [Header("올라오기")]
    [SerializeField, Min(0f)] private float showDuration = 0.35f;
    [Tooltip("가로축은 진행 시간, 세로축은 열린 위치까지의 이동 비율.")]
    [SerializeField] private AnimationCurve showCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("내려가기")]
    [SerializeField, Min(0f)] private float hideDuration = 0.25f;
    [Tooltip("가로축은 진행 시간, 세로축은 닫힌 위치까지의 이동 비율.")]
    [SerializeField] private AnimationCurve hideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("시간")]
    [Tooltip("게임이 일시정지되어도 UI 애니메이션을 재생한다.")]
    [SerializeField] private bool useUnscaledTime = true;

    private Vector3 _shownLocalPosition;
    private Vector3 _hiddenLocalPosition;
    private Coroutine _animationRoutine;
    private State _state;
    private bool _initialized;

    public bool IsHidden
    {
        get
        {
            EnsureInitialized();
            return _state == State.Hidden;
        }
    }

    private enum State
    {
        Hidden,
        Showing,
        Shown,
        Hiding
    }

    public void Show()
    {
        EnsureInitialized();

        if (_state == State.Shown || _state == State.Showing)
            return;

        animatedTarget.gameObject.SetActive(true);
        Play(_shownLocalPosition, showDuration, showCurve, State.Showing, State.Shown, false);
    }

    public void Hide()
    {
        EnsureInitialized();

        if (_state == State.Hidden || _state == State.Hiding)
            return;

        Play(_hiddenLocalPosition, hideDuration, hideCurve, State.Hiding, State.Hidden, true);
    }

    /// <summary>씬 시작 시 애니메이션 없이 닫힌 상태로 맞춘다.</summary>
    public void HideImmediate()
    {
        EnsureInitialized();
        StopCurrentAnimation();

        animatedTarget.localPosition = _hiddenLocalPosition;
        _state = State.Hidden;
        animatedTarget.gameObject.SetActive(false);
    }

    private void Play(
        Vector3 destination,
        float duration,
        AnimationCurve curve,
        State playingState,
        State completedState,
        bool deactivateWhenCompleted)
    {
        StopCurrentAnimation();
        _state = playingState;

        if (duration <= 0f)
        {
            animatedTarget.localPosition = destination;
            Complete(completedState, deactivateWhenCompleted);
            return;
        }

        _animationRoutine = StartCoroutine(Animate(
            destination,
            duration,
            curve,
            completedState,
            deactivateWhenCompleted));
    }

    private IEnumerator Animate(
        Vector3 destination,
        float duration,
        AnimationCurve curve,
        State completedState,
        bool deactivateWhenCompleted)
    {
        Vector3 start = animatedTarget.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float curvedProgress = curve != null ? curve.Evaluate(progress) : progress;
            animatedTarget.localPosition = Vector3.LerpUnclamped(start, destination, curvedProgress);
            yield return null;
        }

        animatedTarget.localPosition = destination;
        _animationRoutine = null;
        Complete(completedState, deactivateWhenCompleted);
    }

    private void Complete(State completedState, bool deactivate)
    {
        _state = completedState;

        if (deactivate)
            animatedTarget.gameObject.SetActive(false);
    }

    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        if (animatedTarget == null)
            animatedTarget = transform;

        _shownLocalPosition = animatedTarget.localPosition;
        _hiddenLocalPosition = _shownLocalPosition + hiddenOffset;
        _state = animatedTarget.gameObject.activeSelf ? State.Shown : State.Hidden;

        if (_state == State.Hidden)
            animatedTarget.localPosition = _hiddenLocalPosition;

        _initialized = true;
    }

    private void StopCurrentAnimation()
    {
        if (_animationRoutine == null)
            return;

        StopCoroutine(_animationRoutine);
        _animationRoutine = null;
    }
}
