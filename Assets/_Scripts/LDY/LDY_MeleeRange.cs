using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LDY
{
    // 근접: 체스 킹처럼 대각선 포함 8방향 1칸.
    public class LDY_MeleeRange : LDY_IAttackRange
    {
        private static readonly Vector3Int[] Directions =
        {
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(1, 0, 0),
            new Vector3Int(1, 0, 1),
            new Vector3Int(1, 0, -1),
            new Vector3Int(-1, 0, 1),
            new Vector3Int(-1, 0, -1),
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
