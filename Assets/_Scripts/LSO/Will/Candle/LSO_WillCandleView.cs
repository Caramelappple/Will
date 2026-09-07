using _Scripts.LSO.Effect;
using _Scripts.LSO.UI.Text;
using DG.Tweening;
using UnityEngine;

namespace _Scripts.LSO.Will.Candle
{
    /// <summary>
    /// 촛대에 꽂힌 초 하나의 겉모습. 어떤 유언인지를 **불꽃 색**으로 보여준다.
    ///
    /// ── 도장에서 넘어온 이유 ──────────────────────────────────
    /// 예전에는 유언마다 도장 모델을 하나씩 만들어 켜고 껐다(LSO_WillStampView).
    /// 모델 5종에 찍는 애니메이션까지 필요해서 만들 것이 너무 많았다.
    ///
    /// 초는 제자리에서 색과 밝기만 바뀐다. 새로 만들 동작이 없다.
    /// 흔들림은 LSO_CandleFlicker가 이미 하고 있어서 건드리지 않는다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 색은 DLJ_WillDataSO.flameColor 에서 가져온다. 이름·설명·아이콘이 거기 있으므로
    /// 색도 같은 자리에 둔다. 여기서 유언마다 값을 또 들고 있지 않는다.
    ///
    /// None을 주면 **불을 끈다.** 그것이 "유언 없음" 초다.
    ///
    /// 씬 배선: 초 프리팹에 붙이고 Flame Light 를 연결할 것.
    /// 불꽃 메시가 따로 있으면 Flame Body 에도 연결하면 색이 같이 바뀐다.
    /// </summary>
    public sealed class LSO_WillCandleView : MonoBehaviour
    {
        [Header("연결")]
        [Tooltip("불빛. 비워두면 자식에서 찾는다.")]
        [SerializeField] private Light flame;

        [Tooltip("불꽃 메시. 없어도 된다. 있으면 색을 같이 바꾼다.")]
        [SerializeField] private Renderer flameBody;

        [Tooltip("불꽃 메시에서 색을 넣을 속성 이름.")]
        [SerializeField] private string colorProperty = "_BaseColor";

        [Header("밝기")]
        [Tooltip("평소 켜져 있을 때의 밝기.")]
        [SerializeField, Min(0f)] private float litIntensity = 1f;

        [Tooltip("고른 초의 밝기. 평소보다 밝아야 어느 것을 들었는지 보인다.")]
        [SerializeField, Min(0f)] private float highlightIntensity = 2.2f;

        [Tooltip("고른 초가 커지는 배율.")]
        [SerializeField, Min(1f)] private float highlightScale = 1.15f;

        [Tooltip("밝기와 크기가 바뀌는 데 걸리는 시간.")]
        [SerializeField, Min(0f)] private float fadeDuration = 0.15f;

        private LSO_CandleFlicker _flicker;
        private MaterialPropertyBlock _block;
        private Vector3 _baseScale;
        private Tween _scaleTween;
        private int _colorId;

        /// <summary>지금 이 초가 나타내는 유언. 불이 꺼져 있으면 None.</summary>
        public LSO_WillType Current { get; private set; } = LSO_WillType.None;

        /// <summary>불이 켜져 있는지. None 초는 늘 꺼져 있다.</summary>
        public bool IsLit => Current != LSO_WillType.None;

        private void Awake()
        {
            if (flame == null) flame = GetComponentInChildren<Light>(true);

            if (flame == null)
                Debug.LogError($"{name}: Light가 없어 불을 켤 수 없습니다.", this);
            else
                _flicker = flame.GetComponent<LSO_CandleFlicker>();

            _baseScale = transform.localScale;
            _colorId = Shader.PropertyToID(colorProperty);
            _block = new MaterialPropertyBlock();
        }

        /// <summary>
        /// 어떤 유언의 초인지 정한다. None이면 불을 끈다.
        ///
        /// 색을 사전에서 가져오므로, 유언이 늘어도 여기는 고치지 않는다.
        /// </summary>
        public void Show(LSO_WillType type)
        {
            Current = type;

            Color color = ResolveColor(type);

            ApplyColor(color);
            ApplyIntensity(IsLit ? litIntensity : 0f, instant: true);

            SetHighlighted(false);
        }

        /// <summary>
        /// 골라졌는지 보여준다. 밝아지고 조금 커진다.
        ///
        /// 값을 기억하지 않는다. 무엇이 골라졌는지는 촛대(LSO_WillRack) 하나만 안다.
        /// </summary>
        public void SetHighlighted(bool on)
        {
            // 불 꺼진 초도 골라질 수 있다("유언 없음"). 그때는 밝기 대신 크기로만 알린다.
            if (IsLit)
                ApplyIntensity(on ? highlightIntensity : litIntensity, instant: false);

            _scaleTween?.Kill();

            Vector3 target = on ? _baseScale * highlightScale : _baseScale;

            if (fadeDuration <= 0f || !isActiveAndEnabled)
            {
                transform.localScale = target;
                return;
            }

            _scaleTween = transform
                .DOScale(target, fadeDuration)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        /// <summary>불을 끈다. 촛대가 정리할 때 부른다.</summary>
        public void Hide()
        {
            Show(LSO_WillType.None);
        }

        /// <summary>
        /// 유언 색을 사전에서 찾는다. 못 찾으면 흰색으로 둔다.
        ///
        /// 경고를 내지 않는 이유는 None(빈 초)이 정상적으로 여기 오기 때문이다.
        /// 색을 안 정한 유언은 흰 불로 보이므로 눈으로 바로 알아챌 수 있다.
        /// </summary>
        private static Color ResolveColor(LSO_WillType type)
        {
            if (type == LSO_WillType.None) return Color.white;

            DLJ_WillDataSO data = LSO_WillText.DataOf(type);

            return data != null ? data.flameColor : Color.white;
        }

        private void ApplyColor(Color color)
        {
            if (flame != null) flame.color = color;

            if (flameBody == null) return;

            // MaterialPropertyBlock을 쓰는 이유는 초마다 머티리얼이 복제되지 않게 하기 위해서다.
            // renderer.material 을 만지면 인스턴스가 하나씩 생기고 씬을 나갈 때 남는다.
            flameBody.GetPropertyBlock(_block);
            _block.SetColor(_colorId, color);
            flameBody.SetPropertyBlock(_block);
        }

        /// <summary>
        /// 밝기를 바꾼다. 흔들림이 켜져 있으면 그쪽의 기준값을 바꾼다 —
        /// Light.intensity 를 직접 만지면 다음 흔들림 구간이 덮어써서 되돌아간다.
        /// </summary>
        private void ApplyIntensity(float value, bool instant)
        {
            if (flame == null) return;

            if (_flicker != null)
            {
                _flicker.BaseIntensity = value;

                // 흔들림이 다음 구간에서 반영하므로 즉시 보여야 할 때만 직접 넣는다.
                if (instant) flame.intensity = value;

                return;
            }

            flame.DOKill();

            if (instant || fadeDuration <= 0f || !isActiveAndEnabled)
            {
                flame.intensity = value;
                return;
            }

            flame.DOIntensity(value, fadeDuration)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void OnDisable()
        {
            _scaleTween?.Kill();
            _scaleTween = null;

            if (flame != null) flame.DOKill();

            transform.localScale = _baseScale;
        }
    }
}
