using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LDY
{
    // 원거리: 맨해튼 거리(x/z 평면 기준) 1~2칸.
    public class LDY_RangedRange : LDY_IAttackRange
    {
        private const int MinDistance = 1;
        private const int MaxDistance = 2;

        public List<Vector3Int> GetAttackableTiles(Vector3Int from, LDY_BoardManager board)
        {
            var result = new List<Vector3Int>();
            for (int dx = -MaxDistance; dx <= MaxDistance; dx++)
            {
                for (int dz = -MaxDistance; dz <= MaxDistance; dz++)
                {
                    int dist = Mathf.Abs(dx) + Mathf.Abs(dz);
                    if (dist < MinDistance || dist > MaxDistance) continue;

                    var tile = new Vector3Int(from.x + dx, 0, from.z + dz);
                    if (board.IsInside(tile))
                        result.Add(tile);
                }
            }
            return result;
        }
    }
}
