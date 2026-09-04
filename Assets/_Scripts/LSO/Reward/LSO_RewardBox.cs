using System.Collections;
using System.Collections.Generic;
using _Scripts.LSO.Will;
using DG.Tweening;
using UnityEngine;
using _Scripts.LSO.UI.Input;

namespace _Scripts.LSO.Reward
{
    /// <summary>
    /// 보상 절차의 순서를 아는 유일한 곳. 상자 그 자체다.
    ///
    /// LSO_RewardDraft(뽑기) · LSO_RewardCard(표시) · LSO_RewardClaim(지급)은 서로를 모른다.
    /// 누군가는 "뽑고 → 보여주고 → 기다리고 → 지급한다"를 알아야 하고, 그것이 이 클래스다.
    ///
    /// 카드가 놓일 자리를 정하는 것도 여기뿐이다.
    /// 카드가 스스로 자리를 잡기 시작하면 상자가 아는 것과 어긋난다.
    /// 턴 레버에서 자리 주인이 셋이라 시스템을 통째로 버린 적이 있다.
    ///
    /// 곁가지는 떼어냈다.
    ///   LSO_RewardLayout     몇 번째면 어디에 서는지 (상태 없는 계산)
    ///   LSO_RewardCardPool   빌려주고 돌려받기, 쉬는 자리
    ///   LSO_RewardNoteStep   유언 메모장 차례 하나를 통째로
    ///   LSO_RewardClickGate  지금 무엇이 눌려도 되는지를 핸들러에 반영
    /// 전부 "언제"는 모른다. 순서는 여전히 이 클래스만 안다.
    ///
    /// 씬 배선: Collider + LSO_ButtonClickHandler 와 함께 붙일 것.
    /// 3D 물건이므로 씬에 EventSystem, 카메라에 Physics Raycaster가 있어야 한다.
    /// </summary>
    [RequireComponent(typeof(LSO_ButtonClickHandler))]
    public partial class LSO_RewardBox : MonoBehaviour, LSO_IClickEffect
    {
        /// <summary>보상이 어디까지 진행됐는지. 클릭의 뜻을 정하는 것이 이 값이다.</summary>
        private enum Phase
        {
            /// <summary>아직 시작하지 않았다. 클릭해도 아무 일이 없다.</summary>
            Idle,

            /// <summary>닫힌 상자. 누르면 열린다.</summary>
            Closed,

            /// <summary>뚜껑이 도는 중. 클릭을 버린다.</summary>
            Opening,

            /// <summary>뚜껑이 열린 채로 카드를 꺼낼 클릭을 기다린다.</summary>
            Opened,

            /// <summary>카드가 하나씩 나오는 중. 클릭을 버린다.</summary>
            Dealing,

            /// <summary>고르기를 기다린다. 카드를 누르면 그 자리에서 확정된다.</summary>
            Selecting,

            /// <summary>
            /// 고른 뒤 나머지 카드가 상자로 돌아가고, 고른 카드가 덱으로 가는 중.
            /// 클릭을 버린다.
            /// </summary>
            Lowering,

            /// <summary>처음 보는 유언이라 메모장이 준비됐다. 상자를 누르면 나온다.</summary>
            NoteWaiting,

            /// <summary>
            /// 메모장 차례가 도는 중. 올라오고, 눌리기를 기다리고, 들어간다.
            /// 그 안의 순서는 LSO_RewardNoteStep이 안다.
            /// </summary>
            NotePlaying,

            /// <summary>정리하는 중. 클릭을 버린다.</summary>
            Closing
        }

        [Header("연결")]
        [Tooltip("뚜껑 연출. 비워두면 같은 오브젝트에서 찾는다.")]
        [SerializeField] private LSO_BoxOpenEffect lid;

        [Tooltip("스테이지별 보상 후보 테이블.")]
        [SerializeField] private LSO_RewardTableSO table;

        [Tooltip("기물 보상에 쓸 카드 원본.")]
        [SerializeField] private LSO_RewardPieceCard pieceCardPrefab;

        [Tooltip("유언에 쓸 메모장 원본. 고르는 것과 보여주는 것이 같아서 하나만 꽂는다.\n" +
                 "비워두면 유언 후보가 나와도 만들지 못한다.")]
        [SerializeField] private LSO_WillNote willNotePrefab;

        [Tooltip("카드가 늘어설 기준 자리. 비워두면 상자 자신을 쓴다.")]
        [SerializeField] private Transform cardAnchor;

        [Tooltip("카드가 드나드는 상자 속 자리. 빈 오브젝트를 하나 만들어 꽂는다.\n" +
                 "\n" +
                 "여기서 솟아오르고, 고르지 않은 것은 여기로 돌아간다.\n" +
                 "뚜껑 안쪽이 보이는 깊이에 두면 상자에서 나오는 것처럼 보인다.\n" +
                 "\n" +
                 "비워두면 Card Anchor 자리를 그대로 쓴다.")]
        [SerializeField] private Transform cardInsideAnchor;

        [Tooltip("메모장이 올라와 멈출 자리. 비워두면 Card Anchor를 쓴다.\n" +
                 "\n" +
                 "카드와 따로 두는 이유는 크기도 개수도 다르기 때문이다.\n" +
                 "카드는 세 장이 벌어져 서고, 메모장은 한 장이 가운데 선다.")]
        [SerializeField] private Transform noteAnchor;

        [Tooltip("메모장이 드나드는 상자 속 자리. 비워두면 Note Anchor 자리를 쓴다.")]
        [SerializeField] private Transform noteInsideAnchor;

        [Tooltip("상자의 클릭 핸들러. 비워두면 같은 오브젝트에서 찾는다.\n" +
                 "연출이 도는 동안 이것을 꺼서 커서까지 함께 막는다.")]
        [SerializeField] private LSO_ButtonClickHandler clickHandler;


        [Header("배치")]
        [Tooltip("카드가 몇 번째면 어디에 서는지. 간격·높이·기울기를 여기서 정한다.")]
        [SerializeField] private LSO_RewardLayout layout = new LSO_RewardLayout();

        [Header("연출")]
        [Tooltip("카드 한 장이 솟는 데 걸리는 시간.")]
        [SerializeField, Min(0f)] private float riseDuration = 0.35f;

        [Tooltip("다음 카드가 나오기까지의 간격.")]
        [SerializeField, Min(0f)] private float dealInterval = 0.12f;

        [SerializeField] private Ease riseEase = Ease.OutBack;

        [Tooltip("카드를 고른 순간 그 자리에서 더 밀려나는 양. 기준 자리의 로컬 축이다.\n" +
                 "\n" +
                 "누른 즉시 이만큼 움직여 '골랐다'를 알린다.\n" +
                 "(0,0,0) 으로 두면 누른 뒤 Pick Hold 동안 아무 변화가 없어 멈춘 것처럼 보인다.")]
        [SerializeField] private Vector3 pickLift = new Vector3(0f, 0.25f, -0.2f);

        [Tooltip("고른 카드가 밀려나는 데 걸리는 시간. 짧아야 누른 즉시 반응한 느낌이 난다.")]
        [SerializeField, Min(0f)] private float pickLiftDuration = 0.12f;

        [SerializeField] private Ease pickLiftEase = Ease.OutCubic;

        [Tooltip("고른 카드가 떠오른 채로 머무는 시간. 위 연출이 끝난 뒤부터 센다.\n" +
                 "\n" +
                 "이 시간이 지나면 나머지는 상자로, 고른 것은 덱으로 움직인다.")]
        [SerializeField, Min(0f)] private float pickHold = 0.35f;

        [Tooltip("카드가 다 정리된 뒤 다음 단계로 넘어가기까지 두는 시간.")]
        [SerializeField, Min(0f)] private float claimHold = 0.6f;

        [Header("덱으로 보내기")]
        [Tooltip("고른 카드가 날아갈 자리. 손패나 덱 더미를 꽂는다.\n" +
                 "\n" +
                 "비워두면 그냥 상자 안으로 들어간다. 덱에 추가되는 것은 마찬가지지만,\n" +
                 "어디로 갔는지 화면에 보이지 않는다.")]
        [SerializeField] private Transform deckAnchor;

        [Tooltip("덱까지 날아가는 데 걸리는 시간.")]
        [SerializeField, Min(0f)] private float toDeckDuration = 0.5f;

        [Tooltip("덱으로 들어갈 때의 크기 배율. 1보다 작으면 멀어지듯 작아진다.")]
        [SerializeField, Min(0.01f)] private float toDeckScale = 0.5f;

        [SerializeField] private Ease toDeckEase = Ease.InCubic;

        [Header("유언 메모장")]
        [Tooltip("고른 카드에 유언이 딸려 있을 때의 차례. 올라오고, 눌리면 들어간다.")]
        [SerializeField] private LSO_RewardNoteStep noteStep = new LSO_RewardNoteStep();

        [Header("반응")]
        [Tooltip("보상이 시작돼 상자를 누를 수 있게 됐을 때.\n" +
                 "아직 닫혀 있다 — 누르면 열린다. 커서 바꾸기를 여기 건다.")]
        [SerializeField] private LSO_RewardEvent onReady;

        [Tooltip("뚜껑을 열기 시작했을 때. 경첩 삐걱이는 소리를 여기 건다.")]
        [SerializeField] private LSO_RewardEvent onOpening;

        [Tooltip("뚜껑이 다 열렸을 때. 상자 안에서 빛이 새어나오는 연출 등을 건다.\n" +
                 "카드는 아직 나오지 않았다 — 한 번 더 눌러야 나온다.")]
        [SerializeField] private LSO_RewardEvent onOpened;

        [Tooltip("카드가 전부 나와 고를 수 있게 됐을 때.")]
        [SerializeField] private LSO_RewardEvent onDealt;

        [Tooltip("보상이 지급됐을 때. 인자는 고른 보상이다.")]
        [SerializeField] private LSO_RewardEvent onClaimed;

        [Tooltip("처음 보는 유언이라 메모장이 준비됐을 때. 커서를 바꾸는 자리다.\n" +
                 "이때 상자를 누르면 메모장이 나온다.")]
        [SerializeField] private LSO_RewardEvent onNoteReady;

        [Tooltip("메모장이 다 올라왔을 때.")]
        [SerializeField] private LSO_RewardEvent onNoteShown;

        [Tooltip("메모장이 상자로 돌아가고 유언이 풀렸을 때.")]
        [SerializeField] private LSO_RewardEvent onWillUnlocked;

        [Tooltip("정리까지 끝났을 때. 다음 연출(체스판 뒤집기 등)을 여기 건다.")]
        [SerializeField] private LSO_RewardEvent onFinished;

        [Header("진단")]
        [Tooltip("켜면 단계가 바뀔 때마다 콘솔에 찍는다.\n" +
                 "연출이 예상과 다르게 흐를 때 어디서 건너뛰었는지 보인다.")]
        [SerializeField] private bool logPhases;

        private readonly LSO_RewardDraft _draft = new();
        private readonly List<LSO_RewardCard> _cards = new();

        // 카드를 빌려주고 돌려받는 곳. 상자 속 자리도 이쪽이 안다.
        private LSO_RewardCardPool _pool;

        // 지금 무엇이 눌려도 되는지를 핸들러에 반영한다.
        private LSO_RewardClickGate _gate;

        private Phase _phase = Phase.Idle;

        /// <summary>
        /// 단계를 바꾸는 유일한 통로.
        ///
        /// 대입을 여기로 모아두면 "언제 어디서 바뀌었나"를 한 곳에서 볼 수 있다.
        /// 연출이 예상과 다르게 흐를 때, 어느 줄이 단계를 옮겼는지가 제일 먼저 알고 싶은 것이다.
        /// </summary>
        private void SetPhase(Phase next)
        {
            if (_phase == next) return;

            if (logPhases)
                Debug.Log($"[{name}] 단계 {_phase} → {next}", this);

            _phase = next;
        }
        private int _chapter;
        private int _stage;

        // 유언 메모장을 거치는 동안 들고 있어야 하는 것들.
        // 클릭 두 번에 걸쳐 진행되므로 코루틴 지역 변수로는 이어지지 않는다.
        private LSO_RewardOption _chosenOption;

        /// <summary>
        /// 지금 씬의 상자.
        ///
        /// DontDestroyOnLoad가 아니다. 상자는 스테이지 연출물이라 씬과 함께 사라지는 것이 맞다.
        /// 해금 목록처럼 런 전체를 살아남아야 하는 것은 LSO_ItemLibraryManager가 들고 있다.
        ///
        /// 맵 매니저가 씬을 넘길 때 이걸 찾는다. 인스펙터 참조로 두면
        /// 상자가 전투 씬에 있고 맵 매니저는 씬을 넘어다녀서 연결이 끊긴다.
        /// </summary>
        public static LSO_RewardBox Instance { get; private set; }

        /// <summary>
        /// 정리까지 끝났을 때. 인자는 고른 보상이며, 못 고르고 끝났으면 null이다.
        ///
        /// 인스펙터의 On Finished와 같은 시점에 발행된다.
        /// 씬을 넘기는 쪽처럼 코드로 구독해야 하는 곳이 이걸 쓴다.
        /// </summary>
        public event System.Action<LSO_RewardOption> OnFinished;

        /// <summary>클릭을 받지 않는 구간인지. 밖에서 커서 모양을 바꿀 때 본다.</summary>
        public bool IsBusy =>
            _phase == Phase.Opening
            || _phase == Phase.Dealing
            || _phase == Phase.Lowering
            || _phase == Phase.NotePlaying
            || _phase == Phase.Closing;

        /// <summary>
        /// 지금 상자를 눌러서 뭔가가 일어나는지.
        ///
        /// 세 번 누른다 — 뚜껑을 열고, 카드를 꺼내고, 메모장을 꺼낸다.
        /// 닫는 것만 자동이다.
        /// </summary>
        private bool BoxAcceptsClick =>
            _phase == Phase.Closed
            || _phase == Phase.Opened
            || _phase == Phase.NoteWaiting;

        /// <summary>지금 카드를 눌러서 고를 수 있는지.</summary>
        private bool CardsAcceptClick => _phase == Phase.Selecting;

        /// <summary>
        /// 지금 무엇이 눌려도 되는지를 매 프레임 반영한다.
        ///
        /// 단계가 바뀌는 자리가 열 군데 넘는데, 그때마다 여닫는 코드를 같이 적으면
        /// 한 곳만 빠뜨려도 그 단계에서만 눌린다. 눈으로 못 찾는 종류의 버그다.
        /// 하는 일이 값 비교 몇 개라 매 프레임 돌아도 부담이 없다.
        /// </summary>
        private void Update()
        {
            if (_gate == null) return;

            _gate.SetBox(BoxAcceptsClick);
            _gate.SetCards(_cards, CardsAcceptClick);

            // 메모장은 _cards에 없다. 고르는 대상이 아니라 차례가 따로 들고 있다.
            _gate.SetNote(noteStep.Note, noteStep.AcceptsClick);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning($"{name}: 씬에 보상 상자가 둘 이상입니다. 마지막 것이 쓰입니다.", this);

            Instance = this;

            if (lid == null) lid = GetComponent<LSO_BoxOpenEffect>();

            if (lid == null)
                Debug.LogError($"{name}: LSO_BoxOpenEffect가 없어 뚜껑을 열 수 없습니다.", this);

            if (cardAnchor == null) cardAnchor = transform;

            // 메모장 자리를 안 꽂았으면 카드와 같은 자리를 쓴다.
            // 풀이 이 값을 부모로 잡으므로 풀을 만들기 전에 정해져 있어야 한다.
            if (noteAnchor == null) noteAnchor = cardAnchor;

            if (clickHandler == null) clickHandler = GetComponent<LSO_ButtonClickHandler>();

            LSO_RewardClickGate.WarnIfShared(this);

            if (clickHandler == null)
            {
                Debug.LogWarning(
                    $"{name}: LSO_ButtonClickHandler가 없어 클릭을 막을 수 없습니다. " +
                    "연출 중에도 커서가 '누를 수 있음'으로 보입니다.", this);
            }

            if (pieceCardPrefab == null && willNotePrefab == null)
            {
                Debug.LogError($"{name}: 카드 원본이 하나도 없어 카드를 만들 수 없습니다.", this);
                return;
            }

            _pool = new LSO_RewardCardPool(
                pieceCardPrefab, cardAnchor, cardInsideAnchor,
                willNotePrefab, noteAnchor, noteInsideAnchor);

            _gate = new LSO_RewardClickGate(clickHandler);

            noteStep.Bind(_pool, noteAnchor);
            noteStep.Shown += _ => onNoteShown?.Invoke(_chosenOption);
            noteStep.Unlocked += _ => onWillUnlocked?.Invoke(_chosenOption);
        }

        private void OnEnable()
        {
            if (lid != null) lid.OnOpened += HandleLidOpened;
        }

        private void OnDisable()
        {
            if (lid != null) lid.OnOpened -= HandleLidOpened;
        }

        private void OnDestroy()
        {
            // 자기가 Instance일 때만 지운다. 중복 상자가 사라질 때 지우면
            // 살아 있는 쪽까지 날아간다.
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// 보상을 시작한다. 스테이지를 클리어한 쪽이 부른다.
        ///
        /// 상자를 누를 수 있게만 해둔다. 여는 것은 플레이어의 첫 클릭이다.
        /// 진행 중에 다시 부르면 하던 것을 버리고 처음부터 다시 한다.
        /// </summary>
        public void Begin(int chapter, int stage)
        {
            if (_pool == null || !_pool.HasAny)
            {
                Debug.LogError($"{name}: 카드 풀이 없어 보상을 시작할 수 없습니다.", this);
                return;
            }

            _chapter = chapter;
            _stage = stage;

            ReleaseAll();

            SetPhase(Phase.Closed);

            onReady?.Invoke(null);

            // 여기서 열지 않는다. 닫힌 채로 기다렸다가 플레이어가 눌러야 열린다.
            // On Ready에 커서 바꾸기를 걸어 "누를 수 있다"를 알려줄 것.
        }

        /// <summary>
        /// 상자를 눌렀다.
        ///
        /// 여는 것과 카드를 꺼내는 것은 클릭을 받지 않는다. 스테이지가 끝나면 상자가
        /// 알아서 열리고 카드까지 나온다. 플레이어가 할 일은 고르는 것부터다.
        ///
        /// 상자 클릭이 필요한 자리는 유언 메모장 앞뒤 두 번뿐이다.
        /// </summary>
        public void OnClick()
        {
            switch (_phase)
            {
                // 첫 클릭. 뚜껑을 연다.
                case Phase.Closed:
                    OpenLid();
                    break;

                // 뚜껑이 열린 뒤의 클릭. 상자 안에서 카드가 나온다.
                case Phase.Opened:
                    StartCoroutine(DealRoutine());
                    break;

                // 처음 보는 유언을 받았다. 메모장을 꺼낸다.
                case Phase.NoteWaiting:
                    StartCoroutine(NoteRoutine());
                    break;

                // 나머지는 클릭을 버린다. 큐에 쌓지 않는다 —
                // 쌓아두면 손을 뗀 뒤에도 상자가 혼자 진행한다.
            }
        }

        private void OpenLid()
        {
            if (lid == null) return;

            SetPhase(Phase.Opening);

            lid.Open();

            onOpening?.Invoke(null);
        }

        /// <summary>
        /// 뚜껑이 다 열렸다.
        ///
        /// 상자가 lid.IsOpened를 묻지 않고 이 신호만 듣는 것이 중요하다.
        /// 양쪽에 상태를 두고 서로 확인하기 시작하면, 어긋났을 때 어느 쪽이 맞는지 정할 수 없다.
        /// </summary>
        private void HandleLidOpened()
        {
            if (_phase != Phase.Opening) return;

            SetPhase(Phase.Opened);

            // 여기서 멈춘다. 카드는 한 번 더 눌러야 나온다.
            // 상자 안이 보인 뒤에 뭔가가 올라와야 "상자에서 나왔다"로 읽힌다.
            onOpened?.Invoke(null);
        }

        private IEnumerator DealRoutine()
        {
            SetPhase(Phase.Dealing);

            List<LSO_RewardOption> options = _draft.Draw(table, _chapter, _stage);

            if (options.Count == 0)
            {
                Debug.LogWarning($"{name}: 뽑힌 보상이 없어 그대로 끝냅니다.", this);

                yield return StartCoroutine(CloseRoutine());
                yield break;
            }

            for (int i = 0; i < options.Count; i++)
            {
                SpawnCard(options[i], i, options.Count);

                if (dealInterval > 0f)
                    yield return new WaitForSeconds(dealInterval);
            }

            SetPhase(Phase.Selecting);

            onDealt?.Invoke(null);
        }

        /// <summary>
        /// 카드 한 장을 상자 안에서 띄운다. 자리를 정하는 곳은 여기뿐이다.
        ///
        /// 가운데를 기준으로 좌우 대칭이 되도록 민다.
        /// 세 장이면 -1, 0, +1 칸이다.
        /// </summary>
        private void SpawnCard(LSO_RewardOption option, int index, int total)
        {
            // 상자에서 나오는 세 장은 전부 기물 카드다. 유언은 고르는 대상이 아니라
            // 고른 뒤에 메모장으로 보여줄 것이라 여기로 오지 않는다.
            if (option.type != LSO_RewardType.Piece)
            {
                Debug.LogWarning(
                    $"{name}: {option.type} 보상은 카드로 나오지 않습니다. " +
                    "보상 테이블의 카드 후보에 Unlock Will 로 넣으세요.", this);
                return;
            }

            LSO_RewardCard card = _pool.TakePiece();

            if (card == null)
            {
                Debug.LogError($"{name}: Piece Card Prefab이 없어 기물 카드를 만들지 못했습니다.", this);
                return;
            }

            card.transform.SetParent(cardAnchor, false);

            // 상자 안에서 시작한다. 켜기 전에 자리를 잡아야 한 프레임 튀지 않는다.
            // 좌우로는 미리 벌려둔다 — 세 장이 한 점에서 겹쳐 나오면 뭉쳐 보인다.
            card.transform.localPosition = _pool.CardInsideLocal + layout.Spread(index, total);
            card.transform.localRotation = layout.Tilt(index, total);

            card.Bind(option, HandleCardClicked);

            card.transform
                .DOLocalMove(layout.Position(index, total), riseDuration)
                .SetEase(riseEase)
                .SetLink(card.gameObject);

            _cards.Add(card);
        }

        /// <summary>
        /// 꺼내둔 카드를 지금 설정대로 다시 늘어놓는다.
        ///
        /// 간격을 인스펙터에서 만지는 동안 결과를 바로 보기 위한 것이다.
        /// 트윈 없이 즉시 옮긴다 — 값을 조금씩 바꿔볼 때 연출이 끼면 오히려 보기 어렵다.
        /// </summary>
        private void Relayout()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                LSO_RewardCard card = _cards[i];
                if (card == null) continue;

                card.transform.DOKill();

                card.transform.localPosition = layout.Position(i, _cards.Count);
                card.transform.localRotation = layout.Tilt(i, _cards.Count);
            }
        }

        /// <summary>
        /// 카드를 눌렀다. 한 번 클릭으로 그 자리에서 확정된다.
        ///
        /// 되돌릴 방법이 없으므로 Selecting이 아닐 때는 절대 받지 않는다.
        /// 카드 쪽에서도 콜백을 한 번 쓰면 비우므로 연타가 두 번 들어오지 않는다.
        /// </summary>
        private void HandleCardClicked(LSO_RewardCard card)
        {
            if (_phase != Phase.Selecting) return;
            if (card == null || card.Option == null) return;

            SetPhase(Phase.Lowering);

            StartCoroutine(ClaimRoutine(card));
        }

        /// <summary>
        /// 고른 것을 지급하고 나머지 카드를 내린다.
        ///
        /// 처음 보는 유언이었으면 여기서 멈추고 메모장을 꺼낼 클릭을 기다린다.
        /// 아니면 그대로 정리한다.
        /// </summary>
        private IEnumerator ClaimRoutine(LSO_RewardCard chosen)
        {
            _chosenOption = chosen.Option;

            // 지급 전에 확인한다. 지급하고 나면 해금 목록에 들어가
            // "처음 보는 유언"인지 알 수 없게 된다.
            DLJ_WillDataSO note = noteStep.Resolve(_chosenOption);

            // 처음 보는 유언은 메모장을 보여준 뒤에 푼다. 여기서 같이 풀어버리면
            // 종이가 올라오기도 전에 해금이 끝나 순서가 뒤집힌다.
            // 이미 가진 유언은 종이가 안 나오므로 지금 함께 푼다.
            Claim(_chosenOption, includeAttachedWill: note == null);

            // 누른 즉시 움직여 "골랐다"를 알린다. 여기서 아무것도 안 하면
            // Pick Hold 동안 화면이 멈춰 보여서 클릭이 씹힌 것처럼 느껴진다.
            yield return StartCoroutine(LiftChosenRoutine(chosen));

            // 떠오른 채로 한 박자 머문다.
            if (pickHold > 0f)
                yield return new WaitForSeconds(pickHold);

            // 나머지가 상자로 돌아가는 것과 고른 카드가 덱으로 가는 것을 함께 돌린다.
            // 순서대로 하면 "치우고 나서야 받는" 것처럼 보여 한 박자 늘어진다.
            Coroutine toDeck = StartCoroutine(SendToDeckRoutine(chosen));

            yield return StartCoroutine(LowerRoutine(chosen));
            yield return toDeck;

            if (claimHold > 0f)
                yield return new WaitForSeconds(claimHold);

            if (note != null)
            {
                SetPhase(Phase.NoteWaiting);

                onNoteReady?.Invoke(_chosenOption);
                yield break;
            }

            yield return StartCoroutine(CloseRoutine());
        }

        /// <summary>
        /// 고른 카드를 제자리에서 한 번 밀어낸다. 클릭에 대한 즉시 반응이다.
        ///
        /// 트윈이 끝날 때까지 기다린다. 기다리지 않고 넘어가면 밀려나는 도중에
        /// 덱으로 가는 트윈이 시작돼 두 움직임이 겹친다.
        /// </summary>
        private IEnumerator LiftChosenRoutine(LSO_RewardCard chosen)
        {
            if (chosen == null) yield break;
            if (pickLift == Vector3.zero || pickLiftDuration <= 0f) yield break;

            Transform card = chosen.transform;

            card.DOKill();

            yield return card
                .DOLocalMove(card.localPosition + pickLift, pickLiftDuration)
                .SetEase(pickLiftEase)
                .SetLink(chosen.gameObject)
                .WaitForCompletion();
        }

        /// <summary>
        /// 고른 카드를 덱 쪽으로 날려 보낸다.
        ///
        /// 덱에 실제로 넣는 것은 이 연출이 아니다. 지급은 이미 끝났고(LSO_RewardClaim →
        /// LSO_ItemLibraryManager), 여기서는 "어디로 갔는지" 만 보여준다.
        /// 둘을 묶으면 연출이 끊겼을 때 카드가 사라지거나 두 번 들어간다.
        ///
        /// 덱 자리를 안 꽂았으면 상자 안으로 들어간다. 받은 것은 마찬가지지만
        /// 어디로 갔는지 보이지 않으므로 한 번 짚어준다.
        /// </summary>
        private IEnumerator SendToDeckRoutine(LSO_RewardCard chosen)
        {
            if (chosen == null) yield break;

            Transform card = chosen.transform;

            card.DOKill();

            if (deckAnchor == null)
            {
                Debug.LogWarning(
                    $"{name}: Deck Anchor가 비어 있어 고른 카드가 상자 안으로 들어갑니다. " +
                    "덱으로 가는 것을 보여주려면 손패나 덱 더미를 꽂으세요.", this);

                yield return card
                    .DOLocalMove(_pool.CardInsideLocal, toDeckDuration)
                    .SetEase(toDeckEase)
                    .SetLink(chosen.gameObject)
                    .WaitForCompletion();

                yield break;
            }

            // 월드 좌표로 움직인다. 덱은 상자의 자식이 아니라 화면 아래 다른 곳에 있다.
            Sequence flight = DOTween.Sequence()
                .Append(card.DOMove(deckAnchor.position, toDeckDuration).SetEase(toDeckEase))
                .Join(card.DOScale(card.localScale * toDeckScale, toDeckDuration).SetEase(toDeckEase))
                .SetLink(chosen.gameObject);

            yield return flight.WaitForCompletion();
        }

        private void Claim(LSO_RewardOption option, bool includeAttachedWill)
        {
            if (option == null) return;

            LSO_ItemLibraryManager library = LSO_ItemLibraryManager.Instance;

            if (library != null && library.Claim != null)
                library.Claim.Claim(option, includeAttachedWill);
            else
                Debug.LogWarning($"{name}: LSO_ItemLibraryManager가 없어 보상을 지급하지 못했습니다.", this);

            onClaimed?.Invoke(option);
        }

        /// <summary>
        /// 고르지 않은 카드를 상자 안으로 도로 집어넣는다. 고른 카드는 그대로 둔다.
        ///
        /// 제자리에서 내려가는 것이 아니라 상자 입구(기준 자리의 원점)로 모인다.
        /// 벌어져 있던 자리로만 내리면 상자 옆 허공으로 가라앉는 것처럼 보인다.
        ///
        /// 내려간 카드는 도착한 뒤에 풀로 돌려보낸다. 먼저 돌려보내면
        /// 꺼지면서 사라져 들어가는 것이 보이지 않는다.
        /// </summary>
        private IEnumerator LowerRoutine(LSO_RewardCard chosen)
        {
            var lowering = new List<LSO_RewardCard>();

            foreach (LSO_RewardCard card in _cards)
            {
                if (card == null || card == chosen) continue;

                card.transform.DOKill();

                card.transform
                    .DOLocalMove(_pool.CardInsideLocal, riseDuration)
                    .SetEase(riseEase)
                    .SetLink(card.gameObject);

                lowering.Add(card);
            }

            if (lowering.Count == 0) yield break;

            yield return new WaitForSeconds(riseDuration);

            foreach (LSO_RewardCard card in lowering)
            {
                _cards.Remove(card);
                _pool.Return(card);
            }
        }

        /// <summary>
        /// 메모장 차례. 올라오고, 눌리기를 기다리고, 들어가고, 유언이 풀린다.
        /// 그 안의 순서는 LSO_RewardNoteStep이 안다.
        /// </summary>
        private IEnumerator NoteRoutine()
        {
            SetPhase(Phase.NotePlaying);

            yield return StartCoroutine(noteStep.Run());

            yield return StartCoroutine(CloseRoutine());
        }

        private IEnumerator CloseRoutine()
        {
            SetPhase(Phase.Closing);

            // 연출이 중간에 끊겨 여기로 바로 왔을 수도 있다.
            // 메모장이 남아 있으면 치우고, 못 푼 유언이 있으면 여기서 푼다.
            noteStep.Finish();

            ReleaseAll();

            if (lid != null) lid.Close();

            LSO_RewardOption option = _chosenOption;

            _chosenOption = null;
            SetPhase(Phase.Idle);

            onFinished?.Invoke(option);
            OnFinished?.Invoke(option);

            yield break;
        }

        /// <summary>꺼내 쓴 카드를 전부 돌려준다. 트윈이 돌던 중이어도 끊는다.</summary>
        private void ReleaseAll()
        {
            foreach (LSO_RewardCard card in _cards)
            {
                if (card == null) continue;

                card.transform.DOKill();

                _pool.Return(card);
            }

            _cards.Clear();
        }
    }
}
