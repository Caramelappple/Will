using System;
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

        /// <summary>
        /// 기물 배치가 바뀌었을 때 발행된다(소환 · 이동 · 퇴장 · 초기화).
        ///
        /// 값을 싣지 않는 "다시 계산해라" 신호다.
        /// 공격력처럼 특성이 매번 계산해서 만드는 값은 대입되는 순간이 없어서 정확한 값을 실을 수가 없다.
        /// 대신 그런 값이 결국 의존하는 건 보드 상태이므로, 여기서 알리면 구독자가 필요할 때 다시 계산한다.
        ///
        /// 새 특성이 무엇에 의존하든 이 신호에 따라오기 때문에 특성을 추가할 때 손볼 곳이 없다.
        /// </summary>
        public event Action OnBoardChanged;

        // Awake에서 씬의 기물을 한꺼번에 등록할 때 기물 수만큼 신호가 나가는 걸 막는다.
        private bool _suppressChangeNotice;

        private void Awake()
        {
            // 특성 등 보드 조회가 필요한 쪽이 찾아올 수 있도록 스스로 등록한다.
            GameManager.Instance?.RegisterBoard(this);

            // 씬에 이미 배치된 3D 기물들을 각자의 pos 필드(Inspector에서 미리 설정)를 기준으로 보드에 등록한다.
            // 이 구간은 "여러 번의 변화"가 아니라 "한 번의 초기 구성"이므로 신호를 묶어서 한 번만 보낸다.
            _suppressChangeNotice = true;

            var animals = FindObjectsByType<LDY_Animal>(FindObjectsSortMode.None);
            foreach (var animal in animals)
                Place(animal, animal.pos);

            _suppressChangeNotice = false;
            RaiseBoardChanged();
        }

        private void RaiseBoardChanged()
        {
            if (_suppressChangeNotice) return;

            OnBoardChanged?.Invoke();
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

        /// <summary>
        /// 기물을 칸에 등록한다. 이미 다른 기물이 서 있으면 놓지 않는다.
        ///
        /// 덮어쓰기를 허용하면 밀려난 기물은 씬 오브젝트로는 살아 있는데 격자에서만 사라진다.
        /// 그 기물은 GetAllByTeam·공격 대상·AI 판단에서 전부 빠지므로 죽일 수도 없는 유령이 되고,
        /// 원인이 된 호출은 아무 흔적도 남기지 않는다. 조용히 지우느니 놓지 않는 편이 낫다.
        /// </summary>
        public void Place(LDY_Animal animal, Vector3Int p)
        {
            if (animal == null || !IsInside(p)) return;

            LDY_Animal occupant = _grid[p.x, p.z];
            if (occupant != null && occupant != animal)
            {
                Debug.LogWarning(
                    $"[LDY_BoardManager] {p} 칸에 이미 {occupant.name}이(가) 있어 {animal.name}을(를) 놓지 않는다. " +
                    "인스펙터의 pos가 겹쳤거나 소환 전 빈 칸 검사를 건너뛴 것이다.", animal);
                return;
            }

            // 다른 칸에 등록돼 있던 기물을 옮겨 놓는 경우다.
            // 이전 칸을 비우지 않으면 같은 기물이 두 칸에서 동시에 조회되어, 있지도 않은 자리가 공격 대상이 된다.
            if (IsInside(animal.pos) && _grid[animal.pos.x, animal.pos.z] == animal)
                _grid[animal.pos.x, animal.pos.z] = null;

            _grid[p.x, p.z] = animal;
            animal.pos = p;
            animal.modelTransform.position = GridToWorld(p);

            RaiseBoardChanged();
        }

        public void Move(LDY_Animal animal, Vector3Int from, Vector3Int to)
        {
            if (animal == null || !IsInside(from) || !IsInside(to)) return;
            if (_grid[from.x, from.z] != animal) return;

            // Place와 같은 이유로 막는다. 점유된 칸으로 밀고 들어가면 원래 있던 기물이 유령이 된다.
            // 호출부(LDY_MoveSystem)는 빈 칸만 후보로 주므로, 이 경고가 뜬다면 검증과 실행 사이에서
            // 보드가 바뀐 것이니 그 경로를 찾아야 한다.
            LDY_Animal occupant = _grid[to.x, to.z];
            if (occupant != null && occupant != animal)
            {
                Debug.LogWarning(
                    $"[LDY_BoardManager] {to} 칸에 {occupant.name}이(가) 있어 {animal.name}을(를) 옮기지 않는다.", animal);
                return;
            }

            _grid[from.x, from.z] = null;
            _grid[to.x, to.z] = animal;
            animal.pos = new Vector3Int(to.x, animal.pos.y, to.z);

            RaiseBoardChanged();
        }

        public void Remove(LDY_Animal animal)
        {
            if (animal == null || !IsInside(animal.pos)) return;

            // 실제로 지운 경우에만 알린다. 이미 없는 기물에 Remove가 또 들어와도 헛신호가 나가지 않는다.
            if (_grid[animal.pos.x, animal.pos.z] != animal) return;

            _grid[animal.pos.x, animal.pos.z] = null;

            RaiseBoardChanged();
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

            // 칸마다 알리면 최대 64번이 나간다. 스테이지 교체는 한 번의 변화로 취급한다.
            RaiseBoardChanged();
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
