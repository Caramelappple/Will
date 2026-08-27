using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LSO.Ability;
using UnityEngine;
using _Scripts.LSO.Interfaces;

namespace _Scripts.LSO.Boss.CrowKing
{
    public class LSO_PreyMarking : LSO_IAbility, LSO_IAbilityInitializable, IOnTurnStart
    {
        private LSO_AbilityContext _context;
        private LDY_Animal _owner;
        private LSO_PreyTracker _tracker;

        public void Initialize(LSO_AbilityContext context)
        {
            _context = context;
            _owner = context?.Owner;

            _tracker = _owner != null ? _owner.GetComponent<LSO_PreyTracker>() : null;

            if (_tracker == null)
                Debug.LogError($"{_owner?.name}: LSO_PreyTracker가 없어 사냥감 물색이 동작하지 않습니다.", _owner);
        }

        /// <summary>
        /// 사냥감이 없으면 새로 하나 지정한다.
        ///
        /// team을 보지 않는 것은 의도된 것이다. 플레이어 턴 시작에도 지정하므로,
        /// 사냥감이 죽은 직후 플레이어가 자기 턴에 곧바로 다음 표적을 보고 대비할 수 있다.
        ///
        /// 그래도 매 턴 표적이 흔들리지는 않는다. 아래 가드가 살아 있는 사냥감을 그대로 두기 때문이다.
        /// </summary>
        public void OnTurnStart(LDY_Team team)
        {
            if (_tracker == null || _owner == null) return;

            // 살아 있으면 계속 쫓는다. 죽으면 Prey가 null이 되어 아래로 내려온다.
            if (_tracker.Prey != null) return;

            LDY_BoardManager board = _context?.Board;
            if (board == null) return;

            LDY_Animal next = PickRandom(board, _owner.team.Opposite());

            //고를 것이 없으면 스킵
            if (next == null) return;

            _tracker.SetPrey(next);
        }
        
        /// <summary>
        /// 랜덤으로 타겟 지정
        /// </summary>
        /// <param name="board"></param>
        /// <param name="team"></param>
        /// <returns></returns>
        private static LDY_Animal PickRandom(LDY_BoardManager board, LDY_Team team)
        {
            List<LDY_Animal> candidates = board.GetAllByTeam(team);
            if (candidates.Count == 0) return null;

            return candidates[Random.Range(0, candidates.Count)];
        }
    }
}
