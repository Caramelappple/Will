using _Scripts.LDY;
using _Scripts.LSO.HealthSystem;
using _Scripts.LSO.Will;
using UnityEngine;

/// <summary>Legacy component shim. Runtime wills no longer use animal components.</summary>
[AddComponentMenu("")]
public sealed class DLJ_RageSystem : MonoBehaviour
{
    public static LSO_IWill Create(DLJ_WillContext context, DLJ_WillData data)
    {
        return new DLJ_RageWill(context, data);
    }
}

internal sealed class DLJ_RageWill : LSO_IWill
{
    private readonly LDY_Animal owner;
    private readonly LDY_BoardManager board;
    private readonly LDY_AttackSystem attackSystem;
    private readonly DLJ_WillData data;

    internal DLJ_RageWill(DLJ_WillContext context, DLJ_WillData sourceData)
    {
        owner = context.animal;
        board = context.board;
        attackSystem = context.attackSystem;
        data = sourceData;
    }

    public void InvokeWill()
    {
        if (owner == null || board == null || attackSystem == null)
        {
            Debug.LogError("Rage dependencies are missing.");
            return;
        }

        Vector3Int center = owner.pos;
        if (!board.IsInside(center))
        {
            Debug.LogError("Rage owner is outside the board.");
            return;
        }

        DamageAnimalsInArea(center);

        Vector3 centerWorld = board.GridToWorld(center);
        Vector3 verticalWorld = board.GridToWorld(center + new Vector3Int(0, 0, 1));
        Vector3 horizontalWorld = board.GridToWorld(center + new Vector3Int(1, 0, 0));
        float diameter = data.range * 2f + 1f;
        Vector3 areaSize = new Vector3(
            Vector3.Distance(centerWorld, verticalWorld) * diameter,
            0f,
            Vector3.Distance(centerWorld, horizontalWorld) * diameter);

        DLJ_RageEffect.Play(
            centerWorld,
            areaSize,
            data.effectPrefab,
            data.expandTime,
            data.holdTime,
            data.effectHeight);
        Debug.Log("Rage Activated");
    }

    private void DamageAnimalsInArea(Vector3Int center)
    {
        for (int x = -data.range; x <= data.range; x++)
        for (int z = -data.range; z <= data.range; z++)
        {
            Vector3Int tile = center + new Vector3Int(x, 0, z);
            if (!board.IsInside(tile))
                continue;

            LDY_Animal target = board.Get(tile);
            if (target == null || target.health == null || target.health.IsDestroyed)
                continue;

            target.health.GetDamage(DamageData.Create(null, data.damage));
            if (target.health.IsDestroyed)
                attackSystem.HandleDeath(target);
        }
    }
}
