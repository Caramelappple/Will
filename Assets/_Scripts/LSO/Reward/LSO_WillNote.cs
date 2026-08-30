using _Scripts.LSO.CoreLib;
using _Scripts.LSO.Will;
using TMPro;
using UnityEngine;

namespace _Scripts.LSO.Reward
{
    /// <summary>
    /// 유언 설명이 적힌 작은 종이. 받은 내용을 그리는 것 외의 책임은 갖지 않는다.
    ///
    /// 자기 자리를 정하지 않는다. 언제 나오고 어디에 놓일지는 LSO_RewardBox만 안다.
    /// LSO_RewardCard와 같은 규칙이다.
    ///
    /// 씬 배선: 상자 아래에 두고 꺼둔 채로 시작한다. 상자가 필요할 때 켠다.
    /// </summary>
    public class LSO_WillNote : MonoBehaviour, LSO_IPoolable
    {
        [SerializeField] private SpriteRenderer iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;

        [Header("수치")]
        [Tooltip("비워두면 그 칸은 건너뛴다.\n" +
                 "차례대로 피해량 / 범위 / 지속시간 / 버프 / 디버프")]
        [SerializeField] private TMP_Text[] statTexts = new TMP_Text[5];

        private DLJ_WillDataSO _will;

        public DLJ_WillDataSO Will => _will;

        public void Bind(DLJ_WillDataSO will)
        {
            _will = will;

            if (will == null)
            {
                Debug.LogWarning($"{name}: 유언이 비어 있어 빈 종이로 둡니다.", this);
                Clear();
                return;
            }

            SetText(nameText, will.WillType.ToString());
            SetText(descriptionText, will.description);

            if (iconImage != null)
            {
                iconImage.sprite = will.icon;
                iconImage.enabled = will.icon != null;
            }

            // 0이면 칸을 비운다. "버프 : 0"은 효과가 있는 것처럼 읽힌다.
            SetStats(
                $"피해량 {will.DisplayDamage}",
                $"범위 {will.DisplayRange}",
                $"지속시간 {will.DisplayDuration}",
                will.DisplayBuffAmount != 0 ? $"버프 {will.DisplayBuffAmount}" : string.Empty,
                will.DisplayDebuffAmount != 0 ? $"디버프 {will.DisplayDebuffAmount}" : string.Empty);
        }

        private void SetStats(params string[] values)
        {
            if (statTexts == null) return;

            for (int i = 0; i < statTexts.Length; i++)
                SetText(statTexts[i], i < values.Length ? values[i] : string.Empty);
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label != null) label.text = value;
        }

        private void Clear()
        {
            SetText(nameText, string.Empty);
            SetText(descriptionText, string.Empty);

            if (iconImage != null) iconImage.enabled = false;

            SetStats(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        }

        public void OnSpawned()
        {
            _will = null;
        }

        public void OnDespawned()
        {
            _will = null;
        }
    }
}
