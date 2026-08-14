using System.Reflection;
using _Scripts.LSO.Ability;
using _Scripts.LSO.Boss;
using UnityEngine;

/// <summary>공용 보스 페이즈 컴포넌트에 여우왕의 HP 경계만 설정한다.</summary>
public sealed class DLJ_FoxKingPhase : LSO_IAbility, LSO_IAbilityInitializable
{
    private const int PhaseTwoHealthThreshold = 12;
    private const string ThresholdFieldName = "healthThreshold";

    public void Initialize(LSO_AbilityContext context)
    {
        if (context?.Owner == null)
            return;

        LSO_BossPhase phase = context.Owner.GetComponent<LSO_BossPhase>();
        if (phase == null)
            phase = context.Owner.gameObject.AddComponent<LSO_BossPhase>();

        FieldInfo field = typeof(LSO_BossPhase).GetField(
            ThresholdFieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (field == null)
        {
            Debug.LogError("[여우왕] LSO_BossPhase 체력 경계를 설정할 수 없습니다.", context.Owner);
            return;
        }

        field.SetValue(phase, PhaseTwoHealthThreshold);
    }
}
