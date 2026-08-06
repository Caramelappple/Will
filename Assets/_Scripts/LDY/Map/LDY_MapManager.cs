using System.Collections.Generic;
using _Scripts.LDY.Stage;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[System.Serializable]
public class LDY_MapNodeUnityEvent : UnityEvent<LDY_MapNode> { }

public class LDY_MapManager : MonoBehaviour
{
    public static LDY_MapManager Instance { get; private set; }

    [Header("테스트용")]
    [SerializeField] private bool isTest = false;
    private bool isWaitingSecondClick = false;

    [Header("맵 씬 이름 (테스트 클리어 버튼 사용 후 복귀할 씬)")]
    [SerializeField] private string mapSceneName = "MapScene";

    [Header("별자리 노드 (좌표/타입)")]
    [SerializeField] private Vector2[] nodePositions;
    [SerializeField] private LDY_NodeType[] nodeTypes;

    [Header("노드 연결 (분기 가능: 한 노드가 여러 연결을 가질 수 있음)")]
    [SerializeField] private LDY_NodeConnection[] connections;

    [Header("씬 전환용 씬 이름 (스테이지가 배정되지 않은 노드의 기본값)")]
    [SerializeField] private string battleSceneName = "BattleScene";
    [SerializeField] private string bossSceneName = "BossScene";

    [Header("스테이지 배정 (연결하면 스테이지SO가 이동할 씬까지 결정)")]
    [Tooltip("LDY_IStageRouter를 구현한 컴포넌트(기본 제공: LDY_StageRouter). 비워두면 위의 기본 씬 이름을 그대로 쓴다.")]
    [SerializeField] private MonoBehaviour stageRouterSource;

    private LDY_IStageRouter _stageRouter;

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

    // 지금 플레이어가 진입해서 진행 중인 노드 (전투/상점/이벤트가 끝나면 CompleteActiveNode로 완료 처리)
    [SerializeField] private int activeNodeIndex = -1;
    public int ActiveNodeIndex => activeNodeIndex;

    // 플레이어가 맵 위에서 "지금 서 있는" 노드 - activeNodeIndex와 달리 완료되어도 -1로 안 돌아간다.
    // LDY_MapUIController(씬 로컬이라 전투 씬을 갔다 오면 통째로 새로 생김)가 이 값을 읽어서 플레이어
    // 토큰을 항상 시작 노드가 아니라 마지막으로 도착한 노드에 놓기 위해 씀.
    public int CurrentNodeIndex { get; private set; } = -1;

    public List<LDY_MapNode> Nodes { get; private set; } = new List<LDY_MapNode>();
    public LDY_NodeConnection[] Connections => connections;

    // 전투(Battle) 노드에 들어간 횟수. 이 매니저는 DontDestroyOnLoad라서 Map -> Battle 씬 전환에도
    // 값이 유지된다 - 전투 씬에서 이 값을 읽어 적 숫자 등 난이도를 올리는 데 쓰면 된다(예: LDY_BattleDifficultyManager).
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
        BuildNodes();
    }

    private void ResolveStageRouter()
    {
        _stageRouter = stageRouterSource as LDY_IStageRouter;

        if (_stageRouter == null && stageRouterSource != null)
            Debug.LogError($"[LDY_MapManager] {stageRouterSource.GetType().Name}은(는) LDY_IStageRouter를 구현하지 않습니다.", this);

        if (_stageRouter == null)
            _stageRouter = GetComponent<LDY_IStageRouter>();
    }

    private void OnValidate()
    {
        if (nodeTypes != null && nodePositions != null && nodeTypes.Length != nodePositions.Length)
        {
            Debug.LogWarning($"[LDY_MapManager] nodePositions({nodePositions.Length})와 nodeTypes({nodeTypes.Length}) 길이가 다릅니다.", this);
        }
    }

    private void BuildNodes()
    {
        Nodes.Clear();
        activeNodeIndex = -1;

        for (int i = 0; i < nodePositions.Length; i++)
        {
            LDY_NodeType type = (nodeTypes != null && i < nodeTypes.Length) ? nodeTypes[i] : LDY_NodeType.Battle;
            Nodes.Add(new LDY_MapNode(nodePositions[i], type));
        }

        if (connections != null)
        {
            foreach (LDY_NodeConnection c in connections)
            {
                if (!IsValidIndex(c.fromIndex) || !IsValidIndex(c.toIndex)) continue;
                Nodes[c.fromIndex].nextIndices.Add(c.toIndex);
            }
        }

        if (Nodes.Count == 0) return;

        int startIndex = Nodes.FindIndex(n => n.type == LDY_NodeType.Start);
        if (startIndex < 0) startIndex = 0;

        Nodes[startIndex].isUnlocked = true;
        CurrentNodeIndex = startIndex;

        if (Nodes[startIndex].type == LDY_NodeType.Start)
            CompleteNode(startIndex);
    }

    // screenUV: 클릭한 노드의 화면상 위치(0~1). 씬 전환 아이리스 연출의 중심점으로 씀
    public void OnNodeClicked(int index, Vector2 screenUV)
    {
        if (!IsValidIndex(index)) return;

        LDY_MapNode node = Nodes[index];
        if (!node.isUnlocked) return;

        // 이미 지나온(클리어된) 노드는 되돌아가는 것만 허용하고, 전투/상점/이벤트 등 실제 효과는 다시
        // 발동시키지 않는다 - 플레이어가 서 있는 자리만 그쪽으로 옮겨준다.
        if (node.isCleared)
        {
            CurrentNodeIndex = index;
            onMapChanged?.Invoke();
            return;
        }

        if (isTest)
        {
            if (!isWaitingSecondClick)
            {
                // 첫 클릭: 이동만 하고 끝. 클리어 처리 안 함
                CurrentNodeIndex = index;
                isWaitingSecondClick = true;
                onMapChanged?.Invoke();
                return;
            }

            // 두 번째 클릭: 씬 이동 없이 바로 클리어 처리
            activeNodeIndex = index;
            isWaitingSecondClick = false;
            CompleteNode(index);
            return;
        }

        activeNodeIndex = index;
        CurrentNodeIndex = index;

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

    // 전투 승리, 상점/이벤트 종료 시점에 마지막으로 진입했던 노드를 완료 처리 (가장 흔히 쓰는 진입점)
    public void CompleteActiveNode()
    {
        if (activeNodeIndex >= 0) CompleteNode(activeNodeIndex);
    }

    // 테스트 씬의 버튼에서 호출: 활성 노드를 클리어 처리하고 곧바로 맵 씬으로 돌아간다.
    // LDY_MapManager는 DontDestroyOnLoad라서 테스트 씬이 사라져도 이 매니저와 노드 상태는 그대로 유지된다.
    public void CompleteActiveNodeAndReturnToMap()
    {
        CompleteActiveNode();

        if (string.IsNullOrEmpty(mapSceneName))
        {
            Debug.LogWarning("[LDY_MapManager] mapSceneName이 비어 있어 맵 씬으로 돌아갈 수 없습니다.", this);
            return;
        }

        SceneManager.LoadScene(mapSceneName);
    }

    // 특정 노드를 클리어 처리하고, 그 노드와 연결된 다음 노드들(분기면 여러 개)을 모두 unlock
    public void CompleteNode(int index)
    {
        if (!IsValidIndex(index))
            return;

        Nodes[index].isCleared = true;

        // ===========================
        // 스테이지 클리어 리워드 지급
        // ===========================
        if (KTH_Reward.Instance != null)
        {
            KTH_UnlockResult result =
        KTH_Reward.Instance.UnlockByStage(currentChapter, currentStage);

            if (result.HasAnyNewUnlock)
            {
                Debug.Log($"===== Stage {currentStage} Reward =====");

                foreach (var piece in result.newlyUnlockedPieces)
                    Debug.Log($"새 기물 해금 : {piece}");

                foreach (var will in result.newlyUnlockedWills)
                    Debug.Log($"새 유언 해금 : {will}");

                Debug.Log("==============================");
            }
            else
            {
                Debug.Log($"Stage {currentStage} : 새로 해금된 리워드 없음");
            }
        }
        else
        {
            Debug.LogWarning("KTH_Reward가 존재하지 않습니다.");
        }

        // 일반 전투
        if (Nodes[index].type == LDY_NodeType.Battle)
        {
            currentStage++;
            onStageChanged?.Invoke();
        }

        // 보스
        if (Nodes[index].type == LDY_NodeType.Boss)
        {
            currentChapter++;
            currentStage = 1;
            onStageChanged?.Invoke();
        }

        // ===========================
        // 다음 노드 해금
        // ===========================
        foreach (int next in Nodes[index].nextIndices)
        {
            if (IsValidIndex(next))
                Nodes[next].isUnlocked = true;
        }

        if (activeNodeIndex == index)
            activeNodeIndex = -1;

        onMapChanged?.Invoke();
    }

    private bool IsValidIndex(int index) => index >= 0 && index < Nodes.Count;

    // 이 노드에 배정된 스테이지를 골라 다음 씬에 넘겨주고, 스테이지가 지정한 씬으로 이동한다.
    // 배정된 스테이지가 없으면 예전처럼 인스펙터에 적어둔 기본 씬 이름을 쓴다.
    private void EnterStage(int index, LDY_MapNode node, string fallbackSceneName, Vector2 screenUV)
    {
        LDY_StageSO stage = _stageRouter?.Resolve(index, node.type);

        if (stage != null)
        {
            // 다음 씬의 LDY_StageDirector가 이걸 집어서 스테이지를 시작한다.
            LDY_StageSelection.Select(stage);

            if (!string.IsNullOrEmpty(stage.SceneName))
            {
                RequestSceneLoad(stage.SceneName, screenUV, node.type);
                return;
            }

            Debug.LogWarning($"[LDY_MapManager] 스테이지 '{stage.name}'에 씬 이름이 비어 있어 기본 씬으로 이동합니다.", stage);
        }

        RequestSceneLoad(fallbackSceneName, screenUV, node.type);
    }

    // 씬 전환 연출(LDY_SceneTransition)이 씬에 있으면 그걸 거쳐서, 없으면 곧바로 씬을 로드
    private void RequestSceneLoad(string sceneName, Vector2 screenUV, LDY_NodeType nodeType)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        KTH_LoadingSceneController.LoadScene(sceneName);
    }

    // 상점/이벤트처럼 씬은 안 넘어가고 팝업만 여는 경우에도, 아이리스가 다 닫힌 뒤에 팝업 이벤트를 발생시킴.
    // 전투/보스와 달리 상점/이벤트는 "실패"가 없으므로, 팝업을 연 시점에 바로 완료 처리해서 그 다음
    // 노드들이 갈라진 길 전부 잠금 해제되게 한다(안 그러면 CompleteActiveNode를 부르는 곳이 아무 데도
    // 없어서 이 노드 뒤로는 영원히 진행이 안 막힌다).
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