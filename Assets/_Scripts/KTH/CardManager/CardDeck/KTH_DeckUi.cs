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
    [SerializeField] private float hideOffsetY = 1.5f;

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
        // 전체 덱 오브젝트를 원래 위치로 올림
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

        // 카드들을 손패 형태로 펼침. 예전에는 이걸 위 시퀀스의 AppendCallback으로
        // "덱이 다 올라온 뒤"에 실행했는데, 그 사이(moveDuration)에 턴이 다시
        // 바뀌면 HandleTurnChanged가 currentSequence를 통째로 Kill해서 이 콜백이
        // 영영 실행되지 못하고, 카드가 GatherCardsToCenter가 모아둔 가운데
        // 자리(Vector3.zero)에 그대로 쌓인 채로 남는 버그가 있었다(빠른 턴 전환에서
        // 가끔 재현됨). GatherCardsToCenter는 애초에 즉시 실행이라 이 문제가 없으니,
        // 대칭을 맞춰 여기서도 시퀀스 완료를 기다리지 않고 바로 부른다.
        // RestoreCardsFromCenter -> UpdateHandLayout은 카드마다 DOKill 후 새
        // 트윈을 거는 구조라, 직전에 다른 애니메이션이 진행 중이었어도 항상 최신
        // 호출이 이긴다.
        if (handCardLayout != null)
        {
            handCardLayout.RestoreCardsFromCenter(
                spreadDuration
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
