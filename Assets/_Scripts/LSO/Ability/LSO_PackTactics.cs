using _Scripts.LDY;
using UnityEngine;

namespace _Scripts.LSO.Ability
{
    /// <summary>
    /// 무리 사냥: 인접한 같은 종 아군 1기당 공격력이 1씩 오른다.
    /// "늑대 옆의 늑대"처럼 동물SO가 같은 기물을 센다. 늑대에게 붙이면 곧 "인접 늑대 수"가 된다.
    /// 매번 보드를 다시 세므로 기물이 이동해도 값이 알아서 따라간다.
    /// </summary>
    public class LSO_PackTactics : LSO_IAbility, IStatModifier, LSO_IAbilityInitializable
    {
        // 대각선 포함 8방향. 사거리 판정(LDY_MeleeRange)과 같은 기준이다.
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

        private const int DefaultBonusPerAlly = 1;

        public int BonusPerAlly { get; private set; } = DefaultBonusPerAlly;

        private LSO_AbilityContext _context;

        public LSO_PackTactics() { }

        public LSO_PackTactics(int bonusPerAlly)
        {
            BonusPerAlly = Mathf.Max(0, bonusPerAlly);
        }

        public void Initialize(LSO_AbilityContext context)
        {
            _context = context;
        }

        public int ModifyAttack(LDY_Animal self, int atk)
        {
            return atk + CountAdjacentKin(self) * BonusPerAlly;
        }

        /// <summary>인접 8칸에서 같은 팀·같은 동물SO인 기물 수를 센다. 자기 자신은 제외된다.</summary>
        private int CountAdjacentKin(LDY_Animal self)
        {
            LDY_BoardManager board = _context?.Board;
            if (board == null || self == null || self.data == null) return 0;

            int count = 0;

            foreach (Vector3Int direction in Directions)
            {
                Vector3Int tile = new Vector3Int(
                    self.pos.x + direction.x,
                    0,
                    self.pos.z + direction.z);

                if (!board.IsInside(tile)) continue;

                LDY_Animal neighbor = board.Get(tile);
                if (neighbor == null || neighbor == self) continue;
                if (neighbor.team != self.team) continue;
                if (neighbor.data != self.data) continue;

                count++;
            }

            return count;
        }
    }
}
