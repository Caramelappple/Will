using System;
using UnityEngine;

public class DLJ_PlayerHealth : MonoBehaviour
{
    public const int CandleCount = 3;
    public const int MaxHealthPerCandle = 100;

    public static DLJ_PlayerHealth Instance { get; private set; }

    [SerializeField]
    private int[] candleHealth =
    {
        MaxHealthPerCandle,
        MaxHealthPerCandle,
        MaxHealthPerCandle
    };

    public event Action<int, int> OnCandleHealthChanged;
    public event Action<int> OnCandleExtinguished;
    public event Action OnPlayerDeath;

    public int TotalHealth
    {
        get
        {
            int total = 0;

            for (int i = 0; i < candleHealth.Length; i++)
                total += candleHealth[i];

            return total;
        }
    }
    
    public bool IsDead => TotalHealth <= 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ValidateHealth();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public int GetCandleHealth(int index)
    {
        if (!IsValidIndex(index))
            return 0;

        return candleHealth[index];
    }

    /// <summary>
    /// 첫 번째 초부터 순서대로 피해를 적용한다.
    /// 한 초의 남은 체력보다 피해가 크면 나머지 피해는 다음 초로 넘어간다.
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (damage <= 0 || IsDead)
            return;

        int remainingDamage = damage;

        for (int i = 0; i < CandleCount && remainingDamage > 0; i++)
        {
            if (candleHealth[i] <= 0)
                continue;

            int previousHealth = candleHealth[i];
            int appliedDamage = Mathf.Min(previousHealth, remainingDamage);

            candleHealth[i] -= appliedDamage;
            remainingDamage -= appliedDamage;

            OnCandleHealthChanged?.Invoke(i, candleHealth[i]);

            if (previousHealth > 0 && candleHealth[i] == 0)
                OnCandleExtinguished?.Invoke(i);
        }

        if (IsDead)
            OnPlayerDeath?.Invoke();
    }

    /// <summary>
    /// 다음 스테이지로 넘어갈 때 아직 켜져 있는 초만 최대 체력으로 회복한다.
    /// 이미 꺼진 초는 0을 유지한다.
    /// </summary>
    public void RecoverForNextStage()
    {
        for (int i = 0; i < CandleCount; i++)
        {
            if (candleHealth[i] <= 0 || candleHealth[i] == MaxHealthPerCandle)
                continue;

            candleHealth[i] = MaxHealthPerCandle;
            OnCandleHealthChanged?.Invoke(i, candleHealth[i]);
        }
    }

    /// <summary>새 게임을 시작할 때 세 초를 모두 되살린다.</summary>
    public void ResetForNewRun()
    {
        for (int i = 0; i < CandleCount; i++)
        {
            candleHealth[i] = MaxHealthPerCandle;
            OnCandleHealthChanged?.Invoke(i, candleHealth[i]);
        }
    }

    /// <summary>세이브 데이터에서 초 체력을 복원한다.</summary>
    public bool TryLoadHealth(int[] savedHealth)
    {
        if (savedHealth == null || savedHealth.Length != CandleCount)
            return false;

        for (int i = 0; i < CandleCount; i++)
        {
            candleHealth[i] = Mathf.Clamp(savedHealth[i], 0, MaxHealthPerCandle);
            OnCandleHealthChanged?.Invoke(i, candleHealth[i]);
        }

        return true;
    }

    /// <summary>외부에서 배열을 바꾸지 못하도록 복사본을 반환한다.</summary>
    public int[] GetHealthSaveData()
    {
        return (int[])candleHealth.Clone();
    }

    /// <summary>UI가 구독한 직후 현재 상태를 한 번에 갱신할 때 사용한다.</summary>
    public void NotifyCurrentHealth()
    {
        for (int i = 0; i < CandleCount; i++)
            OnCandleHealthChanged?.Invoke(i, candleHealth[i]);
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < CandleCount;
    }

    private void ValidateHealth()
    {
        if (candleHealth == null || candleHealth.Length != CandleCount)
        {
            candleHealth = new int[CandleCount];

            for (int i = 0; i < CandleCount; i++)
                candleHealth[i] = MaxHealthPerCandle;

            return;
        }

        for (int i = 0; i < CandleCount; i++)
            candleHealth[i] = Mathf.Clamp(candleHealth[i], 0, MaxHealthPerCandle);
    }
}
