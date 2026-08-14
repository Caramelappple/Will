using _Scripts.LDY;
using UnityEngine;

namespace _Scripts.LSO.Ability
{
    /// <summary>
    /// 특성이 기물을 죽일 때 거치는 창구.
    ///
    /// 직접 Destroy하면 보드에서 안 지워지고 유언도 안 돈다.
    /// 사망 서비스가 없을 때 조용히 넘어가지 않도록 여기서 한 번 알린다.
    /// </summary>
    internal static class LSO_AbilityDeath
    {
        public static void KillThrough(LSO_AbilityContext context, LDY_Animal victim)
        {
            if (victim == null) return;

            if (context?.Deaths != null)
            {
                context.Deaths.Kill(victim, null);
                return;
            }

            Debug.LogWarning(
                $"{victim.name}: 씬에 LDY_DeathHandler가 없어 사망 처리를 건너뜁니다.", victim);
        }
    }
}
