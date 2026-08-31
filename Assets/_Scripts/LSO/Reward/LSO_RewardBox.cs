using System.Collections;
using System.Collections.Generic;
using _Scripts.LSO.CoreLib;
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
    /// 씬 배선: Collider + LSO_ButtonClickHandler 와 함께 붙일 것.
    /// 3D 물건이므로 씬에 EventSystem, 카메라에 Physics Raycaster가 있어야 한다.
    /// </summary>
    [RequireComponent(typeof(LSO_ButtonClickHandler))]
    public class LSO_RewardBox : MonoBehaviour, LSO_IClickEffect
    {
        /// <summary>보상이 어디까지 진행됐는지. 클릭의 뜻을 정하는 것이 이 값이다.</summary>
        private enum Phase
        {
            /// <summary>아직 시작하지 않았다. 클릭해도 아무 일이 없다.</summary>
            Idle,

            /// <summary>
            /// 닫힌 상자.
            ///
            /// Open On Begin이 켜져 있으면 Begin이 곧바로 여므로 여기 머물지 않는다.
            /// 꺼져 있을 때만 여기서 클릭을 기다린다.
            /// </summary>
            Closed,

            /// <summary>뚜껑이 도는 중. 클릭을 버린다.</summary>
            Opening,

            /// <summary>
            /// 뚜껑이 열린 채로 카드를 꺼낼 클릭을 기다린다.
            ///
            /// Deal On Open을 켜면 여기 머물지 않고 곧바로 Dealing으로 넘어간다.
            /// </summary>
            Opened,

            /// <summary>카드가 하나씩 나오는 중. 클릭을 버린다.</summary>
            Dealing,

            /// <summary>고르기를 기다린다. 카드를 누르면 그 자리에서 확정된다.</summary>
            Selecting,

            /// <summary>고른 뒤 나머지 카드가 내려가는 중. 클릭을 버린다.</summary>
            Lowering,

            /// <summary>처음 보는 유언이라 도장이 준비됐다. 상자를 누르면 나온다.</summary>
            StampWaiting,

            /// <summary>도장이 올라오는 중. 클릭을 버린다.</summary>
            StampRising,

            /// <summary>도장을 다 보기를 기다린다. 상자를 누르면 정리한다.</summary>
            StampShown,

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

        [Tooltip("유언에 쓸 도장 원본. 고르는 것과 보여주는 것이 같아서 하나만 꽂는다.\n" +
                 "비워두면 유언 후보가 나와도 만들지 못한다.")]
        [SerializeField] private LSO_WillStamp willStampPrefab;

        [Tooltip("카드가 늘어설 기준 자리. 비워두면 상자 자신을 쓴다.")]
        [SerializeField] private Transform cardAnchor;


        [Header("배치")]
        [Tooltip("카드 한 칸의 간격과 방향. 기준 자리(Card Anchor)의 로컬 축이다.\n" +
                 "\n" +
                 "(0.6, 0, 0)      가로로 나란히\n" +
                 "(0.6, 0.1, 0)    가로로 가면서 조금씩 올라감 (계단)\n" +
                 "(0.5, 0, 0.2)    부채꼴처럼 앞뒤로도 벌어짐\n" +
                 "\n" +
                 "가운데를 기준으로 좌우 대칭이 된다. 세 장이면 -1, 0, +1 칸이다.")]
        [SerializeField] private Vector3 cardSpacing = new Vector3(0.6f, 0f, 0f);

        [Tooltip("상자 안에서 솟아오르는 높이. Card Spacing과 별개로 더해진다.")]
        [SerializeField] private float riseHeight = 0.8f;

        [Tooltip("카드마다 더 기울일 각도. 가운데 카드는 0이고 바깥으로 갈수록 커진다.\n" +
                 "(0, 0, 5) 를 넣으면 부채꼴처럼 좌우로 기울어진다.")]
        [SerializeField] private Vector3 cardTilt;

        [Tooltip("켜면 Begin과 동시에 뚜껑이 스스로 열린다. 여는 클릭이 없다.\n" +
                 "끄면 닫힌 채로 기다렸다가 눌러야 열린다.")]
        [SerializeField] private bool openOnBegin = true;

        [Tooltip("켜면 뚜껑이 다 열리는 즉시 카드가 나온다.\n" +
                 "끄면 열린 채로 기다렸다가 한 번 눌러야 카드가 나온다.")]
        [SerializeField] private bool dealOnOpen;

        [Header("연출")]
        [Tooltip("카드 한 장이 솟는 데 걸리는 시간.")]
        [SerializeField, Min(0f)] private float riseDuration = 0.35f;

        [Tooltip("다음 카드가 나오기까지의 간격.")]
        [SerializeField, Min(0f)] private float dealInterval = 0.12f;

        [SerializeField] private Ease riseEase = Ease.OutBack;

        [Tooltip("고른 뒤 정리를 시작하기까지 두는 시간. 무엇을 얻었는지 볼 틈을 준다.")]
        [SerializeField, Min(0f)] private float claimHold = 0.6f;

        [Header("유언 도장")]
        [Tooltip("켜면 처음 보는 유언일 때만 도장이 나온다. 두 번째부터는 그냥 닫힌다.\n" +
                 "\n" +
                 "끄면 받을 때마다 나온다. 같은 유언을 여러 번 받는 것이 흔하다면 이쪽이 낫다 —\n" +
                 "재고에는 쌓이는데 화면에는 아무 반응이 없으면 받은 줄 모른다.")]
        [SerializeField] private bool stampOnlyWhenNew = true;

        [Tooltip("도장이 올라와 멈출 자리. 기준 자리(Card Anchor)의 로컬 좌표다.")]
        [SerializeField] private Vector3 stampPosition = new Vector3(0f, 0.9f, -0.3f);

        [Tooltip("도장이 올라오는 데 걸리는 시간.")]
        [SerializeField, Min(0f)] private float stampRiseDuration = 0.4f;

        [SerializeField] private Ease stampRiseEase = Ease.OutBack;

        [Header("반응")]
        [Tooltip("보상이 시작돼 상자를 누를 수 있게 됐을 때. 커서 모양 바꾸기 등을 건다.")]
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

        [Tooltip("처음 보는 유언이라 도장이 준비됐을 때. 커서를 바꾸는 자리다.\n" +
                 "이때 상자를 누르면 도장이 나온다.")]
        [SerializeField] private LSO_RewardEvent onStampReady;

        [Tooltip("도장이 다 올라왔을 때.")]
        [SerializeField] private LSO_RewardEvent onStampShown;

        [Tooltip("정리까지 끝났을 때. 다음 연출(체스판 뒤집기 등)을 여기 건다.")]
        [SerializeField] private LSO_RewardEvent onFinished;

        private readonly LSO_RewardDraft _draft = new();
        private readonly List<LSO_RewardCard> _cards = new();

        // 종류마다 풀을 따로 둔다. 하나로 묶으면 꺼낼 때마다 기물인지 유언인지 확인해야 하고,
        // 잘못 꺼낸 카드가 조용히 빈 채로 나온다.
        private LSO_ObjectPool<LSO_RewardPieceCard> _piecePool;
        private LSO_ObjectPool<LSO_WillStamp> _willPool;

        // 고른 뒤 무엇을 얻었는지 보여주려고 올려둔 도장. 고르는 것과 같은 풀에서 나온다.
        private LSO_WillStamp _stamp;

        private Phase _phase = Phase.Idle;
        private int _chapter;
        private int _stage;

        // 유언 도장을 거치는 동안 들고 있어야 하는 것들.
        // 클릭 두 번에 걸쳐 진행되므로 코루틴 지역 변수로는 이어지지 않는다.
        private LSO_RewardOption _chosenOption;
        private DLJ_WillDataSO _pendingWill;

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
            || _phase == Phase.StampRising
            || _phase == Phase.Closing;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning($"{name}: 씬에 보상 상자가 둘 이상입니다. 마지막 것이 쓰입니다.", this);

            Instance = this;

            if (lid == null) lid = GetComponent<LSO_BoxOpenEffect>();

            if (lid == null)
                Debug.LogError($"{name}: LSO_BoxOpenEffect가 없어 뚜껑을 열 수 없습니다.", this);

            if (cardAnchor == null) cardAnchor = transform;

            if (pieceCardPrefab == null && willStampPrefab == null)
            {
                Debug.LogError($"{name}: 카드 원본이 하나도 없어 카드를 만들 수 없습니다.", this);
                return;
            }

            // 후보는 보통 셋이다. 미리 만들어 두면 첫 스테이지에서 끊기지 않는다.
            // 어느 쪽이 몇 장 나올지는 뽑기 결과에 달렸으므로 둘 다 넉넉히 잡는다.
            if (pieceCardPrefab != null)
                _piecePool = new LSO_ObjectPool<LSO_RewardPieceCard>(pieceCardPrefab, cardAnchor, prewarm: 3);

            // 세 개가 전부 유언일 수도 있고, 그 위에 보여줄 도장이 하나 더 필요하다.
            if (willStampPrefab != null)
                _willPool = new LSO_ObjectPool<LSO_WillStamp>(willStampPrefab, cardAnchor, prewarm: 4);
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
        /// Open On Begin이 켜져 있으면 여기서 뚜껑까지 연다.
        /// 진행 중에 다시 부르면 하던 것을 버리고 처음부터 다시 한다.
        /// </summary>
        public void Begin(int chapter, int stage)
        {
            if (_piecePool == null && _willPool == null)
            {
                Debug.LogError($"{name}: 카드 풀이 없어 보상을 시작할 수 없습니다.", this);
                return;
            }

            _chapter = chapter;
            _stage = stage;

            ReleaseAll();

            _phase = Phase.Closed;

            onReady?.Invoke(null);

            // 여는 것은 플레이어가 할 일이 아니다. 스테이지가 끝나면 상자가 알아서 열린다.
            // 플레이어의 클릭은 카드를 꺼내는 것부터 시작한다.
            if (openOnBegin)
                OpenLid();
        }

        public void OnClick()
        {
            switch (_phase)
            {
                // Open On Begin이 꺼져 있을 때만 여기까지 온다.
                case Phase.Closed:
                    OpenLid();
                    break;

                // 첫 클릭이 받아지는 자리. 카드를 꺼낸다.
                case Phase.Opened:
                    StartCoroutine(DealRoutine());
                    break;

                // 처음 보는 유언을 받았다. 도장을 꺼낸다.
                case Phase.StampWaiting:
                    StartCoroutine(ShowStampRoutine());
                    break;

                // 다 봤다는 뜻으로 친다.
                case Phase.StampShown:
                    StartCoroutine(CloseRoutine());
                    break;

                // 나머지는 클릭을 버린다. 큐에 쌓지 않는다 —
                // 쌓아두면 손을 뗀 뒤에도 상자가 혼자 진행한다.
                default:
                    break;
            }
        }

        private void OpenLid()
        {
            if (lid == null) return;

            _phase = Phase.Opening;

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

            _phase = Phase.Opened;

            onOpened?.Invoke(null);

            // 이 신호를 받고 곧바로 이어간다. 클릭을 한 번 더 기다리지 않는다.
            // On Opened는 위에서 이미 발행했으므로, 거기 건 연출은 카드와 겹쳐 재생된다.
            if (dealOnOpen)
                StartCoroutine(DealRoutine());
        }

        private IEnumerator DealRoutine()
        {
            _phase = Phase.Dealing;

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

            _phase = Phase.Selecting;

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
            LSO_RewardCard card = Take(option);

            if (card == null) return;

            card.transform.SetParent(cardAnchor, false);

            // 상자 안에서 시작한다. 켜기 전에 자리를 잡아야 한 프레임 튀지 않는다.
            card.transform.localPosition = SpreadOf(index, total);
            card.transform.localRotation = TiltOf(index, total);

            card.Bind(option, HandleCardClicked);

            card.transform
                .DOLocalMove(PositionOf(index, total), riseDuration)
                .SetEase(riseEase)
                .SetLink(card.gameObject);

            _cards.Add(card);
        }

        /// <summary>
        /// 보상 종류에 맞는 카드를 꺼낸다. 그 종류의 원본이 없으면 null.
        ///
        /// 조용히 넘기지 않는다. 유언 후보를 테이블에 넣어두고 원본을 안 꽂으면
        /// 카드가 두 장만 나오는 것으로 보이는데, 원인이 화면에 드러나지 않는다.
        /// </summary>
        private LSO_RewardCard Take(LSO_RewardOption option)
        {
            if (option.type == LSO_RewardType.Will)
            {
                if (_willPool != null) return _willPool.Get();

                Debug.LogError($"{name}: Will Stamp Prefab이 없어 유언 도장을 만들지 못했습니다.", this);
                return null;
            }

            if (_piecePool != null) return _piecePool.Get();

            Debug.LogError($"{name}: Piece Card Prefab이 없어 기물 카드를 만들지 못했습니다.", this);
            return null;
        }

        /// <summary>
        /// 다 쓴 카드를 제 풀로 돌려보낸다.
        ///
        /// 어느 풀에서 왔는지는 카드의 실제 타입으로 정한다.
        /// 카드에 출처를 적어두는 방법도 있지만, 그러면 상태가 하나 늘고
        /// 그 값이 실제와 어긋날 수 있는 자리가 생긴다.
        /// </summary>
        private void Return(LSO_RewardCard card)
        {
            switch (card)
            {
                case LSO_WillStamp stamp when _willPool != null:
                    _willPool.Release(stamp);
                    break;

                case LSO_RewardPieceCard piece when _piecePool != null:
                    _piecePool.Release(piece);
                    break;

                default:
                    Debug.LogWarning($"{card.name}: 돌려보낼 풀을 찾지 못해 그대로 껐습니다.", card);
                    card.gameObject.SetActive(false);
                    break;
            }
        }

        /// <summary>
        /// 가운데를 기준으로 몇 칸 밀린 자리인지. 솟는 높이는 빼고 좌우 배치만이다.
        ///
        /// 세 장이면 -1, 0, +1 칸이 된다. 짝수여도 가운데가 비어 대칭이 유지된다.
        /// </summary>
        private Vector3 SpreadOf(int index, int total)
        {
            return cardSpacing * Step(index, total);
        }

        /// <summary>카드가 최종적으로 놓일 자리.</summary>
        private Vector3 PositionOf(int index, int total)
        {
            return SpreadOf(index, total) + Vector3.up * riseHeight;
        }

        /// <summary>가운데에서 멀수록 더 기울인다. Card Tilt가 0이면 전부 똑바로 선다.</summary>
        private Quaternion TiltOf(int index, int total)
        {
            if (cardTilt == Vector3.zero) return Quaternion.identity;

            return Quaternion.Euler(cardTilt * Step(index, total));
        }

        /// <summary>가운데를 0으로 놓았을 때 이 카드가 몇 칸째인지. 왼쪽은 음수다.</summary>
        private static float Step(int index, int total)
        {
            return index - (total - 1) * 0.5f;
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

                card.transform.localPosition = PositionOf(i, _cards.Count);
                card.transform.localRotation = TiltOf(i, _cards.Count);
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

            _phase = Phase.Lowering;

            StartCoroutine(ClaimRoutine(card));
        }

        /// <summary>
        /// 고른 것을 지급하고 나머지 카드를 내린다.
        ///
        /// 처음 보는 유언이었으면 여기서 멈추고 도장을 꺼낼 클릭을 기다린다.
        /// 아니면 그대로 정리한다.
        /// </summary>
        private IEnumerator ClaimRoutine(LSO_RewardCard chosen)
        {
            _chosenOption = chosen.Option;

            // 지급 전에 확인한다. 지급하고 나면 해금 목록에 들어가
            // "처음 보는 유언"인지 알 수 없게 된다.
            _pendingWill = FindStampWill(_chosenOption);

            Claim(_chosenOption);

            yield return StartCoroutine(LowerRoutine(chosen));

            if (claimHold > 0f)
                yield return new WaitForSeconds(claimHold);

            if (_pendingWill != null && _willPool != null)
            {
                _phase = Phase.StampWaiting;

                onStampReady?.Invoke(_chosenOption);
                yield break;
            }

            yield return StartCoroutine(CloseRoutine());
        }

        /// <summary>
        /// 도장으로 보여줄 유언. 없으면 null.
        ///
        /// 기물 카드는 여기서 걸러진다. 카드가 들고 있는 것은 LSO_WillType 이라
        /// 도장을 고를 데이터 에셋이 없다. 유언 보상만 도장을 낸다.
        ///
        /// 반드시 지급 전에 부를 것. 지급하고 나면 해금 목록에 들어가
        /// 처음 보는 것인지 알 수 없게 된다.
        /// </summary>
        private DLJ_WillDataSO FindStampWill(LSO_RewardOption option)
        {
            if (option == null || option.type != LSO_RewardType.Will) return null;
            if (option.will == null) return null;

            if (!stampOnlyWhenNew) return option.will;

            LSO_ItemLibraryManager library = LSO_ItemLibraryManager.Instance;

            // 해금 목록을 못 보면 처음인지 알 수 없다. 그럴 때는 보여준다 —
            // 한 번 더 보는 것이 못 보고 넘어가는 것보다 낫다.
            if (library == null || library.Claim == null) return option.will;

            return library.Claim.Unlocks.IsWillUnlocked(option.will) ? null : option.will;
        }

        private void Claim(LSO_RewardOption option)
        {
            if (option == null) return;

            LSO_ItemLibraryManager library = LSO_ItemLibraryManager.Instance;

            if (library != null && library.Claim != null)
                library.Claim.Claim(option);
            else
                Debug.LogWarning($"{name}: LSO_ItemLibraryManager가 없어 보상을 지급하지 못했습니다.", this);

            onClaimed?.Invoke(option);
        }

        /// <summary>
        /// 고르지 않은 카드를 상자 안으로 내린다. 고른 카드는 그대로 둔다.
        ///
        /// 내려간 카드는 도착한 뒤에 풀로 돌려보낸다. 먼저 돌려보내면
        /// 꺼지면서 사라져 내려가는 것이 보이지 않는다.
        /// </summary>
        private IEnumerator LowerRoutine(LSO_RewardCard chosen)
        {
            var lowering = new List<LSO_RewardCard>();

            for (int i = 0; i < _cards.Count; i++)
            {
                LSO_RewardCard card = _cards[i];

                if (card == null || card == chosen) continue;

                card.transform.DOKill();

                card.transform
                    .DOLocalMove(SpreadOf(i, _cards.Count), riseDuration)
                    .SetEase(riseEase)
                    .SetLink(card.gameObject);

                lowering.Add(card);
            }

            if (lowering.Count == 0) yield break;

            yield return new WaitForSeconds(riseDuration);

            foreach (LSO_RewardCard card in lowering)
            {
                _cards.Remove(card);
                Return(card);
            }
        }

        /// <summary>
        /// 도장을 상자에서 꺼내 올린다.
        ///
        /// 고르는 것과 같은 풀에서 꺼낸다. 둘이 같은 물건이라 원본도 하나뿐이다.
        /// 클릭 콜백은 붙이지 않는다 — 이미 고른 뒤라 다시 고를 것이 없다.
        /// </summary>
        private IEnumerator ShowStampRoutine()
        {
            _phase = Phase.StampRising;

            _stamp = _willPool.Get();

            _stamp.transform.SetParent(cardAnchor, false);
            _stamp.transform.localPosition = Vector3.zero;
            _stamp.transform.localRotation = Quaternion.identity;

            _stamp.Bind(_pendingWill);

            Tween rise = _stamp.transform
                .DOLocalMove(stampPosition, stampRiseDuration)
                .SetEase(stampRiseEase)
                .SetLink(_stamp.gameObject);

            yield return rise.WaitForCompletion();

            _phase = Phase.StampShown;

            onStampShown?.Invoke(_chosenOption);
        }

        private IEnumerator CloseRoutine()
        {
            _phase = Phase.Closing;

            if (_stamp != null)
            {
                _stamp.transform.DOKill();
                _willPool.Release(_stamp);
                _stamp = null;
            }

            ReleaseAll();

            if (lid != null) lid.Close();

            LSO_RewardOption option = _chosenOption;

            _chosenOption = null;
            _pendingWill = null;
            _phase = Phase.Idle;

            onFinished?.Invoke(option);
            OnFinished?.Invoke(option);

            yield break;
        }

#if UNITY_EDITOR

        #region 테스트용

        [Header("테스트용")]
        [Tooltip("컨텍스트 메뉴로 Begin을 부를 때 쓸 챕터·스테이지. 빌드에는 들어가지 않는다.")]
        [SerializeField] private int testChapter = 1;

        [SerializeField] private int testStage = 1;

        [Tooltip("켜면 플레이를 누르는 순간 스스로 시작한다.\n" +
                 "맵도 전투도 거치지 않으므로 상자 연출만 볼 때 쓴다.")]
        [SerializeField] private bool testAutoBegin;

        [Tooltip("Auto Begin이 켜졌을 때 어디까지 스스로 진행할지.\n" +
                 "\n" +
                 "Ready   누를 준비만 한다. 뚜껑부터 직접 눌러본다\n" +
                 "Opened  뚜껑까지 열어둔다 (Deal On Open이 꺼져 있을 때만 의미가 있다)\n" +
                 "Dealt   카드까지 꺼내둔다. 고르는 것만 해본다")]
        [SerializeField] private TestAutoStep testAutoStep = TestAutoStep.Ready;

        private enum TestAutoStep
        {
            Ready,
            Opened,
            Dealt
        }

        private void Start()
        {
            if (!testAutoBegin) return;

            StartCoroutine(Co_TestAuto());
        }

        /// <summary>
        /// 클릭 없이 정해둔 단계까지 밀어준다.
        ///
        /// OnClick을 그대로 부르지 않고 내부 함수를 직접 쓴다.
        /// OnClick은 단계를 보고 갈라지는데, 여기서는 어느 단계를 거칠지 이미 정해져 있다.
        /// </summary>
        private IEnumerator Co_TestAuto()
        {
            // 한 프레임 기다린다. 다른 컴포넌트의 Start가 끝나야
            // LSO_ItemLibraryManager 같은 것들이 자리를 잡는다.
            yield return null;

            TestBegin();

            if (testAutoStep == TestAutoStep.Ready) yield break;

            OpenLid();

            while (_phase == Phase.Opening)
                yield return null;

            if (testAutoStep == TestAutoStep.Opened) yield break;

            yield return StartCoroutine(DealRoutine());
        }

        /// <summary>
        /// 맵을 거치지 않고 보상을 시작한다. 컴포넌트 톱니바퀴에서 부른다.
        ///
        /// 플레이 중에만 쓸 것. 정지 상태에서는 풀이 아직 없어 아무 일도 일어나지 않는다.
        /// </summary>
        [ContextMenu("테스트: 보상 시작")]
        private void TestBegin()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"{name}: 플레이 중에만 됩니다.", this);
                return;
            }

            Debug.Log($"{name}: 테스트 시작 (챕터 {testChapter} 스테이지 {testStage})", this);

            Begin(testChapter, testStage);
        }

        /// <summary>클릭 대신 뚜껑을 연다. 아직 시작하지 않았으면 시작부터 한다.</summary>
        [ContextMenu("테스트: 뚜껑 열기")]
        private void TestOpen()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"{name}: 플레이 중에만 됩니다.", this);
                return;
            }

            if (_phase == Phase.Idle) TestBegin();

            if (_phase != Phase.Closed)
            {
                Debug.LogWarning($"{name}: 지금은 {_phase} 라 뚜껑을 열 수 없습니다.", this);
                return;
            }

            OpenLid();
        }

        /// <summary>클릭 대신 카드를 꺼낸다. 뚜껑이 열려 있어야 한다.</summary>
        [ContextMenu("테스트: 카드 꺼내기")]
        private void TestDeal()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"{name}: 플레이 중에만 됩니다.", this);
                return;
            }

            if (_phase != Phase.Opened)
            {
                Debug.LogWarning($"{name}: 지금은 {_phase} 라 카드를 꺼낼 수 없습니다. 뚜껑부터 여세요.", this);
                return;
            }

            StartCoroutine(DealRoutine());
        }

        /// <summary>시작부터 카드가 다 나올 때까지 한 번에. 고르는 것만 남긴다.</summary>
        [ContextMenu("테스트: 카드까지 한 번에")]
        private void TestOpenAndDeal()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"{name}: 플레이 중에만 됩니다.", this);
                return;
            }

            // StopAllCoroutines는 쓰지 않는다. 지급 중인 코루틴까지 끊어
            // 단계가 중간에서 굳어버린다. Begin이 알아서 처음으로 되돌린다.
            testAutoStep = TestAutoStep.Dealt;

            StartCoroutine(Co_TestAuto());
        }

        /// <summary>
        /// 인스펙터에서 값을 만지면 꺼내둔 카드를 그 자리에서 다시 늘어놓는다.
        ///
        /// 플레이 중에만 한다. 정지 상태에서는 꺼내둔 카드가 없다.
        /// </summary>
        private void OnValidate()
        {
            if (!Application.isPlaying) return;

            Relayout();
        }

        /// <summary>지금 어느 단계인지 콘솔에 찍는다. 눌러도 반응이 없을 때 본다.</summary>
        [ContextMenu("테스트: 지금 상태")]
        private void TestDumpState()
        {
            Debug.Log(
                $"{name}\n" +
                $"  단계    : {_phase}\n" +
                $"  바쁨    : {IsBusy}\n" +
                $"  카드    : {_cards.Count}장\n" +
                $"  기물 풀 : {(_piecePool == null ? "없음" : $"대기 {_piecePool.IdleCount} / 만든 것 {_piecePool.CreatedCount}")}\n" +
                $"  유언 풀 : {(_willPool == null ? "없음" : $"대기 {_willPool.IdleCount} / 만든 것 {_willPool.CreatedCount}")}\n" +
                $"  뚜껑    : {(lid == null ? "없음" : lid.IsOpened ? "열림" : "닫힘")}",
                this);
        }

        #endregion

#endif

        /// <summary>꺼내 쓴 카드를 전부 돌려준다. 트윈이 돌던 중이어도 끊는다.</summary>
        private void ReleaseAll()
        {
            foreach (LSO_RewardCard card in _cards)
            {
                if (card == null) continue;

                card.transform.DOKill();

                Return(card);
            }

            _cards.Clear();
        }
    }
}
