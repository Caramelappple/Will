using System.Collections.Generic;
using _Scripts.LDY.Stage;
using UnityEngine;

namespace _Scripts.LSO.Stage
{
    /// <summary>
    /// 스테이지가 바뀌는 동안 코스트 케이스를 화면 밖으로 내보냈다가 다시 들인다.
    ///
    ///   클리어      → 케이스가 왼쪽으로 빠진다
    ///   세팅 완료   → 들어온다 (코인은 케이스가 도착한 뒤 쏟아진다)
    ///
    /// ── 왜 따로 두나 ─────────────────────────────────────────
    /// 코스트 쪽(DLJ)은 자기가 어떻게 들어오고 나가는지만 알고, 흐름(LSO)은
    /// 언제 판이 바뀌는지만 안다. 둘을 아는 곳이 하나 필요한데 어느 쪽에 넣어도
    /// 그쪽이 남의 일을 알게 된다. 그래서 이 사이에 얇게 하나 둔다.
    ///
    /// 이 컴포넌트를 지우면 케이스는 그냥 계속 떠 있는다 — 게임은 그대로 돌아간다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 씬 배선: 씬 아무 곳에나 하나. 비워두면 알아서 찾는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LSO_CostCaseStageHook : MonoBehaviour
    {
        [Header("연결 (비우면 씬에서 찾는다)")]
        [Tooltip("케이스들이 담긴 부모. 케이스는 런타임에 늘었다 줄었다 하므로\n" +
                 "그때그때 이 밑을 다시 훑는다.\n" +
                 "\n" +
                 "비워두면 씬 전체에서 DLJ_CostAnimation 을 찾는다.")]
        [SerializeField] private Transform caseRoot;

        [Tooltip("세팅이 끝나는 것을 들을 쪽. 비워두면 씬에서 찾는다.")]
        [SerializeField] private LSO_StageIntroDirector introDirector;

        [Header("동작")]
        [Tooltip("끄면 아무것도 하지 않는다. 연출을 잠깐 빼보고 싶을 때.")]
        [SerializeField] private bool enabledHook = true;

        [Header("진단")]
        [Tooltip("어디까지 갔는지 콘솔에 찍는다. 배선을 맞출 때까지는 켜두는 편이 낫다.")]
        [SerializeField] private bool logSteps = true;

        private LSO_StageFlow _flow;

        /// <summary>매번 새로 담는다. 케이스가 늘거나 줄면 목록이 달라진다.</summary>
        private readonly List<DLJ_CostAnimation> _cases = new();

        private void Start()
        {
            Subscribe();
        }

        private void OnDestroy()
        {
            if (_flow != null) _flow.StageCleared -= HandleStageCleared;
            if (introDirector != null) introDirector.Ready -= HandleReady;
        }

        /// <summary>
        /// 모든 Awake 가 끝난 Start 에서 잡는다.
        ///
        /// LSO_StageFlow 는 MonoSingleton 이라 Awake 에서 Instance 가 채워지고,
        /// LSO_StageIntroDirector 도 그때 씬에 있어야 찾을 수 있다.
        /// </summary>
        private void Subscribe()
        {
            if (LSO_StageFlow.HasInstance) _flow = LSO_StageFlow.Instance;

            if (_flow != null)
            {
                _flow.StageCleared -= HandleStageCleared;
                _flow.StageCleared += HandleStageCleared;

                Log("클리어 신호를 듣기 시작했습니다.");
            }
            else
            {
                Debug.LogWarning(
                    $"{name}: LSO_StageFlow가 없어 클리어를 들을 수 없습니다. " +
                    "케이스가 빠지지 않습니다.", this);
            }

            if (introDirector == null) introDirector = FindAnyObjectByType<LSO_StageIntroDirector>();

            if (introDirector != null)
            {
                introDirector.Ready -= HandleReady;
                introDirector.Ready += HandleReady;

                Log("세팅 완료 신호를 듣기 시작했습니다.");
            }
            else
            {
                Debug.LogWarning(
                    $"{name}: LSO_StageIntroDirector가 없어 세팅 완료를 들을 수 없습니다. " +
                    "한 번 빠진 케이스가 돌아오지 않습니다.", this);
            }
        }

        private void HandleStageCleared(LDY_StageSO stage)
        {
            Log("클리어 신호 받음");

            if (!enabledHook)
            {
                Log("Enabled Hook 이 꺼져 있어 아무것도 하지 않습니다.");
                return;
            }

            Exit();
        }

        private void HandleReady(LDY_StageSO stage)
        {
            Log("세팅 완료 신호 받음");

            if (!enabledHook)
            {
                Log("Enabled Hook 이 꺼져 있어 아무것도 하지 않습니다.");
                return;
            }

            Enter();
        }

        /// <summary>케이스를 내보낸다. 밖에서도 부를 수 있게 열어둔다.</summary>
        public void Exit()
        {
            // 훑을 때마다 다시 담는다. 그 사이에 케이스가 늘거나 줄었을 수 있다.
            Collect();

            foreach (DLJ_CostAnimation animation in _cases)
            {
                animation.PlayExit();

                Log($"  내보냄 — {animation.name} " +
                    $"(활성 {animation.gameObject.activeInHierarchy}, {animation.TotalExitDuration:0.##}초)");
            }
        }

        /// <summary>케이스를 들인다. 밖에서도 부를 수 있게 열어둔다.</summary>
        public void Enter()
        {
            Collect();

            foreach (DLJ_CostAnimation animation in _cases)
            {
                animation.PlayEntrance();

                Log($"  들임 — {animation.name} " +
                    $"(활성 {animation.gameObject.activeInHierarchy}, {animation.TotalDuration:0.##}초)");
            }
        }

        /// <summary>
        /// 지금 있는 케이스를 담는다.
        ///
        /// 목록을 들고 있지 않고 매번 다시 훑는 이유는, 코스트가 6 이상이 되면
        /// 케이스가 하나 더 생기고 줄면 지워지기 때문이다(DLJ_CostSystem).
        /// 들고 있으면 그 사이에 생긴 케이스만 화면에 남는다.
        /// </summary>
        private int Collect()
        {
            _cases.Clear();

            if (caseRoot != null)
                _cases.AddRange(caseRoot.GetComponentsInChildren<DLJ_CostAnimation>(true));
            else
                _cases.AddRange(FindObjectsByType<DLJ_CostAnimation>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None));

            if (_cases.Count == 0)
            {
                Debug.LogWarning(
                    $"{name}: DLJ_CostAnimation 을 하나도 찾지 못했습니다. " +
                    "케이스 프리팹에 그 컴포넌트가 붙어 있는지 확인하세요.", this);
            }

            return _cases.Count;
        }

        private void Log(string message)
        {
            if (logSteps) Debug.Log($"[{name}] {message}", this);
        }

#if UNITY_EDITOR
        // =========================================================
        // 손으로 눌러보는 자리
        //
        // 클리어까지 가지 않고도 확인할 수 있어야 한다.
        // 여기서 되는데 클리어 때 안 되면 신호 문제,
        // 여기서도 안 되면 케이스 쪽 문제다.
        // =========================================================

        [ContextMenu("테스트: 케이스 내보내기")]
        private void TestExit()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"{name}: 플레이 중에만 됩니다.", this);
                return;
            }

            Exit();
        }

        [ContextMenu("테스트: 케이스 들이기")]
        private void TestEnter()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"{name}: 플레이 중에만 됩니다.", this);
                return;
            }

            Enter();
        }
#endif
    }
}
