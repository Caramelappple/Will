using _Scripts.LSO.Deck.Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening; // ★ DOTween 네임스페이스

public class KTH_DeckBuilderManager : MonoBehaviour
{
    public static KTH_DeckBuilderManager Instance { get; private set; }

    [Header("카드 데이터 리스트")]
    public List<LSO_CardSO> cardDatabase = new List<LSO_CardSO>();
    public List<LSO_CardSO> initialInventoryCards = new List<LSO_CardSO>();

    [Header("UI 프리팹 및 컨테이너")]
    public KTH_CardDragUI cardUIPrefab;
    public RectTransform poolContainer;
    public RectTransform inventoryContainer;

    [Header("카드 UI 크기 설정")]
    public Vector2 targetCardSize = new Vector2(120f, 160f);

    [Header("페이지네이션 설정 (상단 풀)")]
    public int cardsPerPage = 4;
    private int currentPageIndex = 0;

    [Header("페이지 이동 버튼")]
    public Button prevButton;
    public Button nextButton;

    [Header("기타 버튼 설정")]
    public Button completeButton;
    public Button resetButton;
    public string nextSceneName = "KTH_BattleScene";

    [Header("DOTween 연출 설정")]
    public float flipAnimDuration = 0.4f;   // 회전 등장 시간
    public float resetAnimDuration = 0.35f;  // 리셋 시 올라가는 시간
    public float cardAnimInterval = 0.08f;   // 카드 간 등장 시차

    private bool isAnimating = false; // 연출 중 중복 클릭 방지 플래그

    // 인벤토리에 담긴 카드의 고유 인덱스. 화면(하이라키)이 아니라 이 목록이 정답이다.
    // UI는 이 목록을 그리기만 하므로, 정렬·필터·스크롤을 넣어도 상태가 흐트러지지 않는다.
    private readonly List<int> _inventoryIndices = new List<int>();

    private void Awake()
    {
        // 씬을 다시 열었을 때 이전 인스턴스를 덮어쓰지 않도록 먼저 자리를 잡은 쪽을 살린다.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (completeButton) completeButton.onClick.AddListener(OnCompleteButtonClick);
        if (resetButton) resetButton.onClick.AddListener(OnResetButtonClick);

        if (prevButton)
        {
            prevButton.onClick.RemoveAllListeners();
            prevButton.onClick.AddListener(OnPrevPageButtonClick);
        }
        if (nextButton)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextPageButtonClick);
        }

        InitializeDeckBuilder();
    }

    private void Update()
    {
        if (isAnimating) return; // 연출 중에는 키보드 입력 방지

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) OnPrevPageButtonClick();
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) OnNextPageButtonClick();
    }

    /// <summary>시작할 때 초기화 (회전 연출 적용)</summary>
    public void InitializeDeckBuilder()
    {
        currentPageIndex = 0;

        BuildInitialInventory();

        ClearContainerImmediate(poolContainer);
        RefreshInventoryView(true);

        // 시작 시 상단 풀 카드도 회전 연출 적용
        RefreshPoolPage(true);
    }

    /// <summary>인스펙터의 초기 인벤토리 카드를 고유 인덱스로 바꿔 목록에 담는다.</summary>
    private void BuildInitialInventory()
    {
        _inventoryIndices.Clear();

        if (initialInventoryCards == null || cardDatabase == null) return;

        foreach (LSO_CardSO card in initialInventoryCards)
        {
            if (card == null) continue;

            int index = ResolveUnclaimedIndex(card, _inventoryIndices);
            if (index < 0)
            {
                Debug.LogWarning(
                    $"[KTH_DeckBuilderManager] {card.name}가 cardDatabase에 없거나 수량이 모자라 인벤토리에 넣지 못했습니다.", card);
                continue;
            }

            _inventoryIndices.Add(index);
        }
    }

    /// <summary>인벤토리 목록대로 화면을 다시 그린다.</summary>
    private void RefreshInventoryView(bool useFlipAnim = false)
    {
        if (inventoryContainer == null || cardUIPrefab == null) return;

        ClearContainerImmediate(inventoryContainer);

        for (int i = 0; i < _inventoryIndices.Count; i++)
        {
            int index = _inventoryIndices[i];
            if (index < 0 || index >= cardDatabase.Count) continue;

            CreateCardUI(cardDatabase[index], inventoryContainer, index, i * cardAnimInterval, useFlipAnim);
        }

        RefreshLayout(inventoryContainer);
    }

    /// <summary>현재 인벤토리에 담긴 카드의 고유 인덱스 목록. 읽기 전용.</summary>
    public IReadOnlyList<int> GetCurrentInventoryIndices() => _inventoryIndices;

    /// <summary>현재 인벤토리에 들어있는 카드 목록 (씬 저장용). 인덱스 목록에서 만들어낸다.</summary>
    public List<LSO_CardSO> GetCurrentInventoryCardData()
    {
        List<LSO_CardSO> inventoryList = new List<LSO_CardSO>();

        foreach (int index in _inventoryIndices)
        {
            if (index < 0 || index >= cardDatabase.Count) continue;

            LSO_CardSO card = cardDatabase[index];
            if (card != null) inventoryList.Add(card);
        }

        return inventoryList;
    }

    /// <summary>
    /// 카드 드래그가 끝났을 때 카드가 알려준다.
    /// 인벤토리 목록을 먼저 갱신하고 나서 화면을 다시 그린다. 화면이 아니라 이 목록이 정답이다.
    /// </summary>
    private void HandleCardDropped(KTH_CardDragUI card, bool droppedInInventory)
    {
        if (card == null) return;

        int index = card.DatabaseIndex;
        if (index < 0)
        {
            Debug.LogWarning($"[KTH_DeckBuilderManager] {card.name}: cardDatabase에 없는 카드라 인벤토리에 반영할 수 없습니다.", card);
            return;
        }

        if (droppedInInventory)
        {
            if (!_inventoryIndices.Contains(index)) _inventoryIndices.Add(index);
        }
        else
        {
            _inventoryIndices.Remove(index);
        }

        RefreshPoolPage();
    }

    /// <summary>
    /// 카드가 cardDatabase에서 차지할 고유 인덱스를 찾는다.
    /// 이미 다른 카드가 가져간 인덱스는 건너뛰므로, 같은 카드가 여러 장 있어도 서로 다른 자리를 잡는다.
    /// </summary>
    private int ResolveUnclaimedIndex(LSO_CardSO card, ICollection<int> claimed)
    {
        if (card == null || cardDatabase == null) return -1;

        for (int i = 0; i < cardDatabase.Count; i++)
        {
            if (cardDatabase[i] != card) continue;
            if (claimed.Contains(i)) continue;

            return i;
        }

        return -1;
    }

    /// <summary>고유 인덱스 기반으로 상단 풀 페이지 새로고침 (중복 카드가 있어도 정확히 동작)</summary>
    public void RefreshPoolPage(bool useFlipAnim = false)
    {
        if (poolContainer == null || cardUIPrefab == null || cardDatabase == null) return;

        ClearContainerImmediate(poolContainer);

        // 페이지별 고유 범위 계산 (1페이지 = 0~3, 2페이지 = 4~7 등)
        int startIndex = currentPageIndex * cardsPerPage;
        int endIndex = Mathf.Min(startIndex + cardsPerPage, cardDatabase.Count);

        int spawnedCount = 0;

        for (int i = startIndex; i < endIndex; i++)
        {
            LSO_CardSO card = cardDatabase[i];
            if (card == null) continue;

            // 이미 자기 고유 번호(i)가 인벤토리에 있다면 해당 자리는 생성 안 함 (빈자리 유지)
            if (_inventoryIndices.Contains(i))
            {
                continue;
            }

            // 고유 인덱스(i)를 보존하여 생성
            CreateCardUI(card, poolContainer, i, spawnedCount * cardAnimInterval, useFlipAnim);
            spawnedCount++;
        }

        RefreshLayout(poolContainer);
        UpdatePageButtons();
    }

    /// <summary>카드 UI 생성 함수 (databaseIndex 매개변수 포함, useFlipAnimation이 true일 때만 Y축 회전 연출)</summary>
    private void CreateCardUI(LSO_CardSO data, Transform parent, int databaseIndex, float delay = 0f, bool useFlipAnimation = false)
    {
        if (data == null) return;

        var cardUI = Instantiate(cardUIPrefab, parent);
        // 카드는 드롭이 끝났다는 사실만 알리고, 목록을 다시 그리는 건 매니저가 결정한다.
        cardUI.Setup(data, databaseIndex, HandleCardDropped);

        RectTransform cardRect = cardUI.GetComponent<RectTransform>();
        if (cardRect != null)
        {
            cardRect.sizeDelta = targetCardSize;

            if (useFlipAnimation)
            {
                // ★ 회전 연출 조건이 true일 때만 Y축 180도 세팅 후 DOTween 회전
                cardRect.localScale = new Vector3(0f, 1f, 1f);
                cardRect.localRotation = Quaternion.Euler(0f, 180f, 0f);

                Sequence seq = DOTween.Sequence();
                seq.PrependInterval(delay);
                seq.Join(cardRect.DOScale(Vector3.one, flipAnimDuration).SetEase(Ease.OutBack));
                seq.Join(cardRect.DORotate(Vector3.zero, flipAnimDuration).SetEase(Ease.OutCubic));
            }
            else
            {
                // 회전이 필요 없는 일반 페이지 이동 시에는 즉시 기본값 설정
                cardRect.localScale = Vector3.one;
                cardRect.localRotation = Quaternion.identity;
            }
        }
    }

    /// <summary>리셋 버튼 클릭: 인벤토리 카드가 상단으로 올라간 후 회전하며 재등장</summary>
    private void OnResetButtonClick()
    {
        if (isAnimating || _inventoryIndices.Count == 0)
        {
            _inventoryIndices.Clear();
            currentPageIndex = 0;
            RefreshPoolPage(true); // 카드가 없을 때도 리셋 회전 연출 적용
            return;
        }

        isAnimating = true;

        Sequence resetSequence = DOTween.Sequence();
        Vector3 targetPoolPos = poolContainer.position;

        // 인벤토리에 있는 모든 카드를 상단 풀 방향으로 올려보냄
        foreach (Transform child in inventoryContainer)
        {
            child.DOKill();

            resetSequence.Join(child.DOMove(targetPoolPos, resetAnimDuration).SetEase(Ease.InQuad));
            resetSequence.Join(child.DOScale(Vector3.zero, resetAnimDuration).SetEase(Ease.InQuad));
        }

        // 올라가는 연출 완료 후, 'true'를 전달하여 상단 풀 카드가 회전하면서 짠 하고 다시 생성됨
        resetSequence.OnComplete(() =>
        {
            _inventoryIndices.Clear();          // 목록을 비우는 것이 먼저
            RefreshInventoryView();             // 화면은 목록을 따라간다
            currentPageIndex = 0;
            RefreshPoolPage(true);              // ★ 리셋 후 생성 시 회전 연출 적용
            isAnimating = false;
        });
    }

    public void UpdatePageButtons()
    {
        int totalCards = cardDatabase != null ? cardDatabase.Count : 0;
        int maxPages = Mathf.Max(1, Mathf.CeilToInt((float)totalCards / cardsPerPage));

        if (prevButton)
            prevButton.interactable = (currentPageIndex > 0);

        if (nextButton)
            nextButton.interactable = (currentPageIndex + 1 < maxPages);
    }

    public void OnNextPageButtonClick()
    {
        if (isAnimating) return;

        int totalCards = cardDatabase != null ? cardDatabase.Count : 0;
        int maxPages = Mathf.CeilToInt((float)totalCards / cardsPerPage);

        if (currentPageIndex + 1 < maxPages)
        {
            currentPageIndex++;
            RefreshPoolPage(false); // 페이지 이동 시에는 회전 안 함
        }
    }

    public void OnPrevPageButtonClick()
    {
        if (isAnimating) return;

        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            RefreshPoolPage(false); // 페이지 이동 시에는 회전 안 함
        }
    }

    private void ClearContainerImmediate(Transform container)
    {
        if (!container) return;

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Transform child = container.GetChild(i);
            child.DOKill();
            child.SetParent(null);
            Destroy(child.gameObject);
        }
    }

    private void RefreshLayout(RectTransform container)
    {
        if (!container) return;

        var grid = container.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (grid != null)
        {
            grid.cellSize = targetCardSize;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(container);
    }

    private void OnCompleteButtonClick()
    {
        if (completeButton) completeButton.interactable = false;

        List<LSO_CardSO> currentInventoryList = GetCurrentInventoryCardData();

        if (KTH_DeckDataPersistent.Instance != null)
        {
            KTH_DeckDataPersistent.Instance.SaveInventory(currentInventoryList);
        }

        SceneManager.LoadScene(nextSceneName);
    }
}