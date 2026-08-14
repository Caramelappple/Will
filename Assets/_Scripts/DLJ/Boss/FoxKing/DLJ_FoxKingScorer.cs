using System;
using _Scripts.LDY;
using _Scripts.LDY.AI;
using _Scripts.LSO;
using UnityEngine;

/// <summary>
/// 여우왕이 계약·희생 유언을 가진 기물을 사냥하도록 공격과 이동 후보를 평가한다.
/// </summary>
[Serializable]
public sealed class DLJ_FoxKingScorer : LDY_IActionScorer
{
    [Header("Attack")]
    [Min(0)] [SerializeField] private int attackBonus = 20;
    [Min(0)] [SerializeField] private int killBonus = 50;
    [Min(0)] [SerializeField] private int preferredWillBonus = 40;
    [Min(0)] [SerializeField] private int preferredWillKillBonus = 70;

    [Header("Move")]
    [Min(0)] [SerializeField] private int preferredWillApproachWeight = 20;

    public int Score(LDY_Animal self, in LDY_EnemyAction action, LDY_BoardManager board)
    {
        if (self == null || self.GetComponent<DLJ_FoxKingBoss>() == null)
            return 0;

        switch (action.Kind)
        {
            case LDY_ActionKind.Attack:
                return ScoreAttack(self, action.Target);

            case LDY_ActionKind.Move:
                return ScoreMove(self, action.MoveTo, board);

            default:
                return 0;
        }
    }

    private int ScoreAttack(LDY_Animal self, LDY_Animal target)
    {
        if (target == null || target.health == null)
            return 0;

        bool preferredWill = IsPreferredWill(target.WillType);
        bool canKill = target.health.GetValue() <= EstimatedAttackDamage(self);

        int score = attackBonus;
        if (canKill)
            score += killBonus;
        if (preferredWill)
            score += preferredWillBonus;
        if (preferredWill && canKill)
            score += preferredWillKillBonus;

        return score;
    }

    private int ScoreMove(LDY_Animal self, Vector3Int destination, LDY_BoardManager board)
    {
        if (board == null)
            return 0;

        bool foundPreferredTarget = false;
        int bestScore = int.MinValue;

        foreach (LDY_Animal target in board.GetAllByTeam(Opponent(self.team)))
        {
            if (target == null || !IsPreferredWill(target.WillType))
                continue;

            foundPreferredTarget = true;
            int currentDistance = LDY_AttackRangeMetrics.Distance(self.pos, target.pos);
            int destinationDistance = LDY_AttackRangeMetrics.Distance(destination, target.pos);
            int score = (currentDistance - destinationDistance) * preferredWillApproachWeight;

            if (score > bestScore)
                bestScore = score;
        }

        return foundPreferredTarget ? bestScore : 0;
    }

    private static int EstimatedAttackDamage(LDY_Animal self)
    {
        DLJ_FoxKingBoss state = self.GetComponent<DLJ_FoxKingBoss>();
        int investmentBonus = state != null ? state.PendingAttackBonus : 0;
        return self.GetAtk() + investmentBonus;
    }

    private static bool IsPreferredWill(LSO_WillType willType)
    {
        return willType == LSO_WillType.Contract || willType == LSO_WillType.Sacrifice;
    }

    private static LDY_Team Opponent(LDY_Team team)
    {
        return team == LDY_Team.Enemy ? LDY_Team.Player : LDY_Team.Enemy;
    }
}
