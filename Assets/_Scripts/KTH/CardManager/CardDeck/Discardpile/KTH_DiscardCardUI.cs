using _Scripts.LSO.Deck.Data;
using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

using Random = UnityEngine.Random;

// 3D 전환 메모:
// - RectTransform -> Transform, TMP_Text(UGUI) -> TextMeshPro(3D)로 교체.
// - anchoredPosition -> localPosition. (Transform.DOMove / childCount / GetChild는
//   원래도 RectTransform 전용이 아니라 Transform 공통 API라 손 안 댔음)
// - CanvasGroup(blocksRaycasts/interactable) -> Collider.enabled.
// - transform.SetAsLastSibling() -> KTH_CardSorting.BringToFront() (sortingOrder).
public class KTH_DiscardCardUI : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private Transform discardCardTransform;
    [SerializeField] private TextMeshPro discardCountText;

    [Header("버림 카드 표시")]
    [SerializeField] private KTH_HandCard discardCardPrefab;

    [Header("더미 설정")]
    [Tooltip("카드가 눕혀져 쌓일 때 테이블 면 위에서 좌우로 퍼지는 최대 범위 (월드 스페이스 유닛)")]
    [SerializeField] private float maxStackOffset = 0.05f;

    [Tooltip("카드가 눕혀져 쌓일 때 테이블 면 위에서 앞뒤(깊이)로 퍼지는 범위 (월드 스페이스 유닛). " +
             "높이(Stack Height)와는 다른 축이라 쌓인 순서에는 영향 없음.")]
    [SerializeField] private float depthStackOffset = 0.03f;

    [Tooltip("카드 한 장이 추가될 때마다 더미가 두꺼워지는 높이 (월드 스페이스 유닛, 카드 두께 느낌). " +
             "쌓인 순서를 결정하는 값이라 랜덤을 섞지 않는다.")]
    [SerializeField] private float stackHeight = 0.015f;

    [Header("카드 눕히기 / 뒤집기")]
    [Tooltip("더미에서 카드가 테이블에 눕는 각도(X축). 이 값 하나로 눕는 것과 어느 면이 보이는지(뒤집기)가 " +
             "동시에 결정된다. 90과 -90이 서로 반대 면이 보이는 상태이니, 둘 중 원하는 면이 보이는 쪽으로 조정.")]
    [SerializeField] private float lieFlatXAngle = -90f;

    [Header("카드 랜덤 기울기")]
    [Tooltip("카드가 쌓일 때 랜덤으로 기울어지는 최대 각도")]
    [SerializeField] private float maxRandomAngle = 15f;

    [Tooltip("이 각도보다 작으면 최소 기울기를 적용")]
    [SerializeField] private float minRandomAngle = 5f;

    [Header("리셔플 연출")]
    [Tooltip("카드를 뽑았던 위치(덱 오브젝트)")]
    [SerializeField] private Transform drawPileTransform;

    [SerializeField] private float reshuffleFlyDuration = 0.35f;

    [Tooltip("카드마다 날아가기 시작하는 시간차")]
    [SerializeField] private float reshuffleStaggerDelay = 0.03f;

    [SerializeField] private Ease reshuffleEase = Ease.InOutQuad;

    [Tooltip("덱으로 돌아갈 때 살짝 떠올랐다가 내려오는 정점 높이")]
    [SerializeField] private float reshuffleArcHeight = 0.4f;

    private readonly List<LSO_CardSO> _discardCardList =
        new List<LSO_CardSO>();

    private KTH_HandCard _topDiscardCard;

    public Transform DiscardCardTransform =>
        discardCardTransform;

    public int Count =>
        _discardCardList.Count;

    public event Action<int> OnCardAdded;


    // =========================================================
    // Unity
    // =========================================================

    private void Awake()
    {
        UpdateUI();
    }


    // =========================================================
    // 카드 추가
    // =========================================================

    public void AddToDiscardPile(LSO_CardSO cardData)
    {
        if (cardData == null)
        {
            Debug.LogWarning(
                "[KTH_DiscardCardUI] cardData가 null입니다.",
                this
            );

            return;
        }

        _discardCardList.Add(cardData);

        UpdateUI();

        CreateDiscardCard(cardData);

        Debug.Log(
            $"[KTH_DiscardCardUI:{GetInstanceID()}] " +
            $"카드 추가됨: {cardData.name} | " +
            $"현재 총 {_discardCardList.Count}장"
        );

        OnCardAdded?.Invoke(
            _discardCardList.Count
        );
    }


    // =========================================================
    // 기존 손패 카드 추가
    // =========================================================

    public void AddExistingCardToDiscardPile(
        KTH_HandCard card,
        LSO_CardSO cardData)
    {
        if (card == null)
        {
            Debug.LogWarning(
                "[KTH_DiscardCardUI] card가 null입니다.",
                this
            );

            return;
        }

        if (cardData == null)
        {
            Debug.LogWarning(
                "[KTH_DiscardCardUI] cardData가 null입니다.",
                this
            );

            return;
        }

        _discardCardList.Add(cardData);

        UpdateUI();

        PlaceExistingCardInPile(card);

        Debug.Log(
            $"[KTH_DiscardCardUI:{GetInstanceID()}] " +
            $"카드 추가됨(재사용): {cardData.name} | " +
            $"현재 총 {_discardCardList.Count}장"
        );

        OnCardAdded?.Invoke(
            _discardCardList.Count
        );
    }


    /// <summary>
    /// 다음 카드가 더미에 들어갈 때 도착해야 할 최종 월드 위치/회전을 미리 계산해서 알려준다.
    ///
    /// KTH_DiscardAnimation이 던지는 애니메이션을 시작하기 전에 이걸 불러서,
    /// 처음부터 "진짜 쌓임 위치(더미 높이 반영)"로 날아가게 한다.
    /// 예전엔 대충 아무 데나 던진 다음 도착 후에 다시 진짜 위치로 옮겼는데,
    /// 그러면 착지하자마자 한 번 더 미끄러지듯 보정되는 게 보여서 이 방식으로 통일했다.
    ///
    /// 중요: 이 카드가 실제로 리스트에 추가되기 전(AddToDiscardPile /
    /// AddExistingCardToDiscardPile 호출 전)에 불러야 스택 높이 계산이 맞는다.
    /// </summary>
    public void GetNextStackTarget(
        out Vector3 worldPosition,
        out Quaternion rotation)
    {
        int stackIndex =
            _discardCardList.Count;

        float offsetX =
            Random.Range(
                -maxStackOffset,
                maxStackOffset
            );

        // 높이(Y)는 쌓인 순서를 결정하는 값이라 랜덤을 섞지 않는다.
        // 지저분해 보이는 효과는 테이블 면 위의 앞뒤(Z)로 대신 준다.
        float offsetY =
            stackIndex * stackHeight;

        float offsetZ =
            Random.Range(
                -depthStackOffset,
                depthStackOffset
            );

        worldPosition =
            discardCardTransform.position +
            discardCardTransform.rotation *
            new Vector3(
                offsetX,
                offsetY,
                offsetZ
            );

        float randomAngle =
            GetRandomAngle();

        rotation =
            Quaternion.Euler(
                lieFlatXAngle,
                0f,
                randomAngle
            );
    }


    // =========================================================
    // 기존 카드 더미 편입
    // =========================================================

    private void PlaceExistingCardInPile(
        KTH_HandCard card)
    {
        if (card == null)
        {
            return;
        }

        if (discardCardTransform == null)
        {
            Debug.LogWarning(
                "[KTH_DiscardCardUI] " +
                "Discard Card Transform이 연결되지 않았습니다.",
                this
            );

            return;
        }

        Transform cardTransform =
            card.transform;

        // KTH_DiscardAnimation이 GetNextStackTarget()으로 미리 받은
        // 최종 위치/회전까지 이미 다 던져놓은 상태다. 여기서는 그 상태를
        // 유지한 채 부모만 갈아끼운다 (SetParent(worldPositionStays: true)).

        cardTransform.SetParent(
            discardCardTransform,
            true
        );

        // ==================================================
        // 손패 인터랙션 비활성화
        // ==================================================

        card.enabled = false;

        KTH_CardSorting cardSorting =
            card.GetComponent<KTH_CardSorting>();

        if (cardSorting != null)
        {
            cardSorting.BringToFront();

            cardSorting.enabled = false;
        }

        Collider cardCollider =
            card.GetComponent<Collider>();

        if (cardCollider != null)
        {
            cardCollider.enabled = false;
        }

        // ==================================================
        // 현재 최상단 카드 저장
        // ==================================================

        _topDiscardCard = card;
    }


    // =========================================================
    // 랜덤 각도
    // =========================================================

    private float GetRandomAngle()
    {
        if (maxRandomAngle <= 0f)
        {
            return 0f;
        }

        float clampedMinAngle =
            Mathf.Clamp(
                minRandomAngle,
                0f,
                maxRandomAngle
            );

        float randomAngle =
            Random.Range(
                -maxRandomAngle,
                maxRandomAngle
            );

        if (
            clampedMinAngle > 0f &&
            Mathf.Abs(randomAngle) < clampedMinAngle
        )
        {
            randomAngle =
                Random.value < 0.5f
                    ? -clampedMinAngle
                    : clampedMinAngle;
        }

        return randomAngle;
    }


    // =========================================================
    // 새 카드 생성
    // =========================================================

    private void CreateDiscardCard(
        LSO_CardSO cardData)
    {
        if (discardCardPrefab == null)
        {
            Debug.LogWarning(
                "[KTH_DiscardCardUI] " +
                "Discard Card Prefab이 연결되지 않았습니다.",
                this
            );

            return;
        }

        if (discardCardTransform == null)
        {
            Debug.LogWarning(
                "[KTH_DiscardCardUI] " +
                "Discard Card Transform이 연결되지 않았습니다.",
                this
            );

            return;
        }

        KTH_HandCard newCard =
            KTH_HandCardPool.Instance != null
                ? KTH_HandCardPool.Instance.Get(
                    discardCardTransform
                )
                : Instantiate(
                    discardCardPrefab,
                    discardCardTransform
                );

        ApplyStackPosition(newCard.transform);

        newCard.enabled = false;

        KTH_CardSorting cardSorting =
            newCard.GetComponent<KTH_CardSorting>();

        if (cardSorting != null)
        {
            // 가장 위로 (구 SetAsLastSibling 대체)
            cardSorting.BringToFront();
        }

        Collider cardCollider =
            newCard.GetComponent<Collider>();

        if (cardCollider != null)
        {
            cardCollider.enabled = false;
        }

        _topDiscardCard = newCard;
    }


    // =========================================================
    // 카드 쌓임 위치
    // =========================================================

    private void ApplyStackPosition(
        Transform cardTransform)
    {
        if (cardTransform == null)
        {
            return;
        }

        int stackIndex =
            Mathf.Max(
                0,
                _discardCardList.Count - 1
            );

        float offsetX =
            Random.Range(
                -maxStackOffset,
                maxStackOffset
            );

        // 높이(Y)는 쌓인 순서를 결정하는 값이라 랜덤을 섞지 않는다.
        // 지저분해 보이는 효과는 테이블 면 위의 앞뒤(Z)로 대신 준다.
        float offsetY =
            stackIndex * stackHeight;

        float offsetZ =
            Random.Range(
                -depthStackOffset,
                depthStackOffset
            );

        // localPosition을 직접 쓰면 부모(discardCardTransform)의 작은 Scale이
        // 그대로 곱해져서 간격이 거의 사라진다. 월드 포지션으로 직접 지정해서
        // 부모 스케일과 무관하게 의도한 간격이 나오게 한다.
        cardTransform.position =
            discardCardTransform.position +
            discardCardTransform.rotation *
            new Vector3(
                offsetX,
                offsetY,
                offsetZ
            );

        float randomAngle =
            GetRandomAngle();

        cardTransform.localRotation =
            Quaternion.Euler(
                lieFlatXAngle,
                0f,
                randomAngle
            );
    }


    // =========================================================
    // 풀 반납 / 삭제
    // =========================================================

    private void ReleaseOrDestroyCard(
        Transform cardTransform)
    {
        if (cardTransform == null)
        {
            return;
        }

        KTH_HandCard card =
            cardTransform.GetComponent<KTH_HandCard>();

        if (
            card != null &&
            KTH_HandCardPool.Instance != null
        )
        {
            KTH_HandCardPool.Instance.Release(
                card
            );
        }
        else
        {
            Destroy(
                cardTransform.gameObject
            );
        }
    }


    // =========================================================
    // UI
    // =========================================================

    public void UpdateUI()
    {
        if (discardCountText != null)
        {
            discardCountText.text =
                _discardCardList.Count.ToString();
        }
    }


    // =========================================================
    // 리셔플
    // =========================================================

    public List<LSO_CardSO> ClearAndGetList()
    {
        List<LSO_CardSO> currentPile =
            new List<LSO_CardSO>(
                _discardCardList
            );

        _discardCardList.Clear();

        _topDiscardCard = null;

        UpdateUI();

        PlayReshuffleFlyAwayAnimation();

        return currentPile;
    }


    // =========================================================
    // 리셔플 카드 날아가기
    // =========================================================

    private void PlayReshuffleFlyAwayAnimation()
    {
        if (discardCardTransform == null)
        {
            return;
        }

        int childCount =
            discardCardTransform.childCount;

        if (childCount == 0)
        {
            return;
        }

        if (drawPileTransform == null)
        {
            for (
                int i = childCount - 1;
                i >= 0;
                i--
            )
            {
                ReleaseOrDestroyCard(
                    discardCardTransform.GetChild(i)
                );
            }

            return;
        }

        Vector3 targetWorldPos =
            drawPileTransform.position;

        List<Transform> cards =
            new List<Transform>(
                childCount
            );

        for (
            int i = 0;
            i < childCount;
            i++
        )
        {
            Transform child =
                discardCardTransform.GetChild(i);

            if (child != null)
            {
                cards.Add(child);
            }
        }

        for (
            int i = 0;
            i < cards.Count;
            i++
        )
        {
            Transform cardTransform =
                cards[i];

            if (cardTransform == null)
            {
                continue;
            }

            cardTransform.DOKill();

            float startDelay =
                i * reshuffleStaggerDelay;

            Transform capturedTransform =
                cardTransform;

            Sequence sequence =
                DOTween.Sequence();

            sequence.AppendInterval(
                startDelay
            );

            sequence.AppendCallback(() =>
            {
                if (capturedTransform == null)
                {
                    return;
                }

                Vector3 startWorldPos =
                    capturedTransform.position;

                Vector3 midWorldPos =
                    Vector3.Lerp(
                        startWorldPos,
                        targetWorldPos,
                        0.5f
                    );

                midWorldPos +=
                    discardCardTransform.rotation *
                    new Vector3(
                        0f,
                        reshuffleArcHeight,
                        0f
                    );

                Sequence flightSequence =
                    DOTween.Sequence();

                flightSequence.Append(
                    capturedTransform.DOPath(
                        new[]
                        {
                            startWorldPos,
                            midWorldPos,
                            targetWorldPos
                        },
                        reshuffleFlyDuration,
                        PathType.CatmullRom
                    )
                    .SetEase(reshuffleEase)
                );

                flightSequence.Join(
                    capturedTransform.DOLocalRotate(
                        Vector3.zero,
                        reshuffleFlyDuration
                    )
                    .SetEase(Ease.OutQuad)
                );

                flightSequence.OnComplete(() =>
                {
                    if (capturedTransform != null)
                    {
                        ReleaseOrDestroyCard(
                            capturedTransform
                        );
                    }
                });
            });
        }
    }
}
