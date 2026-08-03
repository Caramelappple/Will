using System;
using _Scripts.LDY;
using _Scripts.LSO.Will;
using UnityEngine;

public class DLJ_CurseSystem : MonoBehaviour, LSO_IWill
{
    [Header("Curse")]
    [SerializeField] private int remainingTurn = 2;
    [SerializeField] private int damage = 1;
    [SerializeField] private int range = 1;

    private LDY_TurnManager activationTurnManager;
    private LDY_BoardManager activationBoard;
    private LDY_AttackSystem activationAttackSystem;
    private LDY_Team activationSourceTeam;

    private LDY_TurnManager effectTurnManager;
    private LDY_BoardManager effectBoard;
    private Vector3Int effectCenter;
    private LDY_AttackSystem effectAttackSystem;
    private LDY_Team effectSourceTeam;

    public int RemainingTurn { get; private set; }
    public bool ShouldDeferDestruction => false;

    public event Action<Vector3, Vector3, Action<DLJ_CurseSystem>> OnCurseActivated;

    public static LSO_IWill Create(DLJ_WillContext context)
    {
        DLJ_CurseSystem system =
            context.owner.GetComponent<DLJ_CurseSystem>();

        if (system == null)
            system = context.owner.AddComponent<DLJ_CurseSystem>();

        system.Configure(
            context.turnManager,
            context.board,
            context.attackSystem,
            context.animal.team);

        DLJ_CurseEffect effect = context.owner.GetComponent<DLJ_CurseEffect>();
        if (effect == null)
            effect = context.owner.AddComponent<DLJ_CurseEffect>();

        effect.Bind(
            system,
            context.curseObject,
            context.curseExpandTime,
            context.curseEffectHeight);

        return system;
    }

    public void InvokeWill()
    {
        Activate();
    }

    public void Configure(
        LDY_TurnManager turnManager,
        LDY_BoardManager boardManager,
        LDY_AttackSystem attackSystem,
        LDY_Team sourceTeam)
    {
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

        OnCurseActivated?.Invoke(
            centerWorld,
            areaSize,
            effectSystem => effectSystem.InitializeEffect(
                activationTurnManager,
                activationBoard,
                center,
                activationAttackSystem,
                activationSourceTeam));

        Debug.Log("Curse Activated");
        return true;
    }

    private void InitializeEffect(
        LDY_TurnManager turnManager,
        LDY_BoardManager boardManager,
        Vector3Int center,
        LDY_AttackSystem attackSystem,
        LDY_Team sourceTeam)
    {
        effectTurnManager = turnManager;
        effectBoard = boardManager;
        effectCenter = center;
        effectAttackSystem = attackSystem;
        effectSourceTeam = sourceTeam;
        RemainingTurn = remainingTurn;

        effectTurnManager.OnTurnChanged += HandleTurnChanged;
        DamageAnimalsInArea();
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

    private void HandleTurnChanged(LDY_Team team)
    {
        DamageAnimalsInArea();
        RemainingTurn--;

        if (RemainingTurn <= 0)
            Expire();
    }

    private void DamageAnimalsInArea()
    {
        for (int x = -range; x <= range; x++)
        {
            for (int z = -range; z <= range; z++)
            {
                Vector3Int tile = effectCenter + new Vector3Int(x, 0, z);

                if (!effectBoard.IsInside(tile))
                    continue;

                LDY_Animal target = effectBoard.Get(tile);

                if (target == null ||
                    target.health == null ||
                    target.health.IsDestroyed)
                    continue;

                if (effectSourceTeam == target.team)
                    continue;

                DamageData damageData = DamageData.Create(null, damage);
                target.health.GetDamage(damageData);

                if (target.health.IsDestroyed)
                    effectAttackSystem.HandleDeath(target);
            }
        }
    }

    private void Expire()
    {
        Unsubscribe();
        Destroy(gameObject);
    }

    private void Unsubscribe()
    {
        if (effectTurnManager != null)
            effectTurnManager.OnTurnChanged -= HandleTurnChanged;

        effectTurnManager = null;
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }
}
