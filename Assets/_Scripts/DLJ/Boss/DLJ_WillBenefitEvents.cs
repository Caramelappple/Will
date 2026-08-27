using System;
using _Scripts.LDY;
using UnityEngine;
using _Scripts.LSO.Will;

/// <summary>DLJ 유언이 만든 실질적인 이득을 보스 기믹에 전달하는 전투 이벤트.</summary>
public static class DLJ_WillBenefitEvents
{
    public static event Action<LDY_Animal, LSO_WillType> OnBenefit;

    public static void Raise(LDY_Animal owner, LSO_WillType willType)
    {
        OnBenefit?.Invoke(owner, willType);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        OnBenefit = null;
    }
}
