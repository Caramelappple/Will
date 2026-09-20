using _Scripts.LDY.Stage;
using _Scripts.LSO.Stage;
using UnityEngine;

namespace _Scripts.LSO.UI.Panel
{
    /// <summary>
    /// 전투가 끝나면 열려 있던 기물 인포창을 내린다.
    ///
    /// ── 왜 필요한가 ───────────────────────────────────────────
    /// 창은 플레이어가 닫기 전까지 떠 있다. 그래서 창을 열어둔 채 마지막 적을
    /// 잡으면, 판이 돌아가고 보상 상자가 올라오는 내내 죽은 기물의 정보가
    /// 화면을 가린 채 남았다. 그 기물은 이미 판에 없다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// **DLJ_InfoPanel 을 고치지 않고 밖에서 닫는다.** 창은 "무엇을 띄울지"만
    /// 알면 되고, 스테이지가 언제 끝나는지는 알 필요가 없다. 반대로 이 클래스는
    /// 닫으라고 시키는 일만 한다 — 무엇이 떠 있었는지는 묻지 않는다.
    ///
    /// 씬 배선: 아무 관리 오브젝트에나 하나. 인포창은 비워두면 Instance 로 찾는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LSO_InfoPanelAutoClose : MonoBehaviour
    {
        [Header("연결 (비우면 찾는다)")]
        [Tooltip("내릴 인포창. 비워두면 DLJ_InfoPanel.Instance 를 쓴다.")]
        [SerializeField] private DLJ_InfoPanel infoPanel;

        [Header("언제 내릴지")]
        [Tooltip("스테이지를 깼을 때.")]
        [SerializeField] private bool onStageCleared = true;

        [Tooltip("새 판이 세워질 때.\n" +
                 "\n" +
                 "클리어로 이미 내려가 있으면 할 일이 없다. 다시 시작하거나\n" +
                 "판을 건너뛰는 길로 들어왔을 때를 위한 안전망이다.")]
        [SerializeField] private bool onStageLoaded = true;

        private LSO_StageFlow _flow;
        private LDY_StageDirector _stageDirector;

        private void OnEnable()
        {
            if (onStageCleared && LSO_StageFlow.HasInstance)
            {
                _flow = LSO_StageFlow.Instance;
                _flow.StageCleared += HandleStageCleared;
            }

            if (!onStageLoaded) return;

            _stageDirector = FindAnyObjectByType<LDY_StageDirector>();

            if (_stageDirector != null)
                _stageDirector.OnStageLoaded += HandleStageLoaded;
        }

        private void OnDisable()
        {
            if (_flow != null)
            {
                _flow.StageCleared -= HandleStageCleared;
                _flow = null;
            }

            if (_stageDirector != null)
            {
                _stageDirector.OnStageLoaded -= HandleStageLoaded;
                _stageDirector = null;
            }
        }

        private void HandleStageCleared(LDY_StageSO stage)
        {
            Close();
        }

        private void HandleStageLoaded(LDY_StageSO stage)
        {
            Close();
        }

        /// <summary>
        /// 창을 내린다. 이미 내려가 있으면 아무 일도 하지 않는다 —
        /// 그 판단은 창이 한다(DLJ_InfoPanelAnimation.Hide).
        /// </summary>
        private void Close()
        {
            DLJ_InfoPanel panel = infoPanel != null ? infoPanel : DLJ_InfoPanel.Instance;

            if (panel == null)
            {
                Debug.LogWarning(
                    $"{name}: DLJ_InfoPanel을 찾지 못해 인포창을 내리지 못했습니다. " +
                    "열어둔 채로 판이 끝나면 죽은 기물의 정보가 화면에 남습니다.", this);

                return;
            }

            panel.Hide();
        }
    }
}
