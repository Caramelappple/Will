using System;
using _Scripts.LDY;
using _Scripts.LSO.Will;
using UnityEngine;

public class DLJ_CurseSystem : MonoBehaviour, LSO_IWill
{
    private int remainingTurn;
    private int damage;
    private int range;

    private LDY_TurnManager activationTurnManager;
    private LDY_BoardManager activationBoard;
    private LDY_AttackSystem activationAttackSystem;
    private LDY_Team activationSourceTeam;

    public event Action<DLJ_CurseActivationData> OnCurseActivated;

    public static LSO_IWill Create(
        DLJ_WillContext context,
        DLJ_WillData data)
    {
        DLJ_CurseSystem system =
            context.owner.GetComponent<DLJ_CurseSystem>();

        if (system == null)
            system = context.owner.AddComponent<DLJ_CurseSystem>();

        system.Configure(
            data.duration,
            data.damage,
            data.range,
            context.turnManager,
            context.board,
            context.attackSystem,
            context.animal.team);

        DLJ_CurseEffect effect = context.owner.GetComponent<DLJ_CurseEffect>();
        if (effect == null)
            effect = context.owner.AddComponent<DLJ_CurseEffect>();

        effect.Bind(
            system,
            data.effectPrefab,
            data.expandTime,
            data.effectHeight);
        
        return system;
    }

    public void InvokeWill()
    {
        Activate();
    }

    public void Configure(
        int sourceDuration,
        int sourceDamage,
        int sourceRange,
        LDY_TurnManager turnManager,
        LDY_BoardManager boardManager,
        LDY_AttackSystem attackSystem,
        LDY_Team sourceTeam)
    {
        remainingTurn = sourceDuration;
        damage = sourceDamage;
        range = sourceRange;
        activationTurnManager = turnManager;
        activationBoard = boardManager;
        activationAttackSystem = attackSystem;
        activationSourceTeam = sourceTeam;
    }

    public bool Activate()
    {
        if (!TryGetActivationData(
                out Vector3Int center,
                out Vector3 centerWorld,
                out Vector3 areaSize))
            return false;

        DLJ_CurseActivationData activationData =
            new DLJ_CurseActivationData();
        activationData.duration = remainingTurn;
        activationData.damage = damage;
        activationData.range = range;
        activationData.center = center;
        activationData.centerWorld = centerWorld;
        activationData.areaSize = areaSize;
        activationData.sourceTeam = activationSourceTeam;
        activationData.turnManager = activationTurnManager;
        activationData.board = activationBoard;
        activationData.attackSystem = activationAttackSystem;

        OnCurseActivated?.Invoke(activationData);

        Debug.Log("Curse Activated");
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

        if (activationTurnManager == null ||
            activationBoard == null ||
            activationAttackSystem == null)
        {
            Debug.LogError($"{name}: Curse dependencies are missing.", this);
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

}
