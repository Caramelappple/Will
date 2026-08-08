using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.LDY.AI
{
    /// <summary>사거리의 최소·최대 도달 거리(맨해튼). 판단용 휴리스틱 값이다.</summary>
    public readonly struct LDY_RangeSpan
    {
        public readonly int Min;
        public readonly int Max;
        public readonly bool IsValid;

        public LDY_RangeSpan(int min, int max)
        {
            Min = min;
            Max = max;
            IsValid = true;
        }

        /// <summary>주어진 거리가 사거리에서 얼마나 어긋났는지. 사거리 안이면 0.</summary>
        public int ErrorFrom(int distance)
        {
            if (distance < Min) return Min - distance;
            if (distance > Max) return distance - Max;
            return 0;
        }
    }

    /// <summary>
    /// 사거리 전략이 실제로 짚는 타일에서 도달 거리를 역산한다. 사거리 숫자를 여기에 옮겨 적지 않기 위해서다.
    ///
    /// 이 값은 "붙어라 / 떨어져라" 기울기를 만드는 내부 휴리스틱으로만 쓴다.
    /// 근접은 대각선이 맨해튼 2라서 원거리와 같은 [1,2]가 나오는데, 실제 근접 사거리는 체비쇼프 1이다.
    /// 공격 가능 여부의 진짜 판정은 항상 LDY_AttackSystem.HasTargetFrom이 해야 한다.
    /// </summary>
    public static class LDY_AttackRangeMetrics
    {
        // RangeType별로 고정된 값이라 한 번만 재고 재사용한다.
        private static readonly Dictionary<LDY_RangeType, LDY_RangeSpan> Cache = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Cache.Clear();
        }

        /// <summary>
        /// 보드 두 칸 사이의 거리. 이동이 대각선을 포함한 8방향이므로 체비쇼프(King 거리)를 쓴다.
        /// 맨해튼으로 재면 대각선 한 칸이 2로 계산되어 같은 1칸 이동인데 점수가 두 배가 된다.
        ///
        /// 사거리 실측과 상대까지의 거리는 반드시 같은 척도여야 한다.
        /// 둘이 어긋나면 error() 계산이 통째로 무의미해지므로 여기 하나만 쓴다.
        /// y(모델 표시용 높이)는 거리에 관여하지 않는다.
        /// </summary>
        public static int Distance(Vector3Int a, Vector3Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.z - b.z));
        }

        public static LDY_RangeSpan Get(LDY_RangeType type, LDY_BoardManager board)
        {
            if (Cache.TryGetValue(type, out LDY_RangeSpan cached))
                return cached;

            LDY_RangeSpan measured = Measure(type, board);
            Cache[type] = measured;
            return measured;
        }

        private static LDY_RangeSpan Measure(LDY_RangeType type, LDY_BoardManager board)
        {
            LDY_IAttackRange strategy = LDY_AttackRangeFactory.Create(type);
            if (strategy == null || board == null) return default;

            // 보드 한가운데에서 잰다. 가장자리에서 재면 IsInside 필터에 타일이 잘려 실제보다 좁게 나온다.
            var center = new Vector3Int(LDY_BoardManager.Size / 2, 0, LDY_BoardManager.Size / 2);
            List<Vector3Int> tiles = strategy.GetAttackableTiles(center, board);
            if (tiles.Count == 0) return default;

            int min = int.MaxValue;
            int max = 0;

            foreach (Vector3Int tile in tiles)
            {
                int distance = Distance(tile, center);
                if (distance < min) min = distance;
                if (distance > max) max = distance;
            }

            return new LDY_RangeSpan(min, max);
        }
    }
}
