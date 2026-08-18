using System.Collections;
using System.Collections.Generic;
using _Scripts.LSO.Ability;
using UnityEngine;

namespace _Scripts.LDY
{
    public class LDY_MoveSystem : MonoBehaviour
    {
        [SerializeField] private LDY_BoardManager board;
        [SerializeField] private LDY_ActionPointManager actionPoints;
        [Tooltip("한 칸을 지나는 데 걸리는 연출 시간. 여러 칸을 움직이면 칸 수에 비례해 늘어난다.")]
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
        // 기본 8방향으로 BFS 확장하되, 방향을 좁히는 특성이 있으면 그쪽을 따른다.
        // 반환되는 타일은 항상 y=0으로 정규화된다(클릭 좌표 등과 비교 가능하도록).
        public List<Vector3Int> GetMovableTiles(LDY_Animal animal, int? range = null)
        {
            var result = new List<Vector3Int>();
            if (animal == null) return result;
            if (actionPoints != null && !actionPoints.HasActionPoints) return result;

            int steps = Mathf.Max(1, range ?? animal.MoveRange);
            IReadOnlyList<Vector3Int> directions = ResolveDirections(animal);

            var start = new Vector3Int(animal.pos.x, 0, animal.pos.z);
            var visited = new HashSet<Vector3Int> { start };
            var frontier = new List<Vector3Int> { start };

            for (int step = 0; step < steps; step++)
            {
                var next = new List<Vector3Int>();
                foreach (var cur in frontier)
                {
                    for (int d = 0; d < directions.Count; d++)
                    {
                        Vector3Int dir = directions[d];
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

            Vector3Int from = animal.pos;
            board.Move(animal, animal.pos, target);

            // 검증과 행동력 소모를 모두 통과해 실제로 자리를 옮긴 뒤에만 알린다.
            // 밀려남은 board.Move를 직접 쓰므로 여기로 오지 않는다 — "스스로 움직였다"만 이 신호를 탄다.
            LSO_AbilityNotify.Notify<LDY_IOnMoved>(
                animal.Abilities, a => a.OnMoveStarted(animal, from, animal.pos));

            // board.Move가 높이(y)를 유지한 채 animal.pos를 갱신하므로, 연출도 그 최종 위치를 따라간다.
            StartCoroutine(MoveVisual(animal, from, board.GridToWorld(animal.pos)));
        }

        /// <summary>
        /// 이 기물이 쓸 이동 방향. 좁히는 특성이 없으면 기본 8방향이다.
        /// 이동 가능 타일을 만드는 유일한 지점이라, 여기서 좁히면 AI 후보와
        /// 플레이어가 보는 타일 표시가 함께 좁아진다.
        /// </summary>
        private static IReadOnlyList<Vector3Int> ResolveDirections(LDY_Animal animal)
        {
            IReadOnlyList<LSO_IAbility> abilities = animal.Abilities;
            for (int i = 0; i < abilities.Count; i++)
            {
                if (abilities[i] is LDY_IMoveDirections provider && provider.MoveDirections != null)
                    return provider.MoveDirections;
            }

            return Directions;
        }

        /// <summary>
        /// 연출 시간은 칸 수에 비례한다. 거리와 무관하게 같은 시간을 쓰면
        /// 멀리 갈수록 빨라져서, 여러 칸을 움직이는 기물이 미끄러지지 않고 튀어 보인다.
        /// </summary>
        private float ResolveDuration(LDY_Animal animal, int distance)
        {
            float duration = moveDuration * Mathf.Max(1, distance);

            IReadOnlyList<LSO_IAbility> abilities = animal.Abilities;
            for (int i = 0; i < abilities.Count; i++)
            {
                if (abilities[i] is LDY_IMoveVisualModifier modifier)
                    duration = modifier.ModifyMoveDuration(animal, distance, duration);
            }

            return duration;
        }

        /// <summary>가속 곡선을 쥔 특성이 있으면 그것을 쓴다. 없으면 등속이다.</summary>
        private static AnimationCurve ResolveEasing(LDY_Animal animal)
        {
            IReadOnlyList<LSO_IAbility> abilities = animal.Abilities;
            for (int i = 0; i < abilities.Count; i++)
            {
                if (abilities[i] is LDY_IMoveVisualModifier modifier && modifier.MoveEasing != null)
                    return modifier.MoveEasing;
            }

            return null;
        }

        private IEnumerator MoveVisual(LDY_Animal animal, Vector3Int from, Vector3 targetWorldPos)
        {
            _activeCount++;
            try
            {
                int distance = Mathf.Max(
                    Mathf.Abs(animal.pos.x - from.x), Mathf.Abs(animal.pos.z - from.z));

                yield return Travel(animal, targetWorldPos, ResolveDuration(animal, distance), ResolveEasing(animal));

                // 도착한 뒤에 알린다. 돌진처럼 이동이 방아쇠인 특성은 부딪히는 순간에 맞춰
                // 밀어내기를 일으켜야 하는데, 출발할 때 알리면 황소왕이 아직 오는 중인데
                // 맞은 기물이 먼저 날아간다.
                //
                // _activeCount를 아직 되돌리기 전이라, 여기서 생기는 밀어내기와 사망까지 IsBusy가 덮는다.
                // 밀려남은 board.Move를 직접 쓰므로 여기로 오지 않는다 — "스스로 움직였다"만 이 신호를 탄다.
                if (animal != null)
                {
                    LSO_AbilityNotify.Notify<LDY_IOnMoved>(
                        animal.Abilities, a => a.OnMoved(animal, from, animal.pos));
                }
            }
            finally
            {
                // 중간에 빠져나가도 IsBusy가 켜진 채 남지 않도록 finally에서 되돌린다.
                _activeCount--;
            }
        }

        private static IEnumerator Travel(
            LDY_Animal animal, Vector3 targetWorldPos, float duration, AnimationCurve easing)
        {
            Transform t = animal != null ? animal.modelTransform : null;
            if (t == null) yield break;

            if (duration <= 0f)
            {
                t.position = targetWorldPos;
                yield break;
            }

            Vector3 startPos = t.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                // 연출이 도는 동안 유언·계승이 이 기물을 파괴할 수 있다.
                // 확인하지 않으면 파괴된 Transform에 값을 써서 예외가 나고 연출이 중간에 죽는다.
                // (LDY_AttackSystem.LerpPosition이 같은 이유로 같은 검사를 한다.)
                if (t == null) yield break;

                elapsed += Time.deltaTime;

                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = easing != null ? easing.Evaluate(progress) : progress;

                // 곡선이 1을 넘겨 목적지를 지나쳤다 돌아오는 연출도 그대로 살리려고 Unclamped를 쓴다.
                t.position = Vector3.LerpUnclamped(startPos, targetWorldPos, eased);
                yield return null;
            }

            if (t != null)
                t.position = targetWorldPos;
        }
    }
}
