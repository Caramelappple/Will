using _Scripts.LDY;
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
    [SerializeField] private WillType willType;

    [Header("Rage")]
    [SerializeField] private GameObject rageObject;
    [SerializeField] private float rageExpandTime = 0.25f;
    [SerializeField] private float rageHoldTime = 0.3f;
    [SerializeField] private float effectHeight = 0.12f;
    [SerializeField] private DLJ_RageSystem rageSystem;

    [Header("Curse")]
    [SerializeField] private GameObject curseObject;
    [SerializeField] private float curseExpandTime = 0.25f;
    [SerializeField] private float curseEffectHeight = 0.12f;
    [SerializeField] private DLJ_CurseSystem curseSystem;

    [Header("Succession")]
    [FormerlySerializedAs("SuccesionObject")]
    [SerializeField] private GameObject successionObject;
    [SerializeField] private DLJ_SuccessionSystem successionSystem;

    public bool ShouldDeferDestruction =>
        willType == WillType.Succession &&
        successionSystem != null &&
        successionSystem.ShouldDeferDestruction;

    private void Awake()
    {
        LDY_Animal animal = GetComponent<LDY_Animal>();

        if (board == null)
            board = FindFirstObjectByType<LDY_BoardManager>();

        switch (willType)
        {
            case WillType.Curse:
                if (curseSystem == null)
                    curseSystem = GetComponent<DLJ_CurseSystem>();

                if (curseSystem == null)
                    curseSystem = gameObject.AddComponent<DLJ_CurseSystem>();

                curseSystem.Configure(
                    curseObject,
                    curseExpandTime,
                    curseEffectHeight,
                    turnManager,
                    board,
                    attackSystem,
                    animal.team);
                break;

            case WillType.Rage:
                if (rageSystem == null)
                    rageSystem = GetComponent<DLJ_RageSystem>();

                if (rageSystem == null)
                    rageSystem = gameObject.AddComponent<DLJ_RageSystem>();

                rageSystem.Configure(
                    rageObject,
                    rageExpandTime,
                    rageHoldTime,
                    effectHeight,
                    board,
                    attackSystem);
                break;

            case WillType.Succession:
                if (successionSystem == null)
                    successionSystem = GetComponent<DLJ_SuccessionSystem>();

                if (successionSystem == null)
                    successionSystem = gameObject.AddComponent<DLJ_SuccessionSystem>();

                successionSystem.Initialize(animalSo, successionObject);
                break;
        }
    }

    public void WillActivate()
    {
        switch (willType)
        {
            case WillType.Curse:
                curseSystem?.Activate();
                break;

            case WillType.Rage:
                rageSystem?.Activate();
                break;

            case WillType.Succession:
                successionSystem?.Activate();
                break;
        }
    }

}
