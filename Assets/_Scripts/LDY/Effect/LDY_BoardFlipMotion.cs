using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace _Scripts.LDY.Effect
{
    /// <summary>
    /// 트랜스폼 하나를 월드 공간의 임의 축 둘레로 돌린다. 부모를 바꾸지 않는다.
    ///
    /// 피벗을 만들어 붙였다 떼는 방식을 쓰지 않는 이유:
    /// 보드 루트(LDY_Board)의 부모가 바뀌면 그 아래 타일의 월드 좌표를 읽는 쪽이 전부 영향을 받고,
    /// 연출이 중간에 끊겼을 때 원래 계층으로 되돌려 놓는 책임까지 생긴다.
    /// 시작 시점의 위치·회전만 기억해 두면 매 프레임 정확한 값을 계산할 수 있으므로 그럴 필요가 없다.
    ///
    /// 누적 회전(RotateAround)도 쓰지 않는다. 프레임마다 더하면 오차가 쌓여 180°가 정확히 180°가 되지 않는다.
    /// 대신 항상 "시작 자세 × 지금까지의 각도"를 새로 계산한다.
    /// </summary>
    public sealed class LDY_BoardFlipMotion
    {
        private Transform _target;
        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private Tween _tween;

        /// <summary>이 모션이 대상을 실제로 옮겨놓은 상태인지. Restore로 되돌릴 것이 있는지와 같은 뜻이다.</summary>
        public bool HasMoved { get; private set; }

        /// <param name="pivot">회전축이 지나가는 월드 좌표.</param>
        /// <param name="axis">회전축 방향(월드). 정규화하지 않아도 된다.</param>
        /// <param name="link">
        /// 트윈을 묶어둘 오브젝트. 대상이 파괴되면 트윈도 같이 죽어야 하므로 보통 대상의 GameObject를 넘긴다.
        /// </param>
        public IEnumerator Rotate(
            Transform target,
            Vector3 pivot,
            Vector3 axis,
            float angle,
            float duration,
            Ease ease,
            GameObject link)
        {
            if (target == null) yield break;

            Kill();

            _target = target;
            _startPosition = target.position;
            _startRotation = target.rotation;
            HasMoved = true;

            Vector3 arm = _startPosition - pivot;
            Vector3 unitAxis = axis.sqrMagnitude > Mathf.Epsilon ? axis.normalized : Vector3.right;

            // timeScale이 0으로 잡혀 있어도 연출은 돌아야 한다(유언·계승 시스템이 timeScale을 쥐는 경우가 있다).
            _tween = DOVirtual.Float(0f, 1f, Mathf.Max(0.01f, duration), progress =>
                {
                    if (_target == null) return;

                    Quaternion step = Quaternion.AngleAxis(angle * progress, unitAxis);
                    _target.SetPositionAndRotation(pivot + step * arm, step * _startRotation);
                })
                .SetEase(ease)
                .SetUpdate(true)
                .SetLink(link);

            while (_tween != null && _tween.IsActive() && !_tween.IsComplete())
                yield return null;

            _tween = null;

            // 트윈이 중간에 죽었을 수도 있으므로 끝값을 한 번 못 박는다.
            if (_target != null)
            {
                Quaternion final = Quaternion.AngleAxis(angle, unitAxis);
                _target.SetPositionAndRotation(pivot + final * arm, final * _startRotation);
            }
        }

        /// <summary>돌리기 전 자세로 되돌린다. 연출이 중단됐을 때 쓴다.</summary>
        public void Restore()
        {
            Kill();

            if (!HasMoved) return;

            if (_target != null)
                _target.SetPositionAndRotation(_startPosition, _startRotation);

            HasMoved = false;
            _target = null;
        }

        /// <summary>되돌리지 않고 트윈만 멈춘다.</summary>
        public void Kill()
        {
            if (_tween == null) return;

            if (_tween.IsActive()) _tween.Kill();
            _tween = null;
        }
    }
}
