using _Scripts.LSO.Deck.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class KTH_BuildUi : MonoBehaviour, IDropHandler
{
    [Header("Card UI")]
    [SerializeField] private KTH_SelectCardUi cardPrefab;
    [SerializeField] private Transform cardParent;

    [Header("Pagination Settings")]
    [SerializeField] private int itemsPerPage = 5;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button prevButton;

    [Header("Page Indicator UI")]
    [SerializeField] private TextMeshProUGUI pageText;

    [Header("Managers")]
    [SerializeField] private KTH_DeckBuildManager buildManager;
    [SerializeField] private KTH_FinalCardList finalCardList;

    private readonly Pagination _pagination = new Pagination();

    public int CurrentPage => _pagination.CurrentPage;

    private void Awake()
    {
        if (buildManager == null) buildManager = FindAnyObjectByType<KTH_DeckBuildManager>();
        if (finalCardList == null) finalCardList = FindAnyObjectByType<KTH_FinalCardList>();

        if (nextButton != null) nextButton.onClick.AddListener(OnClickNextPage);
        if (prevButton != null) prevButton.onClick.AddListener(OnClickPrevPage);
    }

    private void OnEnable()
    {
        if (buildManager != null) buildManager.OnCardListChanged += HandleCardListChanged;
        RefreshUI(playAnimation: true); // [최초 시작 시] 회전 애니메이션 재생
    }

    private void OnDisable()
    {
        if (buildManager != null) buildManager.OnCardListChanged -= HandleCardListChanged;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.used) return;

        KTH_SelectCardUi cardUi = eventData.pointerDrag?.GetComponent<KTH_SelectCardUi>();
        if (cardUi == null || !cardUi.IsInInventory) return;

        eventData.Use();

        int targetPage = _pagination.CurrentPage - 1;

        if (buildManager.ReturnCardToPage(targetPage, cardUi.CardData))
        {
            cardUi.MarkDroppedSuccess();
            if (finalCardList != null) finalCardList.RemoveCard(cardUi.CardData);
            Destroy(cardUi.gameObject);
        }
    }

    private void HandleCardListChanged()
    {
        CancelInvoke(nameof(RefreshUIWithoutAnim));
        Invoke(nameof(RefreshUIWithoutAnim), 0.01f);
    }

    private void RefreshUIWithoutAnim()
    {
        RefreshUI(playAnimation: false); // 드래그 앤 드롭 시에는 애니메이션 없이 갱신
    }

    public void RefreshUI(bool playAnimation = false)
    {
        if (buildManager == null) return;

        int savedPage = _pagination.CurrentPage > 0 ? _pagination.CurrentPage : 1;
        int totalPages = Mathf.Max(1, buildManager.TotalPages);

        _pagination.Setup(totalPages * itemsPerPage, itemsPerPage);

        int targetPage = Mathf.Clamp(savedPage, 1, totalPages);
        while (_pagination.CurrentPage < targetPage) _pagination.NextPage();
        while (_pagination.CurrentPage > targetPage) _pagination.PrevPage();

        UpdateCardUI(playAnimation);
    }

    private void UpdateCardUI(bool playAnimation = false)
    {
        ClearCardUI();

        int currentPageIndex = _pagination.CurrentPage - 1;
        var pageCards = buildManager.GetCardsAtPage(currentPageIndex);

        for (int i = 0; i < pageCards.Count; i++)
        {
            KTH_SelectCardUi cardUI = Instantiate(cardPrefab, cardParent);
            float delay = i * 0.05f;
            cardUI.Setup(pageCards[i], currentPageIndex, delay, playAnimation);
        }

        UpdatePageButtons();
        UpdatePageText();
    }

    private void OnClickNextPage()
    {
        if (_pagination.NextPage()) UpdateCardUI(playAnimation: true); // [페이지 이동 시] 애니메이션 재생
    }

    private void OnClickPrevPage()
    {
        if (_pagination.PrevPage()) UpdateCardUI(playAnimation: true); // [페이지 이동 시] 애니메이션 재생
    }

    private void UpdatePageButtons()
    {
        if (nextButton != null) nextButton.interactable = _pagination.CurrentPage < buildManager.TotalPages;
        if (prevButton != null) prevButton.interactable = _pagination.CurrentPage > 1;
    }

    private void UpdatePageText()
    {
        if (pageText != null)
        {
            pageText.text = $"{_pagination.CurrentPage} / {buildManager.TotalPages}";
        }
    }

    private void ClearCardUI()
    {
        for (int i = cardParent.childCount - 1; i >= 0; i--)
        {
            Destroy(cardParent.GetChild(i).gameObject);
        }
    }
}