using _Scripts.LDY;
using _Scripts.LSO.Will;
using UnityEngine;

/// <summary>Legacy component shim. Runtime wills no longer use animal components.</summary>
[AddComponentMenu("")]
public sealed class DLJ_CurseSystem : MonoBehaviour
{
    public static LSO_IWill Create(DLJ_WillContext context, DLJ_WillDataSO data)
    {
        if (data is not DLJ_CurseWillDataSO curseData)
        {
            Debug.LogError($"Curse requires {nameof(DLJ_CurseWillDataSO)}.", data);
            return null;
        }

        return new DLJ_CurseWill(context, curseData);
    }
}

internal sealed class DLJ_CurseWill : LSO_IWill
{
    private readonly LDY_Animal owner;
    private readonly LDY_TurnManager turnManager;
    private readonly LDY_BoardManager board;
    private readonly LDY_AttackSystem attackSystem;
    private readonly DLJ_CurseWillDataSO data;
    private readonly int duration;
    private readonly DLJ_IWillEffect effect = new DLJ_CurseEffect();

    internal DLJ_CurseWill(DLJ_WillContext context, DLJ_CurseWillDataSO sourceData)
    {
        owner = context.animal;
        turnManager = context.turnManager;
        board = context.board;
        attackSystem = context.attackSystem;
        data = sourceData;
        duration = DLJ_WillEnhancement.IsActive(owner) ? 3 : sourceData.duration;
    }

    public void InvokeWill()
    {
        if (owner == null || turnManager == null || board == null || attackSystem == null)
        {
            Debug.LogError("Curse dependencies are missing.");
            return;
        }

        Vector3Int center = owner.pos;
        if (!board.IsInside(center))
        {
            Debug.LogError("Curse owner is outside the board.");
            return;
        }

        Vector3 centerWorld = board.GridToWorld(center);
        Vector3 verticalWorld = board.GridToWorld(center + new Vector3Int(0, 0, 1));
        Vector3 horizontalWorld = board.GridToWorld(center + new Vector3Int(1, 0, 0));
        float diameter = data.range * 2f + 1f;

        DLJ_CurseActivationData activation = new DLJ_CurseActivationData
        {
            duration = duration,
            damage = data.damage,
            range = data.range,
            center = center,
            centerWorld = centerWorld,
            areaSize = new Vector3(
                Vector3.Distance(centerWorld, verticalWorld) * diameter,
                0f,
                Vector3.Distance(centerWorld, horizontalWorld) * diameter),
            sourceTeam = owner.team,
            turnManager = turnManager,
            board = board,
            attackSystem = attackSystem
        };

        GameObject effectInstance = data.effectPrefab != null
            ? Object.Instantiate(data.effectPrefab, centerWorld, Quaternion.identity)
            : null;
        effect.Play(
            effectInstance,
            new DLJ_WillEffectContext
            {
                data = data,
                owner = owner.gameObject,
                origin = centerWorld,
                areaSize = activation.areaSize
            });

        GameObject zoneObject = new GameObject("Curse Zone");
        zoneObject.transform.position = centerWorld;
        zoneObject.AddComponent<DLJ_CurseZone>().Initialize(
            activation,
            effectInstance);
        Debug.Log("Curse Activated");
    }
}
