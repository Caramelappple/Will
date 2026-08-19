using _Scripts.LDY;
using _Scripts.LSO;
using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.Will;
using DG.Tweening;
using System.Collections.Generic;
using _Scripts.LSO.Animal.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class KTH_DeckBuilderManager : MonoBehaviour
{
    public static KTH_DeckBuilderManager Instance { get; private set; }

    [Header("카드 데이터 리스트 (ItemLibraryManager와 자동 동기화됨)")]
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
    public float flipAnimDuration = 0.4f;
    public float resetAnimDuration = 0.35f;
    public float cardAnimInterval = 0.08f;

    private bool isAnimating = false;

    // 인덱스 대신 카드 식별명(ID)을 관리 (순서 변경 시 덱 뒤틀림 방지)
    private readonly List<string> _inventoryCardIDs = new List<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        if (ItemLibraryManager.Instance != null)
        {
            ItemLibraryManager.Instance.onItemLibraryUpdated += OnItemLibraryUpdated;
        }
    }

    private void OnDisable()
    {
        if (ItemLibraryManager.Instance != null)
        {
            ItemLibraryManager.Instance.onItemLibraryUpdated -= OnItemLibraryUpdated;
        }
    }

    private void OnDestroy()
    {
        DOTween.Kill(this);
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

        StartCoroutine(InitializeDeckBuilderDelayed());
    }

    private System.Collections.IEnumerator InitializeDeckBuilderDelayed()
    {
        // ItemLibraryManager Awake가 아직 안 돌았을 경우 대비
        if (ItemLibraryManager.Instance == null)
            yield return null;

        InitializeDeckBuilder();
    }

    private void Update()
    {
        if (isAnimating) return;

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) OnPrevPageButtonClick();
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) OnNextPageButtonClick();
    }

    /// <summary>
    /// ItemLibraryManager로부터 LSO_CardSO 타입 카드 데이터를 수집합니다.
    /// (CS8121 에러 우회를 위해 object 타입 캐스팅 검사 적용)
    /// </summary>
    public void SyncCardDatabaseFromLibrary()
    {
        cardDatabase.Clear();

        if (ItemLibraryManager.Instance != null)
        {
            if (ItemLibraryManager.Instance.UnlockedPieces != null)
            {
                foreach (var piece in ItemLibraryManager.Instance.UnlockedPieces)
                {
                    if (piece == null) continue;

                    if ((object)piece is LSO_CardSO cardSO)
                    {
                        // Contains 체크 제거 -> 중복 허용
                        cardDatabase.Add(cardSO);
                    }
                    else if ((object)piece is LSO_AnimalSO animalSO)
                    {
                        LSO_CardSO matchedCard = initialInventoryCards.Find(c => c != null && c.Animal == animalSO);
                        if (matchedCard != null)
                        {
                            cardDatabase.Add(matchedCard); // 여기도 Contains 체크 제거
                        }
                    }
                }
            }
        }
    }

    private void OnItemLibraryUpdated()
    {
        SyncCardDatabaseFromLibrary();
        RefreshPoolPage(true);
    }

    private string GetCardID(LSO_CardSO card)
    {
        if (card == null) return string.Empty;
        return $"{card.GetType().Name}_{card.name}";
    }

    public void InitializeDeckBuilder()
    {
        currentPageIndex = 0;

        SyncCardDatabaseFromLibrary();
        BuildInitialInventory();

        ClearContainerImmediate(poolContainer);
        RefreshInventoryView(true);
        RefreshPoolPage(true);
    }

    private void BuildInitialInventory()
    {
        _inventoryCardIDs.Clear();

        IReadOnlyList<LSO_CardSO> sourceCards = initialInventoryCards;

        if (KTH_DeckDataPersistent.Instance != null &&
            KTH_DeckDataPersistent.Instance.SavedInventory != null &&
            KTH_DeckDataPersistent.Instance.SavedInventory.Count > 0)
        {
            sourceCards = KTH_DeckDataPersistent.Instance.SavedInventory;
        }

        if (sourceCards == null)
            return;

        foreach (LSO_CardSO card in sourceCards)
        {
            if (card == null)
                continue;

            string cardID = GetCardID(card);

            if (!string.IsNullOrEmpty(cardID))
            {
                _inventoryCardIDs.Add(cardID);
            }
        }
    }

    private void RefreshInventoryView(bool useFlipAnim = false)
    {
        if (inventoryContainer == null || cardUIPrefab == null) return;

        ClearContainerImmediate(inventoryContainer);

        for (int i = 0; i < _inventoryCardIDs.Count; i++)
        {
            string id = _inventoryCardIDs[i];
            LSO_CardSO card = cardDatabase.Find(c => GetCardID(c) == id);

            if (card == null) continue;

            int dbIndex = cardDatabase.IndexOf(card);
            CreateCardUI(card, inventoryContainer, dbIndex, i * cardAnimInterval, useFlipAnim);
        }

        RefreshLayout(inventoryContainer);
        UpdateCompleteButtonState();
    }

    public List<LSO_CardSO> GetCurrentInventoryCardData()
    {
        List<LSO_CardSO> inventoryList = new List<LSO_CardSO>();

        foreach (string id in _inventoryCardIDs)
        {
            LSO_CardSO card = cardDatabase.Find(c => GetCardID(c) == id);
            if (card != null) inventoryList.Add(card);
        }

        return inventoryList;
    }

    private void HandleCardDropped(KTH_CardDragUI cardUI, bool droppedInInventory)
    {
        if (cardUI == null || cardUI.CardData == null) return;

        string cardID = GetCardID((LSO_CardSO)cardUI.CardData);

        if (droppedInInventory)
        {
            if (!cardUI.IsFromInventory)
            {
                _inventoryCardIDs.Add(cardID);
            }
        }
        else
        {
            _inventoryCardIDs.Remove(cardID);
        }

        RefreshInventoryView();
        RefreshPoolPage();
    }

    public void RefreshPoolPage(bool useFlipAnim = false)
    {
        if (poolContainer == null || cardUIPrefab == null || cardDatabase == null) return;

        ClearContainerImmediate(poolContainer);

        int startIndex = currentPageIndex * cardsPerPage;
        int endIndex = Mathf.Min(startIndex + cardsPerPage, cardDatabase.Count);

        int spawnedCount = 0;

        for (int i = startIndex; i < endIndex; i++)
        {
            LSO_CardSO card = cardDatabase[i];
            if (card == null) continue;

            string cardID = GetCardID(card);

            int countInInventory = _inventoryCardIDs.FindAll(id => id == cardID).Count;
            int countInPoolSoFar = 0;

            for (int k = startIndex; k < i; k++)
            {
                if (cardDatabase[k] != null && GetCardID(cardDatabase[k]) == cardID)
                    countInPoolSoFar++;
            }

            if (countInPoolSoFar < countInInventory)
            {
                continue;
            }

            CreateCardUI(card, poolContainer, i, spawnedCount * cardAnimInterval, useFlipAnim);
            spawnedCount++;
        }

        RefreshLayout(poolContainer);
        UpdatePageButtons();
    }

    private void CreateCardUI(LSO_CardSO data, Transform parent, int databaseIndex, float delay = 0f, bool useFlipAnimation = false)
    {
        if (data == null) return;

        var cardUI = Instantiate(cardUIPrefab, parent);

        bool isInventoryCard = (parent == inventoryContainer);

        cardUI.Setup(data, databaseIndex, HandleCardDropped, isInventoryCard);

        RectTransform cardRect = cardUI.GetComponent<RectTransform>();
        if (cardRect != null)
        {
            cardRect.sizeDelta = targetCardSize;

            if (useFlipAnimation)
            {
                cardRect.localScale = new Vector3(0f, 1f, 1f);
                cardRect.localRotation = Quaternion.Euler(0f, 180f, 0f);

                Sequence seq = DOTween.Sequence().SetTarget(cardRect);
                seq.PrependInterval(delay);
                seq.Join(cardRect.DOScale(Vector3.one, flipAnimDuration).SetEase(Ease.OutBack));
                seq.Join(cardRect.DORotate(Vector3.zero, flipAnimDuration).SetEase(Ease.OutCubic));
            }
            else
            {
                cardRect.localScale = Vector3.one;
                cardRect.localRotation = Quaternion.identity;
            }
        }
    }

    private void OnResetButtonClick()
    {
        if (isAnimating) return;

        if (_inventoryCardIDs.Count == 0)
        {
            _inventoryCardIDs.Clear();
            currentPageIndex = 0;
            RefreshPoolPage(true);
            return;
        }

        isAnimating = true;

        Sequence resetSequence = DOTween.Sequence().SetTarget(this);
        Vector3 targetPoolPos = poolContainer.position;

        if (inventoryContainer.childCount == 0)
        {
            _inventoryCardIDs.Clear();
            RefreshInventoryView();
            currentPageIndex = 0;
            RefreshPoolPage(true);
            isAnimating = false;
            return;
        }

        foreach (Transform child in inventoryContainer)
        {
            child.DOKill();
            resetSequence.Join(child.DOMove(targetPoolPos, resetAnimDuration).SetEase(Ease.InQuad));
            resetSequence.Join(child.DOScale(Vector3.zero, resetAnimDuration).SetEase(Ease.InQuad));
        }

        resetSequence.OnComplete(() =>
        {
            _inventoryCardIDs.Clear();
            RefreshInventoryView();
            currentPageIndex = 0;
            RefreshPoolPage(true);
            isAnimating = false;
        });
    }

    public void UpdatePageButtons()
    {
        int totalCards = cardDatabase != null ? cardDatabase.Count : 0;
        int maxPages = Mathf.Max(1, Mathf.CeilToInt((float)totalCards / cardsPerPage));

        if (prevButton) prevButton.interactable = (currentPageIndex > 0) && !isAnimating;
        if (nextButton) nextButton.interactable = (currentPageIndex + 1 < maxPages) && !isAnimating;
    }

    private void UpdateCompleteButtonState()
    {
        if (completeButton == null) return;
        completeButton.interactable = (_inventoryCardIDs.Count > 0) && !isAnimating;
    }

    public void OnNextPageButtonClick()
    {
        if (isAnimating) return;

        int totalCards = cardDatabase != null ? cardDatabase.Count : 0;
        int maxPages = Mathf.CeilToInt((float)totalCards / cardsPerPage);

        if (currentPageIndex + 1 < maxPages)
        {
            currentPageIndex++;
            RefreshPoolPage(false);
        }
    }

    public void OnPrevPageButtonClick()
    {
        if (isAnimating) return;

        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            RefreshPoolPage(false);
        }
    }

    private void ClearContainerImmediate(Transform container)
    {
        if (!container) return;

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Transform child = container.GetChild(i);
            child.DOKill(true);
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
        if (isAnimating) return;

        if (_inventoryCardIDs.Count <= 0)
        {
            Debug.LogWarning("[DeckBuilder] 최소 1장 이상의 카드가 필요합니다.");

            if (completeButton != null)
            {
                completeButton.transform.DOComplete();
                completeButton.transform.DOPunchPosition(new Vector3(10f, 0f, 0f), 0.3f, 10, 1f);
            }
            return;
        }

        if (completeButton) completeButton.interactable = false;

        List<LSO_CardSO> currentInventoryList = GetCurrentInventoryCardData();

        if (KTH_DeckDataPersistent.Instance != null)
        {
            KTH_DeckDataPersistent.Instance.SaveInventory(currentInventoryList);
        }

        SceneManager.LoadScene(nextSceneName);
    }

    public void AddCardsToInventory(List<LSO_CardSO> newCards)
    {
        if (newCards == null || newCards.Count == 0) return;

        foreach (var card in newCards)
        {
            if (card == null) continue;
            string cardID = GetCardID(card);

            if (!string.IsNullOrEmpty(cardID))
            {
                _inventoryCardIDs.Add(cardID);
            }
        }

        RefreshInventoryView(true);
        RefreshPoolPage(true);
    }
}