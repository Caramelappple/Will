using _Scripts.LDY;
using _Scripts.LSO;
using _Scripts.LSO.Will;
using UnityEngine;

/// <summary>
/// Legacy component kept only so existing scenes do not gain a missing-script entry.
/// LDY_DeathHandler now invokes wills directly from LDY_Animal.WillType.
/// </summary>
[AddComponentMenu("")]
public sealed class DLJ_WillSystem : MonoBehaviour
{
}

/// <summary>Creates and invokes a will directly from an animal's WillType.</summary>
public static class DLJ_WillRuntime
{
    private const string DatabasePath = "DLJ/DLJ_WillDatabase";
    private static DLJ_WillDatabaseSO database;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        database = null;
    }

    public static LSO_IWill Invoke(LDY_Animal animal, LDY_BoardManager knownBoard = null)
    {
        if (animal == null)
            return null;

        DLJ_WillDatabaseSO willDatabase = GetDatabase();
        if (willDatabase == null)
            return null;

        DLJ_WillData data = willDatabase.Get(animal.WillType);
        if (data == null)
            return null;

        LDY_TurnManager turnManager = FindTurnManager();
        LDY_AttackSystem attackSystem = Object.FindFirstObjectByType<LDY_AttackSystem>();
        LDY_ActionPointManager actionPoints = turnManager != null
            ? turnManager.ActionPoints
            : null;
        if (actionPoints == null && attackSystem != null)
            actionPoints = attackSystem.ActionPoints;
        if (actionPoints == null)
            actionPoints = Object.FindFirstObjectByType<LDY_ActionPointManager>();

        DLJ_WillContext context = new DLJ_WillContext
        {
            owner = animal.gameObject,
            animal = animal,
            board = knownBoard != null ? knownBoard : FindBoard(),
            turnManager = turnManager,
            attackSystem = attackSystem,
            actionPoints = actionPoints
        };

        LSO_IWill will = LSO_WillFactory.Create(animal.WillType, context, data);
        will?.InvokeWill();
        return will;
    }

    private static DLJ_WillDatabaseSO GetDatabase()
    {
        if (database == null)
            database = Resources.Load<DLJ_WillDatabaseSO>(DatabasePath);

        if (database == null)
            Debug.LogError($"Will database is missing at Resources/{DatabasePath}.");

        return database;
    }

    private static LDY_BoardManager FindBoard()
    {
        if (GameManager.HasInstance && GameManager.Instance.Board != null)
            return GameManager.Instance.Board;

        return Object.FindFirstObjectByType<LDY_BoardManager>();
    }

    private static LDY_TurnManager FindTurnManager()
    {
        if (GameManager.HasInstance && GameManager.Instance.TurnManager != null)
            return GameManager.Instance.TurnManager;

        return Object.FindFirstObjectByType<LDY_TurnManager>();
    }
}
