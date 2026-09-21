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

    /// <summary>
    /// 내려갔다 올라오는 중인지. Hiding 이 "닫는 중"인지 "갈아 끼우려고 내려가는 중"인지
    /// 가르는 데 쓴다. 두 경우에 닫기 요청을 다르게 받아야 한다.
    /// </summary>
    private bool _replaying;

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
        _Scripts.LSO.Sound.LSO_GameAudio.Play(_Scripts.LSO.Sound.LSO_SoundCue.InfoOpen, 107, 0f);
        Play(_shownLocalPosition, showDuration, showCurve, State.Showing, State.Shown, false);
    }

    public void Hide()
    {
        EnsureInitialized();

        // 내려갔다 올라오는 중이면 Hiding 이어도 받아준다.
        // 안 받으면 내려가는 도중에 누른 닫기가 씹히고, 창은 도로 올라온다 —
        // 닫으라고 눌렀는데 열리는 것처럼 보인다.
        if (!_replaying && (_state == State.Hidden || _state == State.Hiding))
            return;

        _replaying = false;
        _Scripts.LSO.Sound.LSO_GameAudio.Play(_Scripts.LSO.Sound.LSO_SoundCue.InfoOpen, 107, 0f);

        Play(_hiddenLocalPosition, hideDuration, hideCurve, State.Hiding, State.Hidden, true);
    }

    /// <summary>
    /// 내려갔다가 다시 올라온다. 띄운 채로 내용이 바뀔 때 쓴다.
    ///
    /// ── 왜 필요한가 ───────────────────────────────────────────
    /// 창이 떠 있는 동안 다른 기물을 보면 글자만 갈리고 창은 제자리에 있었다.
    /// 눈에 띄는 변화가 없어서 지금 누가 떠 있는지, 클릭이 먹기는 했는지
    /// 알기 어려웠다. 한 번 내렸다 올리면 "바뀌었다"가 분명해진다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 내려가는 동안 오브젝트를 끄지 않는다. 껐다 켜면 이 코루틴이 같이 죽어
    /// 창이 내려간 채로 남는다.
    /// </summary>
    /// <param name="onBottom">
    /// 바닥에 닿은 순간 부른다. 내용을 갈아 끼우는 자리다 —
    /// 올라오는 도중에 바꾸면 바뀌는 장면이 그대로 보인다.
    /// </param>
    public void Replay(System.Action onBottom = null)
    {
        EnsureInitialized();

        // 아직 한 번도 안 떴으면 갈아 끼울 것이 없다. 그냥 올린다.
        if (_state == State.Hidden)
        {
            onBottom?.Invoke();
            Show();
            return;
        }

        StopCurrentAnimation();

        animatedTarget.gameObject.SetActive(true);
        _replaying = true;
        _animationRoutine = StartCoroutine(ReplayRoutine(onBottom));
    }

    private IEnumerator ReplayRoutine(System.Action onBottom)
    {
        // ── 열고 닫을 때의 절반 속도로 돈다 ───────────────────────
        // 내려갔다 올라오느라 두 번 움직이므로, 그대로 쓰면 창을 새로 여는 것보다
        // 두 배로 오래 걸린다. 대상을 바꾸는 것은 여는 것보다 가벼운 동작인데
        // 기다림은 더 길어지는 셈이다.
        //
        // 값을 따로 두지 않는 이유는 맞춰야 할 숫자가 늘기 때문이다. 여는 속도를
        // 고치면 이쪽도 같이 따라가야 하는데, 한쪽만 고치고 지나가기 쉽다.
        // ─────────────────────────────────────────────────────────
        float downDuration = hideDuration * 0.5f;
        float upDuration = showDuration * 0.5f;

        _state = State.Hiding;

        if (downDuration > 0f)
            yield return Move(_hiddenLocalPosition, downDuration, hideCurve);
        else
            animatedTarget.localPosition = _hiddenLocalPosition;

        onBottom?.Invoke();

        _state = State.Showing;
        _Scripts.LSO.Sound.LSO_GameAudio.Play(_Scripts.LSO.Sound.LSO_SoundCue.InfoOpen, 107, 0f);

        if (upDuration > 0f)
            yield return Move(_shownLocalPosition, upDuration, showCurve);
        else
            animatedTarget.localPosition = _shownLocalPosition;

        _animationRoutine = null;
        _replaying = false;
        _state = State.Shown;
    }

    /// <summary>씬 시작 시 애니메이션 없이 닫힌 상태로 맞춘다.</summary>
    public void HideImmediate()
    {
        EnsureInitialized();
        StopCurrentAnimation();

        _replaying = false;

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
        yield return Move(destination, duration, curve);

        _animationRoutine = null;
        Complete(completedState, deactivateWhenCompleted);
    }

    /// <summary>
    /// 지금 자리에서 destination 까지 옮긴다. 상태도 활성화도 건드리지 않는다.
    ///
    /// 옮기는 계산은 여기 하나다. Replay 가 이것을 두 번 이어 붙여 쓰므로,
    /// 곡선이나 시간 처리를 고칠 일이 생겨도 한 곳만 보면 된다.
    /// </summary>
    private IEnumerator Move(Vector3 destination, float duration, AnimationCurve curve)
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
