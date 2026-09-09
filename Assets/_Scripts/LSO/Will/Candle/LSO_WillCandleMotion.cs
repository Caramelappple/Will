using System;
using DG.Tweening;
using UnityEngine;

namespace _Scripts.LSO.Will.Candle
{
    /// <summary>
    /// 유언을 붙일 때 양초가 카드 쪽으로 기울여 다가갔다 돌아온다.
    ///
    /// ── 도장과 뭐가 다른가 ────────────────────────────────────
    /// 도장은 "찍는" 동작이라 접촉 순간이 정확해야 했다. 눌리는 깊이, 종이가 눌리는 정도,
    /// 떼는 타이밍이 조금만 어긋나도 어설퍼 보였다.
    ///
    /// 불을 대는 것은 **닿을 필요가 없다.** 가까이 가기만 하면 되고,
    /// "닿았다"는 신호는 카드 쪽 연출(LSO_WillRevealEffect)이 대신 낸다.
    /// 그래서 위치 트윈 하나로 끝난다.
    /// ─────────────────────────────────────────────────────────
    ///
    /// 집으로 돌아갈 자리는 Awake 때의 자세다. 그 뒤로 양초를 옮기면
    /// 옮긴 자리가 아니라 처음 자리로 돌아가므로, 옮길 일이 있으면 SetHome 을 부를 것.
    ///
    /// 씬 배선: 양초 오브젝트에 붙이기만 하면 된다. LSO_WillPainter 가 찾아 쓴다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LSO_WillCandleMotion : MonoBehaviour
    {
        [Header("다가가기")]
        [Tooltip("카드까지의 거리 중 얼마나 갈지. 1이면 카드 자리까지 가서 겹친다.\n" +
                 "0.7 쯤이면 가까이 기울인 것처럼 보이면서 카드를 가리지 않는다.")]
        [SerializeField, Range(0.1f, 1f)] private float reachRatio = 0.7f;

        [Tooltip("다가가는 데 걸리는 시간.")]
        [SerializeField, Min(0.01f)] private float reachDuration = 0.25f;

        [SerializeField] private Ease reachEase = Ease.OutCubic;

        [Tooltip("다가가면서 기울이는 각도(도). 불을 대는 모양이 난다.\n" +
                 "0이면 기울이지 않고 평행하게 움직인다.")]
        [SerializeField, Range(0f, 90f)] private float tiltAngle = 25f;

        [Header("머무르기")]
        [Tooltip("닿은 자리에 머무는 시간. 이 사이에 카드에서 아이콘이 드러난다.")]
        [SerializeField, Min(0f)] private float holdDuration = 0.35f;

        [Header("돌아오기")]
        [Tooltip("제자리로 돌아오는 데 걸리는 시간.")]
        [SerializeField, Min(0.01f)] private float returnDuration = 0.3f;

        [SerializeField] private Ease returnEase = Ease.InOutCubic;

        private Vector3 _homePosition;
        private Quaternion _homeRotation;
        private Sequence _sequence;

        /// <summary>지금 움직이는 중인지.</summary>
        public bool IsMoving => _sequence != null && _sequence.IsActive() && _sequence.IsPlaying();

        private void Awake()
        {
            SetHome();
        }

        /// <summary>
        /// 지금 자세를 "집"으로 삼는다. 양초를 옮긴 뒤에 부른다.
        ///
        /// 움직이는 중에 부르면 어중간한 자리가 집이 되므로, 먼저 끊고 잡는다.
        /// </summary>
        public void SetHome()
        {
            Stop();

            transform.GetPositionAndRotation(out _homePosition, out _homeRotation);
        }

        /// <summary>
        /// 그 자리로 기울여 다가갔다 돌아온다.
        /// </summary>
        /// <param name="worldTarget">불을 댈 자리. 보통 카드의 위치다.</param>
        /// <param name="onTouch">
        /// 가장 가까이 닿은 순간에 부른다. 아이콘이 드러나는 것을 여기 건다.
        ///
        /// 다가가기 전에 부르면 아직 멀리 있는데 아이콘이 뜨고,
        /// 돌아온 뒤에 부르면 이미 떠난 자리에서 뜬다.
        /// </param>
        public void Reach(Vector3 worldTarget, Action onTouch = null)
        {
            // 연달아 누르면 하던 것을 끊고 새 목표로 간다.
            // 큐에 쌓으면 손을 뗀 뒤에도 양초가 혼자 돌아다닌다.
            Stop();

            if (!isActiveAndEnabled)
            {
                // 꺼져 있으면 트윈이 돌지 않는다. 결과만 알린다.
                onTouch?.Invoke();
                return;
            }

            Vector3 near = Vector3.Lerp(_homePosition, worldTarget, reachRatio);
            Quaternion tilted = TiltToward(worldTarget);

            _sequence = DOTween.Sequence()
                .Append(transform.DOMove(near, reachDuration).SetEase(reachEase))
                .Join(transform.DORotateQuaternion(tilted, reachDuration).SetEase(reachEase))
                .AppendCallback(() => onTouch?.Invoke())
                .AppendInterval(holdDuration)
                .Append(transform.DOMove(_homePosition, returnDuration).SetEase(returnEase))
                .Join(transform.DORotateQuaternion(_homeRotation, returnDuration).SetEase(returnEase))
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(() => _sequence = null);
        }

        /// <summary>
        /// 목표 쪽으로 기울인 자세.
        ///
        /// 초의 위쪽(로컬 +Y)이 목표를 향해 눕는 모양이다.
        /// 각도를 제한하는 이유는 완전히 눕히면 초가 뒤집혀 보이기 때문이다.
        /// </summary>
        private Quaternion TiltToward(Vector3 worldTarget)
        {
            if (tiltAngle <= 0f) return _homeRotation;

            Vector3 toTarget = worldTarget - _homePosition;

            if (toTarget.sqrMagnitude <= Mathf.Epsilon) return _homeRotation;

            // 초를 눕히는 축은 "목표 방향"과 "초가 선 방향"에 모두 직각인 축이다.
            Vector3 up = _homeRotation * Vector3.up;
            Vector3 axis = Vector3.Cross(up, toTarget.normalized);

            if (axis.sqrMagnitude <= Mathf.Epsilon) return _homeRotation;

            return Quaternion.AngleAxis(tiltAngle, axis.normalized) * _homeRotation;
        }

        /// <summary>움직임을 끊는다. 자세는 그대로 둔다.</summary>
        public void Stop()
        {
            _sequence?.Kill();
            _sequence = null;
        }

        /// <summary>
        /// 도중에 꺼지면 기울어진 자리에서 굳는다. 집으로 못 박는다.
        ///
        /// 이번 세션에 같은 종류로 세 번 물렸다 — 중단 경로에서 상태가 남는 것.
        /// </summary>
        private void OnDisable()
        {
            Stop();

            transform.SetPositionAndRotation(_homePosition, _homeRotation);
        }
    }
}
