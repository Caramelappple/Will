using System.Collections.Generic;
using DG.Tweening;
using _Scripts.LDY;
using _Scripts.LSO.Manager;
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

    [Tooltip("공격 영역 너비에 대한 큐브 한 변의 비율")]
    [SerializeField, Range(0.1f, 1f)] private float attackCubeSizeRatio = 0.6f;
    [Tooltip("최고점에서 공격 모델 아랫면과 바닥 사이의 거리")]
    [SerializeField, Min(0f)] private float attackCubeRiseHeight = 1f;
    [SerializeField, Min(0.01f)] private float attackCubeRiseDuration = 0.2f;
    [SerializeField, Min(0f)] private float attackCubeHoldDuration = 0.15f;
    [SerializeField, Min(0.01f)] private float attackCubeSinkDuration = 0.35f;
    [Tooltip("비워두면 기본 큐브 머티리얼 사용")]
    [SerializeField] private Material attackCubeMaterial;

    private readonly Dictionary<object, List<GameObject>> _warnings = new();
    private readonly HashSet<GameObject> _attackEffects = new();
    private LDY_TurnManager _turnManager;
    private DLJ_SharkKingHuntingGround _huntingGround;

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
        float cubeSize = Mathf.Max(0.01f,
            Mathf.Min(cellWidth, cellDepth) * areaSize * Mathf.Clamp(attackCubeSizeRatio, 0.1f, 1f));

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
            }
            else
            {
                effect = GameObject.CreatePrimitive(PrimitiveType.Cube);
                effect.name = "DLJ_SharkKingAttackCube";
                effect.transform.position = center;
                effect.transform.localScale = Vector3.one * cubeSize;
                if (attackCubeMaterial != null)
                    effect.GetComponent<Renderer>().sharedMaterial = attackCubeMaterial;
            }

            DisableEffectColliders(effect);
            if (!TryGetRendererBounds(effect, out Bounds effectBounds))
                effectBounds = new Bounds(effect.transform.position, Vector3.one * cubeSize);

            // 모델 피벗 위치가 달라도 외형 중심을 영역에 맞추고, 전체가 바닥 아래로 내려가게 한다.
            Vector3 position = effect.transform.position;
            float buriedY = position.y + center.y - effectBounds.max.y - 0.05f;
            float peakY = position.y + center.y - effectBounds.min.y + Mathf.Max(0f, attackCubeRiseHeight);
            effect.transform.position = new Vector3(
                position.x + center.x - effectBounds.center.x,
                buriedY,
                position.z + center.z - effectBounds.center.z);

            // 경고가 제거된 뒤에도 재생을 마치도록 공격 연출은 별도로 관리한다.
            _attackEffects.Add(effect);
            DOTween.Sequence()
                .Append(effect.transform.DOMoveY(peakY, Mathf.Max(0.01f, attackCubeRiseDuration)).SetEase(Ease.OutCubic))
                .AppendInterval(Mathf.Max(0f, attackCubeHoldDuration))
                .Append(effect.transform.DOMoveY(buriedY, Mathf.Max(0.01f, attackCubeSinkDuration)).SetEase(Ease.InCubic))
                .SetTarget(effect.transform)
                .SetLink(effect)
                .OnComplete(() =>
                {
                    _attackEffects.Remove(effect);
                    Destroy(effect);
                });
        }
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
