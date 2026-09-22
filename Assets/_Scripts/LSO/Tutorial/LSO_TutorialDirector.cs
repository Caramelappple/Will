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

        [Tooltip("켜면 스페이스를 꾹 눌러 건너뛸 수 있다.\n" +
                 "\n" +
                 "ESC 가 아닌 이유는 DLJ_EscapeKey 가 그 키를 쓰기 때문이다 —\n" +
                 "꾹 누르면 메인 메뉴로 나간다. 같은 키를 쓰면 한 번 누르는 동안\n" +
                 "튜토리얼이 건너뛰어지고 화면도 어두워진다.")]
        [SerializeField] private bool skipWithSpace = true;

        [Tooltip("이만큼 누르고 있어야 건너뛴다(초).\n" +
                 "\n" +
                 "한 번 눌러 바로 넘기면 실수로 튜토리얼이 통째로 사라진다.\n" +
                 "튜토리얼은 다시 볼 수 없으므로 되돌릴 방법이 없다.")]
        [SerializeField, Min(0.1f)] private float skipHoldSeconds = 1f;

        [Header("진단")]
        [SerializeField] private bool logSteps = true;

        /// <summary>
        /// 튜토리얼이 도는 중인지.
        ///
        /// 정적인 이유는 **밖에서도 물어야 하기 때문이다.** 클릭을 막는 쪽
        /// (LSO_TurnClickGate)이 씬마다 여러 개라, 각자 감독을 찾아 물고 있게 하면
        /// 배선이 그만큼 늘고 하나라도 빠지면 그것만 조용히 안 막힌다.
        ///
        /// 값을 들고 있는 곳은 여기 하나다. 보는 쪽은 복사해두지 말고 그때그때 물을 것.
        /// 씬에 감독은 하나뿐이라 정적으로 두어도 주체가 갈리지 않는다.
        /// </summary>
        public static bool IsRunning { get; private set; }

        /// <summary>돌고 있는지. 이 인스턴스로 묻는 길.</summary>
        public bool IsPlaying
        {
            get => IsRunning;
            private set => IsRunning = value;
        }

        /// <summary>끝났을 때. 건너뛰어도 똑같이 쏜다 — 끝난 것은 끝난 것이다.</summary>
        public event Action Finished;

        private Coroutine _routine;

        // 튜토리얼이 직접 선택한 샷만 종료할 때 되돌린다.
        // 보상처럼 다른 흐름이 이후에 카메라를 바꿨다면 그 선택을 덮어쓰지 않는다.
        private string _ownedCameraShotId;

        /// <summary>지금 기다리는 관문. 건너뛸 때 이것부터 풀어야 한다.</summary>
        private LSO_TutorialGateSO _armedGate;
        private bool _gatePassed;

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
            // 정적 값이라 씬을 다시 열어도 지난 판의 "도는 중"이 남아 있을 수 있다.
            // (Enter Play Mode Options 로 도메인 재로드를 끄면 특히 그렇다)
            // 안 지우면 튜토리얼도 안 도는데 클릭이 통째로 막힌다.
            if (_routine == null) IsRunning = false;

            if (playOnStart) Play();
        }

        private void OnDisable()
        {
            // 꺼질 때 정리하지 않으면 칸 제한과 조작 잠금이 남는다.
            // 그 상태는 화면에 아무 표시가 없어 원인을 짐작할 수 없다.
            if (IsPlaying) Stop();
        }

        /// <summary>스페이스를 누르고 있은 시간. 떼면 0으로 돌아간다.</summary>
        private float _skipHeld;

        /// <summary>
        /// 스페이스를 꾹 누르면 건너뛴다.
        ///
        /// ── ESC 가 아닌 이유 ──────────────────────────────────────
        /// ESC 는 DLJ_EscapeKey 가 쓴다 — 꾹 누르면 메인 메뉴로 나간다.
        /// 같은 키를 쓰면 한 번 누르는 동안 튜토리얼이 건너뛰어지고 화면도 어두워진다.
        ///
        /// 여기서 저쪽을 껐다 켜는 방법도 있지만, 그러면 튜토리얼이 남의 컴포넌트를
        /// 여닫는 것을 알게 된다. 키를 나누는 편이 서로 모르는 채로 남는다.
        /// ─────────────────────────────────────────────────────────
        ///
        /// 누르고 있는 동안 안내문이 흐려진다. 아무 반응이 없으면 눌리고 있는지
        /// 알 수 없어서, 사람들은 되는지 확인하려고 손을 뗀다.
        /// </summary>
        private void Update()
        {
            if (!IsPlaying || !skipWithSpace || Keyboard.current == null ||
                !Keyboard.current.spaceKey.isPressed)
            {
                ReleaseSkipHold();
                return;
            }

            // Realtime 이다. 연출이 timeScale 을 쥐는 구간에서도 같은 시간만 누르면 된다.
            _skipHeld += Time.unscaledDeltaTime;

            if (banner != null)
                banner.SetSkipProgress(Mathf.Clamp01(_skipHeld / skipHoldSeconds));

            if (_skipHeld < skipHoldSeconds) return;

            Log($"스페이스를 {skipHoldSeconds:0.#}초 눌렀습니다 — 건너뜁니다.");

            ReleaseSkipHold();

            Stop();
        }

        /// <summary>누름을 놓는다. 안내문도 원래대로 돌린다.</summary>
        private void ReleaseSkipHold()
        {
            if (_skipHeld <= 0f) return;

            _skipHeld = 0f;

            if (banner != null) banner.SetSkipProgress(0f);
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

            _ownedCameraShotId = string.Empty;
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
            ReleaseOwnedCamera();

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

            // 카메라가 이동하는 동안 직전 단계의 조작이 새 단계로 새지 않게 한다.
            if (locks != null) locks.Apply(LSO_TutorialAction.None);

            // 1. 카메라
            //
            // 돌아가기가 샷보다 우선한다. 둘 다 적혀 있으면 "평소 화면으로"가 뜻이 분명하고,
            // 샷을 고른 채 돌아가기를 켜는 것은 대개 실수다.
            bool movedCamera = false;

            if (cameraDirector != null)
            {
                if (step.returnToDefaultCamera)
                {
                    cameraDirector.ReturnToDefault();
                    _ownedCameraShotId = string.Empty;
                    movedCamera = true;
                }
                else if (!string.IsNullOrEmpty(step.shotId))
                {
                    bool wasAlreadyActive = cameraDirector.CurrentId == step.shotId;
                    cameraDirector.Play(step.shotId);

                    // 이미 같은 샷이었다면 보상 등의 다른 흐름이 먼저 선택했을 수 있다.
                    // 실제로 이 호출이 전환시킨 경우에만 튜토리얼 소유로 기록한다.
                    if (!wasAlreadyActive && cameraDirector.CurrentId == step.shotId)
                        _ownedCameraShotId = step.shotId;

                    movedCamera = true;
                }
            }

            if (movedCamera && step.waitForCamera)
            {
                // 한 프레임 준다. 부른 직후에는 아직 IsBlending 이 안 켜져 있다.
                yield return null;

                while (cameraDirector.IsBlending) yield return null;
            }

            // 2. 가이드와 칸 제한 — 같은 값에서 나온다
            ApplyGuide(step);

            _gatePassed = false;
            // 턴 전환/사망 등은 여러 줄 안내를 읽는 동안에도 일어난다.
            if (step.gate is LSO_GatePractice)
            {
                _armedGate = step.gate;
                _armedGate.Arm(this, () => _gatePassed = true);
            }

            // 3. 조작 잠금
            if (locks != null)
            {
                locks.Apply(
                    step.allowed,
                    step.restrictSelectionToWill,
                    step.selectionWill,
                    step.restrictSelectionToTile,
                    step.selectionTile);
            }

            // 4. 안내문
            yield return Co_Lines(step);

            // 5. 관문
            yield return Co_Gate(step);

            // 6. 정리 — 통과했든 시간이 지났든 여기를 지난다
            DisarmGate();

            if (guide != null) guide.Clear();

            ClearRestriction();
        }

        private void ReleaseOwnedCamera()
        {
            // 보상 흐름이 시작됐다면 카메라의 최종 소유자는 튜토리얼이 아니라 보상이다.
            // 두 흐름이 같은 Reward 샷을 선택했더라도 종료 시 메인으로 덮어쓰지 않는다.
            if (_Scripts.LSO.Reward.LSO_RewardBox.Instance != null &&
                _Scripts.LSO.Reward.LSO_RewardBox.Instance.HasBegun)
            {
                _ownedCameraShotId = string.Empty;
                return;
            }

            if (cameraDirector != null &&
                !string.IsNullOrEmpty(_ownedCameraShotId) &&
                cameraDirector.CurrentId == _ownedCameraShotId)
            {
                cameraDirector.ReturnToDefault();
            }

            _ownedCameraShotId = string.Empty;
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

            if (_armedGate == null)
            {
                _armedGate = step.gate;
                _armedGate.Arm(this, () => _gatePassed = true);
            }

            float deadline = step.gateTimeout > 0f
                ? Time.unscaledTime + step.gateTimeout
                : float.PositiveInfinity;

            while (!_gatePassed)
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
                case LSO_TutorialGuideKind.PlaceYellow:
                case LSO_TutorialGuideKind.PlaceRed:
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
            _gatePassed = true;
        }
#endif
    }
}
