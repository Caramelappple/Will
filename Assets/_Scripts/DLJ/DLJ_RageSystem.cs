using System;
using _Scripts.LDY;
using _Scripts.LSO.Will;
using UnityEngine;

public class DLJ_RageSystem : MonoBehaviour, LSO_IWill
{
    [Header("Rage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private int range = 1;

    private LDY_BoardManager activationBoard;
    private LDY_AttackSystem activationAttackSystem;

    public bool ShouldDeferDestruction => false;

    public event Action<Vector3, Vector3> OnRageActivated;

    public static LSO_IWill Create(DLJ_WillContext context)
    {
        DLJ_RageSystem system =
            context.owner.GetComponent<DLJ_RageSystem>();

        if (system == null)
            system = context.owner.AddComponent<DLJ_RageSystem>();

        system.Configure(context.board, context.attackSystem);

        DLJ_RageEffect effect = context.owner.GetComponent<DLJ_RageEffect>();
        if (effect == null)
            effect = context.owner.AddComponent<DLJ_RageEffect>();

        effect.Bind(
            system,
            context.rageObject,
            context.rageExpandTime,
            context.rageHoldTime,
            context.effectHeight);

        return system;
    }

    public void InvokeWill()
    {
        Activate();
    }

    public void Configure(
        LDY_BoardManager boardManager,
        LDY_AttackSystem attackSystem)
    {
        activationBoard = boardManager;
        activationAttackSystem = attackSystem;
    }

    public bool Activate()
    {
        if (!TryGetActivationData(
                out Vector3Int center,
                out Vector3 centerWorld,
                out Vector3 areaSize))
            return false;

        DamageAnimalsInArea(center);
        OnRageActivated?.Invoke(centerWorld, areaSize);

        Debug.Log("Rage Activated");
        return true;
    }

    private bool TryGetActivationData(
        out Vector3Int center,
        out Vector3 centerWorld,
        out Vector3 areaSize)
    {
        center = default;
        centerWorld = default;
        areaSize = default;

        if (activationBoard == null || activationAttackSystem == null)
        {
            Debug.LogError($"{name}: Rage dependencies are missing.", this);
            return false;
        }

        center = activationBoard.WorldToGrid(transform.position);

        if (!activationBoard.IsInside(center))
        {
            Debug.LogError($"{name}: Animal is outside the board.", this);
            return false;
        }

        centerWorld = activationBoard.GridToWorld(center);
        Vector3 verticalWorld =
            activationBoard.GridToWorld(center + new Vector3Int(0, 0, 1));
        Vector3 horizontalWorld =
            activationBoard.GridToWorld(center + new Vector3Int(1, 0, 0));

        float cellWidth = Vector3.Distance(centerWorld, verticalWorld);
        float cellDepth = Vector3.Distance(centerWorld, horizontalWorld);
        float diameter = range * 2f + 1f;
        areaSize = new Vector3(cellWidth * diameter, 0f, cellDepth * diameter);
        return true;
    }

    private void DamageAnimalsInArea(Vector3Int center)
    {
        for (int x = -range; x <= range; x++)
        {
            for (int z = -range; z <= range; z++)
            {
                Vector3Int tile = center + new Vector3Int(x, 0, z);

                if (!activationBoard.IsInside(tile))
                    continue;

                LDY_Animal target = activationBoard.Get(tile);

                if (target == null ||
                    target.health == null ||
                    target.health.IsDestroyed)
                    continue;

                DamageData damageData = DamageData.Create(null, damage);
                target.health.GetDamage(damageData);

                if (target.health.IsDestroyed)
                    activationAttackSystem.HandleDeath(target);
            }
        }
    }
}
