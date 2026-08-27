using System;
using System.Collections.Generic;

namespace _Scripts.LSO.Reward
{
    /// <summary>
    /// 가중치 랜덤 뽑기. 기물이든 유언이든 같은 규칙이라 한 곳에서만 계산한다.
    ///
    /// MonoBehaviour가 아니라서 확률이 의심스러울 때 따로 돌려볼 수 있다.
    ///
    /// 책임 분리를 위해서 스태틱 클래스로 따로 나누었다.
    /// </summary>
    public static class LSO_RewardRoller
    {
        /// <summary>
        /// 후보 중 하나를 가중치대로 뽑는다. 뽑을 게 없으면 default.
        /// </summary>
        /// <param name="pool">후보 목록.</param>
        /// <param name="getWeight">각 후보의 가중치.</param>
        /// <param name="isExcluded">이미 해금됐다든지 해서 빼야 하는 후보.</param>
        private static T Pick<T>(
            IReadOnlyList<T> pool,
            Func<T, float> getWeight,
            Func<T, bool> isExcluded)
        {
            List<T> candidates = Collect(pool, getWeight, isExcluded, out float totalWeight);

            if (candidates == null || totalWeight <= 0f)
                return default;

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var t in candidates)
            {
                cumulative += getWeight(t);

                if (roll <= cumulative)
                    return t;
            }

            // 부동소수점 오차로 여기까지 오는 경우가 있다. 마지막 후보로 떨어뜨린다.
            return candidates[^1];
        }

        /// <summary>
        /// 여러 개를 겹치지 않게 뽑는다. 후보가 모자라면 뽑을 수 있는 만큼만 돌려준다.
        /// </summary>
        public static List<T> PickMany<T>(
            IReadOnlyList<T> pool,
            int count,
            Func<T, float> getWeight,
            Func<T, bool> isExcluded)
        {
            List<T> results = new();
            if (count <= 0) return results;

            // 이번 뽑기에서 이미 나온 것도 후보에서 빼야 같은 게 두 번 나오지 않는다.
            HashSet<T> taken = new();

            for (int i = 0; i < count; i++)
            {
                T picked = Pick(pool, getWeight, x => isExcluded(x) || taken.Contains(x));

                if (EqualityComparer<T>.Default.Equals(picked, default)) break;

                taken.Add(picked);
                results.Add(picked);
            }

            return results;
        }

        private static List<T> Collect<T>(
            IReadOnlyList<T> pool,
            Func<T, float> getWeight,
            Func<T, bool> isExcluded,
            out float totalWeight)
        {
            totalWeight = 0f;

            if (pool == null || pool.Count == 0) return null;

            List<T> candidates = null;

            foreach (var entry in pool)
            {
                if (entry == null) continue;
                if (isExcluded != null && isExcluded(entry)) continue;

                float weight = getWeight(entry);

                // 인스펙터 실수로 NaN이나 Infinity가 들어오면 누적합이 통째로 망가진다.
                if (!IsUsableWeight(weight)) continue;

                candidates ??= new List<T>();
                candidates.Add(entry);
                totalWeight += weight;
            }

            return candidates;
        }

        private static bool IsUsableWeight(float weight)
        {
            return weight > 0f && !float.IsNaN(weight) && !float.IsInfinity(weight);
        }
    }
}
