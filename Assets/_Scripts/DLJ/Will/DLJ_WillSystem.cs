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

    public static LSO_IWill Invoke(
        LDY_Animal animal,
        LDY_BoardManager legacyBoard = null)
    {
        if (animal == null)
            return null;

        // Inherited stats last for the receiver's lifetime, but must not be
        // passed on again or remain on its deferred corpse.
        DLJ_SuccessionBonus.RemoveFrom(animal);

        DLJ_WillDatabaseSO willDatabase = GetDatabase();
        if (willDatabase == null)
            return null;

        DLJ_WillDataSO data = willDatabase.Get(animal.WillType);
        if (data == null)
            return null;

        if (!GameManager.HasInstance)
        {
            Debug.LogError("GameManager is missing. Will cannot be invoked.");
            return null;
        }

        GameManager gameManager = GameManager.Instance;
        LDY_TurnManager turnManager = gameManager.TurnManager;
        LDY_AttackSystem attackSystem =
            Object.FindFirstObjectByType<LDY_AttackSystem>();
        LDY_ActionPointManager actionPoints =
            Object.FindFirstObjectByType<LDY_ActionPointManager>();

        DLJ_WillContext context = new DLJ_WillContext
        {
            owner = animal.gameObject,
            animal = animal,
            board = gameManager.Board,
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

}
