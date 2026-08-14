using _Scripts.LDY;
using _Scripts.LSO;
using _Scripts.LSO.Ability;
using _Scripts.LSO.DeathSystem;
using _Scripts.LSO.HealthSystem.Data;
using UnityEngine;

/// <summary>Accumulates 1 summon cost per survived turn, up to 3, and refunds it on death.</summary>
public sealed class DLJ_CostRefund : LSO_IAbility, IOnTurnStart, LSO_IOnDeath,
    LSO_IAbilityInitializable
{
    private const int MaxStoredCost = 3;

    private LSO_AbilityContext context;
    private LDY_ActionPointManager actionPoints;
    private DLJ_ContractRefund refundService;
    private int storedCost;
    private bool hasQueuedRefund;

    public void Initialize(LSO_AbilityContext abilityContext)
    {
        context = abilityContext;
        actionPoints = LDY_ActionPointManager.instance;
        refundService = CreateRefundService();
        storedCost = 0;
        hasQueuedRefund = false;

        LDY_Animal owner = context?.Owner;
        if (owner != null && owner.health != null)
            owner.health.OnDamage += HandleDamage;
    }

    public void OnTurnStart(LDY_Team team)
    {
        LDY_Animal owner = context?.Owner;
        if (owner == null || owner.team != team)
            return;

        if (owner.health == null || owner.health.IsDestroyed)
            return;

        if (storedCost >= MaxStoredCost)
            return;

        storedCost++;

        Debug.Log(
            $"<color=yellow>{owner.name}: Cost Refund stored {storedCost}/{MaxStoredCost}.</color>",
            owner);
    }

    public void OnDeath(LDY_Animal self, LDY_Animal killer)
    {
        QueueRefund(self);
    }

    private void HandleDamage(DamageResultData damage)
    {
        LDY_Animal owner = context?.Owner;
        if (owner != null && owner.health != null && owner.health.IsDestroyed)
            QueueRefund(owner);
    }

    private void QueueRefund(LDY_Animal owner)
    {
        if (hasQueuedRefund || storedCost <= 0)
            return;

        if (owner == null || owner.team != LDY_Team.Player)
            return;

        if (refundService == null)
            refundService = CreateRefundService();

        if (refundService == null)
        {
            Debug.LogError("Cost Refund could not create the AP refund queue.", owner);
            return;
        }

        hasQueuedRefund = true;
        refundService.QueueRefund(storedCost);

        Debug.Log(
            $"<color=yellow>{owner.name}: Cost Refund queued {storedCost} AP " +
            "for the next player turn.</color>",
            owner);
    }

    private DLJ_ContractRefund CreateRefundService()
    {
        if (actionPoints == null)
            actionPoints = LDY_ActionPointManager.instance;

        LDY_TurnManager turnManager = GameManager.HasInstance
            ? GameManager.Instance.TurnManager
            : null;

        return DLJ_ContractRefund.GetOrCreate(actionPoints, turnManager);
    }
}
