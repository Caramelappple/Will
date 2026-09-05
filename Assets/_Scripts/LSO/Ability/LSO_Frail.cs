using _Scripts.LDY;
using _Scripts.LSO.CoreLib;
using _Scripts.LSO.HealthSystem;
using _Scripts.LSO.HealthSystem.Data;
using UnityEngine;
using _Scripts.LSO.Interfaces;

namespace _Scripts.LSO.Ability
{
    /// <summary>
    /// 허약: 턴이 시작될 때마다 일정 확률로 죽는다. 어느 팀 턴이든 판정한다.
    /// 한 라운드(아군 턴 + 적 턴)에 두 번 굴리므로, 확률 p의 실효 사망률은 1-(1-p)^2 이다.
    /// 기본값 0.6 기준으로 라운드당 약 84%.
    /// 직접 파괴하지 않고 사망 창구를 거치므로 유언과 사망 이벤트가 정상적으로 발동한다.
    /// </summary>
    public sealed class LSO_Frail : LSO_IAbility, IOnTurnStart, LSO_IAbilityInitializable, LSO_IDamageModifier
    {
        private const float DefaultDeathChance = 0.33f;

        public float DeathChance { get; private set; } = DefaultDeathChance;

        private LSO_AbilityContext _context;

        public LSO_Frail() { }

        public LSO_Frail(float deathChance)
        {
            DeathChance = Mathf.Clamp01(deathChance);
        }

        public void Initialize(LSO_AbilityContext context)
        {
            _context = context;
        }

        public void OnTurnStart(LDY_Team team)
        {
            LDY_Animal owner = _context?.Owner;
            if (owner == null) return;

            // 팀을 가리지 않고 모든 턴 시작에 판정한다.
            if (owner.health != null && owner.health.IsDestroyed) return;
            if (Random.value >= DeathChance) return;

            LSO_AbilityLog.Log($"<color=grey>{owner.name}: 허약 발동 — 쓰러졌습니다.</color>", owner);

            LSO_AbilityDeath.KillThrough(_context, owner);
        }

        /// <summary>
        /// 보통 자리에 두는 것은 의도다.
        /// 회피·저주 면역(Nullify)이 먼저 돌고, 여기서 0이 된 값을 옹골참(LastStand)이 받는다.
        /// 무효화 계열보다 앞에 두면 그 특성들이 헛되이 발동한 것으로 기록된다.
        /// </summary>
        public int Priority => LSO_DamagePriority.Normal;

        /// <summary>피해를 전부 무시한다. 허약은 맞아서 죽지 않고 턴 판정으로만 쓰러진다.</summary>
        public int ModifyIncomingDamage(DamageableResources target, DamageData data, int damage)
        {
            return 0;
        }
    }
}
