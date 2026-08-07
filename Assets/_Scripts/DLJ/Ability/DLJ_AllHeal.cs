using _Scripts.HealthSystem;
using _Scripts.LDY;
using _Scripts.LSO;
using _Scripts.LSO.Ability;
using UnityEngine;

/// <summary>Each attack has a 10% chance to heal every living ally by 1.</summary>
public sealed class DLJ_AllHeal : LSO_IAbility, IOnAnimalAttack,
    LSO_IAbilityInitializable
{
    private const float HealChance = 0.1f;
    private const int HealAmount = 1;

    private LSO_AbilityContext context;

    public void Initialize(LSO_AbilityContext abilityContext)
    {
        context = abilityContext;
    }

    public void OnAttack(LSO_AnimalSO animal)
    {
        LDY_Animal owner = context?.Owner;
        LDY_BoardManager board = context?.Board;

        if (owner == null || board == null)
            return;

        if (owner.health == null || owner.health.IsDestroyed)
            return;

        if (Random.value >= HealChance)
            return;

        foreach (LDY_Animal ally in board.GetAllByTeam(owner.team))
        {
            if (ally == null || ally.health == null || ally.health.IsDestroyed)
                continue;

            ally.health.Recover(RecoverData.Create(owner.health, HealAmount));
        }

        Debug.Log(
            $"<color=cyan>{owner.name}: All Heal activated. All allies recovered {HealAmount} HP.</color>",
            owner);
    }
}
