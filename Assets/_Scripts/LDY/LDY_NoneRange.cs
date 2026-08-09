using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LDY
{
    public class LDY_NoneRange: LDY_IAttackRange
    {
        public List<Vector3Int> GetAttackableTiles(Vector3Int from, LDY_BoardManager board)
        {
            return new List<Vector3Int>();
        }
    }
}