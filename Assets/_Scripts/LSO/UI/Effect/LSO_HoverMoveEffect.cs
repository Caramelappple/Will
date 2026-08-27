using DG.Tweening;
using UnityEngine;
using _Scripts.LSO.UI.Input;

namespace _Scripts.LSO.UI.Effect
{
    /// <summary>
    /// 커서가 올라가 있는 동안 자리를 옮기고, 벗어나면 원래 자리로 돌아온다.
    ///
    /// 클릭 연출과 달리 "한 번 재생"이 아니라 상태다.
    /// 커서가 머무는 내내 옮긴 자리에 있어야 하므로 되돌아오는 트윈을 붙이지 않는다.
    ///
    /// 옮기는 것은 localPosition이다. 부모가 돌아가 있어도 방향이 함께 따라간다.
    /// Awake에서 잡아둔 자리가 기준이므로, 이 자리를 다른 스크립트나 애니메이터가
    /// 함께 건드리면 어긋난다. 자리를 정하는 주체는 하나여야 한다.
    /// </summary>
    [RequireComponent(typeof(LSO_ButtonHoverHandler))]
    public class LSO_HoverMoveEffect : MonoBehaviour, LSO_IHoverEffect
    {
        [Header("대상")]
        [Tooltip("비워두면 자기 자신을 옮긴다.\n" +
                 "콜라이더가 같이 움직여 커서에서 벗어나는 것이 문제라면,\n" +
                 "콜라이더는 이 오브젝트에 두고 자식 비주얼을 여기 연결할 것.")]
        [SerializeField] private Transform target;

        [Header("연출 설정")]
        [Tooltip("원래 자리에서 얼마나 옮길지. 대상의 로컬 기준이다.\n" +
                 "3D 월드는 0.05~0.2, UI(RectTransform)는 픽셀 단위라 10~20이 보인다.")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 0.1f, 0f);

        [Tooltip("커서가 올라갈 때 걸리는 시간")]
        [SerializeField, Min(0f)] private float enterDuration = 0.1f;

        [Tooltip("커서가 벗어날 때 걸리는 시간. 조금 길게 두면 부드럽다.")]
        [SerializeField, Min(0f)] private float exitDuration = 0.16f;

        [Tooltip("timescale 영향 여부")]
        [SerializeField] private bool ignoreTimeScale = true;

        [SerializeField] private Ease easeEnter = Ease.OutQuad;
        [SerializeField] private Ease easeExit = Ease.OutQuad;

        private Transform _target;
        private Vector3 _originalPosition;
        private Tween _tween;

        private void Awake()
        {
            _target = target != null ? target : transform;

            _originalPosition = _target.localPosition;
        }

        public void OnHoverEnter()
        {
            MoveTo(_originalPosition + offset, enterDuration, easeEnter);
        }

        public void OnHoverExit()
        {
            MoveTo(_originalPosition, exitDuration, easeExit);
        }

        private void MoveTo(Vector3 position, float duration, Ease ease)
        {
            // 커서를 빠르게 들락거려도 트윈이 쌓이지 않게 이전 것을 먼저 끊는다.
            KillTween();

            if (duration <= 0f)
            {
                _target.localPosition = position;
                return;
            }

            _tween = _target.DOLocalMove(position, duration)
                .SetEase(ease)
                .SetUpdate(ignoreTimeScale)
                .SetLink(gameObject);
        }

        private void KillTween()
        {
            if (_tween == null) return;

            _tween.Kill();
            _tween = null;
        }

        private void OnDisable()
        {
            // 커서가 올라간 채로 창이 닫히면 OnHoverExit이 오지 않아 옮겨진 자리에서 굳는다.
            KillTween();

            if (_target != null)
                _target.localPosition = _originalPosition;
        }
    }
}
