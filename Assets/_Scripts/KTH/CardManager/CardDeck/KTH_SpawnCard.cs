using _Scripts.LSO.Deck.Data;
using UnityEngine;

public class KTH_SpawnCard : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KTH_DeckManager deckManager;
    [SerializeField] private KTH_DrawButton drawButton;
    [SerializeField] private KTH_HandCard cardPrefab;
    [SerializeField] private KTH_HandCardLayout handLayout;

    private void OnEnable()
    {
        if (drawButton != null) drawButton.OnDrawRequested += SpawnNextCard;
        if (handLayout != null) handLayout.OnHandCountChanged += HandleHandCountChanged;
        if (deckManager != null) deckManager.OnDrawLimitChanged += HandleDrawLimitChanged;
    }

    private void OnDisable()
    {
        if (drawButton != null) drawButton.OnDrawRequested -= SpawnNextCard;
        if (handLayout != null) handLayout.OnHandCountChanged -= HandleHandCountChanged;
        if (deckManager != null) deckManager.OnDrawLimitChanged -= HandleDrawLimitChanged;
    }

    private void RefreshDrawButtonState()
    {
        if (drawButton == null) return;

        bool deckHasCards = deckManager == null || deckManager.RemainingCards > 0;
        bool handHasRoom = handLayout == null || !handLayout.IsFull;
        bool drawAllowed = deckManager == null || deckManager.CanDraw();

        drawButton.SetInteractable(deckHasCards && handHasRoom && drawAllowed);
    }

    private void HandleHandCountChanged(int currentCount, int maxCount)
    {
        RefreshDrawButtonState();
    }

    private void HandleDrawLimitChanged()
    {
        RefreshDrawButtonState();
    }

    private void SpawnNextCard()
    {
        SpawnOneCard(bypassDrawLimit: false);
    }

    /// <summary>
    /// 외부(KTH_StartCardSet)에서 호출할 수 있는 카드 드로우 함수
    /// </summary>
    public bool SpawnOneCardPublic(bool bypassDrawLimit = false)
    {
        return SpawnOneCard(bypassDrawLimit);
    }

    private bool SpawnOneCard(bool bypassDrawLimit = false)
    {
        if (deckManager == null || cardPrefab == null || handLayout == null)
        {
            Debug.LogError($"[KTH_SpawnCard] 참조 누락! deckManager:{deckManager != null}, cardPrefab:{cardPrefab != null}, handLayout:{handLayout != null}");
            return false;
        }

        if (handLayout.IsFull)
        {
            Debug.LogWarning($"[KTH_SpawnCard] 손패가 가득 차서 드로우할 수 없습니다! ({handLayout.HandCount}/{handLayout.MaxHandSize})");
            RefreshDrawButtonState();
            return false;
        }

        if (!bypassDrawLimit && !deckManager.CanDraw())
        {
            Debug.LogWarning($"[KTH_SpawnCard] 이번 턴 드로우 횟수를 모두 사용했습니다! ({deckManager.DrawsUsedThisTurn}/{deckManager.MaxDrawsPerTurn})");
            RefreshDrawButtonState();
            return false;
        }

        LSO_CardSO cardData = deckManager.DrawCard(bypassTurnLimit: bypassDrawLimit);
        if (cardData == null)
        {
            Debug.LogWarning("[KTH_SpawnCard] 덱에 남아있는 카드가 없습니다!");
            RefreshDrawButtonState();
            return false;
        }

        // 1. Instantiation 및 초기 회전/스케일 설정
        KTH_HandCard newCard = Instantiate(cardPrefab, handLayout.transform);
        newCard.transform.localRotation = Quaternion.identity;
        newCard.transform.localScale = Vector3.one;

        newCard.Setup(cardData);

        // 2. 드로우 버튼 위치 설정 (시작 위치 저장)
        if (drawButton != null)
        {
            newCard.SetSpawnPosition(drawButton.transform.position);
        }

        // 3. 손패 추가 및 정렬 애니메이션 트리거
        // 드로우 버튼(스포너)이 손패 컨테이너 기준 왼쪽/오른쪽 중
        // 어디에 있는지에 따라 카드가 채워지는 방향이 자동으로 결정된다.
        // (스포너가 왼쪽 -> 손패가 왼쪽부터 채워짐,
        //  스포너가 오른쪽 -> 손패가 오른쪽부터 채워짐)
        if (drawButton != null)
        {
            handLayout.AddCard(newCard, drawButton.transform.position);
        }
        else
        {
            handLayout.AddCard(newCard);
        }

        Debug.Log($"[KTH_SpawnCard] 카드 생성 완료: {cardData.name} | 남은 덱: {deckManager.RemainingCards} | 손패: {handLayout.HandCount}/{handLayout.MaxHandSize}");

        RefreshDrawButtonState();

        return true;
    }

    public void SpawnStartingHand(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (!SpawnOneCard(bypassDrawLimit: true)) break;
        }
    }
}