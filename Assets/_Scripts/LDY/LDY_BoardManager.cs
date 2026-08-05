using System.Collections.Generic;
using _Scripts.LSO;
using UnityEngine;

namespace _Scripts.LDY
{
    public class LDY_BoardManager : MonoBehaviour
    {
        public const int Size = 8;

        // 씬 배선: boardOrigin에는 격자 (0,0) 칸의 월드 좌표를 가리키는 오브젝트를 연결할 것.
        // cellSize는 한 칸 이동 시 월드 X/Z가 실제로 움직이는 거리(현재 0.75)에 맞출 것.
        // heightStep은 LDY_Animal.pos.y(모델 표시용 높이 레이어) 1당 월드 Y로 몇 유닛을 올릴지 정한다.
        [SerializeField] private Transform boardOrigin;
        [SerializeField] private float cellSize = 0.75f;
        [SerializeField] private float heightStep = 1f;

        // 격자 저장은 x/z만 사용한다. pos.y(높이)는 표시 전용이라 점유 판정에 영향을 주지 않는다.
        private readonly LDY_Animal[,] _grid = new LDY_Animal[Size, Size];

        private void Awake()
        {
            // 특성 등 보드 조회가 필요한 쪽이 찾아올 수 있도록 스스로 등록한다.
            GameManager.Instance?.RegisterBoard(this);

            // 씬에 이미 배치된 3D 기물들을 각자의 pos 필드(Inspector에서 미리 설정)를 기준으로 보드에 등록한다.
            var animals = FindObjectsByType<LDY_Animal>(FindObjectsSortMode.None);
            foreach (var animal in animals)
                Place(animal, animal.pos);
        }

        private void OnDestroy()
        {
            if (GameManager.HasInstance)
                GameManager.Instance.UnregisterBoard(this);
        }

        public bool IsInside(Vector3Int p)
        {
            return p.x >= 0 && p.x < Size && p.z >= 0 && p.z < Size;
        }

        public bool IsEmpty(Vector3Int p)
        {
            return IsInside(p) && _grid[p.x, p.z] == null;
        }

        public LDY_Animal Get(Vector3Int p)
        {
            return IsInside(p) ? _grid[p.x, p.z] : null;
        }

        public void Place(LDY_Animal animal, Vector3Int p)
        {
            if (animal == null || !IsInside(p)) return;

            _grid[p.x, p.z] = animal;
            animal.pos = p;
            animal.modelTransform.position = GridToWorld(p);
        }

        public void Move(LDY_Animal animal, Vector3Int from, Vector3Int to)
        {
            if (animal == null || !IsInside(from) || !IsInside(to)) return;
            if (_grid[from.x, from.z] != animal) return;

            _grid[from.x, from.z] = null;
            _grid[to.x, to.z] = animal;
            animal.pos = new Vector3Int(to.x, animal.pos.y, to.z);
        }

        public void Remove(LDY_Animal animal)
        {
            if (animal == null || !IsInside(animal.pos)) return;
            if (_grid[animal.pos.x, animal.pos.z] == animal)
                _grid[animal.pos.x, animal.pos.z] = null;
        }

        /// <summary>보드 위의 모든 기물을 격자에서 지우고 오브젝트도 파괴한다. 스테이지를 갈아끼울 때 쓴다.</summary>
        public void ClearAll()
        {
            for (int x = 0; x < Size; x++)
            {
                for (int z = 0; z < Size; z++)
                {
                    var animal = _grid[x, z];
                    _grid[x, z] = null;

                    if (animal != null)
                        Destroy(animal.gameObject);
                }
            }
        }

        public List<LDY_Animal> GetAllByTeam(LDY_Team team)
        {
            var result = new List<LDY_Animal>();
            for (int x = 0; x < Size; x++)
            {
                for (int z = 0; z < Size; z++)
                {
                    var animal = _grid[x, z];
                    if (animal != null && animal.team == team)
                        result.Add(animal);
                }
            }
            return result;
        }

        public Vector3 GridToWorld(Vector3Int p)
        {
            Vector3 origin = boardOrigin != null ? boardOrigin.position : Vector3.zero;
            return origin + new Vector3(p.x * cellSize, p.y * heightStep, p.z * cellSize);
        }

        // 타일 클릭 판정은 항상 보드 바닥 기준이므로 높이(y)는 0으로 고정해서 반환한다.
        // (이동 가능 칸 등 다른 타일 좌표도 전부 y=0으로 통일되어 있어 비교가 어긋나지 않는다.)
        public Vector3Int WorldToGrid(Vector3 worldPos)
        {
            Vector3 origin = boardOrigin != null ? boardOrigin.position : Vector3.zero;
            Vector3 local = worldPos - origin;
            int x = Mathf.RoundToInt(local.x / cellSize);
            int z = Mathf.RoundToInt(local.z / cellSize);
            return new Vector3Int(x, 0, z);
        }
    }
}
