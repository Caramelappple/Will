using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LDY.Boss.BullKing
{
    /// <summary>
    /// 돌진 한 번의 기하학. 어느 방향으로 어디까지 가고 무엇에 부딪히는지만 담는다.
    /// 피해·밀어내기 같은 규칙은 들어 있지 않다.
    /// </summary>
    public readonly struct LDY_ChargeLine
    {
        /// <summary>돌진 방향. 상하좌우 중 하나이며 길이는 항상 1이다.</summary>
        public readonly Vector3Int Direction;

        /// <summary>실제로 멈춰 서는 칸. 부딪힌 기물의 칸으로는 들어가지 않는다.</summary>
        public readonly Vector3Int Destination;

        /// <summary>달린 칸 수.</summary>
        public readonly int Steps;

        /// <summary>돌진을 멈춰 세운 기물. 벽에 막혔거나 최대 거리까지 갔으면 null.</summary>
        public readonly LDY_Animal Blocker;

        public bool Moves => Steps > 0;
        public bool Collides => Blocker != null;

        public LDY_ChargeLine(Vector3Int direction, Vector3Int destination, int steps, LDY_Animal blocker)
        {
            Direction = direction;
            Destination = destination;
            Steps = steps;
            Blocker = blocker;
        }
    }

    /// <summary>
    /// 돌진 경로 계산. AI가 후보를 평가할 때와 특성이 충돌을 처리할 때가 같은 답을 봐야 하므로
    /// 계산은 전부 여기 하나에 둔다. 양쪽에 따로 적으면 "AI는 3명을 밀 줄 알았는데 실제로는 2명"이 된다.
    ///
    /// 판정 함수가 둘인 것은 의도된 것이다. 이동 전과 이동 후는 보드가 다르다.
    ///   TryPlan     — 아직 안 움직였다. 황소왕이 출발 칸에 서 있으므로 앞을 훑으면 된다.
    ///   TryIdentify — 이미 움직였다. 도착 칸을 황소왕 자신이 차지하고 있어서 같은 방식으로 훑으면
    ///                 자기 자신을 장애물로 집는다. 그래서 도착 칸 바로 앞만 들여다본다.
    /// </summary>
    public static class LDY_ChargePath
    {
        /// <summary>돌진은 룩처럼 상하좌우로만 달린다. 대각선은 포함하지 않는다.</summary>
        public static readonly Vector3Int[] Directions =
        {
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1),
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
        };

        /// <summary>
        /// 아직 움직이지 않은 상태에서 이 방향으로 달리면 어떻게 되는지 계산한다.
        /// </summary>
        public static LDY_ChargeLine Resolve(
            LDY_BoardManager board, Vector3Int origin, Vector3Int direction, int maxSteps)
        {
            if (board == null || maxSteps <= 0) return default;

            Vector3Int start = Flatten(origin);
            int steps = 0;
            LDY_Animal blocker = null;

            for (int i = 1; i <= maxSteps; i++)
            {
                Vector3Int tile = start + direction * i;

                // 판 밖은 그냥 멈추는 것이다. 벽에 부딪히는 건 밀려나는 기물 쪽 규칙이지
                // 황소왕이 벽에 박는 규칙은 없다.
                if (!board.IsInside(tile)) break;

                LDY_Animal occupant = board.Get(tile);
                if (occupant != null)
                {
                    blocker = occupant;
                    break;
                }

                steps = i;
            }

            return new LDY_ChargeLine(direction, start + direction * steps, steps, blocker);
        }

        /// <summary>
        /// 이 이동 후보가 돌진인지 판정한다. AI가 후보 하나하나를 훑을 때 쓴다.
        ///
        /// 직선 위에 있어도 중간에 멈추는 이동은 돌진이 아니다.
        /// 돌진은 "멈출 이유가 생길 때까지 달리는" 행동이라 목적지를 고를 수 없기 때문이다.
        /// </summary>
        public static bool TryPlan(
            LDY_BoardManager board, Vector3Int origin, Vector3Int destination, int maxSteps,
            out LDY_ChargeLine line)
        {
            line = default;
            if (board == null) return false;

            if (!TryGetDirection(origin, destination, out Vector3Int direction)) return false;

            LDY_ChargeLine resolved = Resolve(board, origin, direction, maxSteps);
            if (!resolved.Moves || resolved.Destination != Flatten(destination)) return false;

            line = resolved;
            return true;
        }

        /// <summary>
        /// 이미 끝난 이동이 돌진이었는지 되짚는다. 이동 알림(LDY_IOnMoved)에서 쓴다.
        ///
        /// 도착 칸에는 이제 움직인 본인이 서 있으므로 경로를 다시 훑을 수 없다.
        /// 대신 "돌진이라면 반드시 멈출 이유가 있었다"를 검사한다 —
        /// 최대 거리를 다 썼거나, 앞에 기물이 있거나, 판 끝이거나.
        /// </summary>
        public static bool TryIdentify(
            LDY_BoardManager board, Vector3Int from, Vector3Int to, int maxSteps,
            out LDY_ChargeLine line)
        {
            line = default;
            if (board == null || maxSteps <= 0) return false;

            if (!TryGetDirection(from, to, out Vector3Int direction)) return false;

            Vector3Int start = Flatten(from);
            Vector3Int end = Flatten(to);

            int steps = Mathf.Abs(end.x - start.x) + Mathf.Abs(end.z - start.z);
            if (steps > maxSteps) return false;

            Vector3Int next = end + direction;
            bool insideNext = board.IsInside(next);
            LDY_Animal ahead = insideNext ? board.Get(next) : null;

            // 최대 거리를 다 쓴 돌진은 앞에 기물이 서 있어도 거기까지 닿지 못한 것이다.
            // 그건 충돌이 아니라 그냥 힘이 다한 것이라 밀어내기가 생기지 않는다.
            // (이동 전 계산인 Resolve도 같은 이유로 그 칸을 아예 들여다보지 않는다. 둘의 답이 같아야 한다.)
            LDY_Animal blocker = steps < maxSteps ? ahead : null;

            bool stoppedForReason = steps == maxSteps || ahead != null || !insideNext;
            if (!stoppedForReason) return false;

            line = new LDY_ChargeLine(direction, end, steps, blocker);
            return true;
        }

        /// <summary>
        /// 부딪힌 기물부터 같은 방향으로 맞닿아 늘어선 기물들을 모은다.
        /// 한도까지만 채우므로, 한도 밖의 기물은 애초에 목록에 들어오지 않는다.
        /// </summary>
        public static void CollectPushChain(
            LDY_BoardManager board, LDY_Animal first, Vector3Int direction, int maxPush,
            List<LDY_Animal> buffer)
        {
            if (buffer == null) return;

            buffer.Clear();
            if (board == null || first == null || maxPush <= 0) return;

            LDY_Animal current = first;

            while (current != null && buffer.Count < maxPush)
            {
                buffer.Add(current);

                Vector3Int next = Flatten(current.pos) + direction;
                current = board.IsInside(next) ? board.Get(next) : null;
            }
        }

        /// <summary>
        /// 줄의 맨 끝 기물이 한 칸 더 밀려날 수 있는지.
        ///
        /// 맞닿아 있는 줄이라 맨 끝이 못 가면 앞의 기물들도 갈 곳이 없다.
        /// 그래서 줄 전체의 이동 여부를 이 한 번의 검사로 정한다.
        /// 판 끝이든 한도 밖의 네 번째 기물이든, 막힌 것은 똑같이 막힌 것이다.
        /// </summary>
        public static bool CanAdvance(LDY_BoardManager board, LDY_Animal last, Vector3Int direction)
        {
            if (board == null || last == null) return false;

            return board.IsEmpty(Flatten(last.pos) + direction);
        }

        /// <summary>두 칸이 상하좌우 직선 위에 있으면 그 방향을 돌려준다.</summary>
        private static bool TryGetDirection(Vector3Int from, Vector3Int to, out Vector3Int direction)
        {
            direction = default;

            Vector3Int delta = Flatten(to) - Flatten(from);
            if (delta.x == 0 && delta.z == 0) return false;

            // 한 축만 움직였을 때가 직선이다. 둘 다 0이 아니면 대각선이라 돌진이 될 수 없다.
            if (delta.x != 0 && delta.z != 0) return false;

            direction = new Vector3Int(
                delta.x == 0 ? 0 : (delta.x > 0 ? 1 : -1),
                0,
                delta.z == 0 ? 0 : (delta.z > 0 ? 1 : -1));

            return true;
        }

        /// <summary>
        /// 격자 비교용으로 높이(y)를 떨어낸다.
        /// LDY_Animal.pos.y는 모델 표시용이라 칸을 가리지 않는데,
        /// 그대로 두면 이동 후보(항상 y=0)와 좌표 비교가 어긋난다.
        /// </summary>
        private static Vector3Int Flatten(Vector3Int p) => new Vector3Int(p.x, 0, p.z);
    }
}
