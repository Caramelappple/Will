using System;
using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LSO.Boss;
using _Scripts.LSO.HealthSystem;
using UnityEngine;

public enum DLJ_GreedEffectType
{
    Attack,
    MaxHealth
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

    public event Action<int> OnStolenResourcesChanged;
    public event Action<int> OnGreedChanged;
    internal event Action<int> OnGreedMilestoneEvaluationRequested;

    public void Gain(int stolenAmount, int greedAmount)
    {
        if (stolenAmount > 0)
        {
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
}
