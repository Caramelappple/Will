using _Scripts.LDY;
using _Scripts.LSO.Ability;
using _Scripts.LSO.DeathSystem;
using UnityEngine;

namespace _Scripts.LSO.Boss.CrowKing
{
    public class LSO_Predation : LSO_IAbility, LSO_IOnKill, LSO_IAbilityInitializable
    {
        private LSO_CrowKingMemory _memory;
        
        public void Initialize(LSO_AbilityContext context)
        {
            LDY_Animal owner = context?.Owner;
            _memory = owner != null ? owner.GetComponent<LSO_CrowKingMemory>() : null;

            if (_memory == null)
                Debug.LogError($"{owner?.name}: LSO_CrowKingMemory가 없어 포식이 동작하지 않습니다.", owner);
        }

        public void OnKill(LDY_Animal self, LDY_Animal victim)
        {
            if (_memory == null) return;
            
            //먹은 목록에 추가
            _memory.TryAddDevour(victim.data);

            if (_memory.HasDevoured(victim.data))
            {
                foreach (LSO_AbilityType type in victim.AbilityTypes)
                    if (_memory.TryStoreAbility(type))
                        self.AddAbility(type);
            }
        }
    }
}