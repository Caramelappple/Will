using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace _Scripts.LDY
{
    /// <summary>
    /// 유언이 파괴를 미룬 사망 기물 기록.
    ///
    /// 계승 유언은 공격 연출이 끝난 뒤에야 발동하고, 발동 시점에 "누구의 유언인지"를 밖으로 알려주지 않는다.
    /// 주체 팀을 알아내려면 사망 시점에 미리 적어두는 수밖에 없다.
    /// 같은 프레임에 여러 기물이 죽는 것은 정상이므로(광역 유언 등) 하나가 아니라 목록으로 받는다.
    ///
    /// 기록하는 쪽(LDY_DeathHandler / LDY_AttackSystem)과 읽는 쪽(LDY_SuccessionResolver)이 서로를
    /// 참조하지 않도록 static으로 둔다. 현재 파괴를 미루는 유언은 계승뿐이다.
    /// </summary>
    public static class LDY_DeferredDeaths
    {
        private static readonly List<LDY_Animal> _pending = new();

        public static IReadOnlyList<LDY_Animal> Pending => _pending;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _pending.Clear();
        }

        public static void Record(LDY_Animal victim)
        {
            if (victim == null || _pending.Contains(victim)) return;

            _pending.Add(victim);
        }

        public static void Clear()
        {
            _pending.Clear();
        }

        /// <summary>
        /// 기록된 기물이 전부 같은 팀이면 그 팀을 돌려준다. 팀이 섞여 있거나 기록이 없으면 false.
        /// 틀린 팀에 계승을 넘기느니 판별 실패로 두는 편이 낫다.
        /// </summary>
        public static bool TryGetCommonTeam(out LDY_Team team)
        {
            team = default;
            bool found = false;

            foreach (LDY_Animal victim in _pending)
            {
                if (victim == null) continue;

                if (!found)
                {
                    team = victim.team;
                    found = true;
                    continue;
                }

                if (victim.team != team) return false;
            }

            return found;
        }

        /// <summary>실패 로그에 그대로 실을 판별 근거.</summary>
        public static string Describe()
        {
            if (_pending.Count == 0) return "유예된 사망 기물 없음";

            var builder = new StringBuilder();
            builder.Append("유예된 사망 기물 ").Append(_pending.Count).Append("개: ");

            for (int i = 0; i < _pending.Count; i++)
            {
                if (i > 0) builder.Append(", ");

                LDY_Animal victim = _pending[i];
                builder.Append(victim != null ? $"{victim.name}(team {victim.team})" : "이미 파괴됨");
            }

            return builder.ToString();
        }
    }
}
