using System.Collections;
using System.Collections.Generic;
using _Scripts.LSO.CoreLib;
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

            /// <summary>닫힌 상자. 누르면 뚜껑이 열린다.</summary>
            Closed,

            /// <summary>뚜껑이 도는 중. 클릭을 버린다.</summary>
            Opening,

            /// <summary>열린 상자. 누르면 카드가 나온다.</summary>
            Opened,

            /// <summary>카드가 하나씩 나오는 중. 클릭을 버린다.</summary>
            Dealing,

            /// <summary>고르기를 기다린다. 카드를 누르면 그 자리에서 확정된다.</summary>
            Selecting,

            /// <summary>지급하고 정리하는 중. 클릭을 버린다.</summary>
            Closing
        }

        [Header("연결")]
        [Tooltip("뚜껑 연출. 비워두면 같은 오브젝트에서 찾는다.")]
        [SerializeField] private LSO_BoxOpenEffect lid;

        [Tooltip("스테이지별 보상 후보 테이블.")]
        [SerializeField] private LSO_RewardTableSO table;

        [Tooltip("복제할 카드 원본. LSO_RewardCard가 붙어 있어야 한다.")]
        [SerializeField] private LSO_RewardCard cardPrefab;

        [Tooltip("카드가 늘어설 기준 자리. 비워두면 상자 자신을 쓴다.")]
        [SerializeField] private Transform cardAnchor;

        [Header("배치")]
        [Tooltip("카드 사이 간격. 기준 자리의 로컬 X 방향으로 늘어선다.")]
        [SerializeField] private float cardSpacing = 0.6f;

        [Tooltip("상자 안에서 솟아오르는 높이.")]
        [SerializeField] private float riseHeight = 0.8f;

        [Header("연출")]
        [Tooltip("카드 한 장이 솟는 데 걸리는 시간.")]
        [SerializeField, Min(0f)] private float riseDuration = 0.35f;

        [Tooltip("다음 카드가 나오기까지의 간격.")]
        [SerializeField, Min(0f)] private float dealInterval = 0.12f;

        [SerializeField] private Ease riseEase = Ease.OutBack;

        [Tooltip("고른 뒤 정리를 시작하기까지 두는 시간. 무엇을 얻었는지 볼 틈을 준다.")]
        [SerializeField, Min(0f)] private float claimHold = 0.6f;

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

        [Tooltip("정리까지 끝났을 때. 다음 연출(체스판 뒤집기 등)을 여기 건다.")]
        [SerializeField] private LSO_RewardEvent onFinished;

        private readonly LSO_RewardDraft _draft = new();
        private readonly List<LSO_RewardCard> _cards = new();

        private LSO_ObjectPool<LSO_RewardCard> _pool;
        private Phase _phase = Phase.Idle;
        private int _chapter;
        private int _stage;

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
            _phase == Phase.Opening || _phase == Phase.Dealing || _phase == Phase.Closing;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning($"{name}: 씬에 보상 상자가 둘 이상입니다. 마지막 것이 쓰입니다.", this);

            Instance = this;

            if (lid == null) lid = GetComponent<LSO_BoxOpenEffect>();

            if (lid == null)
                Debug.LogError($"{name}: LSO_BoxOpenEffect가 없어 뚜껑을 열 수 없습니다.", this);

            if (cardAnchor == null) cardAnchor = transform;

            if (cardPrefab == null)
            {
                Debug.LogError($"{name}: Card Prefab이 비어 있어 카드를 만들 수 없습니다.", this);
                return;
            }

            // 후보는 보통 셋이다. 미리 만들어 두면 첫 스테이지에서 끊기지 않는다.
            _pool = new LSO_ObjectPool<LSO_RewardCard>(cardPrefab, cardAnchor, prewarm: 3);
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
        /// 부른다고 바로 열리지는 않는다. 상자가 눌릴 준비만 한다.
        /// 진행 중에 다시 부르면 하던 것을 버리고 처음부터 다시 한다.
        /// </summary>
        public void Begin(int chapter, int stage)
        {
            if (_pool == null)
            {
                Debug.LogError($"{name}: 카드 풀이 없어 보상을 시작할 수 없습니다.", this);
                return;
            }

            _chapter = chapter;
            _stage = stage;

            ReleaseAll();

            _phase = Phase.Closed;

            onReady?.Invoke(null);
        }

        public void OnClick()
        {
            switch (_phase)
            {
                case Phase.Closed:
                    OpenLid();
                    break;

                case Phase.Opened:
                    StartCoroutine(DealRoutine());
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
        }

        private IEnumerator DealRoutine()
        {
            _phase = Phase.Dealing;

            List<LSO_RewardOption> options = _draft.Draw(table, _chapter, _stage);

            if (options.Count == 0)
            {
                Debug.LogWarning($"{name}: 뽑힌 보상이 없어 그대로 끝냅니다.", this);

                _phase = Phase.Closing;
                yield return StartCoroutine(FinishRoutine(null));
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
            LSO_RewardCard card = _pool.Get();

            card.transform.SetParent(cardAnchor, false);

            float offset = (index - (total - 1) * 0.5f) * cardSpacing;

            Vector3 target = new Vector3(offset, riseHeight, 0f);

            // 상자 안에서 시작한다. 켜기 전에 자리를 잡아야 한 프레임 튀지 않는다.
            card.transform.localPosition = new Vector3(offset, 0f, 0f);
            card.transform.localRotation = Quaternion.identity;

            card.Bind(option, HandleCardClicked);

            card.transform
                .DOLocalMove(target, riseDuration)
                .SetEase(riseEase)
                .SetLink(card.gameObject);

            _cards.Add(card);
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

            _phase = Phase.Closing;

            StartCoroutine(FinishRoutine(card.Option));
        }

        private IEnumerator FinishRoutine(LSO_RewardOption option)
        {
            if (option != null)
            {
                LSO_ItemLibraryManager library = LSO_ItemLibraryManager.Instance;

                if (library != null && library.Claim != null)
                    library.Claim.Claim(option);
                else
                    Debug.LogWarning($"{name}: LSO_ItemLibraryManager가 없어 보상을 지급하지 못했습니다.", this);

                onClaimed?.Invoke(option);

                if (claimHold > 0f)
                    yield return new WaitForSeconds(claimHold);
            }

            ReleaseAll();

            if (lid != null) lid.Close();

            _phase = Phase.Idle;

            onFinished?.Invoke(option);
            OnFinished?.Invoke(option);
        }

        /// <summary>꺼내 쓴 카드를 전부 돌려준다. 트윈이 돌던 중이어도 끊는다.</summary>
        private void ReleaseAll()
        {
            if (_pool == null) return;

            foreach (LSO_RewardCard card in _cards)
            {
                if (card == null) continue;

                card.transform.DOKill();

                _pool.Release(card);
            }

            _cards.Clear();
        }
    }
}
