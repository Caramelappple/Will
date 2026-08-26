using _Scripts.LSO.Deck.Data;
using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Random = UnityEngine.Random;

public class KTH_DiscardCardUI : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private RectTransform discardCardTransform;

    [Header("버림 카드 표시")]
    [SerializeField] private KTH_HandCard discardCardPrefab;

    [Header("더미 설정")]
    [SerializeField] private float minStackOffset = 3f;
    [SerializeField] private float maxStackOffset = 5f;

    [Header("착지 흔들림 (기획서: 착지 시 맨 위 카드 몇 장이 짧게 흔들린 뒤 멈춘다)")]
    [Tooltip("새로 얹힌 카드 아래로 몇 장까지 흔들릴지")]
    [SerializeField] private int shakeCardCount = 2;
    [SerializeField] private float shakeAngle = 4f;
    [SerializeField] private float shakeDuration = 0.15f;
    [SerializeField] private int shakeVibrato = 8;
    [SerializeField] private float shakeElasticity = 0.4f;

    [Header("리셔플 연출")]
    [Tooltip("카드를 뽑았던 위치(덱 파일 UI). 리셔플 시 버린 카드 더미의 카드들이 이 위치로 날아간 뒤 사라진다.")]
    [SerializeField] private RectTransform drawPileTransform;
    [SerializeField] private float reshuffleFlyDuration = 0.35f;
    [Tooltip("카드마다 날아가기 시작하는 시간차 (뭉치가 순차적으로 빨려들어가는 느낌)")]
    [SerializeField] private float reshuffleStaggerDelay = 0.03f;
    [SerializeField] private Ease reshuffleEase = Ease.InQuad;

    private readonly List<LSO_CardSO> _discardCardList =
        new List<LSO_CardSO>();

    private KTH_HandCard _topDiscardCard;

    public RectTransform DiscardCardTransform =>
        discardCardTransform;

    public int Count =>
        _discardCardList.Count;

    public event Action<int> OnCardAdded;
    
    // =========================================================
    // 카드 추가 (프리팹으로 새로 생성 - 애니메이션 없이 즉시 추가할 때 사용)
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
        

        // 버림 더미에 카드 추가
        CreateDiscardCard(cardData);

        // 착지 순간 아래에 깔린 카드 몇 장이 짧게 흔들리는 연출
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
    // 카드 추가 (기존 손패 카드 오브젝트를 그대로 재사용)
    // 디스카드 애니메이션 종료 후 호출됨.
    // 카드를 Destroy하지 않고 더미의 자식으로 편입시킨 뒤
    // 인터랙션 관련 스크립트/설정만 비활성화한다.
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
        
        PlaceExistingCardInPile(card);

        // 착지 순간 아래에 깔린 카드 몇 장이 짧게 흔들리는 연출
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

        // 더미 자식으로 편입 (현재 화면상 위치/회전/스케일 유지)
        cardTransform.SetParent(
            discardCardTransform,
            true
        );

        // 새로 편입된 카드가 항상 가장 위에 보이도록
        cardTransform.SetAsLastSibling();

        // --------------------------------------------------
        // 인터랙션 관련 스크립트/컴포넌트 비활성화
        // (더 이상 손패 카드로서 클릭/호버 동작을 하지 않도록)
        // --------------------------------------------------

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
    }

    // =========================================================
    // 버림 카드 표시 (새 프리팹 생성)
    // =========================================================

    private void CreateDiscardCard(LSO_CardSO cardData)
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

        // 새 카드 생성
        KTH_HandCard newCard =
            Instantiate(
                discardCardPrefab,
                discardCardTransform
            );

        RectTransform cardRect =
            newCard.GetComponent<RectTransform>();

        if (cardRect != null)
        {
            // 기본 위치
            cardRect.anchoredPosition =
                Vector2.zero;

            // 카드마다 조금씩 다른 위치
            float offsetX =
                Random.Range(
                    minStackOffset,
                    maxStackOffset
                );

            float offsetY =
                Random.Range(
                    minStackOffset,
                    maxStackOffset
                );

            if (Random.value < 0.5f)
                offsetX *= -1f;

            if (Random.value < 0.5f)
                offsetY *= -1f;

            cardRect.anchoredPosition =
                new Vector2(
                    offsetX,
                    offsetY
                );

            // 카드마다 랜덤 각도
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

        // 마지막 카드 기억
        _topDiscardCard = newCard;
    }

    // =========================================================
    // 착지 흔들림
    // (기획서: "착지 순간 맨 위의 카드 몇 장이 짧게 흔들린 뒤 멈춘다")
    // =========================================================

    private void ShakeTopCards()
    {
        if (discardCardTransform == null)
        {
            return;
        }

        int childCount =
            discardCardTransform.childCount;

        // 마지막 자식(childCount - 1)은 방금 새로 얹힌 카드이므로 제외.
        // 그 아래 shakeCardCount장만 흔든다.
        int fromIndex =
            Mathf.Max(
                0,
                childCount - 1 - shakeCardCount
            );

        int toIndex =
            childCount - 2; // 새 카드 바로 아래 카드까지

        for (int i = fromIndex; i <= toIndex; i++)
        {
            if (i < 0 || i >= discardCardTransform.childCount)
            {
                continue;
            }

            Transform child =
                discardCardTransform.GetChild(i);

            child.DOKill();

            float punchAngle =
                shakeAngle *
                (Random.value < 0.5f ? -1f : 1f);

            // DOPunchRotation은 흔들린 뒤 원래 회전값으로 자동 복귀한다.
            child
                .DOPunchRotation(
                    new Vector3(0f, 0f, punchAngle),
                    shakeDuration,
                    shakeVibrato,
                    shakeElasticity
                );
        }
    }

    // =========================================================
    // UI
    // =========================================================
    
    // =========================================================
    // 리셔플
    // =========================================================

    /// <summary>
    /// 데이터(카드 목록)는 즉시 비워서 반환하되,
    /// 시각적으로 쌓여있던 카드들은 즉시 사라지지 않고
    /// "드로우했던 위치(drawPileTransform)"로 날아간 뒤 사라진다.
    /// </summary>
    public List<LSO_CardSO> ClearAndGetList()
    {
        List<LSO_CardSO> currentPile =
            new List<LSO_CardSO>(
                _discardCardList
            );

        _discardCardList.Clear();

        _topDiscardCard = null;
        
        PlayReshuffleFlyAwayAnimation();

        return currentPile;
    }

    /// <summary>
    /// 버림 더미에 쌓여있던 카드 오브젝트들을 drawPileTransform 위치로
    /// 순차적으로(약간의 시간차를 두고) 날려보낸 뒤 각각 파괴한다.
    /// drawPileTransform이 연결되어 있지 않으면 기존처럼 즉시 파괴한다.
    /// </summary>
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

        // 목표 위치가 연결되어 있지 않으면 즉시 파괴 (기존 동작 유지)
        if (drawPileTransform == null)
        {
            for (
                int i = childCount - 1;
                i >= 0;
                i--
            )
            {
                Destroy(
                    discardCardTransform.GetChild(i).gameObject
                );
            }

            return;
        }

        Vector3 targetWorldPos =
            drawPileTransform.position;

        // 파괴/애니메이션 도중 자식 목록이 바뀌므로 미리 배열로 복사
        List<Transform> cards =
            new List<Transform>(childCount);

        for (int i = 0; i < childCount; i++)
        {
            cards.Add(
                discardCardTransform.GetChild(i)
            );
        }

        for (int i = 0; i < cards.Count; i++)
        {
            Transform cardTransform = cards[i];

            if (cardTransform == null)
            {
                continue;
            }

            cardTransform.DOKill();

            float startDelay =
                i * reshuffleStaggerDelay;

            Sequence sequence =
                DOTween.Sequence();

            sequence.AppendInterval(startDelay);

            // 드로우 위치로 날아가면서 점점 작아짐
            // (드로우 시 작은 스케일에서 커지는 연출의 역순 느낌)
            sequence.Append(
                cardTransform
                    .DOMove(
                        targetWorldPos,
                        reshuffleFlyDuration
                    )
                    .SetEase(reshuffleEase)
            );

            sequence.Join(
                cardTransform
                    .DOLocalRotate(
                        Vector3.zero,
                        reshuffleFlyDuration
                    )
                    .SetEase(Ease.OutQuad)
            );

            sequence.OnComplete(() =>
            {
                if (cardTransform != null)
                {
                    Destroy(
                        cardTransform.gameObject
                    );
                }
            });
        }
    }
}