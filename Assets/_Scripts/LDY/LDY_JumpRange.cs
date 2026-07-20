using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LDY
{
    // 점프: 정확히 3칸 (1·2칸 불가).
    // 점프 패턴 확정 시 수정: 현재는 4방향(x/z 평면) 직선으로 정확히 3칸 떨어진 칸만 공격 가능하다고 가정한다.
    public class LDY_JumpRange : LDY_IAttackRange
    {
        private const int Distance = 3;

        private static readonly Vector3Int[] Directions =
        {
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(1, 0, 0),
        };

        public List<Vector3Int> GetAttackableTiles(Vector3Int from, LDY_BoardManager board)
        {
            var result = new List<Vector3Int>();
            foreach (var dir in Directions)
            {
                var tile = new Vector3Int(from.x + dir.x * Distance, 0, from.z + dir.z * Distance);
                if (board.IsInside(tile))
                    result.Add(tile);
            }
            return result;
        }
    }
}
