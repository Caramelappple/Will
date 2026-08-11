using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using _Scripts.LDY.Save;
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
    private bool isNodeActionInProgress = false;

    [Header("맵 UI Container Transform (노드 좌표 UV 변환용)")]
    [SerializeField] private RectTransform nodeContainerRect;

    [Header("맵 씬 이름")]
    [SerializeField] private string mapSceneName = "MapScene";

    [Header("에디터에서 편집할 챕터 & 스테이지 번호")]
    [SerializeField] private int editorChapterIndex = 1;
    [SerializeField] private int editorStageIndex = 1;
    public int EditorChapterIndex => editorChapterIndex;
    public int EditorStageIndex => editorStageIndex;

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

    private int previousNodeIndex = -1;

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

        SetChapterAndStage(editorChapterIndex, editorStageIndex);
    }

    /// <summary>
    /// 이어할 런이 있으면 되돌린다.
    ///
    /// 이 오브젝트는 DontDestroyOnLoad라 Start가 게임당 한 번만 돈다.
    /// 맵으로 돌아올 때마다 다시 불러오지 않는 것은 의도한 것이다.
    ///
    /// ⚠ 지금은 "새 런 시작"과 "이어하기"를 가르는 진입점이 없다.
    /// 덱빌드를 마치고 맵에 처음 들어오는 흐름에서도 세이브가 있으면 그쪽이 이겨서
    /// 방금 만든 덱을 덮어쓴다. 메인 메뉴에 이어하기가 생기면 이 호출은 그리로 옮겨야 한다.
    /// </summary>
    private IEnumerator Start()
    {
        // Awake에서 파괴 예약된 중복 인스턴스는 여기까지 오면 안 된다.
        if (Instance != this) yield break;

        // 덱과 해금 매니저가 Awake를 마칠 때까지 한 프레임 기다린다.
        yield return null;

        if (LDY_SaveService.Instance.HasRun)
            LDY_SaveService.Instance.LoadRun();
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
        if (scene.name.Equals(mapSceneName, StringComparison.OrdinalIgnoreCase))
        {
            StartCoroutine(Co_DelayedInitToken());
        }
    }

    private IEnumerator Co_DelayedInitToken()
    {
        yield return null;

        nodeContainerRect = null;
        EnsureNodeContainer();

        onMapChanged?.Invoke();

        if (nodeContainerRect != null && Nodes.Count > 0)
        {
            if (IsValidIndex(previousNodeIndex) && IsValidIndex(CurrentNodeIndex) && previousNodeIndex != CurrentNodeIndex)
            {
                SetTokenPositionToNode(previousNodeIndex);

                List<Vector2> path = new List<Vector2>
                {
                    Nodes[previousNodeIndex].position,
                    Nodes[CurrentNodeIndex].position
                };

                if (ldy_play != null)
                {
                    ldy_play.MoveAlongPath(path, () =>
                    {
                        previousNodeIndex = CurrentNodeIndex;
                        onMapChanged?.Invoke();
                    }, 0.8f);
                }
            }
            else
            {
                int spawnIndex = IsValidIndex(CurrentNodeIndex) ? CurrentNodeIndex : 0;
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

    public void SetChapterAndStage(int chapter, int stage)
    {
        editorChapterIndex = chapter;
        editorStageIndex = stage;
        currentChapter = chapter;
        currentStage = stage;

        LoadChapterToEditor(currentChapter);

        Nodes.Clear();
        activeNodeIndex = -1;
        CurrentNodeIndex = -1;
        previousNodeIndex = -1;

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
                    Nodes[c.fromIndex].nextIndices.Add(c.toIndex);
            }
        }

        int targetIndex = Mathf.Clamp(stage - 1, 0, Nodes.Count - 1);

        for (int i = 0; i < targetIndex; i++)
        {
            Nodes[i].isCleared = true;
            Nodes[i].isUnlocked = true;

            foreach (int next in Nodes[i].nextIndices)
            {
                if (IsValidIndex(next))
                    Nodes[next].isUnlocked = true;
            }
        }

        if (Nodes.Count > 0)
        {
            Nodes[targetIndex].isUnlocked = true;
            CurrentNodeIndex = targetIndex;
            previousNodeIndex = targetIndex > 0 ? targetIndex - 1 : -1;
        }

        onStageChanged?.Invoke();
        onMapChanged?.Invoke();
    }

    /// <summary>
    /// 세이브에서 읽은 진행도를 되돌린다.
    ///
    /// SetChapterAndStage로 노드 뼈대를 세운 뒤 노드별 상태를 덮어쓴다.
    /// 뼈대만으로 끝내지 않는 것은, 그쪽이 "stage 직전까지 순서대로 클리어"를 가정하기 때문이다.
    /// 맵에 분기가 생기면 어느 갈래를 지나왔는지는 저장된 인덱스로만 알 수 있다.
    /// </summary>
    public void RestoreProgress(
        int chapter,
        int stage,
        IReadOnlyList<int> clearedIndices,
        IReadOnlyList<int> unlockedIndices,
        int currentNode,
        int battleEntries)
    {
        SetChapterAndStage(chapter, stage);

        if (Nodes.Count == 0)
        {
            Debug.LogWarning("[LDY_MapManager] 노드가 없어 진행도를 되돌리지 못했습니다.");
            return;
        }

        foreach (LDY_MapNode node in Nodes)
        {
            node.isCleared = false;
            node.isUnlocked = false;
        }

        ApplyNodeFlags(clearedIndices, cleared: true);
        ApplyNodeFlags(unlockedIndices, cleared: false);

        CurrentNodeIndex = IsValidIndex(currentNode) ? currentNode : -1;

        // 불러온 직후에는 토큰이 이동 연출 없이 제자리에 서야 한다.
        // previousNodeIndex가 다르면 Co_DelayedInitToken이 지나온 적 없는 길을 걸어간다.
        previousNodeIndex = CurrentNodeIndex;

        // 저장은 스테이지 경계에서만 일어나므로, 불러온 시점에 들어가 있는 스테이지는 없다.
        activeNodeIndex = -1;

        BattleEntryCount = Mathf.Max(0, battleEntries);

        onStageChanged?.Invoke();
        onMapChanged?.Invoke();
    }

    private void ApplyNodeFlags(IReadOnlyList<int> indices, bool cleared)
    {
        if (indices == null) return;

        foreach (int index in indices)
        {
            if (!IsValidIndex(index)) continue;

            if (cleared) Nodes[index].isCleared = true;
            else Nodes[index].isUnlocked = true;
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
            Debug.LogWarning($"[LDY_MapManager] ChapterMaps 목록에 {chapter}번 챕터 데이터가 존재하지 않습니다.");
        }
    }

    public LDY_ChapterMapData GetCurrentChapterData()
    {
        if (chapterMaps == null || chapterMaps.Count == 0) return null;
        return chapterMaps.Find(c => c.chapter == currentChapter);
    }

    public void BuildNodes()
    {
        Nodes.Clear();
        activeNodeIndex = -1;

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
    }

    public void OnNodeClicked(int index)
    {
        OnNodeClicked(index, GetNodeScreenUV(index));
    }

    public void OnNodeClicked(int index, Vector2 screenUV)
    {
        if (!IsValidIndex(index)) return;

        if (isNodeActionInProgress)
        {
            Debug.LogWarning("[LDY_MapManager] 이전 노드 처리가 끝나지 않아 클릭을 무시합니다.");
            return;
        }

        LDY_MapNode node = Nodes[index];

        if (!node.isUnlocked)
        {
            Debug.LogWarning($"[LDY_MapManager] {index}번 노드는 아직 해금되지 않았습니다.");
            return;
        }

        if (!isTest && node.isCleared)
        {
            Debug.LogWarning($"[LDY_MapManager] {index}번 노드는 이미 클리어한 스테이지입니다.");
            return;
        }

        isNodeActionInProgress = true;

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
                isNodeActionInProgress = false;
            }, 0.6f);
        }
        else
        {
            ExecuteNodeAction(index, node, screenUV);
            isNodeActionInProgress = false;
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

        if (Nodes[index].isCleared)
        {
            Debug.LogWarning($"[LDY_MapManager] {index}번 노드는 이미 클리어 처리되었습니다.");
            return;
        }

        // 1. 현재 노드 클리어
        Nodes[index].isCleared = true;

        // 2. 스테이지 클리어 및 보상 지급 연동 ★
        if (Nodes[index].type == LDY_NodeType.Battle)
        {
            int clearedChapter = currentChapter;
            int clearedStage = currentStage;

            // 스테이지 완료 로그 출력
            Debug.Log($"[LDY_MapManager] 스테이지 클리어 완료! (Chapter: {clearedChapter}, Stage: {clearedStage})");

            // ★ 보상 지급 요청 (현재 클리어한 챕터와 스테이지 전달)
            TriggerStageReward(clearedChapter, clearedStage);

            // 다음 진행을 위해 스테이지 카운트 증가
            currentStage++;
            editorStageIndex = currentStage;
            onStageChanged?.Invoke();
        }
        else if (Nodes[index].type == LDY_NodeType.Boss)
        {
            int clearedChapter = currentChapter;
            int clearedStage = currentStage;

            Debug.Log($"[LDY_MapManager] 보스 스테이지 클리어 완료! (Chapter: {clearedChapter}, Stage: {clearedStage})");

            // ★ 보스 보상 지급 요청
            TriggerStageReward(clearedChapter, clearedStage);

            currentChapter++;
            currentStage = 1;
            editorChapterIndex = currentChapter;
            editorStageIndex = currentStage;

            CurrentNodeIndex = -1;
            previousNodeIndex = -1;
            onStageChanged?.Invoke();

            LoadChapterToEditor(currentChapter);
            BuildNodes();
            return;
        }

        // 3. 연결된 다음 노드 해금
        int firstNextIndex = -1;
        if (Nodes[index].nextIndices != null && Nodes[index].nextIndices.Count > 0)
        {
            foreach (int next in Nodes[index].nextIndices)
            {
                if (IsValidIndex(next))
                {
                    Nodes[next].isUnlocked = true;
                    if (firstNextIndex < 0) firstNextIndex = next;
                }
            }
        }

        activeNodeIndex = -1;

        // 4. 다음 위치 지정
        if (firstNextIndex >= 0)
        {
            previousNodeIndex = index;
            CurrentNodeIndex = firstNextIndex;
        }

        // 5. UI 및 데이터 갱신 이벤트 호출
        onMapChanged?.Invoke();
    }

    /// <summary>
    /// KTH_GiveReward 또는 KTH_Reward에 보상 지급을 연동하는 전용 헬퍼 메서드
    /// </summary>
    private void TriggerStageReward(int chapter, int stage)
    {
        KTH_GiveReward giveReward = FindFirstObjectByType<KTH_GiveReward>();
        if (giveReward != null)
        {
            giveReward.GiveStageReward(chapter, stage);
        }
        else if (KTH_Reward.Instance != null)
        {
            KTH_Reward.Instance.UnlockByStage(chapter, stage);
        }
        else
        {
            Debug.LogWarning("[LDY_MapManager] 씬에서 KTH_GiveReward 또는 KTH_Reward 인스턴스를 찾을 수 없어 보상이 지급되지 않았습니다.");
        }
    }

    private void SetTokenPositionToNode(int nodeIndex)
    {
        if (!IsValidIndex(nodeIndex)) return;

        string currentSceneName = SceneManager.GetActiveScene().name;
        if (!currentSceneName.Equals(mapSceneName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        EnsureNodeContainer();

        Transform targetParent = nodeContainerRect != null ? nodeContainerRect : FindFirstObjectByType<Canvas>()?.transform;
        if (targetParent == null) return;

        if (ldy_play == null || !ldy_play.gameObject.scene.IsValid() || ldy_play.gameObject.scene != SceneManager.GetActiveScene())
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
        if (nodeContainerRect == null || !nodeContainerRect.gameObject.scene.IsValid() || nodeContainerRect.gameObject.scene != SceneManager.GetActiveScene())
        {
            GameObject containerObj = GameObject.Find("NodeContainer");
            if (containerObj != null && containerObj.scene == SceneManager.GetActiveScene())
            {
                nodeContainerRect = containerObj.GetComponent<RectTransform>();
            }
        }
    }

    private void EnterStage(int index, LDY_MapNode node, string fallbackSceneName, Vector2 screenUV)
    {
        LDY_StageSO stage = _stageRouter?.Resolve(index, node.type);

        if (stage == null)
        {
            Debug.LogError($"[LDY_MapManager] {currentChapter}챕터 {index}번 노드({node.type})에 배정된 StageSO가 StageRouter에 없습니다!");
            return;
        }

        CurrentStageSO = stage;
        LDY_StageSelection.Select(stage);

        if (string.IsNullOrEmpty(stage.SceneName))
        {
            Debug.LogError($"[LDY_MapManager] '{stage.name}' StageSO에 이동할 SceneName이 비어있습니다!");
            return;
        }

        RequestSceneLoad(stage.SceneName, screenUV, node.type);
    }

    private void RequestSceneLoad(string targetSceneName, Vector2 screenUV, LDY_NodeType nodeType)
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("[LDY_MapManager] 이동할 씬 이름이 전달되지 않았습니다.");
            return;
        }

        Debug.Log($"[LDY_MapManager] 라우터 기반 씬 이동 시작 -> Target Scene: {targetSceneName}");

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