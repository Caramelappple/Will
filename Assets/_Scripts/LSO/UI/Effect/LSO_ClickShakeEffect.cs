using DG.Tweening;
using UnityEngine;
using _Scripts.LSO.UI.Input;

namespace _Scripts.LSO.UI.Effect
{
    /// <summary>
    /// 클릭하면 대상을 좌우로 갸웃하듯 흔든다. 흔드는 것 외의 책임은 갖지 않는다.
    ///
    /// 자리를 옮기지 않고 Z축 회전만 쓴다.
    /// 위치를 흔들면 레이아웃 그룹이 자리를 다시 잡을 때 서로 밀어내지만,
    /// 회전은 레이아웃이 건드리지 않아 목록 안에서도 안전하다.
    ///
    /// LSO_ButtonClickHandler가 붙은 버튼에 같이 달아두면 눌릴 때마다 재생된다.
    /// </summary>
    public class LSO_ClickShakeEffect : MonoBehaviour, LSO_IClickEffect
    {
        [Header("대상")]
        [Tooltip("비워두면 자신의 Transform을 사용한다.")]
        [SerializeField] private Transform target;

        [Header("연출 설정")]
        [Tooltip("흔들리는 시간(초)")]
        [SerializeField, Min(0.01f)] private float duration = 0.25f;

        [Tooltip("기울어지는 각도. 클릭 반응이라 작을수록 자연스럽다.")]
        [SerializeField, Range(0f, 45f)] private float angle = 7f;

        [Tooltip("좌우로 오가는 횟수. 홀수여야 원래 각도로 끝난다.")]
        [SerializeField, Min(1)] private int swings = 3;

        [Tooltip("체크하면 반대쪽으로 먼저 기운다.")]
        [SerializeField] private bool startClockwise;

        [Tooltip("timescale 영향 여부")]
        [SerializeField] private bool ignoreTimeScale = true;

        [Tooltip("Easing 타입")]
        [SerializeField] private Ease ease = Ease.InOutSine;

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

            float first = startClockwise ? -angle : angle;

            // 한 번 오갈 때마다 반대쪽으로 넘어가고 폭이 줄어든다.
            // 마지막에 원래 각도로 돌아오는 구간이 하나 더 붙으므로 시간을 그만큼 나눈다.
            float step = duration / (swings + 1);

            Sequence sequence = DOTween.Sequence();

            for (int i = 0; i < swings; i++)
            {
                float falloff = 1f - (float)i / (swings + 1);
                float side = i % 2 == 0 ? first : -first;

                sequence.Append(
                    Target.DOLocalRotate(Tilt(side * falloff), step).SetEase(ease));
            }

            sequence.Append(Target.DOLocalRotate(_originalRotation, step).SetEase(ease));

            _tween = sequence
                .SetUpdate(ignoreTimeScale)
                .SetLink(gameObject)
                .OnComplete(() => Target.localEulerAngles = _originalRotation);
        }

        private Vector3 Tilt(float z)
        {
            return _originalRotation + new Vector3(0f, 0f, z);
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
