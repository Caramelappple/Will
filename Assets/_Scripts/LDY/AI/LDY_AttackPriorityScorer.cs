using System;
using UnityEngine;

namespace _Scripts.LDY.AI
{
    [Serializable]
    public class LDY_AttackPriorityScorer : LDY_IActionScorer
    {
        /// <summary>
        /// 공격 후보의 기본 가산점.
        /// LDY_PositioningScorer의 사거리 진입 보너스 상한이 이 값에 묶여 있다
        /// ([Range(0, DefaultBonus - 1)]). "공격할 수 있으면 공격한다"를 지키려면
        /// 이동 점수가 이 값을 넘어서는 안 되므로, 이 값을 바꾸면 그쪽 상한도 함께 검토할 것.
        /// </summary>
        public const int DefaultBonus = 100;

        [Min(0)]
        [SerializeField] private int bonus = DefaultBonus;

        public int Score(LDY_Animal self, in LDY_EnemyAction action, LDY_BoardManager board)
        {
            return action.Kind == LDY_ActionKind.Attack ? bonus : 0;
        }
    }
}
