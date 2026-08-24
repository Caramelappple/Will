using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LSO.Ability;
using _Scripts.LSO.Boss;
using _Scripts.LSO.DeathSystem;
using _Scripts.LSO.HealthSystem;
using _Scripts.LSO.HealthSystem.Data;
using UnityEngine;

/// <summary>
/// 사냥터 개장. 상어왕의 적 턴을 기준으로 영역을 예약하고, 경고 후 범위 피해를 준다.
/// 첫 행동력을 사용해 영역을 예약하고, 남은 행동력으로 일반 근접 공격을 진행한다.
/// </summary>
public sealed class DLJ_SharkKingHuntingGround :
    LSO_IAbility,
    LSO_IAbilityInitializable,
    LSO_IPhaseAware,
    LSO_IOnDeath
{
    private const int ReuseTurnInterval = 2;

    private const int PhaseOneSize = 2;
    private const int PhaseOneDelay = 2;
    private const int PhaseOneDamage = 4;
    private const int PhaseOneZoneCount = 1;

    private const int PhaseTwoSize = 3;
    private const int PhaseTwoDelay = 1;
    private const int PhaseTwoDamage = 5;
    private const int PhaseTwoZoneCount = 2;

    private sealed class PendingVolley
    {
        public readonly List<Vector3Int> Origins;
        public readonly int Size;
        public readonly int Damage;
        public readonly int CreatedPhase;
        public readonly bool AppliesPredation;
        public int RemainingTurns;

        public PendingVolley(
            List<Vector3Int> origins,
            int size,
            int damage,
            int remainingTurns,
            int createdPhase,
            bool appliesPredation)
        {
            Origins = origins;
            Size = size;
            Damage = damage;
            RemainingTurns = remainingTurns;
            CreatedPhase = createdPhase;
            AppliesPredation = appliesPredation;
        }
    }

    private sealed class ZoneCandidate
    {
        public readonly Vector3Int Origin;
        public readonly HashSet<LDY_Animal> Targets;

        public ZoneCandidate(Vector3Int origin, HashSet<LDY_Animal> targets)
        {
            Origin = origin;
            Targets = targets;
        }
    }

    private readonly List<PendingVolley> _pending = new();

    private LSO_AbilityContext _context;
    private LDY_Animal _owner;
    private DLJ_SharkKing _sharkKing;
    private int _phase = 1;
    private int _enemyTurnCount;
    private int _lastUsedEnemyTurn;

    public void Initialize(LSO_AbilityContext context)
    {
        _context = context;
        _owner = context?.Owner;
        _sharkKing = _owner != null ? _owner.GetComponent<DLJ_SharkKing>() : null;

        if (_owner != null && _sharkKing == null)
        {
            Debug.LogError(
                $"{_owner.name}: DLJ_SharkKing 컴포넌트가 없어 사냥 영역 경고를 표시할 수 없습니다.",
                _owner);
        }
        else
        {
            _sharkKing.RegisterHuntingGround(this);
        }

        LSO_BossPhase phase = _owner != null ? _owner.GetComponent<LSO_BossPhase>() : null;
        _phase = phase != null ? phase.CurrentPhase : 1;
    }

    public void OnPhaseChanged(LDY_Animal self, int phase)
    {
        _phase = Mathf.Max(_phase, phase);
    }

    public void HandleEnemyTurnStart(LDY_ActionPointManager actionPoints)
    {
        _enemyTurnCount++;
        Debug.Log(
            $"[상어왕] 적 턴 {_enemyTurnCount} 시작 — AP {(actionPoints != null ? actionPoints.Current : -1)}",
            _owner);

        // 1페이즈 경고가 남은 채 2페이즈가 됐다면, 다음 상어왕 턴 시작에 즉시 폭발시킨다.
        // 그 직후의 2페이즈 사냥터 개장은 기존 2턴 재사용 대기를 무시하고 같은 턴에 예약한다.
        bool resolvedPhaseOneOnTransition = ResolvePhaseOneVolleysOnPhaseTwoTurn();
        AdvancePendingVolleys();

        // LDY_TurnManager는 AP 초기화 → 적 턴 이벤트 → EnemyAI 실행 순서다.
        // 여기서 먼저 소비하면 공용 AI를 수정하지 않고도 사냥터 개장이 첫 AP를 사용한다.
        TryOpenHuntingGround(actionPoints, resolvedPhaseOneOnTransition);
    }

    private bool TryOpenHuntingGround(
        LDY_ActionPointManager actionPoints,
        bool ignoreReuseCooldown = false)
    {
        if (actionPoints == null)
        {
            Debug.LogError("[상어왕] ActionPointManager가 없어 사냥터 개장을 사용할 수 없습니다.", _owner);
            return false;
        }

        if (!CanOperate(out string reason))
        {
            Debug.LogError($"[상어왕] 사냥터 개장 중단 — {reason}", _owner);
            return false;
        }

        if (!ignoreReuseCooldown &&
            _lastUsedEnemyTurn > 0 &&
            _enemyTurnCount - _lastUsedEnemyTurn < ReuseTurnInterval)
        {
            Debug.Log("[상어왕] 사냥터 개장 재사용 대기 중", _owner);
            return false;
        }

        LDY_BoardManager board = _context.Board;
        int size = _phase >= 2 ? PhaseTwoSize : PhaseOneSize;
        int delay = _phase >= 2 ? PhaseTwoDelay : PhaseOneDelay;
        int damage = _phase >= 2 ? PhaseTwoDamage : PhaseOneDamage;
        int zoneCount = _phase >= 2 ? PhaseTwoZoneCount : PhaseOneZoneCount;

        List<Vector3Int> origins = SelectBestOrigins(board, size, zoneCount);
        if (origins.Count == 0)
        {
            Debug.LogWarning("[상어왕] 보드에서 공격할 플레이어 기물을 찾지 못했습니다.", _owner);
            return false;
        }

        if (!actionPoints.TryConsume())
        {
            Debug.LogWarning("[상어왕] 액션 포인트가 부족해 사냥터 개장을 사용하지 못했습니다.", _owner);
            return false;
        }

        PendingVolley volley = new PendingVolley(
            origins,
            size,
            damage,
            delay,
            _phase,
            _phase >= 2 &&
            LSO_AbilityNotify.Has<DLJ_SharkKingPredation>(_owner.Abilities));

        _lastUsedEnemyTurn = _enemyTurnCount;
        _pending.Add(volley);
        ShowWarning(volley);
        Debug.Log(
            $"[상어왕] 사냥터 개장 사용 — {size}x{size}, {delay}턴 뒤 피해 {damage}, AP {actionPoints.Current}",
            _owner);
        return true;
    }

    private bool ResolvePhaseOneVolleysOnPhaseTwoTurn()
    {
        if (_phase < 2) return false;

        bool resolvedAny = false;
        for (int i = _pending.Count - 1; i >= 0; i--)
        {
            PendingVolley volley = _pending[i];
            if (volley.CreatedPhase >= 2) continue;

            Resolve(volley);
            ClearWarning(volley);
            _pending.RemoveAt(i);
            resolvedAny = true;
        }

        if (resolvedAny)
        {
            Debug.Log(
                "[상어왕] 2페이즈 전환 — 대기 중이던 1페이즈 사냥 영역을 즉시 공격합니다.",
                _owner);
        }

        return resolvedAny;
    }

    public void OnDeath(LDY_Animal self, LDY_Animal killer)
    {
        ClearAllWarnings();
        _pending.Clear();
    }

    private bool CanOperate(out string reason)
    {
        if (_owner == null)
        {
            reason = "소유자 없음";
            return false;
        }

        if (_owner.health == null)
        {
            reason = "Health 없음";
            return false;
        }

        if (_owner.health.IsDestroyed)
        {
            reason = "상어왕 사망 상태";
            return false;
        }

        if (_context?.Board == null)
        {
            reason = "GameManager에 BoardManager가 등록되지 않음";
            return false;
        }

        reason = null;
        return true;
    }

    private void AdvancePendingVolleys()
    {
        for (int i = _pending.Count - 1; i >= 0; i--)
        {
            PendingVolley volley = _pending[i];
            volley.RemainingTurns--;
            if (volley.RemainingTurns > 0) continue;

            Resolve(volley);
            ClearWarning(volley);
            _pending.RemoveAt(i);
        }
    }

    private List<Vector3Int> SelectBestOrigins(
        LDY_BoardManager board,
        int size,
        int zoneCount)
    {
        List<ZoneCandidate> candidates = CollectZoneCandidates(board, size);
        if (candidates.Count == 0) return new List<Vector3Int>();

        if (zoneCount <= 1 || candidates.Count == 1)
        {
            int bestCount = 0;
            List<Vector3Int> bestOrigins = new();
            for (int i = 0; i < candidates.Count; i++)
            {
                int hitCount = candidates[i].Targets.Count;
                if (hitCount < bestCount) continue;

                if (hitCount > bestCount)
                {
                    bestCount = hitCount;
                    bestOrigins.Clear();
                }

                bestOrigins.Add(candidates[i].Origin);
            }

            return new List<Vector3Int>
            {
                bestOrigins[Random.Range(0, bestOrigins.Count)]
            };
        }

        // 2페이즈의 두 영역은 모든 조합을 비교해 중복을 제외한 적 수가 가장 많은 쌍을 고른다.
        int bestUniqueHits = -1;
        int bestTotalHits = -1;
        List<(Vector3Int first, Vector3Int second)> bestPairs = new();

        for (int i = 0; i < candidates.Count - 1; i++)
        {
            for (int j = i + 1; j < candidates.Count; j++)
            {
                HashSet<LDY_Animal> uniqueTargets = new(candidates[i].Targets);
                uniqueTargets.UnionWith(candidates[j].Targets);

                int uniqueHits = uniqueTargets.Count;
                int totalHits = candidates[i].Targets.Count + candidates[j].Targets.Count;
                if (uniqueHits < bestUniqueHits ||
                    (uniqueHits == bestUniqueHits && totalHits < bestTotalHits))
                    continue;

                if (uniqueHits > bestUniqueHits || totalHits > bestTotalHits)
                {
                    bestUniqueHits = uniqueHits;
                    bestTotalHits = totalHits;
                    bestPairs.Clear();
                }

                bestPairs.Add((candidates[i].Origin, candidates[j].Origin));
            }
        }

        (Vector3Int first, Vector3Int second) selected =
            bestPairs[Random.Range(0, bestPairs.Count)];
        return new List<Vector3Int> { selected.first, selected.second };
    }

    private List<ZoneCandidate> CollectZoneCandidates(LDY_BoardManager board, int size)
    {
        List<LDY_Animal> players = board.GetAllByTeam(Opponent(_owner.team));
        List<ZoneCandidate> result = new();

        int maxOrigin = LDY_BoardManager.Size - size;
        for (int x = 0; x <= maxOrigin; x++)
        {
            for (int z = 0; z <= maxOrigin; z++)
            {
                HashSet<LDY_Animal> targets = new();
                for (int i = 0; i < players.Count; i++)
                {
                    LDY_Animal player = players[i];
                    if (player == null || player.health == null || player.health.IsDestroyed) continue;

                    if (Contains(x, z, size, player.pos))
                        targets.Add(player);
                }

                if (targets.Count > 0)
                    result.Add(new ZoneCandidate(new Vector3Int(x, 0, z), targets));
            }
        }

        return result;
    }

    private void Resolve(PendingVolley volley)
    {
        LDY_BoardManager board = _context.Board;
        if (board == null || _owner == null || _owner.health == null) return;

        // 두 경고 영역이 겹쳐도 같은 발동에서 한 기물은 한 번만 맞는다.
        HashSet<LDY_Animal> victims = new();
        foreach (Vector3Int tile in EnumerateTiles(volley))
        {
            LDY_Animal occupant = board.Get(tile);
            if (occupant != null && occupant != _owner)
                victims.Add(occupant);
        }

        foreach (LDY_Animal victim in victims)
        {
            if (victim == null || victim.health == null || victim.health.IsDestroyed) continue;

            int before = victim.health.Value;
            victim.health.GetDamage(DamageData.Create(
                _owner.health,
                volley.Damage,
                LSO_DamageSource.Ability));

            Debug.Log(
                $"[상어왕] 영역 피해 — {victim.name}: {before} → {victim.health.Value}",
                victim);

            if (victim.health.IsDestroyed)
            {
                LSO_IDeathService deaths = _context.Deaths;
                if (deaths != null)
                    deaths.Kill(victim, _owner);
                else
                    Debug.LogWarning("[상어왕] DeathService가 없어 영역 피해 사망을 처리하지 못했습니다.", _owner);

                continue;
            }

            if (volley.AppliesPredation &&
                victim.team != _owner.team &&
                victim.health.Value < before &&
                victim.health.Value * 2 <= victim.health.MaxValue)
            {
                DLJ_SharkKingPreyMark mark = victim.GetComponent<DLJ_SharkKingPreyMark>();
                if (mark == null)
                    mark = victim.gameObject.AddComponent<DLJ_SharkKingPreyMark>();

                mark.MarkBy(_owner.health);
            }
        }
    }

    private void ShowWarning(PendingVolley volley)
    {
        _sharkKing?.ShowAttackHighlights(
            volley,
            volley.Origins,
            volley.Size,
            _context?.Board);
    }

    private void ClearWarning(PendingVolley volley)
    {
        _sharkKing?.ClearAttackHighlights(volley);
    }

    private void ClearAllWarnings()
    {
        for (int i = 0; i < _pending.Count; i++)
            _sharkKing?.ClearAttackHighlights(_pending[i]);
    }

    private static IEnumerable<Vector3Int> EnumerateTiles(PendingVolley volley)
    {
        for (int i = 0; i < volley.Origins.Count; i++)
        {
            Vector3Int origin = volley.Origins[i];
            for (int x = 0; x < volley.Size; x++)
            {
                for (int z = 0; z < volley.Size; z++)
                    yield return new Vector3Int(origin.x + x, 0, origin.z + z);
            }
        }
    }

    private static bool Contains(int originX, int originZ, int size, Vector3Int position)
    {
        return position.x >= originX && position.x < originX + size &&
               position.z >= originZ && position.z < originZ + size;
    }

    private static LDY_Team Opponent(LDY_Team team)
    {
        return team == LDY_Team.Enemy ? LDY_Team.Player : LDY_Team.Enemy;
    }
}
