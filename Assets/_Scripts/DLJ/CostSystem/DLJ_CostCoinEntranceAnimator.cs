using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 케이스가 도착한 뒤 코인을 화면 밖 왼쪽에서 슬롯 안으로 수평 이동시킨다.
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

    [Header("Timing")]
    [SerializeField, Min(0.01f)] private float horizontalDuration = 0.3f;
    [SerializeField, Min(0f)] private float coinInterval = 0.1f;

    [Tooltip("첫 코인 출발부터 마지막 코인 도착까지 허용할 최대 시간.")]
    [SerializeField, Min(0.01f)] private float maxEntranceDuration = 1.2f;

    [SerializeField] private AnimationCurve horizontalEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Playback")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool waitForCaseEntrance = true;

    [Tooltip("케이스 진입 연출이 끝난 뒤 첫 코인이 출발하기 전까지 추가로 기다릴 시간.")]
    [SerializeField, Min(0f)] private float afterCaseEntranceDelay;

    [SerializeField] private bool ignoreTimeScale;

    private readonly Dictionary<Transform, Sequence> _sequences = new Dictionary<Transform, Sequence>();
    private readonly HashSet<DLJ_CostCoinSlot> _pendingSlots = new HashSet<DLJ_CostCoinSlot>();
    private IReadOnlyList<DLJ_CostCoinSlot> _slots;
    private Transform _animationRoot;
    private IDLJ_CostCaseEntrance _caseEntrance;
    private float _pendingDelay;
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
        _pendingSlots.Add(slot);
    }

    public void PlayInitial(int filledCount)
    {
        if (!playOnStart || _slots == null) return;

        PlayRange(0, filledCount, afterCaseEntranceDelay);
    }

    public void PlayRange(int startIndex, int endIndex, float initialDelay = 0f)
    {
        if (_slots == null) return;

        startIndex = Mathf.Clamp(startIndex, 0, _slots.Count);
        endIndex = Mathf.Clamp(endIndex, startIndex, _slots.Count);
        for (int i = startIndex; i < endIndex; i++)
            PrepareSlot(_slots[i]);

        ResolveCaseEntrance();
        if (waitForCaseEntrance && _caseEntrance != null && _caseEntrance.IsPlaying)
        {
            _pendingDelay = Mathf.Max(_pendingDelay, initialDelay, afterCaseEntranceDelay);
            if (!_waitingForCase)
            {
                _waitingForCase = true;
                _caseEntrance.Completed += HandleCaseEntranceCompleted;
            }
            return;
        }

        PlayPreparedSlots(initialDelay);
    }

    private void PlayPreparedSlots(float initialDelay)
    {
        int animatedCount = _pendingSlots.Count;
        if (animatedCount <= 0) return;

        float rawDuration = horizontalDuration + coinInterval * (animatedCount - 1);
        float timingScale = rawDuration > maxEntranceDuration
            ? maxEntranceDuration / rawDuration
            : 1f;

        float actualHorizontalDuration = horizontalDuration * timingScale;
        float actualInterval = coinInterval * timingScale;

        int order = 0;
        for (int i = 0; i < _slots.Count; i++)
        {
            DLJ_CostCoinSlot slot = _slots[i];
            if (slot == null || !slot.IsValid || !_pendingSlots.Remove(slot)) continue;

            Sequence sequence = DOTween.Sequence()
                .SetDelay(initialDelay + actualInterval * order)
                .AppendCallback(() => BeginEntrance(slot, actualHorizontalDuration))
                .SetUpdate(ignoreTimeScale)
                .SetLink(gameObject);

            Track(slot.Coin, sequence);
            order++;
        }
    }

    public bool IsAnimating(DLJ_CostCoinSlot slot)
    {
        // 케이스를 기다리는 동안에도 코스트 갱신이 코인을 슬롯에 복구하면 안 된다.
        return slot != null && slot.IsValid &&
            (_pendingSlots.Contains(slot) || _sequences.ContainsKey(slot.Coin));
    }

    public void Stop(DLJ_CostCoinSlot slot)
    {
        if (slot == null) return;

        _pendingSlots.Remove(slot);
        if (!slot.IsValid || !_sequences.TryGetValue(slot.Coin, out Sequence sequence)) return;

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
        float actualHorizontalDuration)
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
        Vector3 startViewportPosition = new Vector3(
            offscreenViewportX,
            restViewportPosition.y,
            restViewportPosition.z);

        Vector3 startWorldPosition = cameraForEntrance.ViewportToWorldPoint(startViewportPosition);

        slot.Coin.position = startWorldPosition;
        slot.Coin.gameObject.SetActive(true);

        Sequence movement = DOTween.Sequence()
            .Append(slot.Coin.DOMove(restWorldPosition, actualHorizontalDuration).SetEase(horizontalEase))
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
        float initialDelay = _pendingDelay;
        UnsubscribeFromCaseEntrance();
        // 대기 중 소비된 코인은 Stop에서 제거되므로 다시 나타나지 않는다.
        PlayPreparedSlots(initialDelay);
    }

    private void UnsubscribeFromCaseEntrance()
    {
        if (_waitingForCase && _caseEntrance != null)
            _caseEntrance.Completed -= HandleCaseEntranceCompleted;

        _waitingForCase = false;
        _pendingDelay = 0f;
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
        _pendingSlots.Clear();
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
        coinInterval = Mathf.Max(0f, coinInterval);
        maxEntranceDuration = Mathf.Max(0.01f, maxEntranceDuration);
        afterCaseEntranceDelay = Mathf.Max(0f, afterCaseEntranceDelay);

        if (horizontalEase == null || horizontalEase.length == 0)
            horizontalEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    }
#endif
}
