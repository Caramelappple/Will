using _Scripts.LDY.Stage;
using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.UI.Transition;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.LSO.Deck.UI
{
    /// <summary>
    /// 덱 구성 화면의 배선을 맡는다.
    ///
    /// 규칙 판정은 LSO_DeckDraft가, 그리기는 두 뷰가 한다.
    /// 여기서 하는 일은 "눌렸다"를 "덱을 바꿔라"로 옮기고 결과를 화면에 알리는 것뿐이다.
    ///
    /// 씬 배선: 도감 스크롤 · 덱 줄 · 확정/초기화 버튼 · 규칙 에셋을 연결할 것.
    /// </summary>
    public class LSO_DeckBuildController : MonoBehaviour
    {
        [Header("규칙")]
        [SerializeField] private LSO_DeckRulesSO rules;

        [Header("화면")]
        [SerializeField] private LSO_PaletteScrollView paletteView;
        [SerializeField] private LSO_DeckStripView deckView;

        [Header("버튼")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button resetButton;

        [Header("연출")]
        [Tooltip("거절됐을 때 떨릴 것. 보통 안내 텍스트에 붙인다. 없어도 된다.")]
        [SerializeField] private LSO_RejectShake rejectShake;

        [Header("씬")]
        [Tooltip("맵에서 예약된 스테이지가 없을 때 쓸 씬. 덱빌드 씬을 단독 실행할 때만 쓰인다.\n" +
                 "평소에는 맵이 고른 스테이지의 SceneName으로 간다.")]
        [SerializeField] private string fallbackSceneName = "BattleScene";

        private LSO_CardPalette _palette;
        private LSO_DeckDraft _draft;

        private void Awake()
        {
            Debug.Log($"<color=cyan>[덱빌드] Awake — {name}</color>", this);
        }

        private void Start()
        {
            Debug.Log(
                $"<color=cyan>[덱빌드] Start — Rules={rules != null} " +
                $"PaletteView={paletteView != null} DeckView={deckView != null}</color>", this);

            // 연결이 빠지면 화면이 그냥 비어 있고 콘솔도 조용하다.
            // 무엇이 안 꽂혔는지 여기서 짚어준다.
            if (rules == null)
                Debug.LogError($"{name}: Rules가 비어 있습니다. 최대 8장 기본값으로 진행합니다.", this);

            if (paletteView == null)
                Debug.LogError($"{name}: Palette View가 비어 있어 도감이 만들어지지 않습니다.", this);

            if (deckView == null)
                Debug.LogError($"{name}: Deck View가 비어 있어 고른 카드가 표시되지 않습니다.", this);

            _palette = LSO_CardPalette.From(OwnedCards);
            _draft = new LSO_DeckDraft(_palette, rules);
            _draft.OnChanged += Redraw;
            _draft.OnRejected += HandleRejected;

            if (paletteView != null)
            {
                paletteView.OnSlotClicked += HandleSlotClicked;
                paletteView.Build(_palette);
            }

            if (deckView != null)
                deckView.OnSlotClicked += HandleSlotClicked;

            if (confirmButton != null)
                confirmButton.onClick.AddListener(HandleConfirm);

            if (resetButton != null)
                resetButton.onClick.AddListener(HandleReset);

            Redraw();
        }

        private void OnDestroy()
        {
            if (_draft != null)
            {
                _draft.OnChanged -= Redraw;
                _draft.OnRejected -= HandleRejected;
            }

            if (paletteView != null)
                paletteView.OnSlotClicked -= HandleSlotClicked;

            if (deckView != null)
                deckView.OnSlotClicked -= HandleSlotClicked;

            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(HandleConfirm);

            if (resetButton != null)
                resetButton.onClick.RemoveListener(HandleReset);
        }

        /// <summary>
        /// 보유 카드. 해금 시스템은 건드리지 않고 읽기만 한다.
        ///
        /// 목록 순서가 곧 도감 칸 번호이므로, 덱을 짜는 동안 이 목록이 바뀌면
        /// 이미 고른 칸이 엉뚱한 카드를 가리키게 된다.
        /// </summary>
        private static System.Collections.Generic.IEnumerable<LSO_CardSO> OwnedCards
        {
            get
            {
                if (ItemLibraryManager.Instance != null)
                    return ItemLibraryManager.Instance.UnlockedPieces;

                Debug.LogError("LSO_DeckBuildController: ItemLibraryManager가 없어 보유 카드를 읽지 못했습니다.");
                return System.Array.Empty<LSO_CardSO>();
            }
        }

        // 도감에서 눌렀든 덱 줄에서 눌렀든 같은 곳으로 모인다.
        // 덱 칸은 자기가 어느 도감 번호에서 왔는지 들고 있으므로 그대로 넘겨주면 된다.
        private void HandleSlotClicked(int slot)
        {
            _draft.Toggle(slot);
        }

        private void HandleReset()
        {
            _draft.Clear();
        }

        // 어느 칸이 왜 안 됐는지가 여기로 들어온다.
        //
        // 카드 쪽에도 알려두는 이유는, 나중에 카드를 흔드는 연출을 붙이고 싶어질 때
        // 이 배선을 다시 찾을 필요가 없게 하기 위해서다. 받는 쪽이 없으면 아무 일도 안 한다.
        private void HandleRejected(int slot, LSO_DeckValidation result)
        {
            rejectShake?.Shake();

            paletteView?.Find(slot)?.PlayReject(result);
        }

        private void HandleConfirm()
        {
            LSO_DeckValidation result = _draft.ValidateForConfirm();
            if (!result.IsValid)
            {
                // 확정을 눌렀는데 아무 반응이 없으면 고장으로 보인다.
                rejectShake?.Shake();
                Debug.Log($"[덱 구성] {result.Message}", this);
                return;
            }

            LSO_RunDeck runDeck = LSO_RunDeck.Instance;
            if (runDeck == null)
            {
                Debug.LogError($"{name}: LSO_RunDeck을 만들 수 없어 덱을 넘기지 못했습니다.", this);
                return;
            }

            runDeck.Commit(_draft.ToCards());

            // SceneManager를 직접 부르지 않는다. 페이드와 중복 요청 차단이 로더에 붙어 있다.
            LSO_SceneLoader.Load(ResolveNextScene());
        }

        /// <summary>
        /// 갈 곳은 맵이 정한다.
        ///
        /// 맵에서 노드를 누르면 LDY_StageSelection에 스테이지가 예약되고 덱빌드 씬으로 넘어온다.
        /// 그 스테이지가 자기 씬 이름을 들고 있으므로 여기서는 그대로 따라가면 된다.
        /// 인스펙터에 씬 이름을 또 적어두면 스테이지마다 씬이 갈릴 때 조용히 엉뚱한 곳으로 간다.
        ///
        /// Consume이 아니라 Pending으로 읽는다. 소비하면 전투 씬의 LDY_StageDirector가
        /// 스테이지를 집지 못해 적이 배치되지 않는다.
        /// </summary>
        private string ResolveNextScene()
        {
            LDY_StageSO pending = LDY_StageSelection.Pending;

            if (pending != null && !string.IsNullOrEmpty(pending.SceneName))
                return pending.SceneName;

            Debug.Log(
                $"[덱 구성] 예약된 스테이지가 없어 기본 씬 '{fallbackSceneName}'으로 갑니다. " +
                $"맵을 거치지 않고 실행하면 정상입니다.", this);

            return fallbackSceneName;
        }

        private void Redraw()
        {
            paletteView?.Refresh(_draft);
            deckView?.Refresh(_draft, _palette);
        }

    }
}
