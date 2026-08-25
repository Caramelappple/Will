using _Scripts.LDY;
using _Scripts.LSO.Manager;
using DG.Tweening;
using UnityEngine;

namespace _Scripts.LSO.Effect
{
    /// <summary>
    /// 지금이 누구 턴인지를 촛불 색으로 알린다. 색을 바꾸는 것 외의 책임은 갖지 않는다.
    ///
    /// 깜박임은 LSO_CandleFlicker가, 턴 진행은 LDY_TurnManager가, 어떤 색인지는
    /// LSO_TurnPaletteSO가 맡는다. 이 컴포넌트는 그 사이에서
    /// "턴이 바뀌었다"를 "색을 바꿔라"로 옮기기만 한다.
    ///
    /// 촛불마다 하나씩 붙인다. 중앙에서 촛불 목록을 들고 도는 방식으로 하지 않는 이유는,
    /// 그러면 씬에 촛불을 놓은 뒤 목록에 등록하는 것을 잊었을 때 아무 에러 없이
    /// 그 촛불만 색이 안 바뀌기 때문이다. 프리팹을 끌어다 놓으면 끝나는 쪽이 낫다.
    ///
    /// 밝기는 건드리지 않는다. 같은 라이트를 LSO_CandleFlicker가 밝기로 흔들고 있으므로,
    /// 여기서 밝기까지 손대면 두 트윈이 같은 값을 두고 서로를 덮어쓴다.
    /// </summary>
    public class LSO_TurnCandle : MonoBehaviour
    {
        [Header("설정")]
        [Tooltip("팀 색과 전환 시간을 담은 에셋. 이 촛불만 다른 색을 쓰려면 다른 에셋을 끼우면 된다.")]
        [SerializeField] private LSO_TurnPaletteSO palette;

        [Header("대상")]
        [Tooltip("색을 바꿀 빛. 비워두면 자신과 자식에서 찾는다.")]
        [SerializeField] private Light targetLight;

        [Tooltip("색을 바꿀 촛불 이펙트. 비워두면 자신과 자식에서 찾는다.")]
        [SerializeField] private ParticleSystem flame;

        private LDY_TurnManager _turnManager;
        private Color _flameColor;
        private Tween _lightTween;
        private Tween _flameTween;

        private void Awake()
        {
            if (targetLight == null)
                targetLight = GetComponentInChildren<Light>();

            if (flame == null)
                flame = GetComponentInChildren<ParticleSystem>();

            // 연결이 빠지면 색이 그냥 안 바뀔 뿐이라 화면만 봐서는 원인을 알 수 없다.
            // 무엇이 안 꽂혔는지 여기서 짚어준다.
            if (palette == null)
                Debug.LogError($"{name}: Turn Palette가 비어 있어 색을 정할 수 없습니다.", this);

            if (targetLight == null && flame == null)
                Debug.LogError($"{name}: 색을 바꿀 빛도 촛불 이펙트도 없습니다.", this);

            if (flame != null)
                _flameColor = flame.main.startColor.color;
        }

        private void OnEnable()
        {
            // 전투 씬마다 턴 매니저가 새로 생긴다. 직접 참조로 물고 있으면 씬을 넘길 때 끊긴다.
            GameManager.Instance.TurnManagerChanged += Bind;

            // 구독하는 순간에 이미 턴 매니저가 등록돼 있을 수 있다.
            // 이벤트만 기다리면 그 경우 영영 색이 안 잡힌다.
            Bind(GameManager.Instance.TurnManager);
        }

        private void OnDisable()
        {
            if (GameManager.HasInstance)
                GameManager.Instance.TurnManagerChanged -= Bind;

            Bind(null);

            KillTweens();
        }

        private void Bind(LDY_TurnManager turnManager)
        {
            if (_turnManager == turnManager) return;

            if (_turnManager != null)
                _turnManager.OnTurnChanged -= HandleTurnChanged;

            _turnManager = turnManager;

            if (_turnManager == null) return;

            _turnManager.OnTurnChanged += HandleTurnChanged;

            // LDY_TurnManager는 Start에서 첫 턴을 한 번 알린다.
            // 이쪽이 늦게 붙으면 그 한 번을 놓치므로, 붙자마자 현재 턴을 직접 읽어 맞춘다.
            // 시작 색은 연출 없이 바로 잡는다. 엉뚱한 색에서 서서히 넘어오면 그것대로 어색하다.
            Apply(_turnManager.CurrentTurn, animate: false);
        }

        private void HandleTurnChanged(LDY_Team team)
        {
            Apply(team, animate: true);
        }

        /// <summary>바깥에서 색을 강제로 맞추고 싶을 때 쓴다.</summary>
        public void Apply(LDY_Team team, bool animate)
        {
            if (palette == null) return;

            Color target = palette.ColorFor(team);
            float duration = animate ? palette.TransitionDuration : 0f;

            ApplyToLight(target, duration);
            ApplyToFlame(target, duration);
        }

        private void ApplyToLight(Color target, float duration)
        {
            if (targetLight == null) return;

            _lightTween?.Kill();

            if (duration <= 0f)
            {
                targetLight.color = target;
                return;
            }

            _lightTween = targetLight
                .DOColor(target, duration)
                .SetEase(palette.Ease)
                .SetLink(gameObject);
        }

        /// <summary>
        /// 파티클은 startColor만 바꾼다. 이미 떠 있는 불꽃은 원래 색으로 사라지고
        /// 새로 나오는 것부터 새 색이라, 불이 옮겨붙듯 서서히 갈린다.
        ///
        /// MainModule은 파티클 시스템을 가리키는 구조체다. 지역 변수에 받아 값을 넣어도
        /// 원본에 그대로 반영되므로 다시 대입할 필요가 없다.
        /// </summary>
        private void ApplyToFlame(Color target, float duration)
        {
            if (flame == null) return;

            _flameTween?.Kill();

            if (duration <= 0f)
            {
                SetFlameColor(target);
                return;
            }

            _flameTween = DOTween
                .To(() => _flameColor, SetFlameColor, target, duration)
                .SetEase(palette.Ease)
                .SetLink(gameObject);
        }

        private void SetFlameColor(Color color)
        {
            _flameColor = color;

            ParticleSystem.MainModule main = flame.main;
            main.startColor = color;
        }

        private void KillTweens()
        {
            _lightTween?.Kill();
            _lightTween = null;

            _flameTween?.Kill();
            _flameTween = null;
        }
    }
}
