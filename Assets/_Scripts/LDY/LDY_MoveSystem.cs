using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using _Scripts.LSO.Ability;
using _Scripts.LSO.UI.Effect;
using UnityEngine;

namespace _Scripts.LDY
{
    public class LDY_MoveSystem : MonoBehaviour
    {
        [SerializeField] private LDY_BoardManager board;
        [SerializeField] private LDY_ActionPointManager actionPoints;
        [Tooltip("한 칸을 지나는 데 걸리는 연출 시간. 여러 칸을 움직이면 칸 수에 비례해 늘어난다.")]
        [SerializeField] private float moveDuration = 0.3f;
        [Tooltip("미끄러지듯 가는 대신 떠오르고-이동하고-내려놓는 연출 (KTH).")]
        [SerializeField] private KTH_MoveAnimation moveAnimation = new KTH_MoveAnimation();

        // 이동 연출(코루틴)이 하나라도 재생 중이면 true. 턴 전환이 이 애니메이션 도중에 끼어들지 않도록 막는 용도.
        public bool IsBusy => _activeCount > 0;
        private int _activeCount;
        private readonly HashSet<LDY_Animal> _movingAnimals = new();

        public bool IsMoving(LDY_Animal animal) => animal != null && _movingAnimals.Contains(animal);

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
            if (animal == null || IsMoving(animal)) return result;
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
            // restHeight(KTH)는 배치 때와 같은 이유로 여기서도 더한다 — 안 더하면 이동할 때마다
            // 기물이 자기 바닥 보정치를 잃고 GridToWorld 그대로(바닥)로 내려앉는다.
            StartCoroutine(MoveVisual(animal, from, board.GridToWorld(animal.pos) + Vector3.up * animal.restHeight));
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
            _movingAnimals.Add(animal);
            _activeCount++;
            try
            {
                int distance = Mathf.Max(
                    Mathf.Abs(animal.pos.x - from.x), Mathf.Abs(animal.pos.z - from.z));

                yield return Travel(animal, targetWorldPos, ResolveDuration(animal, distance),
                    ResolveEasing(animal), moveAnimation);

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
                _movingAnimals.Remove(animal);
            }
        }

        private static IEnumerator Travel(
            LDY_Animal animal, Vector3 targetWorldPos, float duration, AnimationCurve easing,
            KTH_MoveAnimation moveAnimation)
        {
            Transform t = animal != null ? animal.modelTransform : null;
            if (t == null) yield break;

            // 이동 방향을 바라보도록 돌려놓는다. 공격 쪽(LDY_AttackSystem)과 마찬가지로
            // 방향을 다시 되돌리지 않는다 — 다음 행동이 있기 전까지 마지막으로 향한 쪽을 계속 본다.
            Vector3 moveDir = targetWorldPos - t.position;
            moveDir.y = 0f;
            if (moveDir.sqrMagnitude > 0.0001f)
                t.rotation = Quaternion.LookRotation(moveDir.normalized, Vector3.up);

            // 이동 애니메이션과 호버 연출(LSO_HoverMoveEffect)이 같은 모델 트랜스폼을 함께
            // 움직인다. 이동 중에 커서가 기물 위를 지나가면, 호버가 "아직 도착하지 않은"
            // 중간 위치를 자기 원래 자리로 캐싱해버려서 이동이 끝난 뒤에도 그 중간 지점으로
            // 다시 스냅되는 문제가 생긴다. 이동하는 동안은 호버 연출만 잠깐 쉬게 한다.
            //
            // LSO_ButtonHoverHandler.enabled를 껐다 켜는 방식은 쓰지 않는다. 유니티 이벤트
            // 시스템의 호버 상태 추적과 어긋나서, 다시 켰을 때 커서가 그대로 머물러 있어도
            // 또 OnHoverEnter가 걸려 기물이 칸과 칸 사이 같은 애매한 위치로 들뜨는 문제가 있었다.
            //
            // restore: false로 끈다 — 선택 해제(Deselect)로 내려오던 중이어도 그 중간 높이를
            // 바닥으로 스냅하지 않고 그대로 둔다. 이동 자체가 KTH_MoveAnimation의 뜨는 동작을
            // 갖고 있어서, 지금 있는 자리에서 그대로 이어받아 뜨면 된다 — 바닥까지 내려오길
            // 기다렸다가 다시 뜨면 "내려갔다 올라오는" 낭비 동작이 됐었다.
            var hoverEffects = animal.GetComponentsInChildren<LSO_HoverMoveEffect>(true);
            foreach (var effect in hoverEffects)
                if (effect != null) effect.SetSuspended(true, restore: false);

            try
            {
                // 뜨는 높이는 따로 정하지 않고 호버 때 뜨는 높이(LSO_HoverMoveEffect.Offset.y)를
                // 그대로 따라간다 — 얼마나 뜨는지를 정하는 주체를 하나로 유지하기 위해서다.
                // 바닥 높이도 지금 화면 위치가 아니라 GroundWorldY()로 구한다 — 위에서 트윈을
                // 스냅 없이 끊었을 뿐이라(restore: false), 이 시점의 실제 위치가 아직 뜬 높이일
                // 수 있다. 둘 다 호버 연출이 없는 기물이면 KTH_MoveAnimation 자체 기본값을 쓴다.
                float? riseHeight = null;
                float? baseHeight = null;
                foreach (var effect in hoverEffects)
                {
                    if (effect == null) continue;
                    riseHeight = effect.Offset.y;
                    baseHeight = effect.GroundWorldY();
                    break;
                }

                // 미끄러지지 않고 떠오르고-이동하고-내려놓는다 (KTH_MoveAnimation).
                // duration/easing은 칸 수·특성에 따라 달라지는 값을 그대로 "이동하는 구간"에 쓴다.
                Sequence sequence = moveAnimation.Play(
                    t, targetWorldPos, duration, easing, animal.gameObject, riseHeight, baseHeight);
                yield return sequence.WaitForCompletion();

                if (t != null)
                    t.position = targetWorldPos;
            }
            finally
            {
                // Unity의 null 비교로 이동 중 파괴된 컴포넌트를 건너뛴다.
                // ClearOffset을 먼저 불러서, 재개될 때 옛 칸의 자리로 끌려가지 않고
                // 방금 도착한 새 칸을 기준으로 삼게 한다.
                foreach (var effect in hoverEffects)
                {
                    if (effect == null) continue;
                    effect.ClearOffset();
                    effect.SetSuspended(false);
                }
            }
        }
    }
}
