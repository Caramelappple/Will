using _Scripts.LDY;
using _Scripts.LSO;
using _Scripts.LSO.Will;
using UnityEngine;

/// <summary>Legacy component shim. Runtime wills no longer use animal components.</summary>
[AddComponentMenu("")]
public sealed class DLJ_SacrificeSystem : MonoBehaviour
{
    public static LSO_IWill Create(DLJ_WillContext context, DLJ_WillDataSO data)
    {
        if (data is not DLJ_SacrificeWillDataSO sacrificeData)
        {
            Debug.LogError($"Sacrifice requires {nameof(DLJ_SacrificeWillDataSO)}.", data);
            return null;
        }

        return new DLJ_SacrificeWill(context.animal, context.board, sacrificeData);
    }
}

internal sealed class DLJ_SacrificeWill : LSO_IWill
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
    private readonly LDY_Animal owner;
    private readonly LDY_BoardManager board;
    private readonly int healthBonus;
    private readonly DLJ_SacrificeWillDataSO data;
    private readonly DLJ_IWillEffect effect = new DLJ_SacrificeEffect();

    internal DLJ_SacrificeWill(
        LDY_Animal sourceOwner,
        LDY_BoardManager sourceBoard,
        DLJ_SacrificeWillDataSO sourceData)
    {
        owner = sourceOwner;
        board = sourceBoard;
        healthBonus = DLJ_WillEnhancement.IsActive(owner) ? 2 : StatBonus;
        data = sourceData;
    }

    public void InvokeWill()
    {
        if (owner == null || board == null)
        {
            Debug.LogError("Sacrifice dependencies are missing.");
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

            int increasedHealth = ally.health.Value + healthBonus;
            ally.health.Init(ally.health.MaxValue + healthBonus, false);
            ally.health.Value = increasedHealth;
            ally.baseAtk += StatBonus;
            buffedCount++;
        }

        Debug.Log($"Sacrifice buffed {buffedCount} adjacent allies.");

        if (buffedCount > 0)
            DLJ_WillBenefitEvents.Raise(owner, LSO_WillType.Sacrifice);

        GameObject effectObject = data.effectPrefab != null
            ? Object.Instantiate(
                data.effectPrefab,
                owner.transform.position,
                Quaternion.identity)
            : null;
        effect.Play(
            effectObject,
            new DLJ_WillEffectContext
            {
                data = data,
                owner = owner.gameObject,
                origin = owner.transform.position
            });
    }
}
