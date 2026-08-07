using System; // CS0246(Action) 에러 방지용
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using _Scripts.LDY.Stage;

[System.Serializable]
public class LDY_MapNodeUnityEvent : UnityEvent<LDY_MapNode> { }

[System.Serializable]
public class LDY_ChapterMapData
{
    [Tooltip("몇 챕터용 맵인지 설정 (예: 1, 2, 3...)")]
    public int chapter = 1;

    [Header("별자리 노드 (좌표/타입)")]
    public Vector2[] nodePositions = new Vector2[0];
    public LDY_NodeType[] nodeTypes = new LDY_NodeType[0];

    [Header("노드 연결 (분기 가능)")]
    public LDY_NodeConnection[] connections = new LDY_NodeConnection[0];

    public LDY_ChapterMapData() { }

    public LDY_ChapterMapData(int chapter, Vector2[] positions, LDY_NodeType[] types, LDY_NodeConnection[] connections)
    {
        this.chapter = chapter;
        this.nodePositions = positions ?? new Vector2[0];
        this.nodeTypes = types ?? new LDY_NodeType[0];
        this.connections = connections ?? new LDY_NodeConnection[0];
    }
}

public class LDY_MapManager : MonoBehaviour
{
    public static LDY_MapManager Instance { get; private set; }

    public LDY_StageSO CurrentStageSO { get; private set; }

    [Header("테스트용")]
    [SerializeField] private bool isTest = false;
    private bool isWaitingSecondClick = false;

    [Header("맵 UI Container Transform (노드 좌표 UV 변환용)")]
    [SerializeField] private RectTransform nodeContainerRect;

    [Header("맵 씬 이름")]
    [SerializeField] private string mapSceneName = "MapScene";

    [Header("에디터에서 편집할 챕터 번호")]
    [SerializeField] private int editorChapterIndex = 1;
    public int EditorChapterIndex => editorChapterIndex;

    [Header("별자리 노드 (에디터 연동용)")]
    [SerializeField] private Vector2[] nodePositions = new Vector2[0];
    [SerializeField] private LDY_NodeType[] nodeTypes = new LDY_NodeType[0];
    [SerializeField] private LDY_NodeConnection[] connections = new LDY_NodeConnection[0];

    [Header("챕터별 맵 구조 데이터 목록")]
    [SerializeField] private List<LDY_ChapterMapData> chapterMaps = new List<LDY_ChapterMapData>();

    [Header("씬 전환용 기본 씬 이름")]
    [SerializeField] private string battleSceneName = "BattleScene";
    [SerializeField] private string bossSceneName = "BossScene";

    [Header("스테이지 배정 (LDY_StageRouter 연결)")]
    [SerializeField] private MonoBehaviour stageRouterSource;
    private LDY_IStageRouter _stageRouter;

    [Header("플레이어 토큰")]
    [SerializeField] private LDY_MapPlayerToken playerTokenPrefab;
    [SerializeField] private LDY_MapPlayerToken ldy_play;

    [Header("Shop / Event 노드 진입 시 호출되는 이벤트")]
    public LDY_MapNodeUnityEvent onShopNodeSelected;
    public LDY_MapNodeUnityEvent onEventNodeSelected;

    [Header("스테이지 변경 이벤트")]
    public UnityEvent onStageChanged = new UnityEvent();

    [Header("노드 상태가 바뀔 때마다 호출 (UI 갱신용)")]
    public UnityEvent onMapChanged;

    [SerializeField] private int currentChapter = 1;
    [SerializeField] private int currentStage = 1;

    public int CurrentChapter => currentChapter;
    public int CurrentStage => currentStage;

    [SerializeField] private int activeNodeIndex = -1;
    public int ActiveNodeIndex => activeNodeIndex;
    public int CurrentNodeIndex { get; private set; } = -1;

    private int previousNodeIndex = -1; // 이동 연출용 출발지 기록

    public List<LDY_MapNode> Nodes { get; private set; } = new List<LDY_MapNode>();

    public LDY_NodeConnection[] Connections
    {
        get
        {
            var data = GetCurrentChapterData();
            return data != null ? data.connections : connections;
        }
    }

    public int BattleEntryCount { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ResolveStageRouter();
        LoadChapterToEditor(currentChapter);
        BuildNodes();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureNodeContainer();
        if (nodeContainerRect != null && Nodes.Count > 0)
        {
            // 씬 진입 시 토큰을 '직전 노드' 위치에 배치해서 이동 애니메이션 준비
            int spawnIndex = IsValidIndex(previousNodeIndex) ? previousNodeIndex : CurrentNodeIndex;
            if (IsValidIndex(spawnIndex))
            {
                SetTokenPositionToNode(spawnIndex);
            }
        }
    }

    private void ResolveStageRouter()
    {
        if (stageRouterSource != null)
            _stageRouter = stageRouterSource as LDY_IStageRouter;

        if (_stageRouter == null && stageRouterSource != null)
            Debug.LogError($"[LDY_MapManager] {stageRouterSource.GetType().Name}은(는) LDY_IStageRouter를 구현하지 않습니다.", this);

        if (_stageRouter == null)
            _stageRouter = GetComponent<LDY_IStageRouter>();
    }

    private void OnValidate()
    {
        SaveCurrentEditorToChapter();
    }

    public void SaveCurrentEditorToChapter()
    {
        if (chapterMaps == null)
            chapterMaps = new List<LDY_ChapterMapData>();

        if (nodePositions != null)
        {
            if (nodeTypes == null || nodeTypes.Length != nodePositions.Length)
            {
                System.Array.Resize(ref nodeTypes, nodePositions.Length);
            }
        }
        else
        {
            nodePositions = new Vector2[0];
            nodeTypes = new LDY_NodeType[0];
        }

        if (connections == null) connections = new LDY_NodeConnection[0];

        var targetData = chapterMaps.Find(c => c.chapter == editorChapterIndex);
        if (targetData == null)
        {
            targetData = new LDY_ChapterMapData(editorChapterIndex, (Vector2[])nodePositions.Clone(), (LDY_NodeType[])nodeTypes.Clone(), (LDY_NodeConnection[])connections.Clone());
            chapterMaps.Add(targetData);
        }
        else
        {
            targetData.nodePositions = (Vector2[])nodePositions.Clone();
            targetData.nodeTypes = (LDY_NodeType[])nodeTypes.Clone();
            targetData.connections = (LDY_NodeConnection[])connections.Clone();
        }
    }

    public void LoadChapterToEditor(int chapter)
    {
        if (chapterMaps == null)
            chapterMaps = new List<LDY_ChapterMapData>();

        var targetData = chapterMaps.Find(c => c.chapter == chapter);
        if (targetData != null)
        {
            nodePositions = targetData.nodePositions != null ? (Vector2[])targetData.nodePositions.Clone() : new Vector2[0];
            nodeTypes = targetData.nodeTypes != null ? (LDY_NodeType[])targetData.nodeTypes.Clone() : new LDY_NodeType[0];
            connections = targetData.connections != null ? (LDY_NodeConnection[])targetData.connections.Clone() : new LDY_NodeConnection[0];
        }
        else
        {
            nodePositions = new Vector2[0];
            nodeTypes = new LDY_NodeType[0];
            connections = new LDY_NodeConnection[0];
        }
    }

    public LDY_ChapterMapData GetCurrentChapterData()
    {
        if (chapterMaps == null || chapterMaps.Count == 0) return null;

        var data = chapterMaps.Find(c => c.chapter == currentChapter);
        return data ?? chapterMaps[0];
    }

    public void BuildNodes()
    {
        Nodes.Clear();
        activeNodeIndex = -1;

        EnsureNodeContainer();

        LDY_ChapterMapData currentMapData = GetCurrentChapterData();

        Vector2[] posArray = (currentMapData != null && currentMapData.nodePositions != null) ? currentMapData.nodePositions : nodePositions;
        LDY_NodeType[] typeArray = (currentMapData != null && currentMapData.nodeTypes != null) ? currentMapData.nodeTypes : nodeTypes;
        LDY_NodeConnection[] connArray = (currentMapData != null && currentMapData.connections != null) ? currentMapData.connections : connections;

        if (posArray == null || posArray.Length == 0)
        {
            Debug.LogWarning($"[LDY_MapManager] 챕터 {currentChapter}의 맵 데이터가 비어있습니다.");
            onMapChanged?.Invoke();
            return;
        }

        for (int i = 0; i < posArray.Length; i++)
        {
            LDY_NodeType type = (typeArray != null && i < typeArray.Length) ? typeArray[i] : LDY_NodeType.Battle;
            Nodes.Add(new LDY_MapNode(posArray[i], type));
        }

        if (connArray != null)
        {
            foreach (LDY_NodeConnection c in connArray)
            {
                if (!IsValidIndex(c.fromIndex) || !IsValidIndex(c.toIndex)) continue;
                if (!Nodes[c.fromIndex].nextIndices.Contains(c.toIndex))
                {
                    Nodes[c.fromIndex].nextIndices.Add(c.toIndex);
                }
            }
        }

        if (Nodes.Count == 0) return;

        if (!IsValidIndex(CurrentNodeIndex))
        {
            int startIndex = Nodes.FindIndex(n => n.type == LDY_NodeType.Start);
            if (startIndex < 0) startIndex = 0;

            Nodes[startIndex].isUnlocked = true;
            CurrentNodeIndex = startIndex;

            if (Nodes[startIndex].type == LDY_NodeType.Start)
            {
                CompleteNode(startIndex);
                return;
            }
        }
        else
        {
            Nodes[CurrentNodeIndex].isUnlocked = true;
        }

        onMapChanged?.Invoke();

        int spawnIndex = IsValidIndex(previousNodeIndex) ? previousNodeIndex : CurrentNodeIndex;
        SetTokenPositionToNode(spawnIndex);
    }

    public void OnNodeClicked(int index)
    {
        OnNodeClicked(index, GetNodeScreenUV(index));
    }

    public void OnNodeClicked(int index, Vector2 screenUV)
    {
        if (!IsValidIndex(index)) return;

        LDY_MapNode node = Nodes[index];

        if (!node.isUnlocked)
        {
            Debug.LogWarning($"[LDY_MapManager] {index}번 노드는 해금되지 않았습니다.");
            return;
        }

        int prevIndex = CurrentNodeIndex;
        CurrentNodeIndex = index;

        if (ldy_play != null && IsValidIndex(prevIndex))
        {
            List<Vector2> path = new List<Vector2>
            {
                Nodes[prevIndex].position,
                Nodes[index].position
            };

            ldy_play.MoveAlongPath(path, () =>
            {
                ExecuteNodeAction(index, node, screenUV);
            }, 0.6f);
        }
        else
        {
            ExecuteNodeAction(index, node, screenUV);
        }
    }

    private void ExecuteNodeAction(int index, LDY_MapNode node, Vector2 screenUV)
    {
        activeNodeIndex = index;
        CurrentNodeIndex = index;
        SetTokenPositionToNode(index);

        Debug.Log($"[LDY_MapManager] 클릭된 노드 index: {index}, type: {node.type}");

        switch (node.type)
        {
            case LDY_NodeType.Battle:
                BattleEntryCount++;
                EnterStage(index, node, battleSceneName, screenUV);
                break;
            case LDY_NodeType.Boss:
                EnterStage(index, node, bossSceneName, screenUV);
                break;
            case LDY_NodeType.Shop:
                RequestPopup(screenUV, node, onShopNodeSelected);
                break;
            case LDY_NodeType.Event:
                RequestPopup(screenUV, node, onEventNodeSelected);
                break;
            case LDY_NodeType.Start:
                CompleteNode(index);
                break;
        }
    }

    private Vector2 GetNodeScreenUV(int nodeIndex)
    {
        if (!IsValidIndex(nodeIndex)) return new Vector2(0.5f, 0.5f);

        Vector2 nodePos = Nodes[nodeIndex].position;

        EnsureNodeContainer();

        if (nodeContainerRect != null)
        {
            Canvas canvas = nodeContainerRect.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                RectTransform canvasRect = (RectTransform)canvas.transform;
                Vector3 worldPos = nodeContainerRect.TransformPoint(nodePos);
                Vector3 localPos = canvasRect.InverseTransformPoint(worldPos);
                Rect rect = canvasRect.rect;

                return new Vector2(
                    (localPos.x - rect.xMin) / rect.width,
                    (localPos.y - rect.yMin) / rect.height
                );
            }
        }

        return new Vector2(0.5f, 0.5f);
    }

    public void CompleteActiveNode()
    {
        if (activeNodeIndex >= 0) CompleteNode(activeNodeIndex);
    }

    public void CompleteActiveNodeAndReturnToMap()
    {
        CompleteActiveNode();

        if (string.IsNullOrEmpty(mapSceneName))
        {
            Debug.LogWarning("[LDY_MapManager] mapSceneName이 비어 있습니다.", this);
            return;
        }

        SceneManager.LoadScene(mapSceneName);
    }

    public void CompleteNode(int index)
    {
        if (!IsValidIndex(index)) return;

        Nodes[index].isCleared = true;

        if (KTH_Reward.Instance != null)
        {
            KTH_UnlockResult result = KTH_Reward.Instance.UnlockByStage(currentChapter, currentStage);
            if (result.HasAnyNewUnlock)
            {
                Debug.Log($"===== Stage {currentStage} Reward =====");
                foreach (var piece in result.newlyUnlockedPieces) Debug.Log($"새 기물 해금 : {piece}");
                foreach (var will in result.newlyUnlockedWills) Debug.Log($"새 유언 해금 : {will}");
            }
        }

        if (Nodes[index].type == LDY_NodeType.Battle)
        {
            currentStage++;
            onStageChanged?.Invoke();
        }

        if (Nodes[index].type == LDY_NodeType.Boss)
        {
            currentChapter++;
            currentStage = 1;
            CurrentNodeIndex = -1;
            previousNodeIndex = -1;
            onStageChanged?.Invoke();

            LoadChapterToEditor(currentChapter);
            BuildNodes();
            return;
        }

        // 연결된 다음 노드들 해금
        int firstNextIndex = -1;
        foreach (int next in Nodes[index].nextIndices)
        {
            if (IsValidIndex(next))
            {
                Nodes[next].isUnlocked = true;
                if (firstNextIndex < 0) firstNextIndex = next;
            }
        }

        if (activeNodeIndex == index)
            activeNodeIndex = -1;

        // ★ [핵심] 자동으로 다음 노드로 부드럽게 이동 연출하는 로직
        if (firstNextIndex >= 0)
        {
            previousNodeIndex = index;            // 출발지점 기억
            CurrentNodeIndex = firstNextIndex;   // 목적지 설정

            onMapChanged?.Invoke(); // UI 갱신 (선 및 노드 해금)

            SetTokenPositionToNode(previousNodeIndex); // 출발점에 토큰 강제 고정

            List<Vector2> path = new List<Vector2>
            {
                Nodes[previousNodeIndex].position,
                Nodes[CurrentNodeIndex].position
            };

            if (ldy_play != null)
            {
                // 출발 노드에서 목적 노드로 0.8초간 이동 모션 실행
                ldy_play.MoveAlongPath(path, () =>
                {
                    previousNodeIndex = CurrentNodeIndex; // 이동 완료 후 업데이트
                }, 0.8f);
            }
        }
        else
        {
            onMapChanged?.Invoke();
            SetTokenPositionToNode(CurrentNodeIndex);
        }
    }

    private void SetTokenPositionToNode(int nodeIndex)
    {
        if (!IsValidIndex(nodeIndex)) return;

        EnsureNodeContainer();

        Transform targetParent = nodeContainerRect != null ? nodeContainerRect : FindFirstObjectByType<Canvas>()?.transform;
        if (targetParent == null) return;

        if (ldy_play == null || !ldy_play.gameObject.scene.IsValid())
        {
            ldy_play = FindFirstObjectByType<LDY_MapPlayerToken>();

            if (ldy_play == null && playerTokenPrefab != null)
            {
                ldy_play = Instantiate(playerTokenPrefab, targetParent);
            }
        }

        if (ldy_play != null)
        {
            if (ldy_play.transform.parent != targetParent)
            {
                ldy_play.transform.SetParent(targetParent, false);
            }

            ldy_play.SetPosition(Nodes[nodeIndex].position);
            ldy_play.BringToFront();
        }
    }

    private bool IsValidIndex(int index) => index >= 0 && index < Nodes.Count;

    private void EnsureNodeContainer()
    {
        if (nodeContainerRect == null || !nodeContainerRect.gameObject.scene.IsValid())
        {
            GameObject containerObj = GameObject.Find("NodeContainer");
            if (containerObj != null)
            {
                nodeContainerRect = containerObj.GetComponent<RectTransform>();
            }
        }
    }

    private void EnterStage(int index, LDY_MapNode node, string fallbackSceneName, Vector2 screenUV)
    {
        LDY_StageSO stage = _stageRouter?.Resolve(index, node.type);

        if (stage != null)
        {
            LDY_StageSelection.Select(stage);

            if (!string.IsNullOrEmpty(stage.SceneName))
            {
                RequestSceneLoad(stage.SceneName, screenUV, node.type);
                return;
            }
        }

        RequestSceneLoad(fallbackSceneName, screenUV, node.type);
    }

    private void RequestSceneLoad(string defaultSceneName, Vector2 screenUV, LDY_NodeType nodeType)
    {
        string targetSceneName = defaultSceneName;

        if (_stageRouter != null)
        {
            CurrentStageSO = _stageRouter.Resolve(activeNodeIndex, nodeType);

            if (CurrentStageSO != null && !string.IsNullOrEmpty(CurrentStageSO.SceneName))
            {
                targetSceneName = CurrentStageSO.SceneName;
            }
        }

        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("[LDY_MapManager] 이동할 씬 이름이 설정되지 않았습니다.");
            return;
        }

        KTH_LoadingSceneController.LoadScene(targetSceneName);
    }

    private void RequestPopup(Vector2 screenUV, LDY_MapNode node, LDY_MapNodeUnityEvent popupEvent)
    {
        if (LDY_SceneTransition.Instance != null)
            LDY_SceneTransition.Instance.PlayIrisCloseThen(screenUV, node.type, () =>
            {
                popupEvent?.Invoke(node);
                CompleteActiveNode();
            });
        else
        {
            popupEvent?.Invoke(node);
            CompleteActiveNode();
        }
    }
}