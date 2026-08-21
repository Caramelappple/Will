using System;
using System.Collections.Generic;
using _Scripts.LSO.Deck.Data;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[Serializable]
public class CardPageBucket
{
    public List<LSO_CardSO> cards = new List<LSO_CardSO>();
}

public class KTH_DeckBuildManager : MonoBehaviour, IDropHandler
{
    [Header("Layout Area Reference")]
    [SerializeField] private RectTransform deckLayoutArea;

    [SerializeField] private List<LSO_CardSO> selectedCards = new();
    [SerializeField] private List<CardPageBucket> pageBuckets = new();

    [SerializeField]private string sceneName;

    public event Action OnCardListChanged;

    public IReadOnlyList<LSO_CardSO> SelectedCards => selectedCards;
    public int TotalPages => pageBuckets.Count;

    private ItemLibraryManager itemLibraryManager;
    private KTH_FinalCardList finalCardList;
    private int _itemsPerPage = 5;

    private void Awake()
    {
        itemLibraryManager = FindAnyObjectByType<ItemLibraryManager>();
        finalCardList = FindAnyObjectByType<KTH_FinalCardList>();
    }

    private void Start()
    {
        InitPageBuckets(_itemsPerPage);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.used) return;

        KTH_SelectCardUi cardUi = eventData.pointerDrag?.GetComponent<KTH_SelectCardUi>();
        if (cardUi == null || !cardUi.IsInInventory) return;

        if (!IsInsideDeckLayout(eventData)) return;

        eventData.Use();

        // [수정] 원본 페이지가 아닌 현재 화면에 보이는 페이지에 드롭
        KTH_BuildUi buildUi = FindAnyObjectByType<KTH_BuildUi>();
        int targetPage = buildUi != null ? buildUi.CurrentPage - 1 : cardUi.OriginalPageIndex;

        if (ReturnCardToPage(targetPage, cardUi.CardData))
        {
            cardUi.MarkDroppedSuccess();

            if (finalCardList != null)
            {
                finalCardList.RemoveCard(cardUi.CardData);
            }

            Destroy(cardUi.gameObject);
        }
    }

    public bool ReturnCardToPage(int pageIndex, LSO_CardSO card)
    {
        if (card == null) return false;

        if (pageIndex < 0 || pageIndex >= pageBuckets.Count)
            return false;

        var targetBucket = pageBuckets[pageIndex].cards;

        // [핵심] 현재 페이지 자리가 꽉 찼으면 다른 페이지로 넘기지 않고 실패 처리 (인벤토리 원복용)
        if (targetBucket.Count >= _itemsPerPage) return false;

        targetBucket.Add(card);
        selectedCards.Remove(card);
        OnCardListChanged?.Invoke();
        return true;
    }

    public bool IsInsideDeckLayout(PointerEventData eventData)
    {
        if (deckLayoutArea == null) return true;

        return RectTransformUtility.RectangleContainsScreenPoint(
            deckLayoutArea,
            eventData.position,
            eventData.pressEventCamera
        );
    }

    public void InitPageBuckets(int itemsPerPage)
    {
        _itemsPerPage = Mathf.Max(1, itemsPerPage);
        pageBuckets.Clear();

        if (itemLibraryManager == null || itemLibraryManager.UnlockedPieces == null) return;

        var unlocked = itemLibraryManager.UnlockedPieces;
        List<LSO_CardSO> currentBucket = new List<LSO_CardSO>();

        for (int i = 0; i < unlocked.Count; i++)
        {
            currentBucket.Add(unlocked[i]);

            if (currentBucket.Count >= _itemsPerPage || i == unlocked.Count - 1)
            {
                pageBuckets.Add(new CardPageBucket { cards = new List<LSO_CardSO>(currentBucket) });
                currentBucket = new List<LSO_CardSO>();
            }
        }

        OnCardListChanged?.Invoke();
    }

    public IReadOnlyList<LSO_CardSO> GetCardsAtPage(int pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= pageBuckets.Count)
            return new List<LSO_CardSO>();

        return pageBuckets[pageIndex].cards;
    }

    public bool RemoveCardFromPage(int pageIndex, LSO_CardSO card)
    {
        if (pageIndex < 0 || pageIndex >= pageBuckets.Count || card == null)
            return false;

        var targetPage = pageBuckets[pageIndex].cards;

        if (targetPage.Remove(card))
        {
            selectedCards.Add(card);
            OnCardListChanged?.Invoke();
            return true;
        }

        return false;
    }

    // [리셋 버튼 클릭 시 호출]
    public void ResetDeck()
    {
        // 1. 선택된 카드 데이터 초기화
        selectedCards.Clear();

        // 2. 인벤토리 카드의 UI 및 데이터 싹 지우기
        if (finalCardList != null)
        {
            finalCardList.ClearCards();
        }

        // 3. 덱 버킷 처음 상태로 재구성
        InitPageBuckets(_itemsPerPage);
    }

    // [최종 결정 버튼 클릭 시 호출]
    public void ConfirmDeck()
    {
        // 인벤토리에 선택된 카드가 있는지 확인 (원하는 최소 수량으로 변경 가능)
        if (selectedCards == null || selectedCards.Count == 0)
        {
            Debug.LogWarning("덱에 선택된 카드가 없습니다!");
            return;
        }

        Debug.Log($"최종 덱 구성 완료! 총 {selectedCards.Count}장의 카드가 선택되었습니다.");
        // TODO: 저장 처리 또는 전투/다음 씬으로 넘어가기

        SceneManager.LoadScene(sceneName);
    }
}