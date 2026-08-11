using System.Collections.Generic;
using _Scripts.LDY;
using _Scripts.LSO.Ability;
using _Scripts.LSO.DeathSystem;
using UnityEngine;

namespace _Scripts.LSO.Boss.CrowKing
{
    public class LSO_Predation : LSO_IAbility, LSO_IOnKill, LSO_IAbilityInitializable, IStatModifier, LSO_IPhaseAware
    {
        private const float InheritDivisor = 3f;
        
        private const int Phase2BonusAtk = 1;
        private const int Phase2BonusHp = 2;
        
        private bool _empowered;

        private LSO_CrowKingMemory _memory;
        private LSO_PreyTracker _tracker;

        public void Initialize(LSO_AbilityContext context)
        {
            LDY_Animal owner = context?.Owner;
            _memory = owner != null ? owner.GetComponent<LSO_CrowKingMemory>() : null;
            _tracker = owner != null ? owner.GetComponent<LSO_PreyTracker>() : null;

            if (_memory == null)
                Debug.LogError($"{owner?.name}: LSO_CrowKingMemory가 없어 포식이 동작하지 않습니다.", owner);

            if (_tracker == null)
                Debug.LogError($"{owner?.name}: LSO_PreyTracker가 없어 포식이 동작하지 않습니다.", owner);
        }
        
        public int ModifyAttack(LDY_Animal self, int atk)
        {
            return _memory != null ? atk + _memory.InheritedAtk : atk;
        }

        public void OnKill(LDY_Animal self, LDY_Animal victim)
        {
            if (_memory == null || _tracker == null || self == null) return;
            
            if (victim == null || victim.data == null) return;
            
            if (_tracker.Prey != victim) return;

            // 처음 먹는 종류면 true. 이미 먹어봤으면 false = 되먹임 조건 성립.
            bool firstTime = _memory.TryAddDevour(victim.data);

            Devour(self, victim);

            if (!firstTime)
                StealAbilities(self, victim);
        }
        
        private void Devour(LDY_Animal self, LDY_Animal victim)
        {
            int atk = Inherit(victim.GetAtk()) + (_empowered ? Phase2BonusAtk : 0);

            int hp = victim.health != null
                ? Inherit(victim.health.MaxValue) + (_empowered ? Phase2BonusHp : 0)
                : 0;

            _memory.Inherit(atk, hp);
            GrowHealth(self, hp);
        }
        
        private static int Inherit(int value) => Mathf.CeilToInt(value / InheritDivisor);
        
        private static void GrowHealth(LDY_Animal self, int amount)
        {
            if (amount <= 0 || self.health == null) return;

            // 최대치를 먼저 올려야 아래 대입이 예전 상한에 잘리지 않는다.
            // heal:false로 둬야 잃었던 체력이 통째로 회복되지 않는다.
            self.health.Init(self.health.MaxValue + amount, false);
            self.health.Value += amount;
        }

        /// <summary>되먹임. 보관에 성공한 것만 실제로 붙인다.</summary>
        private void StealAbilities(LDY_Animal self, LDY_Animal victim)
        {
            IReadOnlyList<LSO_AbilityType> types = victim.AbilityTypes;
            if (types == null) return;

            foreach (LSO_AbilityType type in types)
            {
                if (!_memory.TryStoreAbility(type)) continue;

                self.AddAbility(type);
            }
        }
        
        public void OnPhaseChanged(LDY_Animal self, int phase)
        {
            if (phase < 2) return;

            _empowered = true;
        }
    }
}
