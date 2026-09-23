using System.Collections;
using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LSO.Ability;
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

    /// <summary>
    /// 선택 대기 중 마지막 후보가 사라졌다면 계승을 취소하고 정지 상태를 푼다.
    /// 후보 생존 여부를 감시하는 쪽은 LDY_SuccessionResolver이고,
    /// 실제 계승 상태 정리는 이 시스템이 계속 소유한다.
    /// </summary>
    public static bool TryCancelIfNoSuccessionTarget()
    {
        return DLJ_SuccessionWill.TryCancelIfNoValidTarget();
    }

    /// <summary>
    /// 전투 종료가 확정된 뒤에도 대상 선택을 기다리고 있다면 계승을 취소한다.
    /// </summary>
    public static bool TryCancelIfBattleOver()
    {
        return DLJ_SuccessionWill.TryCancelIfBattleOver();
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
    private readonly LDY_BoardManager board;
    private readonly LDY_AttackSystem attackSystem;
    private readonly GameObject effectPrefab;
    private readonly DLJ_SuccessionWillDataSO data;
    private readonly DLJ_StatIncreaseEffectSO statIncreaseEffect;
    private readonly bool isEnhanced;
    private readonly DLJ_IWillEffect effect = new DLJ_SuccessionEffect();
    private GameObject effectInstance;
    private DLJ_SuccessionCameraState cameraState;
    private bool hasInvoked;
    private bool isWaitingForAttackAnimation;

    internal DLJ_SuccessionWill(DLJ_WillContext context, DLJ_SuccessionWillDataSO data)
    {
        animal = context.animal;
        board = context.board;
        attackSystem = context.attackSystem;
        this.data = data;
        effectPrefab = data.effectPrefab;
        statIncreaseEffect = context.statIncreaseEffect;
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

        // 마지막 적 사망 등으로 전투 종료가 먼저 확정됐다면 계승 선택을 열지 않는다.
        if (SkipIfBattleOver())
            return;

        // 이미 받을 기물이 죽었다면 공격 연출이 끝날 때까지 사망을 유예할 이유가 없다.
        // 여기서 먼저 끝내야 ShouldDeferDestruction이 false를 돌려 정상 사망 처리가 이어진다.
        if (SkipIfNoValidTarget())
            return;

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

        // 공격 연출을 기다리는 동안 클리어가 확정될 수 있으므로 진입 직전 다시 검사한다.
        if (SkipIfBattleOver())
            return false;

        // 받을 같은 팀 생존 기물이 없으면 선택 대기와 시간 정지를 시작하지 않는다.
        // 공격 연출을 기다린 뒤 여기 도착한 경우에는 사망 유예 기록이 이미 남아 있으므로
        // 이 기물의 기록만 제거해 다음 계승의 팀 판정을 오염시키지 않게 한다.
        if (SkipIfNoValidTarget())
            return false;

        successionSource = this;
        successionTeam = animal.team;
        int sourceHealth = ResolveSuccessionHealth(animal);
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

        DLJ_SuccessionNotify.ShowPrompt();

        Time.timeScale = 0f;
        Debug.Log("Pick Target");
        return true;
    }

    private bool HasValidTarget()
    {
        if (board != null)
        {
            List<LDY_Animal> teammates = board.GetAllByTeam(animal.team);

            for (int i = 0; i < teammates.Count; i++)
                if (IsValidTarget(teammates[i])) return true;

            return false;
        }

        // 보드 참조가 빠진 테스트 씬에서도 같은 규칙으로 판단한다.
        LDY_Animal[] animals = Object.FindObjectsByType<LDY_Animal>(FindObjectsSortMode.None);
        for (int i = 0; i < animals.Length; i++)
            if (IsValidTarget(animals[i])) return true;

        return false;
    }

    private bool SkipIfNoValidTarget()
    {
        if (HasValidTarget()) return false;

        // 공격 연출을 기다린 뒤 검사한 경우에는 이미 유예 목록에 들어가 있을 수 있다.
        LDY_DeferredDeaths.Remove(animal);
        Debug.Log($"Succession skipped: {animal.name}의 계승을 받을 아군이 없습니다.", animal);
        return true;
    }

    private bool SkipIfBattleOver()
    {
        if (!KTH_GameEndManager.IsBattleOver) return false;

        // 공격 연출을 기다린 뒤 검사한 경우에는 이미 유예 목록에 들어가 있을 수 있다.
        LDY_DeferredDeaths.Remove(animal);
        Debug.Log($"Succession skipped: 전투 종료가 확정되어 {animal.name}의 계승을 건너뜁니다.", animal);
        return true;
    }

    private bool IsValidTarget(LDY_Animal target)
    {
        return target != null &&
               target != animal &&
               target.team == animal.team &&
               !target.IsDeathProcessing &&
               target.health != null &&
               !target.health.IsDestroyed;
    }

    /// <summary>
    /// 계승 계산에 쓸 체력.
    ///
    /// 보통은 죽는 기물의 최대 체력이다. 다만 특성이 다른 값을 내놓으면 그쪽을 쓴다 —
    /// 개복치처럼 <b>최대 체력이 단단함을 뜻하지 않는</b> 기물이 있기 때문이다.
    /// (LSO_ISuccessionHealth 주석 참고)
    ///
    /// 어떤 특성이 그러는지는 여기서 알지 않는다. 특성 이름을 계승이 직접 알면
    /// 그런 기물이 하나 늘 때마다 이 파일을 같이 고쳐야 한다.
    ///
    /// 여럿이 나서면 가장 낮은 값을 쓴다. 계승으로 새어 나가는 것을 막자는 쪽이
    /// 이 장치의 취지라, 둘이 엇갈릴 때 큰 쪽을 고르면 취지와 반대가 된다.
    /// </summary>
    private static int ResolveSuccessionHealth(LDY_Animal animal)
    {
        int fallback = animal.health != null
            ? animal.health.MaxValue
            : animal.data.maxHealth;

        IReadOnlyList<LSO_IAbility> abilities = animal.Abilities;

        if (abilities == null) return fallback;

        bool found = false;
        int lowest = 0;

        for (int i = 0; i < abilities.Count; i++)
        {
            if (abilities[i] is not LSO_ISuccessionHealth source) continue;

            int value = Mathf.Max(0, source.SuccessionHealth);

            if (!found || value < lowest)
            {
                lowest = value;
                found = true;
            }
        }

        return found ? lowest : fallback;
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

        if (target == null || target.IsDeathProcessing ||
            target.health == null || target.health.IsDestroyed)
        {
            Debug.LogWarning("Invalid succession target.");
            return false;
        }

        if (target.team != successionTeam)
        {
            Debug.LogWarning("Succession target must be on the same team.");
            return false;
        }

        DLJ_SuccessionNotify.HidePrompt();

        isCompletingSuccession = true;
        successionSource.MoveEffectAndApply(target);
        return true;
    }

    public static bool TryCancelIfNoValidTarget()
    {
        if (!IsWaitingForSuccessionTarget || successionSource == null)
            return false;

        if (successionSource.HasValidTarget())
            return false;

        return CancelWaitingSuccession("계승을 받을 생존 기물이 더 이상 없습니다.");
    }

    public static bool TryCancelIfBattleOver()
    {
        if (!IsWaitingForSuccessionTarget || successionSource == null)
            return false;

        if (!KTH_GameEndManager.IsBattleOver)
            return false;

        return CancelWaitingSuccession("전투 종료가 확정되어 계승을 건너뜁니다.");
    }

    private static bool CancelWaitingSuccession(string reason)
    {
        if (successionSource == null)
            return false;

        DLJ_SuccessionWill source = successionSource;
        LDY_Animal sourceAnimal = source.animal;

        DLJ_SuccessionNotify.HidePrompt();
        LDY_DeferredDeaths.Remove(sourceAnimal);

        Debug.Log(
            $"Succession cancelled: {(sourceAnimal != null ? sourceAnimal.name : "destroyed animal")}의 " +
            reason,
            sourceAnimal);

        FinishSuccession();

        // 선택 대기까지 들어온 경우에는 호출부가 다시 사망 처리를 해주지 않는다.
        // 여기서 직접 디졸브를 시작해 유예된 시체가 남지 않게 한다.
        if (sourceAnimal != null)
            LDY_DissolveEffect.PlayOn(sourceAnimal.gameObject);

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
                targetPosition = target.transform.position,
                /*onStarted = () => DLJ_WillCameraFocus.Play(
                    animal != null ? animal.transform.position : Vector3.zero,
                    data.cameraHoldDuration)*/
            },
            () => ApplySuccession(target));
    }

    private static void ApplySuccession(LDY_Animal target)
    {
        DLJ_SuccessionWill source = successionSource;

        if (target != null && !target.IsDeathProcessing &&
            target.health != null && !target.health.IsDestroyed)
        {
            DLJ_SuccessionBonus.Apply(
                target,
                successionHealthBonus,
                successionAttackBonus);
            DLJ_StatIncreaseEffectPlayer.Play(
                target.gameObject,
                source?.statIncreaseEffect);
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
