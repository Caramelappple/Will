using DG.Tweening;
using UnityEngine;

namespace _Scripts.LSO.Effect
{
    /// <summary>
    /// 촛불처럼 라이트를 흔든다. 밝기와 미세한 위치 흔들림 외의 것은 건드리지 않는다.
    ///
    /// 기획서: "촛불의 불꽃은 아주 미세하게 흔들리며 이에 따라 게임판과 기물의 밝기,
    ///          그림자의 위치가 조금씩 변한다."
    ///
    /// 그래서 두 가지를 함께 흔든다. 밝기만 흔들면 그림자는 진해졌다 옅어지기만 하고
    /// 자리는 그대로라, 촛불이 아니라 조광기를 돌리는 것처럼 보인다.
    /// 그림자가 움직이려면 광원이 실제로 조금 움직여야 한다.
    ///
    /// 사인파로 오르내리게 하지 않는다. 규칙적인 맥박은 촛불이 아니라 숨쉬는 네온으로 보인다.
    /// 실제 촛불은 다음 밝기와 도달 시간이 매번 다르므로, 한 구간이 끝날 때마다
    /// 목표와 시간을 새로 뽑아 이어붙인다.
    ///
    /// 색은 LSO_TurnCandle이 맡는다. 여기서는 색을 건드리지 않는다.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class LSO_CandleFlicker : MonoBehaviour
    {
        [Header("밝기 흔들림")]
        [Tooltip("기준 밝기의 몇 배까지 어두워지는지. 기획서의 '아주 미세하게'에 맞춰 좁게 잡는다.")]
        [SerializeField, Range(0f, 1f)] private float minScale = 0.92f;

        [Tooltip("기준 밝기의 몇 배까지 밝아지는지.")]
        [SerializeField, Range(1f, 2f)] private float maxScale = 1.05f;

        [Header("흔들림 속도")]
        [Tooltip("한 번 밝기가 바뀌는 데 걸리는 가장 짧은 시간(초).")]
        [SerializeField, Min(0.01f)] private float minDuration = 0.08f;

        [Tooltip("한 번 밝기가 바뀌는 데 걸리는 가장 긴 시간(초).")]
        [SerializeField, Min(0.01f)] private float maxDuration = 0.26f;

        [Header("바람")]
        [Tooltip("가끔 훅 꺼질 듯 크게 어두워질 확률(0~1). 0이면 일정하게만 떤다.")]
        [SerializeField, Range(0f, 1f)] private float gustChance = 0.05f;

        [Tooltip("바람이 불 때 떨어지는 밝기 배수. Min Scale보다 낮게 잡는다.")]
        [SerializeField, Range(0f, 1f)] private float gustScale = 0.7f;

        [Tooltip("바람이 불 때 걸리는 시간(초). 평소보다 길어야 훅 꺼지는 느낌이 난다.")]
        [SerializeField, Min(0.01f)] private float gustDuration = 0.4f;

        [Header("그림자 흔들림")]
        [Tooltip("광원이 제자리에서 얼마나 움직이는지(월드 단위). 0이면 밝기만 흔들린다.\n" +
                 "그림자가 눈에 띄게 춤추면 과하다. 보드 한 칸 크기의 몇 십 분의 일이 적당하다.")]
        [SerializeField, Min(0f)] private float swayRadius = 0.04f;

        [Tooltip("한 번 자리를 옮기는 데 걸리는 시간(초). 밝기보다 느려야 자연스럽다.")]
        [SerializeField, Min(0.01f)] private float swayDuration = 0.5f;

        [Header("기타")]
        [Tooltip("켜면 일시정지 중에도 계속 떤다.")]
        [SerializeField] private bool ignoreTimeScale;

        private Light _light;
        private float _baseIntensity;
        private Vector3 _basePosition;
        private Tween _intensityTween;
        private Tween _swayTween;

        /// <summary>기준 밝기. 이 값을 바꾸면 다음 구간부터 그 주위로 흔들린다.</summary>
        public float BaseIntensity
        {
            get => _baseIntensity;
            set => _baseIntensity = Mathf.Max(0f, value);
        }

        private void Awake()
        {
            _light = GetComponent<Light>();

            // 인스펙터에서 맞춰둔 값이 기준이 된다.
            // 여기서 잡아두지 않으면 첫 구간이 끝난 뒤의 값이 기준이 되어 점점 어두워지고 떠내려간다.
            _baseIntensity = _light.intensity;
            _basePosition = transform.localPosition;
        }

        private void OnEnable()
        {
            StepIntensity();
            StepSway();
        }

        private void OnDisable()
        {
            KillTweens();

            // 꺼질 때 어두운 구간이나 치우친 자리에 걸려 있으면 그 상태로 굳는다.
            if (_light != null)
                _light.intensity = _baseIntensity;

            transform.localPosition = _basePosition;
        }

        /// <summary>
        /// 다음 밝기 한 구간을 재생하고, 끝나면 자기 자신을 다시 부른다.
        ///
        /// SetLoops로 반복하지 않는 이유는 목표와 시간을 매번 새로 뽑아야 하기 때문이다.
        /// 반복 트윈은 처음 정한 값을 그대로 되풀이해서 같은 무늬가 눈에 띈다.
        /// </summary>
        private void StepIntensity()
        {
            if (_light == null) return;

            bool gust = Random.value < gustChance;

            float target = _baseIntensity * (gust
                ? gustScale
                : Random.Range(minScale, maxScale));

            float duration = gust
                ? gustDuration
                : Random.Range(minDuration, maxDuration);

            // 바람은 훅 꺼졌다 천천히 돌아오는 느낌이라 감속을,
            // 평소 떨림은 딱 끊기는 맛이 있어야 해서 InOutSine을 쓴다.
            Ease ease = gust ? Ease.OutQuad : Ease.InOutSine;

            _intensityTween = _light
                .DOIntensity(target, duration)
                .SetEase(ease)
                .SetUpdate(ignoreTimeScale)
                .SetLink(gameObject)
                .OnComplete(StepIntensity);
        }

        /// <summary>
        /// 기준 자리 주변의 아무 점으로 천천히 옮겨간다. 그림자가 여기에 따라 움직인다.
        ///
        /// 밝기와 주기를 맞추지 않는다. 둘이 같은 박자로 움직이면 규칙이 눈에 보인다.
        /// </summary>
        private void StepSway()
        {
            if (swayRadius <= 0f) return;

            Vector3 target = _basePosition + Random.insideUnitSphere * swayRadius;

            _swayTween = transform
                .DOLocalMove(target, swayDuration)
                .SetEase(Ease.InOutSine)
                .SetUpdate(ignoreTimeScale)
                .SetLink(gameObject)
                .OnComplete(StepSway);
        }

        private void KillTweens()
        {
            _intensityTween?.Kill();
            _intensityTween = null;

            _swayTween?.Kill();
            _swayTween = null;
        }

        private void OnValidate()
        {
            // 최소가 최대를 넘으면 Random.Range가 뒤집힌 구간을 돌려줘 흔들림이 멈춘 것처럼 보인다.
            if (maxScale < minScale) maxScale = minScale;
            if (maxDuration < minDuration) maxDuration = minDuration;
            if (gustScale > minScale) gustScale = minScale;
        }
    }
}
