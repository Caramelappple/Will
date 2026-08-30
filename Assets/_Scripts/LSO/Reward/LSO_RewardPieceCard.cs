using System.Collections.Generic;
using System.Linq;
using _Scripts.LSO.Ability;
using _Scripts.LSO.Deck.Data;
using TMPro;
using UnityEngine;

namespace _Scripts.LSO.Reward
{
    /// <summary>
    /// 기물 보상 카드. 동물의 스탯을 그린다.
    ///
    /// 유언 보상이 들어오면 빈 카드로 둔다. 상자가 타입을 보고 프리팹을 고르므로
    /// 정상적인 흐름에서는 그럴 일이 없지만, 조용히 엉뚱한 것을 그리는 것보다는 낫다.
    /// </summary>
    public class LSO_RewardPieceCard : LSO_RewardCard
    {
        [Header("기물 수치")]
        [Tooltip("비워두면 그 칸은 건너뛴다.")]
        [SerializeField] private TMP_Text attackText;

        [SerializeField] private TMP_Text healthText;

        // 카드에는 이동 칸 수를 표시하지 않기로 했다.
        // 되살릴 때는 LSO_CardSO.MoveRange 도 같이 풀 것.
        //[SerializeField] private TMP_Text moveRangeText;

        [SerializeField] private TMP_Text attackRangeText;
        [SerializeField] private TMP_Text traitText;

        protected override void Draw(LSO_RewardOption option)
        {
            if (option.type != LSO_RewardType.Piece)
            {
                Debug.LogWarning($"{name}: 기물 카드인데 {option.type} 보상이 들어왔습니다.", this);
                Clear();
                return;
            }

            LSO_CardSO card = option.piece;

            if (card == null || !card.IsValid)
            {
                SetName("알 수 없는 기물");
                SetDescription(string.Empty);
                SetIcon(null);

                SetText(attackText, "ATK -");
                SetText(healthText, "HP -");
                //SetText(moveRangeText, "이동 -");
                SetText(attackRangeText, "사거리 -");
                SetText(traitText, "특성 -");
                return;
            }

            // animal을 직접 뚫지 않고 카드의 접근자를 쓴다.
            // "이 카드는 공격력 +1" 같은 카드 단위 보정이 생기면 LSO_CardSO만 고치면 되고,
            // 나중에 손패가 3D로 오면 그쪽도 같은 값을 읽게 된다.
            SetName(card.AnimalName);
            SetDescription(card.Description);
            SetIcon(card.Image);

            SetText(attackText, $"ATK {card.Damage}");
            SetText(healthText, $"HP {card.MaxHealth}");
            //SetText(moveRangeText, $"이동 {card.MoveRange}");
            SetText(attackRangeText, $"사거리 {card.Range}");
            SetText(traitText, BuildTraitText(card.AbilityTypes));
        }

        protected override void Clear()
        {
            ClearCommon();

            SetText(attackText, string.Empty);
            SetText(healthText, string.Empty);
            //SetText(moveRangeText, string.Empty);
            SetText(attackRangeText, string.Empty);
            SetText(traitText, string.Empty);
        }

        private static string BuildTraitText(IReadOnlyList<LSO_AbilityType> abilities)
        {
            if (abilities == null || abilities.Count == 0) return "특성 없음";

            return "특성 " + string.Join(", ", abilities.Select(a => a.ToString()));
        }
    }
}
