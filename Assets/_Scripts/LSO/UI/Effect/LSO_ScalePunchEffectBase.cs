using DG.Tweening;
using UnityEngine;

namespace _Scripts.LSO.UI.Effect
{
    /// <summary>
    /// 대상을 잠깐 축소했다가 원래 크기로 되돌리는 연출의 공통 구현.
    /// "언제 재생할지"는 알지 못하며, 파생 클래스가 트리거를 받아 Play()를 부른다.
    /// </summary>
    public abstract class LSO_ScalePunchEffectBase : MonoBehaviour
    {
        [Header("대상")]
        [Tooltip("비워두면 자신의 Transform을 사용한다.")]
        [SerializeField] private Transform target;

        [Header("연출 설정")]
        [Tooltip("축소 비율")]
        [SerializeField, Range(0.5f, 1.5f)] private float shrinkRatio = 0.92f;

        [Tooltip("축소 시간")]
        [SerializeField, Min(0f)] private float shrinkDuration = 0.07f;

        [Tooltip("복귀 시간")]
        [SerializeField, Min(0f)] private float restoreDuration = 0.12f;

        [Tooltip("timescale 영향 여부")]
        [SerializeField] private bool ignoreTimeScale = true;

        [Tooltip("축소 Easing 타입")]
        [SerializeField] private Ease easeIn = Ease.OutQuad;

        [Tooltip("복귀 Easing 타입")]
        [SerializeField] private Ease easeOut = Ease.OutBack;

        private Vector3 _originalScale;
        private Tween _tween;

        protected Transform Target => target != null ? target : transform;

        protected Vector3 OriginalScale => _originalScale;

        protected bool IgnoreTimeScale => ignoreTimeScale;

        protected virtual void Awake()
        {
            _originalScale = Target.localScale;
        }

        /// <summary>축소 → 복귀를 한 번 재생한다. 재생 중 다시 부르면 처음부터 다시 시작한다.</summary>
        protected void Play()
        {
            KillTween();

            Target.localScale = _originalScale;

            _tween = DOTween.Sequence()
                .Append(Target.DOScale(_originalScale * shrinkRatio, shrinkDuration).SetEase(easeIn))
                .Append(Target.DOScale(_originalScale, restoreDuration).SetEase(easeOut))
                .SetUpdate(ignoreTimeScale)
                .SetLink(gameObject);
        }

        /// <summary>
        /// 원래 크기의 ratio배로 옮기고 그대로 둔다.
        ///
        /// Play가 축소 후 스스로 되돌아오는 것과 달리 이쪽은 도착한 자리에 머문다.
        /// 커서를 올려둔 동안 커진 채로 있어야 하는 연출에 쓴다.
        /// </summary>
        protected void ScaleTo(float ratio, float duration, Ease ease)
        {
            KillTween();

            _tween = Target.DOScale(_originalScale * ratio, duration)
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

        protected virtual void OnDisable()
        {
            KillTween();
            Target.localScale = _originalScale;
        }
    }
}
