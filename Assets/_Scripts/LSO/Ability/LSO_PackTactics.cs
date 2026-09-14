using _Scripts.LDY;
using UnityEngine;

namespace _Scripts.LSO.Ability
{
    /// <summary>
    /// 무리 사냥: 인접한 같은 종 아군 1기당 공격력이 1씩 오른다.
    /// "늑대 옆의 늑대"처럼 동물SO가 같은 기물을 센다. 늑대에게 붙이면 곧 "인접 늑대 수"가 된다.
    /// 매번 보드를 다시 세므로 기물이 이동해도 값이 알아서 따라간다.
    /// </summary>
    public sealed class LSO_PackTactics :
        LSO_IAbility, IStatModifier, LSO_IAbilityInitializable, LSO_IOnBoardChanged
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

        /// <summary>
        /// 지난번에 센 무리 수. 늘어난 순간을 알아내는 데만 쓴다.
        ///
        /// 0 으로 시작한다. 갓 놓인 기물은 "혼자였다가 무리가 생겼다"로 보는 것이 맞다 —
        /// 이미 늑대 옆에 놓았으면 그 순간 0 → 1 이므로 이펙트가 뜬다.
        /// </summary>
        private int _lastKinCount;

        public void Initialize(LSO_AbilityContext context)
        {
            _context = context;
        }

        public int ModifyAttack(LDY_Animal self, int atk)
        {
            return atk + CountAdjacentKin(self) * BonusPerAlly;
        }

        /// <summary>
        /// 판 위의 배치가 바뀌었다. 무리가 늘었으면 알린다.
        ///
        /// ── 왜 여기서 알리나 ──────────────────────────────────────
        /// ModifyAttack 은 공격력을 물어볼 때마다 불린다 — 초당 수십 번이다.
        /// 거기서 알리면 이펙트가 끊임없이 터진다.
        ///
        /// 배치가 바뀌는 순간은 드물고(놓기·이동·죽음) 무리 수가 달라질 수 있는 때도
        /// 그때뿐이다. 세는 것도 인접 8칸이라 값싸다.
        /// ─────────────────────────────────────────────────────────
        ///
        /// **늘어날 때만** 알린다. 줄어드는 것은 동료가 죽거나 떠난 것이라
        /// 축하할 일이 아니고, 그쪽까지 알리면 죽음 연출과 겹친다.
        /// </summary>
        public void OnBoardChanged()
        {
            LDY_Animal self = _context?.Owner;

            if (self == null) return;

            int count = CountAdjacentKin(self);

            // 처음 세는 경우도 막지 않는다.
            //
            // 예전에는 "아직 한 번도 안 셌다"를 -1 로 두고 그때는 건너뛰었는데,
            // 갓 놓인 기물은 언제나 그 첫 번째 세기에 걸린다. 그래서 늑대 옆에
            // 새 늑대를 놓으면 **원래 있던 늑대만** 이펙트가 뜨고 새 늑대는 조용했다.
            if (count > _lastKinCount)
            {
                LSO_AbilitySignal.Raise(
                    LSO_AbilityType.PackTactics, self,
                    $"<color=#8fd14f>{self.name}의 무리 사냥: 인접 {count}기 — 공격력 +{count * BonusPerAlly}</color>");
            }

            _lastKinCount = count;
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
