using _Scripts.LDY;
using _Scripts.LSO;
using _Scripts.LSO.Ability;
using _Scripts.LSO.Boss;
using _Scripts.LSO.DeathSystem;
using UnityEngine;

public interface DLJ_IWillBenefitSource
{
    event System.Action<LDY_Animal, LSO_WillType> Benefit;
}

public sealed class DLJ_WillBenefitSource : DLJ_IWillBenefitSource
{
    public event System.Action<LDY_Animal, LSO_WillType> Benefit
    {
        add => DLJ_WillBenefitEvents.OnBenefit += value;
        remove => DLJ_WillBenefitEvents.OnBenefit -= value;
    }
}

/// <summary>
/// 계약·희생 이득과 여우왕의 직접 처치로 수탈 자원과 탐욕을 획득한다.
/// 직접 처치한 대상이 유언을 선택했다면 수탈 자원만 1만큼 추가로 획득한다.
/// </summary>
public sealed class DLJ_FoxKingPlunder : LSO_IAbility, LSO_IAbilityInitializable,
    LSO_IOnKill, LSO_IPhaseAware
{
    private readonly DLJ_IWillBenefitSource benefitSource;
    private LDY_Animal owner;
    private DLJ_FoxKingBoss state;
    private int phase = 1;

    public DLJ_FoxKingPlunder()
        : this(new DLJ_WillBenefitSource())
    {
    }

    public DLJ_FoxKingPlunder(DLJ_IWillBenefitSource benefitSource)
    {
        this.benefitSource = benefitSource;
    }

    public void Initialize(LSO_AbilityContext context)
    {
        owner = context?.Owner;
        state = owner != null ? owner.GetComponent<DLJ_FoxKingBoss>() : null;
        phase = owner != null ? owner.GetComponent<LSO_BossPhase>()?.CurrentPhase ?? 1 : 1;
        benefitSource.Benefit += HandleWillBenefit;
    }

    public void OnPhaseChanged(LDY_Animal self, int nextPhase)
    {
        phase = nextPhase;
    }

    public void OnKill(LDY_Animal self, LDY_Animal victim)
    {
        if (self != owner || victim == null || state == null)
            return;

        int stolenAmount = victim.IsWillChosen ? 2 : 1;
        state.Gain(stolenAmount, 1);

        Debug.Log(
            $"[여우왕] 직접 처치 → 수탈 자원 +{stolenAmount}, 탐욕 +1 " +
            $"(현재 수탈 자원 {state.StolenResources}, 탐욕 {state.Greed})");
    }

    private void HandleWillBenefit(LDY_Animal beneficiary, LSO_WillType willType)
    {
        if (state == null || beneficiary == null || beneficiary.team != LDY_Team.Player)
            return;
        if (willType != LSO_WillType.Contract && willType != LSO_WillType.Sacrifice)
            return;

        int amount = phase >= 2 ? 2 : 1;
        state.Gain(amount, amount);

        Debug.Log(
            $"[여우왕] {willType} 이득 수탈 → 수탈 자원 +{amount}, 탐욕 +{amount} " +
            $"(현재 수탈 자원 {state.StolenResources}, 탐욕 {state.Greed})");
    }
}
