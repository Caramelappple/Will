using _Scripts.LSO.Deck.Data;
using UnityEngine;

public class KTH_SpawnCard : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KTH_DeckManager deckManager;
    [SerializeField] private KTH_DrawButton drawButton;
    [SerializeField] private KTH_HandCard cardPrefab;
    [SerializeField] private KTH_HandCardLayout handLayout;

    private void Awake()
    {
        if (deckManager == null) deckManager = FindAnyObjectByType<KTH_DeckManager>();
        if (drawButton == null) drawButton = FindAnyObjectByType<KTH_DrawButton>();
        if (handLayout == null) handLayout = FindAnyObjectByType<KTH_HandCardLayout>();
    }

    private void OnEnable()
    {
        if (drawButton != null) drawButton.OnDrawRequested += SpawnNextCard;
    }

    private void OnDisable()
    {
        if (drawButton != null) drawButton.OnDrawRequested -= SpawnNextCard;
    }

    private void SpawnNextCard()
    {
        // 1. 필수 컴포넌트 연결 상태 체크
        if (deckManager == null || cardPrefab == null || handLayout == null)
        {
            Debug.LogError($"[KTH_SpawnCard] 참조 누락! deckManager:{deckManager != null}, cardPrefab:{cardPrefab != null}, handLayout:{handLayout != null}");
            return;
        }

        // 2. 덱에서 카드가 뽑히는지 체크
        LSO_CardSO cardData = deckManager.DrawCard();
        if (cardData == null)
        {
            Debug.LogWarning("[KTH_SpawnCard] 덱에 남아있는 카드가 없습니다! (deck.Count == 0)");
            if (drawButton != null) drawButton.SetInteractable(false);
            return;
        }

        // 3. 카드 정상 생성 체크
        KTH_HandCard newCard = Instantiate(cardPrefab, handLayout.transform);
        newCard.Setup(cardData);
        handLayout.AddCard(newCard);

        Debug.Log($"[KTH_SpawnCard] 카드 생성 완료: {cardData.name} | 남은 카드: {deckManager.RemainingCards}");

        if (deckManager.RemainingCards == 0 && drawButton != null)
        {
            drawButton.SetInteractable(false);
        }
    }
}