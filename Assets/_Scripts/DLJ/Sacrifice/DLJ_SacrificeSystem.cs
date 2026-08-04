using _Scripts.LDY;
using _Scripts.LSO.Will;
using UnityEngine;

/// <summary>
/// 희생: 죽은 기물 주변 8칸의 아군 기물에게 최대/현재 체력과 공격력을 1씩 준다.
/// </summary>
public class DLJ_SacrificeSystem : MonoBehaviour, LSO_IWill
{
    private static readonly Vector3Int[] Directions =
    {
        new Vector3Int(0, 0, 1),
        new Vector3Int(0, 0, -1),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(1, 0, 0),
        new Vector3Int(1, 0, 1),
        new Vector3Int(1, 0, -1),
        new Vector3Int(-1, 0, 1),
        new Vector3Int(-1, 0, -1),
    };

    private const int StatBonus = 1;

    private LDY_Animal owner;
    private LDY_BoardManager board;

    public static LSO_IWill Create(
        DLJ_WillContext context,
        DLJ_WillData data)
    {
        DLJ_SacrificeSystem system =
            context.owner.GetComponent<DLJ_SacrificeSystem>();

        if (system == null)
            system = context.owner.AddComponent<DLJ_SacrificeSystem>();

        system.Configure(context.animal, context.board);
        return system;
    }

    public void Configure(
        LDY_Animal sourceOwner,
        LDY_BoardManager sourceBoard)
    {
        owner = sourceOwner;
        board = sourceBoard;
    }

    public void InvokeWill()
    {
        if (owner == null || board == null)
        {
            Debug.LogError($"{name}: Sacrifice dependencies are missing.", this);
            return;
        }

        int buffedCount = 0;

        foreach (Vector3Int direction in Directions)
        {
            Vector3Int tile = new Vector3Int(
                owner.pos.x + direction.x,
                0,
                owner.pos.z + direction.z);

            if (!board.IsInside(tile))
                continue;

            LDY_Animal ally = board.Get(tile);
            if (ally == null || ally == owner || ally.team != owner.team)
                continue;
            if (ally.health == null || ally.health.IsDestroyed)
                continue;

            IncreaseStats(ally);
            buffedCount++;
        }

        Debug.Log($"{name}: Sacrifice buffed {buffedCount} adjacent allies.", this);
    }

    private static void IncreaseStats(LDY_Animal ally)
    {
        int increasedHealth = ally.health.Value + StatBonus;

        ally.health.Init(ally.health.MaxValue + StatBonus, false);
        ally.health.Value = increasedHealth;
        ally.baseAtk += StatBonus;
    }
}
