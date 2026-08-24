using System;
using _Scripts.LDY;
using _Scripts.LDY.AI;
using _Scripts.LSO.Ability;
using _Scripts.LSO.Boss;
using UnityEngine;

/// <summary>상어왕은 자리를 지키면서, 공격 가능한 기물 중 포식 대상과 처치 대상을 우선한다.</summary>
[Serializable]
public sealed class DLJ_SharkKingScorer : LDY_IActionScorer
{
    [Min(0)] [SerializeField] private int attackBonus = 50;
    [Min(0)] [SerializeField] private int predationTargetBonus = 40;
    [Min(0)] [SerializeField] private int killBonus = 80;
    [Min(0)] [SerializeField] private int movePenalty = 10000;

    public int Score(LDY_Animal self, in LDY_EnemyAction action, LDY_BoardManager board)
    {
        if (self == null || !LSO_AbilityNotify.Has<DLJ_SharkKingHuntingGround>(self.Abilities))
            return 0;

        switch (action.Kind)
        {
            case LDY_ActionKind.Attack:
                return ScoreAttack(self, action.Target);

            case LDY_ActionKind.Move:
                return -movePenalty;

            default:
                return 0;
        }
    }

    private int ScoreAttack(LDY_Animal self, LDY_Animal target)
    {
        if (target == null || target.health == null) return 0;

        bool isPredationTarget = IsPredationTarget(self, target);
        int estimatedDamage = self.GetAtk() + (isPredationTarget ? 2 : 0);
        int score = attackBonus;

        if (isPredationTarget)
            score += predationTargetBonus;
        if (target.health.Value <= estimatedDamage)
            score += killBonus;

        return score;
    }

    private static bool IsPredationTarget(LDY_Animal self, LDY_Animal target)
    {
        LSO_BossPhase phase = self.GetComponent<LSO_BossPhase>();
        if (phase == null || phase.CurrentPhase < 2) return false;

        DLJ_SharkKingPreyMark mark = target.GetComponent<DLJ_SharkKingPreyMark>();
        return mark != null && mark.IsMarkedBy(self.health);
    }
}
