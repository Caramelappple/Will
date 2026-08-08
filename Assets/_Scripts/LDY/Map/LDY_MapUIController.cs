using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    private readonly List<LDY_MapNodeView> nodeViews = new List<LDY_MapNodeView>();
    private int currentPlayerNodeIndex = -1;

    private void Start()
    {
        if (LDY_MapManager.Instance != null) mapManager = LDY_MapManager.Instance;
        if (mapManager == null)
        {
            Debug.LogError("[LDY_MapUIController] LDY_MapManager를 찾을 수 없습니다.", this);
            return;
        }

        if (backgroundPanel != null && theme != null)
            backgroundPanel.color = theme.navyBackground;

        // ★ onMapChanged 발생 시 노드/라인을 전부 재구성하도록 변경
        mapManager.onMapChanged.AddListener(RebuildMap);

        RebuildMap();
    }

    private void OnDestroy()
    {
        if (mapManager != null)
            mapManager.onMapChanged.RemoveListener(RebuildMap);
    }

    // ★ 맵(챕터/스테이지)이 바뀔 때마다 노드 뷰 + 연결선을 전부 새로 그림
    private void RebuildMap()
    {
        ClearNodeViews();
        ClearLines();

        InitPlayerPosition();
        SpawnNodeViews();
        DrawConnections();
        RefreshAll();
    }

    private void ClearNodeViews()
    {
        foreach (LDY_MapNodeView view in nodeViews)
        {
            if (view != null) Destroy(view.gameObject);
        }
        nodeViews.Clear();
    }

    private void ClearLines()
    {
        if (lineContainer == null) return;
        for (int i = lineContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(lineContainer.GetChild(i).gameObject);
        }
    }

    private void InitPlayerPosition()
    {
        List<LDY_MapNode> nodes = mapManager.Nodes;
        if (nodes.Count == 0) return;

        int startIndex = nodes.FindIndex(n => n.type == LDY_NodeType.Start);
        if (startIndex < 0) startIndex = 0;

        // ★ [문제 발생 지점]
        currentPlayerNodeIndex = (mapManager.CurrentNodeIndex >= 0 && mapManager.CurrentNodeIndex < nodes.Count)
            ? mapManager.CurrentNodeIndex
            : startIndex;
    }
    public void OnPlayerArrivedAt(int nodeIndex)
    {
        currentPlayerNodeIndex = nodeIndex;
        RefreshAll();
    }

    public bool IsPlayerAt(int nodeIndex) => nodeIndex == currentPlayerNodeIndex;

    private void SpawnNodeViews()
    {
        List<LDY_MapNode> nodes = mapManager.Nodes;
        for (int i = 0; i < nodes.Count; i++)
        {
            LDY_MapNodeView view = Instantiate(nodeViewPrefab, nodeContainer);
            view.Initialize(mapManager, nodes[i], i, theme, this);
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

            float thickness = theme != null ? theme.lineThickness : 2f;

            // 1. 글로우 라인
            Image glow = Instantiate(linePrefab, lineContainer);
            SetupLine(glow.rectTransform, from, to, thickness * (theme != null ? theme.lineGlowWidthMultiplier : 6f));
            StyleLineGlow(glow);

            // 2. 메인 실선 라인
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

        // Anchor & Pivot을 중앙(0.5, 0.5)으로 설정
        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.pivot = new Vector2(0.5f, 0.5f);

        lineRect.anchoredPosition = (from + to) * 0.5f;
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