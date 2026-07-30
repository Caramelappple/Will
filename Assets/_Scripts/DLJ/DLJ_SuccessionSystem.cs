using _Scripts.LDY;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(LDY_Animal))]
public class DLJ_SuccessionSystem : MonoBehaviour
{
    [SerializeField] private LSO_AnimalSO animalSo;

    [Header("Succession")]
    [FormerlySerializedAs("SuccesionObject")]
    [SerializeField] private GameObject successionObject;

    private static bool isWaitingForSuccessionTarget;
    private static bool isCompletingSuccession;
    private static LDY_Team successionTeam;
    private static int successionHealthBonus;
    private static int successionAttackBonus;
    private static float timeScaleBeforeSuccession = 1f;
    private static DLJ_SuccessionSystem successionSource;

    private LDY_Animal animal;

    public static bool IsWaitingForSuccessionTarget =>
        isWaitingForSuccessionTarget && !isCompletingSuccession;

    public bool ShouldDeferDestruction =>
        isWaitingForSuccessionTarget &&
        successionSource == this;

    private void Awake()
    {
        animal = GetComponent<LDY_Animal>();

        if (successionObject != null)
            successionObject.SetActive(false);
    }

    public void Initialize(LSO_AnimalSO sourceAnimalSo, GameObject effectObject)
    {
        if (sourceAnimalSo != null)
            animalSo = sourceAnimalSo;

        if (effectObject != null)
            successionObject = effectObject;

        if (animal == null)
            animal = GetComponent<LDY_Animal>();

        if (successionObject != null)
            successionObject.SetActive(false);
    }

    public bool Activate()
    {
        if (isWaitingForSuccessionTarget || isCompletingSuccession)
            return false;

        if (animal == null)
            animal = GetComponent<LDY_Animal>();

        if (animal == null)
        {
            Debug.LogError($"{name}: LDY_Animal component is missing.", this);
            return false;
        }

        if (animalSo == null)
        {
            Debug.LogError($"{name}: AnimalSO is missing.", this);
            return false;
        }

        successionSource = this;
        successionTeam = animal.team;
        successionHealthBonus = animalSo.maxHealth;
        successionAttackBonus = animalSo.damage;
        timeScaleBeforeSuccession = Time.timeScale;
        isCompletingSuccession = false;
        isWaitingForSuccessionTarget = true;

        if (successionObject != null)
        {
            successionObject.transform.DOKill();
            successionObject.transform.position = transform.position;
            successionObject.SetActive(true);
        }

        Time.timeScale = 0f;
        Debug.Log("Pick Target");
        return true;
    }

    public static bool TrySelectSuccessionTarget(LDY_Animal target)
    {
        if (!IsWaitingForSuccessionTarget || successionSource == null)
            return false;

        if (target == null || target.IsDead)
        {
            Debug.LogWarning("Invalid succession target.");
            return false;
        }

        if (target.team != successionTeam)
        {
            Debug.LogWarning("Succession target must be on the same team.");
            return false;
        }

        CompleteSuccession(target);
        return true;
    }

    private static void CompleteSuccession(LDY_Animal target)
    {
        isCompletingSuccession = true;

        GameObject effect =
            successionSource != null ? successionSource.successionObject : null;

        if (effect == null)
        {
            ApplySuccession(target);
            return;
        }

        effect.transform.DOMove(target.transform.position, 1f)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .OnComplete(() => ApplySuccession(target));

        Debug.Log("Succession Finished");
    }

    private static void ApplySuccession(LDY_Animal target)
    {
        DLJ_SuccessionSystem sourceToDestroy = successionSource;

        if (target != null && !target.IsDead)
        {
            target.hp += successionHealthBonus;
            target.baseAtk += successionAttackBonus;
        }

        FinishSuccession();

        if (sourceToDestroy != null)
            Destroy(sourceToDestroy.gameObject);
    }

    private static void FinishSuccession()
    {
        if (successionSource != null && successionSource.successionObject != null)
        {
            successionSource.successionObject.transform.DOKill();
            successionSource.successionObject.SetActive(false);
        }

        isWaitingForSuccessionTarget = false;
        isCompletingSuccession = false;
        successionSource = null;
        Time.timeScale = timeScaleBeforeSuccession;
    }

    private void OnDestroy()
    {
        if (successionSource == this)
            FinishSuccession();
    }
}
