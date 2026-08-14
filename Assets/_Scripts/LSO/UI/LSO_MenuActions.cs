using System.Collections.Generic;
using _Scripts.LSO.CoreLib;
using _Scripts.LSO.UI.Transition;
using UnityEngine;

namespace _Scripts.LSO.UI
{
    /// <summary>
    /// 메뉴 버튼들이 부르는 창구를 한곳에 모은 파사드.
    ///
    /// ── 이 클래스의 규칙 ─────────────────────────────────────────
    /// 1. 모든 메서드는 다른 시스템에 넘기기만 한다. 여기서 직접 처리하지 않는다.
    /// 2. 필드는 "무엇을 열지" 같은 참조만 둔다.
    ///    bool이나 int 같은 상태가 생기면 파사드가 아니라 컨트롤러로 변질된 것이다.
    ///    그때는 그 상태를 책임질 클래스를 따로 만들어 옮길 것.
    /// 3. 한 메서드가 세 줄을 넘어가면 그 로직은 여기 있을 게 아니다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 버튼 OnClick을 인스펙터에서 연결하면 메서드 이름이 문자열로 저장된다.
    /// 이름을 바꾸면 컴파일은 통과하고 씬 연결만 조용히 끊기니 주의할 것.
    /// </summary>
    public class LSO_MenuActions : MonoBehaviour
    {
        [Header("창")]
        [Tooltip("메인 화면. LSO_Panel이나 LSO_FadePanel이 붙어 있으면 그 연출을 탄다.")]
        [SerializeField] private GameObject mainPanel;

        [SerializeField] private GameObject settingsPanel;

        [Tooltip("음량 조절 창. 설정 창 안에서 한 단계 더 들어가는 구조를 가정한다.")]
        [SerializeField] private GameObject audioPanel;

        [Tooltip("해상도·화질 창. 오디오와 같은 단계에 있다.")]
        [SerializeField] private GameObject videoPanel;

        [SerializeField] private GameObject creditsPanel;

        [Header("제목")]
        [Tooltip("화면 위쪽 게임 제목. LSO_FadePanel을 붙여두면 켜고 끌 때 페이드된다.")]
        [SerializeField] private GameObject titleObject;

        [Tooltip("여기 등록된 창이 열려 있을 때만 제목이 보인다.\n" +
                 "비워두면 항상 보인다. 창을 추가할 때 이 목록에 넣는 것만 기억하면 된다.")]
        [SerializeField] private List<GameObject> panelsWithTitle = new();

        [Header("씬")]
        [Tooltip("Build Settings에 등록된 이름이어야 한다.")]
        [SerializeField] private string gameSceneName = "BattleScene";

        [SerializeField] private string titleSceneName = "TitleScene";

        // ── 씬 ──────────────────────────────────────────────

        public void StartGame()
        {
            //LDY_RunEntryState.IsStartingNewRun = true;
            LSO_SceneLoader.Load(gameSceneName);
        }

        public void GoToTitle() => LSO_SceneLoader.Load(titleSceneName);

        public void RestartScene() => LSO_SceneLoader.Reload();

        // ── 창 ──────────────────────────────────────────────

        public void OpenSettings() => Swap(mainPanel, settingsPanel);

        public void CloseSettings() => Swap(settingsPanel, mainPanel);

        public void OpenAudio() => Swap(settingsPanel, audioPanel);

        public void CloseAudio() => Swap(audioPanel, settingsPanel);

        public void OpenVideo() => Swap(settingsPanel, videoPanel);

        public void CloseVideo() => Swap(videoPanel, settingsPanel);

        public void OpenCredits() => Swap(mainPanel, creditsPanel);

        public void CloseCredits() => Swap(creditsPanel, mainPanel);

        /// <summary>
        /// 위 조합에 없는 창을 열 때. 버튼 OnClick의 인자 칸에 대상을 끌어다 놓으면 된다.
        /// </summary>
        public void OpenPanel(GameObject panel)
        {
            LSO_PanelOps.Open(panel);
            ApplyTitle(panel);
        }

        public void ClosePanel(GameObject panel) => LSO_PanelOps.Close(panel);

        public void TogglePanel(GameObject panel) => LSO_PanelOps.Toggle(panel);

        // ── 종료 ────────────────────────────────────────────

        public void QuitGame() => LSO_Quit.Request();

        // ── 내부 ────────────────────────────────────────────

        /// <summary>씬에 열린 채로 저장된 창을 보고 제목을 맞춘다.</summary>
        private void Start()
        {
            ApplyTitle(FindOpenPanel());
        }

        /// <summary>
        /// 여는 쪽을 먼저 처리한다.
        /// 닫기가 먼저면 대상이 자식일 때 같이 꺼지고, 아무 창도 없는 한 프레임이 생긴다.
        /// </summary>
        private void Swap(GameObject from, GameObject to)
        {
            LSO_PanelOps.Open(to);
            LSO_PanelOps.Close(from);
            ApplyTitle(to);
        }

        /// <summary>지금 열린 창이 제목을 띄우는 창인지 보고 켜거나 끈다.</summary>
        private void ApplyTitle(GameObject openedPanel)
        {
            if (titleObject == null) return;

            bool show = panelsWithTitle.Count == 0 || panelsWithTitle.Contains(openedPanel);

            // LSO_PanelOps를 거치므로 제목에 LSO_FadePanel을 붙이면 페이드로 나타난다.
            LSO_PanelOps.SetOpen(titleObject, show);
        }

        private GameObject FindOpenPanel()
        {
            GameObject[] known = { mainPanel, settingsPanel, audioPanel, videoPanel, creditsPanel };

            foreach (GameObject panel in known)
            {
                if (panel != null && panel.activeSelf) return panel;
            }

            return null;
        }
    }
}
