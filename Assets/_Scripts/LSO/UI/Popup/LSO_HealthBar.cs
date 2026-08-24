using _Scripts.LSO.HealthSystem;
using _Scripts.LSO.HealthSystem.Data;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.LSO.UI.Popup
{
    /// <summary>
    /// 체력바. 앞 바는 즉시 줄고, 뒤 바가 잠시 뒤에 천천히 따라온다.
    /// 그 사이에 드러나는 색 띠가 "이번에 얼마나 맞았는지"를 보여준다.
    ///
    /// 회복할 때는 뒤 바가 바로 따라붙는다. 늦게 따라오면 회복이 손해처럼 보인다.
    ///
    /// 프리팹 구성(Image Type은 둘 다 Filled):
    ///   Bar
    ///   ├── Ghost  Image  ← 뒤에서 천천히 줄어드는 바
    ///   └── Fill   Image  ← 현재 체력
    /// </summary>
    public class LSO_HealthBar : MonoBehaviour
    {
        [Tooltip("비워두면 부모와 자식에서 찾는다.")]
        [SerializeField] private DamageableResources health;

        [Header("바")]
        [Tooltip("현재 체력. Image Type을 Filled로 둘 것.")]
        [SerializeField] private Image fillImage;

        [Tooltip("지연해서 따라오는 뒤 바. 없어도 동작한다.")]
        [SerializeField] private Image ghostImage;

        [Tooltip("\"12 / 20\" 같은 표시. 선택 사항.")]
        [SerializeField] private TMP_Text label;

        [SerializeField] private string labelFormat = "{0} / {1}";

        [Header("앞 바")]
        [Tooltip("0이면 즉시 반영한다.")]
        [SerializeField, Min(0f)] private float fillDuration = 0.08f;

        [Header("뒤 바")]
        [Tooltip("피해를 받고 이만큼 기다린 뒤 줄기 시작한다.")]
        [SerializeField, Min(0f)] private float drainDelay = 0.35f;

        [SerializeField, Min(0f)] private float drainDuration = 0.45f;

        [SerializeField] private Ease drainEase = Ease.InOutQuad;

        [Header("표시")]
        [Tooltip("체력이 가득이면 숨긴다. 기물 머리 위 바에 유용하다.")]
        [SerializeField] private bool hideWhenFull;

        [Tooltip("숨김에 사용할 CanvasGroup. 비우면 자신에게서 찾는다.")]
        [SerializeField] private CanvasGroup canvasGroup;

        [SerializeField, Min(0f)] private float hideFadeDuration = 0.2f;

        private Health _recoverable;
        private Tween _fillTween;
        private Tween _ghostTween;
        private Tween _visibilityTween;

        /// <summary>현재 체력 비율(0~1).</summary>
        private float Ratio
        {
            get
            {
                if (health == null) return 0f;

                int span = health.MaxValue - health.MinValue;
                if (span <= 0) return 0f;

                return Mathf.Clamp01((float)(health.Value - health.MinValue) / span);
            }
        }

        private void Awake()
        {
            if (health == null)
                health = GetComponentInParent<DamageableResources>();
            if (health == null)
                health = GetComponentInChildren<DamageableResources>(true);

            if (health == null)
            {
                Debug.LogWarning($"{name}: 체력을 찾지 못했습니다.", this);
                return;
            }

            // 회복 이벤트는 Health에만 있다. 없으면 피해만 반영한다.
            _recoverable = health as Health;

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            if (health == null) return;

            health.OnDamage += HandleDamage;

            if (_recoverable != null)
                _recoverable.OnRecover += HandleRecover;

            Refresh(true);
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnDamage -= HandleDamage;

                if (_recoverable != null)
                    _recoverable.OnRecover -= HandleRecover;
            }

            KillTweens();
        }

        // MaxValue는 카드로 기물을 세울 때 뒤늦게 정해진다.
        // Start는 그 뒤에 돌기 때문에 여기서 한 번 더 맞춰준다.
        private void Start()
        {
            Refresh(true);
        }

        private void HandleDamage(DamageResultData data) => Refresh(false);

        private void HandleRecover(RecoverResultData data) => Refresh(false);

        /// <summary>
        /// 최대 체력이 바뀌었을 때처럼 외부에서 강제로 다시 그리게 할 수 있다.
        /// </summary>
        /// <param name="instant">연출 없이 즉시 맞출지.</param>
        public void Refresh(bool instant)
        {
            if (health == null) return;

            float ratio = Ratio;

            ApplyFill(ratio, instant);
            ApplyGhost(ratio, instant);
            ApplyLabel();
            ApplyVisibility(ratio, instant);
        }

        private void ApplyFill(float ratio, bool instant)
        {
            if (fillImage == null) return;

            KillTween(ref _fillTween);

            if (instant || fillDuration <= 0f)
            {
                fillImage.fillAmount = ratio;
                return;
            }

            _fillTween = DOTween
                .To(() => fillImage.fillAmount, v => fillImage.fillAmount = v, ratio, fillDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);
        }

        private void ApplyGhost(float ratio, bool instant)
        {
            if (ghostImage == null) return;

            KillTween(ref _ghostTween);

            // 늘어나는 쪽(회복)은 기다릴 이유가 없다. 바로 붙인다.
            bool draining = ratio < ghostImage.fillAmount;

            if (instant || !draining || drainDuration <= 0f)
            {
                ghostImage.fillAmount = ratio;
                return;
            }

            _ghostTween = DOTween
                .To(() => ghostImage.fillAmount, v => ghostImage.fillAmount = v, ratio, drainDuration)
                .SetDelay(drainDelay)
                .SetEase(drainEase)
                .SetLink(gameObject);
        }

        private void ApplyLabel()
        {
            if (label == null) return;

            label.text = string.Format(labelFormat, health.Value, health.MaxValue);
        }

        private void ApplyVisibility(float ratio, bool instant)
        {
            if (!hideWhenFull || canvasGroup == null) return;

            float target = ratio >= 1f ? 0f : 1f;

            KillTween(ref _visibilityTween);

            if (instant || hideFadeDuration <= 0f)
            {
                canvasGroup.alpha = target;
                return;
            }

            _visibilityTween = canvasGroup
                .DOFade(target, hideFadeDuration)
                .SetLink(gameObject);
        }

        private static void KillTween(ref Tween tween)
        {
            if (tween == null) return;

            tween.Kill();
            tween = null;
        }

        private void KillTweens()
        {
            KillTween(ref _fillTween);
            KillTween(ref _ghostTween);
            KillTween(ref _visibilityTween);
        }
    }
}
