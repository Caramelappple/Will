using _Scripts.LDY;
using UnityEngine;

public class DLJ_CurseZone : MonoBehaviour
{
    private int damage;
    private int range;
    private LDY_TurnManager turnManager;
    private LDY_BoardManager board;
    private LDY_AttackSystem attackSystem;
    private LDY_Team sourceTeam;
    private Vector3Int center;

    public int RemainingTurn { get; private set; }

    public void Initialize(DLJ_CurseActivationData data)
    {
        if (data == null ||
            data.turnManager == null ||
            data.board == null ||
            data.attackSystem == null)
        {
            Debug.LogError($"{name}: Curse zone data is missing.", this);
            Destroy(gameObject);
            return;
        }

        RemainingTurn = data.duration;
        damage = data.damage;
        range = data.range;
        turnManager = data.turnManager;
        board = data.board;
        attackSystem = data.attackSystem;
        sourceTeam = data.sourceTeam;
        center = data.center;

        turnManager.OnTurnChanged += HandleTurnChanged;
        DamageAnimalsInArea();
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
                Vector3Int tile = center + new Vector3Int(x, 0, z);

                if (!board.IsInside(tile))
                    continue;

                LDY_Animal target = board.Get(tile);

                if (target == null ||
                    target.health == null ||
                    target.health.IsDestroyed ||
                    target.team == sourceTeam)
                    continue;

                DamageData damageData = DamageData.Create(null, damage);
                target.health.GetDamage(damageData);

                if (target.health.IsDestroyed)
                    attackSystem.HandleDeath(target);
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
        if (turnManager != null)
            turnManager.OnTurnChanged -= HandleTurnChanged;

        turnManager = null;
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }
}
