using _Scripts.LDY.Save;
using _Scripts.LSO.UI.Text;
using _Scripts.LSO.UI.Transition;
using _Scripts.LSO.Will;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts.LDY
{
    /// <summary>
    /// 전투·맵 씬에서 ESC를 전담한다. 벅샷 룰렛과 같은 규칙이다.
    ///   배치 중이면 → 배치를 취소한다
    ///   그 외에는  → 누르고 있는 동안 안내 문구가 차오르고, 다 차면 저장하고 메인 메뉴로
    ///
    /// ── 일시정지가 없다 ────────────────────────────────────────
    /// 한때 짧게 누르면 화면을 덮는 정지 오버레이가 떴다. 지금은 없앴다.
    /// 정지 화면이 보드를 가리는 것에 비해 얻는 것이 없었고, timeScale을 건드리는
    /// 코드가 하나 줄어드는 편이 낫다 — DLJ_WillSystem과 DLJ_SuccessionSystem이
    /// 각자 연출 직전의 timeScale을 저장했다 되돌리기 때문에, 여기서 0으로 만들면
    /// 그 "원래 값"이 0으로 저장돼 연출이 끝난 뒤에도 게임이 멈춘 채로 남는다.
    ///
    /// 그래서 이 컴포넌트는 시간을 아예 손대지 않는다. LDY_GameplayPause를 쓰지 않는다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// ── 프로젝트에서 ESC를 보는 곳 ──────────────────────────────
    /// 전투·맵은 여기, 메뉴 씬은 LDY_EscapeKeyHandler다.
    /// 한 씬에 둘 다 두면 한 번의 ESC를 두 곳이 처리한다. 씬마다 하나만 둘 것.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 씬 배선: 상단 메뉴 "LDY > ESC 안내 문구 만들기"가 채워준다.
    /// </summary>
    [DisallowMultipleComponent]
    public class LDY_GameplayEscapeHandler : MonoBehaviour
    {
        [Header("전투 씬 참조 (맵 씬에서는 비워둔다)")]
        [Tooltip("배치 중 ESC로 배치를 취소한다. 우클릭 취소와 같은 동작이다.")]
        [SerializeField] private LDY_CardPlacer cardPlacer;

        [Tooltip("이동 연출 중에는 나가기를 받지 않는다.")]
        [SerializeField] private LDY_MoveSystem moveSystem;

        [Tooltip("공격 연출 중에는 나가기를 받지 않는다.")]
        [SerializeField] private LDY_AttackSystem attackSystem;

        [Header("메인 메뉴로 나가기")]
        [Tooltip("Build Settings에 등록된 이름이어야 한다.")]
        [SerializeField] private string titleSceneName = "LSO_UI Scene";

        [Tooltip("ESC를 이만큼 누르고 있으면 저장 후 메인 메뉴로 나간다.")]
        [SerializeField, Min(0.1f)] private float longPressSeconds = 1.5f;

        [Header("화면 표시")]
        [Tooltip("누르고 있는 동안 뜨는 안내 문구. 진행률만큼 글자가 차오른다.\n" +
                 "비워두면 표시만 생략되고 나가기는 그대로 동작한다.")]
        [SerializeField] private LSO_HoldTextPrompt prompt;

        private readonly LDY_EscapeHoldTimer _hold = new();

        /// <summary>
        /// 이번 누름을 배치 취소로 이미 써버렸는지.
        ///
        /// 이게 없으면 배치를 취소한 뒤에도 손을 떼지 않는 한 누적이 계속 쌓여
        /// "취소하려다 메인 메뉴로 튕겨나간다". 한 번 누르면 한 가지만 한다.
        /// </summary>
        private bool _pressConsumed;

#if UNITY_EDITOR
        /// <summary>
        /// "LDY/ESC 안내 문구 만들기" 도구가 배선을 채워 넣는 자리. 런타임 코드는 부르지 않는다.
        ///
        /// titleSceneName과 longPressSeconds는 일부러 받지 않는다.
        /// 인스펙터에서 조절하는 값이라 도구가 덮어쓰면 튜닝이 매번 날아간다.
        /// </summary>
        public void EditorApplyWiring(
            LDY_CardPlacer placer,
            LDY_MoveSystem move,
            LDY_AttackSystem attack,
            LSO_HoldTextPrompt holdPrompt)
        {
            cardPlacer = placer;
            moveSystem = move;
            attackSystem = attack;
            prompt = holdPrompt;
        }
#endif

        private void Update()
        {
            // 키보드가 없는 환경(모바일 등)에서도 조용히 넘어가야 한다.
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            // 인스펙터에서 값을 바꾸면 플레이 중에도 바로 반영된다.
            _hold.Threshold = longPressSeconds;

            // 배치 취소를 가장 앞에 둔다. 배치 중 ESC는 언제나 "지금 하려던 걸 무른다"여야 한다.
            if (keyboard.escapeKey.wasPressedThisFrame && TryCancelPlacement())
            {
                _pressConsumed = true;
                return;
            }

            if (keyboard.escapeKey.isPressed)
            {
                HandleHeld();
                return;
            }

            if (keyboard.escapeKey.wasReleasedThisFrame)
                ResetHold();
        }

        /// <summary>누르고 있는 동안. 다 차면 그 자리에서 메인 메뉴로 나간다.</summary>
        private void HandleHeld()
        {
            if (_pressConsumed) return;

            // 연출이나 플레이어의 답을 기다리는 중이면 문구도 띄우지 않는다.
            // 띄워놓고 안 나가면 "눌렀는데 왜 안 되지"가 된다.
            if (IsBlocked())
            {
                HidePrompt();
                return;
            }

            ShowPrompt();

            // 정지가 없어졌어도 unscaled를 그대로 쓴다. 유언·계승 연출이 timeScale을
            // 0으로 눕히는 구간이 있어서, scaled로 재면 그 동안 진행이 멈춘다.
            if (_hold.Advance(Time.unscaledDeltaTime))
            {
                GoToTitle();
                return;
            }

            SetProgress(_hold.Progress);
        }

        private bool TryCancelPlacement()
        {
            if (cardPlacer == null || !cardPlacer.IsPlacing) return false;

            cardPlacer.CancelPlacement();
            return true;
        }

        /// <summary>
        /// 플레이어의 답이나 연출을 기다리는 중인지. 하나라도 걸리면 나가기를 받지 않는다.
        /// timeScale이 0인 대기 중에도 Keyboard.current는 unscaled 입력이라 이 프레임이 그대로 돌아온다.
        /// </summary>
        private bool IsBlocked()
        {
            if (DLJ_SuccessionSystem.IsWaitingForSuccessionTarget) return true;
            if (LSO_WillSelection.IsSelecting) return true;

            // 맵 씬에서는 참조가 비어 있다. 그때는 이 조건이 없는 것으로 친다.
            if (moveSystem != null && moveSystem.IsBusy) return true;
            if (attackSystem != null && attackSystem.IsBusy) return true;

            return false;
        }

        private void GoToTitle()
        {
            ResetHold();

            LDY_SaveService.Instance.SaveRun();

            if (string.IsNullOrEmpty(titleSceneName))
            {
                Debug.LogWarning($"{name}: Title Scene Name이 비어 있어 메인 메뉴로 나가지 못했습니다.", this);
                return;
            }

            LSO_SceneLoader.Load(titleSceneName);
        }

        private void ResetHold()
        {
            _hold.Reset();
            _pressConsumed = false;

            HidePrompt();
        }

        private void ShowPrompt()
        {
            if (prompt != null) prompt.Show();
        }

        private void HidePrompt()
        {
            if (prompt != null) prompt.Hide();
        }

        private void SetProgress(float progress)
        {
            if (prompt != null) prompt.SetProgress(progress);
        }

        /// <summary>
        /// 꺼질 때 문구를 들고 가지 않는다. 씬을 넘길 때 뜬 채로 남으면
        /// 다음 씬에서 왜 떠 있는지 알 수 없다.
        /// </summary>
        private void OnDisable()
        {
            ResetHold();
        }
    }
}
