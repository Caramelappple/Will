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
        public static readonly IReadOnlyList<Vector3Int> Directions = new[]
        {
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1),
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
        };

        /// <summary>
        /// 아직 움직이지 않은 상태에서 이 방향으로 달리면 어떻게 되는지 계산한다.
        /// </summary>
        /// <param name="ignore">
        /// 없는 셈 치고 지나갈 기물. "저 자리로 옮기면 다음에 뭘 들이받을 수 있나"를
        /// 미리 재볼 때 자기 자신을 넘긴다. 그러지 않으면 아직 출발 칸에 서 있는 자신을
        /// 장애물로 집어서, 실제로는 뚫린 길을 막혔다고 판단한다.
        /// </param>
        public static LDY_ChargeLine Resolve(
            LDY_BoardManager board, Vector3Int origin, Vector3Int direction, int maxSteps,
            LDY_Animal ignore = null)
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
                if (occupant != null && occupant != ignore)
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
        /// 직선 위에 있고 최대 거리 안이면 몇 칸이든 돌진이다 — 중간에 멈춰도 된다.
        /// 예전에는 "막힐 때까지 끝까지"만 인정했는데, 그러면 황소왕이 자리를 고를 수가 없어서
        /// 상대를 지나쳐 벽까지 달렸다가 되돌아오기만 반복했다.
        /// </summary>
        public static bool TryPlan(
            LDY_BoardManager board, Vector3Int origin, Vector3Int destination, int maxSteps,
            out LDY_ChargeLine line)
        {
            line = default;
            if (board == null || maxSteps <= 0) return false;

            if (!TryGetDirection(origin, destination, out Vector3Int direction)) return false;

            Vector3Int start = Flatten(origin);
            Vector3Int end = Flatten(destination);

            int steps = Distance(start, end);
            if (steps > maxSteps) return false;

            // 이동 후보는 모퉁이를 돌아오는 길로도 만들어진다.
            // 돌진은 직선으로만 달리므로, 그 직선이 실제로 비어 있는지 여기서 확인한다.
            for (int i = 1; i <= steps; i++)
            {
                if (!board.IsEmpty(start + direction * i)) return false;
            }

            line = new LDY_ChargeLine(direction, end, steps, BlockerAhead(board, end, direction));
            return true;
        }

        /// <summary>
        /// 이미 끝난 이동이 돌진이었는지 되짚는다. 이동 알림(LDY_IOnMoved)에서 쓴다.
        ///
        /// 경로를 다시 훑지 않는 것은 도착 칸을 움직인 본인이 차지하고 있어서다.
        /// 어차피 실행된 이동이라 경로가 비어 있었던 건 이미 증명됐고,
        /// 여기서 알아야 할 것은 "바로 앞에 누가 있느냐"뿐이다.
        /// </summary>
        public static bool TryIdentify(
            LDY_BoardManager board, Vector3Int from, Vector3Int to, int maxSteps,
            out LDY_ChargeLine line)
        {
            line = default;
            if (board == null || maxSteps <= 0) return false;

            if (!TryGetDirection(from, to, out Vector3Int direction)) return false;

            Vector3Int end = Flatten(to);

            int steps = Distance(Flatten(from), end);
            if (steps > maxSteps) return false;

            line = new LDY_ChargeLine(direction, end, steps, BlockerAhead(board, end, direction));
            return true;
        }

        /// <summary>
        /// 멈춰 선 자리 바로 앞의 기물. 이게 있으면 들이받은 것이다.
        ///
        /// 최대 거리를 다 썼는지는 보지 않는다. 짧은 돌진이 가능해진 뒤로는
        /// "힘이 다해 멈춘 것"과 "스스로 멈춘 것"이 판 위에서 구분되지 않기 때문이다.
        /// 앞에 서 있으면 받힌다 — 규칙이 단순해야 플레이어가 경로를 읽을 수 있다.
        /// </summary>
        private static LDY_Animal BlockerAhead(
            LDY_BoardManager board, Vector3Int destination, Vector3Int direction)
        {
            Vector3Int next = destination + direction;
            return board.IsInside(next) ? board.Get(next) : null;
        }

        /// <summary>직선 위 두 칸 사이의 칸 수.</summary>
        private static int Distance(Vector3Int a, Vector3Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.z - b.z);
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
