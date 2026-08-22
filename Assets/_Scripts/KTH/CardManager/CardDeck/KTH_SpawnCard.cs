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
        // 드로우 버튼을 통한 일반 드로우는 항상 턴당 횟수 제한을 따른다.
        SpawnOneCard(bypassDrawLimit: false);
    }

    /// <summary>
    /// 외부(KTH_StartCardSet)에서 호출할 수 있는 카드 1장 드로우 함수 (턴 제한 적용)
    /// </summary>
    public bool SpawnOneCardPublic()
    {
        return SpawnOneCard(bypassDrawLimit: false);
    }

    /// <param name="bypassDrawLimit">
    /// true면 턴당 드로우 횟수 제한을 무시하고 뽑는다. (시작 핸드 셋업 전용)
    /// 덱에 카드가 없거나 손패가 가득 찬 경우에는 여전히 중단된다.
    /// </param>
    private bool SpawnOneCard(bool bypassDrawLimit = false)
    {
        if (deckManager == null || cardPrefab == null || handLayout == null)
        {
            Debug.LogError($"[KTH_SpawnCard] 참조 누락! deckManager:{deckManager != null}, cardPrefab:{cardPrefab != null}, handLayout:{handLayout != null}");
            return false;
        }

        // 손패가 이미 가득 찼으면 덱을 건드리지 않고 바로 중단
        if (handLayout.IsFull)
        {
            Debug.LogWarning($"[KTH_SpawnCard] 손패가 가득 차서 드로우할 수 없습니다! ({handLayout.HandCount}/{handLayout.MaxHandSize})");
            RefreshDrawButtonState();
            return false;
        }

        // 턴당 드로우 횟수를 이미 다 썼으면 중단 (bypassDrawLimit이면 통과)
        if (!bypassDrawLimit && !deckManager.CanDraw())
        {
            Debug.LogWarning($"[KTH_SpawnCard] 이번 턴 드로우 횟수를 모두 사용했습니다! ({deckManager.DrawsUsedThisTurn}/{deckManager.MaxDrawsPerTurn})");
            RefreshDrawButtonState();
            return false;
        }

        LSO_CardSO cardData = deckManager.DrawCard(bypassTurnLimit: bypassDrawLimit);
        if (cardData == null)
        {
            Debug.LogWarning("[KTH_SpawnCard] 덱에 남아있는 카드가 없습니다! (deck.Count == 0)");
            RefreshDrawButtonState();
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

        Debug.Log($"[KTH_SpawnCard] 카드 생성 완료: {cardData.name} | 남은 덱: {deckManager.RemainingCards} | 손패: {handLayout.HandCount}/{handLayout.MaxHandSize} | 드로우: {deckManager.DrawsUsedThisTurn}/{deckManager.MaxDrawsPerTurn}{(bypassDrawLimit ? " (제한 우회)" : "")}");

        RefreshDrawButtonState();

        return true;
    }

    public bool SpawnOneCardPublic(bool bypassDrawLimit = false)
    {
        return SpawnOneCard(bypassDrawLimit);
    }

    public void SpawnStartingHand(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (!SpawnOneCard(bypassDrawLimit: true)) break;
        }
    }
}