using _Scripts.LSO.Will;
using TMPro;
using UnityEngine;

namespace _Scripts.LSO.Reward
{
    /// <summary>
    /// 유언 보상 카드. 유언의 수치를 그린다.
    ///
    /// 상자에서 고르는 세 장 중 하나로 쓰인다.
    /// 고른 뒤에 따로 올라오는 설명 종이는 LSO_WillNote 다.
    /// </summary>
    public class LSO_RewardWillCard : LSO_RewardCard
    {
        [Header("유언 수치")]
        [Tooltip("비워두면 그 칸은 건너뛴다.")]
        [SerializeField] private TMP_Text damageText;

        [SerializeField] private TMP_Text rangeText;
        [SerializeField] private TMP_Text durationText;
        [SerializeField] private TMP_Text buffText;
        [SerializeField] private TMP_Text debuffText;

        protected override void Draw(LSO_RewardOption option)
        {
            if (option.type != LSO_RewardType.Will)
            {
                Debug.LogWarning($"{name}: 유언 카드인데 {option.type} 보상이 들어왔습니다.", this);
                Clear();
                return;
            }

            DLJ_WillDataSO will = option.will;

            if (will == null)
            {
                SetName("알 수 없는 유언");
                SetDescription(string.Empty);
                SetIcon(null);

                SetText(damageText, "피해량 -");
                SetText(rangeText, "범위 -");
                SetText(durationText, "지속시간 -");
                SetText(buffText, string.Empty);
                SetText(debuffText, string.Empty);
                return;
            }

            SetName(will.WillType.ToString());
            SetDescription(will.description);
            SetIcon(will.icon);

            SetText(damageText, $"피해량 {will.DisplayDamage}");
            SetText(rangeText, $"범위 {will.DisplayRange}");
            SetText(durationText, $"지속시간 {will.DisplayDuration}");

            // 0이면 칸을 비운다. "버프 0"은 효과가 있는 것처럼 읽힌다.
            SetText(buffText,
                will.DisplayBuffAmount != 0 ? $"버프 {will.DisplayBuffAmount}" : string.Empty);

            SetText(debuffText,
                will.DisplayDebuffAmount != 0 ? $"디버프 {will.DisplayDebuffAmount}" : string.Empty);
        }

        protected override void Clear()
        {
            ClearCommon();

            SetText(damageText, string.Empty);
            SetText(rangeText, string.Empty);
            SetText(durationText, string.Empty);
            SetText(buffText, string.Empty);
            SetText(debuffText, string.Empty);
        }
    }
}
