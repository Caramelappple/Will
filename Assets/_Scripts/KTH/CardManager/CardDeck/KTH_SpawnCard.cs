using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.UI.Feedback;
using UnityEngine;

/// <summary>
/// 덱에서 카드 한 장을 꺼내 손패에 놓는다.
///
/// ── 손으로 뽑는 길은 없어졌다 ─────────────────────────────
/// 예전에는 덱을 눌러 뽑는 KTH_DrawButton 이 있었고, 턴당 몇 장까지인지를
/// KTH_DeckManager 가 세고 있었다.
///
/// 판마다·턴마다 손패를 통째로 새로 받는 규칙으로 바뀌면서 그 길이 필요
/// 없어졌다. 턴당 제한도 셀 것이 없다 — 받는 장수는 KTH_StartCardSet 이 정한다.
///
/// 뽑는 것을 정하는 곳은 이제 KTH_StartCardSet 하나다.
/// 여기는 "뽑아서 놓는다"만 한다.
/// ─────────────────────────────────────────────────────────
///
/// 덱 오브젝트는 남아 있다. 카드가 **거기서 날아오기** 때문이다(drawOrigin).
/// </summary>
public class KTH_SpawnCard : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KTH_DeckManager deckManager;
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

    /// <summary>
    /// 카드 한 장을 뽑아 손패에 놓는다.
    ///
    /// 뽑을 수 없으면 false. 이유는 부르는 쪽이 아니라 아래쪽에서 이미 알린다
    /// (손패 가득 참은 여기서, 덱이 빈 것은 KTH_DeckManager 가).
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

        if (handLayout.IsFull)
        {
            // 손패가 찬 것도 규칙대로 돌아간 결과다. 콘솔이 아니라 화면으로 알린다.
            LSO_RejectSignal.Raise(LSO_RejectReason.HandFull);
            return false;
        }

        // 왜 못 뽑는지는 KTH_DeckManager 가 이미 거부 신호로 알린다.
        // 여기서 또 알리면 같은 사건이 두 번 나가고, 나중에 문구를 고칠 때
        // 두 곳을 맞춰야 한다.
        LSO_CardSO cardData = deckManager.DrawCard();
        if (cardData == null)
        {
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

        return true;
    }

    /// <summary>손패를 한 번에 count 장까지 채운다.</summary>
    public void SpawnStartingHand(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (!SpawnOneCard()) break;
        }
    }
}
