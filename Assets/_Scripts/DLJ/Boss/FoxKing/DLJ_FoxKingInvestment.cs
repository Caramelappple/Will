using System;
using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LSO;
using _Scripts.LSO.Ability;
using _Scripts.LSO.Boss;
using _Scripts.LSO.HealthSystem;
using _Scripts.LSO.HealthSystem.Data;
using UnityEngine;

/// <summary>턴별 투자, 코스트 약탈, 2페이즈 투자 수치만 담당한다.</summary>
public sealed class DLJ_FoxKingInvestment : LSO_IAbility, LSO_IAbilityInitializable,
    IOnTurnStart, LSO_IPhaseAware
{
    private const int PhaseOneCost = 3;
    private const int PhaseOneHeal = 3;
    private const int PhaseOneAttack = 2;
    private const int PhaseTwoCost = 2;
    private const int PhaseTwoHeal = 5;
    private const int PhaseTwoAttack = 3;
    private const int PlunderInterval = 3;
    private const int FrenzyGreed = 15;

    private LDY_Animal owner;
    private Health health;
    private DLJ_FoxKingBoss state;
    private LDY_ActionPointManager actionPoints;
    private LDY_BoardManager board;
    private int phase = 1;
    private int playerTurns;
    private int cachedPlayerCost;
    private int pendingCostLoss;
    private readonly Dictionary<Health, TargetSubscription> subscriptions = new();

    public void Initialize(LSO_AbilityContext context)
    {
        owner = context?.Owner;
        health = owner != null ? owner.health : null;
        state = owner != null ? owner.GetComponent<DLJ_FoxKingBoss>() : null;
        phase = owner != null ? owner.GetComponent<LSO_BossPhase>()?.CurrentPhase ?? 1 : 1;
        board = context?.Board;
        actionPoints = GameManager.HasInstance ? GameManager.Instance.TurnManager?.ActionPoints : null;

        if (actionPoints != null)
        {
            cachedPlayerCost = actionPoints.Current;
            actionPoints.OnActionPointsChanged += TrackPlayerCost;
        }

        if (board != null)
        {
            board.OnBoardChanged += RefreshTargets;
            RefreshTargets();
        }
    }

    public void OnPhaseChanged(LDY_Animal self, int nextPhase)
    {
        phase = nextPhase;
    }

    public void OnTurnStart(LDY_Team team)
    {
        if (owner == null || health == null || health.IsDestroyed || state == null)
            return;

        if (team == LDY_Team.Enemy)
        {
            HandlePlayerTurnEnded();
            Invest();
            return;
        }

        HandleEnemyTurnEnded();
    }

    private void TrackPlayerCost(int current, int max)
    {
        if (GameManager.HasInstance &&
            GameManager.Instance.TurnManager?.CurrentTurn == LDY_Team.Player)
            cachedPlayerCost = current;
    }

    private void HandlePlayerTurnEnded()
    {
        playerTurns++;

        if (playerTurns % PlunderInterval != 0)
            return;

        int amount = Math.Min(phase >= 2 ? 2 : 1, cachedPlayerCost);
        if (amount <= 0)
            return;

        pendingCostLoss += amount;
        state.Gain(amount, amount);
    }

    private void HandleEnemyTurnEnded()
    {
        // 2페이즈의 턴 종료 수입은 여우왕 자신의 턴이 끝날 때 한 번만 지급한다.
        if (phase >= 2)
            state.Gain(1, 0);

        if (pendingCostLoss > 0 && actionPoints != null)
        {
            int amount = Math.Min(pendingCostLoss, actionPoints.Current);
            if (amount > 0 && actionPoints.TryConsume(amount))
                pendingCostLoss -= amount;
        }
    }

    private void Invest()
    {
        int count = state.Greed >= FrenzyGreed ? 2 : 1;
        for (int i = 0; i < count && TryInvest(); i++) { }
    }

    private bool TryInvest()
    {
        int cost = phase >= 2 ? PhaseTwoCost : PhaseOneCost;
        int resourcesBeforeInvestment = state.StolenResources;
        if (!state.TrySpend(cost))
            return false;

        Debug.Log(
            $"[여우왕] 투자 비용 지불 → 수탈 자원 -{cost} " +
            $"({resourcesBeforeInvestment} → {state.StolenResources})");

        bool heal = health.Value < health.MaxValue && health.Value * 2 <= health.MaxValue;
        if (heal)
        {
            health.Recover(RecoverData.Create(health, phase >= 2 ? PhaseTwoHeal : PhaseOneHeal));
            return true;
        }

        state.PendingAttackBonus += phase >= 2 ? PhaseTwoAttack : PhaseOneAttack;
        return true;
    }

    private void RefreshTargets()
    {
        ClearTargets();
        if (board == null) return;

        foreach (LDY_Animal target in board.GetAllByTeam(LDY_Team.Player))
        {
            if (target == null || target.health == null) continue;
            var modifier = new InvestmentDamageModifier(this);
            target.health.AddDamageModifier(modifier);
            subscriptions[target.health] = new TargetSubscription(target.health, modifier);
        }
    }

    private void ClearTargets()
    {
        foreach (TargetSubscription subscription in subscriptions.Values)
            subscription.Dispose();
        subscriptions.Clear();
    }

    private bool IsOwnerAttack(DamageData data)
    {
        return data.giver == health &&
               (data.source == LSO_DamageSource.Melee ||
                data.source == LSO_DamageSource.Ranged ||
                data.source == LSO_DamageSource.Jump);
    }

    private sealed class InvestmentDamageModifier : LSO_IDamageModifier
    {
        private readonly DLJ_FoxKingInvestment investment;
        public int Priority => -2000;

        public InvestmentDamageModifier(DLJ_FoxKingInvestment investment)
        {
            this.investment = investment;
        }

        public int ModifyIncomingDamage(DamageableResources target, DamageData data, int damage)
        {
            if (!investment.IsOwnerAttack(data) || investment.state.PendingAttackBonus <= 0)
                return damage;

            int bonus = investment.state.PendingAttackBonus;
            investment.state.PendingAttackBonus = 0;
            return damage + bonus;
        }
    }

    private sealed class TargetSubscription
    {
        private readonly Health health;
        private readonly LSO_IDamageModifier modifier;

        public TargetSubscription(Health health, LSO_IDamageModifier modifier)
        {
            this.health = health;
            this.modifier = modifier;
        }

        public void Dispose()
        {
            if (health != null)
                health.RemoveDamageModifier(modifier);
        }
    }
}
