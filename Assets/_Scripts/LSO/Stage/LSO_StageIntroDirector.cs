using System;
using System.Collections;
using _Scripts.LDY.Effect;
using _Scripts.LDY.Stage;
using _Scripts.LSO.Reward;
using UnityEngine;
using UnityEngine.Events;

namespace _Scripts.LSO.Stage
{
    /// <summary>
    /// 보상이 끝난 뒤부터 다음 스테이지가 시작되기까지의 순서를 안다.
    ///
    /// 기획서(새로운 UI · 여기서부터 다시 스테이지)의 흐름이다.
    ///   보상 종료 → 판이 앞면으로 되돌아온다 → 기물이 배치된다 → 갈래 연출
    ///
    /// 실제 작업은 남이 한다. 판은 LDY_BoardFlipDirector가 돌리고,
    /// 기물은 LDY_StageDirector가 놓는다. 여기는 "무엇 다음에 무엇" 만 안다.
    /// LDY_BoardFlipDirector가 전투 종료 연출에서 하는 역할과 같은 자리다.
    ///
    /// ── 갈래는 아직 비어 있다 ──────────────────────────────────
    /// 일반 · 챕터 전환 · 보스 세 갈래는 UnityEvent로 뽑아만 뒀다.
    /// 하얀 암전도 지역 이름도 데이터가 없어서 지금은 만들 수 없다.
    /// 자세한 것은 LSO_스테이지_연출_계획.md 참고.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 씬 배선: 씬 아무 곳에나 하나 두면 된다. 비운 참조는 씬에서 찾는다.
    /// </summary>
    [DisallowMultipleComponent]
    public class LSO_StageIntroDirector : MonoBehaviour
    {
        [Header("연결 (비우면 씬에서 찾는다)")]
        [Tooltip("판을 되돌릴 쪽. 뒤집은 것과 같은 것이어야 한다.")]
        [SerializeField] private LDY_BoardFlipDirector flipDirector;

        [Tooltip("기물을 놓을 쪽. 스테이지 하나를 통째로 세운다.")]
        [SerializeField] private LDY_StageDirector stageDirector;

        [Header("시작 조건")]
        [Tooltip("켜면 보상이 끝나는 것을 스스로 듣는다.\n" +
                 "\n" +
                 "끄면 밖에서 Play(stage)를 불러줘야 한다.\n" +
                 "보상 없이 스테이지만 시작하는 경로가 생기면 그때 끈다.")]
        [SerializeField] private bool followReward = true;

        [Tooltip("넘겨받은 스테이지가 없을 때 지금 스테이지를 다시 세울지.\n" +
                 "\n" +
                 "켜면 보상이 끝날 때마다 같은 스테이지가 처음 상태로 돌아온다.\n" +
                 "다음 스테이지를 넘겨줄 길이 아직 없어서, 지금은 이쪽이 실제로 도는 경로다.\n" +
                 "\n" +
                 "끄면 판만 되돌리고 기물은 그대로 둔다.")]
        [SerializeField] private bool reloadCurrentWhenNoStage = true;

        [Header("타이밍")]
        [Tooltip("상자가 닫히고 판이 돌기 시작하기까지의 뜸(초).\n" +
                 "뚜껑이 닫히는 것과 판이 도는 것이 겹치지 않게 한 박자 둔다.")]
        [SerializeField, Min(0f)] private float beforeFlipDelay = 1f;

        [Tooltip("판이 되돌아온 뒤 기물이 놓이기까지의 뜸(초).")]
        [SerializeField, Min(0f)] private float beforePlaceDelay = 0.2f;

        [Header("갈래")]
        [Tooltip("기물 배치가 끝났을 때. 세 갈래 중 무엇도 아닐 때도 발행된다.")]
        [SerializeField] private UnityEvent onPlaced;

        [Tooltip("일반 스테이지. 위 대화창에 스테이지 이름을 띄우는 자리다.")]
        [SerializeField] private UnityEvent onNormalStage;

        [Tooltip("챕터가 바뀐 스테이지. 지지직 암전 → 디자인 교체 → 지역 이름.")]
        [SerializeField] private UnityEvent onChapterChanged;

        [Tooltip("보스 스테이지. 하얀 암전 → 보스 이름 → 이명.")]
        [SerializeField] private UnityEvent onBossStage;

        [Tooltip("마지막 챕터까지 다 깼을 때. 엔딩으로 넘기는 자리다.\n" +
                 "이 뒤로는 다음 스테이지가 없어 판만 되돌리고 멈춘다.")]
        [SerializeField] private UnityEvent onRunFinished;

        [Header("진단")]
        [Tooltip("켜면 단계를 콘솔에 찍는다.")]
        [SerializeField] private bool logSteps;

        private Coroutine _routine;
        private int _lastChapter = -1;

        /// <summary>연출이 도는 중인지. 기다리는 쪽이 본다.</summary>
        public bool IsPlaying { get; private set; }

        /// <summary>기물까지 다 놓여 스테이지가 시작될 준비가 됐을 때.</summary>
        public event Action<LDY_StageSO> Ready;

        private void Awake()
        {
            if (flipDirector == null) flipDirector = FindAnyObjectByType<LDY_BoardFlipDirector>();
            if (stageDirector == null) stageDirector = FindAnyObjectByType<LDY_StageDirector>();

            if (flipDirector == null)
                Debug.LogWarning($"{name}: LDY_BoardFlipDirector가 없어 판을 되돌리지 못합니다.", this);

            if (stageDirector == null)
                Debug.LogWarning($"{name}: LDY_StageDirector가 없어 기물을 놓지 못합니다.", this);
        }

        private void OnEnable()
        {
            if (!followReward) return;

            // 상자는 스테이지마다 새로 생긴다. 인스펙터 참조로 물면 씬을 넘길 때 끊긴다.
            LSO_RewardBox box = LSO_RewardBox.Instance;

            if (box == null)
            {
                Debug.LogWarning(
                    $"{name}: 씬에 LSO_RewardBox가 없어 보상 종료를 들을 수 없습니다. " +
                    "밖에서 Play를 불러주세요.", this);
                return;
            }

            box.OnFinished -= HandleRewardFinished;
            box.OnFinished += HandleRewardFinished;
        }

        private void OnDisable()
        {
            LSO_RewardBox box = LSO_RewardBox.Instance;

            if (box != null) box.OnFinished -= HandleRewardFinished;

            // 꺼지면 코루틴도 함께 죽는다. IsPlaying을 켠 채로 두면
            // 다시 켰을 때 Play가 "이미 도는 중"으로 보고 아무것도 하지 않는다.
            // 그러면 원인이 화면에 드러나지 않는 채로 영영 안 돈다.
            _routine = null;
            IsPlaying = false;
        }

        /// <summary>
        /// 다음 스테이지를 세운다. 진행 중이면 무시한다.
        ///
        /// stage가 null이면 기물 배치를 건너뛰고 판만 되돌린다.
        /// 다음에 무엇이 올지 아직 모르는 경우가 있어서 막지 않는다.
        /// </summary>
        public void Play(LDY_StageSO stage)
        {
            if (IsPlaying) return;

            IsPlaying = true;
            _routine = StartCoroutine(Co_Play(stage));
        }

        /// <summary>
        /// 진행을 한 칸 넘기고 그 스테이지를 세운다.
        ///
        /// 보상을 건너뛰는 경로(상자가 없는 씬 등)에서 흐름이 멈추지 않게 열어둔다.
        /// 보상이 있으면 OnFinished가 같은 일을 한다.
        /// </summary>
        public void PlayNext()
        {
            HandleRewardFinished(null);
        }

        /// <summary>연출을 끊는다. 판은 돌던 자리에 남으므로 부르는 쪽이 정리할 것.</summary>
        public void Abort()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            IsPlaying = false;
        }

        /// <summary>
        /// 상자가 다 닫혔다. 여기가 "클리어 조건을 만족했다"의 끝이다.
        ///
        /// 고르는 화면을 거치지 않고 곧바로 다음 칸으로 넘어간다.
        /// 진행이 없으면 null이 넘어가고, Resolve가 지금 스테이지를 다시 세운다.
        /// </summary>
        private void HandleRewardFinished(LSO_RewardOption option)
        {
            LSO_StageProgression progression = LSO_StageProgression.HasInstance
                ? LSO_StageProgression.Instance
                : null;

            if (progression == null)
            {
                Play(null);
                return;
            }

            LDY_StageSO next = progression.Advance();

            if (next == null)
            {
                // 마지막 챕터까지 다 깼다. 판만 되돌리고 멈춘다 —
                // 엔딩으로 무엇을 보여줄지는 아직 정해지지 않았다.
                Log("런이 끝나 다음 스테이지가 없습니다.");

                onRunFinished?.Invoke();
            }

            Play(next);
        }

        private IEnumerator Co_Play(LDY_StageSO stage)
        {
            Log("시작");

            if (beforeFlipDelay > 0f)
                yield return new WaitForSecondsRealtime(beforeFlipDelay);

            // 1. 판을 앞면으로 되돌린다.
            if (flipDirector != null)
            {
                Log("판 되돌리기");

                yield return StartCoroutine(flipDirector.PlayReverse());
            }

            if (beforePlaceDelay > 0f)
                yield return new WaitForSecondsRealtime(beforePlaceDelay);

            // 2. 기물을 놓는다. 판을 비우는 것도 LDY_StageDirector의 스텝이 한다.
            LDY_StageSO target = Resolve(stage);

            if (target != null && stageDirector != null)
            {
                Log($"기물 배치 — {target.stageName}");

                stageDirector.LoadStage(target);
            }

            onPlaced?.Invoke();

            // 3. 갈래를 고른다.
            RaiseBranch(target);

            Ready?.Invoke(target);

            _routine = null;
            IsPlaying = false;

            Log("끝");
        }

        /// <summary>
        /// 실제로 세울 스테이지.
        ///
        /// 넘겨받은 것이 있으면 그것이다. 없으면 지금 세워져 있는 것을 다시 쓴다 —
        /// 같은 스테이지가 처음 상태로 돌아오므로 "리셋"이 된다.
        ///
        /// 다음 스테이지를 넘겨줄 길이 생기면 이 되돌림은 필요 없어진다.
        /// </summary>
        private LDY_StageSO Resolve(LDY_StageSO stage)
        {
            if (stage != null) return stage;

            if (!reloadCurrentWhenNoStage) return null;

            return stageDirector != null ? stageDirector.CurrentStage : null;
        }

        /// <summary>
        /// 세 갈래 중 하나를 발행한다.
        ///
        /// 보스가 챕터 전환보다 앞선다. 챕터의 마지막이 보스라 둘이 겹칠 수 있는데,
        /// 그때 보여줄 것은 보스 등장이지 지역 소개가 아니다.
        /// </summary>
        private void RaiseBranch(LDY_StageSO stage)
        {
            if (IsBossStage())
            {
                Log("갈래: 보스");
                onBossStage?.Invoke();
                return;
            }

            if (HasChapterChanged())
            {
                Log("갈래: 챕터 전환");
                onChapterChanged?.Invoke();
                return;
            }

            Log("갈래: 일반");
            onNormalStage?.Invoke();
        }

        /// <summary>
        /// 지금 들어가는 곳이 보스 스테이지인지.
        ///
        /// 챕터 목록의 마지막 칸을 보스로 본다. 스테이지에 따로 적어두지 않는 이유는,
        /// 두 곳에 적으면 어긋났을 때 어느 쪽이 맞는지 정할 수 없어서다.
        ///
        /// 진행이 없으면 보스가 아닌 것으로 본다 — 없다는 이유로 보스 연출을 틀면
        /// 일반 스테이지가 하얗게 덮인다.
        /// </summary>
        private bool IsBossStage()
        {
            return LSO_StageProgression.HasInstance && LSO_StageProgression.Instance.IsBoss;
        }

        /// <summary>
        /// 지난번과 챕터가 달라졌는지.
        ///
        /// 처음 한 번은 "바뀌었다"로 치지 않는다. 게임을 켜고 첫 스테이지에
        /// 들어갈 때마다 지역 소개가 뜨면 지겨워진다.
        /// </summary>
        private bool HasChapterChanged()
        {
            if (!LSO_StageProgression.HasInstance) return false;

            int chapter = LSO_StageProgression.Instance.ChapterNumber;

            bool changed = _lastChapter >= 0 && chapter != _lastChapter;

            _lastChapter = chapter;

            return changed;
        }

        private void Log(string message)
        {
            if (logSteps) Debug.Log($"[{name}] {message}", this);
        }

#if UNITY_EDITOR
        [ContextMenu("테스트: 되돌리기부터 재생")]
        private void TestPlay()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"{name}: 플레이 중에만 됩니다.", this);
                return;
            }

            Play(null);
        }
#endif
    }
}
