using _Scripts.LDY;
using _Scripts.LSO.Will;
using UnityEngine;

public class DLJ_ContractSystem : MonoBehaviour, LSO_IWill
{
    private int unitCost;
    private LDY_Team ownerTeam;
    private DLJ_ContractRefund _refund;

    public static LSO_IWill Create(
        DLJ_WillContext context,
        DLJ_WillData data)
    {
        DLJ_ContractSystem system =
            context.owner.GetComponent<DLJ_ContractSystem>();

        if (system == null)
            system = context.owner.AddComponent<DLJ_ContractSystem>();

        int cost = context.animal != null && context.animal.data != null
            ? context.animal.data.cost
            : 0;

        DLJ_ContractRefund service =
            DLJ_ContractRefund.GetOrCreate(
                context.actionPoints,
                context.turnManager);

        system.Configure(
            cost,
            context.animal != null ? context.animal.team : LDY_Team.Player,
            service);

        return system;
    }

    public void Configure(
        int sourceUnitCost,
        LDY_Team sourceOwnerTeam,
        DLJ_ContractRefund sourceRefund)
    {
        unitCost = Mathf.Max(0, sourceUnitCost);
        ownerTeam = sourceOwnerTeam;
        _refund = sourceRefund;
    }

    public void InvokeWill()
    {
        if (ownerTeam != LDY_Team.Player)
            return;

        if (_refund == null)
        {
            Debug.LogError($"{name}: Contract cost receiver is missing.", this);
            return;
        }

        int refund = unitCost / 2;
        _refund.QueueRefund(refund);
    }
}
