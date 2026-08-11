using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LDY
{
    public class LDY_MoveSystem : MonoBehaviour
    {
        [SerializeField] private LDY_BoardManager board;
        [SerializeField] private LDY_ActionPointManager actionPoints;
        [SerializeField] private float moveDuration = 0.3f;

        // 이동 연출(코루틴)이 하나라도 재생 중이면 true. 턴 전환이 이 애니메이션 도중에 끼어들지 않도록 막는 용도.
        public bool IsBusy => _activeCount > 0;
        private int _activeCount;

        // 체스 킹처럼 대각선 포함 8방향. y(높이)는 타일 값에 관여하지 않는다.
        private static readonly Vector3Int[] Directions =
        {
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(1, 0, 0),
            new Vector3Int(1, 0, 1),
            new Vector3Int(1, 0, -1),
            new Vector3Int(-1, 0, 1),
            new Vector3Int(-1, 0, -1),
        };

        // 이동 칸 수는 기물마다 다르며 동물 데이터(AnimalSO.moveRange)가 원본이다.
        // range를 직접 넘기면 그 값이 우선하므로, 일시적으로 이동력을 늘리는 특성도 그대로 얹을 수 있다.
        // 8방향으로 BFS 확장하며, 반환되는 타일은 항상 y=0으로 정규화된다(클릭 좌표 등과 비교 가능하도록).
        public List<Vector3Int> GetMovableTiles(LDY_Animal animal, int? range = null)
        {
            var result = new List<Vector3Int>();
            if (animal == null) return result;
            if (actionPoints != null && !actionPoints.HasActionPoints) return result;

            int steps = Mathf.Max(1, range ?? animal.MoveRange);

            var start = new Vector3Int(animal.pos.x, 0, animal.pos.z);
            var visited = new HashSet<Vector3Int> { start };
            var frontier = new List<Vector3Int> { start };

            for (int step = 0; step < steps; step++)
            {
                var next = new List<Vector3Int>();
                foreach (var cur in frontier)
                {
                    foreach (var dir in Directions)
                    {
                        var candidate = new Vector3Int(cur.x + dir.x, 0, cur.z + dir.z);
                        if (!board.IsInside(candidate) || visited.Contains(candidate)) continue;
                        visited.Add(candidate);

                        if (board.IsEmpty(candidate))
                        {
                            result.Add(candidate);
                            next.Add(candidate);
                        }
                    }
                }
                frontier = next;
            }

            return result;
        }

        public void MoveTo(LDY_Animal animal, Vector3Int target)
        {
            if (animal == null) return;
            if (!GetMovableTiles(animal).Contains(target)) return;
            if (actionPoints != null && !actionPoints.TryConsume()) return;

            board.Move(animal, animal.pos, target);

            // board.Move가 높이(y)를 유지한 채 animal.pos를 갱신하므로, 연출도 그 최종 위치를 따라간다.
            StartCoroutine(MoveVisual(animal, board.GridToWorld(animal.pos)));
        }

        private IEnumerator MoveVisual(LDY_Animal animal, Vector3 targetWorldPos)
        {
            _activeCount++;
            try
            {
                Transform t = animal.modelTransform;
                Vector3 startPos = t.position;
                float elapsed = 0f;

                while (elapsed < moveDuration)
                {
                    elapsed += Time.deltaTime;
                    t.position = Vector3.Lerp(startPos, targetWorldPos, elapsed / moveDuration);
                    yield return null;
                }

                t.position = targetWorldPos;
            }
            finally
            {
                _activeCount--;
            }
        }
    }
}
