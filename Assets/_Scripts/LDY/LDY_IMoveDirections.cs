using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LDY
{
    /// <summary>
    /// 이 기물이 쓸 수 있는 이동 방향을 정한다.
    ///
    /// 기본은 체스 킹처럼 대각선을 포함한 8방향이다.
    /// 황소왕의 돌진처럼 룩으로 움직이는 기물은 이 훅으로 방향을 좁힌다.
    ///
    /// AI 점수로 대각선을 억제하지 않고 이동 시스템에서 막는 것은 의도된 것이다.
    /// 점수는 "가고 싶지 않다"일 뿐 "갈 수 없다"가 아니라서,
    /// 다른 scorer가 더 큰 점수를 얹으면 뚫린다. 무엇보다 플레이어가 직접 조종할 때는
    /// 점수를 아예 거치지 않으므로, 이동 가능 타일 표시부터 대각선이 켜진다.
    /// </summary>
    public interface LDY_IMoveDirections
    {
        /// <summary>쓸 수 있는 방향. null을 돌려주면 기본 8방향을 쓴다.</summary>
        IReadOnlyList<Vector3Int> MoveDirections { get; }
    }
}
