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

        [Header("연출")]
        [SerializeField, Min(0f)] private float fadeDuration = 0.2f;

        private Coroutine _fade;

        private void Awake()
        {
            if (label == null) label = GetComponentInChildren<TMP_Text>(true);
            if (group == null) group = GetComponent<CanvasGroup>();

            if (label == null)
                Debug.LogError($"{name}: TMP_Text 가 없어 안내문을 띄울 수 없습니다.", this);

            SetAlpha(0f);
        }

        /// <summary>한 줄 띄운다. 이미 떠 있으면 글자만 바꾼다.</summary>
        public void Show(string line)
        {
            if (label == null) return;

            label.text = line;

            Fade(1f);
        }

        /// <summary>지운다.</summary>
        public void Clear()
        {
            Fade(0f);
        }

        private void Fade(float target)
        {
            if (group == null)
            {
                SetAlpha(target);
                return;
            }

            if (_fade != null) StopCoroutine(_fade);

            if (!isActiveAndEnabled || fadeDuration <= 0f)
            {
                SetAlpha(target);
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

            _fade = null;
        }

        private void SetAlpha(float value)
        {
            if (group != null) group.alpha = value;
        }
    }
}
