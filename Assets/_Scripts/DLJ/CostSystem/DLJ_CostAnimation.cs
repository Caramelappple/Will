using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 코스트 케이스를 왼쪽 바깥에서 원래 자리로 밀어 넣는다.
///
/// 이 컴포넌트가 붙은 오브젝트의 로컬 위치를 도착점으로 사용한다.
/// 화면 왼쪽에서 시작하거나 로컬 오프셋을 사용하도록 설정할 수 있다.
/// </summary>
[DefaultExecutionOrder(-100)]
public class DLJ_CostAnimation : MonoBehaviour, IDLJ_CostCaseEntrance
{
    [Header("Entrance")]
    [SerializeField] private bool enterFromScreenLeft;
    [SerializeField] private Camera screenCamera;

    [Tooltip("화면 밖 시작 위치를 계산할 케이스 중심. 비워두면 이 오브젝트를 기준으로 한다.")]
    [SerializeField] private Transform entranceAnchor;

    [Tooltip("0은 화면 왼쪽 끝. 케이스 폭까지 화면 밖에 놓이도록 음수 여백을 준다.")]
    [SerializeField] private float offscreenViewportX = -0.2f;

    [Tooltip("도착점 기준 시작 위치의 로컬 오프셋. 화면에서 보이는 방향은 카메라와 부모 Transform의 축 방향에 따라 달라진다.")]
    [SerializeField] private Vector3 entranceOffset = new Vector3(6f, 0f, 0f);

    [Tooltip("도착까지 걸리는 시간(초).")]
    [SerializeField, Min(0.01f)] private float entranceDuration = 0.45f;

    [Tooltip("등장 전 대기 시간(초). 여러 케이스를 순서대로 넣을 때 각자 다르게 설정한다.")]
    [SerializeField, Min(0f)] private float entranceDelay;

    [Tooltip("DOTween에 적용할 이동 그래프. 그래프를 눌러 원하는 가속감을 직접 조절할 수 있다.")]
    [SerializeField] private AnimationCurve entranceEase = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0f),
        new Keyframe(1f, 1f, 0f, 0f));

    [Header("Playback")]
    [Tooltip("켜면 첫 Start에서 자동으로 등장한다. 프리팹 생성 직후 위치를 정해도 그 위치를 도착점으로 잡는다.")]
    [SerializeField] private bool playOnStart = true;

    [Tooltip("켜면 Time.timeScale이 0이어도 연출이 진행된다.")]
    [SerializeField] private bool ignoreTimeScale;

    [Header("Exit")]
    [Tooltip("들어온 길로 되나가는 데 걸리는 시간(초).")]
    [SerializeField, Min(0.01f)] private float exitDuration = 0.35f;

    [Tooltip("나가기 전 대기 시간(초).")]
    [SerializeField, Min(0f)] private float exitDelay;

    [SerializeField] private Ease exitEase = Ease.InCubic;

    private Vector3 _restLocalPosition;
    private Tween _entranceTween;
    private Tween _exitTween;
    private bool _hasRestPosition;

    /// <summary>등장 대기부터 도착까지 걸리는 전체 시간.</summary>
    public float TotalDuration => entranceDelay + entranceDuration;

    /// <summary>나가는 데 걸리는 전체 시간. 기다렸다 다음 일을 할 쪽이 본다.</summary>
    public float TotalExitDuration => exitDelay + exitDuration;

    /// <summary>
    /// 들어오는 중인지. **나가는 중은 포함하지 않는다.**
    ///
    /// 코인 연출(DLJ_CostCoinEntranceAnimator)이 이 값을 보고 케이스를 기다리는데,
    /// 나가는 것까지 세면 나가는 케이스를 기다렸다가 코인을 쏟는다.
    /// </summary>
    public bool IsPlaying => _entranceTween != null && _entranceTween.IsActive();

    /// <summary>나가는 중인지.</summary>
    public bool IsExiting => _exitTween != null && _exitTween.IsActive();

    /// <summary>도착했을 때. **들어올 때만 나간다** — 코인은 이 신호를 듣고 쏟아진다.</summary>
    public event Action Completed;

    /// <summary>다 나갔을 때. 나가는 것을 기다렸다 다음 일을 하는 쪽이 쓴다.</summary>
    public event Action Exited;

    private void Awake()
    {
        CaptureRestPosition();
    }

    private void Start()
    {
        // 스포너가 Start 전에 PlayEntrance를 호출했다면 그 도착점을 그대로 쓴다.
        if (_entranceTween != null) return;

        // Instantiate 직후 스포너가 위치를 정할 시간을 준 뒤 최종 위치를 저장한다.
        // Awake/OnEnable에서 재생하면 프리팹 원점이 도착점으로 굳을 수 있다.
        CaptureRestPosition();

        if (playOnStart)
            PlayEntrance();
    }

    /// <summary>
    /// 현재 로컬 위치를 도착점으로 다시 저장한다.
    /// 런타임에 케이스 위치를 옮긴 뒤 재생하려면 먼저 호출한다.
    /// </summary>
    public void CaptureRestPosition()
    {
        _restLocalPosition = transform.localPosition;
        _hasRestPosition = true;
    }

    /// <summary>저장된 도착점을 향해 왼쪽 진입 연출을 재생한다.</summary>
    [ContextMenu("Play Entrance")]
    public void PlayEntrance()
    {
        if (!_hasRestPosition)
            CaptureRestPosition();

        KillEntranceTween();
        KillExitTween();

        transform.localPosition = _restLocalPosition;
        transform.localPosition = GetEntranceLocalPosition();

        _entranceTween = transform
            .DOLocalMove(_restLocalPosition, entranceDuration)
            .SetDelay(entranceDelay)
            .SetEase(entranceEase)
            .SetUpdate(ignoreTimeScale)
            .SetLink(gameObject)
            .OnComplete(() => Completed?.Invoke())
            .OnKill(() => _entranceTween = null);
    }

    private Vector3 GetEntranceLocalPosition()
    {
        Camera entranceCamera = screenCamera != null ? screenCamera : Camera.main;
        if (!enterFromScreenLeft || entranceCamera == null)
            return _restLocalPosition + entranceOffset;

        Vector3 anchorPosition = entranceAnchor != null ? entranceAnchor.position : transform.position;
        Vector3 viewportPosition = entranceCamera.WorldToViewportPoint(anchorPosition);
        viewportPosition.x = offscreenViewportX;
        Vector3 startWorldPosition = transform.position +
            entranceCamera.ViewportToWorldPoint(viewportPosition) - anchorPosition;

        return transform.parent != null
            ? transform.parent.InverseTransformPoint(startWorldPosition)
            : startWorldPosition;
    }

    /// <summary>
    /// 들어온 길로 되나간다. 도착점은 진입 시작점과 같은 자리다.
    ///
    /// Completed 를 쏘지 않는다. 그 신호는 "다 들어왔다"는 뜻이고,
    /// 코인 연출이 그걸 듣고 쏟아지기 때문이다. 나갈 때는 Exited 를 쏜다.
    /// </summary>
    [ContextMenu("Play Exit")]
    public void PlayExit()
    {
        if (!_hasRestPosition)
            CaptureRestPosition();

        KillEntranceTween();
        KillExitTween();

        // 지금 자리에서 계산한다. 진입 시작점은 화면 기준이라
        // 케이스가 제자리에 있을 때 재야 맞다.
        Vector3 away = GetEntranceLocalPosition();

        _exitTween = transform
            .DOLocalMove(away, exitDuration)
            .SetDelay(exitDelay)
            .SetEase(exitEase)
            .SetUpdate(ignoreTimeScale)
            .SetLink(gameObject)
            .OnComplete(() => Exited?.Invoke())
            .OnKill(() => _exitTween = null);
    }

    /// <summary>진행 중인 연출을 멈추고 즉시 도착점에 놓는다.</summary>
    public void CompleteEntrance()
    {
        bool wasPlaying = IsPlaying;
        KillEntranceTween();

        if (_hasRestPosition)
            transform.localPosition = _restLocalPosition;

        if (wasPlaying)
            Completed?.Invoke();
    }

    private void OnDisable()
    {
        KillEntranceTween();
        KillExitTween();

        // 나가던 도중에 꺼지면 화면 밖에 굳는다. 다시 켰을 때 케이스가 사라진 것처럼 보인다.
        if (_hasRestPosition)
            transform.localPosition = _restLocalPosition;
    }

    private void KillEntranceTween()
    {
        if (_entranceTween == null) return;

        Tween tween = _entranceTween;
        _entranceTween = null;
        tween.Kill();
    }

    private void KillExitTween()
    {
        if (_exitTween == null) return;

        Tween tween = _exitTween;
        _exitTween = null;
        tween.Kill();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        entranceDuration = Mathf.Max(0.01f, entranceDuration);
        entranceDelay = Mathf.Max(0f, entranceDelay);

        if (entranceEase == null || entranceEase.length == 0)
            entranceEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }
#endif
}
