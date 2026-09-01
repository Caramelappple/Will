using _Scripts.LDY;
using DG.Tweening;
using UnityEngine;

public class KTH_DeckUi : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform deckTransform;
    [SerializeField] private LDY_TurnManager turnManager;

    [Header("Hand Card Layout")]
    [SerializeField] private KTH_HandCardLayout handCardLayout;

    [Header("Move Settings")]
    [Tooltip("적 턴일 때 내려가는 로컬 Y 거리")]
    [SerializeField] private float hideOffsetY = 3f;

    [Tooltip("내려가고 올라오는 애니메이션 지속 시간")]
    [SerializeField] private float moveDuration = 0.35f;

    [Tooltip("내려갈 때 이징")]
    [SerializeField] private Ease hideEase = Ease.InBack;

    [Tooltip("올라올 때 이징")]
    [SerializeField] private Ease showEase = Ease.OutBack;

    [Header("Card Gather Settings")]
    [Tooltip("턴 종료 시 카드가 가운데로 모이는 시간")]
    [SerializeField] private float gatherDuration = 0.2f;

    [Tooltip("가운데에서 다시 손패 형태로 펼쳐지는 시간")]
    [SerializeField] private float spreadDuration = 0.25f;

    private Vector3 originalLocalPos;
    private Tween currentTween;
    private Sequence currentSequence;

    private void Awake()
    {
        if (deckTransform != null)
        {
            originalLocalPos =
                deckTransform.localPosition;
        }
    }

    private void OnEnable()
    {
        if (turnManager != null)
        {
            turnManager.OnTurnChanged +=
                HandleTurnChanged;
        }
    }

    private void OnDisable()
    {
        if (turnManager != null)
        {
            turnManager.OnTurnChanged -=
                HandleTurnChanged;
        }

        currentTween?.Kill();
        currentSequence?.Kill();
    }

    private void Start()
    {
        if (turnManager != null)
        {
            ApplyPositionInstant(
                turnManager.CurrentTurn
            );
        }
    }

    private void HandleTurnChanged(
        LDY_Team newTurn)
    {
        currentTween?.Kill();
        currentSequence?.Kill();

        if (deckTransform == null)
        {
            return;
        }

        if (newTurn == LDY_Team.Enemy)
        {
            PlayHideAnimation();
        }
        else
        {
            PlayShowAnimation();
        }
    }

    private void PlayHideAnimation()
    {
        // 카드들을 먼저 가운데로 모음
        if (handCardLayout != null)
        {
            handCardLayout.GatherCardsToCenter(
                gatherDuration
            );
        }

        // 카드가 가운데로 모인 후 전체 덱 오브젝트를 아래로 내림
        currentSequence =
            DOTween.Sequence();

        currentSequence.AppendInterval(
            handCardLayout != null
                ? gatherDuration
                : 0f
        );

        currentSequence.Append(
            deckTransform
                .DOLocalMove(
                    originalLocalPos -
                    new Vector3(
                        0f,
                        hideOffsetY,
                        0f
                    ),
                    moveDuration
                )
                .SetEase(hideEase)
        );
    }

    private void PlayShowAnimation()
    {
        // 먼저 전체 덱 오브젝트를 원래 위치로 올림
        currentSequence =
            DOTween.Sequence();

        currentSequence.Append(
            deckTransform
                .DOLocalMove(
                    originalLocalPos,
                    moveDuration
                )
                .SetEase(showEase)
        );

        // 올라온 후 다시 카드들을 손패 형태로 펼침
        if (handCardLayout != null)
        {
            currentSequence.AppendCallback(
                () =>
                {
                    handCardLayout
                        .RestoreCardsFromCenter(
                            spreadDuration
                        );
                }
            );
        }
    }

    private void ApplyPositionInstant(
        LDY_Team turn)
    {
        if (deckTransform == null)
        {
            return;
        }

        bool isEnemyTurn =
            turn == LDY_Team.Enemy;

        deckTransform.localPosition =
            isEnemyTurn
                ? originalLocalPos -
                  new Vector3(
                      0f,
                      hideOffsetY,
                      0f
                  )
                : originalLocalPos;
    }
}
