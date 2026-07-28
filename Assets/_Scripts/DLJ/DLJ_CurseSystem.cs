using System;
using _Scripts.LDY;
using UnityEngine;

public class DLJ_CurseSystem : MonoBehaviour
{
    [SerializeField] private int remainingTurn = 2;

    [SerializeField] private int damage = 1;
    [SerializeField] private int range = 1;
    
    private LDY_BoardManager boardManager;
    private Vector3Int center;
    private LDY_AttackSystem attackSystem;

    public int RemainingTurn { get; private set; }
    
    private LDY_TurnManager turnManager;
    
    public void Initialize(LDY_TurnManager turnManager, LDY_BoardManager boardManager, Vector3Int center, LDY_AttackSystem attackSystem)
    {
        this.turnManager = turnManager;
        this.boardManager = boardManager;
        this.center = center;
        this.attackSystem = attackSystem;
        
        RemainingTurn = remainingTurn;

        this.turnManager.OnTurnChanged += HandleTurnChanged;
        
        DamageAnimalsInArea();
    }

    private void HandleTurnChanged(LDY_Team obj)
    {
        DamageAnimalsInArea();
        
        RemainingTurn--;
        
        if  (RemainingTurn <= 0)
            Expire();
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
    
    private void DamageAnimalsInArea()
    {
        for (int x = -range; x <= range; x++)
        {
            for (int z = -range; z <= range; z++)
            {
                Vector3Int tile = center + new Vector3Int(x, 0, z);

                if (!boardManager.IsInside(tile))
                    continue;

                LDY_Animal target = boardManager.Get(tile);
                
                if (target == null || target.IsDead)
                    continue;
                
                Debug.Log(target.name);
                
                LDY_Animal anim = target.GetComponent<LDY_Animal>();

                if (anim.team == target.team)
                    return;

                target.hp -= damage;
                
                if  (target.hp <= 0)
                    attackSystem.HandleDeath(target);
            }
        }
        Debug.Log("damaged");
    }
}
