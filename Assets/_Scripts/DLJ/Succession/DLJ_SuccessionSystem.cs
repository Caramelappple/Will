using System;
using _Scripts.HealthSystem;
using _Scripts.LDY;
using _Scripts.LSO.Will;
using UnityEngine;

[RequireComponent(typeof(LDY_Animal))]
public class DLJ_SuccessionSystem : MonoBehaviour, LSO_IWill, DLJ_IDeferredDestruction
{
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

    public event Action<Vector3> OnSelectionStarted;
    public event Action<Vector3, Action> OnTargetSelected;
    public event Action OnSuccessionFinished;

    public static LSO_IWill Create(
        DLJ_WillContext context,
        DLJ_WillData data)
    {
        DLJ_SuccessionSystem system =
            context.owner.GetComponent<DLJ_SuccessionSystem>();

        if (system == null)
            system = context.owner.AddComponent<DLJ_SuccessionSystem>();

        system.Initialize();

        DLJ_SuccessionEffect effect =
            context.owner.GetComponent<DLJ_SuccessionEffect>();
        if (effect == null)
            effect = context.owner.AddComponent<DLJ_SuccessionEffect>();

        effect.Bind(system, data.effectPrefab, data.moveDuration);

        return system;
    }

    public void InvokeWill()
    {
        Activate();
    }

    private void Awake()
    {
        animal = GetComponent<LDY_Animal>();
    }

    public void Initialize()
    {
        if (animal == null)
            animal = GetComponent<LDY_Animal>();
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

        if (animal.data == null)
        {
            Debug.LogError($"{name}: Animal data is missing.", this);
            return false;
        }

        successionSource = this;
        successionTeam = animal.team;
        successionHealthBonus = animal.data.maxHealth;
        successionAttackBonus = animal.data.damage;
        timeScaleBeforeSuccession = Time.timeScale;
        isCompletingSuccession = false;
        isWaitingForSuccessionTarget = true;

        OnSelectionStarted?.Invoke(transform.position);

        Time.timeScale = 0f;
        Debug.Log("Pick Target");
        return true;
    }

    public static bool TrySelectSuccessionTarget(LDY_Animal target)
    {
        if (!IsWaitingForSuccessionTarget || successionSource == null)
            return false;

        if (target == null ||
            target.health == null ||
            target.health.IsDestroyed)
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

        if (successionSource.OnTargetSelected == null)
        {
            ApplySuccession(target);
            return;
        }

        successionSource.OnTargetSelected.Invoke(
            target.transform.position,
            () => ApplySuccession(target));

        Debug.Log("Succession Finished");
    }

    private static void ApplySuccession(LDY_Animal target)
    {
        DLJ_SuccessionSystem sourceToDestroy = successionSource;

        if (target != null &&
            target.health != null &&
            !target.health.IsDestroyed)
        {
            RecoverData recoverData =
                RecoverData.Create(null, successionHealthBonus);
            target.health.Recover(recoverData);
            target.baseAtk += successionAttackBonus;
        }

        FinishSuccession();

        if (sourceToDestroy != null)
            Destroy(sourceToDestroy.gameObject);
    }

    private static void FinishSuccession()
    {
        successionSource?.OnSuccessionFinished?.Invoke();

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
