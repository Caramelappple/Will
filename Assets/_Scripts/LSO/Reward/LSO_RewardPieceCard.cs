using _Scripts.LSO.Deck.Data;
using _Scripts.LSO.UI.Text;
using TMPro;
using UnityEngine;

namespace _Scripts.LSO.Reward
{
    /// <summary>
    /// 기물 보상 카드. 이름·공격력·체력·코스트·점수·사거리·특성을 그린다.
    ///
    /// 손패 카드(KTH_InitCardData)와 같은 것을 보여준다. 상자에서 고를 때 본 것과
    /// 손에 들고 볼 때 본 것이 다르면 같은 기물인데 판단 근거가 갈린다.
    ///
    /// 설명은 넣지 않는다. 쓰는 것은 유언 메모장뿐이다.
    ///
    /// 칸을 하나 더 그리게 되면 Clear 에도 같이 적을 것. 카드는 풀에서
    /// 돌려쓰므로 안 비운 칸에는 지난 카드의 값이 그대로 남는다.
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
        [SerializeField] private TMP_Text pointText;
        [SerializeField] private TMP_Text rangeText;

        [Tooltip("이 기물이 가진 특성 이름들. 쉼표로 이어 적는다.")]
        [SerializeField] private TMP_Text abilityText;

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
                SetIcon(null);

                SetText(attackText, "-");
                SetText(healthText, "-");
                SetText(costText, "-");
                SetText(pointText, "-");
                SetText(rangeText, "-");
                SetText(abilityText, "-");
                return;
            }

            // animal을 직접 뚫지 않고 카드의 접근자를 쓴다.
            // "이 카드는 공격력 +1" 같은 카드 단위 보정이 생기면 LSO_CardSO만 고치면 되고,
            // 나중에 손패가 3D로 오면 그쪽도 같은 값을 읽게 된다.
            SetName(card.AnimalName);
            SetIcon(card.Image);

            SetText(attackText, $"{card.Damage}");
            SetText(healthText, $"{card.MaxHealth}");
            SetText(costText, $"{card.Cost}");
            SetText(pointText, $"{card.Point}");

            // 한글로 바꾸는 곳은 LSO_DisplayNames 하나다.
            // 예전에는 여기서 enum 을 그대로 문자열로 만들어서 "Melee" 가 찍혔다 —
            // 사거리 문구를 아무리 고쳐도 이 카드만 영문으로 남았다.
            SetText(rangeText, LSO_DisplayNames.Of(card.Range));

            // 이어 붙이는 것도 창구가 한다. 손패 카드와 같은 함수를 쓰므로
            // 쉼표 간격이나 None 처리가 두 화면에서 갈리지 않는다.
            SetText(abilityText, LSO_DisplayNames.Of(card.AbilityTypes));
        }

        /// <summary>
        /// 그리는 칸은 **전부** 비운다.
        ///
        /// 예전에는 점수와 사거리를 빠뜨렸다. 카드는 풀에서 돌려쓰므로 안 비운 칸에
        /// 지난 카드의 값이 그대로 남는다 — 이름과 공격력은 새 기물인데 사거리만
        /// 앞 기물 것인 카드가 나온다.
        /// </summary>
        protected override void Clear()
        {
            ClearCommon();

            SetText(attackText, string.Empty);
            SetText(healthText, string.Empty);
            SetText(costText, string.Empty);
            SetText(pointText, string.Empty);
            SetText(rangeText, string.Empty);
            SetText(abilityText, string.Empty);
        }
    }
}
