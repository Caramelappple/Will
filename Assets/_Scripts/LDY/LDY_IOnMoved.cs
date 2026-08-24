using UnityEngine;

namespace _Scripts.LDY
{
    /// <summary>
    /// 이동이 실제로 일어난 직후 호출된다.
    ///
    /// 돌진처럼 "이동한 것 자체"가 방아쇠인 특성을 위해 있다.
    /// 그 전에는 이동에 끼어들 지점이 아예 없어서, 특성이 보드 변경 신호(OnBoardChanged)를 듣고
    /// 자기 좌표가 바뀌었는지 되짚어보는 수밖에 없었다. 그 방식은 밀려남·소환에도 똑같이 울려서
    /// "내가 움직인 것"과 "누가 나를 움직인 것"을 구분할 수 없다.
    ///
    /// 알림은 LDY_MoveSystem.MoveTo 한 곳에서만 나간다.
    /// 특성이 남을 밀어낼 때 쓰는 LDY_BoardManager.Move는 알리지 않으므로,
    /// 밀어내기가 다시 밀어내기를 부르는 되먹임이 생기지 않는다.
    /// </summary>
    public interface LDY_IOnMoved
    {
        /// <summary>
        /// 자리를 옮긴 것이 확정되고 연출이 시작될 때. 울음소리처럼 출발을 알리는 연출이 여기 붙는다.
        /// 격자상으로는 이미 도착한 뒤라, 보드를 조회하면 목적지에 서 있는 상태로 보인다.
        /// </summary>
        void OnMoveStarted(LDY_Animal self, Vector3Int from, Vector3Int to);

        /// <summary>
        /// 이동 연출까지 끝나 실제로 도착했을 때. 충돌처럼 닿는 순간에 일어나야 하는 것이 여기 붙는다.
        /// </summary>
        /// <param name="self">움직인 기물. 이 특성의 소유자다.</param>
        /// <param name="from">떠나온 칸.</param>
        /// <param name="to">도착한 칸.</param>
        void OnMoved(LDY_Animal self, Vector3Int from, Vector3Int to);
    }
}
