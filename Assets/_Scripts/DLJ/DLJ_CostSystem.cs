using System.Collections.Generic;
using _Scripts.LDY;
using UnityEngine;

/// <summary>
/// 행동력 값과 코스트 케이스 표시를 연결한다.
/// 케이스 하나는 기본 5코스트를 맡고, 6부터 다음 케이스를 생성한다.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class DLJ_CostSystem : MonoBehaviour
{
    [Header("References")]
    [Tooltip("행동력 관리자. 비워두면 LDY_ActionPointManager.instance를 찾는다.")]
    [SerializeField] private LDY_ActionPointManager actionPoints;

    [Tooltip("현재 턴을 확인할 턴 관리자. 비워두면 씬에서 자동으로 찾는다.")]
    [SerializeField] private LDY_TurnManager turnManager;

    [Tooltip("씬에 처음부터 놓여 있는 첫 번째 코스트 케이스 루트.")]
    [SerializeField] private GameObject firstCase;

    [Tooltip("추가 케이스의 부모. 비워두면 첫 케이스의 부모를 쓴다.")]
    [SerializeField] private Transform caseRoot;

    [Header("Layout")]
    [Tooltip("케이스 하나가 담는 코인 수.")]
    [SerializeField, Min(1)] private int coinsPerCase = 5;

    [Tooltip("다음 케이스가 놓일 로컬 위치 간격.")]
    [SerializeField] private Vector3 caseStep = new Vector3(6f, 0f, 0f);

    [Tooltip("코스트가 다시 낮아지면 필요 없어진 추가 케이스를 제거한다.")]
    [SerializeField] private bool removeUnusedCases = true;

    private readonly List<CaseInstance> _cases = new List<CaseInstance>();
    private LDY_ActionPointManager _subscribedActionPoints;
    private LDY_TurnManager _subscribedTurnManager;
    private GameObject _caseTemplateContainer;
    private GameObject _caseTemplate;
    private Vector3 _firstCaseLocalPosition;
    private bool _initialized;

    private sealed class CaseInstance
    {
        public readonly GameObject Root;
        public readonly DLJ_CostCase View;

        public CaseInstance(GameObject root, DLJ_CostCase view)
        {
            Root = root;
            View = view;
        }
    }

    private void Awake()
    {
        CreateRuntimeCaseTemplate();
        InitializeCases();
    }

    private void OnEnable()
    {
        TryBindActionPoints();
        TryBindTurnManager();
    }

    private void Start()
    {
        // ActionPointManager가 자신의 Awake에서 instance를 등록하므로 한 번 더 시도한다.
        TryBindActionPoints();
        TryBindTurnManager();

        if (_subscribedActionPoints != null)
        {
            DLJ_CostVisualTransition transition = IsPlayerTurn
                ? DLJ_CostVisualTransition.Refill
                : DLJ_CostVisualTransition.Immediate;
            Refresh(IsPlayerTurn ? _subscribedActionPoints.Current : 0, transition);
        }
    }

    private void OnDisable()
    {
        UnbindActionPoints();
        UnbindTurnManager();
    }

    private void OnDestroy()
    {
        // 첫 케이스는 씬 오브젝트라 건드리지 않고, 런타임에 만든 것만 정리한다.
        for (int i = _cases.Count - 1; i >= 1; i--)
        {
            if (_cases[i]?.Root != null)
                Destroy(_cases[i].Root);
        }

        _cases.Clear();

        if (_caseTemplateContainer != null)
            Destroy(_caseTemplateContainer);
    }

    /// <summary>
    /// 별도 Cost 프리팹 없이 추가 케이스를 만들기 위한 비활성 원본을 준비한다.
    /// 비활성 컨테이너 아래에서 복제해 첫 케이스의 생명주기를 건드리지 않고 온전한 계층을 보관한다.
    /// </summary>
    private void CreateRuntimeCaseTemplate()
    {
        if (firstCase == null || _caseTemplate != null) return;

        Transform templateParent = caseRoot != null ? caseRoot : firstCase.transform.parent;

        _caseTemplateContainer = new GameObject($"{name}_CaseTemplateContainer");
        _caseTemplateContainer.hideFlags = HideFlags.HideInHierarchy;
        _caseTemplateContainer.transform.SetParent(templateParent, false);
        _caseTemplateContainer.SetActive(false);

        _caseTemplate = Instantiate(firstCase, _caseTemplateContainer.transform, false);
        _caseTemplate.name = $"{firstCase.name}_RuntimeTemplate";
        _caseTemplate.hideFlags = HideFlags.HideInHierarchy;
    }

    private void InitializeCases()
    {
        if (_initialized) return;

        if (firstCase == null)
        {
            Debug.LogError($"{name}: First Case가 연결되지 않았습니다.", this);
            return;
        }

        if (caseRoot == null)
            caseRoot = firstCase.transform.parent;

        DLJ_CostCase firstCaseView = ResolveCaseView(firstCase);
        if (firstCaseView == null)
        {
            Debug.LogError($"{firstCase.name}: DLJ_CostCase를 만들지 못했습니다.", firstCase);
            return;
        }

        _firstCaseLocalPosition = firstCase.transform.localPosition;
        firstCaseView.Initialize();

        _cases.Clear();
        _cases.Add(new CaseInstance(firstCase, firstCaseView));
        _initialized = true;
    }

    private void TryBindActionPoints()
    {
        if (_subscribedActionPoints != null) return;

        if (actionPoints == null)
            actionPoints = LDY_ActionPointManager.instance;

        if (actionPoints == null) return;

        _subscribedActionPoints = actionPoints;
        _subscribedActionPoints.OnActionPointsChanged += HandleActionPointsChanged;
    }

    private void UnbindActionPoints()
    {
        if (_subscribedActionPoints == null) return;

        _subscribedActionPoints.OnActionPointsChanged -= HandleActionPointsChanged;
        _subscribedActionPoints = null;
    }

    private void HandleActionPointsChanged(int current, int max)
    {
        // 적 턴용 행동력도 같은 풀을 사용하지만, 플레이어 코스트 UI에는 보여주지 않는다.
        Refresh(
            IsPlayerTurn ? current : 0,
            IsPlayerTurn ? DLJ_CostVisualTransition.Spend : DLJ_CostVisualTransition.Immediate);
    }

    private void TryBindTurnManager()
    {
        if (_subscribedTurnManager != null) return;

        if (turnManager == null)
            turnManager = FindFirstObjectByType<LDY_TurnManager>();

        if (turnManager == null) return;

        _subscribedTurnManager = turnManager;
        _subscribedTurnManager.OnTurnChanged += HandleTurnChanged;
    }

    private void UnbindTurnManager()
    {
        if (_subscribedTurnManager == null) return;

        _subscribedTurnManager.OnTurnChanged -= HandleTurnChanged;
        _subscribedTurnManager = null;
    }

    private void HandleTurnChanged(LDY_Team team)
    {
        int visibleCost = team == LDY_Team.Player && _subscribedActionPoints != null
            ? _subscribedActionPoints.Current
            : 0;

        DLJ_CostVisualTransition transition = team == LDY_Team.Player
            ? DLJ_CostVisualTransition.Refill
            : DLJ_CostVisualTransition.Immediate;
        Refresh(visibleCost, transition);
    }

    private bool IsPlayerTurn =>
        _subscribedTurnManager == null ||
        _subscribedTurnManager.CurrentTurn == LDY_Team.Player;

    /// <summary>현재 행동력에 맞춰 케이스 수와 코인 표시를 갱신한다.</summary>
    public void Refresh(int current)
    {
        Refresh(current, DLJ_CostVisualTransition.Spend);
    }

    private void Refresh(int current, DLJ_CostVisualTransition transition)
    {
        InitializeCases();
        if (!_initialized) return;

        current = Mathf.Max(0, current);
        int requiredCaseCount = Mathf.Max(1, Mathf.CeilToInt((float)current / coinsPerCase));

        EnsureCaseCount(requiredCaseCount);

        for (int i = 0; i < _cases.Count; i++)
        {
            if (_cases[i]?.View == null) continue;

            int filledCoins = Mathf.Clamp(current - i * coinsPerCase, 0, coinsPerCase);
            _cases[i].View.SetFilled(filledCoins, transition);
        }
    }

    private void EnsureCaseCount(int requiredCaseCount)
    {
        while (_cases.Count < requiredCaseCount)
        {
            if (_caseTemplate == null)
            {
                Debug.LogError(
                    $"{name}: {requiredCaseCount}개의 케이스가 필요하지만 런타임 케이스 원본을 만들지 못했습니다.",
                    this);
                return;
            }

            int index = _cases.Count;
            GameObject newCaseRoot = Instantiate(_caseTemplate, caseRoot, false);
            newCaseRoot.hideFlags = HideFlags.None;
            newCaseRoot.name = $"{firstCase.name}_{index + 1}";
            newCaseRoot.transform.localPosition = _firstCaseLocalPosition + caseStep * index;
            newCaseRoot.SetActive(true);

            DLJ_CostCase newCaseView = ResolveCaseView(newCaseRoot);
            if (newCaseView == null)
            {
                Debug.LogError($"{newCaseRoot.name}: DLJ_CostCase를 만들지 못했습니다.", newCaseRoot);
                Destroy(newCaseRoot);
                return;
            }

            newCaseView.Initialize();

            // Start를 기다리지 않고 정확한 최종 위치를 저장한 뒤 등장시킨다.
            DLJ_CostAnimation animation = newCaseRoot.GetComponentInChildren<DLJ_CostAnimation>(true);
            if (animation != null)
            {
                animation.CaptureRestPosition();
                animation.PlayEntrance();
            }
            else
            {
                Debug.LogWarning(
                    $"{newCaseRoot.name}: DLJ_CostAnimation이 없어 등장 연출 없이 생성됩니다.",
                    newCaseRoot);
            }

            _cases.Add(new CaseInstance(newCaseRoot, newCaseView));
        }

        if (!removeUnusedCases) return;

        while (_cases.Count > requiredCaseCount && _cases.Count > 1)
        {
            int lastIndex = _cases.Count - 1;
            CaseInstance unusedCase = _cases[lastIndex];
            _cases.RemoveAt(lastIndex);

            if (unusedCase?.Root != null)
                Destroy(unusedCase.Root);
        }
    }

    private static DLJ_CostCase ResolveCaseView(GameObject caseRootObject)
    {
        if (caseRootObject == null) return null;

        DLJ_CostCase view = caseRootObject.GetComponentInChildren<DLJ_CostCase>(true);
        return view != null ? view : caseRootObject.AddComponent<DLJ_CostCase>();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        coinsPerCase = Mathf.Max(1, coinsPerCase);
    }
#endif
}
