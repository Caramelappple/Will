using UnityEngine;

namespace _Scripts.LSO.Board
{
    public class BoardManager : MonoBehaviour
    {
        public static BoardManager Instance;
        private const int Size = 8;
        
        private void Awake()
        {
            Instance = this;
        }

        private LSO_AnimalBase[,] _board = new LSO_AnimalBase[Size, Size];

        private bool IsInBounds(int row, int col) 
            => row is >= 0 and < Size && col is >= 0 and < Size;
        
        public LSO_AnimalBase SpawnAnimal(GameObject prefab, int row, int col)
        {
            if (!IsInBounds(row, col) || GetAnimal(row, col) != null) return null;

            GameObject obj = Instantiate(prefab);
            LSO_AnimalBase animal = obj.GetComponent<LSO_AnimalBase>();
            animal.Init(this, row, col);
            _board[row, col] = animal;
            return animal;
        }
        
        /// <summary>
        /// 보드에 동물을 추가하는 메서드
        /// </summary>
        /// <param name="animal">동물</param>
        /// <param name="row">가로</param>
        /// <param name="col">세로</param>
        /// <returns></returns>
        public bool AddAnimal(LSO_AnimalBase animal, int row, int col)
        {
            if (animal == null) return false;//동물이 널일떄
            if (row < 0 || row >= 8 || col < 0 || col >= 8) return false;//인덱스를 벗어났을때
            if (_board[row, col] != null) return false;//지정한 인덱스가 차있을때
        
            _board[row, col] = animal;//지워주기
        
            return true;
        }
        
        /// <summary>
        /// 보드에서 동물을 제거 하는 메서드
        /// </summary>
        /// <param name="animal">동물</param>
        /// <param name="row">가로</param>
        /// <param name="col">세로</param>
        /// <returns></returns>
        public bool RemoveAnimal(LSO_AnimalBase animal, int row, int col)
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
            foreach (LSO_AnimalBase animal in _board)
            {
                Destroy(animal.gameObject);
            }
        }
        
        /// <summary>
        /// 보드에서 동물의 정보를 가져오는 메서드
        /// </summary>
        /// <param name="row">가로</param>
        /// <param name="col">세로</param>
        /// <returns></returns>
        public LSO_AnimalBase GetAnimal(int row, int col)
        {
            if (!IsInBounds(row, col)) return null;
            return _board[row, col];
        }

        public LSO_AnimalBase[] IsInRange(LSO_RangeType rangeType, LSO_AnimalBase attacker)
        {
            switch (rangeType)
            {
                case LSO_RangeType.Normal :
                    break;
                case LSO_RangeType.Range :
                    break;
                case LSO_RangeType.Jump :
                    break;
                default:
                    Debug.LogError($"{rangeType} is not a valid range");
                    break;
            }
        }
    }
}
