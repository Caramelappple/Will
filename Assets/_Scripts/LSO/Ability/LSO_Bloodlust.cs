using System;
using _Scripts.LDY;
using _Scripts.LSO.CoreLib;
using UnityEngine;
using _Scripts.LSO.Interfaces;

namespace _Scripts.LSO.Ability
{
    public sealed class LSO_Bloodlust : LSO_IAbility, LSO_IOnAnimalDead, IStatModifier, LSO_IAbilityInitializable
    {
        private const int DefaultMaxStack = 3;

        public int MaxStack { get; private set; } = DefaultMaxStack;
        public int Stack { get; private set; }

        /// <summary>누적치가 변했을 때 알린다. UI 표시용.</summary>
        public event Action<int> StackChanged;

        private LSO_AbilityContext _context;

        public LSO_Bloodlust() { }

        public LSO_Bloodlust(int maxStack)
        {
            MaxStack = Mathf.Max(0, maxStack);
        }

        public void Initialize(LSO_AbilityContext context)
        {
            _context = context;

            if (_context?.Owner == null)
                Debug.LogWarning("LSO_Bloodlust: 주인을 몰라 적군과 아군을 구분할 수 없습니다.");
        }

        public void OnAnimalDead(LDY_Animal animal)
        {
            if (Stack >= MaxStack) return;
            if (!IsEnemyOf(animal)) return;

            Stack++;
            StackChanged?.Invoke(Stack);
            LSO_AbilityLog.Log($"<color=orange>처치 누적: 공격력 +{Stack} (최대 +{MaxStack})</color>");
        }
        
        private bool IsEnemyOf(LDY_Animal dead)
        {
            LDY_Animal owner = _context?.Owner;

            if (owner == null || dead == null) return false;
            if (dead == owner) return false;

            return owner.team.IsEnemyOf(dead.team);
        }

        public int ModifyAttack(LDY_Animal self, int atk)
        {
            return atk + Stack;
        }
    }
}
