using System.Collections.Generic;
using _Scripts.LSO.Ability;
using _Scripts.LSO.DeathSystem;
using _Scripts.LSO.HealthSystem.Data;
using UnityEngine;
using _Scripts.LSO.Reward;

namespace _Scripts.LDY.Boss.BullKing
{
    /// <summary>
    /// 분노의 연쇄. 2페이즈에 열린다.
    ///
    /// 돌진으로 죽은 기물이 자기 유언과 무관하게 그 자리에서 한 번 더 터진다.
    /// 원래 유언은 사망 처리에서 이미 발동했으므로, 여기서 터지는 건 그 위에 덧붙는 것이다.
    ///
    /// 폭발로 또 기물이 죽으면 그 기물의 유언은 사망 창구가 알아서 발동한다.
    /// 그중 분노 유언이 있으면 자연히 연쇄가 이어진다 — 여기서 따로 부르지 않는다.
    /// 아래의 최대 횟수는 "돌진이 만들어낸 폭발"만 센다.
    ///
    /// 페이즈 검사를 LSO_IPhaseAware가 아니라 LDY_BullKingBoss.Phase 조회로 하는 이유는
    /// 그쪽 주석에 적어두었다 — 같은 사실을 두 벌로 들고 있지 않기 위해서다.
    /// </summary>
    public sealed class LDY_BullRageChain : LSO_IAbility, LSO_IAbilityInitializable
    {
        private LSO_AbilityContext _context;
        private LDY_Animal _owner;
        private LDY_BullKingBoss _boss;

        public void Initialize(LSO_AbilityContext context)
        {
            _context = context;
            _owner = context?.Owner;
            _boss = _owner != null ? _owner.GetComponent<LDY_BullKingBoss>() : null;

            if (_boss == null)
            {
                Debug.LogError($"{_owner?.name}: LDY_BullKingBoss가 없어 분노의 연쇄가 동작하지 않습니다.", _owner);
                return;
            }

            _boss.ChargeResolved += HandleChargeResolved;
        }

        private void HandleChargeResolved(IReadOnlyList<Vector3Int> deathTiles)
        {
            if (_boss == null || deathTiles == null || deathTiles.Count == 0) return;

            // 1페이즈의 사망은 각자의 유언만 발동한다. 여기서 덧붙는 폭발은 광란부터다.
            if (_boss.Phase < 2) return;

            int limit = Mathf.Min(deathTiles.Count, _boss.MaxRageChainPerCharge);

            for (int i = 0; i < limit; i++)
                Burst(deathTiles[i]);
        }

        /// <summary>죽은 자리를 중심으로 터뜨린다. 아군과 적군을 가리지 않는다.</summary>
        private void Burst(Vector3Int center)
        {
            LDY_BoardManager board = _context?.Board;
            if (board == null) return;

            int range = _boss.RageChainRange;
            int damage = _boss.RageChainDamage;
            LSO_IDeathService deaths = _context.Deaths;

            Debug.Log($"[황소왕] 분노의 연쇄 — {center}에서 {damage} 피해", _owner);

            for (int x = -range; x <= range; x++)
            {
                for (int z = -range; z <= range; z++)
                {
                    Vector3Int tile = center + new Vector3Int(x, 0, z);
                    if (!board.IsInside(tile)) continue;

                    LDY_Animal target = board.Get(tile);
                    if (target == null || target.health == null || target.health.IsDestroyed) continue;

                    // 황소왕은 방금 들이받은 기물 바로 옆에 서 있어서 거의 항상 범위 안이다.
                    // 자기 피해를 감수할지는 기획 선택이라 인스펙터로 열어 두었다.
                    if (target == _owner && !_boss.RageChainHitsBullKing) continue;

                    // 유언 분노와 같은 출처로 보낸다. "분노 피해 무효" 같은 특성이 생기면 함께 걸린다.
                    target.health.GetDamage(DamageData.Create(null, damage, LSO_DamageSource.Rage));

                    if (!target.health.IsDestroyed) continue;

                    // 죽인 주체는 없다. 유언 분노(DLJ_RageWill)도 같은 방식으로 처리한다.
                    deaths?.Kill(target, null);
                }
            }
        }
    }
}
