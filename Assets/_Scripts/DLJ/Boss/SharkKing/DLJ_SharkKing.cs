using System.Collections.Generic;
using DG.Tweening;
using _Scripts.LDY;
using _Scripts.LSO.Manager;
using _Scripts.LSO.UI.Popup;
using UnityEngine;

/// <summary>
/// 상어왕 프리팹에 붙이는 인스펙터 설정 컴포넌트.
/// 사냥 영역의 바닥 경고와 SharkSpin 모델 설정을 보관하고 생성된 이펙트를 관리한다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(LDY_Animal))]
public sealed class DLJ_SharkKing : MonoBehaviour
{
    [Header("일반 공격 · 반투명 이빨")]
    [SerializeField] private Color biteColor = new(0.78f, 0.95f, 1f, 0.62f);
    [SerializeField, Min(0.5f)] private float biteSize = 1.25f;
    [SerializeField, Min(0.05f)] private float biteAppearDuration = 0.16f;
    [SerializeField, Min(0.05f)] private float biteCloseDuration = 0.12f;
    [SerializeField, Min(0.05f)] private float biteFadeDuration = 0.22f;

    public System.Collections.IEnumerator PlayBiteAttack(LDY_Animal target, System.Action onImpact)
    {
        return DLJ_SharkKingBiteEffect.Play(this, target, biteColor, biteSize,
            biteAppearDuration, biteCloseDuration, biteFadeDuration, onImpact);
    }

    [Header("Hunting Ground Warning")]
    [Tooltip("사냥 영역의 각 바닥 칸에 생성할 AttackHighlight 프리팹")]
    [SerializeField] private GameObject attackHighlightPrefab;

    [Tooltip("바닥과 겹쳐 깜빡이는 현상을 막기 위한 높이 보정")]
    [SerializeField] private float attackHighlightHeightOffset = 0.05f;

    [Header("Hunting Ground Effect")]
    [Tooltip("각 경고 쿼드의 중심에 생성할 SharkSpin 모델 또는 프리팹")]
    [SerializeField] private GameObject sharkTeethPrefab;

    [Tooltip("경고 쿼드 표면을 기준으로 SharkSpin 모델을 띄울 높이")]
    [SerializeField] private float sharkTeethHeightOffset = 0f;

    [Header("Hunting Ground Attack Effect")]
    [Tooltip("공격 시 솟아오를 상어 모델 또는 프리팹. 비워두면 큐브 사용")]
    [SerializeField] private GameObject attackSharkPrefab;

    [Tooltip("공격 영역 너비에 대한 모델의 가로/세로 중 긴 쪽 크기 비율. 모델 비율은 유지")]
    [SerializeField, Range(0.1f, 1f)] private float attackCubeSizeRatio = 0.6f;
    [Tooltip("수직으로 솟을 때 모델 중심을 수면 위로 올리는 추가 높이")]
    [SerializeField, Min(0f)] private float attackCubeRiseHeight = 1f;
    [Tooltip("부드럽게 가속했다가 정점에서 감속하는 상승 시간")]
    [SerializeField, Min(0.01f), InspectorName("상승 시간")] private float attackCubeRiseDuration = 0.75f;
    [Tooltip("정점에서 위치와 자세를 그대로 유지하는 체공 시간")]
    [SerializeField, Min(0f), InspectorName("체공 시간")] private float attackCubeHoldDuration = 0.22f;
    [Tooltip("체공 뒤 점점 빠르게 떨어지는 시간. 짧을수록 강하게 낙하")]
    [SerializeField, Min(0.01f), InspectorName("낙하 시간")] private float attackCubeSinkDuration = 0.45f;
    [Tooltip("공격 모델의 모든 머티리얼 슬롯에 적용. 비워두면 프리팹의 원래 머티리얼 유지")]
    [SerializeField] private Material attackCubeMaterial;

    [Header("Shark Attack Water Splash")]
    [Tooltip("공격 시 상어가 튀어나오는 지점에 생성할 물방울 머티리얼. 비워두면 런타임 파티클 머티리얼 사용")]
    [SerializeField] private Material attackWaterMaterial;
    [SerializeField] private Color attackWaterColor = new(0.14f, 0.78f, 0.88f, 0.85f);
    [Tooltip("상어 주변에서 솟는 물막 높이. 물보라 크기에 대한 비율")]
    [SerializeField, Range(0.1f, 1.5f), InspectorName("물막 높이")] private float attackWaterCrownHeight = 0.65f;
    [Tooltip("청록색 수면의 반지름. 물보라 크기에 대한 비율")]
    [SerializeField, Range(0.4f, 1.5f), InspectorName("수면 반지름")] private float attackWaterPoolRadius = 0.9f;
    [Tooltip("첫 분출의 물방울 개수. 설정한 개수 그대로 생성하며 0이면 물방울 끄기")]
    [SerializeField, Range(0, 128), InspectorName("물방울 개수")] private int attackWaterParticleCount = 12;
    [Tooltip("첫 분출의 굵은 물줄기 개수. 0이면 물줄기 끄기")]
    [SerializeField, Range(0, 64), InspectorName("물줄기 개수")] private int attackWaterJetCount = 6;
    [Tooltip("0.08초 뒤 추가 분출할 비율. 0이면 추가 분출 없음, 1이면 같은 개수 한 번 더 분출")]
    [SerializeField, Range(0f, 1f), InspectorName("추가 분출 비율")] private float attackWaterExtraBurstRatio = 0f;
    [SerializeField, Min(0.1f)] private float attackWaterLifetime = 0.8f;
    [SerializeField, Range(0f, 1f)] private float attackWaterRadiusRatio = 0.18f;
    [Tooltip("물보라 전체 크기 배율. 기존 씬에서도 적용")]
    [SerializeField, Range(0.5f, 3f)] private float attackWaterScale = 1.35f;
    [Tooltip("낙하할 때 나오는 물줄기·물막·잔물결의 크기 배율. 1이면 상승 때와 같은 크기. 입자 개수는 유지")]
    [SerializeField, Range(1f, 3f), InspectorName("낙하 물보라 크기 배율")]
    private float attackWaterFallScale = 1.8f;

    [Header("Shark Attack Motion")]
    [Tooltip("모델의 로컬 전방 축. 상어가 향하는 축에 맞춰 조절")]
    [SerializeField] private Vector3 attackForwardAxis = Vector3.forward;
    [Tooltip("재생 내내 유지할 고정 자세. -90이면 머리가 수직 위를 향함")]
    [SerializeField, Range(-90f, 90f)] private float attackLaunchAngle = -78f;

    [Header("Predation Mark")]
    [Tooltip("포식 상태의 기물 위에 표시할 문양 프리팹. 비워두면 임시 Quad 사용")]
    [SerializeField] private GameObject predationMarkPrefab;
    [Tooltip("기물 모델을 기준으로 한 문양 위치")]
    [SerializeField] private Vector3 predationMarkLocalOffset = new(0f, 1f, 0f);
    [Tooltip("문양 프리팹의 원래 크기에 곱할 배율")]
    [SerializeField] private Vector3 predationMarkScale = new(0.35f, 0.35f, 0.35f);
    [Tooltip("임시 Quad에만 적용할 색상")]
    [SerializeField] private Color fallbackPredationMarkColor = new(0.65f, 0.05f, 0.05f, 1f);

    private readonly Dictionary<object, List<GameObject>> _warnings = new();
    private readonly HashSet<GameObject> _attackEffects = new();
    private readonly HashSet<GameObject> _previewEffects = new();
    private LDY_TurnManager _turnManager;
    private DLJ_SharkKingHuntingGround _huntingGround;
    private Material _runtimeWaterMaterial;
    private Material _runtimeSurfaceMaterial;

    private void Start()
    {
        if (!GameManager.HasInstance)
        {
            Debug.LogError("[상어왕] GameManager가 없어 턴 이벤트에 연결할 수 없습니다.", this);
            return;
        }

        GameManager.Instance.TurnManagerChanged += BindTurnManager;
        BindTurnManager(GameManager.Instance.TurnManager);
    }

    public void RegisterHuntingGround(DLJ_SharkKingHuntingGround huntingGround)
    {
        _huntingGround = huntingGround;
    }

    public GameObject CreatePredationMarkVisual(Transform targetAnchor)
    {
        if (targetAnchor == null) return null;

        GameObject mark;
        if (predationMarkPrefab != null)
        {
            mark = Instantiate(predationMarkPrefab, targetAnchor);
            mark.name = $"{predationMarkPrefab.name}_PredationMark";
        }
        else
        {
            mark = GameObject.CreatePrimitive(PrimitiveType.Quad);
            mark.name = "DLJ_SharkKingPredationMark_Quad";
            mark.transform.SetParent(targetAnchor, false);
            mark.AddComponent<LSO_Billboard>();

            Renderer renderer = mark.GetComponent<Renderer>();
            if (renderer != null)
            {
                MaterialPropertyBlock properties = new();
                renderer.GetPropertyBlock(properties);
                properties.SetColor("_BaseColor", fallbackPredationMarkColor);
                properties.SetColor("_Color", fallbackPredationMarkColor);
                renderer.SetPropertyBlock(properties);
                renderer.sortingOrder = 10;
            }
        }

        mark.transform.localPosition = predationMarkLocalOffset;
        mark.transform.localScale = Vector3.Scale(mark.transform.localScale, predationMarkScale);
        DisableEffectColliders(mark);
        return mark;
    }

    private void BindTurnManager(LDY_TurnManager turnManager)
    {
        if (_turnManager == turnManager) return;

        if (_turnManager != null)
            _turnManager.OnTurnChanged -= HandleTurnChanged;

        _turnManager = turnManager;

        if (_turnManager != null)
        {
            _turnManager.OnTurnChanged += HandleTurnChanged;
            Debug.Log("[상어왕] TurnManager 연결 완료", this);
        }
        else
        {
            Debug.LogWarning("[상어왕] 현재 TurnManager가 없어 사냥터 개장을 대기합니다.", this);
        }
    }

    private void HandleTurnChanged(LDY_Team team)
    {
        if (team != LDY_Team.Enemy) return;

        if (_huntingGround == null)
        {
            Debug.LogError("[상어왕] DLJ_SharkKingHuntingGround 능력 인스턴스가 연결되지 않았습니다.", this);
            return;
        }

        _huntingGround.HandleEnemyTurnStart(_turnManager != null ? _turnManager.ActionPoints : null);
    }

    public void ShowAttackHighlights(
        object owner,
        IEnumerable<Vector3Int> origins,
        int areaSize,
        LDY_BoardManager board)
    {
        if (owner == null || origins == null || areaSize <= 0 || board == null) return;

        ClearAttackHighlights(owner);

        if (attackHighlightPrefab == null)
        {
            Debug.LogWarning(
                $"{name}: DLJ_SharkKing의 Attack Highlight Prefab이 비어 있어 사냥 영역을 표시할 수 없습니다.",
                this);
            return;
        }

        List<GameObject> instances = new();
        _warnings.Add(owner, instances);

        HashSet<Vector3Int> uniqueOrigins = new();
        foreach (Vector3Int origin in origins)
        {
            Vector3Int floorOrigin = new Vector3Int(origin.x, 0, origin.z);
            Vector3Int opposite = new Vector3Int(
                floorOrigin.x + areaSize - 1,
                0,
                floorOrigin.z + areaSize - 1);
            if (!board.IsInside(floorOrigin) ||
                !board.IsInside(opposite) ||
                !uniqueOrigins.Add(floorOrigin))
                continue;

            Vector3 firstCenter = board.GridToWorld(floorOrigin);
            Vector3 lastCenter = board.GridToWorld(opposite);
            Vector3 areaCenter = (firstCenter + lastCenter) * 0.5f;
            GameObject instance = Instantiate(
                attackHighlightPrefab,
                areaCenter,
                Quaternion.Euler(90f, 0f, 0f),
                null);

            FitHighlightToArea(instance, board, areaSize, areaCenter);

            DisableEffectColliders(instance);
            instances.Add(instance);

            if (sharkTeethPrefab != null)
            {
                // 쿼드의 회전과 영역 크기 보정이 모델에 적용되지 않도록 별도로 생성한다.
                GameObject sharkSpin = Instantiate(
                    sharkTeethPrefab,
                    areaCenter + Vector3.up * (attackHighlightHeightOffset + sharkTeethHeightOffset),
                    sharkTeethPrefab.transform.rotation,
                    null);
                sharkSpin.transform.localScale = new Vector3(15f, 15f, 15f);
                DisableEffectColliders(sharkSpin);
                instances.Add(sharkSpin);
            }
        }

        Debug.Log($"[상어왕] {areaSize}x{areaSize} 사냥 영역 경고 {uniqueOrigins.Count}개 표시", this);
    }

    public void PlayAttackEffects(
        IEnumerable<Vector3Int> origins,
        int areaSize,
        LDY_BoardManager board)
    {
        PlayAttackEffects(origins, areaSize, board, _attackEffects);
    }

    public bool IsPreviewingAttack => _previewEffects.Count > 0;

    // 미리보기는 실제 공격과 같은 연출만 호출. 턴, 피해, AP, 포식 표식은 건드리지 않음.
    public void PreviewAttack(LDY_BoardManager board, Vector2Int origin, int areaSize, bool waterOnly)
    {
        if (!Application.isPlaying || !isActiveAndEnabled || board == null) return;

        StopAttackPreview();
        areaSize = Mathf.Clamp(areaSize, 1, LDY_BoardManager.Size);
        Vector3Int first = new(
            Mathf.Clamp(origin.x, 0, LDY_BoardManager.Size - areaSize), 0,
            Mathf.Clamp(origin.y, 0, LDY_BoardManager.Size - areaSize));
        if (waterOnly)
        {
            Vector3Int last = first + new Vector3Int(areaSize - 1, 0, areaSize - 1);
            Vector3 center = (board.GridToWorld(first) + board.GridToWorld(last)) * 0.5f;
            CreateWaterSplash(center, GetAttackEffectSize(board, areaSize), _previewEffects);
        }
        else
        {
            PlayAttackEffects(new[] { first }, areaSize, board, _previewEffects);
        }
    }

    public void StopAttackPreview()
    {
        ClearEffects(_previewEffects);
    }

    private float GetAttackEffectSize(LDY_BoardManager board, int areaSize)
    {
        float cellWidth = Vector3.Distance(board.GridToWorld(Vector3Int.zero), board.GridToWorld(Vector3Int.right));
        float cellDepth = Vector3.Distance(board.GridToWorld(Vector3Int.zero), board.GridToWorld(new Vector3Int(0, 0, 1)));
        return Mathf.Max(0.01f,
            Mathf.Min(cellWidth, cellDepth) * areaSize * Mathf.Clamp(attackCubeSizeRatio, 0.1f, 1f));
    }

    private void PlayAttackEffects(
        IEnumerable<Vector3Int> origins,
        int areaSize,
        LDY_BoardManager board,
        HashSet<GameObject> effects)
    {
        if (!isActiveAndEnabled || origins == null || areaSize <= 0 || board == null) return;

        float effectSize = GetAttackEffectSize(board, areaSize);

        HashSet<Vector3Int> uniqueOrigins = new();
        foreach (Vector3Int origin in origins)
        {
            Vector3Int floorOrigin = new Vector3Int(origin.x, 0, origin.z);
            Vector3Int opposite = floorOrigin + new Vector3Int(areaSize - 1, 0, areaSize - 1);
            if (!board.IsInside(floorOrigin) || !board.IsInside(opposite) || !uniqueOrigins.Add(floorOrigin))
                continue;

            Vector3 center = (board.GridToWorld(floorOrigin) + board.GridToWorld(opposite)) * 0.5f;
            GameObject effect;
            if (attackSharkPrefab != null)
            {
                effect = Instantiate(attackSharkPrefab, center, attackSharkPrefab.transform.rotation);
                // 이펙트 복제본은 위치만 이동. 원본 모델의 애니메이션 설정은 유지.
                foreach (Animator animator in effect.GetComponentsInChildren<Animator>(true))
                    animator.enabled = false;
                foreach (Animation animation in effect.GetComponentsInChildren<Animation>(true))
                {
                    animation.Stop();
                    animation.enabled = false;
                }
                if (TryGetRendererBounds(effect, out Bounds modelBounds))
                {
                    float footprint = Mathf.Max(modelBounds.size.x, modelBounds.size.z);
                    if (footprint > Mathf.Epsilon)
                        effect.transform.localScale *= effectSize / footprint;
                }
            }
            else
            {
                effect = GameObject.CreatePrimitive(PrimitiveType.Cube);
                effect.name = "DLJ_SharkKingAttackCube";
                effect.transform.position = center;
                effect.transform.localScale = Vector3.one * effectSize;
            }

            if (attackCubeMaterial != null)
            {
                foreach (Renderer effectRenderer in effect.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] materials = effectRenderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                        materials[i] = attackCubeMaterial;
                    effectRenderer.sharedMaterials = materials;
                }
            }

            DisableEffectColliders(effect);
            if (!TryGetRendererBounds(effect, out Bounds effectBounds))
                effectBounds = new Bounds(effect.transform.position, Vector3.one * effectSize);

            // FBX 피벗과 무관하게 외형 중심을 기준으로 수직 이동.
            GameObject motionRoot = new GameObject("DLJ_SharkKingAttackMotion");
            motionRoot.transform.position = center;
            effect.transform.SetParent(motionRoot.transform, true);
            effect.transform.position += center - effectBounds.center;

            Vector3 modelForward = effect.transform.rotation * attackForwardAxis;
            modelForward = modelForward.sqrMagnitude > 0.0001f ? modelForward.normalized : Vector3.forward;
            Vector3 forward = Vector3.ProjectOnPlane(modelForward, Vector3.up);
            forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Quaternion levelRotation = Quaternion.FromToRotation(modelForward, forward);
            // 회전한 모델의 모서리까지 잠기도록 바운딩 구의 반지름만큼 내린다.
            float buriedY = center.y - effectBounds.extents.magnitude - 0.05f;
            Vector3 launch = center;
            launch.y = buriedY;
            // 정점에서는 높이를 유지하고, 체공이 끝나면 꼬리부터 가속 낙하.
            Vector3 peak = center + Vector3.up * (effectBounds.extents.y + Mathf.Max(0f, attackCubeRiseHeight));
            Vector3 dive = launch;
            Quaternion launchRotation = Quaternion.AngleAxis(attackLaunchAngle, right)
                * levelRotation;
            motionRoot.transform.SetPositionAndRotation(launch, launchRotation);
            float riseDuration = Mathf.Max(0.01f, attackCubeRiseDuration);
            float holdDuration = Mathf.Max(0f, attackCubeHoldDuration);
            float sinkDuration = Mathf.Max(0.01f, attackCubeSinkDuration);

            // 모델의 세로 중심이 수면을 지날 때 절반쯤 잠긴 것으로 처리.
            float modelCenterOffsetY = TryGetRendererBounds(effect, out Bounds posedBounds)
                ? posedBounds.center.y - launch.y : 0f;
            float waterSurfaceY = center.y + Mathf.Max(0.12f, attackHighlightHeightOffset);
            float submergedDistanceRatio = Mathf.InverseLerp(peak.y, dive.y,
                waterSurfaceY - modelCenterOffsetY);
            // 낙하의 InCubic(t^3)을 역산해야 이동 거리와 물보라 타이밍이 일치.
            float fallSplashTime = riseDuration + holdDuration
                + sinkDuration * Mathf.Pow(submergedDistanceRatio, 1f / 3f);

            // 경고가 제거된 뒤에도 재생을 마치도록 공격 연출은 별도로 관리한다.
            effects.Add(motionRoot);
            Sequence motion = DOTween.Sequence()
                .Append(motionRoot.transform.DOMove(peak, riseDuration).SetEase(Ease.OutCubic))
                .AppendInterval(holdDuration)
                .Append(motionRoot.transform.DOMove(dive, sinkDuration).SetEase(Ease.InCubic))
                .InsertCallback(fallSplashTime, () => CreateWaterSplash(center,
                    effectSize, effects, Mathf.Clamp(attackWaterFallScale, 1f, 3f)))
                .SetTarget(motionRoot.transform)
                .SetLink(motionRoot)
                .OnComplete(() =>
                {
                    effects.Remove(motionRoot);
                    Destroy(motionRoot);
                });

            // 먼저 수면이 열리고 상승 중 물막이 솟도록 상어와 동시에 시작.
            CreateWaterSplash(center, effectSize, effects);

        }
    }

    private void CreateWaterSplash(Vector3 center, float effectSize, HashSet<GameObject> effects,
        float sizeMultiplier = 1f)
    {
        Material waterMaterial = GetWaterMaterial();
        if (waterMaterial == null) return;

        float size = Mathf.Max(0.4f, effectSize) * Mathf.Clamp(attackWaterScale, 0.5f, 3f)
            * sizeMultiplier;
        float motionDuration = Mathf.Max(0.01f, attackCubeRiseDuration)
            + Mathf.Max(0f, attackCubeHoldDuration) + Mathf.Max(0.01f, attackCubeSinkDuration);
        float lifetime = Mathf.Max(1.15f, attackWaterLifetime, motionDuration + 0.2f);
        int count = Mathf.Clamp(attackWaterParticleCount, 0, 128);
        int jetCount = Mathf.Clamp(attackWaterJetCount, 0, 64);
        float extraBurstRatio = Mathf.Clamp01(attackWaterExtraBurstRatio);
        float radius = size * Mathf.Clamp(attackWaterRadiusRatio * 2f, 0.28f, 0.65f);
        Color waterColor = attackWaterColor;

        GameObject splash = new("DLJ_SharkKingWaterSplash");
        splash.transform.position = center + Vector3.up * Mathf.Max(0.12f, attackHighlightHeightOffset);
        effects.Add(splash);

        CreateSplashParticles(splash.transform, "WaterJets", waterMaterial,
            size, radius, jetCount, 0.45f, 32f, 3.1f, 0.055f, 0.1f, waterColor, true,
            Mathf.RoundToInt(jetCount * extraBurstRatio));
        CreateSplashParticles(splash.transform, "BrightDroplets", waterMaterial,
            size, radius, count, lifetime * 0.7f, 48f, 3.4f, 0.035f, 0.065f,
            Color.Lerp(waterColor, new Color(0.75f, 1f, 0.97f, waterColor.a), 0.5f), true,
            Mathf.RoundToInt(count * extraBurstRatio));

        Material surfaceMaterial = GetSurfaceMaterial();
        if (surfaceMaterial != null)
            splash.AddComponent<DLJ_SharkWaterCrown>().Initialize(surfaceMaterial, size,
                lifetime, attackWaterCrownHeight, attackWaterPoolRadius, waterColor);

        // 마지막 분출의 입자까지 사라진 뒤에 정리.
        foreach (ParticleSystem system in splash.GetComponentsInChildren<ParticleSystem>())
            system.Play(false);
        DOVirtual.DelayedCall(lifetime + 0.2f, () =>
            {
                effects.Remove(splash);
                Destroy(splash);
            }, false)
            .SetTarget(splash.transform)
            .SetLink(splash);
    }

    private ParticleSystem CreateSplashParticles(
        Transform parent, string name, Material material, float size, float radius,
        int count, float lifetime, float angle, float speed, float minSize, float maxSize,
        Color color, bool stretch, int extraCount = 0)
    {
        if (count <= 0) return null;
        extraCount = Mathf.Clamp(extraCount, 0, count);
        GameObject emitter = new(name);
        emitter.transform.SetParent(parent, false);
        // Cone은 로컬 +Z 방향으로 분출하므로 월드 +Y로 회전.
        emitter.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        ParticleSystem particles = emitter.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = lifetime;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.75f, lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(size * speed * 0.7f, size * speed);
        main.startSize = new ParticleSystem.MinMaxCurve(size * minSize, size * maxSize);
        main.startColor = color;
        main.gravityModifier = size * 0.85f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = count + extraCount;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.rateOverDistance = 0f;
        emission.SetBursts(extraCount > 0
            ? new[]
            {
                new ParticleSystem.Burst(0f, (short)count),
                new ParticleSystem.Burst(0.08f, (short)extraCount)
            }
            : new[] { new ParticleSystem.Burst(0f, (short)count) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = angle;
        shape.radius = radius;
        shape.radiusThickness = 0.2f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fade = new();
        fade.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(0.7f, 0.95f, 1f), 0.45f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.85f, 0.55f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fade);

        var sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f, 0.7f), new Keyframe(0.15f, 1f), new Keyframe(1f, 0.25f)));

        ParticleSystemRenderer renderer = emitter.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = stretch
            ? ParticleSystemRenderMode.Stretch
            : ParticleSystemRenderMode.Billboard;
        renderer.lengthScale = 1.8f;
        renderer.velocityScale = 0.015f;
        renderer.maxParticleSize = 0.5f;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sharedMaterial = material;

        return particles;
    }

    private Material GetWaterMaterial()
    {
        if (attackWaterMaterial != null) return attackWaterMaterial;
        if (_runtimeWaterMaterial != null) return _runtimeWaterMaterial;

        Shader shader = Resources.Load<Shader>("DLJ/DLJ_SharkWaterSplash");
        if (shader == null) return null;

        _runtimeWaterMaterial = new Material(shader)
        {
            name = "DLJ_SharkKingWaterRuntime"
        };
        return _runtimeWaterMaterial;
    }

    private Material GetSurfaceMaterial()
    {
        if (_runtimeSurfaceMaterial != null) return _runtimeSurfaceMaterial;
        Shader shader = Resources.Load<Shader>("DLJ/DLJ_SharkWaterSplash");
        if (shader == null) return null;
        _runtimeSurfaceMaterial = new Material(shader) { name = "DLJ_SharkKingSurfaceRuntime" };
        return _runtimeSurfaceMaterial;
    }

    private void ClearAttackEffects()
    {
        ClearEffects(_attackEffects);
        StopAttackPreview();
    }

    private static void ClearEffects(HashSet<GameObject> effects)
    {
        foreach (GameObject effect in effects)
        {
            if (effect == null) continue;
            effect.transform.DOKill();
            Destroy(effect);
        }

        effects.Clear();
    }

    private void OnDisable()
    {
        ClearAttackEffects();
    }

    private static void DisableEffectColliders(GameObject instance)
    {
        // 경고와 모델은 판정용 오브젝트가 아니므로 보드 클릭과 공격 레이캐스트를 막지 않는다.
        Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;
    }

    private void FitHighlightToArea(
        GameObject instance,
        LDY_BoardManager board,
        int areaSize,
        Vector3 areaCenter)
    {
        float cellWidth = Mathf.Abs(
            board.GridToWorld(Vector3Int.right).x -
            board.GridToWorld(Vector3Int.zero).x);
        float cellDepth = Mathf.Abs(
            board.GridToWorld(new Vector3Int(0, 0, 1)).z -
            board.GridToWorld(Vector3Int.zero).z);
        float targetWidth = cellWidth * areaSize;
        float targetDepth = cellDepth * areaSize;

        // 경고판은 눕혀서 생성하므로(ShowAttackHighlights의 Euler(90,0,0)) 로컬 축과 월드 축이 다르다.
        // 크기는 월드에서 재고, 그 크기를 실제로 만들어내는 로컬 축에 곱해야 한다.
        int widthAxis = LocalAxisOf(instance.transform, Vector3.right);
        int depthAxis = LocalAxisOf(instance.transform, Vector3.forward);

        if (widthAxis == depthAxis)
        {
            Debug.LogWarning(
                $"{name}: 경고판이 비스듬히 놓여 가로와 세로가 같은 로컬 축({widthAxis})에 걸립니다. " +
                "세로는 맞추지 못합니다. 프리팹 회전을 직각으로 두세요.", this);
        }

        if (!TryGetRendererBounds(instance, out Bounds bounds))
        {
            Vector3 fallbackScale = instance.transform.localScale;
            fallbackScale[widthAxis] = targetWidth;
            if (widthAxis != depthAxis) fallbackScale[depthAxis] = targetDepth;
            instance.transform.localScale = fallbackScale;
            instance.transform.position = areaCenter + Vector3.up * attackHighlightHeightOffset;
            return;
        }

        Vector3 scale = instance.transform.localScale;
        if (bounds.size.x > Mathf.Epsilon)
            scale[widthAxis] *= targetWidth / bounds.size.x;
        if (bounds.size.z > Mathf.Epsilon && widthAxis != depthAxis)
            scale[depthAxis] *= targetDepth / bounds.size.z;
        instance.transform.localScale = scale;

        TryGetRendererBounds(instance, out bounds);
        Vector3 correction = areaCenter - bounds.center;
        // 두께 전체를 보드 위에 올리면 기물을 가리므로, 윗면만 살짝 보이게 나머지는 바닥 아래로 묻는다.
        correction.y = areaCenter.y + attackHighlightHeightOffset - bounds.max.y;
        instance.transform.position += correction;
    }

    /// <summary>
    /// 그 월드 축의 길이를 실제로 만들어내는 로컬 축 번호(0=x, 1=y, 2=z).
    ///
    /// 눕힌 Quad 처럼 회전이 걸린 물건은 "월드에서 가로를 늘리고 싶다"가
    /// 곧 "로컬 x를 늘려라"가 아니다. 회전을 되돌려서 어느 축인지 물어본다.
    /// </summary>
    private static int LocalAxisOf(Transform target, Vector3 worldAxis)
    {
        Vector3 local = Quaternion.Inverse(target.rotation) * worldAxis;

        float x = Mathf.Abs(local.x);
        float y = Mathf.Abs(local.y);
        float z = Mathf.Abs(local.z);

        if (x >= y && x >= z) return 0;

        return y >= z ? 1 : 2;
    }

    private static bool TryGetRendererBounds(GameObject instance, out Bounds bounds)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return true;
    }

    public void ClearAttackHighlights(object owner)
    {
        if (owner == null || !_warnings.TryGetValue(owner, out List<GameObject> instances)) return;

        for (int i = 0; i < instances.Count; i++)
        {
            if (instances[i] != null)
                Destroy(instances[i]);
        }

        _warnings.Remove(owner);
    }

    private void OnDestroy()
    {
        ClearAttackEffects();

        if (_runtimeWaterMaterial != null)
            Destroy(_runtimeWaterMaterial);
        if (_runtimeSurfaceMaterial != null)
            Destroy(_runtimeSurfaceMaterial);

        if (GameManager.HasInstance)
            GameManager.Instance.TurnManagerChanged -= BindTurnManager;

        if (_turnManager != null)
            _turnManager.OnTurnChanged -= HandleTurnChanged;

        foreach (List<GameObject> instances in _warnings.Values)
        {
            for (int i = 0; i < instances.Count; i++)
            {
                if (instances[i] != null)
                    Destroy(instances[i]);
            }
        }

        _warnings.Clear();
    }
}
