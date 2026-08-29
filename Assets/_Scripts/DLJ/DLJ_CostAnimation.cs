using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 코스트 케이스를 왼쪽 바깥에서 원래 자리로 밀어 넣는다.
///
/// 이 컴포넌트가 붙은 오브젝트의 로컬 위치를 도착점으로 사용한다.
/// Entrance Offset과 Ease Curve는 인스펙터에서 직접 조절할 수 있다.
/// </summary>
[DefaultExecutionOrder(-100)]
public class DLJ_CostAnimation : MonoBehaviour, IDLJ_CostCaseEntrance
{
    [Header("Entrance")]
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

    private Vector3 _restLocalPosition;
    private Tween _entranceTween;
    private bool _hasRestPosition;

    /// <summary>등장 대기부터 도착까지 걸리는 전체 시간.</summary>
    public float TotalDuration => entranceDelay + entranceDuration;
    public bool IsPlaying => _entranceTween != null && _entranceTween.IsActive();
    public event Action Completed;

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

        transform.localPosition = _restLocalPosition + entranceOffset;

        _entranceTween = transform
            .DOLocalMove(_restLocalPosition, entranceDuration)
            .SetDelay(entranceDelay)
            .SetEase(entranceEase)
            .SetUpdate(ignoreTimeScale)
            .SetLink(gameObject)
            .OnComplete(() => Completed?.Invoke())
            .OnKill(() => _entranceTween = null);
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
    }

    private void KillEntranceTween()
    {
        if (_entranceTween == null) return;

        Tween tween = _entranceTween;
        _entranceTween = null;
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
