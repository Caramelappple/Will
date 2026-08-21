using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class KTH_Inventory : MonoBehaviour, IDropHandler
{
    [SerializeField] private KTH_FinalCardList finalCardList;
    [SerializeField] private KTH_DeckBuildManager buildManager;
    [SerializeField] private KTH_BuildUi buildUi;

    [Header("Buttons")]
    [SerializeField] private Button confirmDeck;
    [SerializeField] private Button reset;

    private void Awake()
    {
        if (finalCardList == null) finalCardList = FindAnyObjectByType<KTH_FinalCardList>();
        if (buildManager == null) buildManager = FindAnyObjectByType<KTH_DeckBuildManager>();
        if (buildUi == null) buildUi = FindAnyObjectByType<KTH_BuildUi>();
    }

    private void OnEnable()
    {
        // buildManager 및 버튼 Null 예외 방지 바인딩
        if (buildManager != null)
        {
            if (reset != null)
            {
                reset.onClick.RemoveListener(OnResetClicked);
                reset.onClick.AddListener(OnResetClicked);
            }

            if (confirmDeck != null)
            {
                confirmDeck.onClick.RemoveListener(OnConfirmClicked);
                confirmDeck.onClick.AddListener(OnConfirmClicked);
            }
        }
    }

    private void OnDisable()
    {
        if (reset != null) reset.onClick.RemoveListener(OnResetClicked);
        if (confirmDeck != null) confirmDeck.onClick.RemoveListener(OnConfirmClicked);
    }

    private void OnResetClicked()
    {
        if (buildManager != null) buildManager.ResetDeck();
    }

    private void OnConfirmClicked()
    {
        if (buildManager != null) buildManager.ConfirmDeck();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.used) return;
        if (finalCardList == null || buildManager == null || buildUi == null) return;

        KTH_SelectCardUi cardUi = eventData.pointerDrag?.GetComponent<KTH_SelectCardUi>();
        if (cardUi == null) return;

        // 이미 인벤토리에 들어와 있는 카드는 덱에서 차감하지 않음
        if (cardUi.IsInInventory) return;

        eventData.Use();

        int currentPageIndex = buildUi.CurrentPage - 1;

        // 덱 패널에 있던 카드를 인벤토리로 가져올 때만 실행
        if (buildManager.RemoveCardFromPage(currentPageIndex, cardUi.CardData))
        {
            finalCardList.AddCard(cardUi.CardData);
            cardUi.MoveToInventory(transform);
        }
    }
}