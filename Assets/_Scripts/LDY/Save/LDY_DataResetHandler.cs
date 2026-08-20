using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.LDY.Save
{
    /// <summary>
    /// 데이터 초기화 버튼을 세이브 삭제에 잇는다. 버튼 오브젝트에 붙여서 쓴다.
    ///
    /// ── 확인 절차에 대하여 ───────────────────────────────────────
    /// 되돌릴 수 없는 작업이라 확인이 필요한데, 이 프로젝트에는 확인 팝업 컴포넌트가 없다.
    /// 팝업을 여기서 새로 만들면 이 클래스가 UI까지 떠안게 되므로 만들지 않았다.
    /// 대신 "한 번 더 누르기"로 최소한의 문턱만 둔다. 첫 클릭은 준비만 하고,
    /// confirmWindowSeconds 안에 다시 눌러야 실제로 지운다.
    ///
    /// 정식 확인 팝업이 생기면 Arm/Disarm 자리를 그 팝업 호출로 바꾸고,
    /// 팝업의 "예"가 PerformReset을 부르게 하면 된다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 배선은 둘 중 하나만 고른다.
    ///   1) addListenerAtStart를 켠 채로 붙이기만 한다(기본값).
    ///   2) 끄고, 버튼 OnClick에 OnResetButtonClicked를 인스펙터에서 끌어다 놓는다.
    /// 둘 다 하면 한 번의 클릭이 두 번 들어오지만, 같은 프레임의 두 번째 호출은 버리므로
    /// 확인 절차가 한 클릭에 통과하지는 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    public class LDY_DataResetHandler : MonoBehaviour
    {
        [Header("버튼")]
        [Tooltip("비워두면 이 오브젝트에 붙은 Button을 쓴다.")]
        [SerializeField] private Button targetButton;

        [Tooltip("끄면 리스너를 붙이지 않는다. 버튼 OnClick을 인스펙터에서 직접 배선할 때 사용.")]
        [SerializeField] private bool addListenerAtStart = true;

        [Header("확인")]
        [Tooltip("첫 클릭 뒤 이 시간(초) 안에 한 번 더 눌러야 실제로 지운다.")]
        [SerializeField] private float confirmWindowSeconds = 3f;

        [Header("표시")]
        [Tooltip("비워두면 버튼 아래의 첫 TMP 텍스트를 쓴다. 없으면 글자는 그대로 두고 동작만 한다.")]
        [SerializeField] private TMP_Text label;

        [SerializeField] private string confirmLabel = "정말 지울까요? 한 번 더";

        [SerializeField] private string doneLabel = "초기화 완료";

        [Tooltip("완료 글자를 띄워둘 시간(초).")]
        [SerializeField] private float doneLabelSeconds = 2f;

        private string _idleLabel;

        /// <summary>준비 상태가 풀리는 시각(unscaledTime). 0이면 준비되지 않은 상태다.</summary>
        private float _armedUntil;

        /// <summary>완료 글자를 되돌릴 시각(unscaledTime). 0이면 되돌릴 것이 없다.</summary>
        private float _restoreLabelAt;

        /// <summary>같은 클릭이 두 경로로 들어오는 것을 거르기 위한 표시.</summary>
        private int _lastClickFrame = -1;

        private void Awake()
        {
            if (targetButton == null) targetButton = GetComponent<Button>();
            if (label == null) label = GetComponentInChildren<TMP_Text>(true);

            if (label != null) _idleLabel = label.text;
        }

        private void Start()
        {
            if (!addListenerAtStart) return;

            if (targetButton == null)
            {
                Debug.LogError(
                    $"[LDY_DataResetHandler] '{name}' 에서 Button을 찾지 못했습니다. " +
                    "버튼 오브젝트에 붙이거나 targetButton을 채워주세요.");
                return;
            }

            targetButton.onClick.AddListener(OnResetButtonClicked);
        }

        private void OnDestroy()
        {
            if (targetButton != null) targetButton.onClick.RemoveListener(OnResetButtonClicked);
        }

        /// <summary>창이 닫혔다 열리면 준비 상태가 남아 있지 않게 한다.</summary>
        private void OnDisable()
        {
            _armedUntil = 0f;
            _restoreLabelAt = 0f;
            RestoreLabel();
        }

        private void Update()
        {
            if (_restoreLabelAt > 0f && Time.unscaledTime >= _restoreLabelAt)
            {
                _restoreLabelAt = 0f;
                RestoreLabel();
                return;
            }

            // 준비 시간이 지나면 글자를 되돌린다. 다음 클릭은 다시 첫 클릭으로 친다.
            if (_armedUntil > 0f && Time.unscaledTime > _armedUntil)
            {
                _armedUntil = 0f;
                RestoreLabel();
            }
        }

        /// <summary>버튼 OnClick이 부르는 자리. 인스펙터에서 직접 배선해도 된다.</summary>
        public void OnResetButtonClicked()
        {
            // 인스펙터 배선과 AddListener가 겹치면 한 클릭이 두 번 들어온다.
            // 그대로 두면 준비와 실행이 한 클릭에 이어져 확인 절차가 없는 것과 같아진다.
            if (_lastClickFrame == Time.frameCount) return;
            _lastClickFrame = Time.frameCount;

            if (_armedUntil > 0f && Time.unscaledTime <= _armedUntil)
            {
                PerformReset();
                return;
            }

            Arm();
        }

        private void Arm()
        {
            _armedUntil = Time.unscaledTime + confirmWindowSeconds;
            _restoreLabelAt = 0f;

            SetLabel(confirmLabel);

            Debug.Log(
                $"[LDY_DataResetHandler] 초기화 대기 중입니다. {confirmWindowSeconds:0.#}초 안에 한 번 더 누르면 " +
                "런과 메타 기록을 모두 지웁니다.");
        }

        /// <summary>
        /// 실제로 지운다. 지우기 전후의 파일 상태를 남겨서 결과를 눈으로 확인할 수 있게 한다.
        /// </summary>
        private void PerformReset()
        {
            _armedUntil = 0f;

            string runPath = PathFor(LDY_SaveService.RunKey);
            string metaPath = PathFor(LDY_SaveService.MetaKey);

            bool runBefore = File.Exists(runPath);
            bool metaBefore = File.Exists(metaPath);

            LDY_SaveService.Instance.ClearAll();

            bool runAfter = File.Exists(runPath);
            bool metaAfter = File.Exists(metaPath);

            string detail =
                $"  run  : {Describe(runBefore, runAfter)}\n" +
                $"    {runPath}\n" +
                $"  meta : {Describe(metaBefore, metaAfter)}\n" +
                $"    {metaPath}";

            if (runAfter || metaAfter)
            {
                Debug.LogError(
                    "[LDY_DataResetHandler] 초기화 실패 — 파일이 남아 있습니다.\n" + detail + "\n" +
                    "  위쪽 LDY_FileSaveRepository 로그를 확인하세요.");

                SetLabel("초기화 실패 — 콘솔 확인");
                _restoreLabelAt = Time.unscaledTime + doneLabelSeconds;
                return;
            }

            Debug.Log("[LDY_DataResetHandler] 초기화 완료 — 런과 메타를 모두 지웠습니다.\n" + detail);

            SetLabel(doneLabel);
            _restoreLabelAt = Time.unscaledTime + doneLabelSeconds;
        }

        private static string Describe(bool before, bool after)
        {
            if (!before) return "원래 없었음";

            return after ? "❌ 지우지 못함" : "지움";
        }

        /// <summary>
        /// 저장소가 키에 .json을 붙여 persistentDataPath 아래에 두는 규약을 그대로 따라 짚는다.
        /// 로그 표시용이므로 규약이 바뀌면 여기도 같이 고칠 것.
        /// </summary>
        private static string PathFor(string key)
        {
            return Path.Combine(Application.persistentDataPath, key + ".json");
        }

        private void SetLabel(string text)
        {
            if (label != null) label.text = text;
        }

        private void RestoreLabel()
        {
            if (label != null && _idleLabel != null) label.text = _idleLabel;
        }
    }
}
