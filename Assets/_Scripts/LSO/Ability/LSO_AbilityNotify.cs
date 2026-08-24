using System;
using System.Collections.Generic;

namespace _Scripts.LSO.Ability
{
    /// <summary>
    /// 특성 목록에서 특정 훅만 골라 호출하는 유일한 지점.
    ///
    /// 예전에는 호출부마다 `_abilities.OfType&lt;T&gt;().ToArray()`를 손으로 적었다.
    /// 그러다 보니 어떤 곳은 스냅샷을 뜨고 어떤 곳은 안 떠서,
    /// 특성이 자기 자신을 죽이거나 다른 특성을 붙이면 순회 도중 목록이 바뀌어 터졌다.
    ///
    /// Notify는 스냅샷을 뜨고 호출한다. 알림을 받은 특성이 자기를 죽이거나
    /// 다른 특성을 붙이는 일이 실제로 있기 때문이다(되먹임이 그렇다).
    ///
    /// Accumulate는 스냅샷을 뜨지 않고 원본을 인덱스로 돈다.
    /// GetAtk처럼 초당 수십 번 불리는 질의라 매번 리스트를 만들면 할당이 감당되지 않고,
    /// 값을 묻는 도중에 특성 목록을 바꾸는 구현은 애초에 두면 안 되기 때문이다.
    /// </summary>
    public static class LSO_AbilityNotify
    {
        /// <summary>T를 구현한 특성에게 순서대로 알린다.</summary>
        public static void Notify<T>(IReadOnlyList<LSO_IAbility> abilities, Action<T> action)
            where T : class
        {
            if (abilities == null || action == null) return;

            List<T> targets = Collect<T>(abilities);
            if (targets == null) return;

            for (int i = 0; i < targets.Count; i++)
                action(targets[i]);
        }

        /// <summary>
        /// T를 구현한 특성을 거치며 값을 누적한다. 공격력 보정처럼 순서대로 값을 굴릴 때 쓴다.
        /// </summary>
        public static int Accumulate<T>(
            IReadOnlyList<LSO_IAbility> abilities, int seed, Func<T, int, int> step)
            where T : class
        {
            if (abilities == null || step == null) return seed;

            int value = seed;

            for (int i = 0; i < abilities.Count; i++)
            {
                if (abilities[i] is T target)
                    value = step(target, value);
            }

            return value;
        }

        /// <summary>T를 구현한 특성이 하나라도 있는지.</summary>
        public static bool Has<T>(IReadOnlyList<LSO_IAbility> abilities) where T : class
        {
            if (abilities == null) return false;

            for (int i = 0; i < abilities.Count; i++)
            {
                if (abilities[i] is T) return true;
            }

            return false;
        }

        /// <summary>
        /// 호출 도중 목록이 바뀌어도 안전하도록 미리 담아둔다.
        /// 해당 훅이 없으면 리스트를 만들지 않는다. 대부분의 호출이 여기서 끝난다.
        /// </summary>
        private static List<T> Collect<T>(IReadOnlyList<LSO_IAbility> abilities) where T : class
        {
            List<T> targets = null;

            for (int i = 0; i < abilities.Count; i++)
            {
                if (abilities[i] is not T target) continue;

                targets ??= new List<T>();
                targets.Add(target);
            }

            return targets;
        }
    }
}
