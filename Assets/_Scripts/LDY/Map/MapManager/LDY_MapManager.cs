using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using _Scripts.LDY.Effect;
using _Scripts.LDY.Save;
using _Scripts.LDY.Stage;
using _Scripts.LSO.Reward;
using _Scripts.LDY;

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

    /// <summary>
    /// Reload Domain을 끈 에디터에서는 static이 플레이를 멈춰도 살아남는다.
    /// 지난 플레이의 값이 남아 있으면 두 번째 실행부터 엉뚱하게 동작하므로,
    /// 씬이 로드되기 전에 직접 비운다. LDY_RunSeed와 같은 이유다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    /// <summary>
    /// 씬을 넘기기 전에 전투 연출을 기다려주는 상한(초).
    /// 기획 수치가 아니라 멈춤 방지선이라 인스펙터에 열지 않는다.
    /// 한 번의 공격 연출은 0.3초 남짓이므로 정상적인 경우 이 값에 닿지 않는다.
    /// </summary>
    private const float SceneLoadAnimationWaitTimeout = 3f;

    /// <summary>토큰이 노드 하나를 건너가는 데 걸리는 시간(초).</summary>
    private const float TokenMoveDuration = 0.6f;

    /// <summary>
    /// 승리 뒤 보드 회전 연출을 기다려주는 상한(초).
    /// 기획 수치가 아니라 멈춤 방지선이라 인스펙터에 열지 않는다.
    /// 연출 전체가 기물 정리 + 회전 + 뜸으로 2초 남짓이므로 정상적인 경우 이 값에 닿지 않는다.
    /// </summary>
    private const float BoardFlipWaitTimeout = 6f;

    public LDY_StageSO CurrentStageSO { get; private set; }

    [Header("테스트용")]
    [SerializeField] private bool isTest = false;
    private bool isWaitingSecondClick = false;
    private bool isNodeActionInProgress = false;

    [Header("맵 UI Container Transform (노드 좌표 UV 변환용)")]
    [SerializeField] private RectTransform nodeContainerRect;

    [Header("맵 씬 이름")]
    [SerializeField] private string mapSceneName = "MapScene";

    [Header("패배 시 이동할 씬 이름 (통합 패배 씬)")]
    [SerializeField] private string deathSceneName = "KTH_Death Scene";

    [Header("보스 승리 시 이동할 씬 이름 (비워두면 맵으로 복귀)")]
    [SerializeField] private string bossClearSceneName = "KTH_Boss Clear Scene";

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

    // 전투·보스 노드가 열 씬은 StageSO가 들고 있다(LDY_StageSO.SceneName).
    // 맵에도 씬 이름을 적어두면 스테이지마다 씬이 갈릴 때 어느 쪽이 이겼는지 알 수 없고,
    // 한쪽만 고쳐놓고 왜 안 바뀌는지 찾게 된다. 여기서는 들고 있지 않는다.

    [Header("스테이지 배정 (LDY_StageRouter 연결)")]
    [SerializeField] private MonoBehaviour stageRouterSource;
    private LDY_IStageRouter _stageRouter;

    [Header("노드 선택 연출 (링)")]
    [Tooltip("켜면 토큰이 도착한 뒤에 링을 그린다 (이동 → 원 → 진입).\n" +
             "끄면 이동과 동시에 그린다 (클릭한 순간 바로 반응이 보인다).")]
    [SerializeField] private bool playSelectRingAfterMove = false;

    [Tooltip("링 연출에 보장해줄 시간(초). 이 시간이 지나야 씬으로 넘어간다.\n" +
             "이동과 동시에 그리는 모드에서는 이동에 쓴 시간을 여기서 뺀다 (헛기다리지 않는다).\n" +
             "링이 다 그려지는 데는 drawDuration + holdDuration 만큼 걸린다. 기본값 기준 약 0.55초.")]
    [SerializeField, Min(0f)] private float selectRingHoldDuration = 0.6f;

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

    // 보상 상자는 전투 씬에 있고 이 매니저는 씬을 넘어다닌다.
    // 인스펙터 참조로 두면 씬을 넘기는 순간 끊기므로 LSO_RewardBox.Instance로 찾는다.

    public int CurrentChapter => currentChapter;
    public int CurrentStage => currentStage;

    [SerializeField] private int activeNodeIndex = -1;
    public int ActiveNodeIndex => activeNodeIndex;
    public int CurrentNodeIndex { get; private set; } = -1;

    private int previousNodeIndex = -1;

    private bool waitingForRewardBeforeMapReturn = false;

    /// <summary>전투 연출을 기다리며 씬 이동을 예약해둔 상태인지. 같은 이동이 두 번 걸리는 것을 막는다.</summary>
    private bool _sceneLoadPending = false;

    /// <summary>보드 회전 연출이 끝나기를 기다리는 중인지. 같은 노드가 두 번 완료되는 것을 막는다.</summary>
    private bool _boardFlipPending = false;

    /// <summary>
    /// 보상 대기 중일 때, 보상 UI가 끝난 뒤 어떤 씬으로 갈지 판단하기 위해
    /// 클리어한 노드의 타입을 잠깐 보관해두는 필드.
    /// </summary>
    private LDY_NodeType _pendingClearedNodeType = LDY_NodeType.Battle;

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

    /// <summary>
    /// 맵에 아직 한 번도 들어오지 않았는지. 첫 진입은 TryRestoreOnEntry가 맡으므로 자동저장하지 않는다.
    /// </summary>
    private bool _firstMapEntryPending = true;

    /// <summary>
    /// 방금 클리어한 Battle 노드의 인덱스. 맵으로 돌아왔을 때 그 노드에만 연출을 한 번 틀기 위한 표시다.
    ///
    /// 세이브 대상이 아니다. 씬 전환 한 번을 건너가는 신호일 뿐이라 파일에 남기지 않는다.
    /// 이 매니저가 DontDestroyOnLoad라 전투 씬을 거쳐 맵으로 돌아올 때까지 값이 그대로 살아남으므로
    /// 따로 옮겨줄 자리를 만들 필요가 없다.
    ///
    /// Battle에만 남긴다. Boss는 CompleteNode가 챕터를 넘기면서 노드를 통째로 다시 만들어
    /// 인덱스가 의미를 잃고, Shop/Event는 맵 씬을 떠나지 않아 이 신호가 필요 없다.
    /// </summary>
    private int _pendingClearedNodeIndex = -1;

    /// <summary>
    /// 맵이 다 자리잡은 뒤, 방금 클리어한 노드의 인덱스를 한 번만 알린다.
    /// 예전에 클리어해둔 노드는 여기에 실리지 않으므로 맵에 다시 들어와도 연출 없이 그대로 보인다.
    /// </summary>
    public event Action<int> OnNodeJustCleared;

    /// <summary>
    /// 클릭이 받아들여진 노드를 알린다. 연출(링)과 "현재 위치" 표시를 옮기는 신호다.
    /// 한 번의 클릭에 반드시 한 번만 불린다. 부르는 시점은 playSelectRingAfterMove가 정한다.
    /// 거절된 클릭에는 불리지 않는다.
    /// </summary>
    public event Action<int> OnNodeSelected;

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

    private IEnumerator Start()
    {
        if (Instance != this) yield break;

        yield return null;

        TryRestoreOnEntry();
    }

    public void TryRestoreOnEntry()
    {
        if (LDY_RunEntryState.Consume()) return;

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
        if (!scene.name.Equals(mapSceneName, StringComparison.OrdinalIgnoreCase))
            return;

        AutoSaveOnMapReentry();

        StartCoroutine(Co_InitializeMapAfterLoad());
    }

    private void AutoSaveOnMapReentry()
    {
        if (Instance != this) return;

        if (ConsumeFirstMapEntry()) return;

        Debug.Log("[LDY_MapManager] 맵 재진입 감지 — 자동저장 실행");

        LDY_SaveService.Instance.SaveRun();
    }

    private bool ConsumeFirstMapEntry()
    {
        if (!_firstMapEntryPending) return false;

        _firstMapEntryPending = false;
        return true;
    }

    private IEnumerator Co_DelayedInitToken()
    {
        yield return null;

        nodeContainerRect = null;
        EnsureNodeContainer();

        onMapChanged?.Invoke();

        if (Nodes == null || Nodes.Count == 0)
        {
            Debug.LogWarning("[LDY_MapManager] 노드가 생성되지 않아 토큰을 배치할 수 없습니다.");
            yield break;
        }

        if (!IsValidIndex(CurrentNodeIndex))
        {
            Debug.LogWarning(
                $"[LDY_MapManager] CurrentNodeIndex가 유효하지 않습니다. " +
                $"Chapter: {CurrentChapter}, Stage: {CurrentStage}"
            );

            yield break;
        }

        SetTokenPositionToNode(CurrentNodeIndex);

        yield return null;

        if (ldy_play != null)
            ldy_play.BringToFront();
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

    private IEnumerator Co_InitializeMapAfterLoad()
    {
        yield return null;

        TryRestoreOnEntry();

        yield return null;

        yield return StartCoroutine(Co_DelayedInitToken());

        // 여기까지 와야 노드 뷰 재생성(onMapChanged)이 더 이상 예약돼 있지 않다.
        // 더 이른 곳에서 알리면 방금 만든 뷰가 곧바로 다시 지워져 연출이 보이지 않는다.
        NotifyPendingClearedNode();
    }

    /// <summary>
    /// 맵이 다 자리잡은 뒤에 "방금 클리어한 노드"를 한 번 알리고 표시를 지운다.
    /// 한 번 소비하고 나면 다음 맵 진입은 이 신호를 물려받지 않는다.
    /// </summary>
    private void NotifyPendingClearedNode()
    {
        if (_pendingClearedNodeIndex < 0) return;

        int clearedIndex = _pendingClearedNodeIndex;
        _pendingClearedNodeIndex = -1;

        // 돌아온 사이에 노드 구성이 달라졌다면(챕터 전환 등) 엉뚱한 노드에 연출이 붙는다. 조용히 버린다.
        if (!IsValidIndex(clearedIndex)) return;

        OnNodeJustCleared?.Invoke(clearedIndex);
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
        previousNodeIndex = CurrentNodeIndex;
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

    private int CalculateNodeIndex(int chapter, int stage)
    {
        if (Nodes == null || Nodes.Count == 0)
            return -1;

        int index = stage - 1;
        return IsValidIndex(index) ? index : -1;
    }

    public void OnNodeClicked(int index)
    {
        OnNodeClicked(index, GetNodeScreenUV(index));
    }

    /// <summary>
    /// 지금 이 노드에 들어갈 수 있는지. OnNodeClicked이 실제로 통과시키는 조건과 같은 판단이다.
    /// 클릭 연출처럼 "이 클릭이 먹히는가"만 알고 싶은 쪽에서 미리 물어볼 수 있게 열어둔다.
    /// 상태를 바꾸지 않으므로 몇 번을 불러도 안전하다.
    /// </summary>
    public bool CanEnterNode(int index) => CanEnterNode(index, logReason: false);

    private bool CanEnterNode(int index, bool logReason)
    {
        if (!IsValidIndex(index)) return false;

        if (isNodeActionInProgress)
        {
            if (logReason) Debug.LogWarning("[LDY_MapManager] 이전 노드 처리가 끝나지 않아 클릭을 무시합니다.");
            return false;
        }

        LDY_MapNode node = Nodes[index];

        if (!node.isUnlocked)
        {
            if (logReason) Debug.LogWarning($"[LDY_MapManager] {index}번 노드는 아직 해금되지 않았습니다.");
            return false;
        }

        if (!isTest && node.isCleared)
        {
            if (logReason) Debug.LogWarning($"[LDY_MapManager] {index}번 노드는 이미 클리어한 스테이지입니다.");
            return false;
        }

        return true;
    }

    public void OnNodeClicked(int index, Vector2 screenUV)
    {
        if (!CanEnterNode(index, logReason: true)) return;

        LDY_MapNode node = Nodes[index];

        isNodeActionInProgress = true;

        int prevIndex = CurrentNodeIndex;
        CurrentNodeIndex = index;

        // Inspector에 프리팹 에셋이 연결되어 있을 수 있으므로 이동 전에 반드시
        // 현재 맵 씬의 토큰 인스턴스로 교체합니다. 프리팹 원본 Transform을
        // SetParent/이동하려 하면 Unity가 데이터 손상 방지 오류를 발생시킵니다.
        if (IsValidIndex(prevIndex))
            EnsureScenePlayerToken(prevIndex);

        // 이동과 동시에 연출을 시작하는 모드. 클릭한 순간 반응이 보인다.
        if (!playSelectRingAfterMove)
            OnNodeSelected?.Invoke(index);

        // prevIndex == index면 이미 그 노드에 서 있다는 뜻이라 이동할 거리가 없다.
        // 같은 점 두 개짜리 경로를 넘기면 0.6초 동안 제자리에 멈춰 있다가 진행돼 답답하다.
        bool needsMove = ldy_play != null && IsValidIndex(prevIndex) && prevIndex != index;

        if (needsMove)
        {
            List<Vector2> path = new List<Vector2>
            {
                Nodes[prevIndex].position,
                Nodes[index].position
            };

            ldy_play.MoveAlongPath(
                path,
                () => StartCoroutine(Co_FinishNodeEntry(index, node, screenUV, TokenMoveDuration)),
                TokenMoveDuration);
        }
        else
        {
            StartCoroutine(Co_FinishNodeEntry(index, node, screenUV, 0f));
        }
    }

    /// <summary>
    /// 토큰이 노드에 도착한 뒤 실제 진입까지를 맡는다.
    ///
    /// 연출이 끝나기 전에 씬을 넘겨버리면 링이 그려지다 말고 잘린다. 그래서 여기서 한 박자 쉰다.
    /// timeScale이 0이어도 흘러가도록 Realtime으로 기다린다. 링 트윈도 SetUpdate(true)라 기준이 같다.
    /// (유언/계승 시스템이 timeScale을 쥐고 있으므로 이쪽에서 timeScale을 건드리지 않는다.)
    /// </summary>
    /// <param name="alreadyElapsed">
    /// 링 연출이 이미 재생되고 있던 시간. 이동과 동시에 그리는 모드에서 토큰이 움직인 시간이다.
    /// 이만큼은 이미 기다린 셈이라 빼준다. 안 빼면 링이 진작 끝났는데도 멍하니 더 기다린다.
    /// </param>
    private IEnumerator Co_FinishNodeEntry(int index, LDY_MapNode node, Vector2 screenUV, float alreadyElapsed)
    {
        // 이동과 동시에 쏘는 모드에서는 클릭 시점에 이미 알렸다. 여기서 또 부르면 링이 두 번 그려진다.
        if (playSelectRingAfterMove)
            OnNodeSelected?.Invoke(index);

        // 순차 모드는 지금 막 링을 시작했으니 처음부터 다 기다려야 한다.
        float consumed = playSelectRingAfterMove ? 0f : alreadyElapsed;
        float remain   = selectRingHoldDuration - consumed;

        if (remain > 0f)
            yield return new WaitForSecondsRealtime(remain);

        ExecuteNodeAction(index, node, screenUV);
        isNodeActionInProgress = false;
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
                EnterStage(index, node, screenUV);
                break;
            case LDY_NodeType.Boss:
                EnterStage(index, node, screenUV);
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

    /// <summary>
    /// 전투 승리 뒤 클리어 처리를 시작한다.
    ///
    /// 보드 회전 연출이 씬에 있으면 그것이 끝나기를 먼저 기다린다.
    /// 기다리기만 할 뿐 뒤따르는 순서는 그대로다 — 노드 완료 → 보상 생성 → 보상 UI가
    /// 예전과 같은 자리에서 같은 순서로 일어나고, 그 시작점만 연출 뒤로 밀린다.
    /// </summary>
    public void CompleteActiveNodeAndReturnToMap()
    {
        if (!IsValidIndex(activeNodeIndex))
        {
            GoToMapScene();
            return;
        }

        // 연출을 기다리는 사이에 같은 요청이 또 들어오면 노드가 두 번 완료된다.
        // (KTH_TestClearButton 연타 등)
        if (_boardFlipPending)
        {
            Debug.LogWarning("[LDY_MapManager] 보드 회전 연출을 기다리는 중이라 중복 클리어 요청을 무시합니다.", this);
            return;
        }

        // 전투 씬에만 있는 컴포넌트다. 맵이나 팝업 경로에서 못 찾는 것이 정상이며,
        // 그때는 기다릴 연출이 없으므로 그대로 진행한다.
        LDY_BoardFlipDirector flipDirector = FindFirstObjectByType<LDY_BoardFlipDirector>();

        if (flipDirector == null)
        {
            CompleteClearedNodeAndLeave();
            return;
        }

        _boardFlipPending = true;
        StartCoroutine(Co_FlipBoardThenComplete(flipDirector));
    }

    /// <summary>
    /// 보드 회전 연출이 끝나기를 기다린 뒤 클리어 처리를 이어간다.
    ///
    /// 연출이 끝나지 않는 상황에서 런이 영영 멈추는 쪽이 연출이 잘리는 것보다 나쁘다.
    /// 상한을 두고, 넘겼다면 조용히 넘어가지 않고 남긴다.
    /// </summary>
    private IEnumerator Co_FlipBoardThenComplete(LDY_BoardFlipDirector flipDirector)
    {
        flipDirector.Play();

        float deadline = Time.unscaledTime + BoardFlipWaitTimeout;

        // 씬이 먼저 내려가 디렉터가 파괴되면 null이 되어 루프를 빠져나온다.
        while (flipDirector != null && flipDirector.IsPlaying)
        {
            if (Time.unscaledTime >= deadline)
            {
                Debug.LogWarning(
                    $"[LDY_MapManager] 보드 회전 연출이 {BoardFlipWaitTimeout:0.#}초 안에 끝나지 않아 " +
                    "중단하고 클리어 처리를 이어갑니다.", this);

                flipDirector.Abort();
                break;
            }

            yield return null;
        }

        _boardFlipPending = false;

        CompleteClearedNodeAndLeave();
    }

    /// <summary>
    /// 노드를 완료 처리하고 다음 씬으로 넘어간다.
    /// 보상이 걸린 노드면 보상 UI가 끝날 때까지 씬 전환을 미룬다.
    /// </summary>
    private void CompleteClearedNodeAndLeave()
    {
        // 연출을 기다리는 사이에 노드가 정리됐을 수 있다.
        if (!IsValidIndex(activeNodeIndex))
        {
            GoToMapScene();
            return;
        }

        LDY_NodeType type = Nodes[activeNodeIndex].type;
        bool willGiveReward = type == LDY_NodeType.Battle || type == LDY_NodeType.Boss;

        _pendingClearedNodeType = type;

        if (willGiveReward)
        {
            waitingForRewardBeforeMapReturn = true;

            LSO_RewardBox box = LSO_RewardBox.Instance;
            if (box != null)
            {
                box.OnFinished -= HandleRewardResolvedThenReturnToMap;
                box.OnFinished += HandleRewardResolvedThenReturnToMap;
            }
            else
            {
                Debug.LogWarning("[LDY_MapManager] 씬에 LSO_RewardBox가 없어 즉시 씬을 전환합니다.");
                waitingForRewardBeforeMapReturn = false;
            }
        }

        CompleteActiveNode();

        if (!waitingForRewardBeforeMapReturn)
        {
            GoToPostClearScene(_pendingClearedNodeType);
        }
    }

    /// <summary>
    /// 전투 패배 시 호출된다.
    /// 전투/보스 구분 없이 공용 패배 씬(deathSceneName)으로 이동한다.
    /// </summary>
    public void FailActiveNodeAndReturnToMap()
    {
        Debug.Log($"[LDY_MapManager] 스테이지 패배 처리 (activeNodeIndex: {activeNodeIndex})");

        LSO_RewardBox box = LSO_RewardBox.Instance;
        if (box != null)
        {
            box.OnFinished -= HandleRewardResolvedThenReturnToMap;
        }

        waitingForRewardBeforeMapReturn = false;
        _boardFlipPending = false;
        activeNodeIndex = -1;

        GoToDeathScene();
    }

    private void HandleRewardResolvedThenReturnToMap(LSO_RewardOption option)
    {
        LSO_RewardBox box = LSO_RewardBox.Instance;
        if (box != null)
        {
            box.OnFinished -= HandleRewardResolvedThenReturnToMap;
        }

        waitingForRewardBeforeMapReturn = false;
        GoToPostClearScene(_pendingClearedNodeType);
    }

    private void GoToMapScene()
    {
        if (string.IsNullOrEmpty(mapSceneName))
        {
            Debug.LogWarning("[LDY_MapManager] mapSceneName이 비어 있습니다.", this);
            return;
        }

        LoadSceneAfterAnimations(mapSceneName);
    }

    /// <summary>
    /// 클리어(승리) 후 이동할 씬을 노드 타입에 따라 결정한다.
    /// Boss는 bossClearSceneName으로, 그 외(일반 전투)는 맵으로 돌아간다.
    /// </summary>
    private void GoToPostClearScene(LDY_NodeType clearedNodeType)
    {
        if (clearedNodeType == LDY_NodeType.Boss && !string.IsNullOrEmpty(bossClearSceneName))
        {
            Debug.Log($"[LDY_MapManager] 보스 클리어 씬 이동 -> {bossClearSceneName}");
            LoadSceneAfterAnimations(bossClearSceneName);
            return;
        }

        GoToMapScene();
    }

    /// <summary>
    /// 전투 연출이 끝난 뒤에 씬을 넘긴다.
    ///
    /// 마지막 적을 죽인 공격은 데미지가 들어간 뒤에도 제자리로 돌아오는 연출이 남아 있다
    /// (LDY_AttackSystem.StrikeOnce). 승리 판정은 데미지 한 프레임 뒤에 떨어지므로,
    /// 곧바로 씬을 넘기면 그 복귀 애니메이션이 잘리고 재생 중이던 트윈이 파괴된 대상을 붙들어
    /// DOTween 경고가 뜬다. 그래서 넘기기 전에 연출이 끝나기를 기다린다.
    ///
    /// 기다림 여부만 여기서 정하고 "지금 연출 중인가"는 LDY_TurnManager.IsAnimating에게 묻는다.
    /// 턴 전환이 연출을 기다릴 때 쓰는 것과 같은 판단이라, 규칙이 두 벌로 갈리지 않게 한다.
    /// </summary>
    private void LoadSceneAfterAnimations(string targetSceneName)
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("[LDY_MapManager] 이동할 씬 이름이 비어 있습니다.", this);
            return;
        }

        // 예전에는 여기서 곧바로 LoadScene을 불렀기 때문에 두 번 부르는 것이 문제가 되지 않았다.
        // 이제는 기다리는 사이가 생겨서, 그 틈에 다른 클리어 경로가 한 번 더 들어오면
        // 씬 이동이 두 번 걸린다. 먼저 잡은 쪽만 통과시킨다.
        if (_sceneLoadPending)
        {
            Debug.LogWarning(
                $"[LDY_MapManager] 이미 씬 이동을 기다리는 중이라 '{targetSceneName}' 요청을 무시합니다.", this);
            return;
        }

        _sceneLoadPending = true;

        StartCoroutine(Co_LoadSceneAfterAnimations(targetSceneName));
    }

    private IEnumerator Co_LoadSceneAfterAnimations(string targetSceneName)
    {
        // 전투 씬에만 있는 컴포넌트다. 맵이나 팝업에서 부르면 못 찾는 것이 정상이며,
        // 그때는 기다릴 연출이 없으므로 그대로 넘어간다.
        _Scripts.LDY.LDY_TurnManager turnManager = FindFirstObjectByType<_Scripts.LDY.LDY_TurnManager>();

        if (turnManager != null)
        {
            // 연출이 끝나지 않는 상황에서 씬 전환이 영영 멈추는 쪽이 잘리는 것보다 나쁘다.
            // 상한을 두고, 넘겼다면 조용히 넘어가지 않고 남긴다.
            float deadline = Time.unscaledTime + SceneLoadAnimationWaitTimeout;

            while (turnManager != null && turnManager.IsAnimating())
            {
                if (Time.unscaledTime >= deadline)
                {
                    Debug.LogWarning(
                        $"[LDY_MapManager] 전투 연출이 {SceneLoadAnimationWaitTimeout:0.#}초 안에 끝나지 않아 " +
                        $"기다리지 않고 '{targetSceneName}'(으)로 넘어갑니다.", this);
                    break;
                }

                yield return null;
            }
        }

        try
        {
            SceneManager.LoadScene(targetSceneName);
        }
        finally
        {
            // 씬 이동이 실패해도(빌드 설정에 없는 이름 등) 깃발이 켜진 채 남으면
            // 이후 모든 씬 전환이 함께 막힌다. 성공·실패와 무관하게 되돌린다.
            _sceneLoadPending = false;
        }
    }

    /// <summary>
    /// 패배 시 공용 Death Scene으로 전환한다.
    /// </summary>
    private void GoToDeathScene()
    {
        if (string.IsNullOrEmpty(deathSceneName))
        {
            Debug.LogWarning(
                "[LDY_MapManager] Death Scene 이름이 비어 있어 맵으로 대신 돌아갑니다.",
                this
            );

            GoToMapScene();
            return;
        }

        Debug.Log($"[LDY_MapManager] 패배 씬 이동 -> {deathSceneName}");

        SceneManager.LoadScene(deathSceneName);
    }

    public void CompleteNode(int index)
    {
        if (!IsValidIndex(index)) return;

        if (Nodes[index].isCleared)
        {
            Debug.LogWarning($"[LDY_MapManager] {index}번 노드는 이미 클리어 처리되었습니다.");
            return;
        }

        Nodes[index].isCleared = true;

        if (Nodes[index].type == LDY_NodeType.Battle)
        {
            int clearedChapter = currentChapter;
            int clearedStage = currentStage;

            Debug.Log($"[LDY_MapManager] 스테이지 클리어 완료! (Chapter: {clearedChapter}, Stage: {clearedStage})");

            // 맵으로 돌아왔을 때 이 노드에만 클리어 연출을 틀기 위한 표시.
            _pendingClearedNodeIndex = index;

            TriggerStageReward(clearedChapter, clearedStage);

            currentStage++;
            editorStageIndex = currentStage;
            onStageChanged?.Invoke();
        }
        else if (Nodes[index].type == LDY_NodeType.Boss)
        {
            int clearedChapter = currentChapter;
            int clearedStage = currentStage;

            Debug.Log($"[LDY_MapManager] 보스 스테이지 클리어 완료! (Chapter: {clearedChapter}, Stage: {clearedStage})");

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

        // 여기서 CurrentNodeIndex를 다음 노드로 밀지 않는다.
        // 밀어버리면 클리어하고 맵에 돌아온 순간 토큰이 이미 다음 노드에 서 있게 되고,
        // 그 노드를 눌러도 출발지와 목적지가 같아 이동 연출이 아예 재생되지 않는다.
        // 토큰은 방금 클리어한 노드에 남아 있다가, 플레이어가 다음 노드를 누를 때 그리로 이동한다.
        if (firstNextIndex >= 0)
            previousNodeIndex = index;

        onMapChanged?.Invoke();
    }

    private void TriggerStageReward(int chapter, int stage)
    {
        LSO_RewardBox box = LSO_RewardBox.Instance;

        if (box == null)
        {
            Debug.LogError(
                "[LDY_MapManager] 씬에 LSO_RewardBox가 없어 보상을 시작하지 못했습니다."
            );

            return;
        }

        Debug.Log(
            $"[LDY_MapManager] 보상 요청 " +
            $"(Chapter: {chapter}, Stage: {stage})"
        );

        // 상자를 열어주지는 않는다. 누를 준비만 시킨다.
        // 뚜껑을 여는 것은 플레이어의 첫 클릭이다.
        box.Begin(chapter, stage);
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

    private void EnsureScenePlayerToken(int nodeIndex)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        bool hasSceneInstance = ldy_play != null
            && ldy_play.gameObject.scene.IsValid()
            && ldy_play.gameObject.scene == activeScene;

        if (hasSceneInstance) return;

        // SetTokenPositionToNode가 현재 씬의 토큰을 찾거나, 없으면 프리팹으로부터
        // 새 인스턴스를 만든 뒤 시작 노드 위치에 배치합니다.
        ldy_play = null;
        SetTokenPositionToNode(nodeIndex);
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

    /// <summary>
    /// 노드에 배정된 스테이지를 골라 그 스테이지가 지정한 씬을 연다.
    ///
    /// 갈 곳은 StageSO.SceneName 하나로만 정해진다. 노드마다 다른 전투 씬을 쓰고 싶으면
    /// 노드에 배정한 StageSO의 Scene Name만 바꾸면 되고, 맵은 고칠 것이 없다.
    /// </summary>
    private void EnterStage(int index, LDY_MapNode node, Vector2 screenUV)
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

        LDY_MapCameraController cameraController = FindFirstObjectByType<LDY_MapCameraController>();
        if (cameraController != null)
        {
            cameraController.PlayStageEnterTransition(
                screenUV,
                () => SceneManager.LoadScene(targetSceneName));
            return;
        }

        Debug.LogWarning("[LDY_MapManager] 맵 카메라 컨트롤러가 없어 진입 연출 없이 씬을 이동합니다.", this);
        SceneManager.LoadScene(targetSceneName);
    }

    public void ResetProgress()
    {
        Debug.Log("[LDY_MapManager] 진행도를 1-1로 초기화합니다.");

        // 현재 진행도 초기화
        currentChapter = 1;
        currentStage = 1;

        editorChapterIndex = 1;
        editorStageIndex = 1;

        // 현재 노드 상태 초기화
        activeNodeIndex = -1;
        CurrentNodeIndex = -1;
        previousNodeIndex = -1;

        BattleEntryCount = 0;

        isWaitingSecondClick = false;
        isNodeActionInProgress = false;
        waitingForRewardBeforeMapReturn = false;
        _sceneLoadPending = false;
        _boardFlipPending = false;
        _pendingClearedNodeIndex = -1;

        // 1챕터 맵 다시 불러오기
        LoadChapterToEditor(1);

        // 노드 다시 생성
        BuildNodes();

        // 저장 데이터도 현재 초기화 상태로 덮어쓰기
        if (LDY_SaveService.Instance != null)
        {
            LDY_SaveService.Instance.SaveRun();
            Debug.Log("[LDY_MapManager] 초기화된 진행도를 저장했습니다.");
        }

        onStageChanged?.Invoke();
        onMapChanged?.Invoke();
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
