using _Scripts.LSO.Deck.Data;
using TMPro;
using UnityEngine;

namespace _Scripts.LSO.Reward
{
    /// <summary>
    /// 기물 보상 카드. 이름·설명·공격력·체력·코스트를 그린다.
    ///
    /// 사거리와 특성은 카드에 넣지 않는다. 고를 때 필요한 것만 남긴 것이고,
    /// 자세한 것은 획득 뒤 인포 창에서 본다.
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
        [SerializeField] private TMP_Text costText;

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
                SetText(costText, "코스트 -");
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
            SetText(costText, $"코스트 {card.Cost}");
        }

        protected override void Clear()
        {
            ClearCommon();

            SetText(attackText, string.Empty);
            SetText(healthText, string.Empty);
            SetText(costText, string.Empty);
        }
    }
}
