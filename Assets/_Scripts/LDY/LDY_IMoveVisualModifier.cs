using UnityEngine;

namespace _Scripts.LDY
{
    /// <summary>
    /// 이동 연출의 속도와 가속을 바꾼다.
    ///
    /// 기본 연출은 모든 기물에게 같은 규칙을 쓴다 — 한 칸당 같은 시간, 등속.
    /// 돌진처럼 "달려든다"를 보여줘야 하는 이동은 그 규칙으로는 표현되지 않는다.
    /// 여섯 칸을 한 칸과 같은 속도로 지나가면 이동이 아니라 순간이동으로 보인다.
    ///
    /// 연출 자체를 특성이 통째로 가져가지 않고 수치만 바꾸게 한 것은 의도된 것이다.
    /// 특성이 직접 코루틴을 돌리면 LDY_MoveSystem.IsBusy에 잡히지 않아,
    /// 연출이 재생되는 도중에 턴이 넘어간다.
    /// </summary>
    public interface LDY_IMoveVisualModifier
    {
        /// <param name="distance">움직인 칸 수(체비쇼프 — 대각선 한 칸도 1이다).</param>
        /// <param name="duration">여기까지 누적된 연출 시간.</param>
        /// <returns>바꾼 연출 시간. 0 이하면 즉시 도착한다.</returns>
        float ModifyMoveDuration(LDY_Animal self, int distance, float duration);

        /// <summary>
        /// 0~1 진행도를 0~1 위치 비율로 바꾸는 곡선. null이면 등속이다.
        /// 값이 1을 넘는 곡선을 주면 목적지를 지나쳤다가 돌아오는 연출이 된다.
        /// </summary>
        AnimationCurve MoveEasing { get; }
    }
}
