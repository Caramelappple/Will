using _Scripts.LSO.Deck.Data;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class KTH_DiscardAnimation : MonoBehaviour
{
    [Header("Discard Animation")]
    // 기획서: "사용된 카드는 손패에서 빠져나와 약 0.15~0.2초 동안
    // 버린 카드 더미로 이동한다."
    [SerializeField] private float discardDuration = 0.18f;

    [Header("Arc (포물선)")]
    [Tooltip("이동 중 카드가 그리는 포물선의 높이(정점)")]
    [SerializeField] private float arcHeight = 60f;

    [Header("Random Rotation")]
    [SerializeField] private float minRandomTilt = 5f;
    [SerializeField] private float maxRandomTilt = 15f;

    [Header("Random Stack Offset")]
    [SerializeField] private float minStackOffset = 3f;
    [SerializeField] private float maxStackOffset = 5f;

    [Header("Landing")]
    [SerializeField] private float landingDuration = 0.08f;
    [SerializeField] private float landingScale = 0.96f;

    [Header("Shadow")]
    [SerializeField] private float shadowBoost = 1.8f;
    [SerializeField] private float shadowDuration = 0.12f;

    public void Play(
        KTH_HandCard card,
        KTH_DiscardCardUI discardPile,
        LSO_CardSO cardData)
    {
        if (card == null)
        {
            return;
        }

        if (discardPile == null ||
            discardPile.DiscardCardTransform == null)
        {
            Destroy(card.gameObject);
            return;
        }

        Transform cardTransform = card.transform;

        // --------------------------------------------------
        // CanvasGroup
        // --------------------------------------------------

        CanvasGroup canvasGroup =
            card.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup =
                card.gameObject.AddComponent<CanvasGroup>();
        }

        // 버린 카드가 이동하는 동안 다른 UI 클릭을 막지 않도록 처리
        canvasGroup.blocksRaycasts = false;

        // --------------------------------------------------
        // 부모 변경 (정렬 순서 목적 - 월드 포지션은 그대로 유지)
        // --------------------------------------------------

        Transform discardParent =
            discardPile.DiscardCardTransform.parent;

        cardTransform.DOKill();

        cardTransform.SetParent(
            discardParent,
            true // 화면상 위치/크기는 그대로 유지됨
        );

        // --------------------------------------------------
        // 목표 위치 (월드 스페이스 기준으로 계산 -> 부모 간 스케일 차이로 인한
        // 이동 거리 왜곡(카드가 너무 크게 튕기는 문제) 방지)
        // --------------------------------------------------

        // 기획서:
        // "카드 여러 장이 서로 다른 위치 오프셋(±3~5px)으로 겹쳐 쌓인 더미"
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

        // 방향도 랜덤
        offsetX *= Random.value < 0.5f ? -1f : 1f;
        offsetY *= Random.value < 0.5f ? -1f : 1f;

        // discardParent의 로컬 스케일 기준 오프셋 -> 월드 오프셋으로 변환
        Vector3 localOffset = new Vector3(offsetX, offsetY, 0f);

        Vector3 targetWorldPos =
            discardPile.DiscardCardTransform.position +
            discardParent.TransformVector(localOffset);

        // --------------------------------------------------
        // 포물선 경로 (기획서: "버린 카드에서 카드 뭉치까지
        // 포물선을 그리며 더미 위에 거칠게 떨어진다")
        // --------------------------------------------------

        Vector3 startWorldPos =
            cardTransform.position;

        // 스케일 차이를 고려해 아치 높이도 discardParent 기준으로 변환
        Vector3 arcOffsetWorld =
            discardParent.TransformVector(
                new Vector3(0f, arcHeight, 0f)
            );

        Vector3 midWorldPos =
            Vector3.Lerp(
                startWorldPos,
                targetWorldPos,
                0.5f
            ) + arcOffsetWorld;

        // --------------------------------------------------
        // 랜덤 회전
        // --------------------------------------------------

        float randomTilt =
            Random.Range(
                minRandomTilt,
                maxRandomTilt
            );

        // ±5~15도
        if (Random.value < 0.5f)
        {
            randomTilt *= -1f;
        }

        // --------------------------------------------------
        // 기존 Scale 저장
        // --------------------------------------------------

        Vector3 originalScale =
            cardTransform.localScale;

        // --------------------------------------------------
        // Shadow
        // --------------------------------------------------

        Shadow shadow =
            card.GetComponentInChildren<Shadow>();

        Color originalShadowColor = Color.clear;
        Vector2 originalShadowDistance = Vector2.zero;
        bool hasShadow = shadow != null;

        if (hasShadow)
        {
            originalShadowColor =
                shadow.effectColor;

            originalShadowDistance =
                shadow.effectDistance;

            // 그림자를 강하게 만드는 시점은 "착지 순간"이어야 하므로
            // 여기서는 원본 값만 저장해두고, 실제 boost는
            // 착지 애니메이션 직전(AppendCallback)에 적용한다.
        }

        // --------------------------------------------------
        // Animation
        // --------------------------------------------------

        Sequence sequence =
            DOTween.Sequence();

        // 카드가 포물선을 그리며 목표 위치로 이동 (월드 스페이스 기준)
        sequence.Append(
            cardTransform
                .DOPath(
                    new[]
                    {
                        startWorldPos,
                        midWorldPos,
                        targetWorldPos
                    },
                    discardDuration,
                    PathType.CatmullRom
                )
                .SetEase(Ease.InOutQuad)
        );

        // 이동하는 동안 살짝 회전
        sequence.Join(
            cardTransform
                .DOLocalRotate(
                    new Vector3(
                        0f,
                        0f,
                        randomTilt
                    ),
                    discardDuration,
                    RotateMode.FastBeyond360
                )
                .SetEase(Ease.OutQuad)
        );

        // --------------------------------------------------
        // 착지 (기획서: "착지 순간 그림자가 짧게 강해졌다가 원래대로")
        // 그림자를 강하게 만드는 타이밍을 착지 직전으로 맞춘다.
        // --------------------------------------------------

        if (hasShadow)
        {
            sequence.AppendCallback(() =>
            {
                Color boostedColor =
                    originalShadowColor;

                boostedColor.a =
                    Mathf.Clamp01(
                        originalShadowColor.a * shadowBoost
                    );

                shadow.effectColor =
                    boostedColor;
            });
        }

        sequence.Append(
            cardTransform
                .DOScale(
                    originalScale * landingScale,
                    landingDuration * 0.5f
                )
                .SetEase(Ease.OutQuad)
        );

        sequence.Append(
            cardTransform
                .DOScale(
                    originalScale,
                    landingDuration
                )
                .SetEase(Ease.OutBack)
        );

        // --------------------------------------------------
        // 그림자 복구
        // --------------------------------------------------

        if (hasShadow)
        {
            sequence.Append(
                DOTween.To(
                    () => shadow.effectColor.a,
                    alpha =>
                    {
                        Color color =
                            shadow.effectColor;

                        color.a = alpha;
                        shadow.effectColor = color;
                    },
                    originalShadowColor.a,
                    shadowDuration
                )
            );

            sequence.Join(
                DOTween.To(
                    () => shadow.effectDistance,
                    value =>
                    {
                        shadow.effectDistance = value;
                    },
                    originalShadowDistance,
                    shadowDuration
                )
            );
        }

        // --------------------------------------------------
        // 완료 (카드를 파괴하지 않고 더미에 그대로 편입)
        // --------------------------------------------------

        sequence.OnComplete(() =>
        {
            discardPile.AddExistingCardToDiscardPile(
                card,
                cardData
            );
        });
    }
}