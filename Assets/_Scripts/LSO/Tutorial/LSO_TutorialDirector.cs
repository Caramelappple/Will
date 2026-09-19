using System;
using System.Collections;
using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LSO.Camera;
using _Scripts.LSO.Tutorial.Data;
using _Scripts.LSO.Tutorial.Gate;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts.LSO.Tutorial
{
    /// <summary>
    /// 대본을 한 걸음씩 읽어준다.
    ///
    /// ── 이 클래스가 모르는 것 ─────────────────────────────────
    /// **무엇을 기다리는지 모른다.** 관문에게 "기다려" 하고, 관문이 "됐다" 하면
    /// 다음으로 간다. 조건이 열세 가지든 백 가지든 여기는 안 고쳐진다.
    ///
    /// **무슨 말을 하는지도 모른다.** 대본은 전부 에셋(ChapterSO)에 있다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 여기가 아는 것은 순서 하나다 — 카메라를 옮기고, 칸을 좁히고, 조작을 잠그고,
    /// 읽어주고, 기다리고, 푼다.
    ///
    /// 씬 배선: 씬에 하나. 참조는 비워두면 찾는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LSO_TutorialDirector : MonoBehaviour
    {
        [Header("대본")]
        [SerializeField] private List<LSO_TutorialChapterSO> chapters = new();

        [Header("화면 (비우면 찾는다)")]
        [SerializeField] private LSO_TutorialBanner banner;
        [SerializeField] private LSO_TutorialGuide guide;
        [SerializeField] private LSO_TutorialLock locks;
        [SerializeField] private LSO_CameraDirector cameraDirector;

        [Header("칸 좁히기 (비우면 찾는다)")]
        [Tooltip("대본이 표시한 칸에만 놓을 수 있게 한다.")]
        [SerializeField] private LDY_CardPlacer cardPlacer;

        [Tooltip("대본이 표시한 칸으로만 갈 수 있게 한다.")]
        [SerializeField] private LDY_MoveSystem moveSystem;

        [Header("동작")]
        [SerializeField] private bool playOnStart;

        [Tooltip("켜면 ESC 로 건너뛸 수 있다.")]
        [SerializeField] private bool skipWithEscape = true;

        [Header("진단")]
        [SerializeField] private bool logSteps = true;

        /// <summary>돌고 있는지. 다른 곳이 \"지금 튜토리얼 중인가\"를 물을 때 쓴다.</summary>
        public bool IsPlaying { get; private set; }

        /// <summary>끝났을 때. 건너뛰어도 똑같이 쏜다 — 끝난 것은 끝난 것이다.</summary>
        public event Action Finished;

        private Coroutine _routine;

        /// <summary>지금 기다리는 관문. 건너뛸 때 이것부터 풀어야 한다.</summary>
        private LSO_TutorialGateSO _armedGate;

        private void Awake()
        {
            if (banner == null) banner = FindAnyObjectByType<LSO_TutorialBanner>();
            if (guide == null) guide = FindAnyObjectByType<LSO_TutorialGuide>();
            if (locks == null) locks = FindAnyObjectByType<LSO_TutorialLock>();
            if (cameraDirector == null) cameraDirector = FindAnyObjectByType<LSO_CameraDirector>();
            if (cardPlacer == null) cardPlacer = FindAnyObjectByType<LDY_CardPlacer>();
            if (moveSystem == null) moveSystem = FindAnyObjectByType<LDY_MoveSystem>();
        }

        private void Start()
        {
            if (playOnStart) Play();
        }

        private void OnDisable()
        {
            // 꺼질 때 정리하지 않으면 칸 제한과 조작 잠금이 남는다.
            // 그 상태는 화면에 아무 표시가 없어 원인을 짐작할 수 없다.
            if (IsPlaying) Stop();
        }

        private void Update()
        {
            if (!IsPlaying || !skipWithEscape) return;
            if (Keyboard.current == null) return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Log("ESC — 건너뜁니다.");

                Stop();
            }
        }

        // =========================================================
        // 시작과 끝
        // =========================================================

        public void Play()
        {
            if (IsPlaying)
            {
                Log("이미 돌고 있어 무시합니다.");
                return;
            }

            if (chapters.Count == 0)
            {
                Debug.LogWarning($"{name}: 챕터가 하나도 없어 재생할 것이 없습니다.", this);
                return;
            }

            IsPlaying = true;
            _routine = StartCoroutine(Co_Run());
        }

        /// <summary>
        /// 끝낸다. **정상 종료와 건너뛰기가 함께 쓴다.**
        ///
        /// 끝나는 길은 둘이지만 정리하는 곳은 여기 하나다. 둘을 따로 적으면
        /// 한쪽만 고쳤을 때 "건너뛰면 칸 제한이 안 풀리는" 식으로 갈린다.
        /// </summary>
        public void Stop()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            DisarmGate();

            if (guide != null) guide.Clear();
            if (banner != null) banner.Clear();

            ClearRestriction();

            if (locks != null) locks.Release();
            if (cameraDirector != null) cameraDirector.ReturnToDefault();

            if (!IsPlaying) return;

            IsPlaying = false;

            Log("끝났습니다.");

            Finished?.Invoke();
        }

        // =========================================================
        // 재생
        // =========================================================

        private IEnumerator Co_Run()
        {
            for (int c = 0; c < chapters.Count; c++)
            {
                LSO_TutorialChapterSO chapter = chapters[c];

                if (chapter == null) continue;

                Log($"── {chapter.title} ──");

                for (int s = 0; s < chapter.steps.Count; s++)
                {
                    LSO_TutorialStepSO step = chapter.steps[s];

                    if (step == null) continue;

                    yield return Co_Step(chapter, s, step);
                }
            }

            _routine = null;

            Stop();
        }

        private IEnumerator Co_Step(LSO_TutorialChapterSO chapter, int index, LSO_TutorialStepSO step)
        {
            Log($"[{chapter.title}] {index + 1}. {step.Preview}");

            // 1. 카메라
            if (!string.IsNullOrEmpty(step.shotId) && cameraDirector != null)
            {
                cameraDirector.Play(step.shotId);

                if (step.waitForCamera)
                {
                    // 한 프레임 준다. 부른 직후에는 아직 IsBlending 이 안 켜져 있다.
                    yield return null;

                    while (cameraDirector.IsBlending) yield return null;
                }
            }

            // 2. 가이드와 칸 제한 — 같은 값에서 나온다
            ApplyGuide(step);

            // 3. 조작 잠금
            if (locks != null) locks.Apply(step.allowed);

            // 4. 안내문
            yield return Co_Lines(step);

            // 5. 관문
            yield return Co_Gate(step);

            // 6. 정리 — 통과했든 시간이 지났든 여기를 지난다
            DisarmGate();

            if (guide != null) guide.Clear();

            ClearRestriction();
        }

        private IEnumerator Co_Lines(LSO_TutorialStepSO step)
        {
            if (banner == null || step.lines == null) yield break;

            for (int i = 0; i < step.lines.Length; i++)
            {
                banner.Show(step.lines[i]);

                // 마지막 줄은 관문이 열릴 때까지 남는다. 읽는 도중에 사라지면
                // 무엇을 하라는 것이었는지 다시 볼 방법이 없다.
                bool isLast = i == step.lines.Length - 1;

                if (isLast) yield break;

                yield return new WaitForSecondsRealtime(step.lineHold);
            }
        }

        private IEnumerator Co_Gate(LSO_TutorialStepSO step)
        {
            if (step.gate == null)
            {
                // 관문이 없으면 마지막 줄을 읽을 시간만 준다.
                yield return new WaitForSecondsRealtime(step.lineHold);
                yield break;
            }

            bool passed = false;

            _armedGate = step.gate;
            _armedGate.Arm(this, () => passed = true);

            float deadline = step.gateTimeout > 0f
                ? Time.unscaledTime + step.gateTimeout
                : float.PositiveInfinity;

            while (!passed)
            {
                if (Time.unscaledTime >= deadline)
                {
                    // 멈춰 서는 쪽이 한 걸음 건너뛰는 쪽보다 나쁘다.
                    // 튜토리얼이 안 넘어가면 플레이어는 게임을 끄는 수밖에 없다.
                    Debug.LogWarning(
                        $"{name}: '{step.name}' 의 관문이 {step.gateTimeout:0.#}초 안에 " +
                        "열리지 않아 그냥 넘어갑니다. 대본이 시킨 조작을 할 수 없는 상태일 수 있습니다.",
                        this);

                    break;
                }

                yield return null;
            }
        }

        // =========================================================
        // 곁일
        // =========================================================

        /// <summary>
        /// 칸을 물들이고, 같은 칸으로 조작도 좁힌다.
        ///
        /// **표시와 허용이 한 값(step.guideTiles)에서 나온다.** 따로 적으면 언젠가
        /// 어긋나고, 그때 플레이어는 노란 칸을 눌렀는데 아무 일도 안 일어나는 화면을 본다.
        /// </summary>
        private void ApplyGuide(LSO_TutorialStepSO step)
        {
            if (guide != null) guide.Show(step.guide, step.guideTiles);

            if (step.guide == LSO_TutorialGuideKind.None ||
                step.guideTiles == null || step.guideTiles.Length == 0)
            {
                ClearRestriction();
                return;
            }

            switch (step.guide)
            {
                case LSO_TutorialGuideKind.Place:
                    if (cardPlacer != null) cardPlacer.RestrictTo(step.guideTiles);
                    break;

                case LSO_TutorialGuideKind.Move:
                    if (moveSystem != null) moveSystem.RestrictTo(step.guideTiles);
                    break;
            }
        }

        /// <summary>
        /// 좁혀둔 것을 전부 푼다. 어느 쪽을 좁혔는지 기억하지 않고 둘 다 푼다 —
        /// 기억하면 그 기억이 어긋날 자리가 하나 더 생긴다.
        /// </summary>
        private void ClearRestriction()
        {
            if (cardPlacer != null) cardPlacer.ClearRestriction();
            if (moveSystem != null) moveSystem.ClearRestriction();
        }

        private void DisarmGate()
        {
            if (_armedGate == null) return;

            _armedGate.Disarm();
            _armedGate = null;
        }

        private void Log(string message)
        {
            if (logSteps) Debug.Log($"[{name}] {message}", this);
        }

#if UNITY_EDITOR
        // =========================================================
        // 손으로 눌러보는 자리
        //
        // 챕터 4를 고치는데 매번 1부터 봐야 하면 만들다 지친다.
        // =========================================================

        [ContextMenu("테스트: 재생")]
        private void TestPlay()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"{name}: 플레이 중에만 됩니다.", this);
                return;
            }

            Play();
        }

        [ContextMenu("테스트: 지금 관문 통과시키기")]
        private void TestPassGate()
        {
            if (!Application.isPlaying || _armedGate == null)
            {
                Debug.LogWarning($"{name}: 플레이 중이고 관문을 기다리는 동안에만 됩니다.", this);
                return;
            }

            // 시간 초과와 같은 길로 보낸다. 통과 처리를 여기서 따로 흉내 내면
            // 실제 통과와 다르게 동작할 수 있다.
            _armedGate.Disarm();
            _armedGate = null;
        }
#endif
    }
}
