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
        SpawnOneCard();
    }

    /// <summary>
    /// 외부(KTH_StartCardSet)에서 호출할 수 있는 카드 1장 드로우 함수
    /// </summary>
    public bool SpawnOneCardPublic()
    {
        return SpawnOneCard();
    }

    private bool SpawnOneCard()
    {
        if (deckManager == null || cardPrefab == null || handLayout == null)
        {
            Debug.LogError($"[KTH_SpawnCard] 참조 누락! deckManager:{deckManager != null}, cardPrefab:{cardPrefab != null}, handLayout:{handLayout != null}");
            return false;
        }

        LSO_CardSO cardData = deckManager.DrawCard();
        if (cardData == null)
        {
            Debug.LogWarning("[KTH_SpawnCard] 덱에 남아있는 카드가 없습니다! (deck.Count == 0)");
            if (drawButton != null) drawButton.SetInteractable(false);
            return false;
        }

        // 1. 기본 생성
        KTH_HandCard newCard = Instantiate(cardPrefab, handLayout.transform);

        // 2. 초기 회전/스케일 강제 리셋
        newCard.transform.localRotation = Quaternion.identity;
        newCard.transform.localScale = Vector3.one;

        newCard.Setup(cardData);

        // 3. 드로우 버튼 위치 설정
        if (drawButton != null)
        {
            newCard.SetSpawnPosition(drawButton.transform.position);
        }

        // 4. 손패에 추가 (애니메이션 이동 시작)
        handLayout.AddCard(newCard);

        Debug.Log($"[KTH_SpawnCard] 카드 생성 완료: {cardData.name} | 남은 카드: {deckManager.RemainingCards}");

        if (deckManager.RemainingCards == 0 && drawButton != null)
        {
            drawButton.SetInteractable(false);
        }

        return true;
    }

    public void SpawnStartingHand(int count)
    {
        // KTH_StartCardSet에서 코루틴으로 처리하므로 이 구문은 비워두거나 
        // 외부 직접 호출용으로 남겨둡니다.
        for (int i = 0; i < count; i++)
        {
            if (!SpawnOneCard()) break;
        }
    }
}