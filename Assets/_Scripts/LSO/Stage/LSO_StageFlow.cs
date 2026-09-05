using System;
using System.Collections;
using _Scripts.LDY.Effect;
using _Scripts.LDY.Stage;
using _Scripts.LSO.CoreLib;
using _Scripts.LSO.Reward;
using UnityEngine;
using UnityEngine.Events;

namespace _Scripts.LSO.Stage
{
    /// <summary>
    /// 런 한 판의 흐름을 아는 유일한 곳.
    ///
    /// 클리어 → 보드 회전 → 보상 → 다음 스테이지. 그 순서를 여기서만 안다.
    /// 실제 작업은 남이 한다 — 회전은 LDY_BoardFlipDirector, 보상은 LSO_RewardBox,
    /// 자리는 LSO_StageProgression, 다음 판 세우기는 LSO_StageIntroDirector.
    ///
    /// ── LDY_MapManager를 대신한다 ──────────────────────────────
    /// 예전에는 맵 매니저가 노드·챕터·씬 전환·보상 시작을 한꺼번에 쥐고 있었다.
    /// 고르는 화면이 없어지면서 노드가 사라졌고, 남은 것은 "다음으로 넘긴다" 뿐이라
    /// 이쪽으로 옮겼다.
    ///
    /// 씬을 넘기지 않는다. 같은 화면에서 판을 되돌리고 다음 기물을 놓는다
    /// (기획서 「새로운 UI · 여기서부터 다시 스테이지」).
    /// ─────────────────────────────────────────────────────────
    ///
    /// 씬 배선: 씬 아무 곳에나 하나. 씬을 넘어가도 살아남는다.
    /// </summary>
    [DisallowMultipleComponent]
    public class LSO_StageFlow : MonoSingleton<LSO_StageFlow>
    {
        [Header("연결 (비우면 씬에서 찾는다)")]
        [Tooltip("승리 뒤 보드를 뒤집는 쪽.")]
        [SerializeField] private LDY_BoardFlipDirector flipDirector;

        [Tooltip("보상이 끝난 뒤 판을 되돌리고 다음 기물을 놓는 쪽.")]
        [SerializeField] private LSO_StageIntroDirector introDirector;

        [Header("첫 스테이지")]
        [Tooltip("켜면 게임을 켜자마자 진행이 가리키는 스테이지를 세운다.\n" +
                 "끄면 밖에서 StartRun()을 불러줘야 한다.")]
        [SerializeField] private bool startOnPlay = true;

        [Header("패배")]
        [Tooltip("패배했을 때 진행을 처음으로 되돌릴지.\n" +
                 "\n" +
                 "켜면 다시 시작할 때 1챕터 1스테이지부터다.\n" +
                 "끄면 죽은 자리에 그대로 남는다 — 이어하기를 붙일 때 쓴다.")]
        [SerializeField] private bool restartOnDefeat = true;

        [Header("타이밍")]
        [Tooltip("보드 회전이 끝나기를 기다리는 상한(초). 멈춤 방지선이다.")]
        [SerializeField, Min(0f)] private float flipWaitTimeout = 6f;

        [Header("반응")]
        [Tooltip("스테이지 하나를 깼을 때. 보상이 뜨기 전이다.")]
        [SerializeField] private UnityEvent onStageCleared;

        [Tooltip("패배했을 때.")]
        [SerializeField] private UnityEvent onDefeat;

        [Header("진단")]
        [SerializeField] private bool logSteps;

        private bool _clearing;

        /// <summary>클리어 처리가 도는 중인지. 같은 판이 두 번 정산되는 것을 막는다.</summary>
        public bool IsClearing => _clearing;

        /// <summary>스테이지를 깼을 때. 인자는 방금 깬 스테이지다.</summary>
        public event Action<LDY_StageSO> StageCleared;

        protected override void Awake()
        {
            base.Awake();

            if (Instance != this) return;

            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (!startOnPlay) return;

            StartRun();
        }

        /// <summary>
        /// 진행이 가리키는 스테이지를 세운다. 게임을 시작할 때 부른다.
        ///
        /// 판을 돌리지 않는다 — 아직 뒤집힌 적이 없다.
        /// </summary>
        public void StartRun()
        {
            LDY_StageSO stage = Progression != null ? Progression.Current : null;

            if (stage == null)
            {
                Debug.LogWarning($"{name}: 시작할 스테이지가 없습니다. 챕터 목록을 확인하세요.", this);
                return;
            }

            Log($"런 시작 — {stage.stageName}");

            LDY_StageDirector director = FindAnyObjectByType<LDY_StageDirector>();

            if (director != null)
                director.LoadStage(stage);
            else
                Debug.LogWarning($"{name}: LDY_StageDirector가 없어 기물을 놓지 못했습니다.", this);
        }

        /// <summary>
        /// 스테이지를 깼다. 전투 종료를 판정하는 쪽이 부른다.
        ///
        /// 여기서부터는 손댈 것이 없다 — 회전과 보상이 차례로 돌고,
        /// 보상이 끝나면 LSO_StageIntroDirector가 다음 판을 세운다.
        /// </summary>
        public void ClearStage()
        {
            if (_clearing)
            {
                Log("이미 정산 중이라 무시합니다.");
                return;
            }

            _clearing = true;

            StartCoroutine(Co_Clear());
        }

        /// <summary>
        /// 패배했다.
        ///
        /// 씬을 넘기지 않는다. 패배 화면도 이 화면 위에 띄우고,
        /// 다시 시작하면 같은 자리에서 판만 새로 세운다.
        /// On Defeat에 화면을 걸고, 그 화면의 버튼이 Restart를 부르면 된다.
        /// </summary>
        public void Defeat()
        {
            Log("패배");

            _clearing = false;

            if (restartOnDefeat && Progression != null)
                Progression.Restart();

            onDefeat?.Invoke();
        }

        /// <summary>
        /// 처음부터 다시. 패배 화면의 "다시 하기"가 부른다.
        ///
        /// 진행을 되돌리는 것은 Defeat에서 이미 했다. 여기서는 판만 다시 세운다 —
        /// 두 곳에서 되돌리면 이어하기를 붙일 때 어느 쪽을 꺼야 하는지 알기 어렵다.
        /// </summary>
        public void Restart()
        {
            Log("다시 시작");

            StartRun();
        }

        private IEnumerator Co_Clear()
        {
            LDY_StageSO cleared = Progression != null ? Progression.Current : null;

            Log($"클리어 — {(cleared != null ? cleared.stageName : "알 수 없음")}");

            onStageCleared?.Invoke();
            StageCleared?.Invoke(cleared);

            // 1. 보드를 뒤집는다. 뒷면에 보상 상자가 붙어 있다.
            yield return Co_Flip();

            // 2. 보상을 시작한다.
            //    그 뒤는 LSO_RewardBox.OnFinished → LSO_StageIntroDirector 가 이어간다.
            LSO_RewardBox box = LSO_RewardBox.Instance;

            if (box == null)
            {
                Debug.LogWarning(
                    $"{name}: 씬에 LSO_RewardBox가 없어 보상을 건너뜁니다.", this);

                // 상자가 없으면 OnFinished도 오지 않는다. 진행이 여기서 멈추지 않게 직접 넘긴다.
                if (introDirector != null)
                    introDirector.PlayNext();

                _clearing = false;
                yield break;
            }

            int chapter = Progression != null ? Progression.ChapterNumber : 1;
            int stage = Progression != null ? Progression.StageNumber : 1;

            box.Begin(chapter, stage);

            _clearing = false;
        }

        /// <summary>
        /// 보드 회전을 기다린다. 상한을 두는 이유는 연출이 끝나지 않을 때
        /// 진행이 영영 멈추는 쪽이 잘리는 것보다 나쁘기 때문이다.
        /// </summary>
        private IEnumerator Co_Flip()
        {
            if (flipDirector == null) flipDirector = FindAnyObjectByType<LDY_BoardFlipDirector>();

            if (flipDirector == null)
            {
                Debug.LogWarning($"{name}: LDY_BoardFlipDirector가 없어 회전을 건너뜁니다.", this);
                yield break;
            }

            Log("보드 회전");

            flipDirector.Play();

            float deadline = Time.unscaledTime + flipWaitTimeout;

            while (flipDirector.IsPlaying)
            {
                if (Time.unscaledTime >= deadline)
                {
                    Debug.LogWarning(
                        $"{name}: 회전이 {flipWaitTimeout:0.#}초 안에 끝나지 않아 기다리지 않고 넘어갑니다.", this);
                    break;
                }

                yield return null;
            }
        }

        private LSO_StageProgression Progression =>
            LSO_StageProgression.HasInstance ? LSO_StageProgression.Instance : null;

        private void Log(string message)
        {
            if (logSteps) Debug.Log($"[{name}] {message}", this);
        }

#if UNITY_EDITOR
        [ContextMenu("테스트: 스테이지 클리어")]
        private void TestClear()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"{name}: 플레이 중에만 됩니다.", this);
                return;
            }

            ClearStage();
        }
#endif
    }
}
