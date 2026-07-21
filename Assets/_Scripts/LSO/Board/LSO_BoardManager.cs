using System.Text;
using _Scripts.LSO.Animal;
using UnityEngine;

namespace _Scripts.LSO.Board
{
    public class LSO_BoardManager : MonoBehaviour
    {
        public static LSO_BoardManager Instance;

        public GameObject buttonPrefab;
        public GameObject board;

        private const int Size = 8;
        private const float CellSize = 0.125f;
        
        private readonly LSO_Animal[,] _board = new LSO_Animal[Size, Size];

        private void Awake()
        {
            Instance = this;
        }

        private bool IsInBounds(LSO_AnimalLoc loc)
            => loc.X is >= 0 and < Size && loc.Y is >= 0 and < Size;
        
        public bool IsEmpty(LSO_AnimalLoc loc)
            => IsInBounds(loc) && _board[loc.X, loc.Y] == null;
        
        public LSO_Animal GetAnimal(LSO_AnimalLoc loc)
            => IsInBounds(loc) ? _board[loc.X, loc.Y] : null;
        
        public int OccupiedCount()
        {
            int count = 0;
            foreach (LSO_Animal a in _board)
                if (a != null) count++;
            return count;
        }
        
        public bool AddAnimal(LSO_Animal animal, LSO_AnimalLoc loc)
        {
            if (animal == null) return false;
            if (!IsEmpty(loc)) return false;

            _board[loc.X, loc.Y] = animal;
            animal.Init(loc);
            return true;
        }
        
        public bool RemoveAnimal(LSO_Animal animal, LSO_AnimalLoc loc)
        {
            if (animal == null) return false;
            if (!IsInBounds(loc)) return false;
            if (_board[loc.X, loc.Y] != animal) return false;

            _board[loc.X, loc.Y] = null;
            return true;
        }
        
        public void MoveAnimal(LSO_AnimalLoc to, LSO_Animal animal)
        {
            if (animal == null) return;
            if (!IsEmpty(to)) return;

            if (!RemoveAnimal(animal, animal.animalLoc))
                return;
            AddAnimal(animal, to);
        }
        
        public void ResetBoard()
        {
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    LSO_Animal animal = _board[x, y];
                    if (animal != null) Destroy(animal.gameObject);
                    _board[x, y] = null;
                }
            }
        }
        
        public Vector3 Board2World(LSO_AnimalLoc loc, Vector3 origin)
        {
            float x = origin.x + loc.X * CellSize + CellSize * 0.5f;
            float y = origin.y - (loc.Y * CellSize + CellSize * 0.5f);
            return new Vector3(x, y, origin.z);
        }

        public void DisplayBtn(LSO_AnimalLoc loc)
        {
            Instantiate(buttonPrefab, Board2World(loc, board.transform.position), Quaternion.identity);
        }
        
        public void Summon(LSO_AnimalLoc loc, LSO_Animal animalData)
        {
            Debug.Log($"[Summon] 호출됨 loc=(X{loc.X},Y{loc.Y})");

            if (animalData == null)
            {
                Debug.LogWarning("[Summon] animalSO가 null입니다.");
                return;
            }

            if (!IsEmpty(loc))
            {
                Debug.LogWarning($"[Summon] 이미 동물이 있는 칸입니다. ({loc.X}, {loc.Y})");
                return;
            }

            LSO_Animal animal = Instantiate(
                animalData,
                Board2World(loc, board.transform.position),
                Quaternion.identity,
                board.transform
            );

            AddAnimal(animal, loc);
        }

        // ─────────────────────── 디버그 조회 ───────────────────────

        /// <summary>
        /// 현재 보드 상태를 콘솔에 격자로 출력한다. (■ = 점유, · = 빈 칸)
        /// 행(r)은 위→아래, 열(c)은 왼→오른. 인스펙터 컴포넌트 우클릭 → Print Board.
        /// </summary>
        [ContextMenu("Print Board")]
        public void PrintBoard()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== Board (■ 점유 · 빈칸) ===");

            sb.Append("     ");
            for (int x = 0; x < Size; x++) sb.Append($"c{x} ");
            sb.AppendLine();

            for (int y = 0; y < Size; y++)          // row
            {
                sb.Append($"r{y} | ");
                for (int x = 0; x < Size; x++)      // col
                    sb.Append(_board[x, y] != null ? " ■ " : " · ");
                sb.AppendLine();
            }

            sb.AppendLine($"점유 칸: {OccupiedCount()}");

            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                LSO_Animal a = _board[x, y];
                if (a == null) continue;
                string an = a.animal != null ? a.animal.animalName : a.name;
                sb.AppendLine($"  (c{x}, r{y}) → {an}");
            }

            Debug.Log(sb.ToString());
        }

        private void OnEnable()
        {
            LSO_ButtonManager.SummonButtonData += Summon;
            LSO_ButtonManager.MoveButtonData += MoveAnimal;
        }

        private void OnDisable()
        {
            LSO_ButtonManager.SummonButtonData -= Summon;
        }
    }
}