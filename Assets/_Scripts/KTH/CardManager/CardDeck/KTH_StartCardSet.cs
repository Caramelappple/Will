using System.Collections;
using _Scripts.LDY;
using _Scripts.LDY.Stage;
using _Scripts.LSO.Stage;
using UnityEngine;

/// <summary>
/// 스테이지 첫 턴의 시작 손패(기본 5장)가 언제 채워지고 언제 치워지는지를 아는 곳.
///
/// 손패를 몇 장 받는지를 정하는 **유일한 곳**이다. 덱을 눌러 뽑는 길도,
/// 턴당 몇 장까지라는 셈도 이제 없다 — 판이 바뀌거나 내 턴이 시작되면
/// 여기서 통째로 새로 준다.
///
/// 세 순간을 듣는다.
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

    [Tooltip("카드가 뽑히는 간격(초)")]
    [SerializeField] private float drawInterval = 0.12f;

    [Header("연결 (비우면 씬에서 찾는다)")]
    [Tooltip("판이 새로 세워지는 것을 들을 곳.\n" +
             "\n" +
             "이게 없으면 게임을 켤 때 한 번만 손패를 받고, 다음 판부터는\n" +
             "지난 판 손패를 그대로 들고 시작한다.")]
    [SerializeField] private LDY_StageDirector stageDirector;

    [SerializeField] private KTH_DeckManager deckManager;

    [Tooltip("클리어했을 때 손패를 날려보낼 더미. 비워두면 씬에서 찾는다.\n" +
             "없으면 연출 없이 그냥 치운다.")]
    [SerializeField] private KTH_DiscardCardUI discardPile;

    [Tooltip("판이 다 돌아온 것을 들을 곳. 비워두면 씬에서 찾는다.\n" +
             "\n" +
             "없으면 기물을 놓는 시점에 곧바로 카드를 받는다 — 판이 아직 뒤집혀 있을 때다.")]
    [SerializeField] private LSO_StageIntroDirector introDirector;

    [Tooltip("턴이 바뀌는 것을 들을 곳. 비워두면 씬에서 찾는다.\n" +
             "\n" +
             "없으면 판이 바뀔 때만 손패를 새로 받는다 — 턴이 지나도\n" +
             "지난 턴 손패를 그대로 들고 있게 된다.")]
    [SerializeField] private LDY_TurnManager turnManager;

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
        if (introDirector == null) introDirector = FindAnyObjectByType<LSO_StageIntroDirector>();
        if (turnManager == null) turnManager = FindAnyObjectByType<LDY_TurnManager>();
    }

    private void Start()
    {
        SubscribeStageDirector();
        SubscribeStageFlow();
        SubscribeIntroDirector();
        SubscribeTurnManager();

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

        if (introDirector != null)
            introDirector.Ready -= HandleStageReady;

        if (turnManager != null)
            turnManager.OnTurnChanged -= HandleTurnChanged;
    }

    /// <summary>
    /// 턴이 바뀌는 것을 듣는다.
    ///
    /// ── 규칙 ──────────────────────────────────────────────────
    /// 내 턴이 끝나면 손패를 버리고, 내 턴이 시작되면 5장을 새로 받는다.
    /// 들고 있는 카드를 아껴 쌓아두는 놀이가 되지 않게 하려는 것이다.
    /// ─────────────────────────────────────────────────────────
    /// </summary>
    private void SubscribeTurnManager()
    {
        if (turnManager == null) turnManager = FindAnyObjectByType<LDY_TurnManager>();

        if (turnManager == null)
        {
            Debug.LogWarning(
                "[KTH_StartCardSet] LDY_TurnManager를 찾지 못해 턴이 지나도 손패가 그대로 남습니다.",
                this);

            return;
        }

        turnManager.OnTurnChanged -= HandleTurnChanged;
        turnManager.OnTurnChanged += HandleTurnChanged;
    }

    private void HandleTurnChanged(LDY_Team team)
    {
        if (team != LDY_Team.Player)
        {
            // 내 턴이 끝났다. 남은 카드를 버린다.
            if (KTH_HandCardLayout.Instance == null) return;

            // 고른 카드를 먼저 내려놓는다.
            //
            // 배치까지 간 카드는 LDY_CardPlacer 가 물리면서 내려가지만, 골라서
            // 올라와 있기만 한 카드는 아무도 안 내려놓는다. 그대로 버리면
            // 올라온 자리에서 곧장 버림 더미로 날아가 움직임이 튄다.
            //
            // 베이스 머지로 손패가 partial 로 쪼개지면서 DeselectAll 이 빠져
            // 컴파일이 깨졌고, 그래서 이 줄이 주석 처리돼 있었다.
            // 메서드를 KTH_HandCardLayout.Placement.cs 에 되살렸다.
            KTH_HandCardLayout.Instance.DeselectAll();

            Log($"내 턴 종료 — 손패 {KTH_HandCardLayout.Instance.HandCount}장을 버립니다.");

            KTH_HandCardLayout.Instance.DiscardHand(discardPile);
            return;
        }

        // ── 판이 이미 끝났으면 뽑지 않는다 ────────────────────────
        // 일반적인 클리어는 내 턴에 일어난다. 마지막 적을 내가 잡으니 턴이
        // 바뀌지 않고, 그래서 여기가 불리지도 않았다.
        //
        // 그런데 적이 마지막 아군을 잡으면서 같이 죽는 판은 다르다.
        // 적 턴이 끝나며 내 턴 신호가 오고, 클리어 연출(판 회전·보상)은 아직
        // 돌고 있다. 그 사이에 여기가 카드를 한 벌 뽑아 덱을 축낸다 —
        // 보상을 받기도 전에 카드가 나가는 것처럼 보이던 것이 이것이다.
        //
        // 끝났는지는 KTH_GameEndManager 에게만 묻는다. 여기서 적 수를 세거나
        // 양초를 보면 판정하는 곳이 둘이 되어 언젠가 서로 다른 말을 한다.
        // ─────────────────────────────────────────────────────────
        if (KTH_GameEndManager.IsBattleOver)
        {
            Log("전투가 이미 끝나 뽑지 않습니다.");
            return;
        }

        // 판을 세우는 중에는 여기서 뽑지 않는다.
        //
        // 스테이지를 세울 때 LDY_TurnManager 도 OnStageLoaded 를 듣고 턴을
        // 시작하므로 이 자리가 먼저 불릴 수 있다. 그때 뽑으면 아직 뒤집힌 판
        // 위로 손패가 먼저 올라오고, 곧이어 HandleStageLoaded 가 그것을 지운다.
        //
        // 판이 다 돌아오면 HandleStageReady 가 받는다.
        if (_waitingForBoard || (introDirector != null && introDirector.IsPlaying))
        {
            Log("판을 세우는 중이라 뽑기를 미룹니다.");
            return;
        }

        Log("내 턴 시작 — 손패를 새로 받습니다.");

        Deal(resetDeck: false);
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

    /// <summary>시작 손패를 갖춘다. 뽑던 중이면 끊고 새로 한다.</summary>
    private void Deal(bool resetDeck)
    {
        Refill(resetDeck, deal: true);
    }

    /// <summary>
    /// 손패와 덱을 손본다.
    /// </summary>
    /// <param name="resetDeck">덱과 버린 더미까지 처음 상태로 되돌릴지.</param>
    /// <param name="deal">
    /// 지금 시작 손패를 뽑을지. 끄면 치우고 되돌리기만 한다.
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

        // 이미 들고 있는 만큼은 빼고 뽑는다.
        //
        // 턴마다 받게 되면서 손패가 완전히 비어 있지 않은 채로 들어올 수 있게 됐다
        // (버리는 연출이 아직 끝나지 않았거나, 버리기가 건너뛰어졌거나).
        // 그때 무조건 다섯 번 뽑으면 손패가 가득 차 거부만 쌓인다.
        int have = KTH_HandCardLayout.Instance != null
            ? KTH_HandCardLayout.Instance.HandCount
            : 0;

        int need = Mathf.Max(0, startingHandCount - have);

        int drawn = 0;

        for (int i = 0; i < need; i++)
        {
            // 덱에 남은 카드가 필요한 수보다 적으면 있는 만큼만 받고 멈춘다.
            bool success = spawnCard.SpawnOneCardPublic();

            // 덱이 떨어졌으면 버린 더미를 섞어 한 번 되살리고 이어서 뽑는다.
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

        Log($"{drawn}장 뽑았습니다. (들고 있던 {have}장 + 필요 {need}장)");

        if (drawn == 0 && need > 0)
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
