using _Scripts.LDY;
using _Scripts.LSO;
using _Scripts.LSO.Will;
using UnityEngine;

[RequireComponent(typeof(LDY_Animal))]
public class DLJ_WillSystem : MonoBehaviour, DLJ_IWillActivation
{
    [SerializeField] private LDY_BoardManager board;
    [SerializeField] private LDY_TurnManager turnManager;
    [SerializeField] private LDY_AttackSystem attackSystem;
    [SerializeField] private LDY_ActionPointManager actionPoints;
    [SerializeField] private DLJ_WillDatabaseSO willDatabase;

    private LSO_IWill currentWill;

    public bool ShouldDeferDestruction =>
        currentWill is DLJ_IDeferredDestruction deferredDestruction &&
        deferredDestruction.ShouldDeferDestruction;

    private void Awake()
    {
        //if (board == null)
            //board = FindFirstObjectByType<LDY_BoardManager>();
            board = GameManager.Instance.Board;
        //if (turnManager == null)
            //turnManager = FindFirstObjectByType<LDY_TurnManager>();
            turnManager = GameManager.Instance.TurnManager;
        if (attackSystem == null)
           attackSystem = FindFirstObjectByType<LDY_AttackSystem>();
        if (actionPoints == null)
            actionPoints = FindFirstObjectByType<LDY_ActionPointManager>();
    }

    private void Start()
    {
        LDY_Animal animal = GetComponent<LDY_Animal>();

        DLJ_WillContext context = new DLJ_WillContext();
        context.owner = gameObject;
        context.animal = animal;
        context.board = board;
        context.turnManager = turnManager;
        context.attackSystem = attackSystem;
        context.actionPoints = actionPoints;
        DLJ_WillData willData =
            willDatabase != null ? willDatabase.Get(animal.WillType) : null;

        currentWill = LSO_WillFactory.Create(
            animal.WillType,
            context,
            willData);
    }

    public void WillActivate()
    {
        if (currentWill == null)
        {
            Debug.LogError($"{name}: Will is not initialized.", this);
            return;
        }

        currentWill.InvokeWill();
    }
}
