using System.Collections;
using _Scripts.LDY;
using _Scripts.LSO.Will;
using DG.Tweening;
using UnityEngine;

/// <summary>Legacy component shim and the public succession-selection entry point.</summary>
[AddComponentMenu("")]
public sealed class DLJ_SuccessionSystem : MonoBehaviour
{
    public static bool IsWaitingForSuccessionTarget =>
        DLJ_SuccessionWill.IsWaitingForSuccessionTarget;

    public static bool TrySelectSuccessionTarget(LDY_Animal target)
    {
        return DLJ_SuccessionWill.TrySelectSuccessionTarget(target);
    }

    public static LSO_IWill Create(DLJ_WillContext context, DLJ_WillDataSO data)
    {
        return new DLJ_SuccessionWill(context, data);
    }
}

internal sealed class DLJ_SuccessionWill : LSO_IWill, DLJ_IDeferredDestruction
{
    private static bool isWaitingForSuccessionTarget;
    private static bool isCompletingSuccession;
    private static LDY_Team successionTeam;
    private static int successionHealthBonus;
    private static int successionAttackBonus;
    private static float timeScaleBeforeSuccession = 1f;
    private static DLJ_SuccessionWill successionSource;

    private readonly LDY_Animal animal;
    private readonly LDY_AttackSystem attackSystem;
    private readonly GameObject effectPrefab;
    private readonly float moveDuration;
    private readonly bool isEnhanced;
    private GameObject effectInstance;
    private bool isWaitingForAttackAnimation;

    internal DLJ_SuccessionWill(DLJ_WillContext context, DLJ_WillDataSO data)
    {
        animal = context.animal;
        attackSystem = context.attackSystem;
        effectPrefab = data.effectPrefab;
        moveDuration = data.moveDuration;
        isEnhanced = DLJ_WillEnhancement.IsActive(animal);
    }

    public static bool IsWaitingForSuccessionTarget =>
        isWaitingForSuccessionTarget && !isCompletingSuccession;

    public bool ShouldDeferDestruction =>
        isWaitingForAttackAnimation ||
        (isWaitingForSuccessionTarget && successionSource == this);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        isWaitingForSuccessionTarget = false;
        isCompletingSuccession = false;
        successionSource = null;
        successionHealthBonus = 0;
        successionAttackBonus = 0;
        timeScaleBeforeSuccession = 1f;
    }

    public void InvokeWill()
    {
        if (attackSystem != null && attackSystem.IsBusy)
        {
            isWaitingForAttackAnimation = true;
            attackSystem.StartCoroutine(WaitForAttackAnimation());
            return;
        }

        Activate();
    }

    private IEnumerator WaitForAttackAnimation()
    {
        yield return new WaitUntil(() => attackSystem == null || !attackSystem.IsBusy);

        isWaitingForAttackAnimation = false;
        if (!Activate() && animal != null)
            Object.Destroy(animal.gameObject);
    }

    public bool Activate()
    {
        if (isWaitingForSuccessionTarget || isCompletingSuccession)
            return false;

        if (animal == null || animal.data == null)
        {
            Debug.LogError("Succession animal data is missing.");
            return false;
        }

        successionSource = this;
        successionTeam = animal.team;
        int sourceHealth = animal.health != null
            ? animal.health.MaxValue
            : animal.data.maxHealth;
        int sourceAttack = animal.baseAtk;
        successionHealthBonus = CalculateInheritedStat(sourceHealth);
        successionAttackBonus = CalculateInheritedStat(sourceAttack);
        timeScaleBeforeSuccession = Time.timeScale;
        isCompletingSuccession = false;
        isWaitingForSuccessionTarget = true;

        if (effectPrefab != null)
        {
            effectInstance = Object.Instantiate(
                effectPrefab,
                animal.transform.position,
                Quaternion.identity);
            effectInstance.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Succession effect prefab is missing.");
        }

        Time.timeScale = 0f;
        Debug.Log("Pick Target");
        return true;
    }

    private int CalculateInheritedStat(int sourceStat)
    {
        int inheritedStat = Mathf.CeilToInt(Mathf.Max(0, sourceStat) / 3f);
        return isEnhanced
            ? Mathf.CeilToInt(inheritedStat * 1.5f)
            : inheritedStat;
    }

    public static bool TrySelectSuccessionTarget(LDY_Animal target)
    {
        if (!IsWaitingForSuccessionTarget || successionSource == null)
            return false;

        if (target == null || target.health == null || target.health.IsDestroyed)
        {
            Debug.LogWarning("Invalid succession target.");
            return false;
        }

        if (target.team != successionTeam)
        {
            Debug.LogWarning("Succession target must be on the same team.");
            return false;
        }

        isCompletingSuccession = true;
        successionSource.MoveEffectAndApply(target);
        return true;
    }

    private void MoveEffectAndApply(LDY_Animal target)
    {
        if (effectInstance == null)
        {
            ApplySuccession(target);
            return;
        }

        effectInstance.transform
            .DOMove(target.transform.position, moveDuration)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .OnComplete(() => ApplySuccession(target));
    }

    private static void ApplySuccession(LDY_Animal target)
    {
        DLJ_SuccessionWill source = successionSource;

        if (target != null && target.health != null && !target.health.IsDestroyed)
        {
            DLJ_SuccessionBonus.Apply(
                target,
                successionHealthBonus,
                successionAttackBonus);
        }

        FinishSuccession();

        if (source != null && source.animal != null)
            Object.Destroy(source.animal.gameObject);

        Debug.Log(
            $"Succession Finished: HP +{successionHealthBonus}, " +
            $"ATK +{successionAttackBonus}");
    }

    private static void FinishSuccession()
    {
        DLJ_SuccessionWill source = successionSource;
        if (source != null && source.effectInstance != null)
        {
            source.effectInstance.transform.DOKill();
            Object.Destroy(source.effectInstance);
            source.effectInstance = null;
        }

        isWaitingForSuccessionTarget = false;
        isCompletingSuccession = false;
        successionSource = null;
        Time.timeScale = timeScaleBeforeSuccession;
    }
}
