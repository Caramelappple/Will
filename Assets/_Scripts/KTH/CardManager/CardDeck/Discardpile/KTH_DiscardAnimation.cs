using _Scripts.LSO.Deck.Data;
using DG.Tweening;
using UnityEngine;

// 3D 전환 메모:
// - CanvasGroup(blocksRaycasts/interactable)은 3D에 없는 개념이라 Collider.enabled로 대체.
// - UnityEngine.UI.Shadow는 UI 전용 이펙트라 3D에는 존재하지 않는다.
//   대신 카드 밑에 별도로 둔 그림자용 SpriteRenderer(선택)를 찾아서 알파를 밀었다가 복구하는 방식으로 바꿨다.
//   그림자 오브젝트가 따로 없다면 shadowRenderer를 비워두면 이 부분은 그냥 건너뛴다.
//
// 목표 위치/회전을 여기서 따로 계산하지 않는다:
// 예전엔 여기서 랜덤 목표 위치를 정해서 던진 다음, 도착 후 KTH_DiscardCardUI가
// "진짜 쌓임 위치(더미 높이 반영)"로 또 한 번 옮겨서 착지하자마자 다시 미끄러지는
// 이중 이동이 있었다. 그래서 KTH_DiscardCardUI.GetNextStackTarget()으로 최종
// 목표 위치/회전을 미리 받아서, 처음부터 그 자리로 던진다.
public class KTH_DiscardAnimation : MonoBehaviour
{
    [Header("Discard Animation")]
    [SerializeField] private float discardDuration = 0.18f;

    [Header("Arc (포물선)")]
    [Tooltip("이동 중 카드가 그리는 포물선의 높이")]
    [SerializeField] private float arcHeight = 0.5f;

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
        // 최종 목표 위치/회전을 KTH_DiscardCardUI에서 미리 받아온다.
        // (더미 높이까지 반영된 "진짜" 도착 지점 - 도착 후 다시 옮기지 않는다)
        // ==================================================

        discardPile.GetNextStackTarget(
            out Vector3 targetWorldPos,
            out Quaternion finalRotation
        );

        // ==================================================
        // 포물선
        // ==================================================

        Vector3 startWorldPos =
            cardTransform.position;

        // TransformVector를 쓰면 부모의 Scale까지 곱해져서, 부모 스케일이
        // 작을 때(예: 0.2) 오프셋이 사실상 사라져버린다.
        // 회전만 적용해서 부모 스케일과 무관하게 계산한다.
        Vector3 arcOffsetWorld =
            discardParent.rotation *
            new Vector3(
                0f,
                arcHeight,
                0f
            );

        Vector3 midWorldPos =
            Vector3.Lerp(
                startWorldPos,
                targetWorldPos,
                0.5f
            ) + arcOffsetWorld;

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
                    finalRotation.eulerAngles,
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

            // 이미 최종 위치/회전으로 도착한 상태 - AddExistingCardToDiscardPile은
            // 부모만 갈아끼우고 상태를 유지한다.
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
