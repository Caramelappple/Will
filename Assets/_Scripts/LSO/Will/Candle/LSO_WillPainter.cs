using System.Collections;
using _Scripts.LDY.Stage;
using _Scripts.LSO.Camera;
using _Scripts.LSO.UI.Input;
using UnityEngine;
using UnityEngine.Events;

namespace _Scripts.LSO.Will.Candle
{
    /// <summary>
    /// 양초를 누르면 지금 고른 카드로 가서 불을 댄다.
    ///
    /// ── 어떻게 쓰나 ───────────────────────────────────────────
    ///   1. 숫자키로 불꽃 색을 고른다        (LSO_WillCandle)
    ///   2. 손패에서 카드를 고른다
    ///   3. 양초를 누른다                    → 양초가 그 카드로 가서 불을 댄다
    ///
    /// 고른 카드가 없으면 아무 일도 하지 않는다. 어디에 붙일지 모르는 채로
    /// 양초만 움직이면 무엇이 일어났는지 알 수 없다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 양초(LSO_WillCandle)는 "지금 무슨 색인가"까지만 알고,
    /// 카드(LSO_CardWill)는 "나에게 무엇이 붙었나"만 안다.
    /// 둘을 이어주는 것이 여기다. 어느 쪽도 상대를 모른다.
    ///
    /// 씬 배선: 양초 오브젝트에 LSO_WillCandle 과 함께 붙일 것.
    /// Collider 도 있어야 클릭이 온다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LSO_ButtonClickHandler))]
    public sealed class LSO_WillPainter : MonoBehaviour, LSO_IClickEffect
    {
        [Header("연결")]
        [Tooltip("불을 고르는 양초. 비워두면 같은 오브젝트에서 찾는다.")]
        [SerializeField] private LSO_WillCandle candle;

        [Tooltip("카드로 다가가는 움직임. 비워두면 같은 오브젝트에서 찾는다.\n" +
                 "없어도 된다 — 그때는 움직임 없이 그 자리에서 붙는다.")]
        [SerializeField] private LSO_WillCandleMotion motion;

        [Header("동작")]
        [Tooltip("카드 한 장에 전투당 한 번만 붙일 수 있게 할지.\n" +
                 "\n" +
                 "켜면 한 번 붙인 카드는 다음 전투까지 못 바꾼다. 다른 카드에는 계속 붙일 수 있다.\n" +
                 "끄면 몇 번이든 덮어쓸 수 있다.")]
        [SerializeField] private bool oncePerCardPerBattle = true;

        [Header("카메라")]
        [Tooltip("유언을 붙일 때 자동으로 확대할지.\n" +
                 "\n" +
                 "끄면 카메라를 건드리지 않고, 기다리지도 않는다 —\n" +
                 "붙이는 연출이 곧바로 돈다. 배선을 시험할 때 꺼두면 편하다.")]
        [SerializeField] private bool closeUpOnPaint = true;

        [Tooltip("확대할 때 잡을 샷의 이름(LSO_CameraDirector 의 Shots 에 적힌 Id).\n" +
                 "\n" +
                 "비워두면 위를 켜뒀어도 카메라를 건드리지 않는다.\n" +
                 "\n" +
                 "언제 돌아올지는 여기서 정하지 않는다 — 그 샷의 Hold Time 과 Next Id 가 정한다.\n" +
                 "카메라가 어디를 보는지는 카메라 쪽 하나만 알아야 어긋나지 않는다.")]
        [SerializeField] private string closeUpShotId;

        [Tooltip("카메라가 다 들어오기를 기다리는 상한(초). 멈춤 방지선이다.\n" +
                 "\n" +
                 "블렌드가 끝나지 않는 상황에서 연출이 영영 안 나오는 쪽이,\n" +
                 "조금 겹쳐 보이는 것보다 나쁘다.")]
        [SerializeField, Min(0f)] private float cameraWaitTimeout = 3f;

        [Header("반응")]
        [Tooltip("유언을 붙였을 때. 소리를 여기 걸면 된다.")]
        [SerializeField] private LSO_WillTypeEvent onPainted;

        [Tooltip("고른 카드가 없어서 아무 일도 못 했을 때.\n" +
                 "'카드를 먼저 고르세요' 같은 안내를 여기 걸면 된다.")]
        [SerializeField] private LSO_WillTypeEvent onNoTarget;

        [Tooltip("이번 전투에 이미 다 써서 못 붙일 때.\n" +
                 "'이번 전투에는 이미 붙였습니다' 같은 안내를 여기 걸면 된다.")]
        [SerializeField] private UnityEvent onSpent;

        [Tooltip("다음 전투가 시작돼 다시 붙일 수 있게 됐을 때.")]
        [SerializeField] private UnityEvent onRefilled;

        [Header("진단")]
        [Tooltip("켜면 양초를 누를 때마다 어디까지 갔는지 콘솔에 찍는다.\n" +
                 "붙지 않을 때 어느 단계에서 멈추는지 보인다.")]
        [SerializeField] private bool logSteps;

        /// <summary>
        /// 붙일 카드를 밖에서 직접 지정한다.
        ///
        /// 비워두면 손패에서 고른 카드를 찾는다. 손패 구현이 바뀌어 못 찾게 되면
        /// 이 값을 넣어주는 쪽을 만들면 된다 — 찾는 코드를 고칠 필요가 없다.
        /// </summary>
        public GameObject Target { get; set; }

        /// <summary>지금 양초가 든 유언. 양초가 없으면 None.</summary>
        public LSO_WillType Held => candle != null ? candle.Current : LSO_WillType.None;

        /// <summary>
        /// 확대를 쓸 조건인지. 잡는 쪽과 기다리는 쪽이 같은 값을 봐야 한다 —
        /// 한쪽만 켜지면 카메라는 안 움직이는데 연출만 기다리게 된다.
        /// </summary>
        public bool UsesCloseUp =>
            closeUpOnPaint && !string.IsNullOrWhiteSpace(closeUpShotId);

        private LDY_StageDirector _stageDirector;
        private LSO_CameraDirector _director;
        private Coroutine _routine;

        private void Awake()
        {
            if (candle == null) candle = GetComponent<LSO_WillCandle>();
            if (motion == null) motion = GetComponent<LSO_WillCandleMotion>();

            if (candle == null)
                Debug.LogError($"{name}: LSO_WillCandle이 없어 무슨 색인지 알 수 없습니다.", this);

            // 매번 찾지 않는다. 씬에 하나뿐이고 붙이는 순간마다 훑을 이유가 없다.
            if (UsesCloseUp)
            {
                _director = FindAnyObjectByType<LSO_CameraDirector>();

                if (_director == null)
                {
                    Debug.LogWarning(
                        $"{name}: LSO_CameraDirector가 없어 '{closeUpShotId}' 샷을 잡지 못합니다. " +
                        "연출은 카메라를 기다리지 않고 바로 돕니다.", this);
                }
            }
        }

        private void OnEnable()
        {
            // 스테이지가 세워지는 것이 곧 "새 전투가 시작됐다"이다.
            // 턴이 아니라 스테이지를 기준으로 잡는 이유는, 한 전투 안에서 턴이 여러 번 돌기 때문이다.
            _stageDirector = FindAnyObjectByType<LDY_StageDirector>();

            if (_stageDirector != null)
                _stageDirector.OnStageLoaded += HandleStageLoaded;

            Refill();
        }

        private void OnDisable()
        {
            if (_stageDirector != null)
                _stageDirector.OnStageLoaded -= HandleStageLoaded;

            // 꺼지면 코루틴은 유니티가 죽이지만 핸들은 그대로 남는다.
            // 남겨두면 다시 켰을 때 StopCoroutine 이 죽은 핸들을 붙든다.
            _routine = null;
        }

        private void HandleStageLoaded(LDY_StageSO stage)
        {
            Refill();
        }

        /// <summary>
        /// 손패의 모든 카드를 다시 붙일 수 있게 푼다. 새 전투가 시작될 때 불린다.
        ///
        /// 붙어 있던 유언은 지우지 않는다. 잠금만 푼다.
        ///
        /// 꺼진 카드까지 훑는 이유는 손패가 카드를 풀에 넣어두기 때문이다.
        /// 지금 안 보이는 카드도 다음에 뽑히면 그대로 다시 나온다.
        /// </summary>
        public void Refill()
        {
            LSO_CardWill[] cards =
                FindObjectsByType<LSO_CardWill>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            int unlocked = 0;

            foreach (LSO_CardWill card in cards)
            {
                if (card == null || !card.PaintedThisBattle) continue;

                card.UnlockForNewBattle();
                unlocked++;
            }

            if (unlocked == 0) return;

            Log($"카드 {unlocked}장을 다시 붙일 수 있게 풀었다");

            onRefilled?.Invoke();
        }

        /// <summary>양초를 눌렀다. 고른 카드로 가서 불을 댄다.</summary>
        public void OnClick()
        {
            Paint();
        }

        /// <summary>
        /// 고른 카드에 지금 든 유언을 붙인다.
        /// </summary>
        /// <returns>붙였으면 참. 고를 카드가 없거나 배선이 빠졌으면 거짓.</returns>
        public bool Paint()
        {
            Log("양초를 눌렀다");

            if (candle == null)
            {
                Debug.LogWarning($"{name}: LSO_WillCandle이 없어 무슨 색인지 알 수 없습니다.", this);
                return false;
            }

            GameObject card = Target != null ? Target : FindSelectedCard();

            if (card == null)
            {
                // 화면 안내는 이벤트로 하되, 어디서 멈췄는지는 로그로도 남긴다.
                // 이벤트에 아무것도 안 걸어두면 아무 일도 안 일어난 것처럼 보인다.
                Log("고른 카드가 없다 — 카드를 클릭해서 확정한 뒤에 눌러야 한다 " +
                    "(마우스를 올려두기만 한 것은 확정이 아니다)");

                onNoTarget?.Invoke(candle.Current);
                return false;
            }

            Log($"대상 카드 — {card.name}");

            LSO_CardWill target = card.GetComponentInChildren<LSO_CardWill>(true);

            if (target == null)
            {
                Debug.LogWarning(
                    $"{name}: '{card.name}'에 LSO_CardWill이 없어 유언을 붙이지 못했습니다. " +
                    "손패 카드 프리팹에 붙여 주세요.", card);
                return false;
            }

            // 카드 한 장은 전투당 한 번만 받는다. 양초는 잠기지 않으므로
            // 다른 카드에는 계속 붙일 수 있다.
            if (oncePerCardPerBattle && target.PaintedThisBattle)
            {
                Log($"'{card.name}'에는 이번 전투에 이미 붙였다 — 다음 전투까지 못 바꾼다");

                onSpent?.Invoke();
                return false;
            }

            LSO_WillType will = candle.Current;

            // 값은 지금 붙인다. 연출은 카메라가 들어온 뒤에 도는데, 그 사이에 카드를 놓아도
            // 유언이 빠지면 안 된다.
            target.Apply(will, revealNow: false);

            Log($"붙였다 — {will}");

            onPainted?.Invoke(will);

            PlayCloseUp();

            // 연달아 누르면 하던 연출을 끊는다. 큐에 쌓으면 손을 뗀 뒤에도 혼자 돈다.
            if (_routine != null) StopCoroutine(_routine);

            _routine = StartCoroutine(Co_Reveal(target, card));

            return true;
        }

        /// <summary>
        /// 카메라가 다 들어온 뒤에 붙이는 연출을 돌린다.
        ///
        /// 카메라가 들어오는 도중에 양초가 움직이면 두 움직임이 겹쳐서
        /// 무엇을 보라는 것인지 알기 어렵다. 들어온 뒤에 시작한다.
        /// </summary>
        private IEnumerator Co_Reveal(LSO_CardWill target, GameObject card)
        {
            yield return Co_WaitForCamera();

            _routine = null;

            // 기다리는 사이에 카드가 놓이거나 손패에서 빠졌을 수 있다.
            if (target == null || card == null) yield break;

            if (motion == null)
            {
                target.Reveal(transform.position);
                yield break;
            }

            Transform self = transform;

            // 아이콘은 양초가 가장 가까이 닿은 순간에 드러난다.
            motion.Reach(card.transform.position, () => target.Reveal(self.position));
        }

        /// <summary>
        /// 카메라 블렌드가 끝나기를 기다린다.
        ///
        /// 상한을 두는 이유는 블렌드가 끝나지 않을 때 연출이 영영 안 나오는 쪽이
        /// 조금 겹쳐 보이는 것보다 나쁘기 때문이다.
        /// </summary>
        private IEnumerator Co_WaitForCamera()
        {
            if (!UsesCloseUp || _director == null) yield break;

            // 블렌드는 다음 프레임에 시작한다. 지금 물어보면 아직 false 라 그냥 지나간다.
            yield return null;

            float deadline = Time.unscaledTime + cameraWaitTimeout;

            while (_director.IsBlending)
            {
                if (Time.unscaledTime >= deadline)
                {
                    Debug.LogWarning(
                        $"{name}: 카메라가 {cameraWaitTimeout:0.#}초 안에 안 들어와 " +
                        "기다리지 않고 연출을 시작합니다.", this);
                    yield break;
                }

                yield return null;
            }
        }

        // 붙었다는 기록은 LSO_CardWill.Apply 가 그 자리에서 남긴다(PaintedThisBattle).
        // 연출이 끝나기를 기다리지 않는다 — 양초가 다가가는 0.6초 사이에 또 누르면
        // 같은 카드에 두 번 붙어버린다.

        /// <summary>
        /// 붙이는 동안 클로즈업을 잡는다.
        ///
        /// 돌아오는 것은 여기서 하지 않는다. 샷의 Hold Time 이 지나면
        /// Next Id 로 넘어가도록 카메라 쪽에 적어두면 된다.
        ///
        /// 여기서 "몇 초 뒤에 돌아와라"까지 정하면 카메라가 어디를 보는지를
        /// 두 곳이 나눠 갖게 되고, 어긋났을 때 어느 쪽이 맞는지 알 수 없다.
        /// </summary>
        private void PlayCloseUp()
        {
            if (!UsesCloseUp || _director == null) return;

            Log($"클로즈업 — {closeUpShotId}");

            _director.Play(closeUpShotId);
        }

        private void Log(string message)
        {
            if (logSteps) Debug.Log($"[{name}] {message}", this);
        }

        /// <summary>
        /// 손패에서 지금 고른 카드를 찾는다.
        ///
        /// ── 여기만 손패 구현을 안다 ───────────────────────────
        /// 손패는 "한 번에 한 장만 확정된다"를 이미 지키고 있고, 그 값을
        /// KTH_HandCard.ConfirmedCard 로 열어준다. 그대로 읽는다.
        ///
        /// 예전에는 카드를 전부 훑어 IsSelected 를 하나씩 물어봤다. 같은 사실을
        /// 두 곳에서 세는 셈이라, 손패가 바뀌면 훑는 쪽이 조용히 어긋난다.
        ///
        /// 손패 구현이 바뀌면 **이 메서드 하나만** 고치면 된다.
        /// 아니면 밖에서 Target 을 넣어주면 이쪽은 아예 안 돈다.
        /// ─────────────────────────────────────────────────────
        /// </summary>
        private static GameObject FindSelectedCard()
        {
            KTH_HandCard card = KTH_HandCard.ConfirmedCard;

            return card != null ? card.gameObject : null;
        }
    }
}
