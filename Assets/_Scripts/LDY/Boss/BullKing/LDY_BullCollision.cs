using System.Collections.Generic;
using _Scripts.LSO.DeathSystem;
using _Scripts.LSO.HealthSystem;
using _Scripts.LSO.HealthSystem.Data;
using UnityEngine;

namespace _Scripts.LDY.Boss.BullKing
{
    /// <summary>
    /// 돌진이 기물을 들이받았을 때의 처리. 밀어내기 → 피해 → 사망 순서로 진행한다.
    ///
    /// 밀어내기를 피해보다 먼저 하는 이유는, 피해를 먼저 주면 줄 한가운데의 기물이 죽어
    /// 격자에서 빠지고 그 자리에 구멍이 생기기 때문이다. 그러면 뒤에 있던 기물이
    /// "밀려난" 것인지 "빈칸으로 걸어 들어간" 것인지 구분할 수 없다.
    ///
    /// MonoBehaviour가 아니다. 밀려나는 연출만 코루틴이 필요한데 그건 LDY_BullKingBoss가 맡는다.
    /// </summary>
    internal sealed class LDY_BullCollision
    {
        // 돌진 한 번을 처리하는 동안만 쓰는 작업 버퍼. 매번 새 리스트를 만들지 않으려고 들고 있는다.
        private readonly List<LDY_Animal> _chain = new();
        private readonly List<Vector3Int> _deathTiles = new();

        public void Resolve(
            LDY_Animal bull,
            LDY_BoardManager board,
            in LDY_ChargeLine line,
            LDY_BullChargeRule rule,
            LSO_IDeathService deaths,
            LDY_BullKingBoss boss)
        {
            if (bull == null || board == null || rule == null || boss == null) return;
            if (!line.Collides) return;

            LDY_ChargePath.CollectPushChain(board, line.Blocker, line.Direction, rule.maxChainPush, _chain);
            if (_chain.Count == 0) return;

            // 줄이 맞닿아 있으므로 맨 끝이 못 가면 아무도 못 간다. 한 번만 물어보면 된다.
            bool advanced = LDY_ChargePath.CanAdvance(board, _chain[_chain.Count - 1], line.Direction);

            if (advanced)
                PushChain(board, boss, line.Direction);

            _deathTiles.Clear();
            ApplyDamage(bull, rule, deaths, advanced);

            boss.LastPushedCount = advanced ? _chain.Count : 0;
            boss.LastKilledCount = _deathTiles.Count;

            Debug.Log(
                $"[황소왕] 돌진 {line.Steps}칸 → {_chain.Count}기물 충돌" +
                $"{(advanced ? " · 밀어냄" : " · 벽에 막힘")}" +
                $"{(_deathTiles.Count > 0 ? $" · {_deathTiles.Count}기물 사망" : string.Empty)}", bull);

            // 사망 처리까지 전부 끝난 뒤에 알린다. 분노의 연쇄가 여기에 붙는다.
            boss.RaiseChargeResolved(_deathTiles);

            // 파괴된 기물을 계속 붙들고 있지 않도록 비운다.
            _chain.Clear();
        }

        /// <summary>
        /// 뒤쪽(황소왕에서 먼 쪽)부터 옮긴다. 앞에서부터 옮기면 아직 비지 않은 칸으로 밀어 넣게 되고,
        /// LDY_BoardManager.Move가 점유 검사에 걸려 조용히 거부한다.
        /// </summary>
        private void PushChain(LDY_BoardManager board, LDY_BullKingBoss boss, Vector3Int direction)
        {
            for (int i = _chain.Count - 1; i >= 0; i--)
            {
                LDY_Animal pushed = _chain[i];
                if (pushed == null) continue;

                Vector3Int from = pushed.pos;
                Vector3Int to = new Vector3Int(from.x + direction.x, 0, from.z + direction.z);

                board.Move(pushed, from, to);
                boss.PlayPush(pushed, board.GridToWorld(pushed.pos));
            }
        }

        /// <summary>
        /// 밀려난(또는 밀리지 못한) 기물 전원에게 충돌 피해를 준다.
        /// 줄이 막혀 있었다면 맨 끝 기물만 벽에 부딪힌 것이므로 그 기물에게만 추가 피해가 붙는다.
        /// </summary>
        private void ApplyDamage(
            LDY_Animal bull, LDY_BullChargeRule rule, LSO_IDeathService deaths, bool advanced)
        {
            Health giver = bull.health;

            for (int i = 0; i < _chain.Count; i++)
            {
                LDY_Animal victim = _chain[i];
                if (victim == null || victim.health == null || victim.health.IsDestroyed) continue;

                int damage = rule.collisionDamage;
                if (!advanced && i == _chain.Count - 1)
                    damage += rule.wallDamage;

                // 죽으면 격자에서 빠져 좌표를 되찾을 수 없으므로 때리기 전에 적어둔다.
                Vector3Int tile = new Vector3Int(victim.pos.x, 0, victim.pos.z);

                victim.health.GetDamage(DamageData.Create(giver, damage, LSO_DamageSource.Ability));

                if (!victim.health.IsDestroyed) continue;

                _deathTiles.Add(tile);

                if (deaths != null)
                {
                    // 처치자를 황소왕으로 넘겨야 유언과 처치 특성이 정상적으로 발동한다.
                    deaths.Kill(victim, bull);
                    continue;
                }

                Debug.LogError(
                    $"{bull.name}: 사망 처리 창구(LSO_IDeathService)를 찾을 수 없어 " +
                    $"{victim.name}이(가) 체력 0인 채로 보드에 남습니다.", bull);
            }
        }
    }
}
