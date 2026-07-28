using _Scripts.LDY;
using UnityEngine;

public class DLJ_RageSystem : MonoBehaviour
{

    [SerializeField] private int damage = 1;
    [SerializeField] private int range = 1;
    
    private LDY_BoardManager boardManager;
    private Vector3Int center;
    private LDY_AttackSystem attackSystem;

    
    private LDY_TurnManager turnManager;
    
    public void Initialize(LDY_TurnManager turnManager, LDY_BoardManager boardManager, Vector3Int center, LDY_AttackSystem attackSystem)
    {
        this.turnManager = turnManager;
        this.boardManager = boardManager;
        this.center = center;
        this.attackSystem = attackSystem;
        

        
        DamageAnimalsInArea();
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

                target.hp -= damage;
                
                if  (target.hp <= 0)
                    attackSystem.HandleDeath(target);
            }
        }
        Debug.Log("damaged");
    }
}
