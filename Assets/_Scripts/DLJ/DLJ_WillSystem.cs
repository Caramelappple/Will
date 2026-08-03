using _Scripts.LDY;
using _Scripts.LSO;
using _Scripts.LSO.Will;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(LDY_Animal))]
public class DLJ_WillSystem : MonoBehaviour, DLJ_IWillActivation
{
    [SerializeField] private LSO_AnimalSO animalSo;
    [SerializeField] private LDY_BoardManager board;
    [SerializeField] private LDY_TurnManager turnManager;
    [SerializeField] private LDY_AttackSystem attackSystem;

    [FormerlySerializedAs("Will")]
    [SerializeField] private LSO_WillType willType;

    [Header("Rage")]
    [SerializeField] private GameObject rageObject;
    [SerializeField] private float rageExpandTime = 0.25f;
    [SerializeField] private float rageHoldTime = 0.3f;
    [SerializeField] private float effectHeight = 0.12f;

    [Header("Curse")]
    [SerializeField] private GameObject curseObject;
    [SerializeField] private float curseExpandTime = 0.25f;
    [SerializeField] private float curseEffectHeight = 0.12f;

    [Header("Succession")]
    [FormerlySerializedAs("SuccesionObject")]
    [SerializeField] private GameObject successionObject;

    private LSO_IWill currentWill;

    public bool ShouldDeferDestruction =>
        currentWill != null &&
        currentWill.ShouldDeferDestruction;

    private void Awake()
    {
        LDY_Animal animal = GetComponent<LDY_Animal>();

        if (board == null)
            board = FindFirstObjectByType<LDY_BoardManager>();

        DLJ_WillContext context = new DLJ_WillContext();
        context.owner = gameObject;
        context.animal = animal;
        context.animalSo = animalSo;
        context.board = board;
        context.turnManager = turnManager;
        context.attackSystem = attackSystem;
        context.rageObject = rageObject;
        context.rageExpandTime = rageExpandTime;
        context.rageHoldTime = rageHoldTime;
        context.effectHeight = effectHeight;
        context.curseObject = curseObject;
        context.curseExpandTime = curseExpandTime;
        context.curseEffectHeight = curseEffectHeight;
        context.successionObject = successionObject;

        currentWill = LSO_WillFactory.Create(willType, context);
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
