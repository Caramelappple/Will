using System.Collections;
using _Scripts.LDY;
using _Scripts.LDY.Stage;
using _Scripts.LSO.Stage;
using UnityEngine;

/// <summary>
/// 손패가 언제 채워지고 언제 치워지는지를 아는 곳.
///
/// 세 순간을 듣는다.
///   내 턴이 끝날 때    손패를 버린다 (적 턴 동안 손이 비어 있다)
///   내 턴이 시작될 때  5장을 새로 받는다
///   스테이지 클리어    손패를 버린다 — 판이 뒤집히기 전이라 화면에 보인다
///   판이 새로 세워질 때 손패를 비우고 덱을 되돌린다 (아직 안 뽑는다)
///   판이 다 돌아왔을 때 5장을 받는다
///
/// ── 왜 스테이지마다 다시 하나 ─────────────────────────────
/// 씬을 넘기지 않고 같은 화면에서 다음 판을 세우므로 Start 가 다시 돌지 않는다.
/// 예전에는 그래서 첫 판의 손패가 런 끝까지 따라다녔다.
/// ─────────────────────────────────────────────────────────
///
/// 판을 세울 때의 순서가 중요하다.
///   1. 손패를 비운다      — 덱을 되돌리기 전에 치워야 카드가 두 번 존재하지 않는다
///   2. 덱을 되돌린다      — 보유 카드 전체로 다시 만들고 섞는다
///   3. 판이 다 돌아오면   — 그때 5장을 뽑는다
///
/// 3번을 앞당기면 안 된다. 기물을 놓는 시점에는 판이 아직 뒤집혀 있어서,
/// 그때 카드를 받으면 돌아오는 판 위로 손패가 먼저 올라온다.
///
/// 씬 배선: 아무 곳에나 하나. 참조는 비워두면 찾는다.
/// </summary>
public class KTH_StartCardSet : MonoBehaviour
{
    [Header("손패")]
    [SerializeField] private int startingHandCount = 5;

    [Tooltip("초기 카드가 뽑히는 간격(초)")]
    [SerializeField] private float drawInterval = 0.12f;

    [Header("연결 (비우면 씬에서 찾는다)")]
    [Tooltip("판이 새로 세워지는 것을 들을 곳.\n" +
             "\n" +
             "이게 없으면 게임을 켤 때 한 번만 손패를 받고, 다음 판부터는\n" +
             "지난 판 손패를 그대로 들고 시작한다.")]
    [SerializeField] private LDY_StageDirector stageDirector;

    [SerializeField] private KTH_DeckManager deckManager;

    [Tooltip("턴이 넘어가는 것을 들을 곳. 비워두면 씬에서 찾는다.\n" +
             "\n" +
             "없으면 매 턴 손패를 갈아주지 못한다. 판이 바뀔 때만 새로 받는다.")]
    [SerializeField] private LDY_TurnManager turnManager;

    [Tooltip("클리어했을 때 손패를 날려보낼 더미. 비워두면 씬에서 찾는다.\n" +
             "없으면 연출 없이 그냥 치운다.")]
    [SerializeField] private KTH_DiscardCardUI discardPile;

    [Tooltip("판이 다 돌아온 것을 들을 곳. 비워두면 씬에서 찾는다.\n" +
             "\n" +
             "없으면 기물을 놓는 시점에 곧바로 카드를 받는다 — 판이 아직 뒤집혀 있을 때다.")]
    [SerializeField] private LSO_StageIntroDirector introDirector;

    [Header("진단")]
    [SerializeField] private bool logSteps;

    private KTH_SpawnCard spawnCard;
    private Coroutine _routine;
    private LSO_StageFlow _flow;

    /// <summary>판이 돌아오기를 기다리는 중인지. 그동안에는 카드를 받지 않는다.</summary>
    private bool _waitingForBoard;

    private void Awake()
    {
        spawnCard = FindAnyObjectByType<KTH_SpawnCard>();

        if (deckManager == null) deckManager = FindAnyObjectByType<KTH_DeckManager>();
        if (discardPile == null) discardPile = FindAnyObjectByType<KTH_DiscardCardUI>();
        if (turnManager == null) turnManager = FindAnyObjectByType<LDY_TurnManager>();
        if (introDirector == null) introDirector = FindAnyObjectByType<LSO_StageIntroDirector>();
    }

    private void Start()
    {
        SubscribeStageDirector();
        SubscribeStageFlow();
        SubscribeTurnManager();
        SubscribeIntroDirector();

        if (spawnCard == null)
        {
            Debug.LogError("[KTH_StartCardSet] KTH_SpawnCard를 찾을 수 없습니다.", this);
            return;
        }

        // 첫 판. 덱은 KTH_DeckManager.Start 가 이미 만들어놨으므로 뽑기만 한다.
        Deal(resetDeck: false);
    }

    private void OnDestroy()
    {
        if (stageDirector != null)
            stageDirector.OnStageLoaded -= HandleStageLoaded;

        if (_flow != null)
            _flow.StageCleared -= HandleStageCleared;

        if (turnManager != null)
            turnManager.OnTurnChanged -= HandleTurnChanged;

        if (introDirector != null)
            introDirector.Ready -= HandleStageReady;
    }

    /// <summary>
    /// 판이 완전히 돌아온 것을 듣는다.
    ///
    /// 기물은 판이 뒤집힌 동안에 놓인다(LSO_StageIntroDirector). 그때 카드까지
    /// 받으면 아직 돌아오지도 않은 판 위로 손패가 먼저 올라온다.
    /// 그래서 뽑는 것만 여기까지 미룬다.
    /// </summary>
    private void SubscribeIntroDirector()
    {
        if (introDirector == null) introDirector = FindAnyObjectByType<LSO_StageIntroDirector>();

        if (introDirector == null)
        {
            Debug.LogWarning(
                "[KTH_StartCardSet] LSO_StageIntroDirector를 찾지 못했습니다. " +
                "판이 돌아오기 전에 카드를 받게 됩니다.",
                this);

            return;
        }

        introDirector.Ready -= HandleStageReady;
        introDirector.Ready += HandleStageReady;
    }

    private void HandleStageReady(LDY_StageSO stage)
    {
        if (!_waitingForBoard) return;

        _waitingForBoard = false;

        Log("판이 다 돌아왔습니다 — 손패를 받습니다.");

        Deal(resetDeck: false);
    }

    /// <summary>
    /// 턴이 넘어가는 것을 듣는다.
    ///
    /// 적 턴이 시작됐다 = 내 턴이 끝났다. 그때 손패를 버린다.
    /// 다시 내 턴이 오면 새로 받는다.
    /// </summary>
    private void SubscribeTurnManager()
    {
        if (turnManager == null) turnManager = FindAnyObjectByType<LDY_TurnManager>();

        if (turnManager == null)
        {
            Debug.LogWarning(
                "[KTH_StartCardSet] LDY_TurnManager를 찾지 못해 매 턴 손패를 갈지 못합니다. " +
                "판이 바뀔 때만 새로 받습니다.",
                this);

            return;
        }

        turnManager.OnTurnChanged -= HandleTurnChanged;
        turnManager.OnTurnChanged += HandleTurnChanged;
    }

    private void HandleTurnChanged(LDY_Team team)
    {
        if (KTH_HandCardLayout.Instance == null) return;

        if (team == LDY_Team.Enemy)
        {
            // 내 턴이 끝났다. 남은 카드는 다음 턴으로 넘어가지 않는다.
            if (KTH_HandCardLayout.Instance.HandCount == 0) return;

            Log($"턴 종료 — 손패 {KTH_HandCardLayout.Instance.HandCount}장을 버립니다.");

            KTH_HandCardLayout.Instance.DiscardHand(discardPile);
            return;
        }

        // 내 턴이 시작됐다. 손이 비어 있을 때만 받는다.
        //
        // 판을 세울 때도 이 신호가 한 번 나가는데(LDY_TurnManager.BeginPlayerTurn),
        // 그때는 HandleStageLoaded 가 이미 나눠주고 있다. 두 곳이 각자 5장씩
        // 주면 10장이 된다. 아직 나눠주는 중인지(_routine)와 손이 비었는지를
        // 함께 보고 겹치지 않게 한다.
        // 판이 아직 돌아오는 중이면 기다린다. HandleStageReady 가 받아준다.
        if (_waitingForBoard) return;

        if (_routine != null) return;
        if (KTH_HandCardLayout.Instance.HandCount > 0) return;

        Log("내 턴 — 손패를 새로 받습니다.");

        Deal(resetDeck: false);
    }

    /// <summary>
    /// 클리어를 듣는다. 손패를 버리는 것은 판이 뒤집히기 **전**이라 화면에 보인다.
    ///
    /// 새 판을 세울 때 치우지 않고 여기서 버리는 이유가 그것이다.
    /// 세울 때는 이미 판이 뒤집혀 있어 아무것도 안 보인다.
    /// </summary>
    private void SubscribeStageFlow()
    {
        if (LSO_StageFlow.HasInstance) _flow = LSO_StageFlow.Instance;

        if (_flow == null)
        {
            Debug.LogWarning(
                "[KTH_StartCardSet] LSO_StageFlow를 찾지 못해 클리어 때 손패를 버리지 못합니다. " +
                "다음 판을 세울 때 조용히 치워집니다.",
                this);

            return;
        }

        _flow.StageCleared -= HandleStageCleared;
        _flow.StageCleared += HandleStageCleared;
    }

    private void HandleStageCleared(LDY_StageSO stage)
    {
        if (KTH_HandCardLayout.Instance == null) return;

        Log($"클리어 — 손패 {KTH_HandCardLayout.Instance.HandCount}장을 버립니다.");

        KTH_HandCardLayout.Instance.DiscardHand(discardPile);
    }

    private void SubscribeStageDirector()
    {
        if (stageDirector == null) stageDirector = FindAnyObjectByType<LDY_StageDirector>();

        if (stageDirector == null)
        {
            Debug.LogWarning(
                "[KTH_StartCardSet] LDY_StageDirector를 찾지 못해 " +
                "다음 판에서 손패를 새로 받지 못합니다. 지난 판 손패가 그대로 남습니다.",
                this);

            return;
        }

        stageDirector.OnStageLoaded -= HandleStageLoaded;
        stageDirector.OnStageLoaded += HandleStageLoaded;
    }

    private void HandleStageLoaded(LDY_StageSO stage)
    {
        Log($"새 판 — 손패를 비우고 덱을 되돌립니다. ({(stage != null ? stage.stageName : "알 수 없음")})");

        // 뒤집힌 판을 되돌리는 연출을 거쳐 들어온 경우에는 뽑기를 미룬다.
        // 판이 다 돌아오면 HandleStageReady 가 받는다.
        //
        // 첫 판과 디버그 재적재는 그 연출을 거치지 않으므로 곧바로 뽑는다.
        // 그때는 Ready 가 오지 않아서, 기다리면 영영 빈손이 된다.
        bool waitForBoard = introDirector != null && introDirector.IsPlaying;

        _waitingForBoard = waitForBoard;

        Refill(resetDeck: true, deal: !waitForBoard);
    }

    /// <summary>손패를 갖춘다. 뽑던 중이면 끊고 새로 한다.</summary>
    private void Deal(bool resetDeck)
    {
        Refill(resetDeck, deal: true);
    }

    /// <summary>
    /// 손패와 덱을 손본다.
    /// </summary>
    /// <param name="resetDeck">덱과 버린 더미까지 처음 상태로 되돌릴지.</param>
    /// <param name="deal">
    /// 지금 카드를 뽑을지. 끄면 치우고 되돌리기만 한다.
    ///
    /// 판이 돌아오는 연출을 기다려야 할 때 끈다. 기물은 판이 뒤집힌 동안 놓이는데,
    /// 그때 같이 뽑으면 아직 돌아오지도 않은 판 위로 손패가 먼저 올라온다.
    /// </param>
    private void Refill(bool resetDeck, bool deal)
    {
        if (spawnCard == null) return;

        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        if (resetDeck)
        {
            // 손패를 먼저 치운다. 덱을 다시 만든 뒤에 치우면 같은 카드가
            // 손패와 덱에 동시에 있는 순간이 생긴다.
            if (KTH_HandCardLayout.Instance != null)
                KTH_HandCardLayout.Instance.ClearHand();
            else
                Debug.LogWarning("[KTH_StartCardSet] 손패를 찾지 못해 비우지 못했습니다.", this);

            if (deckManager != null)
                deckManager.ResetForNewStage();
            else
                Debug.LogWarning("[KTH_StartCardSet] 덱 매니저를 찾지 못해 되돌리지 못했습니다.", this);
        }

        if (!deal) return;

        _routine = StartCoroutine(Co_StartDraw());
    }

    private IEnumerator Co_StartDraw()
    {
        // UI 및 Layout 생성이 완료되도록 1프레임 대기
        yield return null;

        int drawn = 0;

        for (int i = 0; i < startingHandCount; i++)
        {
            // 시작 핸드는 턴당 드로우 횟수 제한을 무시하고 지급된다.
            // 덱에 남은 카드가 startingHandCount보다 적으면 있는 만큼만 받고 자동으로 멈춘다.
            bool success = spawnCard.SpawnOneCardPublic();

            // 매 턴 5장씩 나가면 덱이 금방 떨어진다.
            // 떨어졌으면 버린 더미를 섞어 한 번 되살리고 이어서 뽑는다.
            if (!success &&
                deckManager != null &&
                deckManager.RemainingCards == 0 &&
                deckManager.ReshuffleFromDiscard())
            {
                success = spawnCard.SpawnOneCardPublic();
            }

            if (!success) break;

            drawn++;

            yield return new WaitForSeconds(drawInterval);
        }

        _routine = null;

        Log($"{drawn}장 뽑았습니다.");

        if (drawn == 0)
        {
            Debug.LogWarning(
                "[KTH_StartCardSet] 한 장도 뽑지 못했습니다. " +
                "덱이 비었거나 손패가 가득 찼습니다. 위 줄에 이유가 있습니다.",
                this);
        }
    }

    private void Log(string message)
    {
        if (logSteps) Debug.Log($"[KTH_StartCardSet] {message}", this);
    }
}
