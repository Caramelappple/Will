using _Scripts.LSO.Effect;
using _Scripts.LSO.UI.Text;
using UnityEngine;

namespace _Scripts.LSO.Will.Candle
{
    /// <summary>
    /// 유언 양초의 겉모습. 어떤 유언인지를 **불꽃 색**으로 보여준다.
    ///
    /// ── 도장에서 넘어온 이유 ──────────────────────────────────
    /// 예전에는 유언마다 도장 모델을 하나씩 만들어 켜고 껐다.
    /// 모델 5종에 찍는 애니메이션까지 필요해서 만들 것이 너무 많았다.
    ///
    /// 초는 제자리에서 색과 밝기만 바뀐다. 새로 만들 동작이 없다.
    /// 흔들림은 LSO_CandleFlicker가 이미 하고 있어서 건드리지 않는다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 색은 DLJ_WillDataSO.flameColor 에서 가져온다. 이름·설명·아이콘이 거기 있으므로
    /// 색도 같은 자리에 둔다. 여기서 유언마다 값을 또 들고 있지 않는다.
    ///
    /// None을 주면 **불을 끈다.** 그것이 "유언 없음"이다.
    ///
    /// 무엇을 들고 있는지는 LSO_WillCandle 하나만 안다. 여기는 시키는 대로 보여준다.
    ///
    /// 씬 배선: 양초에 붙이고 Flame Light 를 연결할 것.
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
        [Tooltip("켜져 있을 때의 밝기.")]
        [SerializeField, Min(0f)] private float litIntensity = 1f;

        private LSO_CandleFlicker _flicker;
        private MaterialPropertyBlock _block;
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

            _colorId = Shader.PropertyToID(colorProperty);
            _block = new MaterialPropertyBlock();
        }

        /// <summary>
        /// 어떤 유언의 초인지 정한다. None이면 불을 끈다.
        ///
        /// 색을 사전에서 가져오므로, 유언이 늘어도 여기는 고치지 않는다.
        /// </summary>
        /// <remarks>
        /// 바뀌는 순간의 연출은 없다. 색만 그 자리에서 갈린다.
        ///
        /// 예전에는 한 번 번쩍이고 살짝 커졌다. 초가 하나뿐이라 "무엇이 골라졌나"를
        /// 자리로 알릴 수 없으니 반응을 주려던 것이었는데, 휠로 훑으면 그 연출이
        /// 칸마다 겹쳐 초가 계속 들썩였다.
        /// </remarks>
        public void Show(LSO_WillType type)
        {
            Current = type;

            ApplyColor(ResolveColor(type));
            ApplyIntensity(IsLit ? litIntensity : 0f);
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
        /// 밝기를 바꾼다. 흔들림이 켜져 있으면 그쪽의 기준값도 바꾼다 —
        /// Light.intensity 만 만지면 다음 흔들림 구간이 덮어써서 되돌아간다.
        ///
        /// 언제나 그 자리에서 바꾼다. 서서히 바꾸던 길은 번쩍임과 함께 없앴다.
        /// </summary>
        private void ApplyIntensity(float value)
        {
            if (flame == null) return;

            if (_flicker != null) _flicker.BaseIntensity = value;

            flame.intensity = value;
        }
    }
}
