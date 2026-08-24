using System;
using _Scripts.LDY;
using UnityEngine;

/// <summary>처치자 정보가 필요한 DLJ 보스 기믹용 전투 이벤트.</summary>
public static class DLJ_CombatKillEvents
{
    public static event Action<LDY_Animal, LDY_Animal> OnKilled;

    public static void Raise(LDY_Animal victim, LDY_Animal killer)
    {
        OnKilled?.Invoke(victim, killer);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        OnKilled = null;
    }
}
