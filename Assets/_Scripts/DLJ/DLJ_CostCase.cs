using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 코스트 케이스의 슬롯 상태만 관리한다.
/// 이동 연출과 소비 효과는 각각 전용 컴포넌트에 위임한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class DLJ_CostCase : MonoBehaviour
{
    [Header("Coins")]
    [Tooltip("코인을 찾기 시작할 부모. 비워두면 자신의 모든 자식을 검색한다.")]
    [SerializeField] private Transform coinRoot;

    [Tooltip("자동 수집할 코인 오브젝트 이름에 포함된 문자열.")]
    [SerializeField] private string coinNameFilter = "CostCoin";

    [Tooltip("필요하면 자동 수집 대신 코인을 순서대로 직접 넣어도 된다.")]
    [SerializeField] private List<Transform> coins = new List<Transform>();

    [Header("Presentation")]
    [Tooltip("IDLJ_CostCoinEntranceEffect를 구현한 등장 연출 컴포넌트.")]
    [SerializeField] private MonoBehaviour entranceEffectSource;

    [Tooltip("IDLJ_CostCoinSpendEffect를 구현한 소비 연출 컴포넌트.")]
    [SerializeField] private MonoBehaviour spendEffectSource;

    private readonly List<Transform> _found = new List<Transform>();
    private readonly List<DLJ_CostCoinSlot> _slots = new List<DLJ_CostCoinSlot>();
    private IDLJ_CostCoinEntranceEffect _entranceEffect;
    private IDLJ_CostCoinSpendEffect _spendEffect;
    private bool _initialized;
    private bool _started;

    public int Capacity => _initialized ? _slots.Count : coins.Count;
    public int FilledCount { get; private set; }

    private Transform SearchRoot => coinRoot != null ? coinRoot : transform;

    private void Awake()
    {
        ResolveDependencies();
        Initialize();
        PrepareInitialCoins();
    }

    private void OnEnable()
    {
        // Start 전에 비활성화됐다 돌아온 경우 OnDisable이 복구한 코인을 다시 분리한다.
        if (_initialized && !_started)
            PrepareInitialCoins();
    }

    private void Start()
    {
        _started = true;
        _entranceEffect?.PlayInitial(FilledCount);
    }

    private void PrepareInitialCoins()
    {
        // 렌더링 첫 프레임 전에 케이스 계층에서 떼어 둬야 케이스를 따라오지 않는다.
        if (_entranceEffect != null && _entranceEffect.PlayOnStart)
            _entranceEffect.PrepareInitialCoins();
    }

    /// <summary>코인 목록과 슬롯의 원래 Transform 상태를 한 번만 구성한다.</summary>
    public void Initialize()
    {
        if (_initialized) return;

        ResolveDependencies();
        RemoveMissingCoins();
        if (coins.Count == 0)
            CollectCoinsInternal();

        _slots.Clear();
        foreach (Transform coin in coins)
            _slots.Add(new DLJ_CostCoinSlot(coin));

        FilledCount = _slots.Count;
        _initialized = true;
        _entranceEffect?.Bind(_slots);

        if (_slots.Count == 0)
            Debug.LogError($"{name}: 이름에 '{coinNameFilter}'이 포함된 코인을 찾지 못했습니다.", this);
    }

    [ContextMenu("Collect Cost Coins")]
    public void CollectCoins()
    {
        if (Application.isPlaying && _initialized)
        {
            Debug.LogWarning($"{name}: 실행 중에는 초기화된 코인 슬롯을 다시 수집할 수 없습니다.", this);
            return;
        }

        CollectCoinsInternal();
    }

    /// <summary>일반적인 행동력 소비로 간주해 표시를 갱신한다.</summary>
    public void SetFilled(int count)
    {
        SetFilled(count, DLJ_CostVisualTransition.Spend);
    }

    /// <summary>앞에서부터 count개의 슬롯을 채우고 지정한 정책으로 상태 변화를 표현한다.</summary>
    public void SetFilled(int count, DLJ_CostVisualTransition transition)
    {
        Initialize();

        count = Mathf.Clamp(count, 0, _slots.Count);
        int previousCount = FilledCount;

        for (int i = 0; i < _slots.Count; i++)
        {
            DLJ_CostCoinSlot slot = _slots[i];
            if (slot == null || !slot.IsValid) continue;

            if (i >= count)
            {
                HideSlot(slot, transition == DLJ_CostVisualTransition.Spend && _started && i < previousCount);
                continue;
            }

            if (!_started && _entranceEffect != null && _entranceEffect.PlayOnStart)
            {
                _spendEffect?.StopAndReset(slot.Coin);
                _entranceEffect.PrepareSlot(slot);
                continue;
            }

            // 이미 채워진 슬롯의 진행 중인 등장 연출은 같은 값 갱신으로 끊지 않는다.
            if (i < previousCount && _entranceEffect != null && _entranceEffect.IsAnimating(slot))
                continue;

            _spendEffect?.StopAndReset(slot.Coin);
            slot.Restore(true);
        }

        FilledCount = count;

        if (!_started || count <= previousCount) return;

        if (transition == DLJ_CostVisualTransition.Immediate || _entranceEffect == null)
        {
            RestoreRangeImmediately(previousCount, count);
            return;
        }

        _entranceEffect.PlayRange(previousCount, count);
    }

    private void HideSlot(DLJ_CostCoinSlot slot, bool playSpendEffect)
    {
        _entranceEffect?.Stop(slot);

        if (playSpendEffect && _spendEffect != null)
        {
            slot.Restore(true);
            if (_spendEffect.Play(slot.Coin, () => slot.Restore(false)))
                return;
        }

        _spendEffect?.StopAndReset(slot.Coin);
        slot.Restore(false);
    }

    private void RestoreRangeImmediately(int startIndex, int endIndex)
    {
        for (int i = startIndex; i < endIndex && i < _slots.Count; i++)
        {
            DLJ_CostCoinSlot slot = _slots[i];
            if (slot == null || !slot.IsValid) continue;

            _entranceEffect?.Stop(slot);
            _spendEffect?.StopAndReset(slot.Coin);
            slot.Restore(true);
        }
    }

    private void ResolveDependencies()
    {
        _entranceEffect = ResolveEffect<IDLJ_CostCoinEntranceEffect>(ref entranceEffectSource);
        _spendEffect = ResolveEffect<IDLJ_CostCoinSpendEffect>(ref spendEffectSource);
    }

    private T ResolveEffect<T>(ref MonoBehaviour source) where T : class
    {
        if (source is T assignedEffect)
            return assignedEffect;

        foreach (MonoBehaviour candidate in GetComponents<MonoBehaviour>())
        {
            if (!(candidate is T effect)) continue;

            source = candidate;
            return effect;
        }

        return null;
    }

    private void CollectCoinsInternal()
    {
        coins.Clear();
        _found.Clear();
        SearchRoot.GetComponentsInChildren(true, _found);

        foreach (Transform candidate in _found)
        {
            if (candidate == null || candidate == SearchRoot) continue;
            if (string.IsNullOrEmpty(coinNameFilter)) continue;
            if (candidate.name.IndexOf(coinNameFilter, StringComparison.OrdinalIgnoreCase) < 0) continue;

            coins.Add(candidate);
        }
    }

    private void RemoveMissingCoins()
    {
        for (int i = coins.Count - 1; i >= 0; i--)
        {
            if (coins[i] == null)
                coins.RemoveAt(i);
        }
    }
}
