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
    [Tooltip("카드가 쌓일 때 좌우로 퍼지는 최대 범위 (월드 스페이스 유닛)")]
    [SerializeField] private float maxStackOffset = 5f;

    [Tooltip("카드가 쌓일 때 위아래로 랜덤하게 움직이는 범위 (월드 스페이스 유닛)")]
    [SerializeField] private float verticalStackOffset = 3f;

    [Tooltip("카드 한 장이 추가될 때마다 위로 올라가는 높이 (월드 스페이스 유닛)")]
    [SerializeField] private float stackHeight = 2.5f;

    [Header("카드 랜덤 기울기")]
    [Tooltip("카드가 쌓일 때 랜덤으로 기울어지는 최대 각도")]
    [SerializeField] private float maxRandomAngle = 15f;

    [Tooltip("이 각도보다 작으면 최소 기울기를 적용")]
    [SerializeField] private float minRandomAngle = 5f;

    [Header("버림 이동 연출")]
    [SerializeField] private float discardMoveDuration = 0.3f;

    [SerializeField] private Ease discardMoveEase = Ease.OutQuad;

    [SerializeField] private Ease discardRotateEase = Ease.OutQuad;

    [Header("리셔플 연출")]
    [Tooltip("카드를 뽑았던 위치(덱 오브젝트)")]
    [SerializeField] private Transform drawPileTransform;

    [SerializeField] private float reshuffleFlyDuration = 0.35f;

    [Tooltip("카드마다 날아가기 시작하는 시간차")]
    [SerializeField] private float reshuffleStaggerDelay = 0.03f;

    [SerializeField] private Ease reshuffleEase = Ease.InOutQuad;

    [Tooltip("덱으로 돌아갈 때 살짝 떠올랐다가 내려오는 정점 높이")]
    [SerializeField] private float reshuffleArcHeight = 40f;

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

        // ==================================================
        // 현재 월드 위치 / 회전 저장
        //
        // 중요:
        // KTH_DiscardAnimation에서 이미
        // Y = 180도 회전이 끝난 상태다.
        //
        // 따라서 여기서는 현재 회전을 그대로 유지한다.
        // ==================================================

        Quaternion currentWorldRotation =
            cardTransform.rotation;

        // ==================================================
        // 더미의 자식으로 변경
        //
        // 현재 월드 위치/회전 유지
        // ==================================================

        cardTransform.SetParent(
            discardCardTransform,
            true
        );

        // ==================================================
        // 카드가 쌓일 위치 계산
        // ==================================================

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
                -verticalStackOffset,
                verticalStackOffset
            );

        float offsetY =
            (stackIndex * stackHeight) +
            randomY;

        Vector3 targetPosition =
            discardCardTransform.TransformPoint(
                new Vector3(
                    offsetX,
                    offsetY,
                    0f
                )
            );

        // ==================================================
        // 기존 Tween 제거
        // ==================================================

        cardTransform.DOKill();

        // ==================================================
        // 중요
        //
        // 회전 애니메이션을 하지 않는다.
        //
        // KTH_DiscardAnimation에서 이미 회전이 끝났기
        // 때문에 여기서 다시 회전시키면 두 번 회전한다.
        // ==================================================

        Sequence sequence =
            DOTween.Sequence();

        sequence.SetTarget(
            cardTransform
        );

        // ==================================================
        // 위치만 이동
        // ==================================================

        sequence.Append(
            cardTransform
                .DOMove(
                    targetPosition,
                    discardMoveDuration
                )
                .SetEase(
                    discardMoveEase
                )
        );

        // ==================================================
        // 완료
        // ==================================================

        sequence.OnComplete(() =>
        {
            if (cardTransform == null)
            {
                return;
            }

            // ==================================================
            // 최종 위치만 확정
            // ==================================================

            cardTransform.position =
                targetPosition;

            // ==================================================
            // 회전은 절대 다시 설정하지 않는다.
            //
            // KTH_DiscardAnimation에서 만들어진
            // 현재 회전 상태를 그대로 유지한다.
            // ==================================================

            cardTransform.rotation =
                currentWorldRotation;

            // ==================================================
            // 손패 인터랙션 비활성화
            // ==================================================

            card.enabled = false;

            KTH_CardSorting cardSorting =
                card.GetComponent<KTH_CardSorting>();

            if (cardSorting != null)
            {
                // 가장 위로 (구 SetAsLastSibling 대체)
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
        });
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

        float randomY =
            Random.Range(
                -verticalStackOffset,
                verticalStackOffset
            );

        float offsetY =
            (stackIndex * stackHeight) +
            randomY;

        cardTransform.localPosition =
            new Vector3(
                offsetX,
                offsetY,
                0f
            );

        float randomAngle =
            GetRandomAngle();

        cardTransform.localRotation =
            Quaternion.Euler(
                0f,
                180f,
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
                    discardCardTransform.TransformVector(
                        new Vector3(
                            0f,
                            reshuffleArcHeight,
                            0f
                        )
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
