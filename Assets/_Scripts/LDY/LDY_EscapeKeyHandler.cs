using System;
using _Scripts.LSO.Will;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using _Scripts.LSO.UI.Panel;

namespace _Scripts.LDY
{
    /// <summary>
    /// ESC 한 키로 "지금 상황에서 뒤로 가기"를 처리한다.
    ///
    /// 우선순위는 위에서 아래로 한 단계씩만 내려간다. 한 번 누르면 한 가지만 한다.
    ///   0. 계승 대기·유언 선택·이동/공격 연출 중이면 아무것도 하지 않는다
    ///   1. 카드 배치 중이면 배치를 취소한다
    ///   2. escapeTargets 중 열려 있는 첫 항목을 닫는다
    ///   3. 닫을 게 없으면 설정 창을 연다
    ///
    /// ── Time.timeScale을 건드리지 않는다 ────────────────────────
    /// DLJ_WillSystem과 DLJ_SuccessionSystem이 각자 연출 직전의 timeScale을
    /// 저장해뒀다가 끝나면 되돌린다. 여기서 0으로 만들면 그 "원래 값"이 0으로
    /// 저장돼 연출이 끝난 뒤에도 게임이 멈춘 채로 남는다.
    /// 그래서 이 컴포넌트는 창을 여닫기만 하고 시간은 손대지 않는다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 씬 배선: 아무 오브젝트에나 붙이고 escapeTargets에 닫을 창을 우선순위 순으로 넣는다.
    /// 비워둔 참조는 그 단계를 건너뛰므로, 전투 씬처럼 설정 창이 없는 곳에서는
    /// menuActions를 비워둔 채로 배치 취소·창 닫기만 쓰면 된다.
    /// </summary>
    [DisallowMultipleComponent]
    public class LDY_EscapeKeyHandler : MonoBehaviour
    {
        /// <summary>
        /// ESC로 닫을 창 하나. 닫는 방법을 아래 순서로 찾는다.
        ///   1. returnButton — 그 버튼을 누른 것과 똑같이 동작한다 (권장)
        ///   2. onEscape — 버튼이 없는 창을 위한 직접 배선
        ///   3. 둘 다 비었으면 LSO_PanelOps.Close(panel)
        ///
        /// returnButton을 권하는 이유: LSO_MenuActions의 창들은 그냥 꺼지는 게 아니라
        /// 부모 창으로 되돌아간다(설정→메인, 오디오→설정). 그냥 Close만 하면
        /// 돌아갈 창이 열리지 않아 빈 화면이 남는다.
        /// 창 안의 뒤로가기 버튼에는 이미 그 배선(CloseSettings 등)이 되어 있으므로,
        /// 그 버튼을 끌어다 놓으면 같은 배선을 두 번 할 필요도, 틀릴 여지도 없다.
        /// </summary>
        [Serializable]
        public class EscapeTarget
        {
            [Tooltip("열려 있는지 판단할 창. LSO_IPanel이 붙어 있으면 IsOpen을, 없으면 activeSelf를 본다.")]
            public GameObject panel;

            [Tooltip("이 창의 뒤로가기 버튼. 끌어다 놓으면 ESC가 그 버튼을 누른 것과 같아진다.")]
            public Button returnButton;

            [Tooltip("뒤로가기 버튼이 없는 창에만 쓴다. 비워두면 LSO_PanelOps.Close(panel).")]
            public UnityEvent onEscape;
        }

        [Header("닫을 창 (위에 있을수록 우선)")]
        [Tooltip("여러 개가 동시에 열려 있으면 목록에서 먼저 나오는 것 하나만 닫는다.\n" +
                 "안쪽 창(오디오·해상도)을 바깥 창(설정)보다 위에 둘 것.")]
        [SerializeField] private EscapeTarget[] escapeTargets = Array.Empty<EscapeTarget>();

        [Header("설정 열기")]
        [Tooltip("닫을 창이 하나도 없을 때 OpenSettings()를 부른다. 비우면 이 단계를 건너뛴다.")]
        [SerializeField] private LSO_MenuActions menuActions;

        [Header("전투 씬 참조 (없으면 생략)")]
        [Tooltip("배치 중 ESC로 배치를 취소한다. 우클릭 취소와 같은 동작이다.")]
        [SerializeField] private LDY_CardPlacer cardPlacer;

        [Tooltip("이동 연출 중에는 ESC를 무시한다.")]
        [SerializeField] private LDY_MoveSystem moveSystem;

        [Tooltip("공격 연출 중에는 ESC를 무시한다.")]
        [SerializeField] private LDY_AttackSystem attackSystem;

#if UNITY_EDITOR
        /// <summary>
        /// "LDY/ESC 키 배선" 도구가 배선을 채워 넣는 자리. 런타임 코드는 부르지 않는다.
        ///
        /// 손으로 채워도 되지만 On Escape 칸에 함수를 빠뜨리는 실수가 잦아서,
        /// 씬을 훑어 자동으로 채우는 쪽을 기본으로 둔다.
        /// private 필드를 도구에서 건드리려고 리플렉션을 쓰는 것보다 이 편이 낫다.
        /// </summary>
        public void EditorApplyWiring(
            EscapeTarget[] targets,
            LSO_MenuActions actions,
            LDY_CardPlacer placer,
            LDY_MoveSystem move,
            LDY_AttackSystem attack)
        {
            escapeTargets = targets ?? Array.Empty<EscapeTarget>();
            menuActions = actions;
            cardPlacer = placer;
            moveSystem = move;
            attackSystem = attack;
        }
#endif

        private void Update()
        {
            // 키보드가 없는 환경(모바일 등)에서도 조용히 넘어가야 한다.
            if (Keyboard.current == null) return;
            if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;

            // 계승 대기 중에는 timeScale이 0이지만 Keyboard.current는 unscaled 입력이라
            // 이 프레임이 그대로 돌아온다. 가드가 없으면 대기 중에 창이 열려버린다.
            if (IsBlocked()) return;

            // 배치 취소를 보드 잠금보다 앞에 둔다.
            // 잠금은 "손패를 고른 뒤 유언 창이 사라질 때까지 보드를 막는" 것이고,
            // 배치 입력은 LDY_CardPlacer가 따로 처리하므로 잠금의 대상이 아니다.
            if (TryCancelPlacement()) return;

            if (LSO_WillSelection.IsBoardInteractionLocked) return;

            if (TryCloseOpenTarget()) return;

            OpenSettings();
        }

        /// <summary>
        /// 플레이어의 답이나 연출을 기다리는 중인지. 하나라도 걸리면 ESC는 아무 일도 하지 않는다.
        /// 여기서 막는 편이, 창을 열어놓고 그 뒤에서 연출이 끝나기를 기다리는 것보다 안전하다.
        /// </summary>
        private bool IsBlocked()
        {
            if (DLJ_SuccessionSystem.IsWaitingForSuccessionTarget) return true;
            if (LSO_WillSelection.IsSelecting) return true;

            // 전투 씬이 아니면 참조가 비어 있다. 그때는 이 조건이 없는 것으로 친다.
            if (moveSystem != null && moveSystem.IsBusy) return true;
            if (attackSystem != null && attackSystem.IsBusy) return true;

            return false;
        }

        private bool TryCancelPlacement()
        {
            if (cardPlacer == null || !cardPlacer.IsPlacing) return false;

            cardPlacer.CancelPlacement();
            return true;
        }

        /// <summary>열려 있는 첫 창을 닫는다. 닫을 게 있었으면 true.</summary>
        private bool TryCloseOpenTarget()
        {
            foreach (EscapeTarget target in escapeTargets)
            {
                if (target == null || target.panel == null) continue;
                if (!IsOpen(target.panel)) continue;

                Close(target);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 창을 닫는다. 뒤로가기 버튼이 있으면 그 버튼을 누른 것과 똑같이 처리한다.
        /// 버튼의 onClick에는 이미 돌아갈 창까지 포함한 배선이 되어 있다.
        /// </summary>
        private static void Close(EscapeTarget target)
        {
            if (target.returnButton != null)
            {
                // 버튼이 꺼져 있거나 비활성이면 눌리지 않는 상황이므로 ESC도 따르지 않는다.
                if (!target.returnButton.IsInteractable() ||
                    !target.returnButton.gameObject.activeInHierarchy)
                    return;

                target.returnButton.onClick.Invoke();
                return;
            }

            if (HasCall(target.onEscape))
            {
                target.onEscape.Invoke();
                return;
            }

            LSO_PanelOps.Close(target.panel);
        }

        /// <summary>
        /// UnityEvent에 실제로 부를 게 들어 있는지.
        ///
        /// GetPersistentEventCount만 보면 안 된다.
        /// 인스펙터에서 + 를 눌러 칸만 만들고 함수를 안 고르면(No Function)
        /// 개수는 1인데 Invoke는 아무 일도 하지 않는다.
        /// 그 상태를 "처리했다"로 치면 ESC가 조용히 먹통이 된다.
        /// </summary>
        private static bool HasCall(UnityEvent unityEvent)
        {
            if (unityEvent == null) return false;

            for (int i = 0; i < unityEvent.GetPersistentEventCount(); i++)
            {
                if (unityEvent.GetPersistentTarget(i) == null) continue;
                if (string.IsNullOrEmpty(unityEvent.GetPersistentMethodName(i))) continue;

                return true;
            }

            return false;
        }

        /// <summary>
        /// 창이 열려 있는지. LSO_IPanel이 있으면 그쪽 판단을 따른다.
        ///
        /// LSO_FadePanel은 닫히는 연출이 끝날 때까지 오브젝트가 켜져 있으므로
        /// activeSelf만 보면 이미 닫기로 한 창을 또 닫으려 든다.
        /// </summary>
        private static bool IsOpen(GameObject panel)
        {
            // 꺼져 있는 오브젝트라도 컴포넌트 참조는 살아 있다.
            LSO_IPanel handler = panel.GetComponent<LSO_IPanel>();

            return handler != null ? handler.IsOpen : panel.activeSelf;
        }

        private void OpenSettings()
        {
            if (menuActions == null) return;

            menuActions.OpenSettings();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 인스펙터에서 배선이 반쯤 된 항목을 잡아낸다.
        /// 함수를 안 고른 onEscape 칸은 컴파일도 통과하고 에러도 없이 ESC만 먹통이 된다.
        /// 그 상태를 눈으로 알아채기 어려워서 여기서 짚어준다.
        /// </summary>
        private void OnValidate()
        {
            foreach (EscapeTarget target in escapeTargets)
            {
                if (target == null || target.panel == null) continue;
                if (target.returnButton != null) continue;
                if (target.onEscape == null) continue;
                if (target.onEscape.GetPersistentEventCount() == 0) continue; // 비어 있으면 Close로 처리된다
                if (HasCall(target.onEscape)) continue;

                Debug.LogWarning(
                    $"{name}: '{target.panel.name}'의 On Escape에 함수가 지정되지 않았습니다(No Function). " +
                    "ESC를 눌러도 아무 일도 일어나지 않습니다. " +
                    "그 창의 뒤로가기 버튼을 Return Button 칸에 끌어다 놓거나, 칸을 지우세요.",
                    this);
            }
        }
#endif
    }
}
