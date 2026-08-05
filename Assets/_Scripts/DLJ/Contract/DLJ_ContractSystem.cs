using _Scripts.LDY;
using _Scripts.LSO.Will;
using UnityEngine;

/// <summary>Legacy component shim. Runtime wills no longer use animal components.</summary>
[AddComponentMenu("")]
public sealed class DLJ_ContractSystem : MonoBehaviour
{
    public static LSO_IWill Create(DLJ_WillContext context, DLJ_WillData data)
    {
        return new DLJ_ContractWill(context);
    }
}

internal sealed class DLJ_ContractWill : LSO_IWill
{
    private readonly int unitCost;
    private readonly LDY_Team ownerTeam;
    private readonly DLJ_ContractRefund refundService;

    internal DLJ_ContractWill(DLJ_WillContext context)
    {
        unitCost = context.animal != null && context.animal.data != null
            ? Mathf.Max(0, context.animal.data.cost)
            : 0;
        ownerTeam = context.animal != null ? context.animal.team : LDY_Team.Player;
        refundService = DLJ_ContractRefund.GetOrCreate(
            context.actionPoints,
            context.turnManager);
    }

    public void InvokeWill()
    {
        if (ownerTeam != LDY_Team.Player)
            return;

        if (refundService == null)
        {
            Debug.LogError("Contract action point receiver is missing.");
            return;
        }

        refundService.QueueRefund(Mathf.CeilToInt(unitCost / 2f));
    }
}
