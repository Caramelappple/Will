using DG.Tweening;
using UnityEngine;

namespace _Scripts.LSO.UI
{
    /// <summary>
    /// 클릭하면 대상을 Y축으로 한 바퀴 돌린다. 회전 외의 책임은 갖지 않는다.
    /// </summary>
    public class LSO_ClickRotateEffect : MonoBehaviour, LSO_IClickEffect
    {
        [Header("대상")]
        [Tooltip("비워두면 자신의 Transform을 사용한다.")]
        [SerializeField] private Transform target;

        [Header("연출 설정")]
        [Tooltip("회전 바퀴 수")]
        [SerializeField, Min(1)] private int turns = 1;

        [Tooltip("한 바퀴 도는 데 걸리는 시간(초)")]
        [SerializeField, Min(0.01f)] private float duration = 0.4f;

        [Tooltip("체크하면 반대 방향으로 돈다.")]
        [SerializeField] private bool clockwise;

        [Tooltip("timescale 영향 여부")]
        [SerializeField] private bool ignoreTimeScale = true;

        [Tooltip("Easing 타입")]
        [SerializeField] private Ease ease = Ease.OutCubic;

        private Vector3 _originalRotation;
        private Tween _tween;

        private Transform Target => target != null ? target : transform;

        private void Awake()
        {
            _originalRotation = Target.localEulerAngles;
        }

        public void OnClick()
        {
            Play();
        }

        private void Play()
        {
            KillTween();

            // 연타로 각도가 어긋나 쌓이지 않도록 매번 원래 각도에서 다시 시작한다.
            Target.localEulerAngles = _originalRotation;

            float angle = 360f * turns * (clockwise ? -1f : 1f);
            Vector3 destination = _originalRotation + new Vector3(0f, angle, 0f);

            // FastBeyond360: 360도 이상 회전을 최단 경로로 줄이지 않고 그대로 돌린다.
            _tween = Target
                .DOLocalRotate(destination, duration * turns, RotateMode.FastBeyond360)
                .SetEase(ease)
                .SetUpdate(ignoreTimeScale)
                .SetLink(gameObject)
                .OnComplete(() => Target.localEulerAngles = _originalRotation);
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
            Target.localEulerAngles = _originalRotation;
        }
    }
}
