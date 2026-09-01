using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 코인을 케이스와 분리한 뒤 화면 밖 왼쪽 → 슬롯 위 → 슬롯 안 경로로 이동시킨다.
/// 코인 상태 개수는 모르며, 전달받은 슬롯의 입장 연출만 담당한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class DLJ_CostCoinEntranceAnimator : MonoBehaviour, IDLJ_CostCoinEntranceEffect
{
    [Header("Path")]
    [Tooltip("화면 좌표를 계산할 카메라. 비워두면 Main Camera를 사용한다.")]
    [SerializeField] private Camera screenCamera;

    [Tooltip("코인이 출발할 화면 X 위치. 0이 화면 왼쪽 끝이며 음수면 화면 밖이다.")]
    [SerializeField] private float offscreenViewportX = -0.1f;

    [Tooltip("코인이 케이스 위에서 대기할 화면 높이. 화면 높이의 비율로 계산한다.")]
    [SerializeField, Min(0f)] private float aboveViewportOffset = 0.08f;

    [Header("Timing")]
    [SerializeField, Min(0.01f)] private float horizontalDuration = 0.3f;
    [SerializeField, Min(0.01f)] private float dropDuration = 0.18f;
    [SerializeField, Min(0f)] private float coinInterval = 0.1f;

    [Tooltip("첫 코인 출발부터 마지막 코인 도착까지 허용할 최대 시간.")]
    [SerializeField, Min(0.01f)] private float maxEntranceDuration = 1.2f;

    [SerializeField] private AnimationCurve horizontalEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve dropEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Playback")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool waitForCaseEntrance = true;

    [Tooltip("케이스 진입 연출이 끝난 뒤 첫 코인이 출발하기 전까지 추가로 기다릴 시간.")]
    [SerializeField, Min(0f)] private float afterCaseEntranceDelay;

    [SerializeField] private bool ignoreTimeScale;

    private readonly Dictionary<Transform, Sequence> _sequences = new Dictionary<Transform, Sequence>();
    private IReadOnlyList<DLJ_CostCoinSlot> _slots;
    private Transform _animationRoot;
    private IDLJ_CostCaseEntrance _caseEntrance;
    private int _pendingInitialCount;
    private bool _waitingForCase;

    private Camera EntranceCamera => screenCamera != null ? screenCamera : Camera.main;
    public bool PlayOnStart => playOnStart;

    public void Bind(IReadOnlyList<DLJ_CostCoinSlot> slots)
    {
        _slots = slots;
        ResolveCaseEntrance();
    }

    public void PrepareInitialCoins()
    {
        if (_slots == null) return;

        for (int i = 0; i < _slots.Count; i++)
            PrepareSlot(_slots[i]);
    }

    public void PrepareSlot(DLJ_CostCoinSlot slot)
    {
        if (slot == null || !slot.IsValid) return;

        Stop(slot);
        EnsureAnimationRoot();
        slot.DetachTo(_animationRoot);
        slot.Coin.gameObject.SetActive(false);
    }

    public void PlayInitial(int filledCount)
    {
        if (!playOnStart || _slots == null) return;

        _pendingInitialCount = Mathf.Clamp(filledCount, 0, _slots.Count);
        ResolveCaseEntrance();

        if (waitForCaseEntrance && _caseEntrance != null && _caseEntrance.IsPlaying)
        {
            UnsubscribeFromCaseEntrance();
            _waitingForCase = true;
            _caseEntrance.Completed += HandleCaseEntranceCompleted;
            return;
        }

        PlayRange(0, _pendingInitialCount, afterCaseEntranceDelay);
    }

    public void PlayRange(int startIndex, int endIndex, float initialDelay = 0f)
    {
        if (_slots == null) return;

        startIndex = Mathf.Clamp(startIndex, 0, _slots.Count);
        endIndex = Mathf.Clamp(endIndex, startIndex, _slots.Count);
        int animatedCount = endIndex - startIndex;
        if (animatedCount <= 0) return;

        float rawDuration = horizontalDuration + dropDuration + coinInterval * (animatedCount - 1);
        float timingScale = rawDuration > maxEntranceDuration
            ? maxEntranceDuration / rawDuration
            : 1f;

        float actualHorizontalDuration = horizontalDuration * timingScale;
        float actualDropDuration = dropDuration * timingScale;
        float actualInterval = coinInterval * timingScale;

        for (int i = startIndex; i < endIndex; i++)
        {
            DLJ_CostCoinSlot slot = _slots[i];
            if (slot == null || !slot.IsValid) continue;

            int order = i - startIndex;
            PrepareSlot(slot);

            Sequence sequence = DOTween.Sequence()
                .SetDelay(initialDelay + actualInterval * order)
                .AppendCallback(() => BeginEntrance(slot, actualHorizontalDuration, actualDropDuration))
                .SetUpdate(ignoreTimeScale)
                .SetLink(gameObject);

            Track(slot.Coin, sequence);
        }
    }

    public bool IsAnimating(DLJ_CostCoinSlot slot)
    {
        return slot != null && slot.IsValid && _sequences.ContainsKey(slot.Coin);
    }

    public void Stop(DLJ_CostCoinSlot slot)
    {
        if (slot == null || !slot.IsValid || !_sequences.TryGetValue(slot.Coin, out Sequence sequence)) return;

        _sequences.Remove(slot.Coin);
        sequence.Kill();
    }

    public void RestoreAll()
    {
        KillAllSequences();
        if (_slots == null) return;

        for (int i = 0; i < _slots.Count; i++)
        {
            DLJ_CostCoinSlot slot = _slots[i];
            if (slot == null || !slot.IsValid) continue;

            bool wasActive = slot.Coin.gameObject.activeSelf;
            slot.Restore(wasActive);
        }
    }

    private void BeginEntrance(
        DLJ_CostCoinSlot slot,
        float actualHorizontalDuration,
        float actualDropDuration)
    {
        if (slot == null || !slot.IsValid) return;

        Vector3 restWorldPosition = slot.GetRestWorldPosition();
        Camera cameraForEntrance = EntranceCamera;
        if (cameraForEntrance == null)
        {
            Debug.LogError($"{name}: 코인 등장 경로를 계산할 카메라가 없습니다.", this);
            slot.Restore(true);
            return;
        }

        Vector3 restViewportPosition = cameraForEntrance.WorldToViewportPoint(restWorldPosition);
        Vector3 aboveViewportPosition = new Vector3(
            restViewportPosition.x,
            restViewportPosition.y + aboveViewportOffset,
            restViewportPosition.z);
        Vector3 startViewportPosition = new Vector3(
            offscreenViewportX,
            aboveViewportPosition.y,
            restViewportPosition.z);

        Vector3 aboveWorldPosition = cameraForEntrance.ViewportToWorldPoint(aboveViewportPosition);
        Vector3 startWorldPosition = cameraForEntrance.ViewportToWorldPoint(startViewportPosition);

        slot.Coin.position = startWorldPosition;
        slot.Coin.gameObject.SetActive(true);

        Sequence movement = DOTween.Sequence()
            .Append(slot.Coin.DOMove(aboveWorldPosition, actualHorizontalDuration).SetEase(horizontalEase))
            .Append(slot.Coin.DOMove(restWorldPosition, actualDropDuration).SetEase(dropEase))
            .SetUpdate(ignoreTimeScale)
            .SetLink(gameObject)
            .OnComplete(() => slot.Restore(true));

        Track(slot.Coin, movement);
    }

    private void Track(Transform coin, Sequence sequence)
    {
        _sequences[coin] = sequence;
        sequence.OnKill(() =>
        {
            if (_sequences.TryGetValue(coin, out Sequence tracked) && tracked == sequence)
                _sequences.Remove(coin);
        });
    }

    private void ResolveCaseEntrance()
    {
        if (_caseEntrance != null) return;

        MonoBehaviour[] candidates = GetComponentsInParent<MonoBehaviour>(true);
        foreach (MonoBehaviour candidate in candidates)
        {
            if (candidate is IDLJ_CostCaseEntrance entrance)
            {
                _caseEntrance = entrance;
                return;
            }
        }
    }

    private void HandleCaseEntranceCompleted()
    {
        UnsubscribeFromCaseEntrance();
        PlayRange(0, _pendingInitialCount, afterCaseEntranceDelay);
    }

    private void UnsubscribeFromCaseEntrance()
    {
        if (_waitingForCase && _caseEntrance != null)
            _caseEntrance.Completed -= HandleCaseEntranceCompleted;

        _waitingForCase = false;
    }

    private void EnsureAnimationRoot()
    {
        if (_animationRoot != null) return;

        GameObject rootObject = new GameObject($"{name}_CoinAnimationRoot");
        _animationRoot = rootObject.transform;

        if (gameObject.scene.IsValid() && rootObject.scene != gameObject.scene)
            SceneManager.MoveGameObjectToScene(rootObject, gameObject.scene);
    }

    private void KillAllSequences()
    {
        Sequence[] sequences = new Sequence[_sequences.Count];
        _sequences.Values.CopyTo(sequences, 0);
        _sequences.Clear();

        foreach (Sequence sequence in sequences)
            sequence?.Kill();
    }

    private void OnDisable()
    {
        UnsubscribeFromCaseEntrance();
        RestoreAll();
    }

    private void OnDestroy()
    {
        if (_animationRoot != null)
            Destroy(_animationRoot.gameObject);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        horizontalDuration = Mathf.Max(0.01f, horizontalDuration);
        dropDuration = Mathf.Max(0.01f, dropDuration);
        coinInterval = Mathf.Max(0f, coinInterval);
        maxEntranceDuration = Mathf.Max(0.01f, maxEntranceDuration);
        aboveViewportOffset = Mathf.Max(0f, aboveViewportOffset);
        afterCaseEntranceDelay = Mathf.Max(0f, afterCaseEntranceDelay);

        if (horizontalEase == null || horizontalEase.length == 0)
            horizontalEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        if (dropEase == null || dropEase.length == 0)
            dropEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }
#endif
}
