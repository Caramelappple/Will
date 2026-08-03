using System;
using _Scripts.LDY;
using UnityEngine;

namespace _Scripts.LSO.Ability
{
    /// <summary>
    /// 적 기물이 죽을 때마다 공격력이 1씩 오른다. 상한까지만 누적된다.
    /// 개체마다 누적치가 따로 쌓이므로 반드시 개체별 인스턴스로 사용해야 한다.
    /// </summary>
    public class LSO_Bloodlust : LSO_IAbility, IOnEnemyDead, IStatModifier
    {
        private const int DefaultMaxStack = 3;

        public int MaxStack { get; private set; } = DefaultMaxStack;
        public int Stack { get; private set; }

        /// <summary>누적치가 변했을 때 알린다. UI 표시용.</summary>
        public event Action<int> StackChanged;

        public LSO_Bloodlust() { }

        public LSO_Bloodlust(int maxStack)
        {
            MaxStack = Mathf.Max(0, maxStack);
        }

        public void OnEnemyDead(LDY_Animal animal)
        {
            if (Stack >= MaxStack) return;

            Stack++;
            StackChanged?.Invoke(Stack);
            Debug.Log($"<color=orange>처치 누적: 공격력 +{Stack} (최대 +{MaxStack})</color>");
        }

        public int ModifyAttack(LDY_Animal self, int atk)
        {
            return atk + Stack;
        }
    }
}
