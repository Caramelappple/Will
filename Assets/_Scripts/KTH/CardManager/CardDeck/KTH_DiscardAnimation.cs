using _Scripts.LSO.Deck.Data;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class KTH_DiscardAnimation : MonoBehaviour
{
    [Header("Discard Animation")]
    [SerializeField] private float discardDuration = 0.18f;

    [Header("Arc (포물선)")]
    [Tooltip("이동 중 카드가 그리는 포물선의 높이")]
    [SerializeField] private float arcHeight = 60f;

    [Header("Random Rotation")]
    [SerializeField] private float minRandomTilt = 5f;
    [SerializeField] private float maxRandomTilt = 15f;

    [Header("Flip")]
    [Tooltip("버린 카드로 이동할 때 Y축으로 뒤집히는 각도")]
    [SerializeField] private float flipAngle = 180f;

    [Header("Random Stack Offset")]
    [SerializeField] private float minStackOffset = 3f;
    [SerializeField] private float maxStackOffset = 5f;

    [Header("Landing")]
    [SerializeField] private float landingDuration = 0.08f;
    [SerializeField] private float landingScale = 0.96f;

    [Header("Shadow")]
    [SerializeField] private float shadowBoost = 1.8f;
    [SerializeField] private float shadowDuration = 0.12f;

    /// <summary>
    /// 현재 디스카드 애니메이션이 재생 중인지 알려줌.
    /// </summary>
    public bool IsPlaying { get; private set; }

    /// <summary>
    /// 디스카드 애니메이션이 완전히 끝났을 때 호출할 콜백.
    /// </summary>
    public void Play(
        KTH_HandCard card,
        KTH_DiscardCardUI discardPile,
        LSO_CardSO cardData,
        System.Action onComplete = null)
    {
        if (card == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (discardPile == null ||
            discardPile.DiscardCardTransform == null)
        {
            Destroy(card.gameObject);
            onComplete?.Invoke();
            return;
        }

        IsPlaying = true;

        Transform cardTransform = card.transform;

        // ==================================================
        // CanvasGroup
        // ==================================================

        CanvasGroup canvasGroup =
            card.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup =
                card.gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        // ==================================================
        // 부모 변경
        // ==================================================

        Transform discardParent =
            discardPile.DiscardCardTransform.parent;

        cardTransform.DOKill();

        cardTransform.SetParent(
            discardParent,
            true
        );

        // ==================================================
        // 목표 위치 랜덤 오프셋
        // ==================================================

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

        offsetX *=
            Random.value < 0.5f
                ? -1f
                : 1f;

        offsetY *=
            Random.value < 0.5f
                ? -1f
                : 1f;

        Vector3 localOffset =
            new Vector3(
                offsetX,
                offsetY,
                0f
            );

        Vector3 targetWorldPos =
            discardPile.DiscardCardTransform.position +
            discardParent.TransformVector(
                localOffset
            );

        // ==================================================
        // 포물선
        // ==================================================

        Vector3 startWorldPos =
            cardTransform.position;

        Vector3 arcOffsetWorld =
            discardParent.TransformVector(
                new Vector3(
                    0f,
                    arcHeight,
                    0f
                )
            );

        Vector3 midWorldPos =
            Vector3.Lerp(
                startWorldPos,
                targetWorldPos,
                0.5f
            ) + arcOffsetWorld;

        // ==================================================
        // 랜덤 Z 회전
        // ==================================================

        float randomTilt =
            Random.Range(
                minRandomTilt,
                maxRandomTilt
            );

        if (Random.value < 0.5f)
        {
            randomTilt *= -1f;
        }

        // ==================================================
        // 기존 Scale 저장
        // ==================================================

        Vector3 originalScale =
            cardTransform.localScale;

        // ==================================================
        // Shadow
        // ==================================================

        Shadow shadow =
            card.GetComponentInChildren<Shadow>();

        Color originalShadowColor =
            Color.clear;

        Vector2 originalShadowDistance =
            Vector2.zero;

        bool hasShadow =
            shadow != null;

        if (hasShadow)
        {
            originalShadowColor =
                shadow.effectColor;

            originalShadowDistance =
                shadow.effectDistance;
        }

        // ==================================================
        // Animation
        // ==================================================

        Sequence sequence =
            DOTween.Sequence();

        sequence.SetTarget(
            cardTransform
        );

        // ==================================================
        // 포물선 이동
        // ==================================================

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

        // ==================================================
        // 이동하면서 뒤집기
        // ==================================================

        sequence.Join(
            cardTransform
                .DOLocalRotate(
                    new Vector3(
                        0f,
                        flipAngle,
                        randomTilt
                    ),
                    discardDuration,
                    RotateMode.FastBeyond360
                )
                .SetEase(Ease.InOutQuad)
        );

        // ==================================================
        // 착지 그림자 강화
        // ==================================================

        if (hasShadow)
        {
            sequence.AppendCallback(() =>
            {
                if (shadow == null)
                    return;

                Color boostedColor =
                    originalShadowColor;

                boostedColor.a =
                    Mathf.Clamp01(
                        originalShadowColor.a *
                        shadowBoost
                    );

                shadow.effectColor =
                    boostedColor;
            });
        }

        // ==================================================
        // 착지 눌림
        // ==================================================

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

        // ==================================================
        // 그림자 복구
        // ==================================================

        if (hasShadow)
        {
            sequence.Append(
                DOTween.To(
                    () => shadow.effectColor.a,
                    alpha =>
                    {
                        if (shadow == null)
                            return;

                        Color color =
                            shadow.effectColor;

                        color.a = alpha;

                        shadow.effectColor =
                            color;
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
                        if (shadow == null)
                            return;

                        shadow.effectDistance =
                            value;
                    },
                    originalShadowDistance,
                    shadowDuration
                )
            );
        }

        // ==================================================
        // 완료
        // ==================================================

        sequence.OnComplete(() =>
        {
            IsPlaying = false;

            if (card == null)
            {
                onComplete?.Invoke();
                return;
            }

            // Y = 180 상태 그대로 유지
            discardPile.AddExistingCardToDiscardPile(
                card,
                cardData
            );

            // 디스카드 애니메이션이 완전히 끝난 후 호출
            onComplete?.Invoke();
        });

        // ==================================================
        // 혹시 외부에서 Kill 되었을 경우
        // ==================================================

        sequence.OnKill(() =>
        {
            IsPlaying = false;
        });
    }
}