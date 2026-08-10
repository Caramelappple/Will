using _Scripts.LDY;
using _Scripts.LSO.Will;
using UnityEngine;

/// <summary>Legacy component shim. Runtime wills no longer use animal components.</summary>
[AddComponentMenu("")]
public sealed class DLJ_ContractSystem : MonoBehaviour
{
    public static LSO_IWill Create(DLJ_WillContext context, DLJ_WillDataSO data)
    {
        return new DLJ_ContractWill(context, data);
    }
}

internal sealed class DLJ_ContractWill : LSO_IWill
{
    private readonly int unitCost;
    private readonly LDY_Team ownerTeam;
    private readonly DLJ_ContractRefund refundService;
    private readonly bool isEnhanced;
    private readonly GameObject owner;
    private readonly DLJ_WillDataSO data;
    private readonly DLJ_IWillEffect effect = new DLJ_ContractEffect();

    internal DLJ_ContractWill(DLJ_WillContext context, DLJ_WillDataSO sourceData)
    {
        unitCost = context.animal != null && context.animal.data != null
            ? Mathf.Max(0, context.animal.data.cost)
            : 0;
        ownerTeam = context.animal != null ? context.animal.team : LDY_Team.Player;
        isEnhanced = DLJ_WillEnhancement.IsActive(context.animal);
        refundService = DLJ_ContractRefund.GetOrCreate(
            context.actionPoints,
            context.turnManager);
        owner = context.owner;
        data = sourceData;
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

        int refundAmount = isEnhanced
            ? unitCost
            : Mathf.CeilToInt(unitCost / 2f);
        refundService.QueueRefund(refundAmount);

        Vector3 effectPosition = owner != null
            ? owner.transform.position
            : Vector3.zero;
        GameObject effectObject = data.effectPrefab != null
            ? Object.Instantiate(data.effectPrefab, effectPosition, Quaternion.identity)
            : null;
        effect.Play(
            effectObject,
            new DLJ_WillEffectContext
            {
                data = data,
                owner = owner,
                origin = effectPosition
            });
    }
}
