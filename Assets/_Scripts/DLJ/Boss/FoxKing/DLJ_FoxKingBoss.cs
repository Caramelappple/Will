using System;
using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LSO.Boss;
using _Scripts.LSO.HealthSystem;
using UnityEngine;
using _Scripts.LSO.Reward;

public enum DLJ_GreedEffectType
{
    Attack,
    MaxHealth
}

public enum DLJ_InvestmentEffectType
{
    Heal,
    Attack
}

public readonly struct DLJ_FoxKingInvestmentEntry
{
    public int Cost { get; }
    public DLJ_InvestmentEffectType Effect { get; }
    public int Amount { get; }

    public DLJ_FoxKingInvestmentEntry(int cost, DLJ_InvestmentEffectType effect, int amount)
    {
        Cost = cost;
        Effect = effect;
        Amount = amount;
    }
}

[Serializable]
public sealed class DLJ_GreedMilestone
{
    [Min(1)] public int threshold = 5;
    public DLJ_GreedEffectType effect;
    [Min(1)] public int amount = 1;
}

/// <summary>여우왕의 런타임 자원과 조정 가능한 탐욕 마일스톤 설정을 보관한다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(LDY_Animal), typeof(Health), typeof(LSO_BossPhase))]
public sealed class DLJ_FoxKingBoss : MonoBehaviour
{
    [Header("Greed Milestones")]
    [SerializeField] private List<DLJ_GreedMilestone> greedMilestones = new()
    {
        new DLJ_GreedMilestone { threshold = 5, effect = DLJ_GreedEffectType.Attack, amount = 1 },
        new DLJ_GreedMilestone { threshold = 10, effect = DLJ_GreedEffectType.Attack, amount = 1 },
        new DLJ_GreedMilestone { threshold = 15, effect = DLJ_GreedEffectType.MaxHealth, amount = 5 }
    };

    public int StolenResources { get; private set; }
    public int Greed { get; private set; }
    public int PendingAttackBonus { get; internal set; }
    public int Phase => GetComponent<LSO_BossPhase>()?.CurrentPhase ?? 1;
    public IReadOnlyList<DLJ_GreedMilestone> GreedMilestones => greedMilestones;
    public IReadOnlyList<DLJ_FoxKingInvestmentEntry> ActiveInvestments => activeInvestments;

    public event Action<int> OnStolenResourcesChanged;
    public event Action<int> OnGreedChanged;
    public event Action<DLJ_FoxKingInvestmentEntry> OnInvestmentMade;
    public event Action OnInvestmentTurnAdvanced;
    public event Action OnAttackInvestmentConsumed;
    internal event Action<int> OnGreedMilestoneEvaluationRequested;

    private readonly List<DLJ_FoxKingInvestmentEntry> activeInvestments = new();

    public void Gain(int stolenAmount, int greedAmount)
    {
        if (stolenAmount > 0)
        {
            _Scripts.LSO.Sound.LSO_GameAudio.Play(_Scripts.LSO.Sound.LSO_SoundCue.FoxGain);
            StolenResources += stolenAmount;
            OnStolenResourcesChanged?.Invoke(StolenResources);
        }

        if (greedAmount > 0)
        {
            Greed += greedAmount;

            OnGreedChanged?.Invoke(Greed);
            OnGreedMilestoneEvaluationRequested?.Invoke(Greed);
        }
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0 || StolenResources < amount)
            return false;

        StolenResources -= amount;
        OnStolenResourcesChanged?.Invoke(StolenResources);
        return true;
    }

    internal void ReportInvestment(int cost, DLJ_InvestmentEffectType effect, int amount)
    {
        if (cost <= 0 || amount <= 0)
            return;

        var entry = new DLJ_FoxKingInvestmentEntry(cost, effect, amount);
        activeInvestments.Add(entry);
        OnInvestmentMade?.Invoke(entry);
    }

    internal void AdvanceInvestmentTurn()
    {
        activeInvestments.RemoveAll(entry => entry.Effect != DLJ_InvestmentEffectType.Attack);
        OnInvestmentTurnAdvanced?.Invoke();
    }

    internal int ConsumePendingAttackBonus()
    {
        int bonus = PendingAttackBonus;
        if (bonus <= 0)
            return 0;

        PendingAttackBonus = 0;
        activeInvestments.RemoveAll(entry => entry.Effect == DLJ_InvestmentEffectType.Attack);
        OnAttackInvestmentConsumed?.Invoke();
        return bonus;
    }
}
