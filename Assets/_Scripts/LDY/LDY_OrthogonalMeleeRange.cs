using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LDY
{
    /// <summary>
    /// 룩처럼 상하좌우 1칸. 대각선은 닿지 않는다.
    ///
    /// Melee(체스 킹, 8방향)와 나눠 둔 이유는 황소왕처럼 상하좌우로만 움직이는 기물 때문이다.
    /// 이동은 룩인데 공격만 킹이면, 옆으로 갈 수 없는 대각선 칸을 때리는 그림이 된다.
    /// </summary>
    public class LDY_OrthogonalMeleeRange : LDY_IAttackRange
    {
        private static readonly Vector3Int[] Directions =
        {
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1),
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
        };

        public List<Vector3Int> GetAttackableTiles(Vector3Int from, LDY_BoardManager board)
        {
            var result = new List<Vector3Int>();
            foreach (var dir in Directions)
            {
                var tile = new Vector3Int(from.x + dir.x, 0, from.z + dir.z);
                if (board.IsInside(tile))
                    result.Add(tile);
            }
            return result;
        }
    }
}
