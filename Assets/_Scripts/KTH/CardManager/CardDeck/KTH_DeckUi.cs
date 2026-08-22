using _Scripts.LDY;
using DG.Tweening;
using UnityEngine;

public class KTH_DeckUi : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform deckRect;
    [SerializeField] private LDY_TurnManager turnManager;

    [Header("Move Settings")]
    [Tooltip("적 턴일 때 내려가는 Y 거리 (원래 위치 기준 아래로 이동할 값)")]
    [SerializeField] private float hideOffsetY = 300f;

    [Tooltip("내려가고 올라오는 애니메이션 지속 시간")]
    [SerializeField] private float moveDuration = 0.35f;

    [Tooltip("내려갈 때(적 턴) 이징")]
    [SerializeField] private Ease hideEase = Ease.InBack;

    [Tooltip("올라올 때(플레이어 턴) 이징")]
    [SerializeField] private Ease showEase = Ease.OutBack;

    private Vector2 originalAnchoredPos;
    private Tween currentTween;

    private void Awake()
    {
        if (deckRect == null) deckRect = GetComponent<RectTransform>();
        if (turnManager == null) turnManager = FindAnyObjectByType<LDY_TurnManager>();

        if (deckRect != null)
            originalAnchoredPos = deckRect.anchoredPosition;
    }

    private void OnEnable()
    {
        if (turnManager != null)
            turnManager.OnTurnChanged += HandleTurnChanged;
    }

    private void OnDisable()
    {
        if (turnManager != null)
            turnManager.OnTurnChanged -= HandleTurnChanged;

        currentTween?.Kill();
    }

    private void Start()
    {
        // 시작 시 현재 턴 상태에 맞춰 즉시 위치 반영 (애니메이션 없이)
        if (turnManager != null)
            ApplyPositionInstant(turnManager.CurrentTurn);
    }

    private void HandleTurnChanged(LDY_Team newTurn)
    {
        currentTween?.Kill();

        Vector2 targetPos = newTurn == LDY_Team.Enemy
            ? originalAnchoredPos - new Vector2(0f, hideOffsetY)
            : originalAnchoredPos;

        Ease ease = newTurn == LDY_Team.Enemy ? hideEase : showEase;

        currentTween = deckRect.DOAnchorPos(targetPos, moveDuration).SetEase(ease);
    }

    private void ApplyPositionInstant(LDY_Team turn)
    {
        deckRect.anchoredPosition = turn == LDY_Team.Enemy
            ? originalAnchoredPos - new Vector2(0f, hideOffsetY)
            : originalAnchoredPos;
    }
}