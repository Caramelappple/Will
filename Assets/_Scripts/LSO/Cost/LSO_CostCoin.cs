using DG.Tweening;
using UnityEngine;

namespace _Scripts.LSO.Cost
{
    public class LSO_CostCoin : MonoBehaviour
    {
        [Header("대상")]
        [Tooltip("크기를 바꿀 것. 비워두면 자신의 Transform을 쓴다.")]
        [SerializeField] private Transform visual;

        [Header("크기")]
        [Tooltip("차 있을 때의 배율.")]
        [SerializeField, Min(0f)] private float filledScale = 1f;

        [Tooltip("비었을 때의 배율. 0이면 완전히 사라진다.")]
        [SerializeField, Min(0f)] private float spentScale;

        [Header("연출")]
        [Tooltip("채워질 때 걸리는 시간(초).")]
        [SerializeField, Min(0f)] private float fillDuration = 0.22f;

        [Tooltip("쓸 때 걸리는 시간(초). 쓰는 쪽이 빨라야 반응이 붙는 느낌이 난다.")]
        [SerializeField, Min(0f)] private float spendDuration = 0.12f;

        [SerializeField] private Ease fillEase = Ease.OutBack;

        [SerializeField] private Ease spendEase = Ease.InQuad;

        [Tooltip("켜면 일시정지 중에도 연출이 진행된다.")]
        [SerializeField] private bool ignoreTimeScale;

        private Transform Visual => visual != null ? visual : transform;

        private Vector3 _baseScale;
        private Tween _tween;

        /// <summary>차 있는지. 아직 한 번도 정해지지 않았으면 true로 본다.</summary>
        public bool IsFilled { get; private set; } = true;

        private void Awake()
        {
            // 인스펙터에서 맞춰둔 크기가 기준이다. 여기서 잡지 않으면
            // 한 번 줄어든 뒤의 크기가 기준이 되어 쓸 때마다 점점 작아진다.
            _baseScale = Visual.localScale;
        }

        /// <summary>
        /// 차 있는 상태로 만들거나 비운다.
        /// </summary>
        /// <param name="filled">차 있게 할지.</param>
        /// <param name="animate">false면 연출 없이 즉시 맞춘다. 처음 그릴 때 쓴다.</param>
        /// <param name="delay">시작을 미룰 시간(초). 칸마다 조금씩 밀어 물결처럼 만들 때 쓴다.</param>
        public void SetFilled(bool filled, bool animate = true, float delay = 0f)
        {
            // 같은 상태를 다시 넣는 것은 흔하다(코스트가 안 변한 갱신).
            // 그대로 두지 않으면 멀쩡한 칸이 매번 다시 튀어나온다.
            if (IsFilled == filled && _tween == null) return;

            IsFilled = filled;

            KillTween();

            Vector3 target = _baseScale * (filled ? filledScale : spentScale);

            if (!animate)
            {
                Visual.localScale = target;
                return;
            }

            _tween = Visual
                .DOScale(target, filled ? fillDuration : spendDuration)
                .SetDelay(delay)
                .SetEase(filled ? fillEase : spendEase)
                .SetUpdate(ignoreTimeScale)
                .SetLink(gameObject)
                .OnComplete(() => _tween = null);
        }

        private void KillTween()
        {
            if (_tween == null) return;

            _tween.Kill();
            _tween = null;
        }

        private void OnDisable()
        {
            KillTween();

            // 줄어드는 도중에 꺼지면 어중간한 크기로 굳는다.
            Visual.localScale = _baseScale * (IsFilled ? filledScale : spentScale);
        }
    }
}
