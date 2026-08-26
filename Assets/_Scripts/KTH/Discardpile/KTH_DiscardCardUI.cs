using _Scripts.LSO.Deck.Data;
using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

using Random = UnityEngine.Random;

public class KTH_DiscardCardUI : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private RectTransform discardCardTransform;
    [SerializeField] private TMP_Text discardCountText;

    [Header("버림 카드 표시")]
    [SerializeField] private KTH_HandCard discardCardPrefab;

    [Header("더미 설정")]
    [Tooltip("카드가 쌓일 때 X축으로 퍼지는 최대 범위")]
    [SerializeField] private float minStackOffset = 3f;

    [Tooltip("카드가 쌓일 때 X축으로 퍼지는 최대 범위")]
    [SerializeField] private float maxStackOffset = 5f;

    [Tooltip("카드 한 장이 추가될 때마다 위로 올라가는 높이")]
    [SerializeField] private float stackHeight = 2.5f;

    [Header("착지 흔들림")]
    [Tooltip("새로 얹힌 카드 아래로 몇 장까지 흔들릴지")]
    [SerializeField] private int shakeCardCount = 2;

    [SerializeField] private float shakeAngle = 4f;
    [SerializeField] private float shakeDuration = 0.15f;
    [SerializeField] private int shakeVibrato = 8;
    [SerializeField] private float shakeElasticity = 0.4f;

    [Header("리셔플 연출")]
    [Tooltip("카드를 뽑았던 위치(덱 UI)")]
    [SerializeField] private RectTransform drawPileTransform;

    [SerializeField] private float reshuffleFlyDuration = 0.35f;

    [Tooltip("카드마다 날아가기 시작하는 시간차")]
    [SerializeField] private float reshuffleStaggerDelay = 0.03f;

    [SerializeField] private Ease reshuffleEase = Ease.InOutQuad;

    [Tooltip("덱으로 돌아갈 때 살짝 떠올랐다가 내려오는 정점 높이")]
    [SerializeField] private float reshuffleArcHeight = 40f;

    private readonly List<LSO_CardSO> _discardCardList =
        new List<LSO_CardSO>();

    private KTH_HandCard _topDiscardCard;

    public RectTransform DiscardCardTransform =>
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

        ShakeTopCards();

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

        ShakeTopCards();

        Debug.Log(
            $"[KTH_DiscardCardUI:{GetInstanceID()}] " +
            $"카드 추가됨(재사용): {cardData.name} | " +
            $"현재 총 {_discardCardList.Count}장"
        );

        OnCardAdded?.Invoke(
            _discardCardList.Count
        );
    }


    // =========================================================
    // 기존 카드 더미 편입
    // =========================================================

    private void PlaceExistingCardInPile(KTH_HandCard card)
{
    if (discardCardTransform == null)
    {
        Debug.LogWarning(
            "[KTH_DiscardCardUI] " +
            "Discard Card Transform이 연결되지 않았습니다.",
            this
        );

        return;
    }

    Transform cardTransform = card.transform;

    // 현재 월드 위치와 회전 저장
    Vector3 startPosition = cardTransform.position;
    Quaternion startRotation = cardTransform.rotation;

    // 더미의 자식으로 변경
    cardTransform.SetParent(
        discardCardTransform,
        true
    );

    RectTransform cardRect =
        cardTransform.GetComponent<RectTransform>();

    if (cardRect == null)
    {
        return;
    }

    // 카드가 쌓일 위치 계산
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

    float randomY =
        Random.Range(
            -minStackOffset,
            minStackOffset
        );

    float offsetY =
        (stackIndex * stackHeight) +
        randomY;

    // 최종 위치
    Vector3 targetPosition =
        discardCardTransform.TransformPoint(
            new Vector3(
                offsetX,
                offsetY,
                0f
            )
        );

    // 최종 랜덤 회전
    float randomAngle =
        Random.Range(
            -15f,
            15f
        );

    if (Mathf.Abs(randomAngle) < 5f)
    {
        randomAngle =
            randomAngle < 0f
                ? -5f
                : 5f;
    }

    Quaternion targetRotation =
        discardCardTransform.rotation *
        Quaternion.Euler(
            0f,
            0f,
            randomAngle
        );

    // 현재 회전 유지
    cardTransform.rotation = startRotation;

    // 기존 DOTween 제거
    cardTransform.DOKill();

    Sequence sequence =
        DOTween.Sequence();

    // 위치 이동
    sequence.Join(
        cardTransform.DOMove(
            targetPosition,
            0.3f
        )
        .SetEase(Ease.OutQuad)
    );

    // 이동하면서 회전
    sequence.Join(
        cardTransform.DORotate(
            targetRotation.eulerAngles,
            0.3f,
            RotateMode.FastBeyond360
        )
        .SetEase(Ease.OutQuad)
    );

    // 도착 후 처리
    sequence.OnComplete(() =>
    {
        if (cardTransform == null)
        {
            return;
        }

        // 정확하게 최종 위치/회전 고정
        cardTransform.position =
            targetPosition;

        cardTransform.rotation =
            targetRotation;

        // 항상 가장 위
        cardTransform.SetAsLastSibling();

        // 손패 인터랙션 비활성화
        card.enabled = false;

        KTH_CardSorting cardSorting =
            card.GetComponent<KTH_CardSorting>();

        if (cardSorting != null)
        {
            cardSorting.enabled = false;
        }

        CanvasGroup canvasGroup =
            card.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup =
                card.gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        _topDiscardCard = card;

        // 착지 흔들림
        ShakeTopCards();
    });
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

        RectTransform cardRect =
            newCard.GetComponent<RectTransform>();

        if (cardRect != null)
        {
            ApplyStackPosition(cardRect);
        }

        // 새 카드가 항상 가장 위
        newCard.transform.SetAsLastSibling();

        // 버림 더미에서는 클릭 불가능
        newCard.enabled = false;

        CanvasGroup canvasGroup =
            newCard.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup =
                newCard.gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        _topDiscardCard = newCard;
    }


    // =========================================================
    // 카드 쌓임 위치
    // =========================================================

    private void ApplyStackPosition(
        RectTransform cardRect)
    {
        if (cardRect == null)
        {
            return;
        }

        // 현재 카드의 순번
        //
        // _discardCardList에는 이미 카드가 추가되어 있으므로
        //
        // 1장 = 0
        // 2장 = 1
        // 3장 = 2
        // ...
        int stackIndex =
            Mathf.Max(
                0,
                _discardCardList.Count - 1
            );

        // X축 랜덤
        float offsetX =
            Random.Range(
                -maxStackOffset,
                maxStackOffset
            );

        // Y축 랜덤
        // 너무 크게 흔들리지 않도록 minStackOffset 사용
        float randomY =
            Random.Range(
                -minStackOffset,
                minStackOffset
            );

        // 카드가 쌓일수록 위로 올라감
        float offsetY =
            (stackIndex * stackHeight) +
            randomY;

        cardRect.anchoredPosition =
            new Vector2(
                offsetX,
                offsetY
            );

        // 카드마다 랜덤 회전
        float randomAngle =
            Random.Range(
                -15f,
                15f
            );

        // 너무 작은 각도 방지
        if (Mathf.Abs(randomAngle) < 5f)
        {
            randomAngle =
                randomAngle < 0f
                    ? -5f
                    : 5f;
        }

        cardRect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                randomAngle
            );
    }


    // =========================================================
    // 착지 흔들림
    // =========================================================

    private void ShakeTopCards()
    {
        if (discardCardTransform == null)
        {
            return;
        }

        int childCount =
            discardCardTransform.childCount;

        if (childCount <= 1)
        {
            return;
        }

        // 마지막 자식은 방금 추가된 카드
        // 그 아래 카드들만 흔든다.
        int fromIndex =
            Mathf.Max(
                0,
                childCount - 1 - shakeCardCount
            );

        int toIndex =
            childCount - 2;

        for (
            int i = fromIndex;
            i <= toIndex;
            i++
        )
        {
            if (
                i < 0 ||
                i >= discardCardTransform.childCount
            )
            {
                continue;
            }

            Transform child =
                discardCardTransform.GetChild(i);

            if (child == null)
            {
                continue;
            }

            child.DOKill();

            float punchAngle =
                shakeAngle *
                (
                    Random.value < 0.5f
                        ? -1f
                        : 1f
                );

            child.DOPunchRotation(
                new Vector3(
                    0f,
                    0f,
                    punchAngle
                ),
                shakeDuration,
                shakeVibrato,
                shakeElasticity
            );
        }
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

    /// <summary>
    /// 버림 더미의 카드 데이터를 가져오고
    /// UI 카드들은 덱 위치로 날려보낸다.
    /// </summary>
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

        // 목적지가 없으면 즉시 삭제
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

        // 애니메이션 중 자식 목록이 변경되므로
        // 미리 복사
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

        // 카드 순차적으로 날리기
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

            // DOTween Sequence는 지연 실행되므로, 실제 시작 위치는
            // 콜백 안에서 그 시점의 실제 위치를 다시 읽어와야 정확하다.
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

                // 부모(더미)의 스케일을 고려해 정점 높이를 월드 단위로 변환
                midWorldPos +=
                    discardCardTransform.TransformVector(
                        new Vector3(0f, reshuffleArcHeight, 0f)
                    );

                Sequence flightSequence =
                    DOTween.Sequence();

                // 위로 살짝 떠올랐다가 덱 위치로 내려오는 포물선 이동
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

                // 회전 원상복구
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