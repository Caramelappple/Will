using System.Collections;
using _Scripts.LDY;
using _Scripts.LSO.Will;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>계승 연출 동안 Cinemachine 카메라를 스플라인 위로 이동시키고 추적 대상을 교체한다.</summary>
internal sealed class DLJ_SuccessionCameraState
{
    private readonly CinemachineCamera splineCamera;
    private readonly CinemachineSplineDolly splineDolly;
    private readonly CinemachineBrain cinemachineBrain;
    private readonly int originalPriority;
    private readonly float originalSplinePosition;
    private readonly bool originalIgnoreTimeScale;
    private readonly Transform effectTransform;
    private readonly float trackingSwitchPoint;

    private Tween cameraTween;
    private bool isTrackingEffect;

    private DLJ_SuccessionCameraState(
        CinemachineCamera splineCamera,
        CinemachineSplineDolly splineDolly,
        CinemachineBrain cinemachineBrain,
        Transform source,
        Transform effectTransform,
        DLJ_SuccessionEffectSO visual)
    {
        this.splineCamera = splineCamera;
        this.splineDolly = splineDolly;
        this.cinemachineBrain = cinemachineBrain;
        this.effectTransform = effectTransform;

        originalPriority = splineCamera.Priority.Value;
        originalSplinePosition = splineDolly.CameraPosition;
        originalIgnoreTimeScale = cinemachineBrain != null && cinemachineBrain.IgnoreTimeScale;
        trackingSwitchPoint = visual != null
            ? visual.successionCameraTrackingSwitchPoint
            : 0.5f;

        if (cinemachineBrain != null)
            cinemachineBrain.IgnoreTimeScale = true;

        splineCamera.enabled = true;
        splineCamera.Priority = Mathf.Max(100, originalPriority + 100);
        SetTrackingTarget(source);

        splineDolly.PositionUnits = UnityEngine.Splines.PathIndexUnit.Normalized;
        splineDolly.CameraPosition = 0f;
        float startDelay = visual != null
            ? Mathf.Max(0f, visual.successionCameraStartDelay)
            : 0f;
        float moveDuration = visual != null
            ? Mathf.Max(0f, visual.successionCameraMoveDuration)
            : 1f;
        PlaySpline(moveDuration, startDelay);
    }

    public static DLJ_SuccessionCameraState Begin(
        Transform source,
        Transform effectTransform,
        DLJ_SuccessionEffectSO visual)
    {
        if (source == null || visual == null || !visual.successionCameraEnabled)
            return null;

        CinemachineSplineDolly dolly = FindSplineDolly();
        CinemachineCamera camera = dolly != null
            ? dolly.GetComponent<CinemachineCamera>()
            : null;

        if (dolly == null || camera == null)
        {
            Debug.LogWarning(
                "Succession camera requires a Cinemachine Camera with Spline Dolly.");
            return null;
        }

        CinemachineBrain brain = Object.FindFirstObjectByType<CinemachineBrain>();
        return new DLJ_SuccessionCameraState(
            camera,
            dolly,
            brain,
            source,
            effectTransform,
            visual);
    }

    public static void PrepareForGameplay()
    {
        CinemachineSplineDolly dolly = FindSplineDolly();
        CinemachineCamera camera = dolly != null
            ? dolly.GetComponent<CinemachineCamera>()
            : null;

        if (camera == null)
            return;

        camera.Target.TrackingTarget = null;
        camera.Target.LookAtTarget = null;
        camera.Target.CustomLookAtTarget = false;
        camera.enabled = false;
        dolly.CameraPosition = 0f;
    }

    private static CinemachineSplineDolly FindSplineDolly()
    {
        CinemachineSplineDolly[] dollies = Object.FindObjectsByType<CinemachineSplineDolly>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (CinemachineSplineDolly dolly in dollies)
        {
            if (dolly != null && dolly.name == "CinemachineCamera (1)")
                return dolly;
        }

        return dollies.Length > 0 ? dollies[0] : null;
    }

    private void PlaySpline(float duration, float startDelay)
    {
        cameraTween = DOTween.Sequence()
            .SetUpdate(true)
            .AppendInterval(startDelay)
            .Append(DOVirtual.Float(0f, 1f, Mathf.Max(0f, duration), progress =>
                {
                    if (splineDolly == null)
                        return;

                    splineDolly.CameraPosition = progress;
                    if (!isTrackingEffect && progress >= trackingSwitchPoint)
                    {
                        isTrackingEffect = true;
                        SetTrackingTarget(effectTransform);
                    }
                })
                .SetEase(Ease.InOutSine));
    }

    private void SetTrackingTarget(Transform target)
    {
        splineCamera.Target.TrackingTarget = target;
        splineCamera.Target.LookAtTarget = null;
        splineCamera.Target.CustomLookAtTarget = false;
    }

    public void Restore()
    {
        cameraTween?.Kill(false);

        if (splineCamera != null)
        {
            splineCamera.Target.TrackingTarget = null;
            splineCamera.Target.LookAtTarget = null;
            splineCamera.Target.CustomLookAtTarget = false;
            splineCamera.Priority = originalPriority;
            splineCamera.enabled = false;
        }

        if (splineDolly != null)
            splineDolly.CameraPosition = originalSplinePosition;

        if (cinemachineBrain != null)
            cinemachineBrain.IgnoreTimeScale = originalIgnoreTimeScale;
    }
}


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
        if (data is not DLJ_SuccessionWillDataSO successionData)
        {
            Debug.LogError($"Succession requires {nameof(DLJ_SuccessionWillDataSO)}.", data);
            return null;
        }

        DLJ_SuccessionCameraState.PrepareForGameplay();
        return new DLJ_SuccessionWill(context, successionData);
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
    private readonly DLJ_SuccessionWillDataSO data;
    private readonly bool isEnhanced;
    private readonly DLJ_IWillEffect effect = new DLJ_SuccessionEffect();
    private GameObject effectInstance;
    private DLJ_SuccessionCameraState cameraState;
    private bool hasInvoked;
    private bool isWaitingForAttackAnimation;

    internal DLJ_SuccessionWill(DLJ_WillContext context, DLJ_SuccessionWillDataSO data)
    {
        animal = context.animal;
        attackSystem = context.attackSystem;
        this.data = data;
        effectPrefab = data.effectPrefab;
        isEnhanced = DLJ_WillEnhancement.IsActive(animal);
    }

    public static bool IsWaitingForSuccessionTarget =>
        isWaitingForSuccessionTarget && !isCompletingSuccession;

    public bool ShouldDeferDestruction =>
        !hasInvoked || isWaitingForAttackAnimation ||
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
        hasInvoked = true;

        if (attackSystem != null && attackSystem.IsBusy)
        {
            isWaitingForAttackAnimation = true;
            attackSystem.StartCoroutine(WaitForAttackAnimation());
            return;
        }

        if (!Activate() && animal != null)
            Object.Destroy(animal.gameObject);
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

        effectInstance = effectPrefab != null
            ? Object.Instantiate(
                effectPrefab,
                animal.transform.position,
                Quaternion.identity)
            : new GameObject("Succession Effect Origin");
        effectInstance.transform.position = animal.transform.position;
        effectInstance.SetActive(true);

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
        cameraState = DLJ_SuccessionCameraState.Begin(
            animal != null ? animal.transform : null,
            effectInstance != null ? effectInstance.transform : null,
            data.successionEffect);

        PlayEffectAndApply(target);
    }

    private void PlayEffectAndApply(LDY_Animal target)
    {
        effect.Play(
            effectInstance,
            new DLJ_WillEffectContext
            {
                data = data,
                owner = animal != null ? animal.gameObject : null,
                target = target.gameObject,
                origin = animal != null ? animal.transform.position : Vector3.zero,
                targetPosition = target.transform.position
            },
            () => ApplySuccession(target));
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
        source?.cameraState?.Restore();
        if (source != null && source.effectInstance != null)
        {
            Object.Destroy(source.effectInstance);
            source.effectInstance = null;
        }

        isWaitingForSuccessionTarget = false;
        isCompletingSuccession = false;
        successionSource = null;
        Time.timeScale = timeScaleBeforeSuccession;
    }
}
