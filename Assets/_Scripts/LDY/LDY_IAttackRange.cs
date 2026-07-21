using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LDY
{
    public interface LDY_IAttackRange
    {
        // from은 공격자의 pos(x,y,z)이지만, 반환 타일은 항상 y=0으로 정규화된다.
        List<Vector3Int> GetAttackableTiles(Vector3Int from, LDY_BoardManager board);
    }
}
