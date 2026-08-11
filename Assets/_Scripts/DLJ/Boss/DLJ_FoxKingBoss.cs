using System;
using _Scripts.LDY;
using _Scripts.LSO;
using _Scripts.LSO.HealthSystem;
using UnityEngine;

/// <summary>
/// 여우왕의 수탈 기믹만 담당한다.
/// 플레이어가 계약으로 코스트를 환급받거나 희생으로 아군을 구제하면
/// 수탈 자원을 1 획득해 전투가 끝날 때까지 보관한다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(LDY_Animal), typeof(Health))]
public sealed class DLJ_FoxKingBoss : MonoBehaviour
{
    private const int FirstAttackGreedThreshold = 5;
    private const int SecondAttackGreedThreshold = 10;
    private const int MaxHealthGreedThreshold = 15;
    private const int AttackIncrease = 1;
    private const int MaxHealthIncrease = 5;

    private LDY_Animal foxKing;
    private Health health;
    private bool firstAttackIncreaseApplied;
    private bool secondAttackIncreaseApplied;
    private bool maxHealthIncreaseApplied;

    public int StolenResources { get; private set; }
    public int Greed { get; private set; }

    //UI와 연출이 현재 수탈 자원을 갱신할 때 사용한다
    public event Action<int> OnStolenResourcesChanged;
    public event Action<int> OnGreedChanged;

    private void OnEnable()
    {
        foxKing = GetComponent<LDY_Animal>();
        health = GetComponent<Health>();
        DLJ_WillBenefitEvents.OnBenefit += HandleWillBenefit;
        DLJ_CombatKillEvents.OnKilled += HandleAnimalKilled;
    }

    private void OnDisable()
    {
        DLJ_WillBenefitEvents.OnBenefit -= HandleWillBenefit;
        DLJ_CombatKillEvents.OnKilled -= HandleAnimalKilled;
    }

    private void HandleWillBenefit(LDY_Animal owner, LSO_WillType willType)
    {
        if (owner == null || owner.team != LDY_Team.Player)
            return;

        if (willType != LSO_WillType.Contract && willType != LSO_WillType.Sacrifice)
            return;

        GainResources(1, 1);

        Debug.Log($"[여우왕] {willType} 이득 수탈 → 수탈 자원 {StolenResources}", this);
    }

    private void HandleAnimalKilled(LDY_Animal victim, LDY_Animal killer)
    {
        if (victim == null || killer != foxKing)
            return;

        int stolenAmount = victim.IsWillChosen ? 2 : 1;
        GainResources(stolenAmount, 1);

        Debug.Log(
            $"[여우왕] 직접 처치 → 수탈 자원 +{stolenAmount}, 탐욕 +1 " +
            $"(현재 {StolenResources} / {Greed})",
            this);
    }

    private void GainResources(int stolenAmount, int greedAmount)
    {
        if (stolenAmount > 0)
        {
            StolenResources += stolenAmount;
            OnStolenResourcesChanged?.Invoke(StolenResources);
        }

        if (greedAmount > 0)
        {
            Greed += greedAmount;
            ApplyGreedMilestones();
            OnGreedChanged?.Invoke(Greed);
        }
    }

    private void ApplyGreedMilestones()
    {
        if (!firstAttackIncreaseApplied && Greed >= FirstAttackGreedThreshold)
        {
            firstAttackIncreaseApplied = true;
            foxKing.baseAtk += AttackIncrease;
            Debug.Log($"[여우왕] 탐욕 {FirstAttackGreedThreshold} → ATK +{AttackIncrease}", this);
        }

        if (!secondAttackIncreaseApplied && Greed >= SecondAttackGreedThreshold)
        {
            secondAttackIncreaseApplied = true;
            foxKing.baseAtk += AttackIncrease;
            Debug.Log($"[여우왕] 탐욕 {SecondAttackGreedThreshold} → ATK +{AttackIncrease}", this);
        }

        if (!maxHealthIncreaseApplied && Greed >= MaxHealthGreedThreshold)
        {
            maxHealthIncreaseApplied = true;
            health.Init(health.MaxValue + MaxHealthIncrease, false);
            health.Value += MaxHealthIncrease;
            Debug.Log(
                $"[여우왕] 탐욕 {MaxHealthGreedThreshold} → " +
                $"최대 HP +{MaxHealthIncrease}, 현재 HP +{MaxHealthIncrease}",
                this);
        }
    }
}
