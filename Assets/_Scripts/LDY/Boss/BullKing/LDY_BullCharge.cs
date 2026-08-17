using System.Collections.Generic;
using _Scripts.LSO.Ability;
using UnityEngine;

namespace _Scripts.LDY.Boss.BullKing
{
    /// <summary>
    /// 돌진. 황소왕이 직선으로 달려 기물을 들이받고 밀어낸다.
    ///
    /// 이동 자체는 기존 이동 시스템이 그대로 처리한다. 이 특성은 이동이 끝난 뒤 알림을 받아
    /// "방금 그건 돌진이었나"를 판정하고, 맞으면 충돌만 얹는다.
    ///
    /// 이동을 직접 수행하지 않는 것은 의도된 것이다.
    /// 직접 옮기면 행동력 소모·점유 검사·이동 연출을 전부 여기에 다시 적어야 하고,
    /// 그 사본은 원본이 바뀔 때마다 조용히 어긋난다.
    ///
    /// "직선으로만, 끝까지 달린다"는 제약은 AI 쪽(LDY_BullChargeScorer)이 지킨다.
    /// 이동 후보를 만드는 건 LDY_MoveSystem이라 특성이 후보를 고를 수는 없고,
    /// 돌진이 아닌 후보에 큰 감점을 줘서 고르지 않게 하는 방식이다.
    /// </summary>
    public sealed class LDY_BullCharge : LSO_IAbility, LSO_IAbilityInitializable,
        LDY_IOnMoved, LDY_IMoveVisualModifier, LDY_IMoveDirections
    {
        private readonly LDY_BullCollision _collision = new();

        private LSO_AbilityContext _context;
        private LDY_Animal _owner;
        private LDY_BullKingBoss _boss;

        public void Initialize(LSO_AbilityContext context)
        {
            _context = context;
            _owner = context?.Owner;
            _boss = _owner != null ? _owner.GetComponent<LDY_BullKingBoss>() : null;

            if (_boss == null)
                Debug.LogError($"{_owner?.name}: LDY_BullKingBoss가 없어 돌진이 동작하지 않습니다.", _owner);
        }

        /// <summary>
        /// 황소왕은 룩처럼 상하좌우로만 달린다. 대각선 칸은 이동 후보에 아예 오르지 않는다.
        ///
        /// AI 감점으로 억제하지 않고 여기서 막는 이유는, 플레이어가 이 기물을 조종할 때는
        /// 점수를 거치지 않기 때문이다. 그쪽은 이동 가능 타일 표시가 곧 규칙이다.
        /// </summary>
        public IReadOnlyList<Vector3Int> MoveDirections => LDY_ChargePath.Directions;

        /// <summary>
        /// 황소왕의 이동은 전부 돌진이므로 거리를 가리지 않고 돌진 속도를 쓴다.
        /// 일반 기물보다 훨씬 빠르고, 멀리 갈수록 오래 달린다.
        /// </summary>
        public float ModifyMoveDuration(LDY_Animal self, int distance, float duration)
        {
            return _boss != null ? _boss.ChargeDuration(distance) : duration;
        }

        public AnimationCurve MoveEasing => _boss != null ? _boss.ChargeEasing : null;

        public void OnMoved(LDY_Animal self, Vector3Int from, Vector3Int to)
        {
            // 알림은 소유자의 특성 목록에서만 오지만, 남의 이동에 반응하지 않는다는 걸 못박아 둔다.
            if (self != _owner || _owner == null || _boss == null) return;

            LDY_BoardManager board = _context?.Board;
            if (board == null) return;

            LDY_BullChargeRule rule = _boss.Rule;

            // 돌진이 아닌 평범한 이동이면 아무 일도 없다.
            // AI가 감점을 무릅쓰고 골랐거나, 플레이어가 조종하는 경우가 여기로 온다.
            if (!LDY_ChargePath.TryIdentify(board, from, to, rule.chargeRange, out LDY_ChargeLine line))
                return;

            // 앞이 비어 있거나 판 끝이면 그냥 달린 것이다. 충돌해야 밀어내기가 생긴다.
            if (!line.Collides) return;

            _collision.Resolve(_owner, board, line, rule, _context.Deaths, _boss);
        }
    }
}
