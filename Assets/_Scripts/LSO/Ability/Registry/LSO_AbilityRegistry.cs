using _Scripts.LDY.Boss.BullKing;
using _Scripts.LSO.Boss.CrowKing;
using UnityEngine;

namespace _Scripts.LSO.Ability.Registry
{
    /// <summary>
    /// 어떤 특성 종류가 어떤 구현으로 만들어지는지 적어두는 유일한 곳.
    ///
    /// 이 파일만 구체 특성들을 안다. LSO_AbilityFactory는 모른다.
    /// 그래서 특성을 쓰는 코드(LDY_Animal 등)가 구현을 끌고 들어오지 않는다.
    ///
    /// 새 특성을 만들면 여기에 한 줄 추가하면 된다. 고칠 곳은 이 파일 하나다.
    ///
    /// 아무도 이 클래스를 참조하지 않는다. 그게 핵심이다.
    /// 진입점은 아래 RegisterAll 하나뿐이고, 유니티가 씬 로드 전에 직접 부른다.
    /// </summary>
    public static class LSO_AbilityRegistry
    {
        /// <summary>
        /// SubsystemRegistration은 씬이 로드되기 전에 돈다.
        /// 기물의 Awake에서 LSO_AbilityFactory.Create를 부르므로 그보다 먼저 채워져 있어야 한다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterAll()
        {
            // Reload Domain을 끈 에디터에서는 지난 플레이의 표가 그대로 남아 있다.
            // 삭제된 특성이 계속 살아 있지 않도록 매번 비우고 다시 채운다.
            LSO_AbilityFactory.Clear();

            // ---- 일반 특성 ----
            LSO_AbilityFactory.Register(LSO_AbilityType.Sturdy, () => new LSO_Sturdy());
            LSO_AbilityFactory.Register(LSO_AbilityType.Dodge, () => new LSO_Dodge());
            LSO_AbilityFactory.Register(LSO_AbilityType.Bloodlust, () => new LSO_Bloodlust());
            LSO_AbilityFactory.Register(LSO_AbilityType.PackTactics, () => new LSO_PackTactics());
            LSO_AbilityFactory.Register(LSO_AbilityType.Thorns, () => new LSO_Thorns());
            LSO_AbilityFactory.Register(LSO_AbilityType.Vengeance, () => new LSO_Vengeance());
            LSO_AbilityFactory.Register(LSO_AbilityType.Frail, () => new LSO_Frail());
            LSO_AbilityFactory.Register(LSO_AbilityType.CurseImmunity, () => new LSO_CurseImmunity());

            // ---- 유언/되먹임 계열 ----
            LSO_AbilityFactory.Register(LSO_AbilityType.Evolve, () => new DLJ_Evolve());
            LSO_AbilityFactory.Register(LSO_AbilityType.AllHeal, () => new DLJ_AllHeal());
            LSO_AbilityFactory.Register(LSO_AbilityType.LifeSteal, () => new DLJ_LifeSteal());
            LSO_AbilityFactory.Register(LSO_AbilityType.CostRefund, () => new DLJ_CostRefund());
            LSO_AbilityFactory.Register(LSO_AbilityType.WillEnhancement, () => new DLJ_WillEnhancement());

            // ---- 까마귀왕 ----
            LSO_AbilityFactory.Register(LSO_AbilityType.Predation, () => new LSO_Predation());
            LSO_AbilityFactory.Register(LSO_AbilityType.MemoryFrenzy, () => new LSO_MemoryFrenzy());
            LSO_AbilityFactory.Register(LSO_AbilityType.PreyMarking, () => new LSO_PreyMarking());

            // ---- 여우왕 ----
            LSO_AbilityFactory.Register(LSO_AbilityType.FoxKingPlunder, () => new DLJ_FoxKingPlunder());
            LSO_AbilityFactory.Register(LSO_AbilityType.FoxKingGreed, () => new DLJ_FoxKingGreed());
            LSO_AbilityFactory.Register(LSO_AbilityType.FoxKingInvestment, () => new DLJ_FoxKingInvestment());
            LSO_AbilityFactory.Register(LSO_AbilityType.FoxKingPhase, () => new DLJ_FoxKingPhase());

            // ---- 상어왕 ----
            LSO_AbilityFactory.Register(LSO_AbilityType.DLJ_SharkKingHuntingGround, () => new DLJ_SharkKingHuntingGround());
            LSO_AbilityFactory.Register(LSO_AbilityType.DLJ_SharkKingImmobile, () => new DLJ_SharkKingImmobile());
            LSO_AbilityFactory.Register(LSO_AbilityType.DLJ_SharkKingPhase, () => new DLJ_SharkKingPhase());
            LSO_AbilityFactory.Register(LSO_AbilityType.DLJ_SharkKingPredation, () => new DLJ_SharkKingPredation());

            // ---- 황소왕 ----
            LSO_AbilityFactory.Register(LSO_AbilityType.BullCharge, () => new LDY_BullCharge());
            LSO_AbilityFactory.Register(LSO_AbilityType.BullRageChain, () => new LDY_BullRageChain());
        }
    }
}
