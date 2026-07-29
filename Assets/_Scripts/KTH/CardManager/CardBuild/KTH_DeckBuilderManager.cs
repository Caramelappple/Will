using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class KTH_DeckBuilderManager : MonoBehaviour
{
    [Header("카드 데이터 리스트")]
    public List<KTH_CardData> cardDatabase = new List<KTH_CardData>(); // 전체 카드 데이터 리스트
    public List<KTH_CardData> initialInventoryCards = new List<KTH_CardData>(); // 시작 시 인벤토리에 들어갈 카드 리스트 (선택)

    [Header("UI 프리팹 및 컨테이너")]
    public KTH_CardDragUI cardUIPrefab;          // UI 카드 프리팹
    public RectTransform poolContainer;          // 상단: 카드 풀 패널 (선택 가능 구역)
    public RectTransform inventoryContainer;     // 하단: 인벤토리 패널 (덱 구역)

    [Header("카드 UI 크기 설정")]
    public Vector2 targetCardSize = new Vector2(120f, 160f);

    [Header("페이지네이션 설정 (상단 풀)")]
    public int cardsPerPage = 3;                 // 한 페이지에 보여줄 카드 수
    private int currentPageIndex = 0;            // 현재 페이지 번호 (0부터 시작)

    [Header("페이지 이동 버튼")]
    public Button prevButton;                    // 이전 페이지 버튼
    public Button nextButton;                    // 다음 페이지 버튼

    [Header("기타 버튼 설정")]
    public Button completeButton;                // 완료 (다음 씬 이동) 버튼
    public Button resetButton;                   // 인벤토리 리셋 버튼
    public string nextSceneName = "KTH_BattleScene"; // 이동할 전투 씬 이름

    private void Start()
    {
        if (completeButton)
            completeButton.onClick.AddListener(OnCompleteButtonClick);

        if (resetButton)
            resetButton.onClick.AddListener(OnResetButtonClick);

        // 페이지 이동 버튼 이벤트 연결
        if (prevButton)
            prevButton.onClick.AddListener(OnPrevPageButtonClick);

        if (nextButton)
            nextButton.onClick.AddListener(OnNextPageButtonClick);

        // 씬 시작 시 카드 소환 및 초기 세팅
        InitializeDeckBuilder();
    }

    // ★ 키보드 화살표 입력 처리 추가
    private void Update()
    {
        // 왼쪽 화살표 키(←) 또는 A키 입력
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            OnPrevPageButtonClick();
        }

        // 오른쪽 화살표 키(→) 또는 D키 입력
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            OnNextPageButtonClick();
        }
    }

    /// <summary>시작 시 카드 리스트를 받아 UI에 소환</summary>
    public void InitializeDeckBuilder()
    {
        currentPageIndex = 0;

        // 1. 기존 UI 오브젝트 초기화
        ClearContainer(poolContainer);
        ClearContainer(inventoryContainer);

        // 2. 상단 풀(Pool) 카드를 페이지에 맞춰 생성
        RefreshPoolPage();

        // 3. 시작 시 기본으로 들어가야 할 인벤토리 카드가 있다면 소환
        if (initialInventoryCards != null && cardUIPrefab != null && inventoryContainer != null)
        {
            foreach (var cardData in initialInventoryCards)
            {
                CreateCardUI(cardData, inventoryContainer);
            }
            RefreshLayout(inventoryContainer);
        }
    }

    /// <summary>현재 페이지 번호에 맞춰 상단 풀(Pool) 카드들을 새로고침</summary>
    private void RefreshPoolPage()
    {
        if (poolContainer == null || cardUIPrefab == null) return;

        // 기존 상단 풀 카드 삭제
        ClearContainer(poolContainer);

        if (cardDatabase != null && cardDatabase.Count > 0)
        {
            // 현재 페이지에서 보여줄 시작 인덱스와 끝 인덱스 계산
            int startIndex = currentPageIndex * cardsPerPage;
            int endIndex = Mathf.Min(startIndex + cardsPerPage, cardDatabase.Count);

            // 해당 범위의 카드만 생성
            for (int i = startIndex; i < endIndex; i++)
            {
                CreateCardUI(cardDatabase[i], poolContainer);
            }

            RefreshLayout(poolContainer);
        }

        // 페이지 버튼 상태 (활성화/비활성화) 갱신
        UpdatePageButtons();
    }

    /// <summary>이전/다음 버튼 활성화 상태 조절</summary>
    public void UpdatePageButtons()
    {
        int totalCards = cardDatabase != null ? cardDatabase.Count : 0;

        // 전체 페이지 수 계산
        int maxPages = Mathf.CeilToInt((float)totalCards / cardsPerPage);

        // 이전 버튼: 첫 번째 페이지(0)보다 커야 활성화
        if (prevButton)
            prevButton.interactable = (currentPageIndex > 0);

        // 다음 버튼: (현재 페이지 + 1)이 전체 페이지 수보다 작을 때만 활성화
        if (nextButton)
            nextButton.interactable = (currentPageIndex + 1 < maxPages);
    }

    /// <summary>다음 페이지 버튼 클릭 / 오른쪽 화살표</summary>
    public void OnNextPageButtonClick()
    {
        int totalCards = cardDatabase != null ? cardDatabase.Count : 0;
        int maxPages = Mathf.CeilToInt((float)totalCards / cardsPerPage);

        if (currentPageIndex + 1 < maxPages)
        {
            currentPageIndex++;
            RefreshPoolPage();
            Debug.Log($"[페이지 이동] 다음 페이지로 이동 ({currentPageIndex + 1}/{maxPages})");
        }
    }

    /// <summary>이전 페이지 버튼 클릭 / 왼쪽 화살표</summary>
    public void OnPrevPageButtonClick()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            RefreshPoolPage();
            int totalCards = cardDatabase != null ? cardDatabase.Count : 0;
            int maxPages = Mathf.CeilToInt((float)totalCards / cardsPerPage);
            Debug.Log($"[페이지 이동] 이전 페이지로 이동 ({currentPageIndex + 1}/{maxPages})");
        }
    }

    /// <summary>RectTransform을 사용하여 카드 UI 인스턴스 생성 및 크기 고정</summary>
    private void CreateCardUI(KTH_CardData data, Transform parent)
    {
        if (data == null) return;

        var cardUI = Instantiate(cardUIPrefab, parent);
        cardUI.Setup(data);

        RectTransform cardRect = cardUI.GetComponent<RectTransform>();
        if (cardRect != null)
        {
            cardRect.localScale = Vector3.one;
            cardRect.sizeDelta = targetCardSize;
        }
    }

    /// <summary>리셋 버튼 클릭 시 인벤토리의 모든 카드를 상단 풀로 이동 후 1페이지로 리셋</summary>
    private void OnResetButtonClick()
    {
        ClearContainer(inventoryContainer);
        currentPageIndex = 0;
        RefreshPoolPage();
        RefreshLayout(inventoryContainer);

        Debug.Log("[DeckBuilderManager] 인벤토리 카드가 리셋되었습니다.");
    }

    /// <summary>컨테이너 자식 요소 즉시 삭제</summary>
    private void ClearContainer(Transform container)
    {
        if (!container) return;
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>UI 레이아웃(Grid Layout Group) 크기 반영 및 강제 즉시 갱신</summary>
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

    /// <summary>완료 버튼 클릭 시 -> 하단 인벤토리에 들어간 카드 리스트를 추출하여 저장 후 씬 전환</summary>
    private void OnCompleteButtonClick()
    {
        if (completeButton) completeButton.interactable = false;

        List<KTH_CardData> currentInventoryList = new List<KTH_CardData>();

        if (inventoryContainer != null)
        {
            foreach (Transform child in inventoryContainer)
            {
                var cardItem = child.GetComponent<KTH_CardDragUI>();
                if (cardItem != null && cardItem.CardData != null)
                {
                    currentInventoryList.Add(cardItem.CardData);
                }
            }
        }

        if (currentInventoryList.Count == 0)
        {
            Debug.LogWarning("[DeckBuilderManager] 인벤토리에 배치된 카드가 없습니다!");
        }

        if (KTH_DeckDataPersistent.Instance != null)
        {
            KTH_DeckDataPersistent.Instance.SaveInventory(currentInventoryList);
            Debug.Log($"[DeckBuilderManager] 총 {currentInventoryList.Count}장의 카드가 인벤토리 리스트에 저장되었습니다.");
        }

        SceneManager.LoadScene(nextSceneName);
    }
}