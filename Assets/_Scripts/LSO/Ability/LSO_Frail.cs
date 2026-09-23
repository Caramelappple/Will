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
    ///
    /// **맞아서는 죽지 않는다.** 들어오는 피해를 전부 0으로 만들고,
    /// 오직 턴 판정으로만 쓰러진다. 개복치처럼 "때려도 안 죽는데 저절로 죽는"
    /// 기물을 위한 것이다. 때리면 죽는 기물을 만들려는 것이라면 이 특성이 아니다.
    ///
    /// 한 라운드(아군 턴 + 적 턴)에 두 번 굴리므로, 확률 p의 실효 사망률은 1-(1-p)^2 이다.
    /// 기본값 0.33 기준으로 라운드당 약 55%.
    ///
    /// 직접 파괴하지 않고 사망 창구를 거치므로 유언과 사망 이벤트가 정상적으로 발동한다.
    /// </summary>
    public sealed class LSO_Frail : LSO_IAbility, IOnTurnStart, LSO_IAbilityInitializable, LSO_IDamageModifier,
        LSO_ISuccessionHealth
    {
        private const float DefaultDeathChance = 0.33f;

        /// <summary>
        /// 계승에는 체력 1로 친다.
        ///
        /// 허약을 단 기물의 큰 체력은 "단단하다"가 아니라 "맞아서는 안 죽는다"를
        /// 적어둔 숫자다. 실제로 막는 것은 아래 ModifyIncomingDamage 이지 체력이 아니다.
        ///
        /// 그 숫자를 그대로 물려주면 받은 기물이 진짜로 그만큼 단단해진다 —
        /// 개복치(체력 999)가 죽으면 계승 대상이 +333 을 받았다.
        /// 표현용 숫자가 실제 능력으로 바뀌는 자리라 여기서 끊는다.
        ///
        /// 화면에 보이는 체력은 그대로다. 계승 계산에서만 이 값을 쓴다.
        /// </summary>
        private const int SuccessionHealthValue = 1;

        public float DeathChance { get; private set; } = DefaultDeathChance;

        /// <inheritdoc/>
        public int SuccessionHealth => SuccessionHealthValue;

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
            // 덱 선택/미리보기용 모델에는 턴 판정을 적용하지 않는다.
            if (_context.Board == null || _context.Board.Get(owner.pos) != owner) return;

            // 팀을 가리지 않고 모든 턴 시작에 판정한다.
            if (owner.health != null && owner.health.IsDestroyed) return;
            if (Random.value >= DeathChance)
            {
                LSO_AbilitySignal.Raise(
                    LSO_AbilityType.Frail,
                    owner,
                    effectVariant: 1);
                return;
            }

            LSO_AbilitySignal.Raise(LSO_AbilityType.Frail, owner,
                $"<color=grey>{owner.name}: 허약 발동 — 쓰러졌습니다.</color>",
                effectVariant: 0);

            LSO_AbilityDeath.KillThrough(_context, owner);
        }

        /// <summary>
        /// 보통 자리에 두는 것은 의도다.
        /// 회피·저주 면역(Nullify)이 먼저 돌고, 여기서 0이 된 값을 옹골참(LastStand)이 받는다.
        /// 무효화 계열보다 앞에 두면 그 특성들이 헛되이 발동한 것으로 기록된다.
        /// </summary>
        public int Priority => LSO_DamagePriority.Normal;

        /// <summary>
        /// 피해를 전부 무시한다. 허약은 맞아서 죽지 않고 턴 판정으로만 쓰러진다.
        ///
        /// 다만 **막을 수 없는 피해는 그대로 통과시킨다.** 상어왕 영역처럼 특성을
        /// 뚫도록 정해진 공격이 그것이다. 무엇이 뚫는지는 LSO_DamageRules 가 정한다 —
        /// 여기서 출처를 직접 나열하면 같은 목록이 방어 특성마다 한 벌씩 생긴다.
        /// </summary>
        public int ModifyIncomingDamage(DamageableResources target, DamageData data, int damage)
        {
            if (LSO_DamageRules.IgnoresDefenses(data.source)) return damage;

            return 0;
        }
    }
}
