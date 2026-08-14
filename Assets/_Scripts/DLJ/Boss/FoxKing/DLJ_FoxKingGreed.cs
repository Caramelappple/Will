using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LSO.Ability;
using _Scripts.LSO.HealthSystem;

/// <summary>직렬화된 탐욕 마일스톤을 순서와 무관하게 한 번씩 적용한다.</summary>
public sealed class DLJ_FoxKingGreed : LSO_IAbility, LSO_IAbilityInitializable
{
    private readonly HashSet<DLJ_GreedMilestone> applied = new();
    private LDY_Animal owner;
    private DLJ_FoxKingBoss state;
    private Health health;

    public void Initialize(LSO_AbilityContext context)
    {
        owner = context?.Owner;
        state = owner != null ? owner.GetComponent<DLJ_FoxKingBoss>() : null;
        health = owner != null ? owner.health : null;
        if (state != null)
            state.OnGreedMilestoneEvaluationRequested += ApplyMilestones;
    }

    private void ApplyMilestones(int greed)
    {
        if (state == null || owner == null)
            return;

        foreach (DLJ_GreedMilestone milestone in state.GreedMilestones)
        {
            if (milestone == null || greed < milestone.threshold || !applied.Add(milestone))
                continue;

            if (milestone.effect == DLJ_GreedEffectType.Attack)
            {
                owner.baseAtk += milestone.amount;
                continue;
            }

            if (milestone.effect == DLJ_GreedEffectType.MaxHealth && health != null)
            {
                health.Init(health.MaxValue + milestone.amount, false);
                health.Value += milestone.amount;
            }
        }
    }
}
