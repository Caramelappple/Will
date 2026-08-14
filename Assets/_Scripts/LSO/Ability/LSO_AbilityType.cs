namespace _Scripts.LSO.Ability
{
    public enum LSO_AbilityType
    {
        None,
        Immune,
        Double,
        Test,
        Sturdy,
        Dodge,
        Bloodlust,
        PackTactics,
        Thorns,
        Vengeance,
        Frail,
        CurseImmunity,
        Evolve,
        AllHeal,
        LifeSteal,
        CostRefund,
        WillEnhancement,

        // 값이 에셋에 int로 저장되므로 새 항목은 반드시 이 아래에만 붙일 것.
        // 중간에 끼우면 기존 에셋의 특성이 통째로 다른 것을 가리킨다.
        Predation,
        MemoryFrenzy,
        PreyMarking,
        FoxKingPlunder,
        FoxKingGreed,
        FoxKingInvestment,
        FoxKingPhase,
        DLJ_SharkKingPhase,
        DLJ_SharkKingImmobile,
        DLJ_SharkKingHuntingGround,
        DLJ_SharkKingPredation
    }
}
