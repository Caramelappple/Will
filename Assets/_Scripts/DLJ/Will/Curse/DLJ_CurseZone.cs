using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LSO.HealthSystem;
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
    private GameObject effectInstance;
    private readonly HashSet<LDY_Animal> animalsInside = new();
    private readonly HashSet<LDY_Animal> currentAnimalsInside = new();

    public int RemainingTurn { get; private set; }

    public void Initialize(
        DLJ_CurseActivationData data,
        GameObject visualInstance = null)
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
        effectInstance = visualInstance;

        turnManager.OnTurnChanged += HandleTurnChanged;
        DamageAnimalsInArea();
        RecordCurrentOccupants();
    }

    private void Update()
    {
        DamageNewEntrants();
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

                DamageAnimal(target);
            }
        }
    }

    private void DamageNewEntrants()
    {
        currentAnimalsInside.Clear();

        for (int x = -range; x <= range; x++)
        {
            for (int z = -range; z <= range; z++)
            {
                Vector3Int tile = center + new Vector3Int(x, 0, z);
                if (!board.IsInside(tile))
                    continue;

                LDY_Animal target = board.Get(tile);
                if (!IsValidTarget(target))
                    continue;

                currentAnimalsInside.Add(target);

                if (!animalsInside.Contains(target))
                    DamageAnimal(target);
            }
        }

        animalsInside.RemoveWhere(animal =>
            animal == null || !currentAnimalsInside.Contains(animal));

        foreach (LDY_Animal animal in currentAnimalsInside)
        {
            if (animal != null && animal.health != null && !animal.health.IsDestroyed)
                animalsInside.Add(animal);
        }
    }

    private void RecordCurrentOccupants()
    {
        animalsInside.Clear();

        for (int x = -range; x <= range; x++)
        {
            for (int z = -range; z <= range; z++)
            {
                Vector3Int tile = center + new Vector3Int(x, 0, z);
                if (!board.IsInside(tile))
                    continue;

                LDY_Animal target = board.Get(tile);
                if (IsValidTarget(target))
                    animalsInside.Add(target);
            }
        }
    }

    private bool IsValidTarget(LDY_Animal target)
    {
        return target != null &&
               target.health != null &&
               !target.health.IsDestroyed &&
               target.team != sourceTeam;
    }

    private void DamageAnimal(LDY_Animal target)
    {
        DamageData damageData = DamageData.Create(
            null,
            damage,
            LSO_DamageSource.Curse);
        target.health.GetDamage(damageData);

        if (target.health.IsDestroyed)
            attackSystem.HandleDeath(target);
    }

    private void Expire()
    {
        Unsubscribe();
        if (effectInstance != null)
            Destroy(effectInstance);
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
        if (effectInstance != null)
            Destroy(effectInstance);
    }
}
