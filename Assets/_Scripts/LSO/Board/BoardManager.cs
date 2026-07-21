using _Scripts.LSO.Animal;
using UnityEngine;

namespace _Scripts.LSO.Board
{
    public class BoardManager : MonoBehaviour
    {
        public static BoardManager Instance;
        public GameObject buttonPrefab;
        public GameObject board;
        private const int Size = 8;
        
        private void Awake()
        {
            Instance = this;
        }

        private LSO_Animal[,] _board = new LSO_Animal[Size, Size];

        private bool IsInBounds(LSO_AnimalLoc loc) => loc.loc.x is >= 0 and < Size && loc.loc.y is >= 0 and < Size;

        private bool IsEmpty(LSO_Animal animal,LSO_AnimalLoc loc)
        {
            if (animal == null) return false;
            if (!IsInBounds(loc)) return false;
            if (GetAnimal(loc) != animal) return false;
            
            return true;
        }
        
        public LSO_Animal SpawnAnimal(GameObject prefab, LSO_AnimalLoc loc)
        {
            if (!IsInBounds(loc) || GetAnimal(loc) != null) return null;

            GameObject obj = Instantiate(prefab);
            LSO_Animal animal = obj.GetComponent<LSO_Animal>();
            animal.Init(loc);
            return animal;
        }
        
        /// <summary>
        /// 보드에 동물을 추가하는 메서드
        /// </summary>
        public bool AddAnimal(LSO_Animal animal, LSO_AnimalLoc loc)
        {
            if (IsEmpty(animal, loc)) 
                return true;
            return false;
        }
        
        /// <summary>
        /// 보드에서 동물을 제거 하는 메서드
        /// </summary>
        /// <param name="animal">동물</param>
        /// <param name="row">가로</param>
        /// <param name="col">세로</param>
        /// <returns></returns>
        public bool RemoveAnimal(LSO_Animal animal, int row, int col)
        {
            if (animal == null) return false;//동물이 널일떄
            if (row < 0 || row >= 8 || col < 0 || col >= 8) return false;//인덱스를 벗어났을때
            if (_board[row, col] != animal) return false;

            _board[row, col] = null;//넣어주기
        
            return true;
        }
        
        /// <summary>
        /// 보드를 초기화 시키는 메서드
        /// </summary>
        public void ResetBoard()
        {
            foreach (LSO_Animal animal in _board)
            {
                
            }
        }
        
        /// <summary>
        /// 보드에서 동물의 정보를 가져오는 메서드
        /// </summary>
        public LSO_Animal GetAnimal(LSO_AnimalLoc loc)
        {
            if (!IsInBounds(loc)) return null;
            return _board[loc.loc.x, loc.loc.y];
        }

        public bool MoveAnimal(LSO_Animal animal, LSO_AnimalLoc loc)
        {
            if (!IsEmpty(animal, loc)) return false;

            return true;
        }

        public void DisplayBtn(LSO_AnimalLoc loc)
        {
            Instantiate(buttonPrefab, Board2World(loc,board.transform.position), Quaternion.identity);
        }

        public Vector3 Board2World(LSO_AnimalLoc loc, Vector3 pos)
        {
            const float cellSize = 0.125f;
            //const int boardSize = Size;

            float x = pos.x + loc.loc.x * cellSize + cellSize * 0.5f;
            float y = pos.y - (loc.loc.y * cellSize + cellSize * 0.5f);

            return new Vector3(x, y, pos.z);
        }

        public void Summon(LSO_AnimalLoc loc, LSO_Animal animal)
        {
            Vector3 pos = new Vector3(loc.loc.x,0 , loc.loc.y);
            Instantiate(buttonPrefab, pos , Quaternion.identity);
            AddAnimal(null,loc);
        }

        private void OnEnable()
        {
            LSO_ButtonManager.SummonButtonData += Summon;
        }
    }
}
