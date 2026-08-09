using DG.Tweening;
using UnityEngine;

namespace _Scripts.LSO.UI.Feedback
{
    /// <summary>
    /// 대상을 잠깐 흔든다. 언제 흔들지는 모르며, 부르는 쪽이 Play()를 호출한다.
    ///
    /// 버튼 OnClick이나 UnityEvent에 그대로 연결해도 되고,
    /// LSO_RejectShaker가 거부 신호를 받아 대신 불러줘도 된다.
    /// </summary>
    public class LSO_ShakeEffect : MonoBehaviour
    {
        [Tooltip("비워두면 자신의 Transform. UI면 RectTransform이 자동으로 감지된다.")]
        [SerializeField] private Transform target;

        [SerializeField, Min(0.05f)] private float duration = 0.35f;

        [Tooltip("흔들리는 폭. UI는 픽셀, 3D는 월드 단위라 값의 감각이 다르다.")]
        [SerializeField, Min(0f)] private float strength = 16f;

        [Tooltip("클수록 잘게 떤다.")]
        [SerializeField, Min(1)] private int vibrato = 18;

        [SerializeField, Range(0f, 180f)] private float randomness = 90f;

        [Tooltip("좌우로만 흔든다. \"안 돼\"를 뜻하는 고갯짓과 같아서 대체로 이쪽이 읽힌다.")]
        [SerializeField] private bool horizontalOnly = true;

        [Tooltip("timescale 영향 여부.")]
        [SerializeField] private bool ignoreTimeScale = true;

        private RectTransform _rect;
        private Vector2 _originalAnchoredPosition;
        private Vector3 _originalLocalPosition;
        private Tween _tween;

        private Transform Target => target != null ? target : transform;

        private void Awake()
        {
            _rect = Target as RectTransform;

            if (_rect != null)
                _originalAnchoredPosition = _rect.anchoredPosition;
            else
                _originalLocalPosition = Target.localPosition;
        }

        /// <summary>한 번 흔든다. 흔드는 중에 다시 부르면 처음부터 다시 시작한다.</summary>
        public void Play()
        {
            KillTween();
            RestorePosition();

            Vector3 axis = horizontalOnly
                ? new Vector3(strength, 0f, 0f)
                : new Vector3(strength, strength, 0f);

            _tween = _rect != null
                ? _rect.DOShakeAnchorPos(duration, axis, vibrato, randomness, false, true)
                : Target.DOShakePosition(duration, axis, vibrato, randomness, false, true);

            _tween.SetUpdate(ignoreTimeScale)
                .SetLink(gameObject)
                .OnKill(() => _tween = null);
        }

        private void KillTween()
        {
            if (_tween == null) return;

            _tween.Kill();
            _tween = null;
        }

        /// <summary>
        /// 흔들기가 중간에 끊기면 어긋난 위치로 굳는다. 원래 자리로 되돌린다.
        /// </summary>
        private void RestorePosition()
        {
            if (_rect != null)
                _rect.anchoredPosition = _originalAnchoredPosition;
            else
                Target.localPosition = _originalLocalPosition;
        }

        private void OnDisable()
        {
            KillTween();
            RestorePosition();
        }
    }
}
