using System.Collections;
using TMPro;
using UnityEngine;

namespace _Scripts.LSO.Tutorial
{
    /// <summary>
    /// 화면 위쪽 안내문. **뜨고 지는 것만 안다.**
    ///
    /// 무슨 말을 할지, 언제 바꿀지는 감독이 정한다. 여기서 대본을 들고 있으면
    /// 순서를 아는 곳이 둘이 된다.
    ///
    /// 씬 배선: Canvas 아래 TMP_Text 와 CanvasGroup 을 둔 오브젝트에 붙일 것.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LSO_TutorialBanner : MonoBehaviour
    {
        [Header("연결 (비우면 자식에서 찾는다)")]
        [SerializeField] private TMP_Text label;
        [SerializeField] private CanvasGroup group;

        [Tooltip("안내문 뒤에 표시할 배경 오브젝트. Show 때 켜지고 페이드아웃이 끝나면 꺼집니다. " +
                 "텍스트와 함께 페이드하려면 같은 CanvasGroup 아래의 별도 오브젝트를 연결하세요.")]
        [SerializeField] private GameObject background;

        [Header("연출")]
        [SerializeField, Min(0f)] private float fadeDuration = 0.2f;

        private Coroutine _fade;

        private void Awake()
        {
            if (label == null) label = GetComponentInChildren<TMP_Text>(true);
            if (group == null) group = GetComponent<CanvasGroup>();

            if (label == null)
                Debug.LogError($"{name}: TMP_Text 가 없어 안내문을 띄울 수 없습니다.", this);

            if (background == gameObject ||
                (background != null && transform.IsChildOf(background.transform)))
            {
                Debug.LogError(
                    $"{name}: Background에는 배너 자신이나 배너의 부모를 연결할 수 없습니다. " +
                    "꺼도 이 컴포넌트가 남아 있을 별도 자식/형제 오브젝트를 연결하세요.", this);
                background = null;
            }

            SetAlpha(0f);
            SetBackgroundVisible(false);
        }

        private void OnDisable()
        {
            if (_fade != null) StopCoroutine(_fade);
            _fade = null;
            _skipping = false;

            SetAlpha(0f);
            SetBackgroundVisible(false);
        }

        /// <summary>한 줄 띄운다. 이미 떠 있으면 글자만 바꾼다.</summary>
        public void Show(string line)
        {
            if (label == null) return;

            SetBackgroundVisible(true);

            // 건너뛰려고 누르고 있는 중에 걸음이 넘어갈 수 있다.
            // 그때 글자를 덮으면 "건너뛰는 중..." 이 사라지고 흐려진 채로 대사가 뜬다.
            // 새 대사는 적어만 두고, 손을 뗄 때 그것으로 돌아간다.
            if (_skipping)
            {
                _lineBeforeSkip = line;
                return;
            }

            label.text = line;

            Fade(1f);
        }

        /// <summary>지운다.</summary>
        public void Clear()
        {
            SetSkipProgress(0f);

            Fade(0f);
        }

        // =========================================================
        // 건너뛰기 진행 표시
        //
        // 스페이스를 꾹 누르는 동안 얼마나 눌렀는지 보여준다.
        // 아무 반응이 없으면 눌리고 있는지 알 수 없어서, 되는지 확인하려고 손을 뗀다.
        // =========================================================

        [Header("건너뛰기 표시")]
        [Tooltip("다 눌렀을 때의 안내문 투명도. 흐려질수록 '사라지려 한다'로 읽힌다.")]
        [SerializeField, Range(0f, 1f)] private float skipFadedAlpha = 0.2f;

        [Tooltip("누르는 동안 보여줄 문구. 비우면 글자는 그대로 두고 흐려지기만 한다.\n" +
                 "\n" +
                 "{0} 자리에 남은 비율(%)이 들어간다.")]
        [SerializeField] private string skipMessage = "건너뛰는 중...";

        private string _lineBeforeSkip;

        private bool _skipping;

        /// <summary>
        /// 얼마나 눌렀는지 알린다. 0이면 원래대로 돌린다.
        ///
        /// 진행도를 받기만 하고 시간을 재지 않는다. 얼마나 눌러야 하는지는
        /// 감독이 정하는 값이라, 여기서 또 세면 둘이 어긋난다.
        /// </summary>
        public void SetSkipProgress(float progress)
        {
            if (label == null) return;

            bool skipping = progress > 0f;

            if (skipping && !_skipping)
            {
                // 되돌릴 문구를 적어둔다. 안 적어두면 손을 뗐을 때 안내문이 사라진다.
                _lineBeforeSkip = label.text;

                if (!string.IsNullOrEmpty(skipMessage)) label.text = skipMessage;

                if (_fade != null) StopCoroutine(_fade);
                _fade = null;
            }
            else if (!skipping && _skipping)
            {
                label.text = _lineBeforeSkip;

                SetAlpha(1f);
            }

            _skipping = skipping;

            if (skipping) SetAlpha(Mathf.Lerp(1f, skipFadedAlpha, progress));
        }

        private void Fade(float target)
        {
            if (target > 0f) SetBackgroundVisible(true);

            if (group == null)
            {
                SetAlpha(target);
                if (target <= 0f) SetBackgroundVisible(false);
                return;
            }

            if (_fade != null) StopCoroutine(_fade);

            if (!isActiveAndEnabled || fadeDuration <= 0f)
            {
                SetAlpha(target);
                if (target <= 0f) SetBackgroundVisible(false);
                return;
            }

            _fade = StartCoroutine(Co_Fade(target));
        }

        private IEnumerator Co_Fade(float target)
        {
            float start = group.alpha;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                // Realtime 이다. 유언·보상 연출이 timeScale 을 쥐는 구간에서
                // 스케일 시간으로 재면 안내문만 멈춰 보인다.
                elapsed += Time.unscaledDeltaTime;

                SetAlpha(Mathf.Lerp(start, target, elapsed / fadeDuration));

                yield return null;
            }

            SetAlpha(target);

            if (target <= 0f) SetBackgroundVisible(false);

            _fade = null;
        }

        private void SetAlpha(float value)
        {
            if (group != null) group.alpha = value;
        }

        private void SetBackgroundVisible(bool visible)
        {
            if (background != null && background.activeSelf != visible)
                background.SetActive(visible);
        }
    }
}
