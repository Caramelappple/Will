using _Scripts.LSO.Deck.Data;
using DG.Tweening;
using UnityEngine;

// 3D 전환 메모:
// - CanvasGroup(blocksRaycasts/interactable)은 3D에 없는 개념이라 Collider.enabled로 대체.
// - UnityEngine.UI.Shadow는 UI 전용 이펙트라 3D에는 존재하지 않는다.
//   대신 카드 밑에 별도로 둔 그림자용 SpriteRenderer(선택)를 찾아서 알파를 밀었다가 복구하는 방식으로 바꿨다.
//   그림자 오브젝트가 따로 없다면 shadowRenderer를 비워두면 이 부분은 그냥 건너뛴다.
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

    [Header("Shadow (선택)")]
    [Tooltip("카드 밑에 그림자용 SpriteRenderer가 따로 있다면 여기에 연결 (프리팹마다 다르면 카드 쪽에서 GetComponentInChildren로 찾아도 됨). 없으면 비워둬도 된다.")]
    [SerializeField] private string shadowChildName = "Shadow";
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
        // Collider (구 CanvasGroup.blocksRaycasts / interactable 대체)
        // ==================================================

        Collider cardCollider =
            card.GetComponent<Collider>();

        if (cardCollider != null)
        {
            cardCollider.enabled = false;
        }

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
        // Shadow (선택 - 카드 자식 중 "Shadow"라는 이름의 SpriteRenderer)
        // ==================================================

        Transform shadowTransform =
            card.transform.Find(shadowChildName);

        SpriteRenderer shadow =
            shadowTransform != null
                ? shadowTransform.GetComponent<SpriteRenderer>()
                : null;

        Color originalShadowColor =
            Color.clear;

        bool hasShadow =
            shadow != null;

        if (hasShadow)
        {
            originalShadowColor =
                shadow.color;
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

                shadow.color =
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
                    () => shadow.color.a,
                    alpha =>
                    {
                        if (shadow == null)
                            return;

                        Color color =
                            shadow.color;

                        color.a = alpha;

                        shadow.color =
                            color;
                    },
                    originalShadowColor.a,
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
