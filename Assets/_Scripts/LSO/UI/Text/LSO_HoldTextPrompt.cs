using DG.Tweening;
using TMPro;
using UnityEngine;

namespace _Scripts.LSO.UI.Text
{
    /// <summary>
    /// "누르고 있으면 무엇이 일어난다"를 알리는 안내 문구 하나.
    ///
    /// 창도 아니고 배경도 없다. 화면 위에 글자만 뜬다.
    /// 진행률은 글자가 왼쪽부터 선명해지는 것으로 보여준다 — 게이지가 따로 없어도
    /// 얼마나 남았는지 읽힌다.
    ///
    /// 글자가 차오르는 방식이라 라벨이 둘이다.
    ///   Base Label  늘 흐리게 깔려 있다. 문구 전체를 읽을 수 있게 한다
    ///   Fill Label  같은 자리에 겹쳐 있고, 진행률만큼만 보인다
    ///
    /// 한 라벨에 리치 텍스트로 색을 나누는 방법도 있지만, 글자마다 태그를 끼워 넣게 되어
    /// 문구에 태그가 이미 들어 있으면 어긋난다. 두 장을 겹치는 편이 단순하다.
    ///
    /// 문구는 이쪽이 양쪽 라벨에 똑같이 넣는다. 인스펙터에서 따로 적게 두면
    /// 한쪽만 고쳐놓고 왜 안 바뀌는지 찾게 된다.
    ///
    /// ESC 나가기 말고 "BACKSPACE 시 취소" 같은 안내에도 그대로 쓸 수 있다.
    ///
    /// 씬 배선: 캔버스 아래에 빈 오브젝트를 두고 이것을 붙인 뒤,
    /// TMP 라벨 두 개를 자식으로 겹쳐 놓고 연결한다.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [DisallowMultipleComponent]
    public class LSO_HoldTextPrompt : MonoBehaviour
    {
        [Header("라벨")]
        [Tooltip("늘 흐리게 깔리는 바탕. 문구 전체가 여기 보인다.")]
        [SerializeField] private TMP_Text baseLabel;

        [Tooltip("바탕 위에 겹치는 선명한 글자. 진행률만큼만 보인다.\n" +
                 "Base Label과 같은 자리·같은 폰트·같은 크기여야 겹쳐 보인다.")]
        [SerializeField] private TMP_Text fillLabel;

        [Header("문구")]
        [Tooltip("양쪽 라벨에 똑같이 들어간다. 라벨에 직접 적은 글자는 덮어쓴다.")]
        [TextArea]
        [SerializeField] private string message = "ESC를 누르고 있으면 나갑니다";

        [Header("등장")]
        [Tooltip("뜨고 사라지는 데 걸리는 시간(초). 0이면 즉시.")]
        [SerializeField, Min(0f)] private float fadeDuration = 0.12f;

        [Tooltip("켜면 timeScale이 0이어도 뜨고 사라진다.\n" +
                 "정지 중에 안내를 띄울 일이 있으면 켜둘 것.")]
        [SerializeField] private bool ignoreTimeScale = true;

        private CanvasGroup _group;
        private Tween _fade;

        // 문구의 글자 수. maxVisibleCharacters에 넣을 값의 기준이다.
        private int _characterCount;

        /// <summary>지금 떠 있는지.</summary>
        public bool IsShown { get; private set; }

        /// <summary>
        /// 보여줄 문구. 코드에서 상황에 따라 바꿔 끼울 수 있다.
        /// 글자 수가 달라지므로 진행률 기준도 여기서 다시 잡는다.
        /// </summary>
        public string Message
        {
            get => message;
            set
            {
                message = value;
                ApplyMessage();
            }
        }

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();

            if (baseLabel == null || fillLabel == null)
            {
                Debug.LogWarning(
                    $"{name}: 라벨이 비어 있어 안내 문구가 뜨지 않습니다. " +
                    "Base Label과 Fill Label을 연결하세요.", this);
            }

            ApplyMessage();

            // 처음에는 안 보이는 것이 맞다. 오브젝트를 꺼두지 않는 이유는
            // 꺼진 오브젝트에서는 Awake가 돌지 않아 이 초기화 자체를 못 하기 때문이다.
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;

            SetProgress(0f);
        }

        /// <summary>문구를 띄운다. 이미 떠 있으면 아무 일도 하지 않는다.</summary>
        public void Show()
        {
            if (IsShown) return;

            IsShown = true;

            FadeTo(1f);
        }

        /// <summary>
        /// 문구를 내린다. 진행률도 0으로 되돌린다.
        ///
        /// 진행률을 남겨두면 다음에 눌렀을 때 지난번 길이부터 시작한 것처럼 보인다.
        /// </summary>
        public void Hide()
        {
            if (!IsShown) return;

            IsShown = false;

            SetProgress(0f);

            FadeTo(0f);
        }

        /// <summary>
        /// 0~1. 왼쪽부터 이만큼 선명해진다.
        ///
        /// 글자 단위라 짧은 문구에서는 뚝뚝 끊겨 보인다. 그럴 때는 문구를 늘리는 편이
        /// 알파를 함께 건드리는 것보다 낫다 — 두 가지가 동시에 변하면 무엇이 진행률인지 흐려진다.
        /// </summary>
        public void SetProgress(float progress)
        {
            if (fillLabel == null) return;

            int visible = Mathf.RoundToInt(Mathf.Clamp01(progress) * _characterCount);

            fillLabel.maxVisibleCharacters = visible;
        }

        private void ApplyMessage()
        {
            if (baseLabel != null) baseLabel.text = message;

            if (fillLabel == null) return;

            fillLabel.text = message;

            // 글자 수는 리치 텍스트 태그를 뺀 값이어야 한다.
            // text.Length를 쓰면 태그까지 세어 진행률이 실제보다 느리게 찬다.
            fillLabel.ForceMeshUpdate();

            _characterCount = fillLabel.textInfo.characterCount;

            fillLabel.maxVisibleCharacters = 0;
        }

        private void FadeTo(float alpha)
        {
            _fade?.Kill();

            if (fadeDuration <= 0f)
            {
                _group.alpha = alpha;
                return;
            }

            _fade = _group.DOFade(alpha, fadeDuration)
                .SetUpdate(ignoreTimeScale)
                .SetLink(gameObject);
        }

        private void OnDisable()
        {
            // 뜬 채로 꺼지면 다시 켰을 때 알파 1로 남아 있다.
            _fade?.Kill();
            _fade = null;

            IsShown = false;

            if (_group != null) _group.alpha = 0f;

            SetProgress(0f);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (baseLabel != null && fillLabel != null && baseLabel == fillLabel)
            {
                Debug.LogWarning(
                    $"{name}: Base Label과 Fill Label이 같은 라벨입니다. " +
                    "글자가 차오르는 대신 문구가 통째로 안 보이게 됩니다.", this);
            }

            if (Application.isPlaying) ApplyMessage();
        }
#endif
    }
}
