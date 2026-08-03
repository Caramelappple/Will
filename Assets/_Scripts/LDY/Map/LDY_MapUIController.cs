using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 맵 씬에 배치: 노드 버튼들을 생성하고, MapManager의 connections(간선) 목록을 따라
// 노드 사이를 얇은 골드 별자리 선(UI Image)으로 연결 — 한 노드가 여러 간선을 가지면 분기로 표현됨
public class LDY_MapUIController : MonoBehaviour
{
    [Header("Theme")]
    [SerializeField] private LDY_MapTheme theme;

    [Header("References")]
    [SerializeField] private LDY_MapManager mapManager;
    [SerializeField] private RectTransform nodeContainer;
    [SerializeField] private RectTransform lineContainer;
    [SerializeField] private LDY_MapNodeView nodeViewPrefab;
    [SerializeField] private Image linePrefab;
    [SerializeField] private Image backgroundPanel;
    [SerializeField] private LDY_MapPlayerToken playerTokenPrefab;

    private readonly List<LDY_MapNodeView> nodeViews = new List<LDY_MapNodeView>();
    private LDY_MapPlayerToken playerToken;
    private int currentPlayerNodeIndex = -1;

    private void Start()
    {
        // 씬을 다시 로드하면 씬에 새로 생성된 MapManager는 Awake()에서 중복으로 판정되어
        // BuildNodes() 없이 곧 파괴되지만, 이 시점(Start)엔 아직 완전히 파괴되지 않아 null이 아닐 수 있다.
        // 그래서 인스펙터에 연결된 값보다 항상 영속(DontDestroyOnLoad) 인스턴스를 우선한다.
        if (LDY_MapManager.Instance != null) mapManager = LDY_MapManager.Instance;
        if (mapManager == null)
        {
            Debug.LogError("[LDY_MapUIController] LDY_MapManager를 찾을 수 없습니다.", this);
            return;
        }

        if (backgroundPanel != null && theme != null)
            backgroundPanel.color = theme.navyBackground;

        mapManager.onMapChanged.AddListener(RefreshAll);

        // 노드뷰가 참조를 들고 있어야 하므로 플레이어 토큰을 먼저 만들고,
        // 노드들이 다 생긴 뒤에 형제 순서만 맨 뒤로 옮겨서 항상 위에 그려지게 함
        SpawnPlayerToken();
        SpawnNodeViews();
        DrawConnections();
        if (playerToken != null) playerToken.transform.SetAsLastSibling();
        RefreshAll();
    }

    private void SpawnPlayerToken()
    {
        if (playerTokenPrefab == null) return;

        List<LDY_MapNode> nodes = mapManager.Nodes;
        if (nodes.Count == 0) return;

        int startIndex = nodes.FindIndex(n => n.type == LDY_NodeType.Start);
        if (startIndex < 0) startIndex = 0;

        // 전투 씬 등을 갔다 오면 이 컨트롤러(씬 로컬)는 통째로 새로 생기지만, LDY_MapManager는
        // DontDestroyOnLoad라 CurrentNodeIndex에 "마지막으로 도착한 노드"가 그대로 남아있다 - 이게
        // 없으면(-1, 최초 진입) 시작 노드를 기본값으로 쓴다.
        int spawnIndex = (mapManager.CurrentNodeIndex >= 0 && mapManager.CurrentNodeIndex < nodes.Count)
            ? mapManager.CurrentNodeIndex
            : startIndex;

        playerToken = Instantiate(playerTokenPrefab, nodeContainer);
        playerToken.SetPosition(nodes[spawnIndex].position);
        currentPlayerNodeIndex = spawnIndex;
    }

    // 노드 뷰가 플레이어 도착 시 알려주면, 글로우 색을 다시 계산하도록 전체 갱신
    public void OnPlayerArrivedAt(int nodeIndex)
    {
        currentPlayerNodeIndex = nodeIndex;
        RefreshAll();
    }

    public bool IsPlayerAt(int nodeIndex) => nodeIndex == currentPlayerNodeIndex;

    private void OnDestroy()
    {
        if (mapManager != null)
            mapManager.onMapChanged.RemoveListener(RefreshAll);
    }

    private void SpawnNodeViews()
    {
        List<LDY_MapNode> nodes = mapManager.Nodes;
        for (int i = 0; i < nodes.Count; i++)
        {
            LDY_MapNodeView view = Instantiate(nodeViewPrefab, nodeContainer);
            view.Initialize(mapManager, nodes[i], i, theme, playerToken, this);
            nodeViews.Add(view);
        }
    }

    private void DrawConnections()
    {
        List<LDY_MapNode> nodes = mapManager.Nodes;
        LDY_NodeConnection[] connections = mapManager.Connections;
        if (connections == null) return;

        foreach (LDY_NodeConnection c in connections)
        {
            if (c.fromIndex < 0 || c.fromIndex >= nodes.Count) continue;
            if (c.toIndex < 0 || c.toIndex >= nodes.Count) continue;

            Vector2 from = nodes[c.fromIndex].position;
            Vector2 to = nodes[c.toIndex].position;

            // 글로우를 먼저 깔고(뒤) 그 위에 얇은 실선을 그림(앞) - 은은하게 번지는 느낌
            float thickness = theme != null ? theme.lineThickness : 2f;

            Image glow = Instantiate(linePrefab, lineContainer);
            SetupLine(glow.rectTransform, from, to, thickness * (theme != null ? theme.lineGlowWidthMultiplier : 6f));
            StyleLineGlow(glow);

            Image line = Instantiate(linePrefab, lineContainer);
            SetupLine(line.rectTransform, from, to, thickness);
            StyleLine(line);
        }
    }

    private void SetupLine(RectTransform lineRect, Vector2 from, Vector2 to, float thickness)
    {
        Vector2 direction = to - from;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        lineRect.anchoredPosition = from + direction * 0.5f;
        lineRect.sizeDelta = new Vector2(distance, thickness);
        lineRect.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void StyleLine(Image line)
    {
        if (theme == null) return;

        line.raycastTarget = false;
        Color c = theme.lineColor;
        c.a = theme.lineAlpha;
        line.color = c;
    }

    private void StyleLineGlow(Image glow)
    {
        if (theme == null) return;

        glow.sprite = LDY_ProceduralSprite.SoftLine;
        glow.raycastTarget = false;
        Color c = theme.lineColor;
        c.a = theme.lineGlowAlpha;
        glow.color = c;
    }

    private void RefreshAll()
    {
        foreach (LDY_MapNodeView view in nodeViews)
            view.Refresh();
    }
}
