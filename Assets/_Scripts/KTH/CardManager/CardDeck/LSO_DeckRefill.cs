using _Scripts.LDY;
using UnityEngine;

/// <summary>
/// 덱이 다 떨어졌을 때 **언제** 버린 더미를 되돌릴지 정한다.
///
/// ── 왜 덱에서 떼어냈나 ────────────────────────────────────
/// KTH_DeckManager 는 원래 셋을 한꺼번에 했다.
///   · 카드를 들고 있기 · 뽑기 · 되섞기
///   · 언제 되섞을지 정하기
///   · 그러려고 턴과 손패를 들여다보기
///
/// 그래서 덱을 고치려면 턴과 손패까지 같이 봐야 했다. 되섞는 시점을 바꾸는
/// 일과 뽑는 방식을 바꾸는 일이 한 파일에서 부딪혔다.
///
/// 지금은 나뉘어 있다.
///   KTH_DeckManager  카드를 들고 있고, 뽑고, 되섞는 **방법**을 안다
///   여기             되섞을 **때**를 정한다
///
/// 되섞는 일 자체는 여전히 덱이 한다. 여기는 부르기만 한다.
/// ─────────────────────────────────────────────────────────
///
/// 씬 배선: 덱과 같은 오브젝트에 붙이면 된다. 참조는 비워두면 찾는다.
/// </summary>
[DisallowMultipleComponent]
public sealed class LSO_DeckRefill : MonoBehaviour
{
    [Header("연결 (비우면 씬에서 찾는다)")]
    [SerializeField] private KTH_DeckManager deckManager;
    [SerializeField] private LDY_TurnManager turnManager;

    [Tooltip("손패. 덱이 비었어도 손에 카드가 남아 있으면 되돌리기를 미룬다.\n" +
             "\n" +
             "없으면 손패를 안 보고 곧바로 되돌린다.")]
    [SerializeField] private KTH_HandCardLayout handLayout;

    [Header("동작")]
    [Tooltip("적 턴이 시작될 때 자동으로 되돌릴지.\n" +
             "\n" +
             "끄면 덱이 비어도 그대로 둔다. 되돌리는 것은 밖에서\n" +
             "KTH_DeckManager.ReshuffleFromDiscard 를 직접 불러야 한다.")]
    [SerializeField] private bool refillOnEnemyTurn = true;

    [Header("진단")]
    [SerializeField] private bool logSteps;

    private void Awake()
    {
        if (deckManager == null) deckManager = FindAnyObjectByType<KTH_DeckManager>();
        if (turnManager == null) turnManager = FindAnyObjectByType<LDY_TurnManager>();

        if (handLayout == null)
            handLayout = KTH_HandCardLayout.Instance != null
                ? KTH_HandCardLayout.Instance
                : FindAnyObjectByType<KTH_HandCardLayout>();
    }

    private void OnEnable()
    {
        if (turnManager == null) turnManager = FindAnyObjectByType<LDY_TurnManager>();

        if (turnManager == null)
        {
            Debug.LogWarning(
                $"{name}: LDY_TurnManager 를 찾지 못해 덱이 떨어져도 되돌리지 않습니다. " +
                "덱이 빈 채로 남습니다.", this);

            return;
        }

        turnManager.OnTurnChanged -= HandleTurnChanged;
        turnManager.OnTurnChanged += HandleTurnChanged;
    }

    private void OnDisable()
    {
        if (turnManager != null)
            turnManager.OnTurnChanged -= HandleTurnChanged;
    }

    /// <summary>
    /// 적 턴이 시작될 때만 본다. 내 턴 중에 덱이 되살아나면 방금 버린 카드가
    /// 곧바로 손에 돌아와서, 버리는 규칙이 있으나 마나 해진다.
    /// </summary>
    private void HandleTurnChanged(LDY_Team team)
    {
        if (team != LDY_Team.Enemy) return;
        if (!refillOnEnemyTurn) return;

        if (deckManager == null)
        {
            Debug.LogWarning($"{name}: 덱을 찾지 못해 되돌리지 못했습니다.", this);
            return;
        }

        // 다 뽑아 써서 빈 것만 되돌린다. 판을 세우느라 잠깐 비어 있는 것과는
        // 다른 상태다 — 그때 되돌리면 방금 만든 덱 위에 지난 판 카드가 얹힌다.
        if (!deckManager.IsExhausted)
        {
            Log($"아직 덱을 다 쓰지 않았습니다. 남은 {deckManager.RemainingCards}장.");
            return;
        }

        if (deckManager.RemainingCards > 0)
        {
            Log($"소진 표시는 있지만 덱에 {deckManager.RemainingCards}장이 남아 있어 두겠습니다.");
            return;
        }

        // 손에 아직 쓸 카드가 있으면 미룬다. 덱이 비었다고 바로 되돌리면
        // 플레이어가 손패를 다 쓰기도 전에 더미가 사라진다.
        if (handLayout != null && handLayout.HandCount > 0)
        {
            Log($"손패에 {handLayout.HandCount}장이 남아 있어 미룹니다.");
            return;
        }

        if (deckManager.ReshuffleFromDiscard())
        {
            Log("버린 더미를 덱으로 되돌렸습니다.");
            return;
        }

        Log("덱은 비었는데 되돌릴 버린 카드가 없습니다.");
    }

    private void Log(string message)
    {
        if (logSteps) Debug.Log($"[{name}] {message}", this);
    }
}
