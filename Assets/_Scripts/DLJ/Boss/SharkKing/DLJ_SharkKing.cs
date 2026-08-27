using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LSO.Manager;
using UnityEngine;

/// <summary>
/// 상어왕 프리팹에 붙이는 인스펙터 설정 컴포넌트.
/// 사냥 영역의 바닥 경고 프리팹과 표시 높이를 보관하고 경고 오브젝트를 관리한다.
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

    private readonly Dictionary<object, List<GameObject>> _warnings = new();
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
                Quaternion.identity,
                null);

            FitHighlightToArea(instance, board, areaSize, areaCenter);

            // 경고 표시는 판정용 오브젝트가 아니므로 보드 클릭과 공격 레이캐스트를 막지 않는다.
            Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;

            instances.Add(instance);
        }

        Debug.Log($"[상어왕] {areaSize}x{areaSize} 사냥 영역 경고 {instances.Count}개 표시", this);
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
