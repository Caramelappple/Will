using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.UI.Feedback;
using UnityEngine;

/// <summary>
/// 덱에서 카드 한 장을 꺼내 손패에 놓는다.
///
/// ── 뽑는 두 가지 경로 ─────────────────────────────────────
/// 1) 시작 손패: KTH_StartCardSet 이 스테이지 첫 턴에 SpawnStartingHand 로 한 번에 준다.
///    턴당 드로우 제한(KTH_DeckManager.maxDrawsPerTurn)을 무시한다.
/// 2) 턴별 추가 드로우: 플레이어가 덱(KTH_DrawButton)을 클릭할 때마다 한 장씩.
///    턴당 제한이 걸려 있어서, 다 쓰면 버튼 콜라이더 자체가 꺼진다(RefreshDrawButtonState).
/// ─────────────────────────────────────────────────────────
///
/// 덱 오브젝트는 남아 있다. 카드가 **거기서 날아오기** 때문이다(drawOrigin).
/// </summary>
public class KTH_SpawnCard : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KTH_DeckManager deckManager;
    [SerializeField] private KTH_DrawButton drawButton;
    [SerializeField] private KTH_HandCard cardPrefab;
    [SerializeField] private KTH_HandCardLayout handLayout;

    [Tooltip("카드가 날아올 자리. 보통 덱 오브젝트를 꽂는다.\n" +
             "\n" +
             "비워두면 카드가 원점(0,0,0)에서 날아온다.\n" +
             "\n" +
             "이 자리가 손패 기준 왼쪽이면 손패가 왼쪽부터, 오른쪽이면 오른쪽부터 채워진다.")]
    [SerializeField] private Transform drawOrigin;

    // drawOrigin이 안 꽂혀있다는 경고를 이미 냈는지. 매번 뽑을 때마다 내면
    // 콘솔이 덮이니 한 번만 낸다. (SpawnOneCard 참고)
    private bool warnedMissingDrawOrigin;

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

    /// <summary>덱 오브젝트(KTH_DrawButton)를 클릭 가능/불가능으로 갱신한다.</summary>
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
    /// 카드 한 장을 뽑아 손패에 놓는다.
    ///
    /// 뽑을 수 없으면 false. 이유는 부르는 쪽이 아니라 아래쪽에서 이미 알린다
    /// (손패 가득 참은 여기서, 덱이 빈 것/턴당 제한은 KTH_DeckManager 가).
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
            // 손패가 찬 것도 규칙대로 돌아간 결과다. 콘솔이 아니라 화면으로 알린다.
            LSO_RejectSignal.Raise(LSO_RejectReason.HandFull);
            RefreshDrawButtonState();
            return false;
        }

        // 왜 못 뽑는지는 KTH_DeckManager 가 이미 알린다(덱이 빈 경우 거부 신호로,
        // 턴당 제한을 넘긴 경우 로그로). 여기서는 버튼 상태만 되돌린다.
        if (!bypassDrawLimit && !deckManager.CanDraw())
        {
            RefreshDrawButtonState();
            return false;
        }

        LSO_CardSO cardData = deckManager.DrawCard(bypassTurnLimit: bypassDrawLimit);
        if (cardData == null)
        {
            RefreshDrawButtonState();
            return false;
        }

        // 1. 오브젝트 풀에서 카드 대여 (풀 매니저가 없으면 안전하게 Instantiate로 대체)
        KTH_HandCard newCard =
            KTH_HandCardPool.Instance != null
                ? KTH_HandCardPool.Instance.Get(handLayout.transform)
                : Instantiate(cardPrefab, handLayout.transform);

        newCard.transform.localRotation = Quaternion.identity;
        newCard.transform.localScale = Vector3.one;

        handLayout.SetupCard(
            newCard,
            cardData
        );

        // 2. 날아오기 시작할 자리
        if (drawOrigin != null)
        {
            newCard.SetSpawnPosition(drawOrigin.position);
        }
        else if (!warnedMissingDrawOrigin)
        {
            // drawOrigin이 안 꽂혀있으면 SetSpawnPosition이 아예 안 불려서,
            // 카드가 Instantiate된 기본 위치(손패 자리 근처)에서 그대로 시작한다.
            // 그러면 PlayDrawAnimation의 시작점=도착점이 되어 "덱에서 날아오는" 이동은
            // 안 보이고 스케일만 커지는 것처럼 보인다 - 이 경고가 그 증상의 원인을 짚어준다.
            warnedMissingDrawOrigin = true;

            Debug.LogWarning(
                "[KTH_SpawnCard] drawOrigin이 비어있어 카드가 덱 위치에서 스폰되지 않습니다. " +
                "인스펙터에서 Draw Origin에 덱 오브젝트를 연결해 주세요. " +
                "(이 경고는 한 번만 나옵니다)",
                this
            );
        }

        // 3. 손패 추가 및 정렬 애니메이션 트리거
        // 출발 자리가 손패 컨테이너 기준 왼쪽/오른쪽 중 어디에 있는지에 따라
        // 카드가 채워지는 방향이 자동으로 결정된다.
        // (왼쪽 -> 손패가 왼쪽부터 채워짐, 오른쪽 -> 오른쪽부터 채워짐)
        if (drawOrigin != null)
        {
            handLayout.AddCard(newCard, drawOrigin.position);
        }
        else
        {
            handLayout.AddCard(newCard);
        }

        Debug.Log($"[KTH_SpawnCard] 카드 생성 완료: {cardData.name} | 남은 덱: {deckManager.RemainingCards} | 손패: {handLayout.HandCount}/{handLayout.MaxHandSize}");

        RefreshDrawButtonState();

        return true;
    }

    /// <summary>스테이지 첫 턴의 시작 손패를 한 번에 준다. 턴당 드로우 제한을 무시한다.</summary>
    public void SpawnStartingHand(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (!SpawnOneCard(bypassDrawLimit: true)) break;
        }
    }
}
