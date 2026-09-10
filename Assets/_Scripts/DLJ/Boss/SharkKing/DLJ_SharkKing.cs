using System.Collections.Generic;
using DG.Tweening;
using _Scripts.LDY;
using _Scripts.LSO.Camera;
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
    [Tooltip("최고점에서 공격 모델 아랫면과 바닥 사이의 거리")]
    [SerializeField, Min(0f)] private float attackCubeRiseHeight = 1f;
    [SerializeField, Min(0.01f)] private float attackCubeRiseDuration = 0.2f;
    [SerializeField, Min(0f)] private float attackCubeHoldDuration = 0.15f;
    [SerializeField, Min(0.01f)] private float attackCubeSinkDuration = 0.35f;
    [Tooltip("공격 모델의 모든 머티리얼 슬롯에 적용. 비워두면 프리팹의 원래 머티리얼 유지")]
    [SerializeField] private Material attackCubeMaterial;

    [Header("Shark Attack Motion")]
    [Tooltip("모델의 로컬 전방 축. 상어가 향하는 축에 맞춰 조절")]
    [SerializeField] private Vector3 attackForwardAxis = Vector3.forward;
    [Tooltip("모델 크기에 대한 전진 거리 비율")]
    [SerializeField, Range(0f, 1f)] private float attackTravelRatio = 0.35f;
    [SerializeField, Range(-90f, 90f)] private float attackLaunchAngle = -30f;
    [SerializeField, Range(-90f, 90f)] private float attackBiteAngle = 12f;
    [SerializeField, Range(-90f, 90f)] private float attackDiveAngle = 55f;
    [SerializeField, Range(0f, 30f)] private float attackRollAngle = 8f;

    [Header("Shark Attack Camera Shake")]
    [SerializeField, Min(0f)] private float attackShakeDuration = 0.18f;
    [SerializeField, Min(0f)] private float attackShakeStrength = 0.08f;

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
    private LDY_TurnManager _turnManager;
    private DLJ_SharkKingHuntingGround _huntingGround;
    private int _lastAttackShakeFrame = -1;

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
        if (!isActiveAndEnabled || origins == null || areaSize <= 0 || board == null) return;

        float cellWidth = Vector3.Distance(board.GridToWorld(Vector3Int.zero), board.GridToWorld(Vector3Int.right));
        float cellDepth = Vector3.Distance(board.GridToWorld(Vector3Int.zero), board.GridToWorld(new Vector3Int(0, 0, 1)));
        float effectSize = Mathf.Max(0.01f,
            Mathf.Min(cellWidth, cellDepth) * areaSize * Mathf.Clamp(attackCubeSizeRatio, 0.1f, 1f));
        bool shakeScheduled = false;

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

            // 외형 중심을 회전축으로 삼아 FBX 피벗이 멀리 있어도 제자리에서 몸을 꺾는다.
            GameObject motionRoot = new GameObject("DLJ_SharkKingAttackMotion");
            motionRoot.transform.position = center;
            effect.transform.SetParent(motionRoot.transform, true);
            effect.transform.position += center - effectBounds.center;

            Vector3 forward = Vector3.ProjectOnPlane(effect.transform.rotation * attackForwardAxis, Vector3.up);
            forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            float travel = effectSize * Mathf.Clamp01(attackTravelRatio);
            // 회전한 모델의 모서리까지 잠기도록 바운딩 구의 반지름만큼 내린다.
            float buriedY = center.y - effectBounds.extents.magnitude - 0.05f;
            Vector3 launch = center - forward * travel * 0.5f;
            launch.y = buriedY;
            Vector3 peak = center + Vector3.up * (effectBounds.extents.y + Mathf.Max(0f, attackCubeRiseHeight));
            Vector3 dive = center + forward * travel;
            dive.y = buriedY;
            Quaternion launchRotation = Quaternion.AngleAxis(attackLaunchAngle, right)
                * Quaternion.AngleAxis(-attackRollAngle, forward);
            Quaternion biteRotation = Quaternion.AngleAxis(attackBiteAngle, right)
                * Quaternion.AngleAxis(attackRollAngle, forward);
            Quaternion diveRotation = Quaternion.AngleAxis(attackDiveAngle, right)
                * Quaternion.AngleAxis(-attackRollAngle, forward);
            motionRoot.transform.SetPositionAndRotation(launch, launchRotation);
            float riseDuration = Mathf.Max(0.01f, attackCubeRiseDuration);
            float holdDuration = Mathf.Max(0f, attackCubeHoldDuration);
            float sinkDuration = Mathf.Max(0.01f, attackCubeSinkDuration);

            // 경고가 제거된 뒤에도 재생을 마치도록 공격 연출은 별도로 관리한다.
            _attackEffects.Add(motionRoot);
            Sequence motion = DOTween.Sequence()
                .Append(motionRoot.transform.DOMove(peak, riseDuration).SetEase(Ease.OutCubic))
                .Join(motionRoot.transform.DORotateQuaternion(Quaternion.identity, riseDuration).SetEase(Ease.OutCubic))
                .Append(motionRoot.transform.DORotateQuaternion(biteRotation, holdDuration).SetEase(Ease.InOutSine))
                .Append(motionRoot.transform.DOMove(dive, sinkDuration).SetEase(Ease.InQuad))
                .Join(motionRoot.transform.DORotateQuaternion(diveRotation, sinkDuration).SetEase(Ease.InSine))
                .SetTarget(motionRoot.transform)
                .SetLink(motionRoot)
                .OnComplete(() =>
                {
                    _attackEffects.Remove(motionRoot);
                    Destroy(motionRoot);
                });

            if (!shakeScheduled)
            {
                motion.InsertCallback(riseDuration * 0.2f, ShakeOnAttack);
                shakeScheduled = true;
            }
        }
    }

    private void ShakeOnAttack()
    {
        // 같은 프레임에 여러 예약 공격이 터져도 흔들림 세기가 중첩되지 않게 한다.
        if (_lastAttackShakeFrame == Time.frameCount) return;
        _lastAttackShakeFrame = Time.frameCount;
        LSO_CameraImpulse.Shake(attackShakeDuration, attackShakeStrength);
    }

    private void ClearAttackEffects()
    {
        foreach (GameObject effect in _attackEffects)
        {
            if (effect == null) continue;
            effect.transform.DOKill();
            Destroy(effect);
        }

        _attackEffects.Clear();
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

        if (!TryGetRendererBounds(instance, out Bounds bounds))
        {
            Vector3 fallbackScale = instance.transform.localScale;
            fallbackScale.x = targetWidth;
            fallbackScale.z = targetDepth;
            instance.transform.localScale = fallbackScale;
            instance.transform.position = areaCenter + Vector3.up * attackHighlightHeightOffset;
            return;
        }

        Vector3 scale = instance.transform.localScale;
        if (bounds.size.x > Mathf.Epsilon)
            scale.x *= targetWidth / bounds.size.x;
        if (bounds.size.z > Mathf.Epsilon)
            scale.z *= targetDepth / bounds.size.z;
        instance.transform.localScale = scale;

        TryGetRendererBounds(instance, out bounds);
        Vector3 correction = areaCenter - bounds.center;
        // 두께 전체를 보드 위에 올리면 기물을 가리므로, 윗면만 살짝 보이게 나머지는 바닥 아래로 묻는다.
        correction.y = areaCenter.y + attackHighlightHeightOffset - bounds.max.y;
        instance.transform.position += correction;
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
