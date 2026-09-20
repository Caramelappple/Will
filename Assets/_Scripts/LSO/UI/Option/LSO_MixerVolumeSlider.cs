using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace _Scripts.LSO.UI.Option
{
    /// <summary>
    /// 슬라이더 하나가 믹서의 볼륨 하나를 맡는다. Master · Bgm · Sfx 에 하나씩 붙인다.
    ///
    /// ── 왜 슬라이더마다 하나인가 ─────────────────────────────
    /// 셋을 한 컴포넌트가 들고 있으면 슬라이더 참조 세 개를 인스펙터에서 꽂아야
    /// 하고, 하나를 빠뜨리면 그 줄만 조용히 안 먹는다. 슬라이더에 직접 붙이면
    /// 자기 것을 GetComponent 로 찾으므로 꽂을 것이 믹서와 이름뿐이다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// On Value Changed 는 손으로 안 꽂아도 된다. 여기서 코드로 건다 —
    /// 인스펙터로 걸면 프리팹을 고칠 때마다 다시 확인해야 한다.
    ///
    /// 씬 배선: MainMenu 의 Master · Bgm · Sfx 아래 Slider 오브젝트에 하나씩.
    /// Mixer 에 MainMixer 를 꽂고, Exposed Parameter 에 노출한 이름을 적을 것.
    /// </summary>
    [RequireComponent(typeof(Slider))]
    [DisallowMultipleComponent]
    public sealed class LSO_MixerVolumeSlider : MonoBehaviour
    {
        [Header("연결")]
        [Tooltip("볼륨을 바꿀 믹서. Assets/GameModules/System/MainMixer.")]
        [SerializeField] private AudioMixer mixer;

        [Tooltip("믹서에서 노출(Expose)해둔 파라미터 이름. 대소문자까지 똑같아야 한다.\n" +
                 "\n" +
                 "믹서 창에서 그룹을 고르고 인스펙터의 Volume 을 우클릭 → " +
                 "Expose 'Volume (of ...)' to script, 그다음 오른쪽 위 Exposed Parameters 에서 이름을 바꾼다.")]
        [SerializeField] private string exposedParameter = "MasterVolume";

        [Header("범위")]
        [Tooltip("슬라이더를 맨 왼쪽까지 내렸을 때의 데시벨. -80이 유니티의 무음이다.")]
        [SerializeField] private float minDecibel = -80f;

        [Tooltip("슬라이더를 맨 오른쪽까지 올렸을 때의 데시벨. 0이 '원래 크기'다.\n" +
                 "0보다 키우면 원본보다 크게 트는 것이라 찌그러질 수 있다.")]
        [SerializeField] private float maxDecibel = 0f;

        [Tooltip("배선이 없거나 이름이 틀렸을 때 켤 기본값.\n" +
                 "믹서를 못 읽으면 슬라이더를 이 자리에 둔다.")]
        [SerializeField, Range(0f, 1f)] private float fallbackValue = 0.8f;

        private Slider _slider;

        /// <summary>같은 경고를 프레임마다 내지 않는다. 슬라이더를 끌면 수십 번 불린다.</summary>
        private bool _warnedMissingParameter;

        private void Awake()
        {
            _slider = GetComponent<Slider>();
        }

        private void OnEnable()
        {
            _slider.onValueChanged.RemoveListener(HandleValueChanged);
            _slider.onValueChanged.AddListener(HandleValueChanged);

            PullFromMixer();
        }

        private void OnDisable()
        {
            _slider.onValueChanged.RemoveListener(HandleValueChanged);
        }

        /// <summary>
        /// 지금 믹서 값을 읽어 슬라이더를 그 자리에 둔다.
        ///
        /// 반대로 하면(슬라이더 값을 믹서에 밀면) 창을 열 때마다 프리팹에 저장된
        /// 값이 실제 볼륨을 덮어쓴다. 지금 프리팹의 슬라이더는 전부 0이라,
        /// 옵션 창을 여는 순간 소리가 꺼졌을 것이다.
        ///
        /// 읽지 못하면 값을 밀지 않는다 — 못 읽는 상태에서 미는 것은 추측이다.
        /// </summary>
        private void PullFromMixer()
        {
            if (!TryGetDecibel(out float decibel))
            {
                SetSliderWithoutNotify(fallbackValue);
                return;
            }

            SetSliderWithoutNotify(ToLinear(decibel));
        }

        private void HandleValueChanged(float value)
        {
            if (mixer == null)
            {
                WarnOnce("Mixer 가 비어 있어 볼륨을 바꾸지 못합니다.");
                return;
            }

            if (!mixer.SetFloat(exposedParameter, ToDecibel(value)))
            {
                // 조용히 넘기면 슬라이더는 움직이는데 소리는 그대로라, 믹서를 의심하기까지
                // 한참 걸린다. 이름이 틀렸거나 아직 Expose 를 안 한 경우다.
                WarnOnce(
                    $"믹서에 '{exposedParameter}' 라는 노출 파라미터가 없습니다. " +
                    "믹서 창 오른쪽 위 Exposed Parameters 의 이름과 정확히 같은지 확인하세요.");
            }
        }

        private bool TryGetDecibel(out float decibel)
        {
            decibel = maxDecibel;

            if (mixer == null)
            {
                WarnOnce("Mixer 가 비어 있어 지금 볼륨을 읽지 못했습니다.");
                return false;
            }

            if (mixer.GetFloat(exposedParameter, out decibel)) return true;

            WarnOnce(
                $"믹서에 '{exposedParameter}' 라는 노출 파라미터가 없어 지금 볼륨을 읽지 못했습니다.");

            return false;
        }

        /// <summary>
        /// 0~1 을 데시벨로. 사람 귀는 배율이 아니라 비율로 듣기 때문에 로그를 쓴다.
        ///
        /// 그냥 선형으로 넣으면 0.5 로 내려도 거의 안 줄어든 것처럼 들린다 —
        /// -0.5dB 는 귀에 거의 같은 크기다.
        /// </summary>
        private float ToDecibel(float linear)
        {
            if (linear <= 0.0001f) return minDecibel;

            float decibel = Mathf.Log10(linear) * 20f;

            return Mathf.Clamp(decibel, minDecibel, maxDecibel);
        }

        /// <summary>데시벨을 0~1 로. 슬라이더를 지금 볼륨에 맞출 때 쓴다.</summary>
        private float ToLinear(float decibel)
        {
            if (decibel <= minDecibel) return 0f;

            return Mathf.Clamp01(Mathf.Pow(10f, decibel / 20f));
        }

        /// <summary>
        /// 콜백을 돌리지 않고 슬라이더만 옮긴다.
        ///
        /// 그냥 value 를 넣으면 HandleValueChanged 가 불려서 방금 읽은 값을
        /// 믹서에 도로 쓴다. 로그·지수를 오가며 값이 미세하게 깎인다.
        /// </summary>
        private void SetSliderWithoutNotify(float value)
        {
            if (_slider == null) return;

            _slider.SetValueWithoutNotify(Mathf.Clamp01(value));
        }

        private void WarnOnce(string message)
        {
            if (_warnedMissingParameter) return;

            _warnedMissingParameter = true;

            Debug.LogWarning($"{name}: {message} (이 경고는 한 번만 나옵니다)", this);
        }
    }
}
