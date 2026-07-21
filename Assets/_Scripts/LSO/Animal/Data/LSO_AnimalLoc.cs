using UnityEngine;

namespace _Scripts.LSO
{
    public struct LSO_AnimalLoc
    {
        public Vector2Int loc;

        public int X => loc.x;
        public int Y => loc.y;

        private LSO_AnimalLoc(int x, int y)
        {
            this.loc = new Vector2Int(x, y);
        }

        public static LSO_AnimalLoc Create(int row, int col)
        {
            // row = y(세로), col = x(가로) 라고 가정
            return new LSO_AnimalLoc(col, row);
        }

        public override string ToString()
        {
            return $"LSO_AnimalLoc({loc.x}, {loc.y})";
        }
    }
}