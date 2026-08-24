using DG.Tweening;
using UnityEngine;

namespace _Scripts.LSO.UI
{
    /// <summary>
    /// 커서가 올라가 있는 동안 투명도를 바꾸고, 벗어나면 원래대로 돌아온다.
    ///
    /// 클릭 연출과 달리 "한 번 재생"이 아니라 상태다.
    /// 커서가 머무는 내내 유지돼야 하므로 되돌아오는 트윈을 붙이지 않는다.
    ///
    /// CanvasGroup을 쓰기 때문에 아이콘과 글자가 같이 흐려진다.
    /// Image 하나만 건드리면 위에 얹힌 텍스트는 그대로 남아 어색하다.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup),typeof(LSO_ButtonHoverHandler))]
    public class LSO_HoverFadeEffect : MonoBehaviour, LSO_IHoverEffect
    {
        [Header("대상")]
        [Tooltip("비워두면 자신의 CanvasGroup을 쓴다.\n" +
                 "창 전체를 여닫는 CanvasGroup을 지정하면 페이드끼리 싸우니 버튼 단위로 둘 것.")]
        [SerializeField] private CanvasGroup target;

        [Header("연출 설정")]
        [Tooltip("커서가 올라가 있는 동안의 투명도.\n" +
                 "기본 알파보다 높게 주면 반대로 또렷해지는 연출이 된다.")]
        [SerializeField, Range(0f, 1f)] private float hoverAlpha = 0.55f;

        [Tooltip("커서가 올라갈 때 걸리는 시간")]
        [SerializeField, Min(0f)] private float enterDuration = 0.1f;

        [Tooltip("커서가 벗어날 때 걸리는 시간. 조금 길게 두면 부드럽다.")]
        [SerializeField, Min(0f)] private float exitDuration = 0.16f;

        [Tooltip("timescale 영향 여부")]
        [SerializeField] private bool ignoreTimeScale = true;

        [SerializeField] private Ease easeEnter = Ease.OutQuad;
        [SerializeField] private Ease easeExit = Ease.OutQuad;

        private CanvasGroup _group;
        private float _originalAlpha;
        private Tween _tween;

        private void Awake()
        {
            _group = target != null ? target : GetComponent<CanvasGroup>();

            // 항상 1이라고 가정하면 반투명하게 디자인된 버튼이 벗어날 때 불투명해진다.
            _originalAlpha = _group.alpha;
        }

        public void OnHoverEnter()
        {
            FadeTo(hoverAlpha, enterDuration, easeEnter);
        }

        public void OnHoverExit()
        {
            FadeTo(_originalAlpha, exitDuration, easeExit);
        }

        private void FadeTo(float alpha, float duration, Ease ease)
        {
            // 커서를 빠르게 들락거려도 트윈이 쌓이지 않게 이전 것을 먼저 끊는다.
            KillTween();

            if (duration <= 0f)
            {
                _group.alpha = alpha;
                return;
            }

            _tween = _group.DOFade(alpha, duration)
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
            // 커서가 올라간 채로 창이 닫히면 OnHoverExit이 오지 않아 흐린 상태로 굳는다.
            KillTween();

            if (_group != null)
                _group.alpha = _originalAlpha;
        }
    }
}
