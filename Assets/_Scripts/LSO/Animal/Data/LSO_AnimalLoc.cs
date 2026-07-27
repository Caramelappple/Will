using UnityEngine;

namespace _Scripts.LSO
{
    /// <summary>
    /// 보드 좌표를 나타내는 불변(immutable) 구조체.
    /// 규약: X = 가로(col), Y = 세로(row).  Create(row, col)로 생성한다.
    /// </summary>
    public readonly struct LSO_AnimalLoc
    {
        private readonly Vector2Int _loc;

        public int X => _loc.x;
        public int Y => _loc.y;

        private LSO_AnimalLoc(int x, int y)
        {
            _loc = new Vector2Int(x, y);
        }

        /// <summary>
        /// row = Y(세로), col = X(가로) 규약으로 좌표를 생성한다.
        /// </summary>
        public static LSO_AnimalLoc Create(int row, int col)
        {
            return new LSO_AnimalLoc(col, row);
        }

        public override string ToString() => $"LSO_AnimalLoc(X:{_loc.x}, Y:{_loc.y})";
    }
}