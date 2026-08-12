using System;

namespace _Scripts.LSO.Ability
{
    public enum LSO_AbilityType
    {
        None,

        // 아래 셋은 폐기됐다. 구현체가 없어 고르면 아무 특성도 붙지 않고 경고만 뜬다.
        // 지우면 뒤의 값들이 한 칸씩 밀려 기존 에셋의 특성이 다른 것을 가리키므로 자리만 남겨둔다.
        [Obsolete("구현체가 없다. 면역은 CurseImmunity를 쓸 것.")]
        Immune,

        [Obsolete("구현체가 없다. 2연타는 MemoryFrenzy를 쓸 것.")]
        Double,

        [Obsolete("LSO_Test는 삭제됐다. 공격력 조회에 부작용이 있어 조회할 때마다 자해했다.")]
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
        FoxKingPhase
    }
}
